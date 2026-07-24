"""
Remove missing scripts and missing PrefabInstances from Unity YAML
(.prefab / .unity) under Assets/GameRes (and Settings scenes).
"""
from __future__ import annotations

import collections
import re
import shutil
from pathlib import Path

root = Path(r"F:/SelfMaj/NewFishFramework/NewFishFramework")
assets = root / "Assets"
backup_dir = root / "Tools" / "_missing_refs_backup"

HEADER_RE = re.compile(r"^--- !u!(\d+) &(-?\d+)( stripped)?\s*$")
GUID_RE = re.compile(r"guid:\s*([0-9a-fA-F]{32})")
SCRIPT_GUID_RE = re.compile(
    r"m_Script:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*\d+\}"
)
SCRIPT_ZERO_RE = re.compile(r"^\s*m_Script:\s*\{fileID:\s*0\}\s*$", re.M)
SOURCE_PREFAB_RE = re.compile(
    r"m_SourcePrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*\d+\}"
)
PREFAB_INSTANCE_REF_RE = re.compile(r"m_PrefabInstance:\s*\{fileID:\s*(-?\d+)\}")
FATHER_RE = re.compile(r"m_Father:\s*\{fileID:\s*(-?\d+)\}")
GAMEOBJECT_RE = re.compile(r"m_GameObject:\s*\{fileID:\s*(-?\d+)\}")
COMPONENT_LINE_RE = re.compile(r"^\s*- component:\s*\{fileID:\s*(-?\d+)\}\s*$")
CHILD_LINE_RE = re.compile(r"^\s*- \{fileID:\s*(-?\d+)\}\s*$")
ADDED_OBJECT_RE = re.compile(r"addedObject:\s*\{fileID:\s*(-?\d+)\}")
FILEID_ANY_RE = re.compile(r"\{fileID:\s*(-?\d+)(?:,|\})")


def build_guid_db() -> set[str]:
    guids: set[str] = set()
    for search_root in (
        assets,
        root / "Packages",
        root / "Library" / "PackageCache",
    ):
        if not search_root.exists():
            continue
        for meta in search_root.rglob("*.meta"):
            try:
                text = meta.read_text(encoding="utf-8", errors="ignore")
            except Exception:
                continue
            m = re.search(r"^guid:\s*([0-9a-fA-F]{32})", text, re.M)
            if m:
                guids.add(m.group(1).lower())
    return guids


def split_docs(text: str) -> tuple[str, list[tuple[str, str]]]:
    """Return (preamble, [(header_line, body_including_header)])."""
    lines = text.splitlines(keepends=True)
    preamble_parts: list[str] = []
    docs: list[tuple[str, str]] = []
    i = 0
    while i < len(lines) and not lines[i].startswith("--- !u!"):
        preamble_parts.append(lines[i])
        i += 1
    while i < len(lines):
        header = lines[i].rstrip("\r\n")
        start = i
        i += 1
        while i < len(lines) and not lines[i].startswith("--- !u!"):
            i += 1
        body = "".join(lines[start:i])
        docs.append((header, body))
    return "".join(preamble_parts), docs


def parse_header(header: str) -> tuple[str | None, str | None, bool]:
    m = HEADER_RE.match(header)
    if not m:
        return None, None, False
    return m.group(1), m.group(2), bool(m.group(3))


def collect_targets() -> list[Path]:
    targets: list[Path] = []
    game_res = assets / "GameRes"
    if game_res.exists():
        for p in game_res.rglob("*"):
            if p.suffix.lower() in (".prefab", ".unity"):
                targets.append(p)
    settings = assets / "Settings"
    if settings.exists():
        for p in settings.rglob("*.unity"):
            targets.append(p)
    return targets


def clean_file(path: Path, valid_guids: set[str]) -> dict:
    text = path.read_text(encoding="utf-8")
    preamble, docs = split_docs(text)

    # Index docs
    doc_infos = []
    for idx, (header, body) in enumerate(docs):
        class_id, file_id, stripped = parse_header(header)
        doc_infos.append(
            {
                "idx": idx,
                "header": header,
                "body": body,
                "class_id": class_id,
                "file_id": file_id,
                "stripped": stripped,
            }
        )

    remove_ids: set[str] = set()
    removed_prefab_instances: list[str] = []
    removed_missing_scripts: list[str] = []

    # 1) Missing PrefabInstances
    for info in doc_infos:
        if info["class_id"] != "1001":
            continue
        m = SOURCE_PREFAB_RE.search(info["body"])
        if not m:
            continue
        guid = m.group(1).lower()
        if guid not in valid_guids:
            remove_ids.add(info["file_id"])
            removed_prefab_instances.append(f"{info['file_id']}@{guid}")
            # objects added onto the missing prefab
            for am in ADDED_OBJECT_RE.finditer(info["body"]):
                remove_ids.add(am.group(1))

    # 2) Stripped objects belonging to missing PrefabInstances
    changed = True
    while changed:
        changed = False
        for info in doc_infos:
            fid = info["file_id"]
            if fid is None or fid in remove_ids:
                continue
            pm = PREFAB_INSTANCE_REF_RE.search(info["body"])
            if pm and pm.group(1) in remove_ids:
                remove_ids.add(fid)
                changed = True
                continue
            # Cascade: transforms whose father is removed
            fm = FATHER_RE.search(info["body"])
            if fm and fm.group(1) in remove_ids:
                remove_ids.add(fid)
                changed = True
                continue
            # Components / objects referencing removed GameObject
            gm = GAMEOBJECT_RE.search(info["body"])
            if gm and gm.group(1) in remove_ids:
                remove_ids.add(fid)
                changed = True
                continue

    # If we marked a Transform for removal, also remove its GameObject via reverse lookup
    # (already covered if m_GameObject on transform points to GO - but GO itself may not
    # reference transform in a way that cascades). Pull GameObject from transform docs.
    for info in doc_infos:
        if info["file_id"] not in remove_ids:
            continue
        if info["class_id"] in ("4", "224"):  # Transform / RectTransform
            gm = GAMEOBJECT_RE.search(info["body"])
            if gm and gm.group(1) != "0":
                remove_ids.add(gm.group(1))

    # Cascade again for components of newly removed GameObjects
    changed = True
    while changed:
        changed = False
        for info in doc_infos:
            fid = info["file_id"]
            if fid is None or fid in remove_ids:
                continue
            gm = GAMEOBJECT_RE.search(info["body"])
            if gm and gm.group(1) in remove_ids:
                remove_ids.add(fid)
                changed = True
                continue
            fm = FATHER_RE.search(info["body"])
            if fm and fm.group(1) in remove_ids:
                remove_ids.add(fid)
                changed = True

    # 3) Missing scripts on remaining MonoBehaviours (class 114)
    for info in doc_infos:
        if info["file_id"] in remove_ids:
            continue
        if info["class_id"] != "114":
            continue
        body = info["body"]
        if SCRIPT_ZERO_RE.search(body):
            remove_ids.add(info["file_id"])
            removed_missing_scripts.append(f"{info['file_id']}@fileID:0")
            continue
        sm = SCRIPT_GUID_RE.search(body)
        if sm and sm.group(1).lower() not in valid_guids:
            remove_ids.add(info["file_id"])
            removed_missing_scripts.append(f"{info['file_id']}@{sm.group(1).lower()}")

    if not remove_ids:
        return {
            "path": str(path.relative_to(root)).replace("\\", "/"),
            "changed": False,
            "removed_docs": 0,
            "removed_prefab_instances": [],
            "removed_missing_scripts": [],
        }

    # 4) Rebuild docs: drop removed, scrub lists, null dangling local PPtrs
    local_ref_re = re.compile(r"\{fileID:\s*(-?\d+)\}(?!\s*,)")

    def scrub_line(line: str) -> str:
        def repl(m: re.Match) -> str:
            fid = m.group(1)
            if fid in remove_ids:
                return "{fileID: 0}"
            return m.group(0)

        # Do not touch external refs that include guid on the same line
        if "guid:" in line:
            return line
        return local_ref_re.sub(repl, line)

    def drop_null_opelements(body: str) -> str:
        lines = body.splitlines(keepends=True)
        out: list[str] = []
        i = 0
        while i < len(lines):
            raw = lines[i].rstrip("\r\n")
            m = re.match(r"^(\s*)- m_Target: \{fileID: 0\}\s*$", raw)
            if not m:
                out.append(lines[i])
                i += 1
                continue
            indent = m.group(1)
            i += 1
            # skip rest of this list entry
            while i < len(lines):
                raw2 = lines[i].rstrip("\r\n")
                if re.match(rf"^{re.escape(indent)}- m_Target:", raw2):
                    break
                mfield = re.match(r"^(\s*)([A-Za-z_].*):", raw2)
                if (
                    mfield
                    and len(mfield.group(1)) <= len(indent)
                    and not raw2.lstrip().startswith("-")
                ):
                    break
                i += 1
        text = "".join(out)
        # Normalize emptied lists: `m_OpElementList:\n  m_Next` -> `m_OpElementList: []`
        text = re.sub(
            r"(^[ \t]*m_OpElementList:)\s*\n(?=[ \t]*[A-Za-z_])",
            r"\1 []\n",
            text,
            flags=re.M,
        )
        return text

    kept: list[str] = []
    for info in doc_infos:
        if info["file_id"] in remove_ids:
            continue
        body = info["body"]
        new_lines: list[str] = []
        lines = body.splitlines(keepends=True)
        in_children = False
        for line in lines:
            raw = line.rstrip("\r\n")
            stripped = raw.lstrip()
            indent = len(raw) - len(stripped)

            if stripped.startswith("m_Children:"):
                in_children = True
                new_lines.append(line)
                continue
            if in_children:
                if stripped and not stripped.startswith("-") and indent <= 2:
                    in_children = False
                else:
                    ch = CHILD_LINE_RE.match(raw)
                    if ch and ch.group(1) in remove_ids:
                        continue

            cm = COMPONENT_LINE_RE.match(raw)
            if cm and cm.group(1) in remove_ids:
                continue

            # drop bare list entries pointing at removed ids (e.g. m_ComponentList)
            bare = CHILD_LINE_RE.match(raw)
            if bare and bare.group(1) in remove_ids:
                continue

            new_lines.append(scrub_line(line))

        kept.append(drop_null_opelements("".join(new_lines)))

    new_text = preamble + "".join(kept)
    if not new_text.endswith("\n"):
        new_text += "\n"

    # Backup then write
    rel = path.relative_to(root)
    bak = backup_dir / rel
    bak.parent.mkdir(parents=True, exist_ok=True)
    if not bak.exists():
        shutil.copy2(path, bak)

    path.write_text(new_text, encoding="utf-8")

    return {
        "path": str(rel).replace("\\", "/"),
        "changed": True,
        "removed_docs": len(remove_ids),
        "removed_prefab_instances": removed_prefab_instances,
        "removed_missing_scripts": removed_missing_scripts,
    }


def main() -> None:
    valid = build_guid_db()
    print(f"Valid GUID count: {len(valid)}")
    results = []
    for path in collect_targets():
        results.append(clean_file(path, valid))

    changed = [r for r in results if r["changed"]]
    print(f"\nFiles changed: {len(changed)}")
    for r in changed:
        print(f"\n{r['path']}")
        print(f"  removed docs/ids: {r['removed_docs']}")
        if r["removed_prefab_instances"]:
            print("  missing PrefabInstances:")
            for x in r["removed_prefab_instances"]:
                print(f"    - {x}")
        if r["removed_missing_scripts"]:
            print("  missing scripts:")
            for x in r["removed_missing_scripts"]:
                print(f"    - {x}")

    if not changed:
        print("Nothing to clean.")


if __name__ == "__main__":
    main()
