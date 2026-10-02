from pathlib import Path
import json
p=Path(__file__).resolve().parents[1]/'NBFXMeshValidation-20261002/Packages/NB_FX/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
s=p.read_text(encoding='utf-8');decoder=json.JSONDecoder();position=0;rows=[]
while position<len(s):
    while position<len(s) and s[position].isspace():position+=1
    if position==len(s):break
    o,position=decoder.raw_decode(s,position)
    name=o.get('m_OverrideReferenceName')
    if name in ('_CylinderMatrix0','_NB_TierAllowNoise','_NB_Flags0Lo16','_NB_CustomDataFlag0Lo16','_NB_CustomDataFlag1Lo16'):
        rows.append({k:v for k,v in o.items() if k in ('m_Name','m_OverrideReferenceName','m_GeneratePropertyBlock','m_Hidden','m_Exposed','m_Value','m_HLSLDeclarationOverride')})
print(json.dumps(rows,indent=2))
