using System;
using System.IO;
using System.Text;
using NBShaderEditor;
using NBShaders2.Editor.FeatureLevel;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor
{
    [InitializeOnLoad]
    internal static class NBShaderDebugSymbolsSettingsProvider
    {
        internal const string DebugIncludeAssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDebugPragmas.hlsl";
        private const string ShaderAssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        private const string PackageJsonAssetPath = "Packages/com.xuanxuan.nb.fx/package.json";
        private const string PackageRootAssetPath = "Packages/com.xuanxuan.nb.fx/";

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private const string DisabledIncludeContent =
            "// NBShader2 shader compiler debug switches.\n" +
            "// This file is intentionally included with #include_with_pragmas.\n" +
            "\n" +
            "#undef NB_SHADER_DEBUG_SYMBOLS\n" +
            "#define NB_SHADER_DEBUG_SYMBOLS 0\n";

        private const string EnabledIncludeContent =
            "// NBShader2 shader compiler debug switches.\n" +
            "// This file is intentionally included with #include_with_pragmas.\n" +
            "\n" +
            "#undef NB_SHADER_DEBUG_SYMBOLS\n" +
            "#define NB_SHADER_DEBUG_SYMBOLS 1\n" +
            "#pragma enable_d3d11_debug_symbols\n";

        private enum IncludeDebugState
        {
            Missing,
            Disabled,
            Enabled
        }

        static NBShaderDebugSymbolsSettingsProvider()
        {
            NBFXProjectSettings.RegisterSettingsSection(
                "NBShaderDebugSymbols",
                () => new GUIContent(Text("debugSymbols.providerLabel", "NBShader2 Debug Symbols")),
                OnGUI,
                new[]
                {
                    "NBShader",
                    "Debug",
                    "Symbols",
                    "D3D11",
                    "enable_d3d11_debug_symbols"
                },
                90);
        }

        private static void OnGUI(string searchContext)
        {
            var settings = NBShaderFeatureLevelProjectSettings.instance;
            settings.EnsureInitialized();
            var includeState = GetIncludeDebugState();
            var includeEnabled = includeState == IncludeDebugState.Enabled;

            EditorGUILayout.HelpBox(
                Text(
                    "debugSymbols.help.message",
                    "Controls NBShader2 shader compiler debug symbols by rewriting the package include used by NBShader.shader. Enable only while debugging external shader tools."),
                MessageType.Info);

            DrawCachingShaderPreprocessorState();
            DrawStateRows(settings.enableDebugSymbols, includeState);

            if (settings.enableDebugSymbols != includeEnabled || includeState == IncludeDebugState.Missing)
            {
                EditorGUILayout.HelpBox(
                    Text(
                        "debugSymbols.includeMismatch.message",
                        "The ProjectSettings value and package include content do not match. Apply the current setting to resync the shader compiler input."),
                    MessageType.Warning);

                if (GUILayout.Button(ButtonContent("debugSymbols.applySetting", "Apply Current Setting To Include")))
                    ApplySettingToInclude(settings, settings.enableDebugSymbols);
            }

            var enabled = settings.enableDebugSymbols;
            var toggleChanged = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                enabled = EditorGUILayout.Toggle(
                    Content(
                        "debugSymbols.enable",
                        "Enable NBShader2 Debug Symbols",
                        "Writes #pragma enable_d3d11_debug_symbols into the NBShader2 debug pragma include."),
                    settings.enableDebugSymbols);
                toggleChanged = EditorGUI.EndChangeCheck();

                if (GUILayout.Button(ButtonContent("debugSymbols.pingInclude", "Select Debug Toggle Include"), GUILayout.Width(180f)))
                    PingIncludeAsset();
            }

            if (toggleChanged)
            {
                if (!enabled || ConfirmEnableDebugSymbols())
                    ApplySettingToInclude(settings, enabled);
            }

            EditorGUILayout.HelpBox(
                Text(
                    "debugSymbols.dirtyWarning.message",
                    "Enabling this option modifies NBShaderDebugPragmas.hlsl in the NB_FX package. Do not commit the enabled include unless shader debug symbols are deliberately required."),
                MessageType.Warning);

        }

        internal static bool WriteDebugInclude(bool enabled, out string error)
        {
            error = null;
            string physicalPath;
            if (!TryGetPhysicalPath(DebugIncludeAssetPath, out physicalPath, out error))
                return false;

            var desiredContent = enabled ? EnabledIncludeContent : DisabledIncludeContent;
            try
            {
                var folder = Path.GetDirectoryName(physicalPath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (File.Exists(physicalPath) && string.Equals(File.ReadAllText(physicalPath), desiredContent, StringComparison.Ordinal))
                {
                    ReimportShaderAssets();
                    return true;
                }

                File.WriteAllText(physicalPath, desiredContent, Utf8NoBom);
                ReimportShaderAssets();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void DrawCachingShaderPreprocessorState()
        {
            var enabled = EditorSettings.cachingShaderPreprocessor;
            EditorGUILayout.LabelField(
                Content(
                    "debugSymbols.cachingPreprocessor",
                    "Caching Shader Preprocessor",
                    "#include_with_pragmas requires the Caching Shader Preprocessor."),
                new GUIContent(enabled
                    ? Text("debugSymbols.status.enabled", "Enabled")
                    : Text("debugSymbols.status.disabled", "Disabled")));

            if (enabled)
                return;

            EditorGUILayout.HelpBox(
                Text(
                    "debugSymbols.cachingPreprocessorWarning.message",
                    "#include_with_pragmas requires the Caching Shader Preprocessor. Enable it before using NBShader2 debug symbols."),
                MessageType.Warning);

            if (GUILayout.Button(ButtonContent("debugSymbols.enableCachingPreprocessor", "Enable Caching Shader Preprocessor")))
            {
                EditorSettings.cachingShaderPreprocessor = true;
                ReimportShaderAssets();
            }
        }

        private static void DrawStateRows(bool settingsEnabled, IncludeDebugState includeState)
        {
            EditorGUILayout.LabelField(
                Content("debugSymbols.settingsState", "ProjectSettings State", "Saved state in ProjectSettings/NBShaderFeatureLevels.asset."),
                new GUIContent(settingsEnabled
                    ? Text("debugSymbols.status.enabled", "Enabled")
                    : Text("debugSymbols.status.disabled", "Disabled")));

            EditorGUILayout.LabelField(
                Content("debugSymbols.includeState", "Include File State", "Actual package include content used by NBShader.shader."),
                new GUIContent(GetIncludeStateText(includeState)));
        }

        private static void ApplySettingToInclude(NBShaderFeatureLevelProjectSettings settings, bool enabled)
        {
            Undo.RecordObject(settings, Text("debugSymbols.undo.changeSetting", "Change NBShader2 Debug Symbols"));

            string error;
            if (WriteDebugInclude(enabled, out error))
            {
                if (settings.enableDebugSymbols != enabled)
                {
                    settings.SetDebugSymbolsEnabled(enabled);
                    settings.SaveDebugSymbolsProjectSettings();
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                return;
            }

            EditorUtility.DisplayDialog(
                Text("debugSymbols.applyFailed.title", "Apply NBShader2 Debug Symbols Failed"),
                Text("debugSymbols.applyFailed.message", "Could not write NBShaderDebugPragmas.hlsl: ") + error,
                Text("debugSymbols.dialog.ok", "OK"));
        }

        private static bool ConfirmEnableDebugSymbols()
        {
            return EditorUtility.DisplayDialog(
                Text("debugSymbols.confirmEnable.title", "Enable NBShader2 Debug Symbols"),
                Text(
                    "debugSymbols.confirmEnable.message",
                    "This writes #pragma enable_d3d11_debug_symbols into the NB_FX package include. Shader size can increase and optimizations are disabled while it is enabled."),
                Text("debugSymbols.dialog.enable", "Enable"),
                Text("debugSymbols.dialog.cancel", "Cancel"));
        }

        private static IncludeDebugState GetIncludeDebugState()
        {
            string physicalPath;
            string error;
            if (!TryGetPhysicalPath(DebugIncludeAssetPath, out physicalPath, out error) || !File.Exists(physicalPath))
                return IncludeDebugState.Missing;

            try
            {
                return ContainsActiveDebugPragma(File.ReadAllText(physicalPath))
                    ? IncludeDebugState.Enabled
                    : IncludeDebugState.Disabled;
            }
            catch
            {
                return IncludeDebugState.Missing;
            }
        }

        private static bool ContainsActiveDebugPragma(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("//", StringComparison.Ordinal))
                        continue;

                    if (trimmed.StartsWith("#pragma", StringComparison.Ordinal) &&
                        trimmed.IndexOf("enable_d3d11_debug_symbols", StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetPhysicalPath(string assetPath, out string physicalPath, out string error)
        {
            physicalPath = null;
            error = null;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            if (packageInfo == null)
                packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackageJsonAssetPath);

            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                error = "Could not resolve package path for " + assetPath;
                return false;
            }

            if (!assetPath.StartsWith(PackageRootAssetPath, StringComparison.Ordinal))
            {
                error = "Asset path is outside NB_FX package: " + assetPath;
                return false;
            }

            var relativePath = assetPath.Substring(PackageRootAssetPath.Length).Replace('/', Path.DirectorySeparatorChar);
            physicalPath = Path.Combine(packageInfo.resolvedPath, relativePath);
            return true;
        }

        private static void ReimportShaderAssets()
        {
            AssetDatabase.ImportAsset(DebugIncludeAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ShaderAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void PingIncludeAsset()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(DebugIncludeAssetPath);
            if (asset == null)
                return;

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private static string GetIncludeStateText(IncludeDebugState state)
        {
            switch (state)
            {
                case IncludeDebugState.Enabled:
                    return Text("debugSymbols.status.enabled", "Enabled");
                case IncludeDebugState.Disabled:
                    return Text("debugSymbols.status.disabled", "Disabled");
                default:
                    return Text("debugSymbols.status.missing", "Missing");
            }
        }

        private static GUIContent Content(string key, string fallback, string tip)
        {
            return NBShaderInspectorLocalization.MakeInspectorContent(key, fallback, tip);
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
}
