using System.Collections.Generic;
using NBShader;
using NBShaderEditor;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    public static class NBShaderFeatureLevelSettingsProvider
    {
        private const string SettingsPath = "Project/NB_FX/NBShader Feature Levels";
        private const float FeatureColumnWidth = 240f;
        private const float TierColumnWidth = 92f;
        private const float DescriptionColumnWidth = 230f;
        private const float RowHeight = 22f;
        private const float HeaderHeight = 24f;
        private const float TableMinHeight = 360f;
        private const float TableMaxHeight = 640f;
        private const float TableWidth = FeatureColumnWidth + TierColumnWidth * 4f + DescriptionColumnWidth;
        private const float RowIndentWidth = 14f;
        private const string FoldoutSessionPrefix = "NBShaderFeatureLevelSettingsProvider.Foldout.";

        private static readonly NBShaderFeatureTier[] Tiers =
        {
            NBShaderFeatureTier.Low,
            NBShaderFeatureTier.Medium,
            NBShaderFeatureTier.High,
            NBShaderFeatureTier.Ultra
        };

        private static readonly string[] BuildStripPolicyFallbackOptions =
        {
            "Disabled",
            "Explicit Tier",
            "Quality Mapped Union"
        };

        private static Vector2 s_TableScrollPosition;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = Text("featureLevel.providerLabel", "NBShader Feature Levels"),
                keywords = new[]
                {
                    "NB_FX",
                    "NBShader",
                    "Feature",
                    "Tier",
                    "Strip",
                    "Keyword",
                    "Quality",
                    "等级",
                    "分级",
                    "剔除"
                },
                guiHandler = OnGUI
            };
        }

        private static void OnGUI(string searchContext)
        {
            var settings = NBShaderFeatureLevelProjectSettings.instance;
            settings.EnsureInitialized();

            var changed = false;

            EditorGUILayout.HelpBox(
                Text(
                    "featureLevel.help.message",
                    "Configure NBShader managed Catalog keywords per tier, bind Unity Quality levels, and choose build-time shader variant stripping. Catalog-external keywords are ignored."),
                MessageType.Info);

            changed |= DrawBuildStripPolicy(settings);

            EditorGUILayout.Space();
            changed |= DrawFeatureLevelTable(settings);

            EditorGUILayout.Space();
            changed |= DrawButtons(settings);

            if (changed)
                settings.SaveProjectSettings();
        }

        private static bool DrawBuildStripPolicy(NBShaderFeatureLevelProjectSettings settings)
        {
            var options = NBShaderInspectorLocalization.GetInspectorOptions(
                "featureLevel.buildStripPolicy",
                BuildStripPolicyFallbackOptions);
            var current = Mathf.Clamp((int)settings.buildStripPolicy, 0, options.Length - 1);

            EditorGUI.BeginChangeCheck();
            var selected = EditorGUILayout.Popup(
                Content(
                    "featureLevel.buildStripPolicy",
                    "Build Strip Policy",
                    "Controls how NBShader Catalog keyword variants are stripped during build."),
                current,
                options);

            if (!EditorGUI.EndChangeCheck())
                return false;

            Undo.RecordObject(settings, Text("featureLevel.undo.changeBuildStripPolicy", "Change NBShader Build Strip Policy"));
            settings.buildStripPolicy = (NBShaderBuildStripPolicy)selected;
            return true;
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
                changed |= DrawBuildTargetRow(settings);
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
                    var summary = GetQualitySummary(settings, tier);
                    if (GUILayout.Button(
                            new GUIContent(
                                summary,
                                Text("featureLevel.quality.menuTip", "Click to move Unity Quality levels to this NBShader tier.")),
                            EditorStyles.popup,
                            GUILayout.Width(TierColumnWidth),
                            GUILayout.Height(RowHeight)))
                    {
                        ShowQualityBindingMenu(settings, tier);
                    }
                }

                GUILayout.Label(
                    Text("featureLevel.desc.quality", "Unity Quality Level"),
                    EditorStyles.miniLabel,
                    GUILayout.Width(DescriptionColumnWidth),
                    GUILayout.Height(RowHeight));
            }
        }

        private static bool DrawBuildTargetRow(NBShaderFeatureLevelProjectSettings settings)
        {
            var changed = false;
            var isExplicitPolicy = settings.buildStripPolicy == NBShaderBuildStripPolicy.ExplicitTier;

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(RowHeight)))
            {
                GUILayout.Label(
                    Content(
                        "featureLevel.row.buildTarget",
                        "Build Target Tier",
                        "Explicit tier used by Build Strip Policy when set to Explicit Tier."),
                    GUILayout.Width(FeatureColumnWidth),
                    GUILayout.Height(RowHeight));

                using (new EditorGUI.DisabledScope(!isExplicitPolicy))
                {
                    for (var i = 0; i < Tiers.Length; i++)
                    {
                        var tier = Tiers[i];
                        var isSelected = settings.explicitTier == tier;
                        var newSelected = DrawCellToggle(isSelected, EditorStyles.radioButton);
                        if (!isExplicitPolicy || !newSelected || isSelected)
                            continue;

                        Undo.RecordObject(settings, Text("featureLevel.undo.changeExplicitTier", "Change NBShader Explicit Tier"));
                        settings.explicitTier = tier;
                        changed = true;
                    }
                }

                GUILayout.Label(
                    isExplicitPolicy
                        ? Text("featureLevel.desc.buildTarget.active", "Used by Explicit Tier stripping.")
                        : Text("featureLevel.desc.buildTarget.inactive", "Only active when policy is Explicit Tier."),
                    EditorStyles.miniLabel,
                    GUILayout.Width(DescriptionColumnWidth),
                    GUILayout.Height(RowHeight));
            }

            return changed;
        }

        private static bool DrawFeatureRows(NBShaderFeatureLevelProjectSettings settings)
        {
            var changed = false;
            var rows = NBShaderFeatureLevelRowCatalog.Rows;
            var allowedSets = new HashSet<string>[Tiers.Length];
            for (var i = 0; i < Tiers.Length; i++)
                allowedSets[i] = settings.GetAllowedKeywordSet(Tiers[i]);

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
                            var isAllowed = allowedSets[tierIndex].Contains(row.keyword);
                            var newAllowed = DrawCellToggle(isAllowed, GUI.skin.toggle);
                            if (newAllowed == isAllowed)
                                continue;

                            Undo.RecordObject(settings, Text("featureLevel.undo.changeKeyword", "Change NBShader Feature Keyword"));
                            settings.SetKeywordAllowed(tier, row.keyword, newAllowed);
                            if (newAllowed)
                                allowedSets[tierIndex].Add(row.keyword);
                            else
                                allowedSets[tierIndex].Remove(row.keyword);
                            changed = true;
                        }
                    }
                    else
                    {
                        DrawEmptyTierCells();
                    }

                    GUILayout.Label(
                        GetDescriptionContent(row),
                        row.isKeyword ? EditorStyles.miniLabel : EditorStyles.centeredGreyMiniLabel,
                        GUILayout.Width(DescriptionColumnWidth),
                        GUILayout.Height(RowHeight));
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
            if (row == null || row.isKeyword || Event.current.type != EventType.Repaint)
                return;

            rowRect.width = TableWidth;
            EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, EditorGUIUtility.isProSkin ? 0.22f : 0.08f));
        }

        private static void DrawEmptyTierCells()
        {
            for (var i = 0; i < Tiers.Length; i++)
                GUILayout.Space(TierColumnWidth);
        }

        private static bool DrawButtons(NBShaderFeatureLevelProjectSettings settings)
        {
            var changed = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ButtonContent(
                        "featureLevel.resetTierKeywords",
                        "Reset Tier Keywords",
                        "Reset the keyword matrix to the built-in defaults.")))
                {
                    Undo.RecordObject(settings, Text("featureLevel.undo.resetTierKeywords", "Reset NBShader Tier Keywords"));
                    settings.ResetTierKeywordSetsToDefault();
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

                if (GUILayout.Button(ButtonContent(
                        "featureLevel.syncRuntimeAsset",
                        "Sync Runtime Asset",
                        "Write the current ProjectSettings data into the runtime Resources settings asset.")))
                {
                    NBShaderRuntimeSettingsSynchronizer.SyncFromProjectSettings();
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

        private static string GetQualitySummary(NBShaderFeatureLevelProjectSettings settings, NBShaderFeatureTier tier)
        {
            var names = settings.GetQualityNamesForTier(tier);
            if (names == null || names.Length == 0)
                return Text("featureLevel.quality.none", "—");

            return string.Join(", ", names);
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
            if (row.isKeyword)
                return GetKeywordContent(row.keyword, row.labelFallback);

            return NBShaderInspectorLocalization.MakeContent(
                "inspector.featureLevel.group." + row.key + ".label",
                row.labelFallback,
                "inspector.featureLevel.group." + row.key + ".tip",
                Text("featureLevel.group.tooltip", "Click the foldout to show or hide child rows."));
        }

        private static GUIContent GetKeywordContent(string keyword, string fallback)
        {
            var label = NBShaderInspectorLocalization.Get(
                "inspector.featureLevel.keyword." + keyword + ".label",
                string.IsNullOrEmpty(fallback) ? keyword : fallback);
            var tooltipFormat = Text(
                "featureLevel.keyword.tooltip",
                "Raw keyword: {0}. Unchecked in a tier strips variants using this Catalog keyword.");
            return new GUIContent(label, string.Format(tooltipFormat, keyword));
        }

        private static GUIContent GetDescriptionContent(NBShaderFeatureLevelRow row)
        {
            if (row.isKeyword)
                return new GUIContent(row.keyword, row.keyword);

            return new GUIContent(
                Text("featureLevel.desc.group", "Group"),
                Text("featureLevel.group.tooltip", "Click the foldout to show or hide child rows."));
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
