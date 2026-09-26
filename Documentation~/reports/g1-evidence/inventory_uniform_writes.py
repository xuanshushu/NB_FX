#!/usr/bin/env python3
"""List direct assignments to NBShaderInput UnityPerMaterial fields in current HLSL.

This is a lexical audit of active-looking lines, not preprocessed variant reachability.
The function name is assigned from the nearest preceding declaration in these files.
"""
import hashlib
import json
import pathlib
import re

ROOT = next(p for p in pathlib.Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
BASE = ROOT / "Packages/NB_FX/NBShaders2/Shader/HLSL"
FILES = [BASE / "NBShaderInput.hlsl", BASE / "NBShaderForwardPass.hlsl"]
inventory = json.loads(pathlib.Path(__file__).with_name("properties-static.json").read_text())
fields = {x["name"] for x in inventory["cbuffer_fields"]} | {"time"}
write_pattern = re.compile(r"\b(?P<name>_[A-Za-z0-9_]+|time)\b(?P<component>\.[xyzwrgba]{1,4})?\s*(?P<op>\+=|-=|\*=|/=|(?<![=!<>])=(?!=))")
func_pattern = re.compile(r"^\s*(?:[A-Za-z_][A-Za-z0-9_]*(?:[1-4](?:x[1-4])?)?\s+)+(?P<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(")
rows = []
for path in FILES:
    current_function = None
    for line_no, raw in enumerate(path.read_text(errors="replace").splitlines(), 1):
        line = raw.split("//", 1)[0]
        function = func_pattern.match(line)
        if function and not line.lstrip().startswith("return "):
            current_function = function.group("name")
        for match in write_pattern.finditer(line):
            if match.group("name") not in fields:
                continue
            # A field declaration is before CBUFFER_END, not a write.
            rows.append({
                "file": str(path.relative_to(ROOT)),
                "line": line_no,
                "function_candidate": current_function,
                "field": match.group("name"),
                "component": match.group("component") or "",
                "operator": match.group("op"),
                "source": line.strip(),
            })
output = {
    "source_sha256": {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in FILES},
    "note": "Lexical direct assignments only. Conditional compilation and indirect mutation require human review. No claim that writes are legal or functional on every backend.",
    "count": len(rows),
    "fields": sorted({row["field"] for row in rows}),
    "writes": rows,
}
pathlib.Path(__file__).with_name("uniform-writes.json").write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n")
print(json.dumps({"count": len(rows), "fields": output["fields"]}, ensure_ascii=False, indent=2))
