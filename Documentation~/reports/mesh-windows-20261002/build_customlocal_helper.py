from pathlib import Path
import hashlib,json
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX';out=work/'customlocal-helper-preview';assert not out.exists()
path='XuanXuanRenderUtility/Runtime/NBParticleLocalTransformHelper.cs';before=(package/path).read_bytes();s=before.decode('utf-8')
def one(old,new,n=1):
    global s
    assert s.count(old)==n,(old,s.count(old));s=s.replace(old,new)
one('        private const string CustomLocalTransformKeyword = "_CUSTOM_LOCAL_TRANSFORM";', '''        private const string CustomLocalTransformKeyword = "_CUSTOM_LOCAL_TRANSFORM";
        private const string GraphShaderName = "NB FX/Shader Graph/NBShaderGraph";
        private static readonly int GraphCustomLocalToggleId = Shader.PropertyToID("_NB_CustomLocalTransform");
        private static readonly int[] GraphLocalToWorldRowIds =
        {
            Shader.PropertyToID("_NB_CustomLocalToWorld0"),Shader.PropertyToID("_NB_CustomLocalToWorld1"),
            Shader.PropertyToID("_NB_CustomLocalToWorld2"),Shader.PropertyToID("_NB_CustomLocalToWorld3")
        };
        private static readonly int[] GraphWorldToLocalRowIds =
        {
            Shader.PropertyToID("_NB_CustomWorldToLocal0"),Shader.PropertyToID("_NB_CustomWorldToLocal1"),
            Shader.PropertyToID("_NB_CustomWorldToLocal2"),Shader.PropertyToID("_NB_CustomWorldToLocal3")
        };''')
one('                _lastAppliedMaterial.DisableKeyword(CustomLocalTransformKeyword);','                SetMaterialMode(_lastAppliedMaterial,false);')
one('''            material.SetMatrix(CustomLocalTransformLocalToWorldId, localToWorld);
            material.SetMatrix(CustomLocalTransformWorldToLocalId, worldToLocal);
            material.EnableKeyword(CustomLocalTransformKeyword);''','''            if(IsGraphMatrixProtocol(material))
            {
                for(int row=0;row<4;row++)
                {
                    material.SetVector(GraphLocalToWorldRowIds[row],localToWorld.GetRow(row));
                    material.SetVector(GraphWorldToLocalRowIds[row],worldToLocal.GetRow(row));
                }
                SetMaterialMode(material,true);
            }
            else
            {
                material.SetMatrix(CustomLocalTransformLocalToWorldId, localToWorld);
                material.SetMatrix(CustomLocalTransformWorldToLocalId, worldToLocal);
                material.EnableKeyword(CustomLocalTransformKeyword);
            }''')
one('''            if (enabled)
            {
                material.EnableKeyword(CustomLocalTransformKeyword);
            }
            else
            {
                material.DisableKeyword(CustomLocalTransformKeyword);''','''            SetMaterialMode(material,enabled);
            if (!enabled)
            {''')
one('if (material.shader == null || !IsSupportedShader(material.shader))','if (material.shader == null || (!IsSupportedShader(material.shader) && !IsGraphMatrixProtocol(material)))')
one('        private Material GetRuntimeMaterial()', '''        // Graph stores exposed rows in UnityPerMaterial; keep the same helper
        // and lifecycle rather than a second global matrix writer or keyword.
        private static bool IsGraphMatrixProtocol(Material material)
        {
            if(material==null || material.shader==null || material.shader.name!=GraphShaderName ||
                !material.HasProperty(GraphCustomLocalToggleId))return false;
            for(int row=0;row<4;row++)
                if(!material.HasProperty(GraphLocalToWorldRowIds[row]) || !material.HasProperty(GraphWorldToLocalRowIds[row]))return false;
            return true;
        }
        private static void SetMaterialMode(Material material,bool enabled)
        {
            if(material==null)return;
            if(IsGraphMatrixProtocol(material))material.SetFloat(GraphCustomLocalToggleId,enabled?1f:0f);
            else if(enabled)material.EnableKeyword(CustomLocalTransformKeyword);
            else material.DisableKeyword(CustomLocalTransformKeyword);
        }

        private Material GetRuntimeMaterial()''')
one('            _lastAppliedMaterial.DisableKeyword(CustomLocalTransformKeyword);','            SetMaterialMode(_lastAppliedMaterial,false);')
target=out/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(s,encoding='utf-8',newline='\n')
(out/'manifest.json').write_text(json.dumps({'scope':'Preview only: existing helper writes8 exposed Graph rows andeffectiveFloat; same lifecycle/runtime material andold Matrix/keyword path. Full schema andexactshader guards, no assembly dependency added, no Unity verification yet.',
 'path':path,'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(target.read_bytes()).hexdigest(),'installed':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Prepared same-helper Graph row writer, old material path retained')
