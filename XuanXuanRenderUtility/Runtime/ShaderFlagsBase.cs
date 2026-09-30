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

        // One physical word access seam; the default integer Material/MPB contract is unchanged.
        protected virtual int ReadWord(int propertyId, MaterialPropertyBlock propertyBlock = null)
        {
            return propertyBlock is null
                ? _material.GetInteger(propertyId)
                : propertyBlock.GetInteger(propertyId);
        }

        protected virtual void WriteWord(int propertyId, int value, MaterialPropertyBlock propertyBlock = null)
        {
            if (propertyBlock is null)
            {
                _material.SetInteger(propertyId, value);
            }
            else
            {
                propertyBlock.SetInteger(propertyId, value);
            }
        }

        public void SetFlagBits(int flagBits, MaterialPropertyBlock propertyBlock = null, int index = 0)
        {
            if (propertyBlock is null)
            {
                if (_material is null) return;
                int flags = ReadWord(GetShaderFlagsId(index));
                WriteWord(GetShaderFlagsId(index), flags | flagBits);
            }
            else
            {
                int flags = ReadWord(GetShaderFlagsId(index), propertyBlock);
                WriteWord(GetShaderFlagsId(index), flags | flagBits, propertyBlock);
            }
        }

        public void ClearFlagBits(int flagBits, MaterialPropertyBlock propertyBlock = null, int index = 0)
        {
            if (propertyBlock is null)
            {
                if (_material is null) return;
                int flags = ReadWord(GetShaderFlagsId(index));
                WriteWord(GetShaderFlagsId(index), flags & ~flagBits);
            }
            else
            {
                int flags = ReadWord(GetShaderFlagsId(index), propertyBlock);
                WriteWord(GetShaderFlagsId(index), flags & ~flagBits, propertyBlock);
            }
        }

        public bool CheckFlagBits(int flagBits, MaterialPropertyBlock propertyBlock = null, int index = 0)
        {
            int flags = 0;
            if (propertyBlock is null)
            {
                if (_material is null) throw new NullReferenceException("material");
                flags = ReadWord(GetShaderFlagsId(index));
            }
            else
            {
                flags = ReadWord(GetShaderFlagsId(index), propertyBlock);
            }

            return (flags & flagBits) != 0;
        }
    }
}
