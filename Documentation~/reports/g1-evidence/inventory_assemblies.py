#!/usr/bin/env python3
"""Read-only T01 inventory of NB_FX assembly and package dependencies."""
import hashlib
import json
import re
from pathlib import Path

ROOT = next(path for path in Path(__file__).resolve().parents if (path / "Packages/NB_FX/package.json").is_file())
PACKAGE = ROOT / "Packages/NB_FX"
HERE = Path(__file__).resolve().parent

def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def record(path):
    return {"path": path.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

guid_map = {}
for location in (ROOT / "Packages", ROOT / "Library/PackageCache"):
    if not location.exists():
        continue
    for meta in location.rglob("*.asmdef.meta"):
        match = re.search(r"^guid: ([0-9a-f]+)$", meta.read_text(errors="ignore"), re.M)
        if match:
            asset = meta.with_suffix("")
            guid_map[match.group(1)] = {"name": read_json(asset)["name"], "path": asset.relative_to(ROOT).as_posix()}

assemblies = []
for path in sorted(PACKAGE.rglob("*.asmdef")):
    data = read_json(path)
    refs = []
    for value in data.get("references", []):
        target = guid_map.get(value[5:]) if value.startswith("GUID:") else None
        refs.append({"reference": value, "resolved": target})
    assemblies.append({**record(path), "name": data["name"], "includePlatforms": data.get("includePlatforms", []), "references": refs, "defineConstraints": data.get("defineConstraints", []), "versionDefines": data.get("versionDefines", [])})

assembly_references = []
for path in sorted(PACKAGE.rglob("*.asmref")):
    value = read_json(path)["reference"]
    assembly_references.append({**record(path), "reference": value, "resolved": guid_map.get(value[5:]) if value.startswith("GUID:") else None})

package_json = PACKAGE / "package.json"
manifest_json = ROOT / "Packages/manifest.json"
package = read_json(package_json)
manifest = read_json(manifest_json)
result = {
    "package": {**record(package_json), "name": package["name"], "unity": package.get("unity"), "dependencies": package.get("dependencies", {})},
    "projectManifest": {**record(manifest_json), "selectedDependencies": {k: v for k, v in manifest["dependencies"].items() if any(part in k for part in ("render-pipelines", "shadergraph", "visualeffectgraph", "cinemachine"))}},
    "assemblies": assemblies,
    "assemblyReferences": assembly_references,
    "note": "References are syntactic and unconditional unless their entire asmdef is excluded. Version Defines only supply symbols; this inventory does not prove behavior when a package is absent.",
}
output = HERE / "assembly-static.json"
output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": output.relative_to(ROOT).as_posix(), "asmdefCount": len(assemblies), "asmrefCount": len(assembly_references), "unresolved": sum(ref["resolved"] is None for item in assemblies for ref in item["references"] if ref["reference"].startswith("GUID:"))}, ensure_ascii=False))
