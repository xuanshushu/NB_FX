using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    internal sealed class NBShaderSVCBuilderWindow : EditorWindow
    {
        [SerializeField] private List<string> m_SearchFolders = new List<string>();
        [SerializeField] private string m_OutputFolder = NBShaderVariantCollectionBuilder.DefaultOutputFolder;

        private Vector2 m_Scroll;
        private NBShaderVariantCollectionTierResult[] m_Previews = new NBShaderVariantCollectionTierResult[0];
        private string m_Status = string.Empty;

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
        }

        private void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            DrawSearchFolders();
            EditorGUILayout.Space(8f);
            DrawOutputFolder();
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

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview", GUILayout.Height(28f)))
                Preview();

            using (new EditorGUI.DisabledScope(m_Previews == null || m_Previews.Length == 0))
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
            var result = NBShaderVariantCollectionBuilder.Preview(m_SearchFolders, m_OutputFolder);
            m_Previews = result.tiers;
            m_Status = string.Format(
                "Found {0} NBShader materials in {1} valid folder(s). Preview did not write assets.",
                result.materialCount,
                result.validSearchFolderCount);
        }

        private void Generate()
        {
            var result = NBShaderVariantCollectionBuilder.Generate(m_SearchFolders, m_OutputFolder);
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
    }
}
