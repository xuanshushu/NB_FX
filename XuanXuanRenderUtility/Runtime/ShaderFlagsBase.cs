using System;
using UnityEngine;

namespace NBShader
{
    public abstract class ShaderFlagsBase
    {

        private Material _material;

        public Material material
        {
            get { return _material; }
        }

        protected ShaderFlagsBase(Material material)
        {
            _material = material;
        }

        public void SetMaterial(Material material)
        {
            _material = material;
        }

        public Material GetMaterial()
        {
            return _material;
        }

        public abstract int GetShaderFlagsId(int index = 0);
        protected abstract string GetShaderFlagsName(int index = 0);

        private void SetIntValue(Material material, int flagBits, int index = 0)
        {
#if UNITY_EDITOR
            material.SetInteger(GetShaderFlagsId(index), flagBits);
#else
        material.SetInteger(GetShaderFlagsId(index), flagBits);
#endif
        }

        public void SetFlagBits(int flagBits, MaterialPropertyBlock propertyBlock = null, int index = 0)
        {
            if (propertyBlock is null)
            {
                if (_material is null) return;
                int flags = _material.GetInteger(GetShaderFlagsId(index));
                SetIntValue(_material, flags | flagBits, index);
            }
            else
            {
                int flags = propertyBlock.GetInt(GetShaderFlagsId(index));
                propertyBlock.SetInt(GetShaderFlagsId(index), flags | flagBits);
            }
        }

        public void ClearFlagBits(int flagBits, MaterialPropertyBlock propertyBlock = null, int index = 0)
        {
            if (propertyBlock is null)
            {
                if (_material is null) return;
                int flags = _material.GetInteger(GetShaderFlagsId(index));
                SetIntValue(_material, flags & ~flagBits, index);
            }
            else
            {
                int flags = propertyBlock.GetInteger(GetShaderFlagsId(index));
                propertyBlock.SetInteger(GetShaderFlagsId(index), flags & ~flagBits);
            }
        }

        public bool CheckFlagBits(int flagBits, MaterialPropertyBlock propertyBlock = null, int index = 0)
        {
            int flags = 0;
            if (propertyBlock is null)
            {
                if (_material is null) throw new NullReferenceException("material");
                flags = _material.GetInteger(GetShaderFlagsId(index));
            }
            else
            {
                flags = propertyBlock.GetInteger(GetShaderFlagsId(index));
            }

            return (flags & flagBits) != 0;
        }
    }
}