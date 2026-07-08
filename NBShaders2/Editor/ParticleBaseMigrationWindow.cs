using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NBShaderEditor;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor
{
    [InitializeOnLoad]
    internal static class ParticleBaseMigrationSettingsProvider
    {
        static ParticleBaseMigrationSettingsProvider()
        {
            NBFXProjectSettings.RegisterSettingsSection(
                "ParticleBaseMigration",
                () => new GUIContent(Text("particleBaseMigration.providerLabel", "ParticleBase Migration")),
                OnGUI,
                new[]
                {
                    "ParticleBase",
                    "NBShader2",
                    "Migration",
                    "Material",
                    "Shader GUID"
                },
                95);
        }

        private static void OnGUI(string searchContext)
        {
            EditorGUILayout.HelpBox(
                Text(
                    "particleBaseMigration.settingsHelp",
                    "Scan material assets that reference the legacy ParticleBase shader GUID and migrate them to NBShader2 after confirmation. Back up assets or commit to version control before converting."),
                MessageType.Warning);

            if (GUILayout.Button(ButtonContent("particleBaseMigration.openWindow", "Open ParticleBase Migration Window")))
            {
                ParticleBaseMigrationWindow.Open();
            }
        }

        private static GUIContent ButtonContent(string key, string fallback)
        {
            return NBShaderInspectorLocalization.MakeContent("inspector." + key + ".button", fallback);
        }

        private static string Text(string key, string fallback)
        {
            return NBShaderInspectorLocalization.GetInspectorText(key, fallback);
        }
    }

    internal sealed class ParticleBaseMigrationWindow : EditorWindow
    {
        private const string LegacyShaderGuidFallback = "7184a95c20fc1a441a8815af4c795ccd";
        private const string NBShader2Guid = "7787bfdacec31472ca6644d6e3616bd4";
        private const string LegacyShaderAssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders/Shader/ParticleBase.shader";
        private const string NBShader2AssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        private const string LegacyShaderName = "Effects/NBShader(Legacy)";
        private const string UndoNameFallback = "Migrate ParticleBase Materials To NBShader2";

        private static readonly string[] LegacyFoldoutProperties =
        {
            "_W9ParticleShaderGUIFoldToggle",
            "_W9ParticleShaderGUIFoldToggle1",
            "_W9ParticleShaderGUIFoldToggle2"
        };

        private static readonly string[] NBShader2FoldoutProperties =
        {
            "_NBShaderGUIFoldToggle",
            "_NBShaderGUIFoldToggle1",
            "_NBShaderGUIFoldToggle2"
        };

        private readonly List<MaterialMigrationEntry> _entries = new List<MaterialMigrationEntry>();
        private Vector2 _scrollPosition;
        private bool _scanned;
        private string _statusMessage;

        public static void Open()
        {
            var window = GetWindow<ParticleBaseMigrationWindow>(Text("particleBaseMigration.windowTitle", "ParticleBase Migration"));
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            titleContent = new GUIContent(Text("particleBaseMigration.windowTitle", "ParticleBase Migration"));
            EditorGUILayout.LabelField(Text("particleBaseMigration.title", "ParticleBase -> NBShader2"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                Text(
                    "particleBaseMigration.windowWarning",
                    "This operation changes material shader assignments and synchronized material state. Back up assets or commit to version control before converting. The tool does not provide an automatic rollback."),
                MessageType.Warning);

            DrawToolbar();
            DrawStatus();
            DrawMaterialList();
            DrawConvertButton();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ButtonContent("particleBaseMigration.scan", "Scan ParticleBase Materials"), GUILayout.Height(24f)))
                {
                    ScanMaterials();
                }

                EditorGUI.BeginDisabledGroup(_entries.Count == 0);
                if (GUILayout.Button(ButtonContent("particleBaseMigration.selectAll", "Select All"), GUILayout.Width(96f), GUILayout.Height(24f)))
                {
                    SetAllSelected(true);
                }

                if (GUILayout.Button(ButtonContent("particleBaseMigration.selectNone", "Select None"), GUILayout.Width(96f), GUILayout.Height(24f)))
                {
                    SetAllSelected(false);
                }

                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
            else if (!_scanned)
            {
                EditorGUILayout.HelpBox(
                    Text(
                        "particleBaseMigration.scanPrompt",
                        "Click Scan to find .mat assets that reference the ParticleBase shader asset GUID. Shader name is only used as a fallback."),
                    MessageType.Info);
            }
            else if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    Text("particleBaseMigration.noMaterials", "No ParticleBase material assets were found."),
                    MessageType.Info);
            }
        }

        private void DrawMaterialList()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                FormatText("particleBaseMigration.listSummary", "Found {0} material asset(s). Selected: {1}", _entries.Count, CountSelected()),
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("", GUILayout.Width(24f));
                GUILayout.Label(Text("particleBaseMigration.column.material", "Material"), GUILayout.Width(240f));
                GUILayout.Label(Text("particleBaseMigration.column.match", "Match"), GUILayout.Width(160f));
                GUILayout.Label(Text("particleBaseMigration.column.path", "Path"), GUILayout.MinWidth(260f));
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _entries.Count; i++)
            {
                DrawMaterialRow(_entries[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawMaterialRow(MaterialMigrationEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(24f));
                EditorGUILayout.ObjectField(entry.material, typeof(Material), false, GUILayout.Width(240f));
                GUILayout.Label(entry.matchSource, EditorStyles.miniLabel, GUILayout.Width(160f));

                if (GUILayout.Button(ButtonContent("particleBaseMigration.ping", "Ping"), GUILayout.Width(48f)))
                {
                    PingAsset(entry.assetPath);
                }

                EditorGUILayout.SelectableLabel(
                    entry.assetPath,
                    EditorStyles.miniLabel,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight),
                    GUILayout.MinWidth(220f));
            }
        }

        private void DrawConvertButton()
        {
            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(CountSelected() == 0);
            if (GUILayout.Button(ButtonContent("particleBaseMigration.convertSelected", "Convert Selected Materials To NBShader2"), GUILayout.Height(30f)))
            {
                ConvertSelectedMaterials();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void ScanMaterials()
        {
            _entries.Clear();
            _scanned = true;
            _statusMessage = null;

            string legacyShaderGuid = ResolveShaderGuid(LegacyShaderAssetPath, LegacyShaderGuidFallback);
            Shader legacyShader = LoadShaderByGuidOrPath(legacyShaderGuid, LegacyShaderAssetPath);
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            var seenPaths = new HashSet<string>(StringComparer.Ordinal);
            bool canceled = false;

            try
            {
                for (int i = 0; i < materialGuids.Length; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            Text("particleBaseMigration.windowTitle", "ParticleBase Migration"),
                            FormatText("particleBaseMigration.progress.scanning", "Scanning materials {0}/{1}", i + 1, materialGuids.Length),
                            materialGuids.Length > 0 ? (float)i / materialGuids.Length : 1f))
                    {
                        canceled = true;
                        break;
                    }

                    string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                    if (string.IsNullOrEmpty(path) ||
                        !path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) ||
                        !seenPaths.Add(path))
                    {
                        continue;
                    }

                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    string matchSource;
                    if (!IsLegacyParticleBaseMaterial(path, material, legacyShaderGuid, legacyShader, out matchSource))
                    {
                        continue;
                    }

                    _entries.Add(new MaterialMigrationEntry
                    {
                        assetPath = path,
                        material = material,
                        selected = true,
                        matchSource = matchSource
                    });
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _entries.Sort((a, b) => string.CompareOrdinal(a.assetPath, b.assetPath));
            _statusMessage = canceled
                ? FormatText("particleBaseMigration.scanCanceled", "Scan canceled. Found {0} material asset(s) before canceling.", _entries.Count)
                : FormatText("particleBaseMigration.scanComplete", "Scan complete. Found {0} ParticleBase material asset(s).", _entries.Count);
        }

        private void ConvertSelectedMaterials()
        {
            List<MaterialMigrationEntry> selectedEntries = GetSelectedEntries();
            if (selectedEntries.Count == 0)
            {
                return;
            }

            Shader targetShader = LoadShaderByGuidOrPath(NBShader2Guid, NBShader2AssetPath);
            if (targetShader == null)
            {
                EditorUtility.DisplayDialog(
                    Text("particleBaseMigration.nbShader2Missing.title", "NBShader2 Missing"),
                    Text("particleBaseMigration.nbShader2Missing.message", "Could not load NBShader2 shader asset. Migration cannot continue."),
                    Text("particleBaseMigration.dialog.ok", "OK"));
                return;
            }

            if (!ConfirmConversion(selectedEntries.Count))
            {
                return;
            }

            string legacyShaderGuid = ResolveShaderGuid(LegacyShaderAssetPath, LegacyShaderGuidFallback);
            Shader legacyShader = LoadShaderByGuidOrPath(legacyShaderGuid, LegacyShaderAssetPath);
            int convertedCount = 0;
            bool canceled = false;
            var failures = new List<string>();
            int undoGroup = Undo.GetCurrentGroup();
            bool assetEditingStarted = false;
            string undoName = Text("particleBaseMigration.undo", UndoNameFallback);
            Undo.SetCurrentGroupName(undoName);

            try
            {
                AssetDatabase.StartAssetEditing();
                assetEditingStarted = true;
                for (int i = 0; i < selectedEntries.Count; i++)
                {
                    MaterialMigrationEntry entry = selectedEntries[i];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            Text("particleBaseMigration.windowTitle", "ParticleBase Migration"),
                            FormatText("particleBaseMigration.progress.converting", "Converting {0}/{1}: {2}", i + 1, selectedEntries.Count, entry.assetPath),
                            selectedEntries.Count > 0 ? (float)i / selectedEntries.Count : 1f))
                    {
                        canceled = true;
                        break;
                    }

                    if (ConvertMaterialEntry(entry, targetShader, legacyShaderGuid, legacyShader, undoName, failures))
                    {
                        convertedCount++;
                    }
                }
            }
            finally
            {
                if (assetEditingStarted)
                {
                    AssetDatabase.StopAssetEditing();
                }

                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(undoGroup);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (failures.Count > 0)
            {
                Debug.LogWarning(
                    Text("particleBaseMigration.failuresLogged", "ParticleBase migration completed with failures:") +
                    "\n" +
                    string.Join("\n", failures.ToArray()));
            }

            _statusMessage = BuildConversionStatus(convertedCount, selectedEntries.Count, canceled, failures.Count);
        }

        private static bool ConvertMaterialEntry(
            MaterialMigrationEntry entry,
            Shader targetShader,
            string legacyShaderGuid,
            Shader legacyShader,
            string undoName,
            List<string> failures)
        {
            try
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(entry.assetPath);
                if (material == null)
                {
                    failures.Add(FormatText("particleBaseMigration.failure.loadMaterial", "{0}: material could not be loaded.", entry.assetPath));
                    return false;
                }

                string matchSource;
                if (!IsLegacyParticleBaseMaterial(entry.assetPath, material, legacyShaderGuid, legacyShader, out matchSource))
                {
                    failures.Add(FormatText("particleBaseMigration.failure.noLongerParticleBase", "{0}: skipped because it no longer references ParticleBase.", entry.assetPath));
                    return false;
                }

                int[] legacyFoldoutValues = CaptureLegacyFoldoutValues(material, entry.assetPath);
                Undo.RecordObject(material, undoName);
                material.shader = targetShader;
                ApplyNBShader2FoldoutValues(material, legacyFoldoutValues);
                NBShaderSyncService.SyncMaterialState(material);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);

                entry.material = material;
                entry.matchSource = Text("particleBaseMigration.match.converted", "Converted");
                entry.selected = false;
                return true;
            }
            catch (Exception ex)
            {
                failures.Add(FormatText("particleBaseMigration.failure.exception", "{0}: {1}", entry.assetPath, ex.Message));
                return false;
            }
        }

        private static bool ConfirmConversion(int count)
        {
            return EditorUtility.DisplayDialog(
                Text("particleBaseMigration.confirm.title", "Confirm ParticleBase Migration"),
                FormatText(
                    "particleBaseMigration.confirm.message",
                    "You are about to convert {0} material asset(s) from ParticleBase to NBShader2.\n\nBack up assets or commit to version control before continuing. This tool does not provide an automatic rollback.",
                    count),
                Text("particleBaseMigration.dialog.convert", "Convert"),
                Text("particleBaseMigration.dialog.cancel", "Cancel"));
        }

        private static string BuildConversionStatus(int converted, int selected, bool canceled, int failureCount)
        {
            string status = canceled
                ? FormatText("particleBaseMigration.conversionCanceled", "Conversion canceled. Converted {0}/{1} selected material asset(s).", converted, selected)
                : FormatText("particleBaseMigration.conversionComplete", "Conversion complete. Converted {0}/{1} selected material asset(s).", converted, selected);

            if (failureCount > 0)
            {
                status += " " + FormatText("particleBaseMigration.conversionFailures", "{0} failure(s) were logged to the Console.", failureCount);
            }

            return status;
        }

        private static int[] CaptureLegacyFoldoutValues(Material material, string assetPath)
        {
            var values = new int[LegacyFoldoutProperties.Length];
            for (int i = 0; i < LegacyFoldoutProperties.Length; i++)
            {
                values[i] = int.MinValue;
                if (material != null && material.HasProperty(LegacyFoldoutProperties[i]))
                {
                    values[i] = material.GetInteger(LegacyFoldoutProperties[i]);
                    continue;
                }

                int serializedValue;
                if (TryReadSerializedIntegerProperty(assetPath, LegacyFoldoutProperties[i], out serializedValue))
                {
                    values[i] = serializedValue;
                }
            }

            return values;
        }

        private static void ApplyNBShader2FoldoutValues(Material material, int[] legacyValues)
        {
            if (material == null || legacyValues == null)
            {
                return;
            }

            int count = Math.Min(legacyValues.Length, NBShader2FoldoutProperties.Length);
            for (int i = 0; i < count; i++)
            {
                if (legacyValues[i] == int.MinValue || !material.HasProperty(NBShader2FoldoutProperties[i]))
                {
                    continue;
                }

                material.SetInteger(NBShader2FoldoutProperties[i], legacyValues[i]);
            }
        }

        private static bool IsLegacyParticleBaseMaterial(
            string assetPath,
            Material material,
            string legacyShaderGuid,
            Shader legacyShader,
            out string matchSource)
        {
            if (SerializedMaterialReferencesShaderGuid(assetPath, legacyShaderGuid))
            {
                matchSource = Text("particleBaseMigration.match.shaderGuid", "Shader GUID");
                return true;
            }

            if (material != null && material.shader != null)
            {
                if (legacyShader != null && material.shader == legacyShader)
                {
                    matchSource = Text("particleBaseMigration.match.shaderObject", "Shader object");
                    return true;
                }

                if (string.Equals(material.shader.name, LegacyShaderName, StringComparison.Ordinal))
                {
                    matchSource = Text("particleBaseMigration.match.shaderNameFallback", "Shader name fallback");
                    return true;
                }
            }

            matchSource = null;
            return false;
        }

        private static bool SerializedMaterialReferencesShaderGuid(string assetPath, string shaderGuid)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(shaderGuid))
            {
                return false;
            }

            string physicalPath;
            if (!TryGetPhysicalPath(assetPath, out physicalPath) || !File.Exists(physicalPath))
            {
                return false;
            }

            try
            {
                string content = File.ReadAllText(physicalPath);
                return content.IndexOf("m_Shader:", StringComparison.Ordinal) >= 0 &&
                       content.IndexOf("guid: " + shaderGuid, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadSerializedIntegerProperty(string assetPath, string propertyName, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            string physicalPath;
            if (!TryGetPhysicalPath(assetPath, out physicalPath) || !File.Exists(physicalPath))
            {
                return false;
            }

            try
            {
                string prefix = "- " + propertyName + ":";
                using (var reader = new StringReader(File.ReadAllText(physicalPath)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        string rawValue = trimmed.Substring(prefix.Length).Trim();
                        return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryGetPhysicalPath(string assetPath, out string physicalPath)
        {
            physicalPath = null;
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string normalizedPath = assetPath.Replace('\\', '/');
            if (normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                string.Equals(normalizedPath, "Assets", StringComparison.Ordinal))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                physicalPath = Path.Combine(projectRoot, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
                return true;
            }

            if (!normalizedPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                return false;
            }

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(normalizedPath);
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                return false;
            }

            string packageRoot = "Packages/" + packageInfo.name + "/";
            if (!normalizedPath.StartsWith(packageRoot, StringComparison.Ordinal))
            {
                return false;
            }

            string relativePath = normalizedPath.Substring(packageRoot.Length).Replace('/', Path.DirectorySeparatorChar);
            physicalPath = Path.Combine(packageInfo.resolvedPath, relativePath);
            return true;
        }

        private static string ResolveShaderGuid(string assetPath, string fallbackGuid)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            return string.IsNullOrEmpty(guid) ? fallbackGuid : guid;
        }

        private static Shader LoadShaderByGuidOrPath(string guid, string fallbackAssetPath)
        {
            string assetPath = string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = fallbackAssetPath;
            }

            return AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
        }

        private void SetAllSelected(bool selected)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].selected = selected;
            }
        }

        private int CountSelected()
        {
            int count = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].selected)
                {
                    count++;
                }
            }

            return count;
        }

        private List<MaterialMigrationEntry> GetSelectedEntries()
        {
            var selectedEntries = new List<MaterialMigrationEntry>();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].selected)
                {
                    selectedEntries.Add(_entries[i]);
                }
            }

            return selectedEntries;
        }

        private static void PingAsset(string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static GUIContent ButtonContent(string key, string fallback)
        {
            return NBShaderInspectorLocalization.MakeContent("inspector." + key + ".button", fallback);
        }

        private static string Text(string key, string fallback)
        {
            return NBShaderInspectorLocalization.GetInspectorText(key, fallback);
        }

        private static string FormatText(string key, string fallback, params object[] args)
        {
            return string.Format(Text(key, fallback), args);
        }

        private sealed class MaterialMigrationEntry
        {
            public string assetPath;
            public Material material;
            public string matchSource;
            public bool selected;
        }
    }
}
