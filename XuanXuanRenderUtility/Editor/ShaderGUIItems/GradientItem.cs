using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class GradientItem : ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly string[] _colorPropertyNames;
        private readonly string[] _alphaPropertyNames;
        private readonly int _maxCount;
        private readonly bool _hdr;
        private readonly ColorSpace _colorSpace;
        private readonly Func<bool> _isVisible;

        private ShaderPropertyInfo[] _colorPropertyInfos;
        private ShaderPropertyInfo[] _alphaPropertyInfos;

        public GradientItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string countPropertyName,
            int maxCount,
            string[] colorPropertyNames,
            string[] alphaPropertyNames,
            Func<GUIContent> contentProvider,
            bool hdr = false,
            ColorSpace colorSpace = ColorSpace.Gamma,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = countPropertyName;
            _maxCount = Mathf.Max(2, maxCount);
            _colorPropertyNames = colorPropertyNames ?? Array.Empty<string>();
            _alphaPropertyNames = alphaPropertyNames ?? Array.Empty<string>();
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _hdr = hdr;
            _colorSpace = colorSpace;
            _isVisible = isVisible;
            CacheProperties();
            InitTriggerByChild();
        }

        private void CacheProperties()
        {
            _colorPropertyInfos = new ShaderPropertyInfo[_colorPropertyNames.Length];
            for (int i = 0; i < _colorPropertyNames.Length; i++)
            {
                if (RootItem.PropertyInfoDic.TryGetValue(_colorPropertyNames[i], out ShaderPropertyInfo info))
                {
                    _colorPropertyInfos[i] = info;
                }
            }

            _alphaPropertyInfos = new ShaderPropertyInfo[_alphaPropertyNames.Length];
            for (int i = 0; i < _alphaPropertyNames.Length; i++)
            {
                if (RootItem.PropertyInfoDic.TryGetValue(_alphaPropertyNames[i], out ShaderPropertyInfo info))
                {
                    _alphaPropertyInfos[i] = info;
                }
            }
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            CacheProperties();
            GetRect();
            EditorGUI.LabelField(LabelRect, _contentProvider());

            Gradient gradient = ReadGradient();
            EditorGUI.BeginChangeCheck();
            bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, PropertyInfo.Property);
            using (new EditorGUIIndentLevelScope(0))
            {
                gradient = EditorGUI.GradientField(ControlRect, GUIContent.none, gradient, _hdr, _colorSpace);
            }
            EndAnimatedPropertyBackground(animatedScope);
            if (EditorGUI.EndChangeCheck())
            {
                WriteGradient(gradient);
                OnEndChange();
            }

            DrawResetButton();
        }

        private Gradient ReadGradient()
        {
            bool hasColorProperties = _colorPropertyInfos.Length > 0;
            bool hasAlphaProperties = _alphaPropertyInfos.Length > 0;
            bool isBlackAndWhiteGradient = !hasColorProperties && hasAlphaProperties;
            GetGradientKeyCounts(out int colorKeyCount, out int alphaKeyCount);

            GradientColorKey[] colorKeys = new GradientColorKey[colorKeyCount];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[alphaKeyCount];

            for (int i = 0; i < colorKeyCount; i++)
            {
                Color color = Color.white;
                float colorTime = colorKeyCount == 1 ? 0f : i / (float)(colorKeyCount - 1);
                if (isBlackAndWhiteGradient)
                {
                    TryReadBlackAndWhiteKey(i, ref color, ref colorTime);
                }
                else if (i < _colorPropertyInfos.Length && _colorPropertyInfos[i] != null)
                {
                    Color packed = _colorPropertyInfos[i].Property.colorValue;
                    color = new Color(packed.r, packed.g, packed.b, 1f);
                    colorTime = Mathf.Clamp01(packed.a);
                }

                colorKeys[i] = new GradientColorKey(color, colorTime);
            }

            for (int i = 0; i < alphaKeyCount; i++)
            {
                float alpha = 1f;
                float alphaTime = alphaKeyCount == 1 ? 0f : i / (float)(alphaKeyCount - 1);
                if (!isBlackAndWhiteGradient && hasAlphaProperties)
                {
                    TryReadAlphaKey(i, ref alpha, ref alphaTime);
                }

                alphaKeys[i] = new GradientAlphaKey(alpha, alphaTime);
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private void GetGradientKeyCounts(out int colorKeyCount, out int alphaKeyCount)
        {
            int countValue = Mathf.RoundToInt(PropertyInfo.Property.floatValue);
            bool hasColorProperties = _colorPropertyInfos.Length > 0;
            bool hasAlphaProperties = _alphaPropertyInfos.Length > 0;

            if (hasColorProperties && hasAlphaProperties)
            {
                colorKeyCount = countValue & 0xFFFF;
                alphaKeyCount = countValue >> 16;
            }
            else
            {
                colorKeyCount = countValue;
                alphaKeyCount = 2;
            }

            colorKeyCount = Mathf.Clamp(colorKeyCount <= 0 ? 2 : colorKeyCount, 2, _maxCount);
            alphaKeyCount = Mathf.Clamp(alphaKeyCount <= 0 ? 2 : alphaKeyCount, 2, _maxCount);
        }

        private void TryReadBlackAndWhiteKey(int index, ref Color color, ref float time)
        {
            int vectorIndex = index / 2;
            int componentIndex = index % 2;
            if (vectorIndex >= _alphaPropertyInfos.Length || _alphaPropertyInfos[vectorIndex] == null)
            {
                return;
            }

            Vector4 packed = _alphaPropertyInfos[vectorIndex].Property.vectorValue;
            float value = componentIndex == 0 ? packed.x : packed.z;
            time = Mathf.Clamp01(componentIndex == 0 ? packed.y : packed.w);
            color = new Color(value, value, value, 1f);
        }

        private void TryReadAlphaKey(int index, ref float alpha, ref float time)
        {
            int vectorIndex = index / 2;
            int componentIndex = index % 2;
            if (vectorIndex >= _alphaPropertyInfos.Length || _alphaPropertyInfos[vectorIndex] == null)
            {
                return;
            }

            Vector4 packed = _alphaPropertyInfos[vectorIndex].Property.vectorValue;
            if (componentIndex == 0)
            {
                alpha = packed.x;
                time = Mathf.Clamp01(packed.y);
            }
            else
            {
                alpha = packed.z;
                time = Mathf.Clamp01(packed.w);
            }
        }

        private void WriteGradient(Gradient gradient)
        {
            bool hasColorProperties = _colorPropertyInfos.Length > 0;
            bool hasAlphaProperties = _alphaPropertyInfos.Length > 0;
            bool isBlackAndWhiteGradient = !hasColorProperties && hasAlphaProperties;
            int colorKeyCount = Mathf.Clamp(gradient.colorKeys.Length, 2, _maxCount);
            int alphaKeyCount = Mathf.Clamp(gradient.alphaKeys.Length, 2, _maxCount);

            if (isBlackAndWhiteGradient)
            {
                WriteBlackAndWhiteGradient(gradient, colorKeyCount);
                PropertyInfo.Property.floatValue = colorKeyCount;
                return;
            }

            for (int i = 0; i < Mathf.Min(colorKeyCount, _colorPropertyInfos.Length); i++)
            {
                GradientColorKey key = gradient.colorKeys[i];
                if (_colorPropertyInfos[i] != null)
                {
                    _colorPropertyInfos[i].Property.colorValue = new Color(key.color.r, key.color.g, key.color.b, key.time);
                }
            }

            if (hasAlphaProperties)
            {
                WriteAlphaKeys(gradient, alphaKeyCount);
            }

            PropertyInfo.Property.floatValue = hasColorProperties && hasAlphaProperties
                ? colorKeyCount | (alphaKeyCount << 16)
                : colorKeyCount;
        }

        private void WriteBlackAndWhiteGradient(Gradient gradient, int colorKeyCount)
        {
            for (int i = 0; i < _alphaPropertyInfos.Length; i++)
            {
                if (_alphaPropertyInfos[i] == null)
                {
                    continue;
                }

                GradientColorKey key0 = gradient.colorKeys[Mathf.Min(i * 2, colorKeyCount - 1)];
                GradientColorKey key1 = gradient.colorKeys[Mathf.Min(i * 2 + 1, colorKeyCount - 1)];
                _alphaPropertyInfos[i].Property.vectorValue = new Vector4(key0.color.r, key0.time, key1.color.r, key1.time);
            }
        }

        private void WriteAlphaKeys(Gradient gradient, int alphaKeyCount)
        {
            for (int i = 0; i < _alphaPropertyInfos.Length; i++)
            {
                if (_alphaPropertyInfos[i] == null)
                {
                    continue;
                }

                GradientAlphaKey key0 = gradient.alphaKeys[Mathf.Min(i * 2, alphaKeyCount - 1)];
                GradientAlphaKey key1 = gradient.alphaKeys[Mathf.Min(i * 2 + 1, alphaKeyCount - 1)];
                _alphaPropertyInfos[i].Property.vectorValue = new Vector4(key0.alpha, key0.time, key1.alpha, key1.time);
            }
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)
            {
                bool isDefaultValue = IsDefault(PropertyInfo);
                for (int i = 0; i < _colorPropertyInfos.Length; i++)
                {
                    isDefaultValue &= IsDefault(_colorPropertyInfos[i]);
                }

                for (int i = 0; i < _alphaPropertyInfos.Length; i++)
                {
                    isDefaultValue &= IsDefault(_alphaPropertyInfos[i]);
                }

                if (isDefaultValue == PropertyIsDefaultValue)
                {
                    return;
                }

                PropertyIsDefaultValue = isDefaultValue;
            }

            HasModified = !PropertyIsDefaultValue;
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            ResetProperty(PropertyInfo);
            for (int i = 0; i < _colorPropertyInfos.Length; i++)
            {
                ResetProperty(_colorPropertyInfos[i]);
            }

            for (int i = 0; i < _alphaPropertyInfos.Length; i++)
            {
                ResetProperty(_alphaPropertyInfos[i]);
            }

            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private bool IsDefault(ShaderPropertyInfo info)
        {
            if (info == null || info.Property == null || info.Property.hasMixedValue)
            {
                return false;
            }

            switch (info.Property.type)
            {
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    return Mathf.Approximately(info.Property.floatValue, RootItem.Shader.GetPropertyDefaultFloatValue(info.Index));
                case MaterialProperty.PropType.Color:
                    return Approximately(info.Property.colorValue, RootItem.Shader.GetPropertyDefaultVectorValue(info.Index));
                case MaterialProperty.PropType.Vector:
                    return Approximately(info.Property.vectorValue, RootItem.Shader.GetPropertyDefaultVectorValue(info.Index));
                default:
                    return Mathf.Approximately(info.Property.floatValue, GetDefaultScalar(info));
            }
        }

        private void ResetProperty(ShaderPropertyInfo info)
        {
            if (info == null || info.Property == null)
            {
                return;
            }

            switch (info.Property.type)
            {
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    info.Property.floatValue = RootItem.Shader.GetPropertyDefaultFloatValue(info.Index);
                    break;
                case MaterialProperty.PropType.Color:
                    info.Property.colorValue = RootItem.Shader.GetPropertyDefaultVectorValue(info.Index);
                    break;
                case MaterialProperty.PropType.Vector:
                    info.Property.vectorValue = RootItem.Shader.GetPropertyDefaultVectorValue(info.Index);
                    break;
                default:
                    info.Property.floatValue = GetDefaultScalar(info);
                    break;
            }
        }

        private float GetDefaultScalar(ShaderPropertyInfo info)
        {
            try
            {
                return RootItem.Shader.GetPropertyDefaultFloatValue(info.Index);
            }
            catch (ArgumentException)
            {
                return 2f;
            }
        }

        private static bool Approximately(Vector4 a, Vector4 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                   Mathf.Approximately(a.y, b.y) &&
                   Mathf.Approximately(a.z, b.z) &&
                   Mathf.Approximately(a.w, b.w);
        }
    }
}
