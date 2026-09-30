using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using NBShader;

namespace NBShaderEditor
{
    // A capability-specific Root: Graph properties do not contain ShaderLab's
    // foldout, keyword, or single-float packed-flag protocol. Keep Unity's
    // MaterialEditor in charge of property drawers; write only the existing
    // split packed-flag bits for controls with live Graph behavior.
    public sealed class NBShaderGraphRootItem : ShaderGUIRootItem
    {
        // Group only properties with a live Graph implementation. Unknown and
        // future properties stay in the native fallback below, without a second
        // inspector implementation or any NBShaderSyncService side effects.
        static readonly string[] BaseProperties =
        {
            "_BaseMap", "_Color", "_BaseBackColor", "_BaseColorIntensityForTimeline",
            "_ColorA", "_BaseMap_ST", "_BaseMapUVRotation",
            "_BaseMapUVRotationSpeed", "_BaseMapMaskMapOffset"
        };

        static readonly string[] MaskProperties =
        {
            "_Mask_Toggle", "_MaskMap", "_MaskMapVec", "_MaskRefineVec",
            "_MaskMapUVRotation", "_MaskMapRotationSpeed", "_MaskMapOffsetAnition"
        };

        static readonly string[] Overlay1Properties =
        {
            "_EmissionEnabled", "_EmissionMap", "_EmissionMapUVRotation",
            "_EmissionMapUVOffset", "_EmissionMapColor",
            "_EmissionMapColorIntensity", "_EmissionAlphaIntensity"
        };

        static readonly string[] Overlay2Properties =
        {
            "_ColorBlendMap_Toggle", "_ColorBlendMap", "_ColorBlendColor",
            "_ColorBlendColorIntensity", "_ColorBlendVec",
            "_ColorBlendMapOffset"
        };

        static readonly string[] RampProperties =
        {
            "_RampColorToggle", "_RampColorSourceMode", "_RampColorMap",
            "_RampColorMapOffset", "_RampColor0", "_RampColor1",
            "_RampColor2", "_RampColor3", "_RampColor4", "_RampColor5",
            "_RampColorAlpha0", "_RampColorAlpha1", "_RampColorAlpha2",
            "_RampColorCount", "_RampColorBlendColor"
        };

        static readonly string[] AdjustmentProperties =
        {
            "_HueShift", "_Contrast", "_ContrastMidColor",
            "_Saturability", "_BaseMapColorRefine"
        };

        static readonly string[] FresnelProperties =
        {
            "_fresnelEnabled", "_FresnelUnit", "_FresnelColor",
            "_FresnelRotation"
        };

        static readonly string[] DistanceFadeProperties =
        {
            "_DistanceFade_Toggle", "_Fade"
        };

        static readonly string[] SoftParticlesProperties =
        {
            "_SoftParticlesEnabled", "_SoftParticleFadeParams"
        };

        static readonly string[] DepthOutlineProperties =
        {
            "_DepthOutline_Toggle", "_DepthOutline_Color", "_DepthOutline_Vec"
        };

        static readonly string[] DissolveProperties =
        {
            "_Dissolve_Toggle", "_DissolveMap", "_Dissolve",
            "_DissolveOffsetRotateDistort", "_DissolveMask_Toggle",
            "_DissolveMaskMap", "_DissolveMaskMode", "_Dissolve_Vec2",
            "_DissolveLineColor"
        };

        static readonly string[] DissolveRampProperties =
        {
            "_Dissolve_useRampMap_Toggle", "_DissolveRampSourceMode",
            "_DissolveRampMap", "_NB_DissolveRampSTOverrideEnabled",
            "_NB_DissolveRampSTOverride", "_DissolveRampColor",
            "_DissolveRampColor0", "_DissolveRampColor1",
            "_DissolveRampColor2", "_DissolveRampColor3",
            "_DissolveRampColor4", "_DissolveRampColor5",
            "_DissolveRampAlpha0", "_DissolveRampAlpha1",
            "_DissolveRampAlpha2", "_DissolveRampCount"
        };

        static readonly string[] DistortionProperties =
        {
            "_NB_DistortionMode", "_NB_DistortionNoise", "_NB_DistortionIntensity",
            "_NB_DistortionAlphaPow", "_NB_DistortionAlphaMultiplier",
            "_NB_DistortionAlphaAdd"
        };

        readonly HashSet<string> _drawnProperties = new HashSet<string>(StringComparer.Ordinal);
        MaterialProperty[] _properties;
        readonly HashSet<string> _sharedPropertyNames = new HashSet<string>(StringComparer.Ordinal);

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
            => OnGUI(editor, properties, null);

        public void OnGUI(MaterialEditor editor, MaterialProperty[] properties,
            IEnumerable<string> sharedPropertyNames)
        {
            _sharedPropertyNames.Clear();
            if (sharedPropertyNames != null)
                foreach (string name in sharedPropertyNames) _sharedPropertyNames.Add(name);
            _properties = properties;
            base.OnGUI(editor, properties);
        }

        public override void OnChildOnGUI()
        {
            _drawnProperties.Clear();
            _drawnProperties.UnionWith(_sharedPropertyNames);
            DrawGroup(BaseProperties, "block.maintex", "Main Texture");
            DrawGroup(Overlay1Properties, "feature.叠加贴图1", "Overlay 1");
            DrawGroup(Overlay2Properties, "feature.叠加贴图2", "Overlay 2");
            DrawGroup(RampProperties, "feature.颜色映射", "Color Ramp");
            DrawGroup(AdjustmentProperties, "feature.颜色调整", "Color Adjustment");
            DrawGroup(FresnelProperties, "feature.菲涅尔", "Fresnel");
            if (TryGetVisibleProperty("_fresnelEnabled", out _))
            {
                DrawPackedFlag("feature.菲涅尔模式", "Fresnel Alpha Mode",
                    NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_FADE_ON, 0);
                DrawPackedFlag("feature.翻转菲涅尔", "Invert Fresnel",
                    NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON, 0);
                DrawPackedFlag("feature.菲涅尔颜色受Alpha影响", "Color Affected By Alpha",
                    NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA, 0);
            }
            DrawGroup(DistanceFadeProperties, "base.distanceFade", "Distance Fade");
            DrawGroup(SoftParticlesProperties, "base.softParticles", "Soft Particles");
            DrawGroup(DepthOutlineProperties, "feature.深度描边", "Depth Outline");
            DrawGroup(MaskProperties, "feature.遮罩", "Mask");
            DrawGroup(DissolveProperties, "feature.溶解", "Dissolve");
            if (TryGetVisibleProperty("_DissolveLineColor", out _))
                DrawPackedFlag("feature.溶解描边", "Dissolve Line",
                    NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK, 1);
            DrawGroup(DissolveRampProperties, "feature.溶解Ramp", "Dissolve Ramp");
            if (TryGetVisibleProperty("_DissolveRampColor", out _))
            {
                DrawPackedFlag("feature.溶解Ramp混合模式", "Dissolve Ramp Multiply",
                    NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY, 1);
                DrawDissolveRampWrapMode();
                DrawPackedFlag("feature.溶解Ramp无Mip", "Dissolve Ramp Force LOD 0",
                    NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_DISSOLVE_RAMPMAP, 2);
            }
            DrawGroup(DistortionProperties, "feature.扭曲", "Distort");

            // Graph properties can be added without changing this adapter.
            // The same MaterialEditor path draws every remaining visible input.
            foreach (MaterialProperty property in _properties)
            {
                if (IsVisible(property) && _drawnProperties.Add(property.name))
                    DrawProperty(property);
            }
        }

        void DrawGroup(string[] propertyNames, string localizationKey, string fallback)
        {
            bool hasVisibleProperty = false;
            foreach (string name in propertyNames)
            {
                if (TryGetVisibleProperty(name, out MaterialProperty property) &&
                    !_drawnProperties.Contains(property.name))
                {
                    hasVisibleProperty = true;
                    break;
                }
            }

            if (!hasVisibleProperty)
                return;

            // Reuse the common, non-persistent section label item. ShaderLab's
            // foldout items require properties that this Graph does not have.
            new SectionLabelItem(this, null,
                () => NBShaderInspectorLocalization.MakeInspectorContent(localizationKey, fallback))
                .OnGUI();

            foreach (string name in propertyNames)
            {
                if (TryGetVisibleProperty(name, out MaterialProperty property) &&
                    _drawnProperties.Add(property.name))
                    DrawProperty(property);
            }
        }

        void DrawPackedFlag(string localizationKey, string fallback, int flag, int word)
        {
            int bitIndex = 0;
            for (int bits = flag; bits > 1; bits >>= 1)
                bitIndex++;
            string prefix = word == 0 ? "_NB_Flags0" :
                word == 1 ? "_NB_Flags1" : "_NB_ForceNoMipFlags";
            string propertyName = prefix + (bitIndex < 16 ? "Lo16" : "Hi16");
            int sliceBit = 1 << (bitIndex & 15);
            bool first = false, value = false, mixed = false;
            foreach (UnityEngine.Object target in MatEditor.targets)
            {
                if (target is not Material material || !material.HasProperty(propertyName))
                    return;
                int slice = Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(propertyName), 0f, 65535f));
                bool enabled = (slice & sliceBit) != 0;
                if (!first) { value = enabled; first = true; }
                else if (value != enabled) mixed = true;
            }
            if (!first) return;

            GUIContent label = NBShaderInspectorLocalization.MakeInspectorContent(localizationKey, fallback);
            bool oldMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUI.Toggle(GetControlRect(), label, value);
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = oldMixed;
            if (!changed) return;

            Undo.RecordObjects(MatEditor.targets, label.text);
            foreach (UnityEngine.Object target in MatEditor.targets)
            {
                if (target is Material material && SetPackedFlag(material, propertyName, sliceBit, next))
                    EditorUtility.SetDirty(material);
            }
        }

        void DrawDissolveRampWrapMode()
        {
            const string lowName = "_NB_WrapFlagsLo16";
            const string highName = "_NB_WrapFlagsHi16";
            const int bit = NBShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP;
            bool first = false, mixed = false;
            int value = 0;
            foreach (UnityEngine.Object target in MatEditor.targets)
            {
                if (target is not Material material ||
                    !material.HasProperty(lowName) || !material.HasProperty(highName))
                    return;
                int low = Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(lowName), 0f, 65535f));
                int high = Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(highName), 0f, 65535f));
                int mode = ((low & bit) != 0 ? 1 : 0) | ((high & bit) != 0 ? 2 : 0);
                if (!first) { value = mode; first = true; }
                else if (value != mode) mixed = true;
            }
            if (!first) return;

            GUIContent label = NBShaderInspectorLocalization.MakeInspectorContent(
                "feature.溶解RampUV Wrap", "Dissolve Ramp Wrap");
            string[] options = { "Repeat", "Clamp", "Repeat U / Clamp V", "Clamp U / Repeat V" };
            bool oldMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(GetControlRect(), label.text, value, options);
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = oldMixed;
            if (!changed) return;

            Undo.RecordObjects(MatEditor.targets, label.text);
            foreach (UnityEngine.Object target in MatEditor.targets)
            {
                if (target is Material material &&
                    SetPackedWrapMode(material, lowName, highName, bit, next))
                    EditorUtility.SetDirty(material);
            }
        }

        static bool SetPackedWrapMode(Material material, string lowName,
            string highName, int bit, int mode)
        {
            if (!material.HasProperty(lowName) || !material.HasProperty(highName))
                return false;
            int low = Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(lowName), 0f, 65535f));
            int high = Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(highName), 0f, 65535f));
            int nextLow = (low & ~bit) | ((mode & 1) != 0 ? bit : 0);
            int nextHigh = (high & ~bit) | ((mode & 2) != 0 ? bit : 0);
            if (nextLow == low && nextHigh == high) return false;
            material.SetFloat(lowName, nextLow);
            material.SetFloat(highName, nextHigh);
            return true;
        }

        // Preserve every unrelated bit, including flags written by other
        // Graph features or VFX Output. No legacy single-float flag alias.
        static bool SetPackedFlag(Material material, string propertyName, int sliceBit, bool enabled)
        {
            if (!material.HasProperty(propertyName)) return false;
            int current = Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(propertyName), 0f, 65535f));
            int next = enabled ? current | sliceBit : current & ~sliceBit;
            if (next == current) return false;
            material.SetFloat(propertyName, next);
            return true;
        }

        bool TryGetVisibleProperty(string name, out MaterialProperty property)
        {
            property = null;
            if (!PropertyInfoDic.TryGetValue(name, out ShaderPropertyInfo info))
                return false;

            property = info.Property;
            return IsVisible(property);
        }

        static bool IsVisible(MaterialProperty property)
            => property != null && (property.propertyFlags &
                (ShaderPropertyFlags.HideInInspector | ShaderPropertyFlags.PerRendererData)) == 0;

        void DrawProperty(MaterialProperty property)
        {
            string label = GetLabel(property);
            float height = MatEditor.GetPropertyHeight(property, label);
            Rect rect = GetControlRect(height);
            MatEditor.ShaderProperty(rect, property, label);
        }

        static string GetLabel(MaterialProperty property)
        {
            string key;
            switch (property.name)
            {
                case "_BaseMap": key = "maintex.basemap"; break;
                case "_BaseBackColor": key = "base.backColor.color"; break;
                case "_BaseMapUVRotation": key = "maintex.rotation"; break;
                case "_BaseMapUVRotationSpeed": key = "maintex.rotationspeed"; break;
                case "_MaskMap": key = "feature.遮罩贴图"; break;
                case "_MaskMapUVRotation": key = "feature.遮罩旋转"; break;
                case "_MaskMapRotationSpeed": key = "feature.遮罩旋转速度"; break;
                case "_MaskMapOffsetAnition": key = "feature.遮罩偏移速度"; break;
                case "_DissolveMap": key = "feature.溶解贴图"; break;
                case "_DissolveMaskMap": key = "feature.溶解遮罩图"; break;
                case "_DissolveLineColor": key = "feature.溶解描边颜色"; break;
                case "_fresnelEnabled": key = "feature.菲涅尔"; break;
                case "_FresnelColor": key = "feature.菲涅尔颜色"; break;
                case "_DistanceFade_Toggle": key = "base.distanceFade"; break;
                case "_Fade": key = "base.distanceFade.range"; break;
                case "_SoftParticlesEnabled": key = "base.softParticles"; break;
                case "_SoftParticleFadeParams": key = "base.softParticles.range"; break;
                case "_DepthOutline_Toggle": key = "feature.深度描边"; break;
                case "_DepthOutline_Color": key = "feature.深度描边颜色"; break;
                case "_DepthOutline_Vec": key = "feature.深度描边距离"; break;
                case "_NB_DistortionMode": key = "feature.屏幕扰动模式"; break;
                case "_NB_DistortionIntensity": key = "feature.屏幕扭曲强度"; break;
                default: return property.displayName;
            }

            return NBShaderInspectorLocalization.MakeInspectorContent(key, property.displayName).text;
        }
    }
}
