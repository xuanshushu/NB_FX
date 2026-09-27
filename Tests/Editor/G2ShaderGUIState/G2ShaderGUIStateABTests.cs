using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NBShader;
using NBShaderEditor;
using NBShaders2.Editor.FeatureLevel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// G2 original-ShaderLab material state backend only. This does not drive NBShaderGUI.OnGUI,
    /// click an Inspector control, test Undo, or test material persistence.
    /// </summary>
    public sealed class G2ShaderGUIStateABTests
    {
        private const string SourceMaterialPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string TierSettingsPath = "ProjectSettings/NBShaderFeatureLevels.asset";

        private static readonly string[] PassNames =
        {
            "SRPDefaultUnlit", "UniversalForward", "DepthOnly", "ShadowCaster",
            "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D"
        };

        private static readonly string[] IntegerProperties =
        {
            "_W9ParticleShaderFlags", "_W9ParticleShaderFlags1",
            "_W9ParticleShaderWrapFlags", "_NBShaderForceNoMipFlags",
            "_W9ParticleShaderPNoiseBlendFlag", "_W9ParticleCustomDataFlag0"
        };

        private static readonly string[] FloatProperties =
        {
            "_NBShaderFeatureTier", "_MeshSourceMode", "_TransparentMode", "_Blend",
            "_QueueBias", "_ZWrite", "_ForceZWriteToggle", "_SrcBlend", "_DstBlend",
            "_Mask_Toggle", "_MaskRefineToggle", "_Mask3_Toggle", "_EmissionEnabled", "_EmissionAlphaMultiplyMode",
            "_ScreenDistortModeToggle", "_DisableMainPassToggle", "_VertexOffset_StartFromZero"
        };

        private string _sourceHash;
        private string _settingsHash;

        [SetUp]
        public void RecordProtectedInputs()
        {
            Assert.That(File.Exists(FullPath(SourceMaterialPath)), Is.True);
            Assert.That(File.Exists(FullPath(TierSettingsPath)), Is.True,
                "The state service can initialize and save missing tier settings; never run this fixture against an uninitialized project.");
            _sourceHash = HashFile(SourceMaterialPath);
            _settingsHash = HashFile(TierSettingsPath);
        }

        [TearDown]
        public void VerifyProtectedInputs()
        {
            Assert.That(HashFile(SourceMaterialPath), Is.EqualTo(_sourceHash), "The original sample material must remain unchanged.");
            Assert.That(HashFile(TierSettingsPath), Is.EqualTo(_settingsHash), "The project tier settings must remain unchanged.");
        }

        [Test]
        public void UltraMeshStateSynchronizesPackedFlagsKeywordsAndPassesIdempotently()
        {
            RequireTierFeatures(NBShaderFeatureTier.Ultra, new[] { "_MASKMAP_ON", "_EMISSION" });
            var material = CloneSource();
            try
            {
                ConfigureCommon(material, NBShaderFeatureTier.Ultra);
                SetFloat(material, "_MeshSourceMode", 1);
                SetFloat(material, "_Mask_Toggle", 1);
                SetFloat(material, "_MaskRefineToggle", 1);
                SetFloat(material, "_VertexOffset_StartFromZero", 1);
                SetFloat(material, "_EmissionEnabled", 1);
                SetFloat(material, "_EmissionAlphaMultiplyMode", 1);
                SetFloat(material, "_ScreenDistortModeToggle", 0);

                NBShaderSyncService.SyncMaterialState(material);
                var first = Snapshot(material);
                Assert.That(material.IsKeywordEnabled("_MASKMAP_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.True);
                Assert.That(material.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH,
                    Is.Not.Zero);
                Assert.That(material.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,
                    Is.Zero);
                Assert.That(material.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE,
                    Is.Not.Zero);
                Assert.That(material.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO,
                    Is.Not.Zero);
                Assert.That(material.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY,
                    Is.Not.Zero, "The Integer property must preserve the sign/high bit.");
                Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.True);
                Assert.That(material.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.False);
                Assert.That(material.GetShaderPassEnabled("NBDeferredDistortPass"), Is.False);

                NBShaderSyncService.SyncMaterialState(material);
                Assert.That(Snapshot(material), Is.EqualTo(first), "A second backend synchronization must be a semantic no-op.");

                // Visible-to-the-snapshot positive control, not an IMGUI click simulation.
                SetFloat(material, "_Mask_Toggle", 0);
                NBShaderSyncService.SyncMaterialState(material);
                Assert.That(material.IsKeywordEnabled("_MASKMAP_ON"), Is.False);
                Assert.That(Snapshot(material), Is.Not.EqualTo(first));
                SetFloat(material, "_Mask_Toggle", 1);
                NBShaderSyncService.SyncMaterialState(material);
                Assert.That(Snapshot(material), Is.EqualTo(first), "Restoring the property must restore the managed state.");
                LogUniversal2DState("ultra-mesh", material, NBShaderFeatureTier.Ultra);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void TierAndTransparentModesResolveBackendStateWithoutNormalizingRenderQueue()
        {
            RequireTierFeatures(NBShaderFeatureTier.Low, new[] { "_MASKMAP_ON", "_ALPHATEST_ON" });
            RequireTierFeatures(NBShaderFeatureTier.Ultra, new[] { "_MASKMAP3_ON", "_SCREEN_DISTORT_MODE", "_NOISEMAP" });
            RequirePassFeature(NBShaderFeatureTier.Low, "pass.depthOnly");
            RequirePassFeature(NBShaderFeatureTier.Ultra, "pass.screenDistort.deferred");
            RequirePassFeature(NBShaderFeatureTier.Ultra, "pass.screenDistort.cameraOpaque");

            var material = CloneSource();
            try
            {
                ConfigureCommon(material, NBShaderFeatureTier.Low);
                material.renderQueue = 2777; // Queue changes belong to the real Inspector callback, not this backend API.
                SetFloat(material, "_MeshSourceMode", 1);
                SetFloat(material, "_TransparentMode", 2); // CutOff
                SetFloat(material, "_Mask_Toggle", 1);
                SetFloat(material, "_Mask3_Toggle", 1);
                SetFloat(material, "_noisemapEnabled", 1); // _NOISEMAP is the screen-distort keyword's required parent.
                SetFloat(material, "_ScreenDistortModeToggle", 1); // Deferred intent
                SetFloat(material, "_DisableMainPassToggle", 1);

                NBShaderSyncService.SyncMaterialState(material);
                var low = Snapshot(material);
                Assert.That(material.IsKeywordEnabled("_MASKMAP_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_MASKMAP3_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_SCREEN_DISTORT_MODE"), Is.False);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.True);
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(1));
                Assert.That(material.GetFloat("_Blend"), Is.EqualTo(4));
                Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.One));
                Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.Zero));
                Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.True);
                Assert.That(material.GetShaderPassEnabled("DepthOnly"), Is.True);
                Assert.That(material.GetShaderPassEnabled("NBDeferredDistortPass"), Is.False);
                Assert.That(material.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_MODE,
                    Is.Zero);
                Assert.That(material.renderQueue, Is.EqualTo(2777));
                NBShaderSyncService.SyncMaterialState(material);
                Assert.That(Snapshot(material), Is.EqualTo(low));
                LogUniversal2DState("low-cutoff", material, NBShaderFeatureTier.Low);

                SetFloat(material, "_NBShaderFeatureTier", (float)NBShaderFeatureTier.Ultra);
                NBShaderSyncService.SyncMaterialState(material);
                var ultra = Snapshot(material);
                TestContext.WriteLine("ultra: mask3=" + material.IsKeywordEnabled("_MASKMAP3_ON") +
                    " screen=" + material.IsKeywordEnabled("_SCREEN_DISTORT_MODE") +
                    " screenIntent=" + material.GetFloat("_ScreenDistortModeToggle") +
                    " meshMode=" + material.GetFloat("_MeshSourceMode") +
                    " tier=" + material.GetFloat("_NBShaderFeatureTier") +
                    " allowed=" + NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSet(NBShaderFeatureTier.Ultra).Contains("_SCREEN_DISTORT_MODE") +
                    " main=" + material.GetShaderPassEnabled("UniversalForward") +
                    " deferred=" + material.GetShaderPassEnabled("NBDeferredDistortPass") +
                    " opaque=" + material.GetShaderPassEnabled("NBCameraOpaqueDistortPass"));
                Assert.That(ultra, Is.Not.EqualTo(low));
                Assert.That(material.IsKeywordEnabled("_MASKMAP3_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_SCREEN_DISTORT_MODE"), Is.True);
                Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.False);
                Assert.That(material.GetShaderPassEnabled("NBDeferredDistortPass"), Is.True);
                Assert.That(material.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.False);
                Assert.That(material.renderQueue, Is.EqualTo(2777));
                NBShaderSyncService.SyncMaterialState(material);
                Assert.That(Snapshot(material), Is.EqualTo(ultra));
                LogUniversal2DState("ultra-deferred", material, NBShaderFeatureTier.Ultra);

                SetFloat(material, "_ScreenDistortModeToggle", 2); // CameraOpaque intent
                NBShaderSyncService.SyncMaterialState(material);
                Assert.That(material.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.True);
                Assert.That(material.GetShaderPassEnabled("NBDeferredDistortPass"), Is.False);

                SetFloat(material, "_ScreenDistortModeToggle", 0);
                SetFloat(material, "_DisableMainPassToggle", 0);
                SetFloat(material, "_TransparentMode", 1); // Transparent
                NBShaderSyncService.SyncMaterialState(material);
                var transparent = Snapshot(material);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_SCREEN_DISTORT_MODE"), Is.False);
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0));
                Assert.That(material.GetFloat("_Blend"), Is.EqualTo(0));
                Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.SrcAlpha));
                Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                Assert.That(material.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_MODE,
                    Is.Not.Zero);
                Assert.That(material.GetShaderPassEnabled("DepthOnly"), Is.False);
                Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.True);
                Assert.That(material.renderQueue, Is.EqualTo(2777));
                NBShaderSyncService.SyncMaterialState(material);
                Assert.That(Snapshot(material), Is.EqualTo(transparent));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void MultiMaterialBackendSynchronizationKeepsPerMaterialIntentSeparate()
        {
            var first = CloneSource();
            var second = CloneSource();
            try
            {
                ConfigureCommon(first, NBShaderFeatureTier.Ultra);
                ConfigureCommon(second, NBShaderFeatureTier.Ultra);
                SetFloat(first, "_MeshSourceMode", 1);
                SetFloat(second, "_MeshSourceMode", 0);
                SetFloat(first, "_MaskRefineToggle", 1);
                SetFloat(second, "_MaskRefineToggle", 0);
                first.SetColor("_BaseColor", Color.red);
                second.SetColor("_BaseColor", Color.green);

                NBShaderSyncService.SyncMaterialState(new[] { first, second });
                var firstState = Snapshot(first);
                var secondState = Snapshot(second);
                Assert.That(first.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH,
                    Is.Not.Zero);
                Assert.That(first.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,
                    Is.Zero);
                Assert.That(second.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH,
                    Is.Zero);
                Assert.That(second.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,
                    Is.Not.Zero);
                Assert.That(first.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE,
                    Is.Not.Zero);
                Assert.That(second.GetInteger("_W9ParticleShaderFlags1") & NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE,
                    Is.Zero);
                Assert.That(first.GetColor("_BaseColor"), Is.EqualTo(Color.red));
                Assert.That(second.GetColor("_BaseColor"), Is.EqualTo(Color.green));

                NBShaderSyncService.SyncMaterialState(new[] { first, second });
                Assert.That(Snapshot(first), Is.EqualTo(firstState));
                Assert.That(Snapshot(second), Is.EqualTo(secondState));
                Assert.That(firstState, Is.Not.EqualTo(secondState));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        private static Material CloneSource()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(source.shader, Is.Not.Null);
            Assert.That(source.shader.name, Is.EqualTo(NBShaderFeatureCatalog.ShaderName));
            return new Material(source) { hideFlags = HideFlags.HideAndDontSave };
        }

        private static void ConfigureCommon(Material material, NBShaderFeatureTier tier)
        {
            SetFloat(material, "_NBShaderFeatureTier", (float)tier);
            SetFloat(material, "_ForceZWriteToggle", 0);
            SetFloat(material, "_AffectsShadows", 0);
            SetFloat(material, "_BackFirstPassToggle", 0);
            SetFloat(material, "_Blend", 0);
            SetFloat(material, "_Mask_Toggle", 0);
            SetFloat(material, "_Mask3_Toggle", 0);
            SetFloat(material, "_MaskRefineToggle", 0);
            SetFloat(material, "_EmissionEnabled", 0);
            SetFloat(material, "_EmissionAlphaMultiplyMode", 0);
            SetFloat(material, "_VertexOffset_StartFromZero", 0);
            SetFloat(material, "_ScreenDistortModeToggle", 0);
            SetFloat(material, "_DisableMainPassToggle", 0);
        }

        private static void RequireTierFeatures(NBShaderFeatureTier tier, string[] keywords)
        {
            var allowed = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSet(tier);
            foreach (var keyword in keywords)
                Assert.That(allowed.Contains(keyword), Is.True, "Test precondition: tier " + tier + " must allow " + keyword);
        }

        private static void RequirePassFeature(NBShaderFeatureTier tier, string featureId)
        {
            var allowed = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSet(tier);
            Assert.That(allowed.Contains(featureId), Is.True,
                "Test precondition: tier " + tier + " must allow pass feature " + featureId);
        }

        private static void LogUniversal2DState(string caseName, Material material, NBShaderFeatureTier tier)
        {
            var allowed = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSet(tier);
            Debug.Log("NBFX-G2-GUI-BACKEND " + caseName + ": Universal2D enabled=" +
                material.GetShaderPassEnabled("Universal2D") + ", tierAllows=" + allowed.Contains("pass.universal2D") +
                ". Observation only; this fixture does not assert desired 2D policy.");
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            Assert.That(material.HasProperty(propertyName), Is.True, "Missing ShaderLab property " + propertyName);
            material.SetFloat(propertyName, value);
        }

        private static string Snapshot(Material material)
        {
            var builder = new StringBuilder(1024);
            builder.Append("shader=").Append(material.shader.name).Append(";queue=").Append(material.renderQueue);
            foreach (var name in FloatProperties)
            {
                Assert.That(material.HasProperty(name), Is.True, "Missing snapshot property " + name);
                builder.Append(';').Append(name).Append('=').Append(material.GetFloat(name).ToString("R", CultureInfo.InvariantCulture));
            }

            foreach (var name in IntegerProperties)
            {
                Assert.That(material.HasProperty(name), Is.True, "Missing packed Integer property " + name);
                builder.Append(';').Append(name).Append("=0x")
                    .Append(unchecked((uint)material.GetInteger(name)).ToString("X8", CultureInfo.InvariantCulture));
            }

            foreach (var keyword in NBShaderFeatureCatalog.RawKeywords)
                builder.Append(";kw:").Append(keyword).Append('=').Append(material.IsKeywordEnabled(keyword) ? '1' : '0');
            builder.Append(";kw:EVALUATE_SH_VERTEX=").Append(material.IsKeywordEnabled("EVALUATE_SH_VERTEX") ? '1' : '0');
            foreach (var passName in PassNames)
                builder.Append(";pass:").Append(passName).Append('=').Append(material.GetShaderPassEnabled(passName) ? '1' : '0');
            builder.Append(";BaseColor=").Append(material.GetColor("_BaseColor").ToString());
            return builder.ToString();
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), projectRelativePath);
        }

        private static string HashFile(string projectRelativePath)
        {
            using (var sha256 = SHA256.Create())
                return BitConverter.ToString(sha256.ComputeHash(File.ReadAllBytes(FullPath(projectRelativePath)))).Replace("-", "");
        }
    }
}
