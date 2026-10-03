using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Four real Material boundary controls for the new normalized pair API.
    // These are CPU/schema tests, not GPU/Controller/Tier equivalence tests.
    public sealed class G4RootNoiseBoundaryTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string Version = "_NB_GraphGUIStateVersion";
        const string NoiseGate = "_NB_TierAllowNoise";
        const string MaskGate = "_NB_TierAllowNoiseMask";
        static readonly string[] Gates = { NoiseGate, MaskGate };
        static readonly string[] FullNoisePair = { "_NOISEMAP", "_NOISE_MASKMAP" };
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<Object> owned = new List<Object>();

        static Type FindType(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, name); return type;
        }
        static object Invoke(MethodInfo method, object target, object[] args)
        {
            Assert.That(method, Is.Not.Null);
            try { return method.Invoke(target, args); }
            catch (TargetInvocationException error)
            { ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
        }
        static bool Apply(Material material, string[] allowed, out bool changed)
        {
            var method = FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphNoisePair", Static);
            Assert.That(method, Is.Not.Null); Assert.That(method.GetParameters().Length, Is.EqualTo(4));
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), 3), allowed, false };
            bool accepted = (bool)Invoke(method, null, args); changed = (bool)args[3]; return accepted;
        }
        static void AssertNormalizedReaderAcceptsWithoutMutation(Material material)
        {
            var before = Snapshot.Read(material);
            var method = FindType("NBShader.NBShaderMaterialIntentResolver").GetMethod("TryResolveGraphSupportedKeywordIntent", Static);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), 3), FullNoisePair, null, null };
            Assert.That((bool)Invoke(method, null, args), Is.True, "Negative gate control must have an otherwise accepted real Graph schema.");
            Assert.That(args[3], Is.Not.Null);
            Assert.That((string[])args[4], Has.Length.EqualTo(9));
            before.AssertSame(material, "Normalized reader accepts schema without any writes");
        }
        sealed class Snapshot
        {
            readonly object value;
            Snapshot(object value) { this.value = value; }
            static Type Shared
            {
                get
                {
                    var type = typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot", BindingFlags.NonPublic);
                    Assert.That(type, Is.Not.Null); return type;
                }
            }
            public static Snapshot Read(Material material)
                => new Snapshot(Invoke(Shared.GetMethod("Read", Static), null, new object[] { material }));
            public void AssertSame(Material material, string label, params string[] allowed)
                => Invoke(Shared.GetMethod("AssertSame", Instance), value, new object[] { material, label, allowed });
        }
        Material NewMaterial(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path); Assert.That(shader, Is.Not.Null, path);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material); return material;
        }
        [TearDown] public void Cleanup()
        { foreach (Object item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear(); }

        [Test]
        public void G4RootNoiseBoundary_RestoreHighAfterLow_FromNoncanonicalDerivedValues_PreservesDenseIntent()
        {
            var graph = NewMaterial(GraphPath); graph.SetFloat(Version, 2f);
            Invoke(typeof(G4GraphGuiFeatureIntentTests).GetMethod("SeedDense", Static), null,
                new object[] { graph, 0xA569C39Eu, 0xC59347A1u, true });
            graph.SetFloat("_noisemapEnabled", 1f); graph.SetFloat("_noiseMaskMap_Toggle", 1f);
            graph.SetFloat(NoiseGate, -.375f); graph.SetFloat(MaskGate, .25f);
            var original = Snapshot.Read(graph); bool changed;
            Assert.That(Apply(graph, FullNoisePair, out changed), Is.True); Assert.That(changed, Is.True);
            Assert.That(graph.GetFloat(NoiseGate), Is.EqualTo(1)); Assert.That(graph.GetFloat(MaskGate), Is.EqualTo(1));
            original.AssertSame(graph, "First normalization changes only two derived values", Gates);
            var normalized = Snapshot.Read(graph);
            Assert.That(Apply(graph, Array.Empty<string>(), out changed), Is.True); Assert.That(changed, Is.True);
            Assert.That(graph.GetFloat(NoiseGate), Is.Zero); Assert.That(graph.GetFloat(MaskGate), Is.Zero);
            original.AssertSame(graph, "Low preserves complete serialized intent", Gates);
            Assert.That(Apply(graph, FullNoisePair, out changed), Is.True); Assert.That(changed, Is.True);
            Assert.That(graph.GetFloat(NoiseGate), Is.EqualTo(1)); Assert.That(graph.GetFloat(MaskGate), Is.EqualTo(1));
            normalized.AssertSame(graph, "High-low-high restores complete normalized Material state");
            original.AssertSame(graph, "Dense raw halves and original serialized intent remain untouched", Gates);
            Assert.That(Apply(graph, FullNoisePair, out changed), Is.True); Assert.That(changed, Is.False);
            normalized.AssertSame(graph, "Repeated projection is an exact serialized no-op");
        }

        [Test]
        public void G4RootNoiseBoundary_LegacyMaterial_IsRejectedWithoutMutation()
        {
            var legacy = NewMaterial(LegacyPath); var before = Snapshot.Read(legacy); bool changed;
            Assert.That(Apply(legacy, Array.Empty<string>(), out changed), Is.False); Assert.That(changed, Is.False);
            before.AssertSame(legacy, "Partial Graph projection cannot mutate legacy");
        }

        // Reuse the accepted reader113 real Shader property probe: clone all actual
        // Float fields rather than its older abbreviated schema, which the new
        // normalized reader would reject before any gate-specific check.
        Material SchemaProbe(bool omitMaskGate, bool integerMaskGate)
        {
            var graph = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath); Assert.That(graph, Is.Not.Null);
            var source = new StringBuilder("Shader \"Hidden/NBFX/RootNoiseBoundary" + Guid.NewGuid().ToString("N") + "\" { Properties {\n");
            for (int i = 0; i < graph.GetPropertyCount(); ++i)
            {
                string name = graph.GetPropertyName(i);
                if (graph.GetPropertyType(i) != ShaderPropertyType.Float || (omitMaskGate && name == MaskGate)) continue;
                source.Append(name).Append("(\"").Append(name).Append("\",").Append(integerMaskGate && name == MaskGate ? "Integer" : "Float")
                    .Append(")=").Append(name == Version ? "2" : "0").Append('\n');
            }
            source.Append("} SubShader { Pass { } } }");
            var shader = ShaderUtil.CreateShaderAsset(source.ToString(), false); Assert.That(shader, Is.Not.Null);
            shader.hideFlags = HideFlags.HideAndDontSave; owned.Add(shader);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            material.SetFloat("_noisemapEnabled", 1); material.SetFloat("_noiseMaskMap_Toggle", 1);
            return material;
        }
        void AssertGateSpecificRejection(bool wrongType)
        {
            var valid = SchemaProbe(false, false); AssertNormalizedReaderAcceptsWithoutMutation(valid);
            bool changed; Assert.That(Apply(valid, FullNoisePair, out changed), Is.True);
            Assert.That(valid.GetFloat(NoiseGate), Is.EqualTo(1)); Assert.That(valid.GetFloat(MaskGate), Is.EqualTo(1));
            var invalid = SchemaProbe(!wrongType, wrongType); AssertNormalizedReaderAcceptsWithoutMutation(invalid);
            int maskIndex = invalid.shader.FindPropertyIndex(MaskGate);
            if (wrongType) { Assert.That(maskIndex, Is.GreaterThanOrEqualTo(0)); Assert.That(invalid.shader.GetPropertyType(maskIndex), Is.EqualTo(ShaderPropertyType.Int)); }
            else { Assert.That(invalid.HasProperty(MaskGate), Is.False); Assert.That(maskIndex, Is.LessThan(0)); }
            var before = Snapshot.Read(invalid);
            Assert.That(Apply(invalid, Array.Empty<string>(), out changed), Is.False); Assert.That(changed, Is.False);
            before.AssertSame(invalid, "Incomplete pair capability remains untouched despite an accepted normalized reader schema");
        }
        [Test] public void G4RootNoiseBoundary_MissingMaskGate_IsRejectedWithoutMutation() => AssertGateSpecificRejection(false);
        [Test] public void G4RootNoiseBoundary_IntegerMaskGate_IsRejectedWithoutMutation() => AssertGateSpecificRejection(true);
    }
}
