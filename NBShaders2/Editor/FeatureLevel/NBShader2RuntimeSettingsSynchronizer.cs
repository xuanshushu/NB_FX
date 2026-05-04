using System;
using System.IO;
using System.Reflection;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [InitializeOnLoad]
    public static class NBShader2RuntimeSettingsSynchronizer
    {
        public const string RuntimeSettingsAssetPath = "Assets/NBShaders2/Runtime/Resources/NBShader2FeatureRuntimeSettings.asset";

        private static readonly string[] RuntimeTypeNames =
        {
            // Current runtime worker API assumption. Resources.Load path is NBShader2FeatureRuntimeSettings.
            "NBShader.NBShader2FeatureRuntimeSettings, com.xuanxuan.nb.shaders2",
            // Forward-compatible fallbacks if the runtime type is renamed during integration.
            "NBShader2FeatureRuntimeSettings, com.xuanxuan.nb.shaders2",
            "NBShaders2.NBShader2FeatureRuntimeSettings, com.xuanxuan.nb.shaders2",
            "NBShader2FeatureLevelRuntimeSettings, com.xuanxuan.nb.shaders2",
            "NBShaders2.NBShader2FeatureLevelRuntimeSettings, com.xuanxuan.nb.shaders2",
        };

        static NBShader2RuntimeSettingsSynchronizer()
        {
            EditorApplication.delayCall += SyncFromProjectSettingsIfRuntimeTypeExists;
        }

        public static bool SyncFromProjectSettings()
        {
            var runtimeType = FindRuntimeSettingsType();
            if (runtimeType == null)
            {
                Debug.Log("NBShader2 feature level runtime settings type was not found yet; Project Settings were saved and will sync after runtime type lands.");
                return false;
            }

            if (!typeof(ScriptableObject).IsAssignableFrom(runtimeType))
            {
                Debug.LogWarning("NBShader2 feature level runtime settings type must derive from ScriptableObject: " + runtimeType.FullName);
                return false;
            }

            EnsureResourcesFolder();
            var asset = AssetDatabase.LoadAssetAtPath(RuntimeSettingsAssetPath, runtimeType) as ScriptableObject;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance(runtimeType);
                AssetDatabase.CreateAsset(asset, RuntimeSettingsAssetPath);
            }

            ApplyProjectSettingsToRuntimeObject(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static void SyncFromProjectSettingsIfRuntimeTypeExists()
        {
            if (FindRuntimeSettingsType() != null)
                SyncFromProjectSettings();
        }

        private static void ApplyProjectSettingsToRuntimeObject(ScriptableObject asset)
        {
            var settings = NBShader2FeatureLevelProjectSettings.instance;
            settings.EnsureInitialized();
            var type = asset.GetType();

            SetMember(type, asset, "buildStripPolicy", (int)settings.buildStripPolicy);
            SetMember(type, asset, "BuildStripPolicy", (int)settings.buildStripPolicy);
            SetMember(type, asset, "explicitTier", (int)settings.explicitTier);
            SetMember(type, asset, "ExplicitTier", (int)settings.explicitTier);

            SetMember(type, asset, "lowAllowedKeywords", settings.GetAllowedKeywordSet(NBShader2FeatureTier.Low));
            SetMember(type, asset, "mediumAllowedKeywords", settings.GetAllowedKeywordSet(NBShader2FeatureTier.Medium));
            SetMember(type, asset, "highAllowedKeywords", settings.GetAllowedKeywordSet(NBShader2FeatureTier.High));
            SetMember(type, asset, "ultraAllowedKeywords", settings.GetAllowedKeywordSet(NBShader2FeatureTier.Ultra));

            SetMember(type, asset, "tierKeywordSets", settings.tierKeywordSets);
            SetMember(type, asset, "TierKeywordSets", settings.tierKeywordSets);
            SetMember(type, asset, "qualityTierMappings", settings.qualityTierMappings);
            SetMember(type, asset, "QualityTierMappings", settings.qualityTierMappings);

            var method = type.GetMethod("ApplyEditorSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(NBShader2FeatureLevelProjectSettings))
                    method.Invoke(asset, new object[] { settings });
            }
        }

        private static bool SetMember(Type type, object target, string name, object value)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = type.GetField(name, flags);
            if (field != null)
            {
                if (TrySetValue(field.FieldType, delegate(object converted) { field.SetValue(target, converted); }, value))
                    return true;
            }

            var property = type.GetProperty(name, flags);
            if (property != null && property.CanWrite)
            {
                if (TrySetValue(property.PropertyType, delegate(object converted) { property.SetValue(target, converted, null); }, value))
                    return true;
            }

            return false;
        }

        private delegate void ValueSetter(object converted);

        private static bool TrySetValue(Type destinationType, ValueSetter setter, object value)
        {
            try
            {
                if (value == null || destinationType.IsInstanceOfType(value))
                {
                    setter(value);
                    return true;
                }

                if (destinationType == typeof(string[]) && value is System.Collections.Generic.HashSet<string>)
                {
                    var set = (System.Collections.Generic.HashSet<string>)value;
                    var array = new string[set.Count];
                    set.CopyTo(array);
                    setter(array);
                    return true;
                }

                if (destinationType.IsArray && destinationType.GetElementType() != null && value is NBShader2QualityTierMapping[])
                {
                    var converted = ConvertQualityMappings(destinationType.GetElementType(), (NBShader2QualityTierMapping[])value);
                    if (converted != null)
                    {
                        setter(converted);
                        return true;
                    }
                }

                if (destinationType.IsEnum && value is int)
                {
                    setter(Enum.ToObject(destinationType, (int)value));
                    return true;
                }

                if (destinationType == typeof(int) && value.GetType().IsEnum)
                {
                    setter((int)value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to sync NBShader2 runtime setting: " + ex.Message);
            }
            return false;
        }

        private static Array ConvertQualityMappings(Type elementType, NBShader2QualityTierMapping[] source)
        {
            if (elementType == null || source == null)
                return null;

            var result = Array.CreateInstance(elementType, source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var src = source[i];
                var item = Activator.CreateInstance(elementType);
                SetMember(elementType, item, "qualityName", src != null ? src.qualityName : string.Empty);
                SetMember(elementType, item, "QualityName", src != null ? src.qualityName : string.Empty);
                SetMember(elementType, item, "tier", src != null ? (int)src.tier : (int)NBShader2FeatureTier.Ultra);
                SetMember(elementType, item, "Tier", src != null ? (int)src.tier : (int)NBShader2FeatureTier.Ultra);
                result.SetValue(item, i);
            }
            return result;
        }

        private static Type FindRuntimeSettingsType()
        {
            for (var i = 0; i < RuntimeTypeNames.Length; i++)
            {
                var type = Type.GetType(RuntimeTypeNames[i]);
                if (type != null)
                    return type;
            }
            return null;
        }

        private static void EnsureResourcesFolder()
        {
            var folder = Path.GetDirectoryName(RuntimeSettingsAssetPath);
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
        }
    }
}
