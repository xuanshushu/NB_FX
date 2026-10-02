using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Real Material/ShaderGUI lifecycle checks. No render, asset import,
    // refresh, save, Play, resolver, or NB Editor assembly reference is needed.
    public sealed class G4GraphOverrideDepthLifecycleTests
    {
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        static readonly string[] NBKeywords =
        {
            "_OVERRIDE_Z", "EVALUATE_SH_VERTEX", "VFX_SIX_WAY_ABSORPTION"
        };
        static readonly string[] SurfaceProperties =
        {
            "_Surface", "_Blend", "_AlphaClip", "_ZWriteControl", "_QueueControl",
            "_QueueOffset", "_SrcBlend", "_DstBlend", "_SrcBlendAlpha",
            "_DstBlendAlpha", "_ZWrite", "_ZTest", "_Cull"
        };
        static readonly string[] PassNames =
        {
            "Universal Forward", "DepthOnly", "ShadowCaster", "DepthNormalsOnly",
            "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "MotionVectors"
        };
        readonly List<Object> owned = new List<Object>();
        Shader graphShader, legacyShader;
        ShaderGUI nbGUI, urpGUI;

        [OneTimeSetUp]
        public void LoadInstalledMaterialLifecycle()
        {
            graphShader = AssetDatabase.LoadAssetAtPath<Shader>(Package + "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
            legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(Package + "NBShaders2/Shader/NBShader.shader");
            Assert.That(graphShader && legacyShader, Is.True, "Use the installed normal Mesh package.");
            nbGUI = CreateGUI("NBShaderEditor.NBShaderGraphGUI");
            urpGUI = CreateGUI("UnityEditor.ShaderGraphUnlitGUI");
        }

        static ShaderGUI CreateGUI(string typeName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName, false)).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, typeName);
            var gui = Activator.CreateInstance(type, true) as ShaderGUI;
            Assert.That(gui, Is.Not.Null, typeName + " must remain a real ShaderGUI.");
            return gui;
        }

        Material Keep(Material material)
        {
            material.hideFlags = HideFlags.HideAndDontSave;
            owned.Add(material);
            return material;
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; --i)
                if (owned[i]) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        static void SetIntent(Material material, bool overrideZ, bool sixWay, bool absorption)
        {
            foreach (var property in new[] { "_OverrideZ_Toggle", "_OverrideZValue", "_FxLightMode", "_SixWayColorAbsorptionToggle" })
                Assert.That(material.HasProperty(property), Is.True, property);
            material.SetFloat("_OverrideZ_Toggle", overrideZ ? 1 : 0);
            material.SetFloat("_OverrideZValue", 7);
            material.SetFloat("_FxLightMode", sixWay ? 4 : 0);
            material.SetFloat("_SixWayColorAbsorptionToggle", absorption ? 1 : 0);
        }

        static void AssertIntent(Material material, bool overrideZ, bool sixWay, bool absorption)
        {
            Assert.That(material.GetFloat("_OverrideZ_Toggle"), Is.EqualTo(overrideZ ? 1 : 0));
            Assert.That(material.GetFloat("_OverrideZValue"), Is.EqualTo(7));
            Assert.That(material.GetFloat("_FxLightMode"), Is.EqualTo(sixWay ? 4 : 0));
            Assert.That(material.GetFloat("_SixWayColorAbsorptionToggle"), Is.EqualTo(absorption ? 1 : 0));
            Assert.That(material.IsKeywordEnabled("_OVERRIDE_Z"), Is.EqualTo(overrideZ));
            Assert.That(material.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.EqualTo(sixWay));
            Assert.That(material.IsKeywordEnabled("VFX_SIX_WAY_ABSORPTION"), Is.EqualTo(sixWay && absorption));
        }

        [TestCase(false, false, false, TestName = "G4OverrideZGUI_assign_off_unlit_absorptionoff")]
        [TestCase(false, false, true, TestName = "G4OverrideZGUI_assign_off_unlit_absorptionon")]
        [TestCase(false, true, false, TestName = "G4OverrideZGUI_assign_off_sixway_absorptionoff")]
        [TestCase(false, true, true, TestName = "G4OverrideZGUI_assign_off_sixway_absorptionon")]
        [TestCase(true, false, false, TestName = "G4OverrideZGUI_assign_on_unlit_absorptionoff")]
        [TestCase(true, false, true, TestName = "G4OverrideZGUI_assign_on_unlit_absorptionon")]
        [TestCase(true, true, false, TestName = "G4OverrideZGUI_assign_on_sixway_absorptionoff")]
        [TestCase(true, true, true, TestName = "G4OverrideZGUI_assign_on_sixway_absorptionon")]
        public void AssignmentRestoresNBKeywordsImmediately(bool overrideZ, bool sixWay, bool absorption)
        {
            var material = Keep(new Material(legacyShader));
            SetIntent(material, overrideZ, sixWay, absorption);
            // Seed opposing keyword state. Assignment must clear it and restore
            // current property intent without a later OnGUI or Validate call.
            foreach (var keyword in NBKeywords) material.EnableKeyword(keyword);
            nbGUI.AssignNewShaderToMaterial(material, legacyShader, graphShader);
            Assert.That(material.shader, Is.EqualTo(graphShader));
            AssertIntent(material, overrideZ, sixWay, absorption);
        }

        [TestCase(false, false, TestName = "G4OverrideZGUI_validate_off_unlit_transitions")]
        [TestCase(false, true, TestName = "G4OverrideZGUI_validate_off_sixway_transitions")]
        [TestCase(true, false, TestName = "G4OverrideZGUI_validate_on_unlit_transitions")]
        [TestCase(true, true, TestName = "G4OverrideZGUI_validate_on_sixway_transitions")]
        public void ValidateRepairsStateAndHandlesTransitions(bool overrideZ, bool sixWay)
        {
            var material = Keep(new Material(graphShader));
            SetIntent(material, overrideZ, sixWay, true);
            foreach (var keyword in NBKeywords)
                if (overrideZ) material.DisableKeyword(keyword); else material.EnableKeyword(keyword);
            nbGUI.ValidateMaterial(material);
            AssertIntent(material, overrideZ, sixWay, true);
            SetIntent(material, !overrideZ, !sixWay, false);
            nbGUI.ValidateMaterial(material);
            AssertIntent(material, !overrideZ, !sixWay, false);
            SetIntent(material, overrideZ, sixWay, true);
            nbGUI.ValidateMaterial(material);
            AssertIntent(material, overrideZ, sixWay, true);
        }

        [Test]
        public void G4OverrideZGUI_multi_material_independent()
        {
            var first = Keep(new Material(graphShader));
            var second = Keep(new Material(graphShader));
            SetIntent(first, true, true, true);
            SetIntent(second, false, false, false);
            nbGUI.ValidateMaterial(first);
            nbGUI.ValidateMaterial(second);
            AssertIntent(first, true, true, true);
            AssertIntent(second, false, false, false);
            string secondBefore = EditorJsonUtility.ToJson(second);
            SetIntent(first, false, true, false);
            nbGUI.ValidateMaterial(first);
            AssertIntent(first, false, true, false);
            Assert.That(EditorJsonUtility.ToJson(second), Is.EqualTo(secondBefore));
            string firstBefore = EditorJsonUtility.ToJson(first);
            SetIntent(second, true, false, true);
            nbGUI.ValidateMaterial(second);
            AssertIntent(second, true, false, true);
            Assert.That(EditorJsonUtility.ToJson(first), Is.EqualTo(firstBefore));
        }

        [TestCase(false, TestName = "G4OverrideZGUI_validate_noop_off_serialized")]
        [TestCase(true, TestName = "G4OverrideZGUI_validate_noop_on_serialized")]
        public void RepeatedValidatePreservesSerializedState(bool enabled)
        {
            var material = Keep(new Material(graphShader));
            SetIntent(material, enabled, enabled, enabled);
            nbGUI.ValidateMaterial(material);
            string before = EditorJsonUtility.ToJson(material);
            for (int i = 0; i < 3; ++i) nbGUI.ValidateMaterial(material);
            AssertIntent(material, enabled, enabled, enabled);
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(before));
        }

        [TestCase(false, TestName = "G4OverrideZGUI_urp_surface_opaque")]
        [TestCase(true, TestName = "G4OverrideZGUI_urp_surface_transparent")]
        public void NBValidationKeepsURPSurfaceOwnership(bool transparent)
        {
            var actual = Keep(new Material(graphShader));
            Assert.That(actual.HasProperty("_Surface"), Is.True);
            actual.SetFloat("_Surface", transparent ? 1 : 0);
            if (actual.HasProperty("_Blend")) actual.SetFloat("_Blend", 1);
            if (actual.HasProperty("_AlphaClip")) actual.SetFloat("_AlphaClip", 1);
            if (actual.HasProperty("_QueueOffset")) actual.SetFloat("_QueueOffset", 13);
            SetIntent(actual, true, true, true);
            var expected = Keep(new Material(actual));
            urpGUI.ValidateMaterial(expected);
            nbGUI.ValidateMaterial(actual);
            AssertIntent(actual, true, true, true);
            Assert.That(actual.renderQueue, Is.EqualTo(expected.renderQueue));
            foreach (var property in SurfaceProperties)
            {
                Assert.That(actual.HasProperty(property), Is.EqualTo(expected.HasProperty(property)));
                if (actual.HasProperty(property))
                    Assert.That(actual.GetFloat(property), Is.EqualTo(expected.GetFloat(property)), property);
            }
            foreach (var passName in PassNames)
                Assert.That(actual.GetShaderPassEnabled(passName), Is.EqualTo(expected.GetShaderPassEnabled(passName)), passName);
            Assert.That(actual.shaderKeywords.Where(k => !NBKeywords.Contains(k)).OrderBy(k => k),
                Is.EqualTo(expected.shaderKeywords.Where(k => !NBKeywords.Contains(k)).OrderBy(k => k)));
            Assert.That(actual.GetTag("RenderType", false), Is.EqualTo(expected.GetTag("RenderType", false)));
            Assert.That(actual.enableInstancing, Is.EqualTo(expected.enableInstancing));
            Assert.That(actual.doubleSidedGI, Is.EqualTo(expected.doubleSidedGI));
        }
    }
}
