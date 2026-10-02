using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Real existing Component + Renderer lifecycle, without hand-writing its
    // helper bit, frame ST, next ST, or blend intensity. No render/Player claim.
    public sealed class G4GraphFlipbookHelperLifecycleTests
    {
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        const uint HelperBit = 1u << 15;
        const uint Word0Seed = 0x9182a083u; // UI bit14 is off.
        const uint Word1Seed = 0xca400a51u; // Helper bit15 is off.
        static readonly Vector4[] Frames = {
            new Vector4(.5f,.5f,0,.5f), new Vector4(.5f,.5f,.5f,.5f),
            new Vector4(.5f,.5f,0,0), new Vector4(.5f,.5f,.5f,0)
        };
        readonly List<Object> owned = new List<Object>();
        readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        Type helperType;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Serializable] sealed class State
        {
            public string role, stage, scope;
            public uint flags0, flags1;
            public Vector4 currentST, nextST;
            public float blend;
        }

        [OneTimeSetUp]
        public void WarmInstalledCombinedGraph()
        {
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            shaders["frozen"] = AssetDatabase.LoadAssetAtPath<Shader>(Package + "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");
            shaders["current"] = AssetDatabase.LoadAssetAtPath<Shader>(Package + "NBShaders2/Shader/NBShader.shader");
            shaders["graph"] = AssetDatabase.LoadAssetAtPath<Shader>(Package + "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
            foreach (var shader in shaders.Values) Assert.That(shader && shader.isSupported, Is.True);
            helperType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AnimationSheetHelper", false)).FirstOrDefault(t => t != null);
            Assert.That(helperType, Is.Not.Null, "Use the existing AnimationSheetHelper Runtime type.");
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; --i) if (owned[i]) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        T Keep<T>(T item) where T : Object { item.hideFlags = HideFlags.HideAndDontSave; owned.Add(item); return item; }
        void Set(Component helper, string field, object value) => helperType.GetField(field, Instance).SetValue(helper, value);
        T Field<T>(Component helper, string field) => (T)helperType.GetField(field, Instance).GetValue(helper);
        void Init(Component helper) => helperType.GetMethod("Init", Instance).Invoke(helper, null);
        void Tick(Component helper) => helperType.GetMethod("Update", Instance).Invoke(helper, null);

        static int LegacyId(Material material, int word)
        {
            string alias = word == 0 ? "_NBShaderFlags" : "_NBShaderFlags1";
            return Shader.PropertyToID(material.HasProperty(alias) ? alias : word == 0 ? "_W9ParticleShaderFlags" : "_W9ParticleShaderFlags1");
        }
        static uint ReadWord(Material material, string role, int word)
        {
            if (role != "graph") return unchecked((uint)material.GetInteger(LegacyId(material, word)));
            return (uint)material.GetFloat("_NB_Flags" + word + "Lo16") |
                ((uint)material.GetFloat("_NB_Flags" + word + "Hi16") << 16);
        }
        static void SeedWord(Material material, string role, int word, uint value)
        {
            if (role != "graph") material.SetInteger(LegacyId(material, word), unchecked((int)value));
            else {
                material.SetFloat("_NB_Flags" + word + "Lo16", value & 65535u);
                material.SetFloat("_NB_Flags" + word + "Hi16", value >> 16);
            }
        }
        Material NewMaterial(string role)
        {
            var material = Keep(new Material(shaders[role]));
            if (role == "graph")
                foreach (string name in new[] { "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16", "_BaseMap_ST", "_BaseMap_AnimationSheetBlend_ST", "_AnimationSheetHelperBlendIntensity" })
                    Assert.That(material.HasProperty(name), Is.True, "Restore the combined F0 Graph; missing " + name);
            // Only unrelated protocol words are seeded. The Component owns bit15.
            SeedWord(material, role, 0, Word0Seed);
            SeedWord(material, role, 1, Word1Seed);
            return material;
        }
        Behaviour Actor(Material material, float position, out MeshRenderer renderer)
        {
            var go = Keep(new GameObject("Real AnimationSheetHelper lifecycle", typeof(MeshFilter), typeof(MeshRenderer)));
            go.SetActive(false);
            renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            var helper = go.AddComponent(helperType) as Behaviour;
            Assert.That(helper, Is.Not.Null);
            Set(helper, "xSize", 2); Set(helper, "ySize", 2);
            Set(helper, "manualPlay", true); Set(helper, "manualPlayePos", position);
            go.SetActive(true); // Unity invokes the real OnEnable/Init.
            return helper;
        }
        static void Flags(Material material, string role, bool enabled)
        {
            Assert.That(ReadWord(material, role, 0), Is.EqualTo(Word0Seed));
            Assert.That(ReadWord(material, role, 1), Is.EqualTo(Word1Seed | (enabled ? HelperBit : 0u)));
        }
        static bool Finite(Vector4 v) => new[] { v.x, v.y, v.z, v.w }.All(f => !float.IsNaN(f) && !float.IsInfinity(f));
        void Frame(Behaviour helper, Material material, int current, int next, float blend)
        {
            Assert.That(Field<int>(helper, "frameIndex"), Is.EqualTo(current));
            Assert.That(material.GetVector("_BaseMap_ST"), Is.EqualTo(Frames[current]));
            Assert.That(material.GetVector("_BaseMap_AnimationSheetBlend_ST"), Is.EqualTo(Frames[next]));
            Assert.That(material.GetFloat("_AnimationSheetHelperBlendIntensity"), Is.EqualTo(blend));
            Assert.That(Finite(material.GetVector("_BaseMap_ST")) && Finite(material.GetVector("_BaseMap_AnimationSheetBlend_ST")), Is.True);
        }
        static void Record(Material material, string role, string stage)
        {
            var state = new State { role = role, stage = stage, scope = "Real Component writes/flag lifecycle only. No rendered-image/Pass/Player/performance claim.",
                flags0 = ReadWord(material, role, 0), flags1 = ReadWord(material, role, 1),
                currentST = material.GetVector("_BaseMap_ST"), nextST = material.GetVector("_BaseMap_AnimationSheetBlend_ST"), blend = material.GetFloat("_AnimationSheetHelperBlendIntensity") };
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXFlipbookHelper");
            string folder = Path.Combine(root, "flipbook-helper-lifecycle"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, role + "-" + stage + ".json"), JsonUtility.ToJson(state, true));
        }

        [TestCase("frozen", TestName = "G4FlipbookHelper_init_frozen")]
        [TestCase("current", TestName = "G4FlipbookHelper_init_current")]
        [TestCase("graph", TestName = "G4FlipbookHelper_init_graph")]
        public void InitAndReinitProvideFirstAndNextFrames(string role)
        {
            var material = NewMaterial(role); var helper = Actor(material, .125f, out _);
            Flags(material, role, true); Frame(helper, material, 0, 1, 0);
            Tick(helper); Frame(helper, material, 0, 1, .5f);
            Set(helper, "manualPlayePos", .625f); Tick(helper); Frame(helper, material, 2, 3, .5f);
            Init(helper); Frame(helper, material, 0, 1, 0);
            Tick(helper); Frame(helper, material, 2, 3, .5f);
            Record(material, role, "init-reinit");
        }

        [TestCase("frozen", TestName = "G4FlipbookHelper_wrap_frozen")]
        [TestCase("current", TestName = "G4FlipbookHelper_wrap_current")]
        [TestCase("graph", TestName = "G4FlipbookHelper_wrap_graph")]
        public void LastFrameWrapAndSingleFrameRemainFinite(string role)
        {
            var material = NewMaterial(role); var helper = Actor(material, .875f, out _);
            Tick(helper); Frame(helper, material, 3, 0, .5f);
            Set(helper, "manualPlayePos", 1.125f); Tick(helper); Frame(helper, material, 0, 1, .5f);
            Set(helper, "manualPlayePos", -.125f); Tick(helper); Frame(helper, material, 3, 0, .5f);
            Set(helper, "xSize", 0); Set(helper, "ySize", -3); Init(helper);
            Assert.That(Field<int>(helper, "xSize"), Is.EqualTo(1)); Assert.That(Field<int>(helper, "ySize"), Is.EqualTo(1));
            Assert.That(Field<int>(helper, "frameCount"), Is.EqualTo(1));
            Assert.That(material.GetVector("_BaseMap_ST"), Is.EqualTo(new Vector4(1, 1, 0, 0)));
            Assert.That(material.GetVector("_BaseMap_AnimationSheetBlend_ST"), Is.EqualTo(new Vector4(1, 1, 0, 0)));
            Set(helper, "manualPlayePos", .75f); Tick(helper);
            Assert.That(Field<int>(helper, "frameIndex"), Is.Zero);
            Assert.That(material.GetVector("_BaseMap_AnimationSheetBlend_ST"), Is.EqualTo(new Vector4(1, 1, 0, 0)));
            Assert.That(material.GetFloat("_AnimationSheetHelperBlendIntensity"), Is.EqualTo(.75f));
            Flags(material, role, true); Record(material, role, "wrap-single");
        }

        [TestCase("frozen", TestName = "G4FlipbookHelper_disable_destroy_frozen")]
        [TestCase("current", TestName = "G4FlipbookHelper_disable_destroy_current")]
        [TestCase("graph", TestName = "G4FlipbookHelper_disable_destroy_graph")]
        public void DisableReenableAndDestroyClearOnlyHelperBit(string role)
        {
            var material = NewMaterial(role); var helper = Actor(material, .875f, out _);
            string[] keywords = material.shaderKeywords.OrderBy(k => k).ToArray(); int queue = material.renderQueue;
            Tick(helper); Frame(helper, material, 3, 0, .5f);
            helper.enabled = false; Flags(material, role, false);
            Assert.That(material.GetVector("_BaseMap_ST"), Is.EqualTo(Frames[3]), "Disable must not Init/reset frame ST.");
            Init(helper); Flags(material, role, false); // Disabled Renderer does not reacquire the flag.
            helper.enabled = true; Flags(material, role, true); Frame(helper, material, 0, 1, 0);
            Tick(helper); Frame(helper, material, 3, 0, .5f);
            Object.DestroyImmediate(helper); Flags(material, role, false);
            Assert.That(material.GetVector("_BaseMap_ST"), Is.EqualTo(Frames[3]));
            Assert.That(material.shaderKeywords.OrderBy(k => k), Is.EqualTo(keywords)); Assert.That(material.renderQueue, Is.EqualTo(queue));
            Record(material, role, "disable-destroy");
        }

        [TestCase("frozen", TestName = "G4FlipbookHelper_binding_frozen")]
        [TestCase("current", TestName = "G4FlipbookHelper_binding_current")]
        [TestCase("graph", TestName = "G4FlipbookHelper_binding_graph")]
        public void MaterialSwitchNullAndDisableUseOriginalBinding(string role)
        {
            var first = NewMaterial(role); var second = NewMaterial(role); var helper = Actor(first, .625f, out var renderer);
            Tick(helper); Frame(helper, first, 2, 3, .5f); Flags(first, role, true);
            string secondBefore = EditorJsonUtility.ToJson(second);
            renderer.sharedMaterial = second; helper.enabled = false;
            Flags(first, role, false); Assert.That(EditorJsonUtility.ToJson(second), Is.EqualTo(secondBefore), "Disable must not touch newly assigned renderer material.");
            helper.enabled = true; Assert.That(Field<Material>(helper, "mat"), Is.EqualTo(second)); Tick(helper);
            Flags(second, role, true); Frame(helper, second, 2, 3, .5f);
            renderer.sharedMaterial = first; Tick(helper);
            Flags(second, role, false); Flags(first, role, true); Frame(helper, first, 2, 3, .5f);
            renderer.sharedMaterial = null; Assert.DoesNotThrow(() => Tick(helper));
            Assert.That(Field<Material>(helper, "mat"), Is.Null); Flags(first, role, false); Flags(second, role, false);
            Record(first, role, "binding-null-first"); Record(second, role, "binding-null-second");
        }

        [TestCase("frozen", TestName = "G4FlipbookHelper_multirenderer_frozen")]
        [TestCase("current", TestName = "G4FlipbookHelper_multirenderer_current")]
        [TestCase("graph", TestName = "G4FlipbookHelper_multirenderer_graph")]
        public void DistinctMaterialRenderersKeepIndependentFramesAndFlags(string role)
        {
            var first = NewMaterial(role); var second = NewMaterial(role);
            var firstHelper = Actor(first, .125f, out _); var secondHelper = Actor(second, .875f, out _);
            Tick(firstHelper); Tick(secondHelper); Frame(firstHelper, first, 0, 1, .5f); Frame(secondHelper, second, 3, 0, .5f);
            Flags(first, role, true); Flags(second, role, true);
            string secondBefore = EditorJsonUtility.ToJson(second);
            firstHelper.enabled = false; Flags(first, role, false); Flags(second, role, true);
            Assert.That(EditorJsonUtility.ToJson(second), Is.EqualTo(secondBefore));
            Object.DestroyImmediate(secondHelper); Flags(second, role, false);
            Record(first, role, "distinct-first"); Record(second, role, "distinct-second");
        }

        [Test]
        public void G4FlipbookHelper_no_renderer_null_lifecycle()
        {
            var go = Keep(new GameObject("Missing AnimationSheetHelper Renderer"));
            var helper = go.AddComponent(helperType) as Behaviour; Assert.That(helper, Is.Not.Null);
            Assert.That(Field<Material>(helper, "mat"), Is.Null); Assert.DoesNotThrow(() => Tick(helper));
            Assert.DoesNotThrow(() => { helper.enabled = false; helper.enabled = true; Tick(helper); Object.DestroyImmediate(helper); });
        }
    }
}
