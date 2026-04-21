using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderSyncService
    {
        private readonly NBShaderRootItem _rootItem;

        public NBShaderSyncService(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
        }

        public void ApplyTransparentMode(TransparentMode mode)
        {
            if (!_rootItem.PropertyInfoDic.ContainsKey("_ZWrite") || !_rootItem.PropertyInfoDic.ContainsKey("_QueueBias"))
            {
                return;
            }

            MaterialProperty zWriteProperty = _rootItem.PropertyInfoDic["_ZWrite"].Property;
            MaterialProperty queueBiasProperty = _rootItem.PropertyInfoDic["_QueueBias"].Property;
            int queueBias = Mathf.RoundToInt(queueBiasProperty.floatValue);

            switch (mode)
            {
                case TransparentMode.Opaque:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        mat.renderQueue = 2100 + queueBias;
                        mat.DisableKeyword("_ALPHATEST_ON");
                    }
                    break;

                case TransparentMode.Transparent:
                    zWriteProperty.floatValue = 0;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        mat.renderQueue = 3000 + queueBias;
                        mat.DisableKeyword("_ALPHATEST_ON");
                    }
                    break;

                case TransparentMode.CutOff:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        mat.renderQueue = 2450 + queueBias;
                        mat.EnableKeyword("_ALPHATEST_ON");
                    }
                    break;
            }
        }

        public void ApplyBlendMode(BlendMode mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                switch (mode)
                {
                    case BlendMode.Alpha:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Premultiply:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Additive:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Multiply:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.EnableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Opaque:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                }
            }
        }

        public void ApplyLightMode(FxLightMode mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                mat.DisableKeyword("_FX_LIGHT_MODE_UNLIT");
                mat.DisableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                mat.DisableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                mat.DisableKeyword("_FX_LIGHT_MODE_PBR");
                mat.DisableKeyword("_FX_LIGHT_MODE_SIX_WAY");

                switch (mode)
                {
                    case FxLightMode.UnLit:
                        mat.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                        break;
                    case FxLightMode.BlinnPhong:
                        mat.EnableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                        break;
                    case FxLightMode.HalfLambert:
                        mat.EnableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                        break;
                    case FxLightMode.PBR:
                        mat.EnableKeyword("_FX_LIGHT_MODE_PBR");
                        break;
                    case FxLightMode.SixWay:
                        mat.EnableKeyword("_FX_LIGHT_MODE_SIX_WAY");
                        break;
                }
            }
        }

        public void ApplyToggleKeyword(string keyword, bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (enabled)
                {
                    mat.EnableKeyword(keyword);
                }
                else
                {
                    mat.DisableKeyword(keyword);
                }
            }
        }
    }
}
