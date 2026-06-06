using System;
using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [InitializeOnLoad]
    internal static class NBShaderEditorQualityTierWatcher
    {
        private const string UndoApplyLoadedTier = "Apply NBShader Quality Tier";
        private const string UndoApplyProjectTier = "Apply NBShader Quality Tier To Project Materials";

        private static int s_LastQualityLevel;
        private static bool s_DialogQueued;

        static NBShaderEditorQualityTierWatcher()
        {
            s_LastQualityLevel = QualitySettings.GetQualityLevel();
            EditorApplication.update += WatchQualityLevel;
        }

        internal static void ApplyCurrentQualityTierToLoadedMaterials()
        {
            NBShaderFeatureTier tier;
            string qualityName;
            ResolveCurrentQualityTier(out tier, out qualityName);
            var count = ApplyTierToLoadedMaterials(tier);
            Debug.LogFormat(
                "Applied NBShader2 tier {0} from Unity Quality '{1}' to {2} loaded material(s).",
                tier,
                qualityName,
                count);
        }

        internal static void ApplyCurrentQualityTierToProjectMaterials()
        {
            NBShaderFeatureTier tier;
            string qualityName;
            ResolveCurrentQualityTier(out tier, out qualityName);
            var count = ApplyTierToProjectMaterials(tier);
            Debug.LogFormat(
                "Applied NBShader2 tier {0} from Unity Quality '{1}' to {2} project material asset(s).",
                tier,
                qualityName,
                count);
        }

        private static void WatchQualityLevel()
        {
            if (Application.isBatchMode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                s_DialogQueued)
            {
                return;
            }

            var current = QualitySettings.GetQualityLevel();
            if (current == s_LastQualityLevel)
                return;

            s_LastQualityLevel = current;
            s_DialogQueued = true;
            EditorApplication.delayCall += PromptForQualityTierSync;
        }

        private static void PromptForQualityTierSync()
        {
            s_DialogQueued = false;
            if (Application.isBatchMode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            NBShaderFeatureTier tier;
            string qualityName;
            ResolveCurrentQualityTier(out tier, out qualityName);

            var syncLoaded = EditorUtility.DisplayDialog(
                "NBShader2 Feature Tier",
                string.Format(
                    "Unity Quality has switched to '{0}'.\n\nSync currently loaded NBShader2 materials to tier {1}?",
                    qualityName,
                    tier),
                "Sync Loaded",
                "Skip");
            if (!syncLoaded)
                return;

            var loadedCount = ApplyTierToLoadedMaterials(tier);

            var syncProject = EditorUtility.DisplayDialog(
                "NBShader2 Feature Tier",
                string.Format(
                    "Applied tier {0} to {1} loaded NBShader2 material(s).\n\nScan Assets and write current keyword/pass state for all NBShader2 material assets?",
                    tier,
                    loadedCount),
                "Scan Assets",
                "Loaded Only");
            if (syncProject)
            {
                var projectCount = ApplyTierToProjectMaterials(tier);
                Debug.LogFormat(
                    "Applied NBShader2 tier {0} from Unity Quality '{1}' to {2} project material asset(s).",
                    tier,
                    qualityName,
                    projectCount);
            }
        }

        private static void ResolveCurrentQualityTier(out NBShaderFeatureTier tier, out string qualityName)
        {
            qualityName = GetCurrentQualityName();
            if (!NBShaderFeatureLevelProjectSettings.instance.TryGetTierForQualityNameNoSave(qualityName, out tier))
                tier = NBShaderFeatureTier.Ultra;
        }

        private static string GetCurrentQualityName()
        {
            var names = QualitySettings.names;
            var index = QualitySettings.GetQualityLevel();
            if (names == null || index < 0 || index >= names.Length)
                return string.Empty;
            return names[index];
        }

        private static int ApplyTierToLoadedMaterials(NBShaderFeatureTier tier)
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            var editableMaterials = new List<Material>();
            var seen = new HashSet<int>();
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (!CanMutateMaterial(material) || !seen.Add(material.GetInstanceID()))
                    continue;
                editableMaterials.Add(material);
            }

            if (editableMaterials.Count == 0)
                return 0;

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoApplyLoadedTier);
            for (var i = 0; i < editableMaterials.Count; i++)
                ApplyTierToMaterial(editableMaterials[i], tier, UndoApplyLoadedTier);
            Undo.CollapseUndoOperations(undoGroup);
            return editableMaterials.Count;
        }

        private static int ApplyTierToProjectMaterials(NBShaderFeatureTier tier)
        {
            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            var changed = 0;
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoApplyProjectTier);

            try
            {
                for (var i = 0; i < guids.Length; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "NBShader2 Feature Tier",
                            string.Format("Scanning material {0}/{1}", i + 1, guids.Length),
                            guids.Length > 0 ? (float)i / guids.Length : 1f))
                    {
                        break;
                    }

                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!CanMutateMaterial(material))
                        continue;

                    if (ApplyTierToMaterial(material, tier, UndoApplyProjectTier))
                    {
                        AssetDatabase.SaveAssetIfDirty(material);
                        changed++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(undoGroup);
            }

            return changed;
        }

        private static bool CanMutateMaterial(Material material)
        {
            if (!NBShaderMaterialIntentResolver.IsNBShaderMaterial(material))
                return false;

            var flags = material.hideFlags;
            if ((flags & HideFlags.NotEditable) != 0 ||
                (flags & HideFlags.HideAndDontSave) != 0)
            {
                return false;
            }

            if (!EditorUtility.IsPersistent(material))
                return true;

            var path = AssetDatabase.GetAssetPath(material);
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static bool ApplyTierToMaterial(
            Material material,
            NBShaderFeatureTier tier,
            string undoName)
        {
            Undo.RecordObject(material, undoName);
            bool changed;
            if (!NBShaderFeatureLevelMaterialApplier.Apply(material, tier, true, true, out changed))
                return false;

            if (changed)
                EditorUtility.SetDirty(material);

            return changed;
        }
    }
}
