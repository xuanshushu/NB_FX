using System;
using System.Collections.Generic;
using System.Text;
using NBShader;
using NBShaderEditor;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [InitializeOnLoad]
    public static class NBShaderFeatureLevelSettingsProvider
    {
        internal const string SettingsPath = NBFXProjectSettings.SettingsPath;
        private const float FeatureColumnWidth = 240f;
        private const float TierColumnWidth = 92f;
        private const float CostColumnWidth = 84f;
        private const float EffectColumnWidth = 280f;
        private const float DescriptionColumnWidth = 230f;
        private const float RowHeight = 22f;
        private const float HeaderHeight = 24f;
        private const float TableMinHeight = 360f;
        private const float TableMaxHeight = 640f;
        private const float TableWidth = FeatureColumnWidth + TierColumnWidth * 4f + CostColumnWidth + EffectColumnWidth + DescriptionColumnWidth;
        private const float RowIndentWidth = 14f;
        private const string FoldoutSessionPrefix = "NBShaderFeatureLevelSettingsProvider.Foldout.";

        private sealed class RowContentCache
        {
            public GUIContent rowContent;
            public GUIContent effectContent;
            public GUIContent descriptionContent;
        }

        private static readonly NBShaderFeatureTier[] Tiers =
        {
            NBShaderFeatureTier.Low,
            NBShaderFeatureTier.Medium,
            NBShaderFeatureTier.High,
            NBShaderFeatureTier.Ultra
        };

        private static readonly Dictionary<string, RowContentCache> s_RowContentCache =
            new Dictionary<string, RowContentCache>(StringComparer.Ordinal);
        private static readonly Dictionary<NBShaderFeaturePerformanceCost, GUIContent> s_CostContentCache =
            new Dictionary<NBShaderFeaturePerformanceCost, GUIContent>();
        private static readonly GUIContent[] s_QualitySummaryContents = new GUIContent[Tiers.Length];
        private static readonly string[] s_QualitySummaryTexts = new string[Tiers.Length];
        private static readonly StringBuilder s_QualitySummaryBuilder = new StringBuilder(128);

        private static Vector2 s_TableScrollPosition;
        private static string s_ContentCacheLanguage;
        private static GUIContent s_ProviderLabelContent;
        private static GUIContent s_QualityDescriptionContent;
        private static GUIContent s_NoValueContent;
        private static string s_QualityMenuTipText;
        private static int s_QualitySummaryHash;

        static NBShaderFeatureLevelSettingsProvider()
        {
            NBFXProjectSettings.RegisterSettingsSection(
                "NBShaderFeatureLevels",
                GetProviderLabelContent,
                OnGUI,
                new[]
                {
                    "NBShader",
                    "Feature",
                    "Tier",
                    "Strip",
                    "Keyword",
                    "Quality",
                    "Apply",
                    "Material",
                    "Loaded Materials",
                    "Project Materials",
                    "应用",
                    "材质",
                    "等级",
                    "分级",
                    "剔除"
                },
                100);
        }

        private static void OnGUI(string searchContext)
        {
            EnsureContentCacheForCurrentLanguage();

            var settings = NBShaderFeatureLevelProjectSettings.instance;
            settings.EnsureInitialized();

            var changed = false;

            EditorGUILayout.HelpBox(
                Text(
                    "featureLevel.help.message",
                    "Configure NBShader managed Catalog keywords and shader passes per tier, and bind Unity Quality levels. Build scripts select the build stripping tier through NBShaderFeatureLevelEditorAPI.OverrideBuildStripExplicitTier. Runtime settings assets can be updated from their own Inspector or Editor API. Catalog-external features are ignored."),
                MessageType.Info);

            changed |= DrawFeatureLevelTable(settings);

            EditorGUILayout.Space();
            changed |= DrawButtons(settings);

            if (changed)
                settings.SaveProjectSettings();
        }

        private static bool DrawFeatureLevelTable(NBShaderFeatureLevelProjectSettings settings)
        {
            var changed = false;

            EditorGUILayout.LabelField(
                Text("featureLevel.table.title", "Feature Level Matrix"),
                EditorStyles.boldLabel);

            s_TableScrollPosition = EditorGUILayout.BeginScrollView(
                s_TableScrollPosition,
                true,
                true,
                GUILayout.MinHeight(TableMinHeight),
                GUILayout.MaxHeight(TableMaxHeight));

            using (new GUILayout.VerticalScope(GUILayout.Width(TableWidth)))
            {
                DrawTableHeader();
                DrawQualityBindingRow(settings);
                DrawTableSeparator();
                changed |= DrawFeatureRows(settings);
            }

            EditorGUILayout.EndScrollView();

            return changed;
        }

        private static void DrawTableHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(HeaderHeight)))
            {
                DrawHeaderCell(
                    Content(
                        "featureLevel.column.feature",
                        "Config / Feature",
                        "Tier settings and managed Catalog features."),
                    FeatureColumnWidth);

                for (var i = 0; i < Tiers.Length; i++)
                    DrawHeaderCell(GetTierContent(Tiers[i]), TierColumnWidth);

                DrawHeaderCell(
                    Content(
                        "featureLevel.column.cost",
                        "Performance Cost",
                        "Estimated shader cost when this feature is enabled."),
                    CostColumnWidth);

                DrawHeaderCell(
                    Content(
                        "featureLevel.column.effect",
                        "Feature Effect",
                        "Short description of what this keyword enables."),
                    EffectColumnWidth);

                DrawHeaderCell(
                    Content(
                        "featureLevel.column.description",
                        "Raw Keyword / Note",
                        "Raw shader keyword or configuration note."),
                    DescriptionColumnWidth);
            }
        }

        private static void DrawHeaderCell(GUIContent content, float width)
        {
            GUILayout.Label(content, EditorStyles.toolbarButton, GUILayout.Width(width), GUILayout.Height(HeaderHeight));
        }

        private static void DrawQualityBindingRow(NBShaderFeatureLevelProjectSettings settings)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(RowHeight)))
            {
                GUILayout.Label(
                    Content(
                        "featureLevel.row.quality",
                        "Quality Binding",
                        "Each Unity Quality Level belongs to exactly one NBShader tier."),
                    GUILayout.Width(FeatureColumnWidth),
                    GUILayout.Height(RowHeight));

                for (var i = 0; i < Tiers.Length; i++)
                {
                    var tier = Tiers[i];
                    if (GUILayout.Button(
                            GetQualitySummaryContent(settings, tier, i),
                            EditorStyles.popup,
                            GUILayout.Width(TierColumnWidth),
                            GUILayout.Height(RowHeight)))
                    {
                        ShowQualityBindingMenu(settings, tier);
                    }
                }

                DrawCostPlaceholderCell();
                DrawInfoCell(
                    Content(
                        "featureLevel.effect.quality",
                        "Bind Unity Quality Levels to NBShader tiers.",
                        "Each Unity Quality Level belongs to exactly one NBShader tier."),
                    EffectColumnWidth);
                DrawInfoCell(
                    GetQualityDescriptionContent(),
                    DescriptionColumnWidth);
            }
        }

        private static bool DrawFeatureRows(NBShaderFeatureLevelProjectSettings settings)
        {
            var changed = false;
            var rows = NBShaderFeatureLevelRowCatalog.Rows;
            var allowedKeywordSets = new HashSet<string>[Tiers.Length];
            var allowedPassFeatureSets = new HashSet<string>[Tiers.Length];
            for (var i = 0; i < Tiers.Length; i++)
            {
                allowedKeywordSets[i] = settings.GetAllowedKeywordSetForReadOnlyUse(Tiers[i]);
                allowedPassFeatureSets[i] = settings.GetAllowedPassFeatureSetForReadOnlyUse(Tiers[i]);
            }

            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = rows[rowIndex];
                if (!IsRowVisible(row))
                    continue;

                using (var rowScope = new EditorGUILayout.HorizontalScope(GUILayout.Height(RowHeight)))
                {
                    DrawFeatureRowBackground(rowScope.rect, row);
                    DrawFeatureNameCell(row);

                    if (row.isKeyword)
                    {
                        for (var tierIndex = 0; tierIndex < Tiers.Length; tierIndex++)
                        {
                            var tier = Tiers[tierIndex];
                            var isAllowed = allowedKeywordSets[tierIndex].Contains(row.keyword);
                            var newAllowed = DrawCellToggle(isAllowed, GUI.skin.toggle);
                            if (newAllowed == isAllowed)
                                continue;

                            Undo.RecordObject(settings, Text("featureLevel.undo.changeKeyword", "Change NBShader Feature Keyword"));
                            settings.SetKeywordAllowed(tier, row.keyword, newAllowed);
                            if (newAllowed)
                                allowedKeywordSets[tierIndex].Add(row.keyword);
                            else
                                allowedKeywordSets[tierIndex].Remove(row.keyword);
                            changed = true;
                        }
                    }
                    else if (row.isPass)
                    {
                        for (var tierIndex = 0; tierIndex < Tiers.Length; tierIndex++)
                        {
                            var tier = Tiers[tierIndex];
                            var isAllowed = allowedPassFeatureSets[tierIndex].Contains(row.passFeatureId);
                            var newAllowed = DrawCellToggle(isAllowed, GUI.skin.toggle);
                            if (newAllowed == isAllowed)
                                continue;

                            Undo.RecordObject(settings, Text("featureLevel.undo.changePassFeature", "Change NBShader Pass Feature"));
                            settings.SetPassFeatureAllowed(tier, row.passFeatureId, newAllowed);
                            if (newAllowed)
                                allowedPassFeatureSets[tierIndex].Add(row.passFeatureId);
                            else
                                allowedPassFeatureSets[tierIndex].Remove(row.passFeatureId);
                            changed = true;
                        }
                    }
                    else
                    {
                        DrawEmptyTierCells();
                    }

                    DrawCostCell(row);
                    DrawInfoCell(GetEffectContent(row), EffectColumnWidth);
                    DrawInfoCell(
                        GetDescriptionContent(row),
                        DescriptionColumnWidth,
                        row.isKeyword ? EditorStyles.miniLabel : EditorStyles.centeredGreyMiniLabel);
                }
            }

            return changed;
        }

        private static void DrawFeatureNameCell(NBShaderFeatureLevelRow row)
        {
            var rect = EditorGUILayout.GetControlRect(
                false,
                RowHeight,
                GUILayout.Width(FeatureColumnWidth),
                GUILayout.Height(RowHeight));

            var hasChildren = NBShaderFeatureLevelRowCatalog.HasChildren(row);
            var indent = row.depth * RowIndentWidth;
            var foldoutRect = new Rect(rect.x + indent, rect.y, 14f, rect.height);
            var labelRect = new Rect(
                rect.x + indent + (hasChildren ? 15f : 18f),
                rect.y,
                Mathf.Max(0f, rect.width - indent - 18f),
                rect.height);

            if (hasChildren)
            {
                var expanded = IsExpanded(row.key);
                var newExpanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, true);
                if (newExpanded != expanded)
                    SetExpanded(row.key, newExpanded);
            }

            var style = hasChildren ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUI.LabelField(labelRect, GetRowContent(row), style);
        }

        private static void DrawFeatureRowBackground(Rect rowRect, NBShaderFeatureLevelRow row)
        {
            if (!IsSectionRow(row) || Event.current.type != EventType.Repaint)
                return;

            rowRect.width = TableWidth;
            EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, EditorGUIUtility.isProSkin ? 0.22f : 0.08f));
        }

        private static bool IsSectionRow(NBShaderFeatureLevelRow row)
        {
            return row != null &&
                   row.kind == NBShaderFeatureLevelRowKind.Group &&
                   string.IsNullOrEmpty(row.parentKey);
        }

        private static void DrawEmptyTierCells()
        {
            for (var i = 0; i < Tiers.Length; i++)
                GUILayout.Space(TierColumnWidth);
        }

        private static void DrawCostCell(NBShaderFeatureLevelRow row)
        {
            if (row != null && (row.isKeyword || row.isPass))
            {
                DrawInfoCell(GetCostContent(row.performanceCost), CostColumnWidth, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            DrawCostPlaceholderCell();
        }

        private static void DrawCostPlaceholderCell()
        {
            DrawInfoCell(GetNoValueContent(), CostColumnWidth, EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawInfoCell(GUIContent content, float width, GUIStyle style = null)
        {
            GUILayout.Label(
                content,
                style ?? EditorStyles.miniLabel,
                GUILayout.Width(width),
                GUILayout.Height(RowHeight));
        }

        private static bool DrawButtons(NBShaderFeatureLevelProjectSettings settings)
        {
            var changed = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ButtonContent(
                        "featureLevel.resetTierKeywords",
                        "Reset Tier Features",
                        "Reset the keyword and pass matrix to the built-in defaults.")))
                {
                    Undo.RecordObject(settings, Text("featureLevel.undo.resetTierKeywords", "Reset NBShader Tier Features"));
                    settings.ResetTierKeywordSetsToDefault();
                    settings.ResetTierPassSetsToDefault();
                    changed = true;
                }

                if (GUILayout.Button(ButtonContent(
                        "featureLevel.resetQualityMapping",
                        "Reset Quality Mapping",
                        "Reset Unity Quality bindings using the default quality-index rule.")))
                {
                    Undo.RecordObject(settings, Text("featureLevel.undo.resetQualityMapping", "Reset NBShader Quality Mapping"));
                    settings.ResetQualityMappingsToDefault();
                    changed = true;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ButtonContent(
                        "featureLevel.openVariantCollectionBuilder",
                        "Open Variant Collection Builder",
                        "Open the NBShader variant collection builder.")))
                {
                    NBShaderSVCBuilderWindow.Open();
                }

                if (GUILayout.Button(ButtonContent(
                        "featureLevel.saveCurrentAsDefault",
                        "Save Current Config as Default",
                        "Write the current tier keyword and pass matrix into the package default LevelAsset.")))
                {
                    if (!NBShaderFeatureLevelPresetLoader.SaveDefaultFeatureSets(settings.tierKeywordSets, settings.tierPassSets))
                    {
                        EditorUtility.DisplayDialog(
                            Text("featureLevel.saveCurrentAsDefault.failedTitle", "Save Default Config Failed"),
                            Text("featureLevel.saveCurrentAsDefault.failedMessage", "Could not write the package default LevelAsset."),
                            Text("featureLevel.dialog.ok", "OK"));
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ButtonContent(
                        "featureLevel.applyCurrentQualityTierToLoadedMaterials",
                        "Apply Current Quality Tier To Loaded Materials",
                        "Apply the tier bound to the current Unity Quality Level to all loaded NBShader2 materials.")))
                {
                    NBShaderEditorQualityTierWatcher.ApplyCurrentQualityTierToLoadedMaterials();
                }

                if (GUILayout.Button(ButtonContent(
                        "featureLevel.applyCurrentQualityTierToProjectMaterials",
                        "Apply Current Quality Tier To Project Materials",
                        "Scan Assets and apply the tier bound to the current Unity Quality Level to NBShader2 material assets.")))
                {
                    NBShaderEditorQualityTierWatcher.ApplyCurrentQualityTierToProjectMaterials();
                }
            }

            return changed;
        }

        private static void ShowQualityBindingMenu(NBShaderFeatureLevelProjectSettings settings, NBShaderFeatureTier targetTier)
        {
            var menu = new GenericMenu();
            var mappings = settings.qualityTierMappings;
            if (mappings == null || mappings.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent(Text("featureLevel.quality.noLevels", "No Quality Levels")));
                menu.ShowAsContext();
                return;
            }

            for (var i = 0; i < mappings.Length; i++)
            {
                var mapping = mappings[i];
                if (mapping == null || string.IsNullOrEmpty(mapping.qualityName))
                    continue;

                var qualityName = mapping.qualityName;
                NBShaderFeatureTier currentTier;
                var isCurrentTier = settings.TryGetTierForQualityName(qualityName, out currentTier) && currentTier == targetTier;
                if (isCurrentTier)
                {
                    menu.AddDisabledItem(new GUIContent("✓ " + qualityName));
                    continue;
                }

                menu.AddItem(new GUIContent(qualityName), false, () =>
                {
                    Undo.RecordObject(settings, Text("featureLevel.undo.changeQualityBinding", "Change NBShader Quality Binding"));
                    settings.MoveQualityToTier(qualityName, targetTier);
                    settings.SaveProjectSettings();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                });
            }

            menu.ShowAsContext();
        }

        private static bool DrawCellToggle(bool value, GUIStyle style)
        {
            bool result;
            using (new GUILayout.HorizontalScope(GUILayout.Width(TierColumnWidth), GUILayout.Height(RowHeight)))
            {
                GUILayout.FlexibleSpace();
                result = GUILayout.Toggle(
                    value,
                    GUIContent.none,
                    style,
                    GUILayout.Width(18f),
                    GUILayout.Height(RowHeight));
                GUILayout.FlexibleSpace();
            }

            return result;
        }

        private static void DrawTableSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f, GUILayout.Width(TableWidth));
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
        }

        private static bool IsRowVisible(NBShaderFeatureLevelRow row)
        {
            var parentKey = row.parentKey;
            while (!string.IsNullOrEmpty(parentKey))
            {
                if (!IsExpanded(parentKey))
                    return false;

                NBShaderFeatureLevelRow parent;
                parentKey = NBShaderFeatureLevelRowCatalog.TryGetRow(parentKey, out parent) ? parent.parentKey : null;
            }

            return true;
        }

        private static bool IsExpanded(string key)
        {
            return SessionState.GetBool(FoldoutSessionPrefix + key, true);
        }

        private static void SetExpanded(string key, bool expanded)
        {
            SessionState.SetBool(FoldoutSessionPrefix + key, expanded);
        }

        private static GUIContent GetTierContent(NBShaderFeatureTier tier)
        {
            switch (tier)
            {
                case NBShaderFeatureTier.Low:
                    return NBShaderInspectorLocalization.MakeContent(
                        "inspector.toolbar.tierLow.label",
                        "Low",
                        "inspector.toolbar.tier.tip",
                        "NBShader feature tier");
                case NBShaderFeatureTier.Medium:
                    return NBShaderInspectorLocalization.MakeContent(
                        "inspector.toolbar.tierMedium.label",
                        "Medium",
                        "inspector.toolbar.tier.tip",
                        "NBShader feature tier");
                case NBShaderFeatureTier.High:
                    return NBShaderInspectorLocalization.MakeContent(
                        "inspector.toolbar.tierHigh.label",
                        "High",
                        "inspector.toolbar.tier.tip",
                        "NBShader feature tier");
                default:
                    return NBShaderInspectorLocalization.MakeContent(
                        "inspector.toolbar.tierUltra.label",
                        "Ultra",
                        "inspector.toolbar.tier.tip",
                        "NBShader feature tier");
            }
        }

        private static GUIContent GetRowContent(NBShaderFeatureLevelRow row)
        {
            return GetCachedRowContent(row).rowContent;
        }

        private static GUIContent BuildRowContent(NBShaderFeatureLevelRow row)
        {
            if (row.isKeyword)
                return BuildKeywordContent(row.keyword, row.labelFallback);

            if (row.isPass)
                return BuildPassContent(row.passFeatureId, row.labelFallback);

            return NBShaderInspectorLocalization.MakeContent(
                "inspector.featureLevel.group." + row.key + ".label",
                row.labelFallback,
                "inspector.featureLevel.group." + row.key + ".tip",
                Text("featureLevel.group.tooltip", "Click the foldout to show or hide child rows."));
        }

        private static GUIContent BuildKeywordContent(string keyword, string fallback)
        {
            var label = NBShaderInspectorLocalization.Get(
                "inspector.featureLevel.keyword." + keyword + ".label",
                string.IsNullOrEmpty(fallback) ? keyword : fallback);
            var tooltipFormat = Text(
                "featureLevel.keyword.tooltip",
                "Raw keyword: {0}. Unchecked in a tier strips variants using this Catalog keyword.");
            return new GUIContent(label, string.Format(tooltipFormat, keyword));
        }

        private static GUIContent BuildPassContent(string passFeatureId, string fallback)
        {
            string passName;
            string displayName;
            if (!NBShaderFeatureLevelCatalog.TryGetManagedPassFeatureInfo(passFeatureId, out passName, out displayName))
            {
                passName = passFeatureId;
                displayName = string.IsNullOrEmpty(fallback) ? passFeatureId : fallback;
            }

            var label = NBShaderInspectorLocalization.Get(
                "inspector.featureLevel.pass." + passFeatureId + ".label",
                string.IsNullOrEmpty(fallback) ? displayName : fallback);
            var tooltipFormat = Text(
                "featureLevel.pass.tooltip",
                "Shader pass: {0}. Unchecked in a tier strips or gates this pass when material intent requires it.");
            return new GUIContent(label, string.Format(tooltipFormat, passName));
        }

        private static GUIContent GetCostContent(NBShaderFeaturePerformanceCost cost)
        {
            GUIContent content;
            if (s_CostContentCache.TryGetValue(cost, out content))
                return content;

            string key;
            string fallback;
            switch (cost)
            {
                case NBShaderFeaturePerformanceCost.Low:
                    key = "featureLevel.cost.low";
                    fallback = "Low";
                    break;
                case NBShaderFeaturePerformanceCost.Medium:
                    key = "featureLevel.cost.medium";
                    fallback = "Medium";
                    break;
                case NBShaderFeaturePerformanceCost.High:
                    key = "featureLevel.cost.high";
                    fallback = "High";
                    break;
                default:
                    key = "featureLevel.cost.ultra";
                    fallback = "Ultra";
                    break;
            }

            content = new GUIContent(
                Text(key, fallback),
                Text("featureLevel.cost.tooltip", "Estimated shader performance cost for this feature."));
            s_CostContentCache[cost] = content;
            return content;
        }

        private static GUIContent GetEffectContent(NBShaderFeatureLevelRow row)
        {
            return GetCachedRowContent(row).effectContent;
        }

        private static GUIContent BuildEffectContent(NBShaderFeatureLevelRow row)
        {
            if (row.isKeyword)
            {
                var effect = NBShaderInspectorLocalization.Get(
                    "inspector.featureLevel.keyword." + row.keyword + ".effect",
                    string.IsNullOrEmpty(row.effectFallback) ? row.keyword : row.effectFallback);
                return new GUIContent(effect, effect);
            }

            if (row.isPass)
            {
                var effect = NBShaderInspectorLocalization.Get(
                    "inspector.featureLevel.pass." + row.passFeatureId + ".effect",
                    string.IsNullOrEmpty(row.effectFallback) ? row.passFeatureId : row.effectFallback);
                return new GUIContent(effect, effect);
            }

            var groupTip = NBShaderInspectorLocalization.GetTooltip(
                "inspector.featureLevel.group." + row.key + ".label",
                "inspector.featureLevel.group." + row.key + ".tip",
                Text("featureLevel.group.tooltip", "Click the foldout to show or hide child rows."));
            return new GUIContent(groupTip, groupTip);
        }

        private static GUIContent GetDescriptionContent(NBShaderFeatureLevelRow row)
        {
            return GetCachedRowContent(row).descriptionContent;
        }

        private static GUIContent BuildDescriptionContent(NBShaderFeatureLevelRow row)
        {
            if (row.isKeyword)
                return new GUIContent(row.keyword, row.keyword);

            if (row.isPass)
            {
                string passName;
                string displayName;
                if (NBShaderFeatureLevelCatalog.TryGetManagedPassFeatureInfo(row.passFeatureId, out passName, out displayName))
                    return new GUIContent(passName, row.passFeatureId);

                return new GUIContent(row.passFeatureId, row.passFeatureId);
            }

            return new GUIContent(
                Text("featureLevel.desc.group", "Group"),
                Text("featureLevel.group.tooltip", "Click the foldout to show or hide child rows."));
        }

        private static GUIContent GetNoValueContent()
        {
            if (s_NoValueContent != null)
                return s_NoValueContent;

            var text = Text("featureLevel.desc.none", "—");
            s_NoValueContent = new GUIContent(text, text);
            return s_NoValueContent;
        }

        private static GUIContent GetProviderLabelContent()
        {
            EnsureContentCacheForCurrentLanguage();

            if (s_ProviderLabelContent == null)
                s_ProviderLabelContent = new GUIContent(Text("featureLevel.providerLabel", "NBShader Feature Levels"));

            return s_ProviderLabelContent;
        }

        private static GUIContent GetQualityDescriptionContent()
        {
            if (s_QualityDescriptionContent == null)
            {
                var text = Text("featureLevel.desc.quality", "Unity Quality Level");
                s_QualityDescriptionContent = new GUIContent(text, text);
            }

            return s_QualityDescriptionContent;
        }

        private static GUIContent GetQualitySummaryContent(
            NBShaderFeatureLevelProjectSettings settings,
            NBShaderFeatureTier tier,
            int tierIndex)
        {
            EnsureQualitySummaryCache(settings);

            var content = s_QualitySummaryContents[tierIndex];
            if (content == null)
            {
                content = new GUIContent();
                s_QualitySummaryContents[tierIndex] = content;
            }

            content.text = s_QualitySummaryTexts[tierIndex];
            content.tooltip = GetQualityMenuTipText();
            return content;
        }

        private static void EnsureQualitySummaryCache(NBShaderFeatureLevelProjectSettings settings)
        {
            var mappings = settings.qualityTierMappings;
            var hash = GetQualitySummaryHash(mappings);
            if (s_QualitySummaryTexts[0] != null && s_QualitySummaryHash == hash)
                return;

            s_QualitySummaryHash = hash;
            var noneText = Text("featureLevel.quality.none", "—");
            for (var tierIndex = 0; tierIndex < Tiers.Length; tierIndex++)
            {
                s_QualitySummaryBuilder.Length = 0;
                if (mappings != null)
                {
                    var tier = Tiers[tierIndex];
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        var mapping = mappings[i];
                        if (mapping == null ||
                            mapping.tier != tier ||
                            string.IsNullOrEmpty(mapping.qualityName))
                        {
                            continue;
                        }

                        if (s_QualitySummaryBuilder.Length > 0)
                            s_QualitySummaryBuilder.Append(", ");
                        s_QualitySummaryBuilder.Append(mapping.qualityName);
                    }
                }

                s_QualitySummaryTexts[tierIndex] =
                    s_QualitySummaryBuilder.Length == 0
                        ? noneText
                        : s_QualitySummaryBuilder.ToString();
            }

            s_QualitySummaryBuilder.Length = 0;
        }

        private static int GetQualitySummaryHash(NBShaderQualityTierMapping[] mappings)
        {
            unchecked
            {
                var hash = 17;
                if (mappings == null)
                    return hash;

                for (var i = 0; i < mappings.Length; i++)
                {
                    var mapping = mappings[i];
                    hash = hash * 31 + (mapping != null ? (int)mapping.tier : -1);
                    hash = hash * 31 + (mapping != null && mapping.qualityName != null
                        ? StringComparer.Ordinal.GetHashCode(mapping.qualityName)
                        : 0);
                }

                return hash;
            }
        }

        private static string GetQualityMenuTipText()
        {
            if (s_QualityMenuTipText == null)
            {
                s_QualityMenuTipText = Text(
                    "featureLevel.quality.menuTip",
                    "Click to move Unity Quality levels to this NBShader tier.");
            }

            return s_QualityMenuTipText;
        }

        private static RowContentCache GetCachedRowContent(NBShaderFeatureLevelRow row)
        {
            RowContentCache cache;
            if (!s_RowContentCache.TryGetValue(row.key, out cache))
            {
                cache = new RowContentCache
                {
                    rowContent = BuildRowContent(row),
                    effectContent = BuildEffectContent(row),
                    descriptionContent = BuildDescriptionContent(row)
                };
                s_RowContentCache.Add(row.key, cache);
            }

            return cache;
        }

        private static void EnsureContentCacheForCurrentLanguage()
        {
            var language = NBShaderInspectorLocalization.CurrentLanguage;
            if (string.Equals(s_ContentCacheLanguage, language, StringComparison.Ordinal))
                return;

            s_ContentCacheLanguage = language;
            s_RowContentCache.Clear();
            s_CostContentCache.Clear();
            s_ProviderLabelContent = null;
            s_QualityDescriptionContent = null;
            s_NoValueContent = null;
            s_QualityMenuTipText = null;
            s_QualitySummaryHash = 0;
            for (var i = 0; i < s_QualitySummaryContents.Length; i++)
            {
                s_QualitySummaryContents[i] = null;
                s_QualitySummaryTexts[i] = null;
            }
        }

        private static GUIContent Content(string key, string fallback, string tip = "")
        {
            return NBShaderInspectorLocalization.MakeInspectorContent(key, fallback, tip);
        }

        private static GUIContent ButtonContent(string key, string fallback, string tip = "")
        {
            return NBShaderInspectorLocalization.MakeContent("inspector." + key + ".button", fallback, null, tip);
        }

        private static string Text(string key, string fallback)
        {
            return NBShaderInspectorLocalization.GetInspectorText(key, fallback);
        }
    }
}
