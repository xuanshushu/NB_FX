using System;
using System.IO;
using UnityEditor;

namespace NBShader.Editor
{
    public sealed class VATTextureImporter : AssetPostprocessor
    {
        private const string VatKeyword = "vat";
        private const string VatKeyword2 = "vertex_animation_textures";
        
        private const string ExrExtension = ".exr";
        private const string DefaultPlatformName = "DefaultTexturePlatform";
        private const string StandalonePlatformName = "Standalone";

        private void OnPreprocessTexture()
        {
            if (!(assetImporter is TextureImporter textureImporter))
            {
                return;
            }

            if (!ShouldImport(assetPath))
            {
                return;
            }

            ApplyImportSettings(textureImporter);
        }

        internal static bool ShouldImport(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (!HasSupportedExtension(path))
            {
                return false;
            }

            bool shouldImport = false;
            shouldImport |= path.IndexOf(VatKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
            shouldImport |= path.IndexOf(VatKeyword2, StringComparison.OrdinalIgnoreCase) >= 0;

            return shouldImport;
        }

        private static bool HasSupportedExtension(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return extension.Equals(ExrExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyImportSettings(TextureImporter textureImporter)
        {
            ApplyCommonDataSettings(textureImporter);

            string extension = Path.GetExtension(textureImporter.assetPath);
            if (!extension.Equals(ExrExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ApplyHdrPlatformSettings(textureImporter, DefaultPlatformName, TextureImporterFormat.RGBAHalf);
            ApplyHdrPlatformSettings(textureImporter, StandalonePlatformName, TextureImporterFormat.RGBAHalf);
        }

        private static void ApplyCommonDataSettings(TextureImporter textureImporter)
        {
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.sRGBTexture = false;
            // textureImporter.isReadable = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.alphaIsTransparency = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private static void ApplyHdrPlatformSettings(TextureImporter textureImporter, string platformName, TextureImporterFormat format)
        {
            TextureImporterPlatformSettings settings = textureImporter.GetPlatformTextureSettings(platformName);
            settings.name = platformName;
            settings.overridden = true;
            settings.format = format;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.compressionQuality = 100;
            settings.crunchedCompression = false;
            settings.allowsAlphaSplitting = false;
            textureImporter.SetPlatformTextureSettings(settings);
        }
    }
}
