using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
    // Test-only native event host. Never a replacement material Inspector.
    // Legacy constructors read EditorStyles, which batch tests have not
    // necessarily initialized. Run those constructors inside real OnGUI;
    // do not synthesize GUIStyle/skin/cache state or ignore Unity errors.
    public sealed class GUI1AConstructorEventHost : EditorWindow
    {
        internal Action Construct;
        internal Exception Failure;
        internal bool ReceivedNativeEvent;
        internal string CommandName;

        void OnGUI()
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.ExecuteCommand ||
                current.commandName != CommandName || ReceivedNativeEvent) return;
            ReceivedNativeEvent = true;
            try
            {
                var initialize = typeof(EditorStyles).GetMethod("UpdateSkinCache",
                    BindingFlags.Static | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                Assert.That(initialize, Is.Not.Null, "Current official Editor skin initializer is required.");
                initialize.Invoke(null, null); // Actual official skin/cache, inside native GUI context.
                Assert.That(EditorStyles.label, Is.Not.Null);
                Construct();
            }
            catch (Exception exception) { Failure = exception; }
            finally { current.Use(); }
        }
    }

    /// <summary>
    /// GUI1A ordinary-Mesh backend only: real imported Graph/ShaderLab Materials,
    /// original Root/Context/Sync/MainTex/control constructors and actual setters.
    /// Reflection is test-only because this test assembly has no NB dependencies.
    /// No GUILayout outside a GUI event, visible-Inspector, image-parity, Tier,
    /// functional-intent seed, GraphMPB, VFX, Player or full G3/G4 claim.
    /// </summary>
    public sealed class G4GraphGuiMainTextureTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string Version = "_NB_GraphGUIStateVersion";
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] SharedNames = { "_BaseMap", "_Color", "_BaseMap_ST", "_BaseMapUVRotation", "_BaseMapUVRotationSpeed", "_BaseMapMaskMapOffset", "_TexDistortion_intensity" };
        static readonly string[] WordPrefixes = { "_NB_Flags0", "_NB_Flags1", "_NB_WrapFlags", "_NB_ColorChannel", "_NB_PNoiseBlend", "_NB_ForceNoMipFlags", "_NB_UVModeFlag0", "_NB_UVModeFlagType0" };
        static readonly string[] PassNames = { "Universal Forward", "UniversalForward", "DepthOnly", "DepthNormalsOnly", "ShadowCaster", "MotionVectors", "XRMotionVectors", "SRPDefaultUnlit", "Universal2D", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass" };
        readonly List<Object> owned = new List<Object>();
        Shader graphShader, legacyShader;
        Type rootType, contextType, syncType;
        string assetFolder;

        [OneTimeSetUp]
        public void ReimportAndWarmActualGraph()
        {
            // Root runs this exclusively in its isolated clone. Initialize the
            // real URP Camera path before asking an imported Graph for passes or
            // properties. Direct CommandBuffer draws alone did not resolve the
            // placeholder in the v2 standalone run (although v1 full suite did).
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader, Is.True);
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "Warmup must use the actual device.");
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            var rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0] as UniversalRendererData;
            Assert.That(rendererData, Is.Not.Null, "Current ordinary-Mesh renderer is required.");
            const int layer = 4;
            Assert.That(rendererData.opaqueLayerMask.value & (1 << layer), Is.Not.Zero, "Warmup reads allowed opaque layer, never changes it.");
            Assert.That(rendererData.transparentLayerMask.value & (1 << layer), Is.Not.Zero, "Warmup reads allowed transparent layer, never changes it.");
            var nbFeature = rendererData.rendererFeatures.FirstOrDefault(feature => feature && feature.GetType().FullName == "NBShader.NBPostProcess");
            Assert.That(nbFeature, Is.Not.Null, "Current NB renderer feature must be isolated during the warm camera.");
            bool nbWasActive = nbFeature.isActive;
            string project = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));
            var protectedPaths = new[] { "ProjectSettings/ProjectSettings.asset", "Assets/UniversalRenderPipelineGlobalSettings.asset", AssetDatabase.GetAssetPath(rendererData) }
                .Select(path => Path.Combine(project, path)).Distinct().ToArray();
            var protectedBytes = protectedPaths.ToDictionary(path => path, File.ReadAllBytes);
            var warm = new Material(graphShader) { hideFlags = HideFlags.HideAndDontSave };
            Scene scene = EditorSceneManager.NewPreviewScene();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("GUI1A real URP Mesh schema warm camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var target = new RenderTexture(8, 8, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var read = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
            var active = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                SceneManager.MoveGameObjectToScene(quad, scene);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                quad.hideFlags = cameraObject.hideFlags = HideFlags.HideAndDontSave;
                quad.layer = layer;
                quad.transform.localScale = new Vector3(2, 2, 1);
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.sharedMaterial = warm; // Do not pre-query a pass/property to decide the first draw.
                camera.scene = scene; camera.enabled = false;
                camera.orthographic = true; camera.orthographicSize = 1.5f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20f;
                camera.transform.position = new Vector3(0, 0, -5); camera.transform.rotation = Quaternion.identity;
                camera.cullingMask = 1 << layer; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear; camera.allowHDR = true; camera.allowMSAA = false;
                camera.targetTexture = target;
                cameraData.SetRenderer(0); cameraData.renderPostProcessing = false;
                Assert.That(target.Create() && !target.sRGB, Is.True);
                nbFeature.SetActive(false); rendererData.SetDirty();
                for (int i = 0; i < 4; ++i) camera.Render();
                RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, 8, 8), 0, 0, false); read.Apply(false, false);
                // Actual URP Camera draw/readback initializes the test host; pixels are NOT
                // compared, archived, or offered as feature/image-parity evidence.
                Assert.That(graphShader.isSupported && legacyShader.isSupported, Is.True);
                Assert.That(AssetDatabase.LoadAllAssetsAtPath(GraphPath).Any(asset => asset != null &&
                    asset.GetType().FullName == "UnityEditor.Rendering.Universal.ShaderGraph.UniversalMetadata"), Is.True,
                    "Imported Graph must have real URP metadata, not placeholder import state.");
                Assert.That(warm.FindPass("Universal Forward"), Is.GreaterThanOrEqualTo(0), "Generated ordinary-Mesh Forward pass is required.");
                foreach (string name in new[] { "_MainTexBigBlockItemFoldOut", "_BaseMapFoldOut", Version })
                {
                    Assert.That(warm.HasProperty(name), Is.True, "Warm generated Graph must contain " + name);
                    Assert.That(graphShader.GetPropertyType(PropertyIndex(graphShader, name)), Is.EqualTo(ShaderPropertyType.Float), name);
                }
                Assert.That(MaterialEditor.GetMaterialProperties(new Object[] { warm }).Length, Is.GreaterThan(0));
                Assert.That(ShaderUtil.GetShaderMessages(graphShader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error), Is.False);
                Debug.Log("NBFX_GUI1A_WARM actual preview-scene URP Camera.Render x4 + Mesh + float RT readback; layer4 allowed; URP metadata/Forward/3Float schema ready; NOT image parity.");
            }
            finally
            {
                nbFeature.SetActive(nbWasActive); rendererData.SetDirty();
                camera.targetTexture = null;
                ShaderUtil.allowAsyncCompilation = async; RenderTexture.active = active;
                target.Release();
                foreach (Object item in new Object[] { read, target, warm }) Object.DestroyImmediate(item);
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var entry in protectedBytes)
                    Assert.That(File.ReadAllBytes(entry.Key), Is.EqualTo(entry.Value), "Warmup must not change protected asset bytes: " + entry.Key);
            }
        }

        [SetUp]
        public void SetupTypes()
        {
            rootType = FindType("NBShaderEditor.NBShaderRootItem");
            contextType = FindType("NBShaderEditor.NBShaderGUIContext");
            syncType = FindType("NBShaderEditor.NBShaderSyncService");
            Assert.That(rootType.GetMethod("InitializeGraphMainTextureInputs", Members), Is.Not.Null, "Root must integrate GUI1A before this fixture.");
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; --i)
                if (owned[i] && !AssetDatabase.Contains(owned[i])) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            if (assetFolder != null)
            {
                Assert.That(AssetDatabase.DeleteAsset(assetFolder), Is.True, "Remove only this fixture's isolated temporary assets.");
                assetFolder = null;
            }
        }

        static Type FindType(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, name); return type;
        }
        static FieldInfo GetField(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, Members | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            Assert.Fail("Missing field " + name); return null;
        }
        static object Field(object target, string name) => GetField(target.GetType(), name).GetValue(target);
        static object Prop(object target, string name) => target.GetType().GetProperty(name, Members).GetValue(target);
        static object Call(object target, string name, params object[] arguments)
        {
            MethodInfo method = null;
            for (Type type = target.GetType(); type != null && method == null; type = type.BaseType)
                method = type.GetMethod(name, Members | BindingFlags.DeclaredOnly);
            Assert.That(method, Is.Not.Null, target.GetType().FullName + "." + name);
            return method.Invoke(target, arguments);
        }
        static object EnumArg(string name, int value) => Enum.ToObject(FindType("NBShaderEditor." + name), value);
        Material NewMaterial(bool graph)
        {
            var material = new Material(graph ? graphShader : legacyShader) { hideFlags = HideFlags.HideAndDontSave };
            owned.Add(material); return material;
        }
        object NewRoot(params Material[] materials) => NewRoot(true, materials);
        object NewRoot(bool collectNativeProperties, params Material[] materials)
        {
            var root = Activator.CreateInstance(rootType);
            var editor = Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)) as MaterialEditor;
            Assert.That(editor, Is.Not.Null); owned.Add(editor);
            GetField(rootType, "MatEditor").SetValue(root, editor);
            GetField(rootType, "Shader").SetValue(root, materials[0].shader);
            GetField(rootType, "Mats").SetValue(root, new List<Material>(materials));
            Call(root, "InitFlags", new List<Material>(materials));
            var dictionary = (IDictionary)Field(root, "PropertyInfoDic");
            Type infoType = FindType("NBShaderEditor.ShaderPropertyInfo");
            // Native MaterialEditor.OnInspectorGUI rejects heterogeneous shader
            // selections before GetMaterialProperties. Its forced mixed lookup
            // logs missing-property errors. Mixed-guard tests deliberately leave
            // this dictionary empty: the NB guard must need no property metadata.
            foreach (MaterialProperty property in collectNativeProperties
                ? MaterialEditor.GetMaterialProperties(materials.Cast<Object>().ToArray())
                : Array.Empty<MaterialProperty>())
            {
                int index = PropertyIndex(materials[0].shader, property.name);
                var info = Activator.CreateInstance(infoType);
                infoType.GetField("Property").SetValue(info, property);
                infoType.GetField("Name").SetValue(info, property.name);
                infoType.GetField("Index").SetValue(info, index);
                dictionary.Add(property.name, info);
            }
            var context = Activator.CreateInstance(contextType, new[] { root });
            var sync = Activator.CreateInstance(syncType, new[] { root });
            rootType.GetProperty("Context", Members).SetValue(root, context);
            rootType.GetProperty("SyncService", Members).SetValue(root, sync);
            Call(context, "Refresh");
            return root;
        }
        object NewLegacyMainTextureInsideNativeEvent(object root)
        {
            var host = ScriptableObject.CreateInstance<GUI1AConstructorEventHost>();
            host.hideFlags = HideFlags.HideAndDontSave;
            host.titleContent = new GUIContent("NBFX constructor test");
            host.position = new Rect(20, 20, 160, 60);
            host.CommandName = "NBFX_GUI1A_Construct_" + Guid.NewGuid().ToString("N");
            object main = null;
            host.Construct = () => main = Activator.CreateInstance(FindType("NBShaderEditor.MainTexBigBlockItem"), new object[] { root, null });
            try
            {
                // Show/SendEvent establishes Unity's native GUIView context in
                // the isolated batch Editor only. No GUILayout or Inspector draw.
                host.ShowUtility();
                host.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = host.CommandName });
                Assert.That(host.ReceivedNativeEvent, Is.True, "Native OnGUI must actually receive the constructor event; no fake fallback/skip.");
                if (host.Failure != null) ExceptionDispatchInfo.Capture(host.Failure).Throw();
                Assert.That(main, Is.Not.Null);
                Debug.Log("NBFX_GUI1A_LEGACY_CONSTRUCTOR actual native OnGUI/SendEvent + official EditorStyles initialization; NOT Inspector layout evidence.");
                return main;
            }
            finally
            {
                host.Construct = null;
                host.Close();
                if (host) Object.DestroyImmediate(host);
            }
        }
        static int PropertyIndex(Shader shader, string name)
        {
            for (int i = 0; i < shader.GetPropertyCount(); ++i) if (shader.GetPropertyName(i) == name) return i;
            Assert.Fail("Missing actual Shader property " + name); return -1;
        }
        static MaterialProperty ActualProperty(object item) => (MaterialProperty)Field(Field(item, "PropertyInfo"), "Property");
        static string[] Ownership(object root) => ((IEnumerable<string>)Call(root, "GetSharedGraphPropertyNames")).ToArray();
        void StaticSync(params Material[] materials)
        {
            var method = syncType.GetMethods(Statics).Single(m => m.Name == "SyncMaterialState" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType != typeof(Material));
            method.Invoke(null, new object[] { new List<Material>(materials) });
        }
        static void AssertVector(Vector4 a, Vector4 b, string message)
        {
            for (int i = 0; i < 4; ++i) Assert.That(a[i], Is.EqualTo(b[i]), message + " channel " + i);
        }
        static Vector4 NativeST(Material material)
        {
            Vector2 scale = material.GetTextureScale("_BaseMap"), offset = material.GetTextureOffset("_BaseMap");
            return new Vector4(scale.x, scale.y, offset.x, offset.y);
        }
        static void WriteWord(Material material, string prefix, uint word)
        {
            material.SetFloat(prefix + "Lo16", word & 65535u); material.SetFloat(prefix + "Hi16", word >> 16);
        }
        static uint ReadWord(Material material, string prefix) => (uint)material.GetFloat(prefix + "Lo16") | ((uint)material.GetFloat(prefix + "Hi16") << 16);
        static void SeedNonDefaultRaw(Material material, int offset, bool fractional)
        {
            for (int i = 0; i < WordPrefixes.Length; ++i)
            {
                if (fractional)
                {
                    material.SetFloat(WordPrefixes[i] + "Lo16", -17f + i * .5f + offset);
                    material.SetFloat(WordPrefixes[i] + "Hi16", 65534.5f + i * .5f + offset);
                }
                else WriteWord(material, WordPrefixes[i], 0xA569C39Eu ^ ((uint)(i + offset) * 0x01010101u));
            }
            material.SetColor("_Color", new Color(2.3f, .17f, .61f, .43f));
            material.SetVector("_BaseMap_ST", new Vector4(-2.25f, 1.375f, .42f, -.31f));
            material.SetFloat("_ProgramNoise_Toggle", 1f); material.SetFloat("_ProgramNoise_Simple_Toggle", 1f);
            material.SetFloat("_SixWayColorAbsorptionToggle", 1f);
            material.SetFloat("_MainTexBigBlockItemFoldOut", 0f); material.SetFloat("_BaseMapFoldOut", 0f);
            material.SetFloat(Version, 0f); material.renderQueue = 3087;
            material.EnableKeyword("EVALUATE_SH_VERTEX"); material.EnableKeyword("VFX_SIX_WAY_ABSORPTION");
            material.SetShaderPassEnabled("DepthOnly", false); material.SetShaderPassEnabled("ShadowCaster", false);
        }

        sealed class Snapshot
        {
            public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
            public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
            public readonly Dictionary<string, Vector4> Vectors = new Dictionary<string, Vector4>();
            public readonly Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();
            public readonly Dictionary<string, bool> Passes = new Dictionary<string, bool>();
            public string[] Keywords;
            public string Tag;
            public int Queue;
            public bool Instancing, DoubleSided;
            public MaterialGlobalIlluminationFlags GI;
            public static Snapshot Read(Material material)
            {
                var s = new Snapshot { Keywords = material.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal).ToArray(), Tag = material.GetTag("RenderType", false, ""), Queue = material.renderQueue, Instancing = material.enableInstancing, DoubleSided = material.doubleSidedGI, GI = material.globalIlluminationFlags };
                for (int i = 0; i < material.shader.GetPropertyCount(); ++i)
                {
                    string name = material.shader.GetPropertyName(i);
                    switch (material.shader.GetPropertyType(i))
                    {
                        case ShaderPropertyType.Float: case ShaderPropertyType.Range: s.Floats[name] = material.GetFloat(name); break;
                        case ShaderPropertyType.Int: s.Ints[name] = material.GetInteger(name); break;
                        case ShaderPropertyType.Color: s.Vectors[name] = material.GetColor(name); break;
                        case ShaderPropertyType.Vector: s.Vectors[name] = material.GetVector(name); break;
                        case ShaderPropertyType.Texture:
                            s.Textures[name] = material.GetTexture(name);
                            Vector2 scale = material.GetTextureScale(name), offset = material.GetTextureOffset(name);
                            s.Vectors[name + ".nativeST"] = new Vector4(scale.x, scale.y, offset.x, offset.y); break;
                    }
                }
                foreach (string pass in PassNames) s.Passes[pass] = material.GetShaderPassEnabled(pass);
                return s;
            }
            public void AssertSame(Material material, string label, params string[] allowed)
            {
                var next = Read(material); var exceptions = new HashSet<string>(allowed);
                Assert.That(next.Keywords, Is.EqualTo(Keywords), label + " keywords");
                Assert.That(next.Tag, Is.EqualTo(Tag), label + " tag"); Assert.That(next.Queue, Is.EqualTo(Queue), label + " queue");
                Assert.That(next.Instancing, Is.EqualTo(Instancing)); Assert.That(next.DoubleSided, Is.EqualTo(DoubleSided)); Assert.That(next.GI, Is.EqualTo(GI));
                foreach (var row in Floats) if (!exceptions.Contains(row.Key))
                    Assert.That(BitConverter.ToInt32(BitConverter.GetBytes(next.Floats[row.Key]), 0),
                        Is.EqualTo(BitConverter.ToInt32(BitConverter.GetBytes(row.Value), 0)), label + " exact Float bits " + row.Key);
                foreach (var row in Ints) if (!exceptions.Contains(row.Key)) Assert.That(next.Ints[row.Key], Is.EqualTo(row.Value), label + " Integer " + row.Key);
                foreach (var row in Vectors) if (!exceptions.Contains(row.Key)) AssertVector(next.Vectors[row.Key], row.Value, label + " vector " + row.Key);
                foreach (var row in Textures) if (!exceptions.Contains(row.Key)) Assert.That(next.Textures[row.Key], Is.EqualTo(row.Value), label + " texture " + row.Key);
                foreach (var row in Passes) Assert.That(next.Passes[row.Key], Is.EqualTo(row.Value), label + " pass " + row.Key);
            }
        }

        [Test]
        public void GUI1A_ActualGraphSchema_TrueHiddenFloatColorAndVector_NoShadowLegacyProperties()
        {
            var material = NewMaterial(true);
            foreach (string name in new[] { "_MainTexBigBlockItemFoldOut", "_BaseMapFoldOut", Version })
            {
                int index = PropertyIndex(graphShader, name);
                Assert.That(graphShader.GetPropertyType(index), Is.EqualTo(ShaderPropertyType.Float), name);
                Assert.That((graphShader.GetPropertyFlags(index) & ShaderPropertyFlags.HideInInspector) != 0, Is.True, name);
            }
            Assert.That(graphShader.GetPropertyType(PropertyIndex(graphShader, "_Color")), Is.EqualTo(ShaderPropertyType.Color));
            Assert.That((graphShader.GetPropertyFlags(PropertyIndex(graphShader, "_Color")) & ShaderPropertyFlags.HDR) != 0, Is.True);
            Assert.That(graphShader.GetPropertyType(PropertyIndex(graphShader, "_BaseMap_ST")), Is.EqualTo(ShaderPropertyType.Vector));
            foreach (string missing in new[] { "_BaseColor", "_TransparentMode", "_MeshSourceMode", "_W9ParticleShaderFlags", "_W9ParticleShaderPNoiseBlendFlag" })
                Assert.That(material.HasProperty(missing), Is.False, "No shadow property " + missing);
            Assert.That(material.GetFloat("_MainTexBigBlockItemFoldOut"), Is.EqualTo(1f));
            Assert.That(material.GetFloat("_BaseMapFoldOut"), Is.EqualTo(1f)); Assert.That(material.GetFloat(Version), Is.EqualTo(0f));
        }

        [Test]
        public void GUI1A_ActualOriginalMainTexConstructor_SevenOwnedProperties_NativeFeaturesRemain()
        {
            var material = NewMaterial(true); var root = NewRoot(material);
            Assert.That(Ownership(root), Is.Empty);
            Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
            Assert.That(Ownership(root), Is.EquivalentTo(SharedNames));
            var main = Field(root, "_mainTexBlock"); Assert.That(main.GetType().FullName, Is.EqualTo("NBShaderEditor.MainTexBigBlockItem"));
            var group = Field(main, "_baseMapGroupItem");
            Assert.That(ActualProperty(Field(group, "_textureItem")).name, Is.EqualTo("_BaseMap"));
            Assert.That(ActualProperty(Field(group, "_colorItem")).name, Is.EqualTo("_Color"));
            Assert.That(ActualProperty(Field(group, "_scaleOffsetItem")).name, Is.EqualTo("_BaseMap_ST"));
            Assert.That((bool)Field(Field(group, "_scaleOffsetItem"), "_isVectorProperty"), Is.True);
            foreach (string absent in new[] { "_uvModeItem", "_offsetXCustomDataItem", "_offsetYCustomDataItem", "_uiColorItem", "_uiMainTexScaleOffsetItem", "_pNoiseBlendModeItem" }) Assert.That(Field(main, absent), Is.Null);
            foreach (string name in new[] { "_baseMapWrapModeItem", "_baseMapForceNoMipItem", "_alphaChannelItem" }) Assert.That(Field(main, name), Is.Not.Null);
            foreach (string name in new[] { "_ProgramNoise_Toggle", "_ProgramNoise_Simple_Toggle", "_ProgramNoise_Voronoi_Toggle", "_SixWayColorAbsorptionToggle", "_RigRTBk", "_FxLightMode" })
            {
                Assert.That(material.HasProperty(name), Is.True, name);
                Assert.That(Ownership(root).Contains(name), Is.False, "Native fallback must retain " + name);
                var p = MaterialEditor.GetMaterialProperties(new Object[] { material }).Single(x => x.name == name);
                Assert.That((p.propertyFlags & ShaderPropertyFlags.HideInInspector) == 0, Is.True, "Actually visible native input " + name);
            }
            // Ownership/constructors are backend evidence, not proof of actual GUILayout rendering.
        }

        [Test]
        public void GUI1A_LegacyOriginalMainTexConstructor_AllOriginalChildrenAndNativeSTStillExist()
        {
            var material = NewMaterial(false); var root = NewRoot(material); var before = Snapshot.Read(material);
            var main = NewLegacyMainTextureInsideNativeEvent(root);
            before.AssertSame(material, "Legacy MainTex construction");
            var group = Field(main, "_baseMapGroupItem");
            Assert.That(ActualProperty(Field(group, "_colorItem")).name, Is.EqualTo("_BaseColor"));
            Assert.That(ActualProperty(Field(group, "_scaleOffsetItem")).name, Is.EqualTo("_BaseMap"));
            Assert.That((bool)Field(Field(group, "_scaleOffsetItem"), "_isVectorProperty"), Is.False);
            foreach (string field in new[] { "_uvModeItem", "_offsetXCustomDataItem", "_offsetYCustomDataItem", "_uiColorItem", "_uiMainTexScaleOffsetItem", "_pNoiseBlendModeItem" }) Assert.That(Field(main, field), Is.Not.Null, field);
            Assert.That(Ownership(root), Is.Empty);
        }

        [TestCase(0, 0, 0)] [TestCase(0, 1, 2)] [TestCase(1, 0, 1)] [TestCase(1, 1, 1)]
        public void GUI1A_Context_OrdinaryMeshAndOfficialSurface_ReadOnly(int surface, int clip, int transparent)
        {
            var material = NewMaterial(true); material.SetFloat("_Surface", surface); material.SetFloat("_AlphaClip", clip);
            var root = NewRoot(material); var context = Prop(root, "Context"); var before = Snapshot.Read(material);
            Call(context, "Refresh"); before.AssertSame(material, "Context read-only");
            Assert.That((bool)Prop(context, "IsGraphMaterialHost"), Is.True);
            Assert.That(Convert.ToInt32(Prop(context, "MeshSourceMode")), Is.EqualTo(1));
            foreach (string state in new[] { "UIEffectEnabled", "UseGraphicMainTex", "ParticleMode" }) Assert.That(Convert.ToInt32(Prop(context, state)), Is.EqualTo(0), state);
            Assert.That(Convert.ToInt32(Prop(context, "TransparentMode")), Is.EqualTo(transparent));
        }

        [Test]
        public void GUI1A_Context_MixedOfficialSurfaceDoesNotInventSingleMode()
        {
            var a = NewMaterial(true); var b = NewMaterial(true); a.SetFloat("_Surface", 0); b.SetFloat("_Surface", 1);
            var root = NewRoot(a, b); var context = Prop(root, "Context");
            Assert.That((bool)Prop(context, "HasMixedMaterialHosts"), Is.False);
            Assert.That(Convert.ToInt32(Prop(context, "TransparentMode")), Is.EqualTo(-1));
            a.SetFloat("_Surface", 0); b.SetFloat("_Surface", 0); a.SetFloat("_AlphaClip", 0); b.SetFloat("_AlphaClip", 1);
            root = NewRoot(a, b); Assert.That(Convert.ToInt32(Prop(Prop(root, "Context"), "TransparentMode")), Is.EqualTo(-1));
        }

        [TestCase(false)] [TestCase(true)]
        public void GUI1A_MixedHostOrders_ConstructorSyncCallbacksAndResetDoNotWriteEither(bool graphFirst)
        {
            var graph = NewMaterial(true); var legacy = NewMaterial(false); SeedNonDefaultRaw(graph, 2, false);
            var targets = graphFirst ? new[] { graph, legacy } : new[] { legacy, graph };
            var a = Snapshot.Read(graph); var b = Snapshot.Read(legacy);
            var root = NewRoot(false, targets);
            Assert.That(((IDictionary)Field(root, "PropertyInfoDic")).Count, Is.EqualTo(0), "Mixed guard is independent of native property lookup.");
            Assert.That((bool)Call(Field(root, "MatEditor"), "HasMultipleMixedShaderValues"), Is.True,
                "Actual official Inspector rejects this selection before heterogeneous GetMaterialProperties.");
            Assert.That((bool)Prop(Prop(root, "Context"), "HasMixedMaterialHosts"), Is.True);
            Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.False); Assert.That(Ownership(root), Is.Empty);
            Call(Prop(root, "SyncService"), "SyncMaterialState"); StaticSync(targets);
            InvokeLegacyCallbacks(Prop(root, "SyncService")); Call(Prop(root, "SyncService"), "ApplyToggleFlag", int.MinValue, true, 0);
            Call(root, "ExecuteResetAllItems");
            a.AssertSame(graph, "Mixed graph"); b.AssertSame(legacy, "Mixed legacy");
        }

        static string[] GraphSeedChangedNames(Material material)
        {
            var syncType = FindType("NBShaderEditor.NBShaderSyncService");
            var schema = syncType.GetMethod("GraphFlagIntentSchemaAvailable", BindingFlags.Static | BindingFlags.NonPublic);
            if (schema == null || !(bool)schema.Invoke(null, new object[] { material })) return new[] { Version };
            var result = new List<string> { Version };
            foreach (string table in new[] { "ToggleFlagBindings", "ModeFlagBindings" })
                foreach (object binding in (Array)syncType.GetField(table, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
                    result.Add((string)Field(binding, "propertyName"));
            Assert.That(result.Count, Is.EqualTo(30), "Same authority: 29 new UI mirrors plus marker");
            return result.ToArray();
        }

        static float GraphSeedVersion(Material material) => GraphSeedChangedNames(material).Length == 30 ? 2f : 1f;

        static void AssertGraphSeedMirrors(Material material)
        {
            if (GraphSeedVersion(material) < 2f) return;
            var syncType = FindType("NBShaderEditor.NBShaderSyncService");
            foreach (string table in new[] { "ToggleFlagBindings", "ModeFlagBindings" })
                foreach (object binding in (Array)syncType.GetField(table, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
                {
                    string name = (string)Field(binding, "propertyName");
                    int bits = (int)Field(binding, "flagBits"), index = (int)Field(binding, "flagIndex");
                    string prefix = index == 0 ? "_NB_Flags0" : "_NB_Flags1";
                    uint low = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix + "Lo16"), 0, 65535));
                    uint high = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix + "Hi16"), 0, 65535));
                    bool on = (((high << 16) | low) & unchecked((uint)bits)) != 0;
                    int enabled = table == "ToggleFlagBindings" ? 1 : (int)Field(binding, "enabledMode");
                    int disabled = table == "ToggleFlagBindings" ? 0 : enabled == 0 ? 1 : 0;
                    Assert.That(material.GetFloat(name), Is.EqualTo(on ? enabled : disabled), "Exact UI seed " + name);
                }
        }

        [TestCase(false)] [TestCase(true)]
        public void GUI1A_VersionSeed_ChangesOnlyMarker_EightRawWordsAreNeverReencoded(bool fractional)
        {
            var material = NewMaterial(true); SeedNonDefaultRaw(material, 0, fractional); var root = NewRoot(material); var before = Snapshot.Read(material);
            Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
            Assert.That(material.GetFloat(Version), Is.EqualTo(GraphSeedVersion(material))); AssertGraphSeedMirrors(material); before.AssertSame(material, "Version/UI-only seed", GraphSeedChangedNames(material));
            foreach (string prefix in WordPrefixes)
                foreach (string suffix in new[] { "Lo16", "Hi16" }) Assert.That(material.GetFloat(prefix + suffix), Is.EqualTo(before.Floats[prefix + suffix]), prefix + suffix);
        }

        [Test]
        public void GUI1A_OneHundredRepeatedConstructorAndBothSyncEntrypoints_AreIdempotent()
        {
            var material = NewMaterial(true); SeedNonDefaultRaw(material, 1, true); var root = NewRoot(material);
            Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True); var before = Snapshot.Read(material);
            for (int i = 0; i < 100; ++i)
            {
                Call(Prop(root, "Context"), "Refresh"); Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
                Call(Prop(root, "SyncService"), "SyncMaterialState"); StaticSync(material);
            }
            before.AssertSame(material, "100 repeated calls");
            material.SetFloat(Version, 7f); before = Snapshot.Read(material); StaticSync(material); before.AssertSame(material, "Future version preserved");
        }

        [Test]
        public void GUI1A_TwoGraphMaterials_IndependentSeedNeverCopiesFirstRawValues()
        {
            var a = NewMaterial(true); var b = NewMaterial(true); SeedNonDefaultRaw(a, 0, false); SeedNonDefaultRaw(b, 3, true); b.SetFloat(Version, 4f);
            var root = NewRoot(a, b); var aa = Snapshot.Read(a); var bb = Snapshot.Read(b);
            Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
            aa.AssertSame(a, "Independent unseeded target", GraphSeedChangedNames(a)); AssertGraphSeedMirrors(a); bb.AssertSame(b, "Already seeded target");
            Assert.That(a.GetFloat(Version), Is.EqualTo(GraphSeedVersion(a))); Assert.That(b.GetFloat(Version), Is.EqualTo(4f));
        }

        [Test]
        public void GUI1A_VersionPreparation_RealProductUndoRedoRestoresOnlyEachMarker()
        {
            var a = NewMaterial(true); var b = NewMaterial(true); SeedNonDefaultRaw(a, 0, false); SeedNonDefaultRaw(b, 2, true);
            var root = NewRoot(a, b); var aa = Snapshot.Read(a); var bb = Snapshot.Read(b);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                // No caller RecordObjects: test the product's actual version-seed Undo.
                Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(a.GetFloat(Version), Is.EqualTo(GraphSeedVersion(a))); Assert.That(b.GetFloat(Version), Is.EqualTo(GraphSeedVersion(b))); AssertGraphSeedMirrors(a); AssertGraphSeedMirrors(b);
                Undo.PerformUndo(); aa.AssertSame(a, "Seed undo A"); bb.AssertSame(b, "Seed undo B");
                Undo.PerformRedo(); aa.AssertSame(a, "Seed redo A", GraphSeedChangedNames(a)); bb.AssertSame(b, "Seed redo B", GraphSeedChangedNames(b));
                Assert.That(a.GetFloat(Version), Is.EqualTo(GraphSeedVersion(a))); Assert.That(b.GetFloat(Version), Is.EqualTo(GraphSeedVersion(b))); AssertGraphSeedMirrors(a); AssertGraphSeedMirrors(b);
            }
            finally { Undo.RevertAllDownToGroup(group); }
        }

        [Test]
        public void GUI1A_ChannelCallback_ActualSharedSetterOwnUndoKeepsOtherBitsAndBothTargets()
        {
            var a = NewMaterial(true); var b = NewMaterial(true); WriteWord(a, "_NB_ColorChannel", 0xD3E9876Eu); WriteWord(b, "_NB_ColorChannel", 0x8F654323u);
            var root = NewRoot(a, b); Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
            var channel = Field(Field(root, "_mainTexBlock"), "_alphaChannelItem"); var aa = Snapshot.Read(a); var bb = Snapshot.Read(b);
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                // SetChannel is the exact setter used by the original GUI popup.
                // It itself calls Undo.RecordObject per changed material.
                Call(channel, "SetChannel", 0); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(ReadWord(a, "_NB_ColorChannel"), Is.EqualTo(0xD3E9876Cu)); Assert.That(ReadWord(b, "_NB_ColorChannel"), Is.EqualTo(0x8F654320u));
                aa.AssertSame(a, "Channel A", "_NB_ColorChannelLo16"); bb.AssertSame(b, "Channel B", "_NB_ColorChannelLo16");
                Undo.PerformUndo(); aa.AssertSame(a, "Channel undo A"); bb.AssertSame(b, "Channel undo B");
                Undo.PerformRedo(); Assert.That(ReadWord(a, "_NB_ColorChannel"), Is.EqualTo(0xD3E9876Cu)); Assert.That(ReadWord(b, "_NB_ColorChannel"), Is.EqualTo(0x8F654320u));
            }
            finally { Undo.RevertAllDownToGroup(group); }
        }

        [TestCase(false)] [TestCase(true)]
        public void GUI1A_ActualSharedWrapNoMipChannelAndSTSetters_StorageAndReset(bool graph)
        {
            var material = NewMaterial(graph); var root = NewRoot(material);
            object main;
            if (graph) { Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True); main = Field(root, "_mainTexBlock"); }
            else main = NewLegacyMainTextureInsideNativeEvent(root);
            var wrap = Field(main, "_baseMapWrapModeItem"); var mip = Field(main, "_baseMapForceNoMipItem"); var channel = Field(main, "_alphaChannelItem");
            var st = Field(Field(main, "_baseMapGroupItem"), "_scaleOffsetItem");
            for (int mode = 0; mode < 4; ++mode)
            {
                Call(wrap, "SetMode", mode); Assert.That((int)Call(wrap, "GetFirstMode"), Is.EqualTo(mode));
                Call(channel, "SetChannel", mode); Assert.That((int)Call(channel, "GetFirstChannel"), Is.EqualTo(mode));
                Call(mip, "SetValue", (mode & 1) != 0); Assert.That((bool)Call(mip, "GetFirstValue"), Is.EqualTo((mode & 1) != 0));
            }
            var value = new Vector4(-2.25f, 1.375f, .42f, -.31f); Call(st, "Apply", value);
            if (graph) AssertVector(material.GetVector("_BaseMap_ST"), value, "Shared ST callback");
            else AssertVector(new Vector4(material.GetTextureScale("_BaseMap").x, material.GetTextureScale("_BaseMap").y, material.GetTextureOffset("_BaseMap").x, material.GetTextureOffset("_BaseMap").y), value, "Legacy ST callback");
            Call(wrap, "ExecuteReset", false); Call(mip, "ExecuteReset", false); Call(channel, "ExecuteReset", false); Call(st, "ExecuteReset", false);
            Assert.That((int)Call(wrap, "GetFirstMode"), Is.EqualTo(0)); Assert.That((bool)Call(mip, "GetFirstValue"), Is.False); Assert.That((int)Call(channel, "GetFirstChannel"), Is.EqualTo(3));
            if (graph) AssertVector(material.GetVector("_BaseMap_ST"), new Vector4(1, 1, 0, 0), "Shared vectorST reset");
            // Storage/business-setter proof only. Full sampling images are in
            // existing G4GraphSamplingTests, not inferred from this round trip.
        }

        [Test]
        public void GUI1A_VectorSTCallback_TwoTargets_CallerOwnedUndoRedoRestoresIndependentValues()
        {
            var a = NewMaterial(true); var b = NewMaterial(true);
            var first = new Vector4(1, 1, 0, 0); var second = new Vector4(2, 3, .1f, .2f);
            a.SetVector("_BaseMap_ST", first); b.SetVector("_BaseMap_ST", second);
            a.SetTextureScale("_BaseMap", new Vector2(first.x, first.y)); a.SetTextureOffset("_BaseMap", new Vector2(first.z, first.w));
            b.SetTextureScale("_BaseMap", new Vector2(second.x, second.y)); b.SetTextureOffset("_BaseMap", new Vector2(second.z, second.w));
            // MaterialProperty writes invoke the existing Graph/official URP
            // validation callback. Start with its legitimate keyword/pass state,
            // rather than mistaking first-time normalization for an ST mutation.
            var gui = Activator.CreateInstance(FindType("NBShaderEditor.NBShaderGraphGUI"));
            Call(gui, "ValidateMaterial", a); Call(gui, "ValidateMaterial", b);
            var root = NewRoot(a, b); Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
            var st = Field(Field(Field(root, "_mainTexBlock"), "_baseMapGroupItem"), "_scaleOffsetItem");
            var aa = Snapshot.Read(a); var bb = Snapshot.Read(b); var edited = new Vector4(-3, 4, .15f, -.25f);
            AssertVector(a.GetVector("_BaseMap_ST"), first, "Initial vector A"); AssertVector(b.GetVector("_BaseMap_ST"), second, "Initial vector B");
            AssertVector(NativeST(a), first, "Initial native ST A"); AssertVector(NativeST(b), second, "Initial native ST B");
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                // The actual ST business setter uses MaterialProperty. Record an
                // explicit caller transaction, not a fake claim that GUI paint
                // callback/MaterialEditor automatically recorded this operation.
                Undo.RecordObjects(new Object[] { a, b }, "NBFX GUI1A ST backend transaction");
                Call(st, "Apply", edited); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                AssertVector(a.GetVector("_BaseMap_ST"), edited, "ST multi A"); AssertVector(b.GetVector("_BaseMap_ST"), edited, "ST multi B");
                AssertVector(NativeST(a), edited, "ST native alias multi A"); AssertVector(NativeST(b), edited, "ST native alias multi B");
                // Current Unity's MaterialProperty `_BaseMap_ST` writer also
                // updates this texture's native scale/offset. Permit only these
                // two exact aliases, both explicitly asserted above.
                aa.AssertSame(a, "ST other state A", "_BaseMap_ST", "_BaseMap.nativeST"); bb.AssertSame(b, "ST other state B", "_BaseMap_ST", "_BaseMap.nativeST");
                Undo.PerformUndo(); aa.AssertSame(a, "ST undo A"); bb.AssertSame(b, "ST undo B");
                AssertVector(a.GetVector("_BaseMap_ST"), first, "ST vector undo A"); AssertVector(b.GetVector("_BaseMap_ST"), second, "ST vector undo B");
                AssertVector(NativeST(a), first, "ST native undo A"); AssertVector(NativeST(b), second, "ST native undo B");
                Undo.PerformRedo(); AssertVector(a.GetVector("_BaseMap_ST"), edited, "ST redo A"); AssertVector(b.GetVector("_BaseMap_ST"), edited, "ST redo B");
                AssertVector(NativeST(a), edited, "ST native redo A"); AssertVector(NativeST(b), edited, "ST native redo B");
                aa.AssertSame(a, "ST redo other state A", "_BaseMap_ST", "_BaseMap.nativeST"); bb.AssertSame(b, "ST redo other state B", "_BaseMap_ST", "_BaseMap.nativeST");
            }
            finally { Undo.RevertAllDownToGroup(group); }
        }

        static IEnumerable<TestCaseData> SurfaceCases()
        {
            for (int surface = 0; surface <= 1; ++surface)
                for (int blend = 0; blend < 4; ++blend)
                    for (int clip = 0; clip <= 1; ++clip)
                        yield return new TestCaseData(surface, blend, clip).SetName("GUI1A_OfficialSurface_" + surface + "_Blend_" + blend + "_Clip_" + clip);
        }
        [TestCaseSource(nameof(SurfaceCases))]
        public void GUI1A_OfficialSurface16Configurations_BothSyncEntrypointsPreserveAuthority(int surface, int blend, int clip)
        {
            var material = NewMaterial(true); material.SetFloat(Version, GraphSeedVersion(material));
            var root = NewRoot(material); material.SetFloat("_Surface", surface); material.SetFloat("_Blend", blend); material.SetFloat("_AlphaClip", clip);
            var bridge = Activator.CreateInstance(FindType("UnityEditor.Rendering.Universal.ShaderGraph.NBGraphUnlitGUIBridge"));
            Call(bridge, "ValidateMaterial", material); // One exact official baseline, no NB legacy surface implementation.
            var before = Snapshot.Read(material);
            Call(Prop(root, "Context"), "Refresh"); Call(Prop(root, "SyncService"), "SyncMaterialState"); StaticSync(material);
            before.AssertSame(material, "Official URP authority");
            InvokeLegacyCallbacks(Prop(root, "SyncService")); before.AssertSame(material, "Graph legacy-callback guards");
        }

        static void InvokeLegacyCallbacks(object sync)
        {
            Call(sync, "ApplyTransparentMode", EnumArg("TransparentMode", 2)); Call(sync, "ApplyBlendMode", EnumArg("BlendMode", 3));
            Call(sync, "ApplyShaderPass", "DepthOnly", true); Call(sync, "ApplyScreenDistortMode", 2); Call(sync, "ApplyDepthDecalEnabled", true);
            Call(sync, "ApplyPortalState"); Call(sync, "ApplyVatEnabled", true); Call(sync, "ApplyFlipbookEnabled", true);
            Call(sync, "ApplyLightMode", EnumArg("FxLightMode", 4)); Call(sync, "ApplyToggleKeyword", "_NOISEMAP", false); Call(sync, "ApplyStencilPreset", "ParticleBaseDefault");
        }

        [Test]
        public void GUI1A_RealAssetSaveLoadReimport_PreservesRawWordsUIAndNativeFeatureState()
        {
            string project = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\', '/');
            string explicitClone = Environment.GetEnvironmentVariable("NBFX_ISOLATED_PROJECT_DIR");
            bool explicitlyOwnedClone = !string.IsNullOrEmpty(explicitClone) &&
                string.Equals(project, Path.GetFullPath(explicitClone).Replace('\\', '/'), StringComparison.Ordinal) &&
                string.Equals(Path.GetFileName(Path.GetDirectoryName(project)), ".utmp", StringComparison.Ordinal);
            Assert.That(project.StartsWith("/tmp/", StringComparison.Ordinal) || project.StartsWith("/private/tmp/", StringComparison.Ordinal) || explicitlyOwnedClone, Is.True,
                "Persistence fixture refuses to create Assets in the main project. Run the Root-owned isolated clone.");
            string name = "__NBFX_GUI1A_TEMP_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name); assetFolder = "Assets/" + name;
            Assert.That(AssetDatabase.IsValidFolder(assetFolder), Is.True);
            string path = assetFolder + "/Graph.mat";
            var material = NewMaterial(true); material.hideFlags = HideFlags.None; SeedNonDefaultRaw(material, 4, true);
            material.SetFloat("_FxLightMode", 4f);
            var gui = Activator.CreateInstance(FindType("NBShaderEditor.NBShaderGraphGUI"));
            Call(gui, "ValidateMaterial", material); // Persist a valid native/official baseline.
            AssetDatabase.CreateAsset(material, path);
            var root = NewRoot(material); Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.True);
            var before = Snapshot.Read(material); EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            Assert.That(File.Exists(Path.Combine(project, path)), Is.True, "A real serialized .mat file must exist.");
            string yaml = File.ReadAllText(Path.Combine(project, path));
            foreach (string prefix in WordPrefixes) Assert.That(yaml.Contains(prefix + "Lo16") && yaml.Contains(prefix + "Hi16"), Is.True, prefix);
            Assert.That(yaml.Contains(Version), Is.True);
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.That(loaded, Is.Not.Null);
            before.AssertSame(loaded, "Saved/imported asset"); var reloadedRoot = NewRoot(loaded);
            Assert.That((bool)Call(reloadedRoot, "InitializeGraphMainTextureInputs"), Is.True); before.AssertSame(loaded, "Reconstructed saved backend");
            Debug.Log("NBFX_GUI1A_PERSISTENCE actual isolated .mat save/load/force-import passed: " + path);
            // This is actual disk save/import/load, not a claim of Editor restart
            // or a fresh-process deserialization test. Cleanup deletes only GUID folder.
        }

        [TestCase(false)] [TestCase(true)]
        public void GUI1A_MissingOrIntegerFoldSchema_RefusesSharedConstructorWithoutMutation(bool wrongType)
        {
            string properties = "_NB_DistortionMode(\"Mode\",Float)=0 _NB_Flags0Lo16(\"F0L\",Float)=0 _NB_Flags0Hi16(\"F0H\",Float)=0 _NB_Flags1Lo16(\"F1L\",Float)=0 _NB_Flags1Hi16(\"F1H\",Float)=0 ";
            if (wrongType) properties += "_MainTexBigBlockItemFoldOut(\"Fold\",Integer)=1 ";
            properties += "_BaseMapFoldOut(\"Related\",Float)=1 _NB_GraphGUIStateVersion(\"Version\",Float)=0 ";
            string source = "Shader \"Hidden/NBFX/GUI1ASchema" + Guid.NewGuid().ToString("N") + "\" { Properties { " + properties + " } SubShader { Pass { HLSLPROGRAM\n#pragma vertex Vert\n#pragma fragment Frag\nfloat4 Vert(float4 p:POSITION):SV_POSITION{return p;} float4 Frag():SV_Target{return 1;}\nENDHLSL\n} } }";
            var shader = ShaderUtil.CreateShaderAsset(source, false); Assert.That(shader, Is.Not.Null); owned.Add(shader);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            var root = NewRoot(material); var before = Snapshot.Read(material);
            Assert.That((bool)Prop(Prop(root, "Context"), "IsGraphMaterialHost"), Is.True);
            Assert.That((bool)Call(root, "InitializeGraphMainTextureInputs"), Is.False); Assert.That(Ownership(root), Is.Empty);
            StaticSync(material); before.AssertSame(material, "Bad schema never seeds or syncs");
            // Test-only in-memory schema negative; no altered product Graph/shadow properties.
        }
    }
}
