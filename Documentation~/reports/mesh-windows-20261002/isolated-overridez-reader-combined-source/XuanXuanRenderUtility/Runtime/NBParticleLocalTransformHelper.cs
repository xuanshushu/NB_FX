using UnityEngine;

namespace NBShader
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class NBParticleLocalTransformHelper : MonoBehaviour
    {
        private const string NBShaderName = "Effects/NBShader";
        private const string LegacyShaderName = "Effects/NBShader(Legacy)";
        private const string CustomLocalTransformKeyword = "_CUSTOM_LOCAL_TRANSFORM";
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
        };

        private static readonly int CustomLocalTransformLocalToWorldId =
            Shader.PropertyToID("_CustomLocalTransformLocalToWorld");

        private static readonly int CustomLocalTransformWorldToLocalId =
            Shader.PropertyToID("_CustomLocalTransformWorldToLocal");

        private ParticleSystemRenderer _particleRenderer;
        private Material _runtimeMaterial;
        private Material _lastAppliedMaterial;
        private string _lastWarning;

        private void OnEnable()
        {
            CacheRenderer();
            ApplyCustomTransform();
        }

        private void LateUpdate()
        {
            ApplyCustomTransform();
        }

        private void OnValidate()
        {
            CacheRenderer();
            ApplyCustomTransform();
        }

        private void OnDisable()
        {
            SetCustomTransformKeyword(false);
        }

        private void OnDestroy()
        {
            SetCustomTransformKeyword(false);
        }

        private void CacheRenderer()
        {
            if (_particleRenderer == null)
            {
                TryGetComponent(out _particleRenderer);
            }
        }

        private void ApplyCustomTransform()
        {
            if (!TryGetWritableMaterial(out Material material))
            {
                DisableLastAppliedKeyword();
                return;
            }

            if (_lastAppliedMaterial != null && _lastAppliedMaterial != material)
            {
                SetMaterialMode(_lastAppliedMaterial,false);
            }

            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

            if(IsGraphMatrixProtocol(material))
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
            }

            _lastAppliedMaterial = material;
            _lastWarning = null;
        }

        private void SetCustomTransformKeyword(bool enabled)
        {
            Material material = GetExistingMaterial();
            if (material == null)
            {
                return;
            }

            SetMaterialMode(material,enabled);
            if (!enabled)
            {
                if (material == _lastAppliedMaterial)
                {
                    _lastAppliedMaterial = null;
                }
            }
        }

        private Material GetExistingMaterial()
        {
            if (_lastAppliedMaterial != null)
            {
                return _lastAppliedMaterial;
            }

            CacheRenderer();
            if (_particleRenderer == null)
            {
                return null;
            }

            if (Application.isPlaying)
            {
                return _runtimeMaterial;
            }

            return _particleRenderer.sharedMaterial;
        }

        private bool TryGetWritableMaterial(out Material material)
        {
            CacheRenderer();
            if (_particleRenderer == null)
            {
                material = null;
                LogWarningOnce("NBParticleLocalTransformHelper requires a ParticleSystemRenderer on the same GameObject.");
                return false;
            }

            material = Application.isPlaying ? GetRuntimeMaterial() : _particleRenderer.sharedMaterial;
            if (material == null)
            {
                LogWarningOnce("NBParticleLocalTransformHelper could not find a material on the ParticleSystemRenderer.");
                return false;
            }

            if (material.shader == null || (!IsSupportedShader(material.shader) && !IsGraphMatrixProtocol(material)))
            {
                material = null;
                LogWarningOnce("NBParticleLocalTransformHelper only supports NBShader materials using shader '" +
                               NBShaderName + "' or '" + LegacyShaderName + "'.");
                return false;
            }

            return true;
        }

        private static bool IsSupportedShader(Shader shader)
        {
            return shader.name == NBShaderName || shader.name == LegacyShaderName;
        }

        // Graph stores exposed rows in UnityPerMaterial; keep the same helper
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

        private Material GetRuntimeMaterial()
        {
            if (_runtimeMaterial == null && _particleRenderer != null)
            {
                _runtimeMaterial = _particleRenderer.material;
            }

            return _runtimeMaterial;
        }

        private void DisableLastAppliedKeyword()
        {
            if (_lastAppliedMaterial == null)
            {
                return;
            }

            SetMaterialMode(_lastAppliedMaterial,false);
            _lastAppliedMaterial = null;
        }

        private void LogWarningOnce(string message)
        {
            if (_lastWarning == message)
            {
                return;
            }

            _lastWarning = message;
            Debug.LogWarning(message, this);
        }
    }
}
