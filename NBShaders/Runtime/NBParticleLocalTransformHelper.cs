using UnityEngine;

namespace NBShader
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class NBParticleLocalTransformHelper : MonoBehaviour
    {
        private const string ParticleBaseShaderName = "Effects/NBShader";
        private const string CustomLocalTransformKeyword = "_CUSTOM_LOCAL_TRANSFORM";

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
                _lastAppliedMaterial.DisableKeyword(CustomLocalTransformKeyword);
            }

            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

            material.SetMatrix(CustomLocalTransformLocalToWorldId, localToWorld);
            material.SetMatrix(CustomLocalTransformWorldToLocalId, worldToLocal);
            material.EnableKeyword(CustomLocalTransformKeyword);

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

            if (enabled)
            {
                material.EnableKeyword(CustomLocalTransformKeyword);
            }
            else
            {
                material.DisableKeyword(CustomLocalTransformKeyword);
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

            if (material.shader == null || material.shader.name != ParticleBaseShaderName)
            {
                material = null;
                LogWarningOnce("NBParticleLocalTransformHelper only supports ParticleBase materials using shader '" + ParticleBaseShaderName + "'.");
                return false;
            }

            return true;
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

            _lastAppliedMaterial.DisableKeyword(CustomLocalTransformKeyword);
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
