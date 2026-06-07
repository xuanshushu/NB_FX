using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    internal sealed class NBShaderSVCBuilderWindow : EditorWindow
    {
        [SerializeField] private List<string> m_SearchFolders = new List<string>();
        [SerializeField] private string m_OutputFolder = NBShaderVariantCollectionBuilder.DefaultOutputFolder;
        [SerializeField] private int m_TierMask = AllTierMask;

        private Vector2 m_Scroll;
        private NBShaderVariantCollectionTierResult[] m_Previews = new NBShaderVariantCollectionTierResult[0];
        private string m_Status = string.Empty;

        private const int AllTierMask =
            (1 << (int)NBShaderFeatureTier.Low) |
            (1 << (int)NBShaderFeatureTier.Medium) |
            (1 << (int)NBShaderFeatureTier.High) |
            (1 << (int)NBShaderFeatureTier.Ultra);

        private static readonly NBShaderFeatureTier[] Tiers =
        {
            NBShaderFeatureTier.Low,
            NBShaderFeatureTier.Medium,
            NBShaderFeatureTier.High,
            NBShaderFeatureTier.Ultra
        };

        internal static void Open()
        {
            var window = GetWindow<NBShaderSVCBuilderWindow>("NBShader Variant Collection Builder");
            window.minSize = new Vector2(620f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            if (m_SearchFolders == null)
                m_SearchFolders = new List<string>();
            if (m_SearchFolders.Count == 0)
                m_SearchFolders.Add("Assets");
            if (string.IsNullOrEmpty(m_OutputFolder))
                m_OutputFolder = NBShaderVariantCollectionBuilder.DefaultOutputFolder;
            if (m_TierMask == 0)
                m_TierMask = AllTierMask;
        }

        private void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            DrawSearchFolders();
            EditorGUILayout.Space(8f);
            DrawOutputFolder();
            EditorGUILayout.Space(8f);
            DrawTierSelection();
            EditorGUILayout.Space(8f);
            DrawActions();
            EditorGUILayout.Space(8f);
            DrawPreview();
            EditorGUILayout.EndScrollView();
        }

        private void DrawSearchFolders()
        {
            EditorGUILayout.LabelField("Material Search Folders", EditorStyles.boldLabel);
            for (var i = 0; i < m_SearchFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                m_SearchFolders[i] = EditorGUILayout.TextField(m_SearchFolders[i]);
                if (GUILayout.Button("...", GUILayout.Width(28f)))
                {
                    var folder = PickProjectFolder(m_SearchFolders[i]);
                    if (!string.IsNullOrEmpty(folder))
                        m_SearchFolders[i] = folder;
                }

                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    m_SearchFolders.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Folder"))
            {
                var folder = PickProjectFolder("Assets");
                if (!string.IsNullOrEmpty(folder))
                    AddSearchFolder(folder);
            }

            if (GUILayout.Button("Add Selected"))
            {
                AddSelectedFolders();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOutputFolder()
        {
            EditorGUILayout.LabelField("Output Folder", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            m_OutputFolder = EditorGUILayout.TextField(m_OutputFolder);
            if (GUILayout.Button("...", GUILayout.Width(28f)))
            {
                var folder = PickProjectFolder(m_OutputFolder);
                if (!string.IsNullOrEmpty(folder))
                    m_OutputFolder = folder;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTierSelection()
        {
            EditorGUILayout.LabelField("Build Tiers", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < Tiers.Length; i++)
                    DrawTierToggle(Tiers[i]);

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("All", GUILayout.Width(56f)))
                    SetTierMask(AllTierMask);
            }

            if (!HasSelectedTier())
                EditorGUILayout.HelpBox("Select at least one tier before previewing or generating SVC assets.", MessageType.Warning);
        }

        private void DrawTierToggle(NBShaderFeatureTier tier)
        {
            var selected = IsTierSelected(tier);
            var newSelected = EditorGUILayout.ToggleLeft(tier.ToString(), selected, GUILayout.Width(92f));
            if (newSelected == selected)
                return;

            var mask = 1 << (int)tier;
            if (newSelected)
                SetTierMask(m_TierMask | mask);
            else
                SetTierMask(m_TierMask & ~mask);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!HasSelectedTier()))
            {
                if (GUILayout.Button("Preview", GUILayout.Height(28f)))
                    Preview();
            }

            using (new EditorGUI.DisabledScope(!HasSelectedTier() || m_Previews == null || m_Previews.Length == 0))
            {
                if (GUILayout.Button("Generate SVC", GUILayout.Height(28f)))
                    Generate();
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(m_Status))
                EditorGUILayout.HelpBox(m_Status, MessageType.Info);
        }

        private void DrawPreview()
        {
            if (m_Previews == null || m_Previews.Length == 0)
                return;

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawPreviewHeader();
                for (var i = 0; i < m_Previews.Length; i++)
                {
                    var preview = m_Previews[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(preview.tier.ToString(), GUILayout.Width(90f));
                    EditorGUILayout.LabelField(preview.materialCount.ToString(), GUILayout.Width(90f));
                    EditorGUILayout.LabelField(preview.passCount.ToString(), GUILayout.Width(70f));
                    EditorGUILayout.LabelField(preview.variantCount.ToString(), GUILayout.Width(90f));
                    EditorGUILayout.LabelField(preview.outputAssetPath ?? string.Empty);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private static void DrawPreviewHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Tier", EditorStyles.boldLabel, GUILayout.Width(90f));
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel, GUILayout.Width(90f));
            EditorGUILayout.LabelField("Passes", EditorStyles.boldLabel, GUILayout.Width(70f));
            EditorGUILayout.LabelField("Variants", EditorStyles.boldLabel, GUILayout.Width(90f));
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void Preview()
        {
            var selectedTiers = GetSelectedTiers();
            var result = NBShaderVariantCollectionBuilder.Preview(m_SearchFolders, m_OutputFolder, selectedTiers);
            m_Previews = result.tiers;
            m_Status = string.Format(
                "Found {0} NBShader materials in {1} valid folder(s). Previewed {2} tier(s) and did not write assets.",
                result.materialCount,
                result.validSearchFolderCount,
                selectedTiers.Length);
        }

        private void Generate()
        {
            var result = NBShaderVariantCollectionBuilder.Generate(m_SearchFolders, m_OutputFolder, GetSelectedTiers());
            if (result.hasError)
            {
                EditorUtility.DisplayDialog("Generate NBShader SVC", result.firstErrorMessage, "OK");
                m_Previews = result.tiers;
                return;
            }

            m_Previews = result.tiers;
            m_Status = string.Format("Generated {0} NBShader shader variant collection asset(s).", result.generatedCount);
        }

        private void AddSelectedFolders()
        {
            var guids = Selection.assetGUIDs;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetDatabase.IsValidFolder(path))
                    AddSearchFolder(path);
            }
        }

        private void AddSearchFolder(string folder)
        {
            folder = NBShaderVariantCollectionBuilder.NormalizeAssetPath(folder);
            if (string.IsNullOrEmpty(folder))
                return;
            if (!m_SearchFolders.Contains(folder))
                m_SearchFolders.Add(folder);
        }

        private static string PickProjectFolder(string current)
        {
            var start = AssetDatabase.IsValidFolder(current) ? current : "Assets";
            var absoluteStart = NBShaderVariantCollectionBuilder.ToAbsolutePath(start);
            var picked = EditorUtility.OpenFolderPanel("Select Project Folder", absoluteStart, string.Empty);
            return NBShaderVariantCollectionBuilder.NormalizeAssetPath(picked);
        }

        private bool HasSelectedTier()
        {
            return (m_TierMask & AllTierMask) != 0;
        }

        private bool IsTierSelected(NBShaderFeatureTier tier)
        {
            return (m_TierMask & (1 << (int)tier)) != 0;
        }

        private NBShaderFeatureTier[] GetSelectedTiers()
        {
            var result = new List<NBShaderFeatureTier>();
            for (var i = 0; i < Tiers.Length; i++)
            {
                var tier = Tiers[i];
                if (IsTierSelected(tier))
                    result.Add(tier);
            }

            return result.ToArray();
        }

        private void SetTierMask(int tierMask)
        {
            tierMask &= AllTierMask;
            if (m_TierMask == tierMask)
                return;

            m_TierMask = tierMask;
            m_Previews = new NBShaderVariantCollectionTierResult[0];
            m_Status = string.Empty;
        }
    }
}
