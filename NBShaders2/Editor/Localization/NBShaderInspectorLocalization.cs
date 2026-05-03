using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    [InitializeOnLoad]
    internal static class NBShaderInspectorLocalization
    {
        internal const string TableName = "NBShader2";

        private const string CsvAssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Editor/Localization/NBShaderInspectorLocalization.csv";

        static NBShaderInspectorLocalization()
        {
            RegisterTable();
        }

        public static string CurrentLanguage
        {
            get
            {
                RegisterTable();
                return ShaderGUILocalization.GetCurrentLanguage(TableName);
            }
        }

        public static GUIContent MakeContent(string labelKey, string labelFallback, string tooltipKey = null, string tooltipFallback = "")
        {
            RegisterTable();
            return ShaderGUILocalization.MakeContent(TableName, labelKey, labelFallback, tooltipKey, tooltipFallback);
        }

        public static GUIContent MakeInspectorContent(string key, string fallback, string tip = "")
        {
            RegisterTable();
            return ShaderGUILocalization.MakeInspectorContent(TableName, key, fallback, tip);
        }

        public static string GetInspectorText(string key, string fallback = "")
        {
            RegisterTable();
            return ShaderGUILocalization.GetInspectorText(TableName, key, fallback);
        }

        public static string[] GetInspectorOptions(string key, string[] fallback)
        {
            RegisterTable();
            return ShaderGUILocalization.GetInspectorOptions(TableName, key, fallback);
        }

        public static string[] GetOptions(string keyPrefix, string[] fallback)
        {
            RegisterTable();
            return ShaderGUILocalization.GetOptions(TableName, keyPrefix, fallback);
        }

        public static string Get(string key, string fallback = "")
        {
            RegisterTable();
            return ShaderGUILocalization.Get(TableName, key, fallback);
        }

        public static string GetTooltip(string labelKey, string tooltipKey = null, string fallback = "")
        {
            RegisterTable();
            return ShaderGUILocalization.GetTooltip(TableName, labelKey, tooltipKey, fallback);
        }

        public static void Reload()
        {
            RegisterTable();
            ShaderGUILocalization.Reload(TableName);
        }

        private static void RegisterTable()
        {
            ShaderGUILocalization.RegisterCsv(TableName, CsvAssetPath);
        }
    }
}
