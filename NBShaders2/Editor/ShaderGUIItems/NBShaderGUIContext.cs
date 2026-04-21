using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderGUIContext
    {
        private readonly NBShaderRootItem _rootItem;

        public NBShaderGUIContext(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
        }

        public MeshSourceMode MeshSourceMode { get; private set; } = MeshSourceMode.UnKnowOrMixed;
        public TransparentMode TransparentMode { get; private set; } = TransparentMode.UnKnowOrMixed;
        public MixedBool UIEffectEnabled { get; private set; } = MixedBool.Mixed;
        public MixedBool UseGraphicMainTex { get; private set; } = MixedBool.Mixed;
        public FxLightMode FxLightMode { get; private set; } = FxLightMode.UnKnownOrMixedValue;

        public bool HasProperty(string propertyName)
        {
            return _rootItem.PropertyInfoDic.ContainsKey(propertyName);
        }

        public MaterialProperty GetProperty(string propertyName)
        {
            return _rootItem.PropertyInfoDic[propertyName].Property;
        }

        public void Refresh()
        {
            if (HasProperty("_MeshSourceMode"))
            {
                MaterialProperty meshSourceModeProperty = GetProperty("_MeshSourceMode");
                MeshSourceMode = meshSourceModeProperty.hasMixedValue
                    ? MeshSourceMode.UnKnowOrMixed
                    : (MeshSourceMode)meshSourceModeProperty.floatValue;
            }

            if (HasProperty("_TransparentMode"))
            {
                MaterialProperty transparentModeProperty = GetProperty("_TransparentMode");
                TransparentMode = transparentModeProperty.hasMixedValue
                    ? TransparentMode.UnKnowOrMixed
                    : (TransparentMode)transparentModeProperty.floatValue;
            }

            if (MeshSourceMode == MeshSourceMode.UnKnowOrMixed)
            {
                UIEffectEnabled = MixedBool.Mixed;
                UseGraphicMainTex = MixedBool.Mixed;
            }
            else
            {
                UIEffectEnabled = (int)MeshSourceMode >= 2 ? MixedBool.True : MixedBool.False;
                UseGraphicMainTex = MeshSourceMode == MeshSourceMode.UIEffectRawImage || MeshSourceMode == MeshSourceMode.UIEffectSprite
                    ? MixedBool.True
                    : MixedBool.False;
            }

            if (HasProperty("_FxLightMode"))
            {
                MaterialProperty lightModeProperty = GetProperty("_FxLightMode");
                FxLightMode = lightModeProperty.hasMixedValue
                    ? FxLightMode.UnKnownOrMixedValue
                    : (FxLightMode)lightModeProperty.floatValue;
            }
        }
    }
}
