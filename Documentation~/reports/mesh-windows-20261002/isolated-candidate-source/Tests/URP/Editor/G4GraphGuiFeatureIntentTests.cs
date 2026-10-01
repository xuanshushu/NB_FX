using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// GUI1B seed-only contract. These are REAL Graph Material properties, but
    /// version 2 only mirrors the existing Flags0/1 intent once. It does not
    /// activate shared feature controls, Tier projection, Graph MPB, VFX or Player.
    /// Reflection is TEST-ONLY so the test assembly gains no NB Editor reference.
    /// </summary>
    public sealed class G4GraphGuiFeatureIntentTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string Version = "_NB_GraphGUIStateVersion";
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Words = { "_NB_Flags0", "_NB_Flags1", "_NB_WrapFlags", "_NB_ColorChannel", "_NB_PNoiseBlend", "_NB_ForceNoMipFlags", "_NB_UVModeFlag0", "_NB_UVModeFlagType0" };
        static readonly string[] PassNames = { "Universal Forward", "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" };
        readonly List<Object> owned = new List<Object>();
        Shader graphShader, legacyShader;
        Type rootType, contextType, syncType;
        string assetFolder;

        sealed class Binding
        {
            public string Name;
            public int Bit, Word, EnabledMode;
            public bool Mode;
            public int DisabledMode => EnabledMode == 0 ? 1 : 0;
        }

        [OneTimeSetUp]
        public void WarmImportedGraphInRealUrpCamera()
        {
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader, Is.True);
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            var data = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0] as UniversalRendererData;
            Assert.That(data, Is.Not.Null);
            const int layer = 4;
            Assert.That(data.opaqueLayerMask.value & (1 << layer), Is.Not.Zero);
            Assert.That(data.transparentLayerMask.value & (1 << layer), Is.Not.Zero);
            var nbFeature = data.rendererFeatures.FirstOrDefault(f => f && f.GetType().FullName == "NBShader.NBPostProcess");
            Assert.That(nbFeature, Is.Not.Null);
            bool wasActive = nbFeature.isActive;
            string project = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));
            var protectedPaths = new[] { "ProjectSettings/ProjectSettings.asset", "Assets/UniversalRenderPipelineGlobalSettings.asset", AssetDatabase.GetAssetPath(data) }
                .Select(path => Path.Combine(project, path)).Distinct().ToArray();
            var protectedBytes = protectedPaths.ToDictionary(path => path, File.ReadAllBytes);
            var warm = new Material(graphShader) { hideFlags = HideFlags.HideAndDontSave };
            Scene scene = EditorSceneManager.NewPreviewScene();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("GUI1B real URP Mesh schema warm camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var target = new RenderTexture(8, 8, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var read = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            bool async = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                SceneManager.MoveGameObjectToScene(quad, scene);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                quad.hideFlags = cameraObject.hideFlags = HideFlags.HideAndDontSave;
                quad.layer = layer;
                quad.transform.localScale = new Vector3(2, 2, 1);
                quad.GetComponent<MeshRenderer>().sharedMaterial = warm;
                camera.scene = scene; camera.enabled = false;
                camera.orthographic = true; camera.orthographicSize = 1.5f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20f;
                camera.transform.position = new Vector3(0, 0, -5);
                camera.cullingMask = 1 << layer; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear; camera.allowHDR = true; camera.allowMSAA = false;
                camera.targetTexture = target;
                cameraData.SetRenderer(0); cameraData.renderPostProcessing = false;
                Assert.That(target.Create() && !target.sRGB, Is.True);
                nbFeature.SetActive(false); data.SetDirty();
                for (int i = 0; i < 4; ++i) camera.Render();
                RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, 8, 8), 0, 0, false); read.Apply(false, false);
                Assert.That(warm.FindPass("Universal Forward"), Is.GreaterThanOrEqualTo(0));
                Assert.That(graphShader.isSupported, Is.True);
                Assert.That(AssetDatabase.LoadAllAssetsAtPath(GraphPath).Any(asset => asset &&
                    asset.GetType().FullName == "UnityEditor.Rendering.Universal.ShaderGraph.UniversalMetadata"), Is.True);
                Assert.That(ShaderUtil.GetShaderMessages(graphShader).Any(m =>
                    m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error), Is.False);
            }
            finally
            {
                nbFeature.SetActive(wasActive); data.SetDirty();
                camera.targetTexture = null; ShaderUtil.allowAsyncCompilation = async; RenderTexture.active = previous;
                target.Release();
                foreach (Object item in new Object[] { read, target, warm }) Object.DestroyImmediate(item);
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var entry in protectedBytes)
                    Assert.That(File.ReadAllBytes(entry.Key), Is.EqualTo(entry.Value), "Warmup changed protected asset " + entry.Key);
            }
        }

        [SetUp]
        public void SetupTypes()
        {
            rootType = FindType("NBShaderEditor.NBShaderRootItem");
            contextType = FindType("NBShaderEditor.NBShaderGUIContext");
            syncType = FindType("NBShaderEditor.NBShaderSyncService");
            Assert.That(syncType.GetMethod("GraphFlagIntentSchemaAvailable", Static), Is.Not.Null,
                "Apply the GUI1B seed-only candidate before running this fixture.");
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; --i)
                if (owned[i] && !AssetDatabase.Contains(owned[i])) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            if (assetFolder != null)
            {
                Assert.That(AssetDatabase.DeleteAsset(assetFolder), Is.True);
                assetFolder = null;
            }
        }

        static Type FindType(string name)
        {
            var result = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            Assert.That(result, Is.Not.Null, name); return result;
        }
        static FieldInfo GetField(Type type, string name, BindingFlags flags = Instance)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            Assert.Fail("Missing field " + name); return null;
        }
        static object Call(object target, string name, params object[] args)
        {
            MethodInfo method = null;
            for (Type type = target.GetType(); type != null && method == null; type = type.BaseType)
                method = type.GetMethod(name, Instance | BindingFlags.DeclaredOnly);
            Assert.That(method, Is.Not.Null, target.GetType().FullName + "." + name);
            return method.Invoke(target, args);
        }
        static object Prop(object target, string name) => target.GetType().GetProperty(name, Instance).GetValue(target);
        static int Index(Shader shader, string name)
        {
            for (int i = 0; i < shader.GetPropertyCount(); ++i)
                if (shader.GetPropertyName(i) == name) return i;
            Assert.Fail("Missing real Shader property " + name); return -1;
        }
        List<Binding> Bindings()
        {
            var result = new List<Binding>();
            foreach (var row in new[] { ("ToggleFlagBindings", false), ("ModeFlagBindings", true) })
            {
                var array = (Array)GetField(syncType, row.Item1, Static).GetValue(null);
                foreach (object item in array)
                {
                    Type type = item.GetType();
                    result.Add(new Binding {
                        Name = (string)GetField(type, "propertyName").GetValue(item),
                        Bit = (int)GetField(type, "flagBits").GetValue(item),
                        Word = (int)GetField(type, "flagIndex").GetValue(item),
                        Mode = row.Item2,
                        EnabledMode = row.Item2 ? (int)GetField(type, "enabledMode").GetValue(item) : 1
                    });
                }
            }
            Assert.That(result.Count(x => !x.Mode), Is.EqualTo(22));
            Assert.That(result.Count(x => x.Mode), Is.EqualTo(7));
            Assert.That(result.Select(x => x.Name).Distinct().Count(), Is.EqualTo(29));
            return result;
        }
        Material NewMaterial(bool graph = true)
        {
            var material = new Material(graph ? graphShader : legacyShader) { hideFlags = HideFlags.HideAndDontSave };
            owned.Add(material); return material;
        }
        object NewRoot(bool collectNativeProperties, params Material[] materials)
        {
            var root = Activator.CreateInstance(rootType);
            var editor = Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)) as MaterialEditor;
            Assert.That(editor, Is.Not.Null); owned.Add(editor);
            GetField(rootType, "MatEditor").SetValue(root, editor);
            GetField(rootType, "Shader").SetValue(root, materials[0].shader);
            GetField(rootType, "Mats").SetValue(root, new List<Material>(materials));
            Call(root, "InitFlags", new List<Material>(materials));
            var dictionary = (IDictionary)GetField(rootType, "PropertyInfoDic").GetValue(root);
            Type infoType = FindType("NBShaderEditor.ShaderPropertyInfo");
            foreach (MaterialProperty property in collectNativeProperties
                ? MaterialEditor.GetMaterialProperties(materials.Cast<Object>().ToArray())
                : Array.Empty<MaterialProperty>())
            {
                var info = Activator.CreateInstance(infoType);
                infoType.GetField("Property").SetValue(info, property);
                infoType.GetField("Name").SetValue(info, property.name);
                infoType.GetField("Index").SetValue(info, Index(materials[0].shader, property.name));
                dictionary.Add(property.name, info);
            }
            var context = Activator.CreateInstance(contextType, new[] { root });
            var sync = Activator.CreateInstance(syncType, new[] { root });
            rootType.GetProperty("Context", Instance).SetValue(root, context);
            rootType.GetProperty("SyncService", Instance).SetValue(root, sync);
            Call(context, "Refresh");
            return root;
        }
        object NewRoot(params Material[] materials) => NewRoot(true, materials);
        void StaticSync(params Material[] materials)
        {
            var method = syncType.GetMethods(Static).Single(m => m.Name == "SyncMaterialState" &&
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType != typeof(Material));
            method.Invoke(null, new object[] { new List<Material>(materials) });
        }
        static void WriteWord(Material material, int index, uint word)
        {
            string prefix = Words[index];
            material.SetFloat(prefix + "Lo16", word & 65535u);
            material.SetFloat(prefix + "Hi16", word >> 16);
        }
        static uint ReadWord(Material material, int index)
        {
            string prefix = Words[index];
            uint lo = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix + "Lo16"), 0f, 65535f));
            uint hi = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix + "Hi16"), 0f, 65535f));
            return lo | (hi << 16);
        }
        static int Bits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        static void SeedDense(Material material, uint word0, uint word1, bool fractional = false)
        {
            for (int i = 0; i < Words.Length; ++i)
                WriteWord(material, i, i == 0 ? word0 : i == 1 ? word1 : 0xA569C39Eu ^ ((uint)i * 0x01010101u));
            if (fractional)
            {
                material.SetFloat("_NB_Flags0Lo16", -17.25f);
                material.SetFloat("_NB_Flags0Hi16", 65534.5f);
                material.SetFloat("_NB_Flags1Lo16", 65534.75f);
                material.SetFloat("_NB_Flags1Hi16", .5f);
            }
            material.SetColor("_Color", new Color(2.3f, .17f, .61f, .43f));
            material.SetVector("_BaseMap_ST", new Vector4(-2.25f, 1.375f, .42f, -.31f));
            material.SetFloat("_ProgramNoise_Toggle", 1f);
            material.SetFloat("_noisemapEnabled", 1f);
            material.SetFloat("_MainTexBigBlockItemFoldOut", 0f);
            material.SetFloat("_BaseMapFoldOut", 0f);
            material.renderQueue = 3087;
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.EnableKeyword("EVALUATE_SH_VERTEX");
        }
        static string[] AllowedSeedNames(IEnumerable<Binding> bindings) => bindings.Select(x => x.Name).Concat(new[] { Version }).ToArray();
        static void AssertSeed(Material material, IEnumerable<Binding> bindings)
        {
            uint[] words = { ReadWord(material, 0), ReadWord(material, 1) };
            foreach (Binding binding in bindings)
            {
                bool on = (words[binding.Word] & unchecked((uint)binding.Bit)) != 0;
                int expected = binding.Mode ? (on ? binding.EnabledMode : binding.DisabledMode) : (on ? 1 : 0);
                Assert.That(Bits(material.GetFloat(binding.Name)), Is.EqualTo(Bits(expected)), binding.Name + " word " + binding.Word);
            }
        }
        static void SetMirrorSentinels(Material material, IEnumerable<Binding> bindings)
        {
            foreach (Binding binding in bindings) material.SetFloat(binding.Name, -11.25f);
        }

        sealed class Snapshot
        {
            readonly Dictionary<string, float> floats = new Dictionary<string, float>();
            readonly Dictionary<string, int> ints = new Dictionary<string, int>();
            readonly Dictionary<string, Vector4> vectors = new Dictionary<string, Vector4>();
            readonly Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
            readonly Dictionary<string, bool> passes = new Dictionary<string, bool>();
            string[] keywords;
            string tag;
            int queue;
            bool instancing, doubleSided;
            MaterialGlobalIlluminationFlags gi;
            public static Snapshot Read(Material material)
            {
                var s = new Snapshot { keywords = material.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
                    tag = material.GetTag("RenderType", false, ""), queue = material.renderQueue,
                    instancing = material.enableInstancing, doubleSided = material.doubleSidedGI,
                    gi = material.globalIlluminationFlags };
                for (int i = 0; i < material.shader.GetPropertyCount(); ++i)
                {
                    string name = material.shader.GetPropertyName(i);
                    switch (material.shader.GetPropertyType(i))
                    {
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range: s.floats[name] = material.GetFloat(name); break;
                        case ShaderPropertyType.Int: s.ints[name] = material.GetInteger(name); break;
                        case ShaderPropertyType.Color: s.vectors[name] = material.GetColor(name); break;
                        case ShaderPropertyType.Vector: s.vectors[name] = material.GetVector(name); break;
                        case ShaderPropertyType.Texture:
                            s.textures[name] = material.GetTexture(name);
                            Vector2 scale = material.GetTextureScale(name), offset = material.GetTextureOffset(name);
                            s.vectors[name + ".nativeST"] = new Vector4(scale.x, scale.y, offset.x, offset.y);
                            break;
                    }
                }
                foreach (string pass in PassNames) s.passes[pass] = material.GetShaderPassEnabled(pass);
                return s;
            }
            public void AssertSame(Material material, string label, params string[] allowed)
            {
                Snapshot next = Read(material); var exceptions = new HashSet<string>(allowed);
                Assert.That(next.keywords, Is.EqualTo(keywords), label + " keywords");
                Assert.That(next.tag, Is.EqualTo(tag), label + " tag");
                Assert.That(next.queue, Is.EqualTo(queue), label + " queue");
                Assert.That(next.instancing, Is.EqualTo(instancing), label + " instancing");
                Assert.That(next.doubleSided, Is.EqualTo(doubleSided), label + " double sided");
                Assert.That(next.gi, Is.EqualTo(gi), label + " GI");
                foreach (var row in floats) if (!exceptions.Contains(row.Key))
                    Assert.That(Bits(next.floats[row.Key]), Is.EqualTo(Bits(row.Value)), label + " float " + row.Key);
                foreach (var row in ints) if (!exceptions.Contains(row.Key))
                    Assert.That(next.ints[row.Key], Is.EqualTo(row.Value), label + " int " + row.Key);
                foreach (var row in vectors) if (!exceptions.Contains(row.Key))
                    for (int i = 0; i < 4; ++i)
                        Assert.That(Bits(next.vectors[row.Key][i]), Is.EqualTo(Bits(row.Value[i])), label + " vector " + row.Key + "." + i);
                foreach (var row in textures) if (!exceptions.Contains(row.Key))
                    Assert.That(next.textures[row.Key], Is.EqualTo(row.Value), label + " texture " + row.Key);
                foreach (var row in passes) Assert.That(next.passes[row.Key], Is.EqualTo(row.Value), label + " pass " + row.Key);
            }
        }

        [Test]
        public void GUI1B_RealImportedSchema_Has29DistinctHiddenFloatMirrorsAndVersion()
        {
            var bindings = Bindings(); var material = NewMaterial();
            foreach (Binding binding in bindings)
            {
                Assert.That(material.HasProperty(binding.Name), Is.True, binding.Name);
                int index = Index(graphShader, binding.Name);
                Assert.That(graphShader.GetPropertyType(index), Is.EqualTo(ShaderPropertyType.Float), binding.Name);
                Assert.That((graphShader.GetPropertyFlags(index) & ShaderPropertyFlags.HideInInspector) != 0, Is.True, binding.Name);
            }
            Assert.That(material.GetFloat(Version), Is.EqualTo(0f));
            Binding colorBlend = bindings.Single(x => x.Name == "_ColorBlendMode");
            Assert.That(colorBlend.Mode && colorBlend.Word == 1 && colorBlend.EnabledMode == 0 && colorBlend.DisabledMode == 1, Is.True);
            Assert.That((bool)syncType.GetMethod("GraphFlagIntentSchemaAvailable", Static).Invoke(null, new object[] { material }), Is.True);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void GUI1B_VersionOneToTwo_Seeds29FromEachDenseRawWord_OnlyMirrorsAndVersionChange(int pattern)
        {
            uint[] zeros = { 0u, 0u }, ones = { uint.MaxValue, uint.MaxValue },
                dense = { 0xA569C39Eu, 0xC59347A1u }, high = { 0x80000001u, 0x80010002u };
            uint[][] patterns = { zeros, ones, dense, high };
            var material = NewMaterial(); var bindings = Bindings();
            SeedDense(material, patterns[pattern][0], patterns[pattern][1]);
            material.SetFloat(Version, 1f); SetMirrorSentinels(material, bindings);
            Snapshot before = Snapshot.Read(material); var root = NewRoot(material);
            Call(Prop(root, "SyncService"), "PrepareGraphGUIState");
            Assert.That(material.GetFloat(Version), Is.EqualTo(2f));
            AssertSeed(material, bindings);
            before.AssertSame(material, "One-time marker1-to-2 seed", AllowedSeedNames(bindings));
        }

        [Test]
        public void GUI1B_VersionZeroToTwo_AndFiniteNoncanonicalHalves_ReadWithoutReencodingRaw()
        {
            var material = NewMaterial(); var bindings = Bindings();
            SeedDense(material, 0xA569C39Eu, 0xC59347A1u, true);
            material.SetFloat(Version, 0f); SetMirrorSentinels(material, bindings);
            Snapshot before = Snapshot.Read(material); var root = NewRoot(material);
            Call(Prop(root, "SyncService"), "PrepareGraphGUIState");
            Assert.That(material.GetFloat(Version), Is.EqualTo(2f));
            AssertSeed(material, bindings);
            before.AssertSame(material, "Noncanonical raw halves never canonicalized", AllowedSeedNames(bindings));
        }

        [Test]
        public void GUI1B_AfterVersionTwo_NativeFallbackWordEditAndStaleMirrorCannotRollbackRaw()
        {
            var material = NewMaterial(); var bindings = Bindings();
            SeedDense(material, 0xA569C39Eu, 0xC59347A1u);
            material.SetFloat(Version, 1f); var root = NewRoot(material);
            Call(Prop(root, "SyncService"), "PrepareGraphGUIState");
            string firstMirror = bindings[0].Name; float oldMirror = material.GetFloat(firstMirror);
            WriteWord(material, 0, 0x80001234u); WriteWord(material, 1, 0xFFFFFFFFu);
            material.SetFloat(firstMirror, oldMirror == 1f ? 0f : 1f); // Unowned UI mirror intentionally stale.
            Snapshot afterNativeEdit = Snapshot.Read(material);
            for (int i = 0; i < 100; ++i)
            {
                Call(Prop(root, "Context"), "Refresh");
                Call(Prop(root, "SyncService"), "SyncMaterialState");
                StaticSync(material);
            }
            afterNativeEdit.AssertSame(material, "Native raw authority after marker2");
            Assert.That(ReadWord(material, 0), Is.EqualTo(0x80001234u));
            Assert.That(ReadWord(material, 1), Is.EqualTo(0xFFFFFFFFu));
            // No full mirror->flags Sync, Tier effective projection or feature callback is claimed here.
        }

        [Test]
        public void GUI1B_FutureVersion_IsNeverSeededOrDowngraded()
        {
            var material = NewMaterial(); var bindings = Bindings();
            SeedDense(material, 0xA569C39Eu, 0xC59347A1u); SetMirrorSentinels(material, bindings);
            material.SetFloat(Version, 7f); Snapshot before = Snapshot.Read(material);
            var root = NewRoot(material); Call(Prop(root, "SyncService"), "PrepareGraphGUIState"); StaticSync(material);
            before.AssertSame(material, "Future marker is opaque");
        }

        [Test]
        public void GUI1B_ExistingBitSpecificSetter_ChangesOnlyOwnedRawBit_NotUnrelatedMirrorOrWord()
        {
            var a = NewMaterial(); var b = NewMaterial(); var bindings = Bindings();
            SeedDense(a, 0x80000000u, 0x8F013497u); SeedDense(b, 0xC0010000u, 0x1234ABCDu);
            a.SetFloat(Version, 1f); b.SetFloat(Version, 1f);
            var root = NewRoot(a, b); var sync = Prop(root, "SyncService"); Call(sync, "PrepareGraphGUIState");
            Binding one = bindings.First(x => !x.Mode && x.Word == 0 && (unchecked((uint)x.Bit) & 0x80000000u) == 0);
            uint a0 = ReadWord(a, 0), b0 = ReadWord(b, 0), a1 = ReadWord(a, 1), b1 = ReadWord(b, 1);
            Snapshot beforeA = Snapshot.Read(a), beforeB = Snapshot.Read(b);
            Call(sync, "ApplyToggleFlag", one.Bit, true, one.Word);
            Assert.That(ReadWord(a, 0), Is.EqualTo(a0 | unchecked((uint)one.Bit)));
            Assert.That(ReadWord(b, 0), Is.EqualTo(b0 | unchecked((uint)one.Bit)));
            Assert.That(ReadWord(a, 1), Is.EqualTo(a1)); Assert.That(ReadWord(b, 1), Is.EqualTo(b1));
            beforeA.AssertSame(a, "Bit-specific setter A", "_NB_Flags0Lo16", "_NB_Flags0Hi16");
            beforeB.AssertSame(b, "Bit-specific setter B", "_NB_Flags0Lo16", "_NB_Flags0Hi16");
            // The existing setter is the future callback seam; the seed-only slice does not create that GUI callback.
        }

        [TestCase(false)] [TestCase(true)]
        public void GUI1B_MixedGraphAndShaderLab_OfficialFrontGuardProtectsBothOrders(bool graphFirst)
        {
            var graph = NewMaterial(); var legacy = NewMaterial(false); var bindings = Bindings();
            SeedDense(graph, 0xA569C39Eu, 0xC59347A1u); SetMirrorSentinels(graph, bindings);
            graph.SetFloat(Version, 1f);
            var targets = graphFirst ? new[] { graph, legacy } : new[] { legacy, graph };
            var root = NewRoot(false, targets);
            Assert.That((bool)Call(GetField(rootType, "MatEditor").GetValue(root), "HasMultipleMixedShaderValues"), Is.True);
            Assert.That((bool)Prop(Prop(root, "Context"), "HasMixedMaterialHosts"), Is.True);
            Snapshot beforeGraph = Snapshot.Read(graph), beforeLegacy = Snapshot.Read(legacy);
            Call(Prop(root, "SyncService"), "PrepareGraphGUIState");
            Call(Prop(root, "SyncService"), "SyncMaterialState");
            StaticSync(targets);
            Call(Prop(root, "SyncService"), "ApplyToggleFlag", bindings[0].Bit, true, bindings[0].Word);
            beforeGraph.AssertSame(graph, "Mixed Graph"); beforeLegacy.AssertSame(legacy, "Mixed ShaderLab");
        }

        [Test]
        public void GUI1B_TwoGraphMaterials_ActualUndoRedo_SeedsEachOwnBits()
        {
            var a = NewMaterial(); var b = NewMaterial(); var bindings = Bindings();
            SeedDense(a, 0xA569C39Eu, 0xC59347A1u); SeedDense(b, 0x11335577u, 0x80FF0214u);
            a.SetFloat(Version, 1f); b.SetFloat(Version, 0f);
            SetMirrorSentinels(a, bindings); SetMirrorSentinels(b, bindings);
            var root = NewRoot(a, b); Snapshot beforeA = Snapshot.Read(a), beforeB = Snapshot.Read(b);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Call(Prop(root, "SyncService"), "PrepareGraphGUIState");
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(a.GetFloat(Version), Is.EqualTo(2f)); Assert.That(b.GetFloat(Version), Is.EqualTo(2f));
                AssertSeed(a, bindings); AssertSeed(b, bindings);
                Snapshot afterA = Snapshot.Read(a), afterB = Snapshot.Read(b);
                beforeA.AssertSame(a, "Seed A only UI mirrors", AllowedSeedNames(bindings));
                beforeB.AssertSame(b, "Seed B only UI mirrors", AllowedSeedNames(bindings));
                Undo.PerformUndo(); beforeA.AssertSame(a, "Undo A"); beforeB.AssertSame(b, "Undo B");
                Undo.PerformRedo(); afterA.AssertSame(a, "Redo A"); afterB.AssertSame(b, "Redo B");
            }
            finally { Undo.RevertAllDownToGroup(group); }
        }

        [TestCase(false)] [TestCase(true)]
        public void GUI1B_PartialOrWrongTypeSchema_StaysAtMarkerOneWithoutPartialSeed(bool wrongType)
        {
            string properties = "_NB_DistortionMode(\"Mode\",Float)=0 _NB_Flags0Lo16(\"F0L\",Float)=0 _NB_Flags0Hi16(\"F0H\",Float)=0 " +
                "_NB_Flags1Lo16(\"F1L\",Float)=0 _NB_Flags1Hi16(\"F1H\",Float)=0 " +
                "_MainTexBigBlockItemFoldOut(\"Fold\",Float)=1 _BaseMapFoldOut(\"Related\",Float)=1 " +
                "_NB_GraphGUIStateVersion(\"Version\",Float)=0 ";
            properties += wrongType ? "_HueShift_Toggle(\"Wrong\",Integer)=1 " : "_HueShift_Toggle(\"Partial\",Float)=1 ";
            string source = "Shader \"Hidden/NBFX/GUI1BSchema" + Guid.NewGuid().ToString("N") + "\" { Properties { " + properties +
                " } SubShader { Pass { HLSLPROGRAM\n#pragma vertex Vert\n#pragma fragment Frag\nfloat4 Vert(float4 p:POSITION):SV_POSITION{return p;} float4 Frag():SV_Target{return 1;}\nENDHLSL\n} } }";
            var shader = ShaderUtil.CreateShaderAsset(source, false); Assert.That(shader, Is.Not.Null); owned.Add(shader);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            material.SetFloat(Version, 0f);
            if (wrongType) material.SetInteger("_HueShift_Toggle", 13);
            else material.SetFloat("_HueShift_Toggle", 13f);
            var root = NewRoot(material); Snapshot before = Snapshot.Read(material);
            Assert.That((bool)Prop(Prop(root, "Context"), "IsGraphMaterialHost"), Is.True);
            Assert.That((bool)syncType.GetMethod("GraphFlagIntentSchemaAvailable", Static).Invoke(null, new object[] { material }), Is.False);
            Call(Prop(root, "SyncService"), "PrepareGraphGUIState");
            Assert.That(material.GetFloat(Version), Is.EqualTo(1f));
            before.AssertSame(material, "Partial schema only legacy marker", Version);
        }

        [Test]
        public void GUI1B_RealAssetSaveReloadForceReimport_PreservesWordsMirrorsAndOfficialState()
        {
            string project = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\', '/');
            string explicitClone = Environment.GetEnvironmentVariable("NBFX_ISOLATED_PROJECT_DIR");
            bool explicitlyOwnedClone = !string.IsNullOrEmpty(explicitClone) &&
                string.Equals(project, Path.GetFullPath(explicitClone).Replace('\\', '/'), StringComparison.Ordinal) &&
                string.Equals(Path.GetFileName(Path.GetDirectoryName(project)), ".utmp", StringComparison.Ordinal);
            Assert.That(project.StartsWith("/tmp/", StringComparison.Ordinal) || project.StartsWith("/private/tmp/", StringComparison.Ordinal) || explicitlyOwnedClone, Is.True,
                "Persistence fixture refuses to write Assets in the main project; Root must use its isolated clone.");
            string name = "__NBFX_GUI1B_TEMP_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name); assetFolder = "Assets/" + name;
            Assert.That(AssetDatabase.IsValidFolder(assetFolder), Is.True);
            string path = assetFolder + "/Graph.mat";
            var material = NewMaterial(); material.hideFlags = HideFlags.None;
            var bindings = Bindings(); SeedDense(material, 0xA569C39Eu, 0xC59347A1u);
            material.SetFloat(Version, 1f); SetMirrorSentinels(material, bindings);
            var gui = Activator.CreateInstance(FindType("NBShaderEditor.NBShaderGraphGUI"));
            Call(gui, "ValidateMaterial", material); // URP Surface authority first.
            AssetDatabase.CreateAsset(material, path);
            var root = NewRoot(material); Call(Prop(root, "SyncService"), "PrepareGraphGUIState");
            Assert.That(material.GetFloat(Version), Is.EqualTo(2f)); AssertSeed(material, bindings);
            Snapshot before = Snapshot.Read(material); EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            Assert.That(File.Exists(Path.Combine(project, path)), Is.True);
            string yaml = File.ReadAllText(Path.Combine(project, path));
            foreach (Binding binding in bindings) Assert.That(yaml.Contains(binding.Name), Is.True, binding.Name);
            foreach (string word in Words) Assert.That(yaml.Contains(word + "Lo16") && yaml.Contains(word + "Hi16"), Is.True, word);
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.That(loaded, Is.Not.Null);
            before.AssertSame(loaded, "Saved and reimported Graph material");
            var reloadedRoot = NewRoot(loaded); Call(Prop(reloadedRoot, "SyncService"), "SyncMaterialState");
            before.AssertSame(loaded, "No reseed after imported marker2");
        }
    }
}
