import collections
import re
from pathlib import Path

root = Path(r"F:/SelfMaj/NewFishFramework/NewFishFramework")
assets = root / "Assets"

# Collect GUIDs from Assets + Packages + Library/PackageCache
guid_to_path = {}
search_roots = [
    assets,
    root / "Packages",
    root / "Library" / "PackageCache",
]
for search_root in search_roots:
    if not search_root.exists():
        continue
    for meta in search_root.rglob("*.meta"):
        try:
            text = meta.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            continue
        m = re.search(r"^guid:\s*([0-9a-fA-F]{32})", text, re.M)
        if m:
            guid_to_path[m.group(1).lower()] = str(meta)[:-5]

targets = []
for p in (assets / "GameRes").rglob("*"):
    if p.suffix.lower() in (".prefab", ".unity"):
        targets.append(p)
# Also include Settings scenes if any under GameRes only per request focus
for p in (assets / "Settings").rglob("*.unity"):
    targets.append(p)

script_ref_re = re.compile(
    r"m_Script:\s*\{fileID:\s*(\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*\d+\}"
)
script_zero_re = re.compile(r"^\s*m_Script:\s*\{fileID:\s*0\}\s*$")
source_prefab_re = re.compile(
    r"m_SourcePrefab:\s*\{fileID:\s*(\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*\d+\}"
)
object_header_re = re.compile(r"^--- !u!(\d+) &(-?\d+)(?:\s+stripped)?")

missing_scripts = []  # (file, line, guid_or_zero, component_fileid, go_hint)
missing_prefabs = []
unique_missing_script_guids = collections.Counter()
unique_missing_prefab_guids = collections.Counter()

for f in targets:
    rel = str(f.relative_to(root)).replace("\\", "/")
    try:
        text = f.read_text(encoding="utf-8", errors="ignore")
    except Exception as e:
        print("READ FAIL", rel, e)
        continue

    lines = text.splitlines()
    current_obj = None
    current_fileid = None
    for i, line in enumerate(lines, 1):
        hm = object_header_re.match(line)
        if hm:
            current_obj = hm.group(1)
            current_fileid = hm.group(2)
        if script_zero_re.match(line):
            missing_scripts.append((rel, i, "fileID:0", current_fileid))
            unique_missing_script_guids["fileID:0"] += 1

    for m in script_ref_re.finditer(text):
        guid = m.group(2).lower()
        if guid not in guid_to_path:
            line = text[: m.start()].count("\n") + 1
            missing_scripts.append((rel, line, guid, None))
            unique_missing_script_guids[guid] += 1

    for m in source_prefab_re.finditer(text):
        guid = m.group(2).lower()
        if guid not in guid_to_path:
            line = text[: m.start()].count("\n") + 1
            # get PrefabInstance fileID by scanning backwards
            prefab_fileid = None
            start = text[: m.start()].rfind("--- !u!")
            if start >= 0:
                hm = object_header_re.match(text[start:].splitlines()[0])
                if hm:
                    prefab_fileid = hm.group(2)
            missing_prefabs.append((rel, line, guid, prefab_fileid))
            unique_missing_prefab_guids[guid] += 1

print(f"GUID database size: {len(guid_to_path)}")
print(f"Library/PackageCache exists: {(root / 'Library' / 'PackageCache').exists()}")

print("\n=== MISSING SCRIPTS (true) ===")
print("count", len(missing_scripts))
print("unique guids:")
for g, c in unique_missing_script_guids.most_common():
    print(f"  {g}: {c}")
for item in missing_scripts:
    print(item)

print("\n=== MISSING PREFABS (true) ===")
print("count", len(missing_prefabs))
print("unique guids:")
for g, c in unique_missing_prefab_guids.most_common():
    print(f"  {g}: {c}")
for item in missing_prefabs:
    print(item)

print("\n=== SUMMARY BY FILE ===")
by_file = collections.defaultdict(lambda: {"scripts": 0, "prefabs": 0})
for rel, *_ in missing_scripts:
    by_file[rel]["scripts"] += 1
for rel, *_ in missing_prefabs:
    by_file[rel]["prefabs"] += 1
for rel, d in sorted(by_file.items()):
    print(f"{rel}: missing_scripts={d['scripts']} missing_prefabs={d['prefabs']}")

if not missing_scripts and not missing_prefabs:
    print("\nClean: no missing script/prefab GUID references in GameRes (+Settings).")
