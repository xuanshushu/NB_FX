using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // CPU-only contract on real imported Graph Materials. No renderer, scene,
    // refresh, warm-up, save or GPU equivalence assertion is owned by this slice.
    public sealed class G4GraphNormalizedIntentReaderTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string Version = "_NB_GraphGUIStateVersion";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] MissingKeywords =
        {
            "NB_DEBUG_DISSOLVE", "NB_DEBUG_DISTORT", "NB_DEBUG_FRESNEL", "NB_DEBUG_MASK",
            "NB_DEBUG_PNOISE", "NB_DEBUG_VERTEX_OFFSET", "_SHARED_UV", "_SPECULAR_COLOR", "_STENCIL_WITHOUT_PLAYER"
        };
        static readonly string[] MissingProperties =
        {
            "_NB_Debug_Dissolve", "_NB_Debug_Distort", "_NB_Debug_Fresnel", "_NB_Debug_Mask",
            "_NB_Debug_PNoise", "_NB_Debug_VertexOffset", "_SharedUVToggle", "_BlinnPhongSpecularToggle", "_StencilWithoutPlayerToggle"
        };
        static readonly string[] PassNames =
        {
            "Universal Forward", "UniversalForward", "DepthOnly", "DepthNormalsOnly", "ShadowCaster",
            "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "SRPDefaultUnlit", "Universal2D"
        };
        readonly List<Object> owned = new List<Object>();

        static Type FindType(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, name); return type;
        }

        static object Field(object value, string name)
        {
            var field = value.GetType().GetField(name, Instance);
            Assert.That(field, Is.Not.Null, name); return field.GetValue(value);
        }

        static string[] Keywords(object result, string name = "effectiveKeywords") => (string[])Field(result, name);

        static bool Read(Material material, int tier, IEnumerable<string> allowed, out object result, out string[] missing)
        {
            var type = FindType("NBShader.NBShaderMaterialIntentResolver");
            var method = type.GetMethod("TryResolveGraphSupportedKeywordIntent", Static);
            Assert.That(method, Is.Not.Null, "Install the reader preview before this fixture.");
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), tier), allowed, null, null };
            bool accepted = (bool)method.Invoke(null, args);
            result = args[3]; missing = (string[])args[4]; return accepted;
        }

        Material ActualGraph()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(shader, Is.Not.Null, "Existing imported Graph must be present; no test imports it.");
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            owned.Add(material); material.SetFloat(Version, 2f); return material;
        }

        static void ClearToggles(Material material)
        {
            var bindings = (IEnumerable)FindType("NBShader.NBShaderMaterialIntentResolver").GetField("ToggleKeywordBindings", Static).GetValue(null);
            foreach (var binding in bindings)
            {
                string property = (string)Field(binding, "propertyName");
                if (material.HasProperty(property)) material.SetFloat(property, 0f);
            }
            material.SetFloat("_VAT_Toggle", 0f);
            material.SetFloat("_Surface", 0f); material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Blend", 0f); material.SetFloat("_FxLightMode", 0f);
            material.SetFloat("_DistortMode", 0f); material.SetFloat("_RampColorSourceMode", 0f);
            material.SetFloat("_DissolveRampSourceMode", 0f); material.SetFloat("_VATMode", 0f);
            material.SetFloat("_HoudiniVATSubMode", 0f); material.SetFloat("_TyFlowVATSubMode", 0f);
        }

        static string Snapshot(Material material)
        {
            return EditorJsonUtility.ToJson(material) + "\n" + string.Join("|", material.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal)) +
                "\n" + string.Join("|", PassNames.Select(p => p + ":" + material.GetShaderPassEnabled(p)));
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (Object item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item);
            owned.Clear();
        }

        [TestCase(0, TestName = "G4NormalizedReader_ImportedGraph_SupportedCapabilityIsReadOnly_ReportsNineUnavailable_01")] [TestCase(1, TestName = "G4NormalizedReader_ImportedGraph_SupportedCapabilityIsReadOnly_ReportsNineUnavailable_02")] [TestCase(2, TestName = "G4NormalizedReader_ImportedGraph_SupportedCapabilityIsReadOnly_ReportsNineUnavailable_03")] [TestCase(3, TestName = "G4NormalizedReader_ImportedGraph_SupportedCapabilityIsReadOnly_ReportsNineUnavailable_04")]
        public void ImportedGraph_SupportedCapabilityIsReadOnly_ReportsNineUnavailable(int tier)
        {
            var material = ActualGraph();
            material.SetFloat("_NB_Flags0Lo16", -17.25f); material.SetFloat("_NB_Flags1Hi16", 70000.5f);
            material.SetFloat("_InvertFresnel_Toggle", 1f); // GUI mirror deliberately disagrees with raw.
            material.SetFloat("_noisemapEnabled", 1f); material.SetFloat("_noiseMaskMap_Toggle", 1f);
            material.renderQueue = 3127; material.SetOverrideTag("NBReaderProbe", "unchanged");
            material.SetShaderPassEnabled("DepthNormalsOnly", false); material.EnableKeyword("NB_READER_EXTERNAL_PROBE");
            string before = Snapshot(material);
            Assert.That(Read(material, tier, null, out var result, out var missing), Is.True);
            Assert.That(missing, Is.EqualTo(MissingKeywords));
            Assert.That(MissingProperties.All(p => !material.HasProperty(p)), Is.True, "Current v1 unavailable schema changed; review its consumers before extending capability.");
            Assert.That(Keywords(result), Does.Contain("_NOISEMAP"));
            Assert.That(Keywords(result), Does.Contain("_NOISE_MASKMAP"));
            Assert.That(Field(result, "material"), Is.SameAs(material));
            Assert.That(Convert.ToInt32(Field(result, "tier")), Is.EqualTo(tier));
            Assert.That((Array)Field(result, "passes"), Is.Empty);
            Assert.That((string[])Field(result, "includedPassNames"), Is.Empty);
            Assert.That((string[])Field(result, "strippedPassNames"), Is.Empty);
            Assert.That(Snapshot(material), Is.EqualTo(before), "Reader must not seed or canonicalize any material state.");
        }

        [TestCase(0f, TestName = "G4NormalizedReader_UnknownOrUnseededMarker_IsRejectedWithoutMutation_01")] [TestCase(1f, TestName = "G4NormalizedReader_UnknownOrUnseededMarker_IsRejectedWithoutMutation_02")] [TestCase(3f, TestName = "G4NormalizedReader_UnknownOrUnseededMarker_IsRejectedWithoutMutation_03")] [TestCase(-1f, TestName = "G4NormalizedReader_UnknownOrUnseededMarker_IsRejectedWithoutMutation_04")] [TestCase(2.25f, TestName = "G4NormalizedReader_UnknownOrUnseededMarker_IsRejectedWithoutMutation_05")]
        public void UnknownOrUnseededMarker_IsRejectedWithoutMutation(float marker)
        {
            var material = ActualGraph(); material.SetFloat(Version, marker); string before = Snapshot(material);
            Assert.That(Read(material, 3, null, out var result, out var missing), Is.False);
            Assert.That(result, Is.Null); Assert.That(missing, Is.EqualTo(MissingKeywords));
            Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [TestCase(-1, TestName = "G4NormalizedReader_UnknownTier_IsRejectedWithoutMutation_01")] [TestCase(4, TestName = "G4NormalizedReader_UnknownTier_IsRejectedWithoutMutation_02")]
        public void UnknownTier_IsRejectedWithoutMutation(int tier)
        {
            var material = ActualGraph(); string before = Snapshot(material);
            Assert.That(Read(material, tier, null, out var result, out _), Is.False);
            Assert.That(result, Is.Null); Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [TestCase("_NB_Flags0Lo16", float.NaN, TestName = "G4NormalizedReader_NonFiniteOwnedInput_IsRejected_01")]
        [TestCase("_NB_CustomDataFlag3Hi16", float.PositiveInfinity, TestName = "G4NormalizedReader_NonFiniteOwnedInput_IsRejected_02")]
        [TestCase("_noisemapEnabled", float.NegativeInfinity, TestName = "G4NormalizedReader_NonFiniteOwnedInput_IsRejected_03")]
        [TestCase(Version, float.NaN, TestName = "G4NormalizedReader_NonFiniteOwnedInput_IsRejected_04")]
        public void NonFiniteOwnedInput_IsRejected(string property, float value)
        {
            var material = ActualGraph(); material.SetFloat(property, value); string before = Snapshot(material);
            Assert.That(Read(material, 3, null, out var result, out _), Is.False);
            Assert.That(result, Is.Null); Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [Test]
        public void NullMaterial_IsRejected_AndLegacyIdentityRemainsGuarded()
        {
            Assert.That(Read(null, 3, null, out var result, out var missing), Is.False);
            Assert.That(result, Is.Null); Assert.That(missing, Is.EqualTo(MissingKeywords));
            var graph = ActualGraph();
            Assert.That(FindType("NBShader.NBShaderMaterialIntentResolver").GetMethod("IsNBShaderMaterial", Static).Invoke(null, new object[] { graph }), Is.False);
            var legacy = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");
            Assert.That(legacy, Is.Not.Null);
            var material = new Material(legacy) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            Assert.That(Read(material, 3, null, out result, out _), Is.False); Assert.That(result, Is.Null);
        }

        static IEnumerable<TestCaseData> SurfaceCases()
        {
            for (int surface = 0; surface <= 1; ++surface)
                for (int clip = 0; clip <= 1; ++clip)
                    for (int blend = 0; blend <= 3; ++blend)
                        yield return new TestCaseData(surface, clip, blend).SetName("G4NormalizedReader_Surface_s" + surface + "_c" + clip + "_b" + blend);
        }

        [TestCaseSource(nameof(SurfaceCases))]
        public void OfficialURPSurfaceModes_AreReadWithoutLegacyProjection(int surface, int clip, int blend)
        {
            var material = ActualGraph(); ClearToggles(material);
            material.SetFloat("_Surface", surface); material.SetFloat("_AlphaClip", clip); material.SetFloat("_Blend", blend);
            string before = Snapshot(material);
            Assert.That(Read(material, 3, null, out var result, out _), Is.True);
            Assert.That(Keywords(result).Contains("_ALPHATEST_ON"), Is.EqualTo(clip == 1));
            Assert.That(Keywords(result).Contains("_ALPHAMODULATE_ON"), Is.EqualTo(surface == 1 && blend == 3));
            Assert.That(Keywords(result), Does.Not.Contain("_ALPHAPREMULTIPLY_ON"), "Official URP Unlit has no Preserve Specular field; legacy premultiply mapping is not its validation rule.");
            Assert.That(Keywords(result), Does.Not.Contain("_PARCUSTOMDATA_ON"));
            Assert.That(Keywords(result), Does.Not.Contain("_SCREEN_DISTORT_MODE"));
            Assert.That(Keywords(result), Does.Not.Contain("_SCRIPTABLETIME"));
            Assert.That(Keywords(result), Does.Not.Contain("_UNSCALETIME"));
            Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [TestCase(0, "_FX_LIGHT_MODE_UNLIT", TestName = "G4NormalizedReader_LightModes_UseExistingEnumContract_01")] [TestCase(1, "_FX_LIGHT_MODE_BLINN_PHONG", TestName = "G4NormalizedReader_LightModes_UseExistingEnumContract_02")]
        [TestCase(2, "_FX_LIGHT_MODE_HALF_LAMBERT", TestName = "G4NormalizedReader_LightModes_UseExistingEnumContract_03")] [TestCase(3, "_FX_LIGHT_MODE_PBR", TestName = "G4NormalizedReader_LightModes_UseExistingEnumContract_04")]
        [TestCase(4, "_FX_LIGHT_MODE_SIX_WAY", TestName = "G4NormalizedReader_LightModes_UseExistingEnumContract_05")]
        public void LightModes_UseExistingEnumContract(int mode, string expected)
        {
            var material = ActualGraph(); ClearToggles(material);
            material.SetFloat("_FxLightMode", mode); material.SetFloat("_SixWayColorAbsorptionToggle", 1);
            Assert.That(Read(material, 3, null, out var result, out _), Is.True);
            Assert.That(Keywords(result).Where(k => k.StartsWith("_FX_LIGHT_MODE_", StringComparison.Ordinal)), Is.EqualTo(new[] { expected }));
            Assert.That(Keywords(result).Contains("VFX_SIX_WAY_ABSORPTION"), Is.EqualTo(mode == 4));
        }

        [TestCase("_DistortMode", "_DISTORT_REFRACTION", "_noisemapEnabled", TestName = "G4NormalizedReader_DependentEnumModes_UseSharedDependencyCore_01")]
        [TestCase("_RampColorSourceMode", "_COLOR_RAMP_MAP", "_RampColorToggle", TestName = "G4NormalizedReader_DependentEnumModes_UseSharedDependencyCore_02")]
        [TestCase("_DissolveRampSourceMode", "_DISSOLVE_RAMP_MAP", "_Dissolve_Toggle", TestName = "G4NormalizedReader_DependentEnumModes_UseSharedDependencyCore_03")]
        public void DependentEnumModes_UseSharedDependencyCore(string property, string keyword, string parent)
        {
            var material = ActualGraph(); ClearToggles(material); material.SetFloat("_Dissolve_useRampMap_Toggle", 1f);
            for (int mode = 0; mode <= 1; ++mode)
                for (int parentOn = 0; parentOn <= 1; ++parentOn)
                {
                    material.SetFloat(property, mode); material.SetFloat(parent, parentOn);
                    Assert.That(Read(material, 3, null, out var result, out _), Is.True);
                    Assert.That(Keywords(result).Contains(keyword), Is.EqualTo(mode == 1 && parentOn == 1));
                }
        }

        static IEnumerable<TestCaseData> VatCases()
        {
            string[] houdini = { "_HOUDINI_VAT_SOFTBODY", "_HOUDINI_VAT_RIGIDBODY", "_HOUDINI_VAT_DYNAMIC_REMESH", "_HOUDINI_VAT_PARTICLE_SPRITE" };
            string[] tyflow = { "_TYFLOW_VAT_ABSOLUTE", "_TYFLOW_VAT_RELATIVE", "_TYFLOW_VAT_SKIN_R", "_TYFLOW_VAT_SKIN_PR", "_TYFLOW_VAT_SKIN_PRSAVE", "_TYFLOW_VAT_SKIN_PRSXYZ" };
            for (int mode = 0; mode < 2; ++mode)
                for (int sub = 0; sub < (mode == 0 ? houdini.Length : tyflow.Length); ++sub)
                    yield return new TestCaseData(mode, sub, mode == 0 ? houdini[sub] : tyflow[sub]).SetName("G4NormalizedReader_VAT_m" + mode + "_s" + sub);
        }

        [TestCaseSource(nameof(VatCases))]
        public void AllTenVATEnums_RetainLegacyDependencyAndFlipbookExclusion(int mode, int sub, string keyword)
        {
            var material = ActualGraph(); ClearToggles(material); material.SetFloat("_VAT_Toggle", 1f);
            material.SetFloat("_FlipbookBlending", 1f); material.SetFloat("_VATMode", mode);
            material.SetFloat(mode == 0 ? "_HoudiniVATSubMode" : "_TyFlowVATSubMode", sub);
            string before = Snapshot(material);
            Assert.That(Read(material, 3, null, out var result, out _), Is.True);
            Assert.That(Keywords(result), Does.Contain(keyword)); Assert.That(Keywords(result), Does.Contain("_VAT"));
            Assert.That(Keywords(result), Does.Not.Contain("_FLIPBOOKBLENDING_ON"));
            Assert.That(Read(material, 0, new[] { keyword }, out result, out _), Is.True);
            Assert.That(Keywords(result), Is.Empty, "Orphan VAT family/submode must be removed by the same dependency core.");
            Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [TestCase("_Surface", -1f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_01")] [TestCase("_Surface", 2f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_02")] [TestCase("_Blend", 4f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_03")]
        [TestCase("_FxLightMode", 1.25f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_04")] [TestCase("_FxLightMode", 5f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_05")]
        [TestCase("_DistortMode", 2f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_06")] [TestCase("_RampColorSourceMode", 2f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_07")]
        [TestCase("_DissolveRampSourceMode", -1f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_08")] [TestCase("_VATMode", 2f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_09")]
        [TestCase("_HoudiniVATSubMode", 4f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_10")] [TestCase("_TyFlowVATSubMode", 6f, TestName = "G4NormalizedReader_UnknownOrFractionalEnum_IsRejectedWithoutRounding_11")]
        public void UnknownOrFractionalEnum_IsRejectedWithoutRounding(string property, float value)
        {
            var material = ActualGraph(); material.SetFloat(property, value); string before = Snapshot(material);
            Assert.That(Read(material, 3, null, out var result, out _), Is.False);
            Assert.That(result, Is.Null); Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [Test]
        public void AllowedSetFiltering_DoesNotEraseIntentOrEnableUnavailableFeatures()
        {
            var material = ActualGraph(); ClearToggles(material);
            material.SetFloat("_Mask2_Toggle", 1f); material.SetFloat("_noiseMaskMap_Toggle", 1f);
            string before = Snapshot(material);
            Assert.That(Read(material, 0, new[] { "_MASKMAP2_ON", "_NOISE_MASKMAP", "_SPECULAR_COLOR", "CATALOG_EXTERNAL" }, out var result, out var missing), Is.True);
            Assert.That(Keywords(result), Is.Empty);
            Assert.That(Keywords(result, "intendedManagedKeywords"), Does.Contain("_MASKMAP2_ON"));
            Assert.That(Keywords(result, "intendedManagedKeywords"), Does.Contain("_NOISE_MASKMAP"));
            Assert.That(Keywords(result, "strippedManagedKeywords"), Does.Contain("_MASKMAP2_ON"));
            Assert.That(Keywords(result, "strippedManagedKeywords"), Does.Not.Contain("_SPECULAR_COLOR"));
            Assert.That(missing, Is.EqualTo(MissingKeywords)); Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [TestCase("_SoftParticlesEnabled", "_SOFTPARTICLES_ON", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_01")]
        [TestCase("_DistanceFade_Toggle", "_DISTANCE_FADE", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_02")]
        [TestCase("_Mask_Toggle", "_MASKMAP_ON", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_03")]
        [TestCase("_noisemapEnabled", "_NOISEMAP", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_04")]
        [TestCase("_EmissionEnabled", "_EMISSION", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_05")]
        [TestCase("_ColorBlendMap_Toggle", "_COLORMAPBLEND", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_06")]
        [TestCase("_RampColorToggle", "_COLOR_RAMP", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_07")]
        [TestCase("_Dissolve_Toggle", "_DISSOLVE", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_08")]
        [TestCase("_ProgramNoise_Toggle", "_PROGRAM_NOISE", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_09")]
        [TestCase("_fresnelEnabled", "_FRESNEL", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_10")]
        [TestCase("_ParallaxMapping_Toggle", "_PARALLAX_MAPPING", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_11")]
        [TestCase("_VertexOffset_Toggle", "_VERTEX_OFFSET", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_12")]
        [TestCase("_FlipbookBlending", "_FLIPBOOKBLENDING_ON", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_13")]
        [TestCase("_BumpMapToggle", "_NORMALMAP", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_14")]
        [TestCase("_MatCapToggle", "_MATCAP", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_15")]
        [TestCase("_SixWayColorAbsorptionToggle", "VFX_SIX_WAY_ABSORPTION", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_16")]
        [TestCase("_DepthDecal_Toggle", "_DEPTH_DECAL", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_17")]
        [TestCase("_DepthOutline_Toggle", "_DEPTH_OUTLINE", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_18")]
        [TestCase("_OverrideZ_Toggle", "_OVERRIDE_Z", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_19")]
        [TestCase("_Mask2_Toggle", "_MASKMAP2_ON", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_20")]
        [TestCase("_Mask3_Toggle", "_MASKMAP3_ON", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_21")]
        [TestCase("_noiseMaskMap_Toggle", "_NOISE_MASKMAP", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_22")]
        [TestCase("_Distortion_Choraticaberrat_Toggle", "_CHROMATIC_ABERRATION", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_23")]
        [TestCase("_DissolveMask_Toggle", "_DISSOLVE_MASK", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_24")]
        [TestCase("_Dissolve_useRampMap_Toggle", "_DISSOLVE_RAMP", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_25")]
        [TestCase("_ProgramNoise_Simple_Toggle", "_PROGRAM_NOISE_SIMPLE", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_26")]
        [TestCase("_ProgramNoise_Voronoi_Toggle", "_PROGRAM_NOISE_VORONOI", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_27")]
        [TestCase("_VertexOffset_Mask_Toggle", "_VERTEX_OFFSET_MASKMAP", TestName = "G4NormalizedReader_EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold_28")]
        public void EverySupportedToggle_ReadsItsExistingFloatIntentAtOriginalThreshold(string property, string keyword)
        {
            var material = ActualGraph(); ClearToggles(material);
            foreach (float value in new[] { 0.5f, 1f })
            {
                material.SetFloat(property, value); string before = Snapshot(material);
                Assert.That(Read(material, 3, null, out var result, out var missing), Is.True);
                Assert.That(Keywords(result, "intendedManagedKeywords").Contains(keyword), Is.EqualTo(value > 0.5f));
                Assert.That(missing, Is.EqualTo(MissingKeywords));
                Assert.That(Snapshot(material), Is.EqualTo(before));
            }
        }

        // Real Shader + Material schema probe in memory. This validates property
        // types/missing inputs without changing the central Graph asset.
        Material SchemaProbe(string omitted, string integerProperty = null)
        {
            var reference = ActualGraph(); Shader graph = reference.shader;
            var source = new StringBuilder("Shader \"Hidden/NBFX/NormalizedReaderSchema\" { Properties {\n");
            for (int i = 0; i < graph.GetPropertyCount(); ++i)
            {
                string name = graph.GetPropertyName(i); if (name == omitted) continue;
                if (graph.GetPropertyType(i) != ShaderPropertyType.Float) continue;
                source.Append(name).Append("(\"").Append(name).Append("\",").Append(name == integerProperty ? "Integer" : "Float").Append(")=")
                    .Append(name == Version ? "2" : "0").Append('\n');
            }
            source.Append("} SubShader { Pass { } } }");
            var shader = ShaderUtil.CreateShaderAsset(source.ToString(), false);
            Assert.That(shader, Is.Not.Null); shader.hideFlags = HideFlags.HideAndDontSave; owned.Add(shader);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material); return material;
        }

        [TestCase("_NB_Flags0Lo16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_01")] [TestCase("_NB_Flags1Hi16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_02")]
        [TestCase("_NB_WrapFlagsLo16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_03")] [TestCase("_NB_ColorChannelHi16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_04")]
        [TestCase("_NB_PNoiseBlendLo16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_05")] [TestCase("_NB_ForceNoMipFlagsHi16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_06")]
        [TestCase("_NB_UVModeFlag0Lo16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_07")] [TestCase("_NB_UVModeFlagType0Hi16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_08")]
        [TestCase("_NB_CustomDataFlag0Lo16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_09")] [TestCase("_NB_CustomDataFlag1Hi16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_10")]
        [TestCase("_NB_CustomDataFlag2Lo16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_11")] [TestCase("_NB_CustomDataFlag3Hi16", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_12")]
        [TestCase("_noisemapEnabled", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_13")] [TestCase("_OverrideZ_Toggle", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_14")]
        [TestCase("_FxLightMode", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_15")] [TestCase("_Surface", TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_16")] [TestCase(Version, TestName = "G4NormalizedReader_MissingRequiredRealSchema_IsRejected_17")]
        public void MissingRequiredRealSchema_IsRejected(string property)
        {
            var material = SchemaProbe(property); string before = Snapshot(material);
            Assert.That(Read(material, 3, null, out var result, out _), Is.False);
            Assert.That(result, Is.Null); Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [TestCase("_NB_Flags0Lo16", TestName = "G4NormalizedReader_IntegerPropertyCannotMasqueradeAsGraphFloat_01")] [TestCase("_NB_CustomDataFlag3Hi16", TestName = "G4NormalizedReader_IntegerPropertyCannotMasqueradeAsGraphFloat_02")]
        [TestCase("_noiseMaskMap_Toggle", TestName = "G4NormalizedReader_IntegerPropertyCannotMasqueradeAsGraphFloat_03")] [TestCase("_VATMode", TestName = "G4NormalizedReader_IntegerPropertyCannotMasqueradeAsGraphFloat_04")] [TestCase(Version, TestName = "G4NormalizedReader_IntegerPropertyCannotMasqueradeAsGraphFloat_05")]
        public void IntegerPropertyCannotMasqueradeAsGraphFloat(string property)
        {
            var material = SchemaProbe(null, property); string before = Snapshot(material);
            Assert.That(Read(material, 3, null, out var result, out _), Is.False);
            Assert.That(result, Is.Null); Assert.That(Snapshot(material), Is.EqualTo(before));
        }

        [Test]
        public void ExistingNoisePairAndGeneralTierApplyRemainProtected()
        {
            var material = ActualGraph(); ClearToggles(material);
            material.SetFloat("_noisemapEnabled", 1); material.SetFloat("_noiseMaskMap_Toggle", 1);
            var resolver = FindType("NBShader.NBShaderMaterialIntentResolver");
            object[] pair = { material, new[] { "_NOISE_MASKMAP" }, false, false };
            Assert.That(resolver.GetMethod("TryResolveGraphNoisePair", Static).Invoke(null, pair), Is.True);
            Assert.That(pair[2], Is.False); Assert.That(pair[3], Is.False);
            string before = Snapshot(material);
            var applier = FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
            var apply = applier.GetMethods(Static).Single(m => m.Name == "Apply" && m.GetParameters().Length == 5);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), 0), true, true, false };
            Assert.That(apply.Invoke(null, args), Is.False); Assert.That(args[4], Is.False);
            Assert.That(Snapshot(material), Is.EqualTo(before), "New reader must not unlock ordinary Apply.");
        }
    }
}
