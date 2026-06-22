using UnityEditor;
using UnityEngine.Rendering;

namespace NBShaderEditor
{
    public static class ShaderGUIUnityCompat
    {
        public static ShaderPropertyType GetPropertyType(MaterialProperty property)
        {
#if UNITY_6000_1_OR_NEWER
            return property.propertyType;
#else
#pragma warning disable 0618
            return (ShaderPropertyType)property.type;
#pragma warning restore 0618
#endif
        }

        public static bool HasHdrFlag(MaterialProperty property)
        {
#if UNITY_6000_1_OR_NEWER
            return (property.propertyFlags & ShaderPropertyFlags.HDR) != 0;
#else
#pragma warning disable 0618
            return (((ShaderPropertyFlags)property.flags) & ShaderPropertyFlags.HDR) != 0;
#pragma warning restore 0618
#endif
        }

    }
}
