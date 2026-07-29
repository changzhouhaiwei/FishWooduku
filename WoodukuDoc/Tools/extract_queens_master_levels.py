import argparse
import json
import sys
from pathlib import Path


def parse_args():
    parser = argparse.ArgumentParser(description="Extract compatible Queens Master boards.")
    parser.add_argument("--analysis-root", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args()


def main():
    args = parse_args()
    sys.path.insert(0, str(args.analysis_root / "pydeps"))

    import UnityPy
    from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

    type_tree = TypeTreeGenerator("6000.3.15f1")
    type_tree.load_local_dll_folder(str(args.analysis_root / "Cpp2IL_out"))

    data_path = args.analysis_root / "base" / "assets" / "bin" / "Data" / "data.unity3d"
    environment = UnityPy.load(str(data_path))
    environment.typetree_generator = type_tree

    levels = {}
    level_order = []
    object_order = []

    for obj in environment.objects:
        if obj.type.name != "MonoBehaviour":
            continue

        try:
            head = obj.parse_monobehaviour_head()
            script = head.m_Script.deref_parse_as_object()
            class_name = script.m_ClassName
        except Exception:
            continue

        if class_name == "LevelDataScriptable":
            tree = obj.read_typetree()
            levels[obj.path_id] = (tree["m_Name"], tree["data"])
            object_order.append(obj.path_id)
        elif class_name == "LevelOrder":
            tree = obj.read_typetree()
            level_order = [item["m_PathID"] for item in tree["levelOrder"]["levels"]]

    def convert(path_id):
        source_name, data = levels[path_id]
        size_x = data["sizeX"]
        size_y = data["sizeY"]
        regions = list(data["gridColours"])
        queens = list(data["queensGrid"])

        if size_x != size_y or size_x < 2 or size_x > 12:
            return None

        size = size_x
        if len(regions) != size * size or len(queens) != size * size:
            return None

        if len(set(regions)) != size:
            return None

        queen_indices = [index for index, value in enumerate(queens) if value != 0]
        if len(queen_indices) != size:
            return None

        solution_cols = [-1] * size
        used_cols = set()
        for index in queen_indices:
            row, col = divmod(index, size)
            if solution_cols[row] >= 0 or col in used_cols:
                return None
            solution_cols[row] = col
            used_cols.add(col)

        if any(col < 0 for col in solution_cols):
            return None

        fixed_cells = [
            {"r": index // size, "c": index % size}
            for index, value in enumerate(queens)
            if value == 2
        ]

        return {
            "sourceName": source_name,
            "difficulty": int(data["levelDifficulty"]),
            "difficultyScore": int(data["levelDifficultyValue"]),
            "size": size,
            "regions": regions,
            "solutionCols": solution_cols,
            "fixedQueenCells": fixed_cells,
        }

    ordered_ids = []
    seen = set()
    for path_id in level_order + object_order:
        if path_id in seen or path_id not in levels:
            continue
        seen.add(path_id)
        ordered_ids.append(path_id)

    output_levels = []
    rejected = 0
    for path_id in ordered_ids:
        converted = convert(path_id)
        if converted is None:
            rejected += 1
            continue
        output_levels.append(converted)

    payload = {
        "source": "Queens Master 2.3.0",
        "unityVersion": "6000.3.15f1",
        "levels": output_levels,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(payload, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )

    print(f"physical_levels={len(levels)}")
    print(f"ordered_unique={len(ordered_ids)}")
    print(f"compatible_levels={len(output_levels)}")
    print(f"rejected_levels={rejected}")
    print(f"output={args.output}")


if __name__ == "__main__":
    main()
