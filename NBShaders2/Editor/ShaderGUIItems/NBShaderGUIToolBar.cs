using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class NBShaderGUIToolBar
    {
        private const float ButtonWidth = 30f;
        private const float TierButtonWidth = 105f;
        private const string FeatureTierPropertyName = "_NBShader2FeatureTier";
        private const string HelpUrl = "https://owejt9diz2c.feishu.cn/wiki/BHz8wHHSjiYJagk7WrmcAcconlb?from=from_copylink";

        private readonly NBShaderRootItem _rootItem;

        private static Material copiedMaterialSnapshot;
        private static Shader copiedShader;

        public NBShaderGUIToolBar(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
        }

        public void DrawToolbar()
        {
            Rect toolbarRect = ShaderGUIItem.ApplyGlobalRectCompensation(
                EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
            GUI.Box(toolbarRect, GUIContent.none, EditorStyles.toolbar);

            Material material = MainMaterial;
            bool hasMaterial = material != null;
            float buttonX = toolbarRect.x;

            using (new EditorGUI.DisabledScope(!hasMaterial))
            {
                if (ToolbarButton(toolbarRect, ref buttonX, IconContent("Material On Icon", "ping", "跳到当前材质")))
                {
                    EditorGUIUtility.PingObject(material);
                }

                if (ToolbarButton(toolbarRect, ref buttonX, IconContent("TreeEditor.Trash", "cleanUnusedTextures", "清除没有使用的贴图")))
                {
                    CleanUnusedTextures();
                }

                if (ToolbarButton(toolbarRect, ref buttonX, TextContent("copy", "C", "复制材质属性")))
                {
                    CopyMaterial(material);
                }
            }

            using (new EditorGUI.DisabledScope(!hasMaterial || !HasCopiedMaterial()))
            {
                if (ToolbarButton(toolbarRect, ref buttonX, TextContent("paste", "V", "粘贴材质属性")))
                {
                    PasteMaterial();
                }
            }

            using (new EditorGUI.DisabledScope(!hasMaterial))
            {
                if (ToolbarButton(toolbarRect, ref buttonX, TierContent(), TierButtonWidth))
                {
                    ShowTierPopupMenu();
                }
            }

            using (new EditorGUI.DisabledScope(!hasMaterial))
            {
                if (ToolbarButton(toolbarRect, ref buttonX, TextContent("specialReset", "R", "特殊重置功能")))
                {
                    ShowResetPopupMenu();
                }

                if (ToolbarButton(toolbarRect, ref buttonX, IconContent("d_UnityEditor.HierarchyWindow", "collapseAll", "折叠所有控件")))
                {
                    CollapseAll();
                }
            }

            Rect helpRect = MakeToolbarButtonRect(toolbarRect, toolbarRect.xMax - ButtonWidth);
            if (GUI.Button(helpRect, IconContent("d__Help@2x", "help", "说明文档"), EditorStyles.toolbarButton))
            {
                Application.OpenURL(HelpUrl);
            }
        }

        private static bool ToolbarButton(Rect toolbarRect, ref float buttonX, GUIContent content)
        {
            return ToolbarButton(toolbarRect, ref buttonX, content, ButtonWidth);
        }

        private static bool ToolbarButton(Rect toolbarRect, ref float buttonX, GUIContent content, float width)
        {
            Rect buttonRect = MakeToolbarButtonRect(toolbarRect, buttonX, width);
            buttonX += width;
            return GUI.Button(buttonRect, content, EditorStyles.toolbarButton);
        }

        private static Rect MakeToolbarButtonRect(Rect toolbarRect, float x)
        {
            return MakeToolbarButtonRect(toolbarRect, x, ButtonWidth);
        }

        private static Rect MakeToolbarButtonRect(Rect toolbarRect, float x, float width)
        {
            return new Rect(x, toolbarRect.y, width, toolbarRect.height);
        }

        private static bool HasCopiedMaterial()
        {
            return copiedMaterialSnapshot != null;
        }

        private static void CopyMaterial(Material material)
        {
            if (copiedMaterialSnapshot != null)
            {
                UnityEngine.Object.DestroyImmediate(copiedMaterialSnapshot);
                copiedMaterialSnapshot = null;
                copiedShader = null;
            }

            if (material == null)
            {
                return;
            }

            copiedMaterialSnapshot = new Material(material)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            copiedShader = copiedMaterialSnapshot.shader;
        }

        private GUIContent TierContent()
        {
            string label = _rootItem.Context != null && _rootItem.Context.CurrentTierMixed
                ? Label("tierMixed", "Tier: Mixed")
                : string.Format(Label("tierFormat", "Tier: {0}"), GetTierLabel(CurrentTier));
            return new GUIContent(label, Tip("tier", "NBShader2 feature tier"));
        }

        private NBShader2FeatureTier CurrentTier
        {
            get
            {
                return _rootItem.Context != null ? _rootItem.Context.CurrentTier : NBShader2FeatureTier.Ultra;
            }
        }

        private void ShowTierPopupMenu()
        {
            GenericMenu menu = new GenericMenu();
            AddTierMenuItem(menu, NBShader2FeatureTier.Low);
            AddTierMenuItem(menu, NBShader2FeatureTier.Medium);
            AddTierMenuItem(menu, NBShader2FeatureTier.High);
            AddTierMenuItem(menu, NBShader2FeatureTier.Ultra);
            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private void AddTierMenuItem(GenericMenu menu, NBShader2FeatureTier tier)
        {
            bool isChecked = (_rootItem.Context == null || !_rootItem.Context.CurrentTierMixed) && CurrentTier == tier;
            menu.AddItem(new GUIContent(GetTierLabel(tier)), isChecked, () => SetFeatureTier(tier));
        }

        private static string GetTierLabel(NBShader2FeatureTier tier)
        {
            switch (tier)
            {
                case NBShader2FeatureTier.Low:
                    return Label("tierLow", "Low");
                case NBShader2FeatureTier.Medium:
                    return Label("tierMedium", "Medium");
                case NBShader2FeatureTier.High:
                    return Label("tierHigh", "High");
                case NBShader2FeatureTier.Ultra:
                    return Label("tierUltra", "Ultra");
                default:
                    return tier.ToString();
            }
        }

        private void SetFeatureTier(NBShader2FeatureTier tier)
        {
            RecordAllMaterials(UndoText("setTier", "Set NBShader2 Feature Tier"));

            if (_rootItem.PropertyInfoDic.TryGetValue(FeatureTierPropertyName, out ShaderPropertyInfo info))
            {
                info.Property.floatValue = (float)tier;
            }

            foreach (Material mat in Materials)
            {
                if (mat.HasProperty(FeatureTierPropertyName))
                {
                    mat.SetFloat(FeatureTierPropertyName, (float)tier);
                }

                ApplyFeatureTierRuntime(mat, tier);
            }

            FinishMaterialMutation();
        }

        private static void ApplyFeatureTierRuntime(Material material, NBShader2FeatureTier tier)
        {
            if (material == null)
            {
                return;
            }

            NBShader2FeatureRuntime.ApplyTier(material, tier);
        }

        private void ShowResetPopupMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent(Label("resetAll", "重置所有")), false, ResetAll);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(Label("resetSpecialUV", "重置特殊UV通道")), false, ResetSpecialUVChannel);
            menu.AddItem(new GUIContent(Label("resetTwirl", "重置旋转扭曲")), false, ResetTwirl);
            menu.AddItem(new GUIContent(Label("resetPolar", "重置极坐标")), false, ResetPolar);
            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private void CleanUnusedTextures()
        {
            RecordAllMaterials(UndoText("cleanUnusedTextures", "清除没有使用的贴图"));
            foreach (Material mat in Materials)
            {
                CleanUnusedTextureProperties(mat);
            }

            _rootItem.RequestClearUnusedTextureReferences();
            MarkAllMaterialsDirty();
        }

        private void PasteMaterial()
        {
            Material material = MainMaterial;
            if (material == null || !HasCopiedMaterial())
            {
                return;
            }

            Undo.RecordObject(material, UndoText("paste", "粘贴材质属性"));
            if (copiedShader != null)
            {
                material.shader = copiedShader;
            }

            material.CopyPropertiesFromMaterial(copiedMaterialSnapshot);

            EditorUtility.SetDirty(material);
            _rootItem.IsInit = true;
            _rootItem.SyncService?.SyncMaterialState();
            _rootItem.Context?.Refresh();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            GUIUtility.ExitGUI();
        }

        private void ResetSpecialUVChannel()
        {
            RecordAllMaterials(UndoText("resetSpecialUV", "重置特殊UV通道"));
            ResetSpecialUVChannelValues();
            FinishMaterialMutation();
        }

        private void ResetTwirl()
        {
            RecordAllMaterials(UndoText("resetTwirl", "重置旋转扭曲"));
            ResetTwirlValues();
            FinishMaterialMutation();
        }

        private void ResetPolar()
        {
            RecordAllMaterials(UndoText("resetPolar", "重置极坐标"));
            ResetPolarValues();
            FinishMaterialMutation();
        }

        private void ResetAll()
        {
            RecordAllMaterials(UndoText("resetAll", "重置所有特殊功能"));
            _rootItem.ExecuteResetAllItems();
            ResetSpecialUVChannelValues();
            ResetTwirlValues();
            ResetPolarValues();
            FinishMaterialMutation();
        }

        private void ResetSpecialUVChannelValues()
        {
            float defaultValue = GetDefaultFloat("_SpecialUVChannelMode", 0f);
            SetFloatProperty("_SpecialUVChannelMode", defaultValue);

            bool useTexcoord1 = Mathf.RoundToInt(defaultValue) == 0;
            for (int i = 0; i < _rootItem.ShaderFlags.Count; i++)
            {
                ShaderFlagsBase flags = _rootItem.ShaderFlags[i];
                if (useTexcoord1)
                {
                    flags.SetFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, index: 1);
                    flags.ClearFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                }
                else
                {
                    flags.ClearFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, index: 1);
                    flags.SetFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                }
            }
        }

        private void ResetTwirlValues()
        {
            float enabled = GetDefaultFloat("_UTwirlEnabled", 0f);
            SetFloatProperty("_UTwirlEnabled", enabled);
            SetVectorProperty("_TWParameter", GetDefaultVector("_TWParameter", new Vector4(0.5f, 0.5f, 0f, 0f)));
            SetFloatProperty("_TWStrength", GetDefaultFloat("_TWStrength", 1f));
            ApplyFlag(NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON, enabled > 0.5f, 0);
        }

        private void ResetPolarValues()
        {
            float enabled = GetDefaultFloat("_PolarCoordinatesEnabled", 0f);
            SetFloatProperty("_PolarCoordinatesEnabled", enabled);
            SetVectorProperty("_PCCenter", GetDefaultVector("_PCCenter", new Vector4(0.5f, 0.5f, 1f, 0f)));
            ApplyFlag(NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON, enabled > 0.5f, 0);
        }

        private void CollapseAll()
        {
            RecordAllMaterials(UndoText("collapseAll", "折叠所有控件"));
            foreach (KeyValuePair<string, ShaderPropertyInfo> pair in _rootItem.PropertyInfoDic)
            {
                if (!pair.Key.EndsWith("FoldOut", StringComparison.Ordinal))
                {
                    continue;
                }

                MaterialProperty property = pair.Value.Property;
                if (property.type == MaterialProperty.PropType.Float ||
                    property.type == MaterialProperty.PropType.Range)
                {
                    property.floatValue = 0f;
                }
            }

            MarkAllMaterialsDirty();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void CleanUnusedTextureProperties(Material mat)
        {
            if (mat == null || mat.shader == null)
            {
                return;
            }

            Shader shader = mat.shader;
            var shaderTexProps = new HashSet<string>();
            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    shaderTexProps.Add(ShaderUtil.GetPropertyName(shader, i));
                }
            }

            string[] texturePropertyNames = mat.GetTexturePropertyNames();
            foreach (string propertyName in texturePropertyNames)
            {
                if (shaderTexProps.Contains(propertyName) || mat.GetTexture(propertyName) == null)
                {
                    continue;
                }

                mat.SetTexture(propertyName, null);
                Debug.LogFormat(
                    mat,
                    NBShaderInspectorLocalization.Get(
                        "inspector.toolbar.cleanUnusedTextures.log",
                        "清理 {0} 的无效贴图属性: {1}"),
                    mat.name,
                    propertyName);
            }
        }

        private void FinishMaterialMutation()
        {
            _rootItem.SyncService?.SyncMaterialState();
            _rootItem.Context?.Refresh();
            MarkAllMaterialsDirty();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void ApplyFlag(int flagBits, bool enabled, int flagIndex)
        {
            for (int i = 0; i < _rootItem.ShaderFlags.Count; i++)
            {
                ShaderFlagsBase flags = _rootItem.ShaderFlags[i];
                if (enabled)
                {
                    flags.SetFlagBits(flagBits, index: flagIndex);
                }
                else
                {
                    flags.ClearFlagBits(flagBits, index: flagIndex);
                }
            }
        }

        private float GetDefaultFloat(string propertyName, float fallback)
        {
            return _rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info)
                ? _rootItem.Shader.GetPropertyDefaultFloatValue(info.Index)
                : fallback;
        }

        private Vector4 GetDefaultVector(string propertyName, Vector4 fallback)
        {
            return _rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info)
                ? _rootItem.Shader.GetPropertyDefaultVectorValue(info.Index)
                : fallback;
        }

        private void SetFloatProperty(string propertyName, float value)
        {
            if (_rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info))
            {
                info.Property.floatValue = value;
            }

            foreach (Material mat in Materials)
            {
                if (mat.HasProperty(propertyName))
                {
                    mat.SetFloat(propertyName, value);
                }
            }
        }

        private void SetVectorProperty(string propertyName, Vector4 value)
        {
            if (_rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info))
            {
                info.Property.vectorValue = value;
            }

            foreach (Material mat in Materials)
            {
                if (mat.HasProperty(propertyName))
                {
                    mat.SetVector(propertyName, value);
                }
            }
        }

        private void RecordAllMaterials(string undoName)
        {
            var objects = new List<UnityEngine.Object>();
            foreach (Material mat in Materials)
            {
                objects.Add(mat);
            }

            if (objects.Count > 0)
            {
                Undo.RecordObjects(objects.ToArray(), undoName);
            }
        }

        private void MarkAllMaterialsDirty()
        {
            foreach (Material mat in Materials)
            {
                EditorUtility.SetDirty(mat);
            }
        }

        private IEnumerable<Material> Materials
        {
            get
            {
                if (_rootItem.Mats == null)
                {
                    yield break;
                }

                for (int i = 0; i < _rootItem.Mats.Count; i++)
                {
                    Material mat = _rootItem.Mats[i];
                    if (mat != null)
                    {
                        yield return mat;
                    }
                }
            }
        }

        private Material MainMaterial
        {
            get
            {
                return _rootItem.Mats != null && _rootItem.Mats.Count > 0 ? _rootItem.Mats[0] : null;
            }
        }

        private static GUIContent IconContent(string iconName, string key, string fallbackTooltip)
        {
            GUIContent icon = EditorGUIUtility.IconContent(iconName);
            return new GUIContent(icon.image, Tip(key, fallbackTooltip));
        }

        private static GUIContent TextContent(string key, string fallbackLabel, string fallbackTooltip)
        {
            return NBShaderInspectorLocalization.MakeInspectorContent("toolbar." + key, fallbackLabel, fallbackTooltip);
        }

        private static string Label(string key, string fallback)
        {
            return NBShaderInspectorLocalization.Get("inspector.toolbar." + key + ".label", fallback);
        }

        private static string Tip(string key, string fallback)
        {
            return NBShaderInspectorLocalization.Get("inspector.toolbar." + key + ".tip", fallback);
        }

        private static string UndoText(string key, string fallback)
        {
            return NBShaderInspectorLocalization.Get("inspector.toolbar." + key + ".undo", fallback);
        }
    }
}
