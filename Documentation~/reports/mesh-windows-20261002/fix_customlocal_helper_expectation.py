from pathlib import Path
import hashlib,json
work=Path(__file__).resolve().parent;root=work.parents[1];p=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX/Tests/URP/Editor/G4GraphCustomLocalHelperTests.cs';before=p.read_bytes();s=before.decode('utf-8');backup=work/'G4GraphCustomLocalHelperTests-before-transform-authority.cs';assert not backup.exists();backup.write_bytes(before)
assert s.count('Rows(Material material,Matrix4x4 matrix)')==1;s=s.replace('Rows(Material material,Matrix4x4 matrix)','Rows(Material material,Transform transform)')
assert s.count('matrix.GetRow(row)')==1;s=s.replace('matrix.GetRow(row)','transform.localToWorldMatrix.GetRow(row)')
assert s.count('matrix.inverse.GetRow(row)')==1;s=s.replace('matrix.inverse.GetRow(row)','transform.worldToLocalMatrix.GetRow(row)')
assert s.count('go.transform.localToWorldMatrix);')==3;s=s.replace('go.transform.localToWorldMatrix);','go.transform);')
p.write_text(s,encoding='utf-8',newline='\n')
(work/'customlocal-helper-expectation-repair.json').write_text(json.dumps({'scope':'Fixture-only: production writer reads Transform.worldToLocalMatrix, not Matrix4x4.Inverse(Transform.localToWorldMatrix). Compare exact authoritative matrix rather than separate inverse algorithm. Original strict vector equality/lifecycle assertions retained, no product change.',
 'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(p.read_bytes()).hexdigest()},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Corrected fixture to compare Transform worldToLocal authority; exact equality kept')
