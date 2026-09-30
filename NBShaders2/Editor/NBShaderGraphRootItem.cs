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

        static readonly string[] DissolveProperties =
        {
            "_Dissolve_Toggle", "_DissolveMap", "_Dissolve",
            "_DissolveOffsetRotateDistort", "_DissolveMask_Toggle",
            "_DissolveMaskMap", "_DissolveMaskMode", "_Dissolve_Vec2",
            "_DissolveLineColor"
        };

        static readonly string[] DistortionProperties =
        {
            "_NB_DistortionMode", "_NB_DistortionNoise", "_NB_DistortionIntensity",
            "_NB_DistortionAlphaPow", "_NB_DistortionAlphaMultiplier",
            "_NB_DistortionAlphaAdd"
        };

        readonly HashSet<string> _drawnProperties = new HashSet<string>(StringComparer.Ordinal);
        MaterialProperty[] _properties;

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            _properties = properties;
            base.OnGUI(editor, properties);
        }

        public override void OnChildOnGUI()
        {
            _drawnProperties.Clear();
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
            DrawGroup(MaskProperties, "feature.遮罩", "Mask");
            DrawGroup(DissolveProperties, "feature.溶解", "Dissolve");
            if (TryGetVisibleProperty("_DissolveLineColor", out _))
                DrawPackedFlag("feature.溶解描边", "Dissolve Line",
                    NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK, 1);
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
            string prefix = word == 0 ? "_NB_Flags0" : "_NB_Flags1";
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
                case "_NB_DistortionMode": key = "feature.屏幕扰动模式"; break;
                case "_NB_DistortionIntensity": key = "feature.屏幕扭曲强度"; break;
                default: return property.displayName;
            }

            return NBShaderInspectorLocalization.MakeInspectorContent(key, property.displayName).text;
        }
    }
}
