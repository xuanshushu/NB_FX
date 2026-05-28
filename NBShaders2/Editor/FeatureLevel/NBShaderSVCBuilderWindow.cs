using System;
using System.Collections.Generic;
using System.IO;
using NBShader;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaders2.Editor.FeatureLevel
{
    internal sealed class NBShaderSVCBuilderWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/NBShaders2/ShaderVariantCollections";

        private static readonly NBShaderFeatureTier[] Tiers =
        {
            NBShaderFeatureTier.Low,
            NBShaderFeatureTier.Medium,
            NBShaderFeatureTier.High,
            NBShaderFeatureTier.Ultra
        };

        [SerializeField] private List<string> m_SearchFolders = new List<string>();
        [SerializeField] private string m_OutputFolder = DefaultOutputFolder;

        private Vector2 m_Scroll;
        private PreviewSummary[] m_Previews = new PreviewSummary[0];
        private string m_Status = string.Empty;

        [MenuItem("Tools/NBShader2/Feature Levels/SVC Builder")]
        private static void Open()
        {
            var window = GetWindow<NBShaderSVCBuilderWindow>("NBShader2 SVC");
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
                m_OutputFolder = DefaultOutputFolder;
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
            var materials = CollectMaterials();
            m_Previews = BuildPreviews(materials, NormalizeAssetPath(m_OutputFolder));
            m_Status = string.Format(
                "Found {0} NBShader2 materials in {1} valid folder(s). Preview did not write assets.",
                materials.Length,
                CountValidSearchFolders());
        }

        private void Generate()
        {
            var materials = CollectMaterials();
            if (materials.Length == 0)
            {
                EditorUtility.DisplayDialog("Generate NBShader2 SVC", "No NBShader2 materials were found in the selected folders.", "OK");
                return;
            }

            var outputFolder = NormalizeAssetPath(m_OutputFolder);
            if (!EnsureOutputFolder(outputFolder))
                return;

            m_Previews = BuildPreviews(materials, outputFolder);
            var generated = 0;
            for (var i = 0; i < Tiers.Length; i++)
            {
                var tier = Tiers[i];
                var buildInfo = NBShaderFeatureLevelEditorAPI.GetBuildInfo(
                    tier,
                    materials,
                    NBShaderBuildInfoMode.ExactMaterialVariants);
                var variants = BuildSvcVariants(buildInfo);
                var path = GetOutputPath(outputFolder, tier);
                if (WriteCollection(path, variants))
                    generated++;
            }

            m_Status = string.Format("Generated {0} NBShader2 shader variant collection asset(s).", generated);
        }

        private Material[] CollectMaterials()
        {
            var folders = GetValidSearchFolders();
            if (folders.Length == 0)
                return new Material[0];

            var guids = AssetDatabase.FindAssets("t:Material", folders);
            var result = new List<Material>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || !seen.Add(path))
                    continue;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(material))
                    result.Add(material);
            }

            return result.ToArray();
        }

        private PreviewSummary[] BuildPreviews(Material[] materials, string outputFolder)
        {
            var result = new PreviewSummary[Tiers.Length];
            for (var i = 0; i < Tiers.Length; i++)
            {
                var tier = Tiers[i];
                var buildInfo = NBShaderFeatureLevelEditorAPI.GetBuildInfo(
                    tier,
                    materials,
                    NBShaderBuildInfoMode.ExactMaterialVariants);
                var variants = BuildSvcVariants(buildInfo);
                result[i] = new PreviewSummary(
                    tier,
                    buildInfo != null ? buildInfo.materials.Length : 0,
                    buildInfo != null ? buildInfo.includedPassNames.Length : 0,
                    variants.Length,
                    GetOutputPath(outputFolder, tier));
            }

            return result;
        }

        private static SvcVariant[] BuildSvcVariants(NBShaderBuildInfoSet buildInfo)
        {
            if (buildInfo == null)
                return new SvcVariant[0];

            var result = new List<SvcVariant>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var variants = buildInfo.variants;
            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                if (variant == null || variant.shader == null)
                    continue;

                var keywords = variant.keywords;
                var key = variant.shader.name + "|" + variant.passType + "|" + string.Join(";", keywords);
                if (!seen.Add(key))
                    continue;

                result.Add(new SvcVariant(variant.shader, variant.passType, keywords));
            }

            return result.ToArray();
        }

        private static bool WriteCollection(string path, SvcVariant[] variants)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            var collection = existing as ShaderVariantCollection;
            if (existing != null && collection == null)
            {
                EditorUtility.DisplayDialog(
                    "Generate NBShader2 SVC",
                    "Output path already exists and is not a ShaderVariantCollection:\n" + path,
                    "OK");
                return false;
            }

            if (collection == null)
            {
                collection = new ShaderVariantCollection();
                AssetDatabase.CreateAsset(collection, path);
            }
            else
            {
                collection.Clear();
            }

            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                try
                {
                    collection.Add(new ShaderVariantCollection.ShaderVariant(
                        variant.shader,
                        variant.passType,
                        variant.keywords));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to add NBShader2 SVC variant: " + ex.Message);
                }
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssetIfDirty(collection);
            return true;
        }

        private string[] GetValidSearchFolders()
        {
            if (m_SearchFolders == null)
                return new string[0];

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_SearchFolders.Count; i++)
            {
                var path = NormalizeAssetPath(m_SearchFolders[i]);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path) || !seen.Add(path))
                    continue;
                result.Add(path);
            }

            return result.ToArray();
        }

        private int CountValidSearchFolders()
        {
            return GetValidSearchFolders().Length;
        }

        private static bool EnsureOutputFolder(string outputFolder)
        {
            if (string.IsNullOrEmpty(outputFolder))
                return false;

            if (AssetDatabase.IsValidFolder(outputFolder))
                return true;

            if (!outputFolder.StartsWith("Assets/", StringComparison.Ordinal) &&
                !string.Equals(outputFolder, "Assets", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog(
                    "Generate NBShader2 SVC",
                    "Output folder must already exist unless it is under Assets:\n" + outputFolder,
                    "OK");
                return false;
            }

            var parts = outputFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }

            return AssetDatabase.IsValidFolder(outputFolder);
        }

        private static string GetOutputPath(string outputFolder, NBShaderFeatureTier tier)
        {
            outputFolder = NormalizeAssetPath(outputFolder);
            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = DefaultOutputFolder;
            return outputFolder.TrimEnd('/') + "/NBShader2_" + tier + ".shadervariants";
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
            folder = NormalizeAssetPath(folder);
            if (string.IsNullOrEmpty(folder))
                return;
            if (!m_SearchFolders.Contains(folder))
                m_SearchFolders.Add(folder);
        }

        private static string PickProjectFolder(string current)
        {
            var start = AssetDatabase.IsValidFolder(current) ? current : "Assets";
            var absoluteStart = ToAbsolutePath(start);
            var picked = EditorUtility.OpenFolderPanel("Select Project Folder", absoluteStart, string.Empty);
            return NormalizeAssetPath(picked);
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            path = path.Replace('\\', '/').Trim();
            if (path.StartsWith("Assets", StringComparison.Ordinal) ||
                path.StartsWith("Packages", StringComparison.Ordinal))
            {
                return path.TrimEnd('/');
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/').TrimEnd('/');
            var fullPath = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
            if (fullPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(projectRoot.Length + 1);

            return string.Empty;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            assetPath = NormalizeAssetPath(assetPath);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(assetPath))
                return projectRoot;
            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        private sealed class PreviewSummary
        {
            public readonly NBShaderFeatureTier tier;
            public readonly int materialCount;
            public readonly int passCount;
            public readonly int variantCount;
            public readonly string outputAssetPath;

            public PreviewSummary(
                NBShaderFeatureTier tier,
                int materialCount,
                int passCount,
                int variantCount,
                string outputAssetPath)
            {
                this.tier = tier;
                this.materialCount = materialCount;
                this.passCount = passCount;
                this.variantCount = variantCount;
                this.outputAssetPath = outputAssetPath;
            }
        }

        private sealed class SvcVariant
        {
            public readonly Shader shader;
            public readonly PassType passType;
            public readonly string[] keywords;

            public SvcVariant(Shader shader, PassType passType, string[] keywords)
            {
                this.shader = shader;
                this.passType = passType;
                this.keywords = keywords != null ? (string[])keywords.Clone() : new string[0];
            }
        }
    }
}
