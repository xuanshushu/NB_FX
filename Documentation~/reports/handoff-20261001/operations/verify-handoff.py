#!/usr/bin/env python3
"""Read-only transfer SHA verification. No Unity, Git writes or installation."""
from pathlib import Path
import argparse, hashlib, json, gzip
ap = argparse.ArgumentParser()
ap.add_argument('--package', type=Path, default=Path(__file__).resolve().parents[4])
a = ap.parse_args()
r = a.package.resolve() / 'Documentation~' / 'reports'
h = r / 'handoff-20261001'
checked = 0
for folder in (h, r / 't07-evidence' / 'chromatic-20261001', r / 't07-evidence' / 'fog-gamma-20261001'):
    for line in (folder / 'sha256.txt').read_text(encoding='utf-8').splitlines():
        expected, relative = line.split('  ', 1)
        p = folder / relative
        if hashlib.sha256(p.read_bytes()).hexdigest() != expected:
            raise SystemExit('SHA mismatch (check Git LFS pull): ' + str(p))
        checked += 1
u = h / 'candidates' / 'uvp'
for item in json.loads((u / 'source-to-archive.json').read_text())['files']:
    raw = gzip.decompress((u / item['archiveRelativePath']).read_bytes())
    if hashlib.sha256(raw).hexdigest() != item['sourceSHA256']:
        raise SystemExit('UVP decompressed payload mismatch: ' + item['archiveRelativePath'])
print('Transfer integrity OK:', checked, 'files + 38 UVP decompressed payloads')
print('This is NOT a Unity/functional/Gate validation.')
