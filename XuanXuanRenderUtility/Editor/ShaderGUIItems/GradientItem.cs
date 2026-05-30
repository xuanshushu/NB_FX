using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class GradientItem : ShaderGUIItem
    {
        private static bool s_UndoRedoRegistered;
        private static int s_UndoRedoVersion;

        private readonly Func<GUIContent> _contentProvider;
        private readonly string[] _colorPropertyNames;
        private readonly string[] _alphaPropertyNames;
        private readonly int _maxCount;
        private readonly bool _hdr;
        private readonly ColorSpace _colorSpace;
        private readonly Func<bool> _isVisible;

        private ShaderPropertyInfo[] _colorPropertyInfos;
        private ShaderPropertyInfo[] _alphaPropertyInfos;
        private readonly Gradient _gradient = new Gradient();
        private GradientColorKey[] _colorKeys = Array.Empty<GradientColorKey>();
        private GradientAlphaKey[] _alphaKeys = Array.Empty<GradientAlphaKey>();
        private bool _gradientCacheDirty = true;
        private int _lastUndoRedoVersion = -1;

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
            GuiContent = _contentProvider();
            EnsureUndoRedoRegistered();
            CacheProperties();
            InitTriggerByChild();
        }

        private static void EnsureUndoRedoRegistered()
        {
            if (s_UndoRedoRegistered)
            {
                return;
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            s_UndoRedoRegistered = true;
        }

        private static void OnUndoRedoPerformed()
        {
            s_UndoRedoVersion++;
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

            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, GuiContent);
            }

            Gradient gradient = GetCachedGradient();
            using (ParentControlDisabledScope())
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = GradientPropertyHasMixedValue();
                bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, PropertyInfo.Property);
                using (new EditorGUIIndentLevelScope(0))
                {
                    gradient = EditorGUI.GradientField(ControlRect, GUIContent.none, gradient, _hdr, _colorSpace);
                }
                EndAnimatedPropertyBackground(animatedScope);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    WriteGradient(gradient);
                    MarkGradientCacheDirty();
                    OnEndChange();
                }
            }

            DrawResetButton();
        }

        private Gradient GetCachedGradient()
        {
            if (_lastUndoRedoVersion != s_UndoRedoVersion)
            {
                _lastUndoRedoVersion = s_UndoRedoVersion;
                MarkGradientCacheDirty();
            }

            if (_gradientCacheDirty)
            {
                ReadGradient();
                _gradientCacheDirty = false;
                GradientReflectionHelper.RefreshGradientData();
            }

            return _gradient;
        }

        private void MarkGradientCacheDirty()
        {
            _gradientCacheDirty = true;
        }

        private void ReadGradient()
        {
            bool hasColorProperties = _colorPropertyInfos.Length > 0;
            bool hasAlphaProperties = _alphaPropertyInfos.Length > 0;
            bool isBlackAndWhiteGradient = !hasColorProperties && hasAlphaProperties;
            GetGradientKeyCounts(out int colorKeyCount, out int alphaKeyCount);

            if (_colorKeys.Length != colorKeyCount)
            {
                _colorKeys = new GradientColorKey[colorKeyCount];
            }

            if (_alphaKeys.Length != alphaKeyCount)
            {
                _alphaKeys = new GradientAlphaKey[alphaKeyCount];
            }

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
                    color = packed;
                    colorTime = packed.a;
                }

                _colorKeys[i] = new GradientColorKey(color, colorTime);
            }

            for (int i = 0; i < alphaKeyCount; i++)
            {
                float alpha = 1f;
                float alphaTime = alphaKeyCount == 1 ? 0f : i / (float)(alphaKeyCount - 1);
                if (!isBlackAndWhiteGradient && hasAlphaProperties)
                {
                    TryReadAlphaKey(i, ref alpha, ref alphaTime);
                }

                _alphaKeys[i] = new GradientAlphaKey(alpha, alphaTime);
            }

            _gradient.SetKeys(_colorKeys, _alphaKeys);
        }

        private void GetGradientKeyCounts(out int colorKeyCount, out int alphaKeyCount)
        {
            int countValue = PropertyInfo.Property.intValue;
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
            time = componentIndex == 0 ? packed.y : packed.w;
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
                time = packed.y;
            }
            else
            {
                alpha = packed.z;
                time = packed.w;
            }
        }

        private void WriteGradient(Gradient gradient)
        {
            bool hasColorProperties = _colorPropertyInfos.Length > 0;
            bool hasAlphaProperties = _alphaPropertyInfos.Length > 0;
            bool isBlackAndWhiteGradient = !hasColorProperties && hasAlphaProperties;
            int countPropertyValue = PropertyInfo.Property.intValue;

            if (isBlackAndWhiteGradient)
            {
                int finalColorKeyCount = gradient.colorKeys.Length;
                if (finalColorKeyCount <= _maxCount)
                {
                    WriteBlackAndWhiteGradient(gradient, finalColorKeyCount);
                    PropertyInfo.Property.intValue = finalColorKeyCount;
                }

                return;
            }

            if (hasColorProperties)
            {
                int finalColorKeyCount = gradient.colorKeys.Length;
                if (finalColorKeyCount <= _maxCount)
                {
                    WriteColorKeys(gradient, finalColorKeyCount);
                    countPropertyValue &= 0xFFFF << 16;
                    countPropertyValue |= finalColorKeyCount;
                }
            }

            if (!isBlackAndWhiteGradient && hasAlphaProperties)
            {
                int finalAlphaKeyCount = gradient.alphaKeys.Length;
                if (finalAlphaKeyCount <= _maxCount)
                {
                    WriteAlphaKeys(gradient, finalAlphaKeyCount);
                    countPropertyValue &= 0xFFFF;
                    countPropertyValue |= finalAlphaKeyCount << 16;
                }
            }

            PropertyInfo.Property.intValue = countPropertyValue;
        }

        private void WriteColorKeys(Gradient gradient, int colorKeyCount)
        {
            for (int i = 0; i < colorKeyCount && i < _colorPropertyInfos.Length; i++)
            {
                if (_colorPropertyInfos[i] == null)
                {
                    continue;
                }

                GradientColorKey key = gradient.colorKeys[i];
                _colorPropertyInfos[i].Property.colorValue = new Color(key.color.r, key.color.g, key.color.b, key.time);
            }
        }

        private void WriteBlackAndWhiteGradient(Gradient gradient, int colorKeyCount)
        {
            int vectorCount = Mathf.CeilToInt(colorKeyCount / 2f);
            for (int i = 0; i < vectorCount && i < _alphaPropertyInfos.Length; i++)
            {
                if (_alphaPropertyInfos[i] == null)
                {
                    continue;
                }

                Vector4 value = Vector4.zero;
                GradientColorKey key0 = gradient.colorKeys[i * 2];
                value.x = key0.color.r;
                value.y = key0.time;
                if (i * 2 + 1 < colorKeyCount)
                {
                    GradientColorKey key1 = gradient.colorKeys[i * 2 + 1];
                    value.z = key1.color.r;
                    value.w = key1.time;
                }

                _alphaPropertyInfos[i].Property.vectorValue = value;
            }
        }

        private void WriteAlphaKeys(Gradient gradient, int alphaKeyCount)
        {
            int vectorCount = Mathf.CeilToInt(alphaKeyCount / 2f);
            for (int i = 0; i < vectorCount && i < _alphaPropertyInfos.Length; i++)
            {
                if (_alphaPropertyInfos[i] == null)
                {
                    continue;
                }

                Vector4 value = Vector4.zero;
                GradientAlphaKey key0 = gradient.alphaKeys[i * 2];
                value.x = key0.alpha;
                value.y = key0.time;
                if (i * 2 + 1 < alphaKeyCount)
                {
                    GradientAlphaKey key1 = gradient.alphaKeys[i * 2 + 1];
                    value.z = key1.alpha;
                    value.w = key1.time;
                }

                _alphaPropertyInfos[i].Property.vectorValue = value;
            }
        }

        private bool GradientPropertyHasMixedValue()
        {
            if (PropertyInfo == null || PropertyInfo.Property == null)
            {
                return false;
            }

            GetGradientKeyCounts(out int colorKeyCount, out int alphaKeyCount);
            bool hasMixedValue = PropertyInfo.Property.hasMixedValue;
            for (int i = 0; i < colorKeyCount && i < _colorPropertyInfos.Length; i++)
            {
                hasMixedValue |= _colorPropertyInfos[i]?.Property.hasMixedValue == true;
            }

            int alphaPropertyCount = Mathf.CeilToInt(alphaKeyCount / 2f);
            for (int i = 0; i < alphaPropertyCount && i < _alphaPropertyInfos.Length; i++)
            {
                hasMixedValue |= _alphaPropertyInfos[i]?.Property.hasMixedValue == true;
            }

            return hasMixedValue;
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)
            {
                bool isDefaultValue = IsCountDefault();
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
            ResetCountProperty();
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
            MarkGradientCacheDirty();
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private bool IsCountDefault()
        {
            if (PropertyInfo == null || PropertyInfo.Property == null || PropertyInfo.Property.hasMixedValue)
            {
                return false;
            }

            return PropertyInfo.Property.intValue == GetDefaultInt(PropertyInfo);
        }

        private void ResetCountProperty()
        {
            if (PropertyInfo?.Property == null)
            {
                return;
            }

            PropertyInfo.Property.intValue = GetDefaultInt(PropertyInfo);
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
                case MaterialProperty.PropType.Int:
                    return info.Property.intValue == GetDefaultInt(info);
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
                case MaterialProperty.PropType.Int:
                    info.Property.intValue = GetDefaultInt(info);
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

        private int GetDefaultInt(ShaderPropertyInfo info)
        {
            try
            {
                return RootItem.Shader.GetPropertyDefaultIntValue(info.Index);
            }
            catch (ArgumentException)
            {
                return Mathf.RoundToInt(GetDefaultScalar(info));
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
