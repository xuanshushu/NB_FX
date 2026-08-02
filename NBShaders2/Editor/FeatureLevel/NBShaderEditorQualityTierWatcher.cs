using System;
using System.Collections.Generic;
using System.Reflection;
using NBShader;
using NBShaderEditor;
using UnityEditor;
using UnityEngine;

#if UNITY_6000_5_OR_NEWER
using NBShaderObjectId = UnityEngine.EntityId;
#else
using NBShaderObjectId = System.Int32;
#endif

namespace NBShaders2.Editor.FeatureLevel
{
    [InitializeOnLoad]
    internal static class NBShaderEditorQualityTierWatcher
    {
        private const string UndoApplyLoadedTier = "Apply NBShader Quality Tier";
        private const string UndoApplyProjectTier = "Apply NBShader Quality Tier To Project Materials";
        private const float DialogWidth = 460f;
        private const float DialogHeight = 220f;
        private const float DialogMouseOffset = 12f;

        private static readonly MethodInfo s_GetCurrentMousePositionMethod =
            typeof(UnityEditor.Editor).GetMethod(
                "GetCurrentMousePosition",
                BindingFlags.NonPublic | BindingFlags.Static);

        private enum QualityTierDialogResult
        {
            Skip,
            SyncLoaded
        }

        private sealed class QualityTierSyncWindow : EditorWindow
        {
            private string _message;

            internal void Initialize(string title, string message)
            {
                titleContent = new GUIContent(title);
                _message = message;
                minSize = new Vector2(DialogWidth, DialogHeight);
                maxSize = minSize;
                PositionNearMouse();
            }

            private void PositionNearMouse()
            {
                Vector2 mousePosition;
                if (!TryGetCurrentMousePosition(out mousePosition))
                    return;

                var desktopBounds = UnityEditorInternal.InternalEditorUtility.GetBoundsOfDesktopAtPoint(mousePosition);
                var maxX = Mathf.Max(desktopBounds.xMin, desktopBounds.xMax - DialogWidth);
                var maxY = Mathf.Max(desktopBounds.yMin, desktopBounds.yMax - DialogHeight);
                var x = Mathf.Clamp(mousePosition.x + DialogMouseOffset, desktopBounds.xMin, maxX);
                var y = Mathf.Clamp(mousePosition.y + DialogMouseOffset, desktopBounds.yMin, maxY);
                position = new Rect(x, y, DialogWidth, DialogHeight);
            }

            private void OnGUI()
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.HelpBox(_message, MessageType.Warning);

                EditorGUILayout.Space(8f);
                DrawPermanentDisableToggle();

                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(
                            Text("featureLevel.qualityWatcher.dialog.syncLoaded", "Sync Loaded"),
                            GUILayout.Width(120f)))
                    {
                        s_DialogResult = QualityTierDialogResult.SyncLoaded;
                        Close();
                    }

                    if (GUILayout.Button(
                            Text("featureLevel.qualityWatcher.dialog.skip", "Skip"),
                            GUILayout.Width(100f)))
                    {
                        s_DialogResult = QualityTierDialogResult.Skip;
                        Close();
                    }
                }

                EditorGUILayout.Space(10f);
            }

            private void DrawPermanentDisableToggle()
            {
                var settings = NBShaderFeatureLevelProjectSettings.instance;
                var disabled = !settings.enableQualityTierWatcher;
                var requestedDisabled = EditorGUILayout.ToggleLeft(
                    Text(
                        "featureLevel.qualityWatcher.dialog.disablePermanently",
                        "Permanently disable this automatic prompt"),
                    disabled);
                if (requestedDisabled == disabled)
                    return;

                if (requestedDisabled && !ConfirmPermanentDisable())
                    return;

                settings.SetQualityTierWatcherEnabled(!requestedDisabled);
                settings.SaveProjectSettings();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

            private static bool ConfirmPermanentDisable()
            {
                var message =
                    "1. " +
                    Text(
                        "featureLevel.qualityWatcher.disableConfirm.settingsLocation",
                        "You can re-enable this feature at Project Settings > NB_FX > NBShader Feature Levels.") +
                    "\n\n" +
                    "2. " +
                    Text(
                        "featureLevel.qualityWatcher.disableConfirm.materialWarning",
                        "Warning: the current material configuration may no longer match the active Unity Quality Level.") +
                    "\n\n" +
                    Text(
                        "featureLevel.qualityWatcher.disableConfirm.question",
                        "Permanently disable the automatic prompt?");

                return EditorUtility.DisplayDialog(
                    Text(
                        "featureLevel.qualityWatcher.disableConfirm.title",
                        "Disable NBShader2 Quality Watcher"),
                    message,
                    Text(
                        "featureLevel.qualityWatcher.disableConfirm.disable",
                        "Disable Permanently"),
                    Text("featureLevel.qualityWatcher.disableConfirm.cancel", "Cancel"));
            }
        }

        private static int s_LastQualityLevel;
        private static bool s_DialogQueued;
        private static QualityTierDialogResult s_DialogResult;

        private static bool TryGetCurrentMousePosition(out Vector2 mousePosition)
        {
            mousePosition = Vector2.zero;
            if (s_GetCurrentMousePositionMethod == null)
                return false;

            try
            {
                var value = s_GetCurrentMousePositionMethod.Invoke(null, null);
                if (value is Vector2)
                {
                    mousePosition = (Vector2)value;
                    return true;
                }
            }
            catch (TargetInvocationException)
            {
            }
            catch (MethodAccessException)
            {
            }

            return false;
        }

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
            var current = QualitySettings.GetQualityLevel();
            if (!NBShaderFeatureLevelProjectSettings.instance.enableQualityTierWatcher)
            {
                s_LastQualityLevel = current;
                if (s_DialogQueued)
                {
                    EditorApplication.delayCall -= PromptForQualityTierSync;
                    s_DialogQueued = false;
                }
                return;
            }

            if (Application.isBatchMode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                s_DialogQueued)
            {
                return;
            }

            if (current == s_LastQualityLevel)
                return;

            s_LastQualityLevel = current;
            s_DialogQueued = true;
            EditorApplication.delayCall += PromptForQualityTierSync;
        }

        private static void PromptForQualityTierSync()
        {
            s_DialogQueued = false;
            if (!NBShaderFeatureLevelProjectSettings.instance.enableQualityTierWatcher ||
                Application.isBatchMode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            NBShaderFeatureTier tier;
            string qualityName;
            ResolveCurrentQualityTier(out tier, out qualityName);

            var localizedTier = GetLocalizedTierName(tier);
            var title = Text(
                "featureLevel.qualityWatcher.dialog.title",
                "NBShader2 Feature Tier");
            var message =
                string.Format(
                    Text(
                        "featureLevel.qualityWatcher.dialog.qualityChanged",
                        "Unity Quality has switched to '{0}'."),
                    qualityName) +
                "\n\n" +
                string.Format(
                    Text(
                        "featureLevel.qualityWatcher.dialog.syncQuestion",
                        "Sync currently loaded NBShader2 materials to tier {0}?"),
                    localizedTier);

            s_DialogResult = QualityTierDialogResult.Skip;
            var window = ScriptableObject.CreateInstance<QualityTierSyncWindow>();
            window.Initialize(title, message);
            window.ShowModalUtility();
            if (s_DialogResult != QualityTierDialogResult.SyncLoaded)
                return;

            var loadedCount = ApplyTierToLoadedMaterials(tier);

            var syncProject = EditorUtility.DisplayDialog(
                title,
                string.Format(
                    Text(
                        "featureLevel.qualityWatcher.dialog.loadedApplied",
                        "Applied tier {0} to {1} loaded NBShader2 material(s)."),
                    localizedTier,
                    loadedCount) +
                "\n\n" +
                Text(
                    "featureLevel.qualityWatcher.dialog.scanQuestion",
                    "Scan Assets and write current keyword/pass state for all NBShader2 material assets?"),
                Text("featureLevel.qualityWatcher.dialog.scanAssets", "Scan Assets"),
                Text("featureLevel.qualityWatcher.dialog.loadedOnly", "Loaded Only"));
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
            var seen = new HashSet<NBShaderObjectId>();
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (!CanMutateMaterial(material) || !seen.Add(GetObjectId(material)))
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

        private static NBShaderObjectId GetObjectId(Material material)
        {
#if UNITY_6000_5_OR_NEWER
            return material.GetEntityId();
#else
            return material.GetInstanceID();
#endif
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
                            Text(
                                "featureLevel.qualityWatcher.dialog.title",
                                "NBShader2 Feature Tier"),
                            string.Format(
                                Text(
                                    "featureLevel.qualityWatcher.dialog.scanProgress",
                                    "Scanning material {0}/{1}"),
                                i + 1,
                                guids.Length),
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

        private static string GetLocalizedTierName(NBShaderFeatureTier tier)
        {
            switch (tier)
            {
                case NBShaderFeatureTier.Low:
                    return NBShaderInspectorLocalization.Get("inspector.toolbar.tierLow.label", "Low");
                case NBShaderFeatureTier.Medium:
                    return NBShaderInspectorLocalization.Get("inspector.toolbar.tierMedium.label", "Medium");
                case NBShaderFeatureTier.High:
                    return NBShaderInspectorLocalization.Get("inspector.toolbar.tierHigh.label", "High");
                default:
                    return NBShaderInspectorLocalization.Get("inspector.toolbar.tierUltra.label", "Ultra");
            }
        }

        private static string Text(string key, string fallback)
        {
            return NBShaderInspectorLocalization.GetInspectorText(key, fallback);
        }
    }
}
