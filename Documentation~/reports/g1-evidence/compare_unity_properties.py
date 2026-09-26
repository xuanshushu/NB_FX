#!/usr/bin/env python3
"""Compare the captured Unity Shader API property list with the static T01 inventory."""
import collections
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
static_path = HERE / "properties-static.json"
unity_path = HERE / "unity-properties-command.json"
static = json.loads(static_path.read_text(encoding="utf-8"))["properties"]
unity_response = json.loads(unity_path.read_text(encoding="utf-8"))
assert unity_response["success"] and unity_response["data"]["result"]["success"]
unity = [entry.rsplit(":", 1) for entry in unity_response["data"]["result"]["result"].split("|")]
assert len(unity) == len(static)

def normalized_type(value):
    value = value.lower()
    if value.startswith("range"):
        return "Range"
    if value in ("2d", "cube"):
        return "Texture"
    if value == "integer":
        return "Int"
    return value.title()

name_mismatches = []
type_mismatches = []
for index, (property_entry, (unity_name, unity_type)) in enumerate(zip(static, unity)):
    if property_entry["name"] != unity_name:
        name_mismatches.append(index)
    if normalized_type(property_entry["type"]) != unity_type:
        type_mismatches.append(index)

result = {
    "command": "unity --json command eval (Shader.GetPropertyCount/GetPropertyName/GetPropertyType)",
    "unityVersion": "6000.3.18f1",
    "staticCount": len(static),
    "unityCount": len(unity),
    "nameOrderMatches": not name_mismatches,
    "normalizedTypeMatches": not type_mismatches,
    "nameMismatchIndices": name_mismatches,
    "typeMismatchIndices": type_mismatches,
    "unityTypes": dict(sorted(collections.Counter(kind for _, kind in unity).items())),
    "inputSha256": {
        path.name: hashlib.sha256(path.read_bytes()).hexdigest()
        for path in (static_path, unity_path)
    },
}
(HERE / "property-crosscheck.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
assert result["nameOrderMatches"] and result["normalizedTypeMatches"]
print(json.dumps(result, ensure_ascii=False))
