"""
Safely remove null m_OpElementList entries and report true dangling local PPtrs.

Only treats a fileID as dangling when it is referenced AND not defined by any
`--- !u!CLASS &ID` header in the same YAML file.
"""
from __future__ import annotations

import re
from pathlib import Path

root = Path(r"F:/SelfMaj/NewFishFramework/NewFishFramework")
assets = root / "Assets" / "GameRes"

HEADER_RE = re.compile(r"^--- !u!\d+ &(-?\d+)", re.M)
LOCAL_ONLY_RE = re.compile(r"\{fileID:\s*(-?\d+)\}(?!\s*,)")


def defined_ids(text: str) -> set[str]:
    return set(HEADER_RE.findall(text))


def find_dangling(path: Path) -> list[tuple[int, str, str]]:
    text = path.read_text(encoding="utf-8")
    ids = defined_ids(text)
    dangling = []
    for i, line in enumerate(text.splitlines(), 1):
        if "guid:" in line:
            continue
        for m in LOCAL_ONLY_RE.finditer(line):
            fid = m.group(1)
            if fid == "0":
                continue
            if fid not in ids:
                dangling.append((i, fid, line.strip()[:140]))
    return dangling


def clean_opelement_nulls(text: str) -> tuple[str, int]:
    lines = text.splitlines(keepends=True)
    out: list[str] = []
    i = 0
    removed = 0
    while i < len(lines):
        raw = lines[i].rstrip("\r\n")
        m = re.match(r"^(\s*)- m_Target: \{fileID: 0\}\s*$", raw)
        if not m:
            out.append(lines[i])
            i += 1
            continue
        indent = m.group(1)
        i += 1
        removed += 1
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
    return "".join(out), removed


def main() -> None:
    focus = [
        assets / "Prefabs/Login/LoadingLoginUI.prefab",
        assets / "Prefabs/Login/LoginUI.prefab",
        assets / "Prefabs/Shop/UIShopView.prefab",
        assets / "Prefabs/TUI/Canvas/UI Main Menu.prefab",
        assets / "Prefabs/TUIStory/UIMapTravelView.prefab",
        assets / "Prefabs/Main/UIRoot.prefab",
    ]
    print("=== Dangling local PPtrs (true) ===")
    for path in focus:
        if not path.exists():
            continue
        dang = find_dangling(path)
        rel = str(path.relative_to(root)).replace("\\", "/")
        print(f"{rel}: {len(dang)}")
        for line, fid, snip in dang[:20]:
            print(f"  L{line} fileID={fid} :: {snip}")

    print("\n=== Remove null OpElement entries ===")
    for path in focus:
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8")
        new_text, n = clean_opelement_nulls(text)
        if n:
            if not new_text.endswith("\n"):
                new_text += "\n"
            path.write_text(new_text, encoding="utf-8")
        print(f"{path.relative_to(root)}: removed_null_opelements={n}")


if __name__ == "__main__":
    main()
