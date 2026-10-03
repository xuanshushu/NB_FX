using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Shared test-only harness. No product implementation or saved scene/settings.
    internal static class G4SpecDebugFixture
    {
        internal const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        internal const string Version = "_NB_GraphGUIStateVersion";
        internal static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal static readonly string[] DebugProps = { "_NB_Debug_Mask", "_NB_Debug_PNoise", "_NB_Debug_Dissolve", "_NB_Debug_Distort", "_NB_Debug_Fresnel", "_NB_Debug_VertexOffset" };
        internal static readonly string[] DebugKeywords = { "NB_DEBUG_MASK", "NB_DEBUG_PNOISE", "NB_DEBUG_DISSOLVE", "NB_DEBUG_DISTORT", "NB_DEBUG_FRESNEL", "NB_DEBUG_VERTEX_OFFSET" };
        internal static readonly string[] Parents = { "_Mask_Toggle", "_ProgramNoise_Toggle", "_Dissolve_Toggle", "_noisemapEnabled", "_fresnelEnabled", "_VertexOffset_Toggle" };
        internal static readonly string[] ParentKeywords = { "_MASKMAP_ON", "_PROGRAM_NOISE", "_DISSOLVE", "_NOISEMAP", "_FRESNEL", "_VERTEX_OFFSET" };

        internal static Type FindType(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, name); return type;
        }
        internal static void Sync(Material material)
        {
            var method = FindType("NBShaderEditor.NBShaderGraphGUI").GetMethod("SyncSixWayKeywords", Static);
            Assert.That(method, Is.Not.Null); method.Invoke(null, new object[] { material });
        }
        internal static void Validate(Material material)
        {
            var type = FindType("NBShaderEditor.NBShaderGraphGUI");
            type.GetMethod("ValidateMaterial").Invoke(Activator.CreateInstance(type), new object[] { material });
        }
        internal static bool Read(Material material, out object result, out string[] unavailable)
        {
            var method = FindType("NBShader.NBShaderMaterialIntentResolver").GetMethod("TryResolveGraphSupportedKeywordIntent", Static);
            Assert.That(method, Is.Not.Null);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), 3), null, null, null };
            bool accepted = (bool)method.Invoke(null, args); result = args[3]; unavailable = (string[])args[4]; return accepted;
        }
        internal static Material NewGraph()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            Assert.That(material.HasProperty(Version), Is.True); material.SetFloat(Version, 2); return material;
        }
        internal static void PreflightImport()
        {
            string expected = Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets");
            Assert.That(Path.GetFullPath(Application.dataPath), Is.EqualTo(expected).IgnoreCase, "Only the exclusive isolation Editor may run this fixture.");
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                Assert.That((scene.name + "/" + scene.path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), "TAI reload protection");
            }
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        internal static Dictionary<string, string> Properties(Material material)
        {
            var values = new Dictionary<string, string>();
            for (int i = 0; i < material.shader.GetPropertyCount(); ++i)
            {
                string name = material.shader.GetPropertyName(i);
                switch (material.shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Float: case ShaderPropertyType.Range: values[name] = material.GetFloat(name).ToString("R", System.Globalization.CultureInfo.InvariantCulture); break;
                    case ShaderPropertyType.Int: values[name] = material.GetInteger(name).ToString(); break;
                    case ShaderPropertyType.Vector: values[name] = material.GetVector(name).ToString("R"); break;
                    case ShaderPropertyType.Color: values[name] = material.GetColor(name).ToString("R"); break;
                    case ShaderPropertyType.Texture: values[name] = (material.GetTexture(name) ? material.GetTexture(name).GetInstanceID().ToString() : "null") + material.GetTextureScale(name) + material.GetTextureOffset(name); break;
                }
            }
            return values;
        }
        internal static void SetKeyword(Material material, string name, bool enabled)
        { if (enabled) material.EnableKeyword(name); else material.DisableKeyword(name); }

        [Serializable] internal sealed class Metrics
        {
            public string caseId, scope, unityVersion, api, graphPassTag, nativePassTag;
            public bool finite;
            public float[] ab, bc, repeat, response, restore;
            public int[] visible;
            public string[] finalKeywords;
            public bool[] forwardEnabled;
        }
        internal static float Delta(Color[] a, Color[] b)
        {
            float max = 0;
            for (int i = 0; i < a.Length; ++i) for (int k = 0; k < 4; ++k) max = Mathf.Max(max, Mathf.Abs(a[i][k] - b[i][k]));
            return max;
        }
        internal static bool Finite(Color[] a) => a.All(p => Enumerable.Range(0, 4).All(k => !float.IsNaN(p[k]) && !float.IsInfinity(p[k])));
        internal static int Visible(Color[] a, Color[] background) => Enumerable.Range(0, a.Length).Count(i => Enumerable.Range(0, 4).Any(k => a[i][k] != background[i][k]));

        [Serializable] internal sealed class InitializationShaderStatus
        {
            public string role, shaderName, assetPath, assetGUID, error;
            public int shaderInstanceID, activeSubshader, materialPassCount, forwardIndex;
            public bool supported;
            public string[] passNames, lightModes, keywords, compilerMessages;
            public float alphaAll, alphaClip, surface, colorAlpha;
        }
        [Serializable] internal sealed class InitializationStage
        {
            public string pipeline, globalPipelineTag;
            public InitializationShaderStatus[] shaders;
        }
        [Serializable] internal sealed class InitializationStatus
        {
            public string scope = "Real empty-background pipeline initialization only; initial and post-warm states retained, no lighting/parity evidence.";
            public string fixtureSourceAssetPath, fixtureSourceAbsolutePath, fixtureSourceSHA256, error;
            public InitializationStage before, after;
            public int backgroundRenderCalls, cullingMaskBefore, cullingMaskDuring, cullingMaskAfter;
            public bool rendererEnabledBefore, rendererEnabledDuring, rendererEnabledAfter;
        }
        internal static InitializationStage CaptureInitialization(Material[] materials)
        {
            var stage = new InitializationStage { pipeline = RenderPipelineManager.currentPipeline == null ? "null" : RenderPipelineManager.currentPipeline.GetType().FullName,
                globalPipelineTag = Shader.globalRenderPipeline };
            stage.shaders = materials.Select((material, index) => {
                var row = new InitializationShaderStatus { role = "ABC"[index].ToString() };
                try
                {
                    var shader = material.shader; row.shaderName = shader.name; row.shaderInstanceID = shader.GetInstanceID();
                    row.assetPath = AssetDatabase.GetAssetPath(shader); row.assetGUID = AssetDatabase.AssetPathToGUID(row.assetPath); row.supported = shader.isSupported;
                    row.activeSubshader = ShaderUtil.GetShaderData(shader).ActiveSubshaderIndex; row.materialPassCount = material.passCount;
                    row.forwardIndex = material.FindPass(index == 2 ? "Universal Forward" : "UniversalForward");
                    row.passNames = Enumerable.Range(0, material.passCount).Select(material.GetPassName).ToArray();
                    row.lightModes = Enumerable.Range(0, material.passCount).Select(p => shader.FindPassTagValue(p, new ShaderTagId("LightMode")).name).ToArray();
                    row.keywords = material.shaderKeywords.OrderBy(k => k).ToArray();
                    row.compilerMessages = ShaderUtil.GetShaderMessages(shader).Select(m => m.severity + ":" + m.file + ":" + m.line + ":" + m.message).ToArray();
                    row.alphaAll = material.HasProperty("_AlphaAll") ? material.GetFloat("_AlphaAll") : 0;
                    row.alphaClip = material.HasProperty("_AlphaClip") ? material.GetFloat("_AlphaClip") : 0;
                    row.surface = material.HasProperty("_Surface") ? material.GetFloat("_Surface") : 0;
                    string color = index == 2 ? "_Color" : "_BaseColor"; row.colorAlpha = material.HasProperty(color) ? material.GetColor(color).a : 0;
                }
                catch (Exception exception) { row.error = exception.ToString(); }
                return row;
            }).ToArray();
            return stage;
        }

        internal sealed class Harness : IDisposable
        {
            internal readonly Material[] materials;
            internal readonly MeshRenderer renderer;
            internal readonly Camera camera;
            internal readonly string folder;
            readonly List<Object> owned = new List<Object>();
            readonly RenderTexture rt;
            readonly Texture2D read;
            readonly RenderTexture oldRT;
            readonly bool oldAsync, oldFog;
            readonly AmbientMode oldAmbient;
            readonly SphericalHarmonicsL2 oldProbe;
            internal readonly string[] tags = new string[3];
            const int Size = 96, Layer = 4;
            T Keep<T>(T item) where T : Object { owned.Add(item); return item; }
            internal Texture2D Constant(Color color)
            {
                var texture = Keep(new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true));
                texture.SetPixel(0, 0, color); texture.Apply(false); texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Repeat; return texture;
            }
            internal Harness(string id, bool ortho)
            {
                oldRT = RenderTexture.active; oldAsync = ShaderUtil.allowAsyncCompilation;
                oldFog = RenderSettings.fog; oldAmbient = RenderSettings.ambientMode; oldProbe = RenderSettings.ambientProbe;
                try
                {
                Assert.That(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset, Is.True);
                ShaderUtil.allowAsyncCompilation = false; RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Custom; RenderSettings.ambientProbe = new SphericalHarmonicsL2();
                string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXSpecDebug");
                folder = Path.Combine(root, id); Directory.CreateDirectory(folder);
                string[] paths = { "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader", "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader", GraphPath };
                materials = paths.Select(p => { var shader = AssetDatabase.LoadAssetAtPath<Shader>(p); Assert.That(shader && shader.isSupported, Is.True, p); return Keep(new Material(shader) { hideFlags = HideFlags.HideAndDontSave }); }).ToArray();
                materials[2].SetFloat(Version, 2);
                var scene = SceneManager.GetActiveScene(); Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
                var quad = Keep(GameObject.CreatePrimitive(PrimitiveType.Quad)); quad.layer = Layer;
                quad.transform.localScale = new Vector3(2, 2, 1); quad.transform.rotation = Quaternion.Euler(0, 18, 0);
                renderer = quad.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                var cameraGO = Keep(new GameObject("Specular/Debug actual Mesh camera")); camera = cameraGO.AddComponent<Camera>();
                camera.scene = scene; camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 43;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20; camera.transform.position = new Vector3(0, 0, 5); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                camera.cullingMask = 1 << Layer; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear; camera.allowHDR = true; camera.allowMSAA = false;
                cameraGO.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                var sunGO = Keep(new GameObject("Specular directional control", typeof(Light))); var sun = sunGO.GetComponent<Light>();
                sun.type = LightType.Directional; sun.color = new Color(.9f, .8f, .7f); sun.intensity = 2; sun.shadows = LightShadows.None;
                sun.transform.rotation = Quaternion.LookRotation(new Vector3(-.4f, -.2f, -1).normalized);
                foreach (var go in new[] { quad, cameraGO, sunGO }) SceneManager.MoveGameObjectToScene(go, scene);
                rt = Keep(new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear));
                read = Keep(new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true)); rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True); camera.targetTexture = rt;
                // Camera.Render establishes the actual URP runtime before Material
                // FindPass selects its active SubShader. No visible Mesh is drawn here.
                var initialization = new InitializationStatus {
                    fixtureSourceAssetPath = "Packages/com.xuanxuan.nb.fx/Tests/URP/Editor/G4GraphSpecularDebugFixture.cs",
                    fixtureSourceAbsolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Packages/NB_FX/Tests/URP/Editor/G4GraphSpecularDebugFixture.cs")),
                    rendererEnabledBefore = renderer.enabled, cullingMaskBefore = camera.cullingMask,
                    before = CaptureInitialization(materials) };
                using (var hash = SHA256.Create()) initialization.fixtureSourceSHA256 = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(initialization.fixtureSourceAbsolutePath))).Replace("-", "").ToLowerInvariant();
                try
                {
                    renderer.enabled = false; camera.cullingMask = 0;
                    initialization.rendererEnabledDuring = renderer.enabled; initialization.cullingMaskDuring = camera.cullingMask;
                    for (int frame = 0; frame < 4; ++frame) { camera.Render(); ++initialization.backgroundRenderCalls; }
                }
                catch (Exception exception) { initialization.error = exception.ToString(); throw; }
                finally
                {
                    renderer.enabled = initialization.rendererEnabledBefore; camera.cullingMask = initialization.cullingMaskBefore;
                    initialization.rendererEnabledAfter = renderer.enabled; initialization.cullingMaskAfter = camera.cullingMask;
                    initialization.after = CaptureInitialization(materials);
                    File.WriteAllText(Path.Combine(folder, "beforeafterAlpha_Status.json"), JsonUtility.ToJson(initialization, true));
                }
                var baseMap = Constant(new Color(.2f, .4f, .6f, 1));
                for (int m = 0; m < 3; ++m)
                {
                    typeof(G4GraphLightingTests).GetMethod("Configure", Static).Invoke(null, new object[] { materials[m], m == 2, 0, baseMap });
                    if (m == 2) materials[m].SetFloat("_NB_DistortionMode", 0);
                    foreach (string property in Parents.Concat(new[] { "_VAT_Toggle", "_BlinnPhongSpecularToggle", "_FlipbookBlending", "_BumpMapToggle", "_MatCapToggle" }))
                        if (materials[m].HasProperty(property)) materials[m].SetFloat(property, 0);
                    RestoreForward(materials[m], m == 2); tags[m] = ResolveForwardTag(materials[m], m == 2);
                }
                }
                catch { Dispose(); throw; }
            }
            static string ResolveForwardTag(Material material, bool graph)
            {
                int index = material.FindPass(graph ? "Universal Forward" : "UniversalForward");
                string inventory = string.Join(",", Enumerable.Range(0, material.passCount).Select(i => i + ":" + material.GetPassName(i)));
                Assert.That(index, Is.GreaterThanOrEqualTo(0), "shader=" + material.shader.name + ";graph=" + graph + ";passes=" + inventory + ";keywords=" + string.Join(",", material.shaderKeywords));
                string tag = material.shader.FindPassTagValue(index, new ShaderTagId("LightMode")).name;
                return string.IsNullOrEmpty(tag) ? "SRPDefaultUnlit" : tag;
            }
            internal static void RestoreForward(Material material, bool graph)
            {
                string tag = ResolveForwardTag(material, graph);
                foreach (string pass in new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward", "DepthOnly", "DepthNormalsOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" }) material.SetShaderPassEnabled(pass, false);
                material.SetShaderPassEnabled(tag, true); Assert.That(material.GetShaderPassEnabled(tag), Is.True, tag);
                material.SetFloat("_Cull", 0); material.SetFloat("_ZWrite", 0); material.SetFloat("_SrcBlend", 1); material.SetFloat("_DstBlend", 0);
                if (graph) { material.SetFloat("_SrcBlendAlpha", 1); material.SetFloat("_DstBlendAlpha", 0); }
                material.renderQueue = 3000;
            }
            internal Color[] Snap(string label, Material material = null)
            {
                renderer.enabled = material != null; if (material) renderer.sharedMaterial = material;
                for (int i = 0; i < 3; ++i) camera.Render();
                RenderTexture.active = rt; read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0, false); read.Apply(false, false);
                var pixels = read.GetPixels();
                using (var file = File.Create(Path.Combine(folder, label + ".rgba-f32.gz")))
                using (var zip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
                using (var writer = new BinaryWriter(zip)) foreach (var p in pixels) { writer.Write(p.r); writer.Write(p.g); writer.Write(p.b); writer.Write(p.a); }
                return pixels;
            }
            internal void SaveAndAssert(Metrics metrics)
            {
                metrics.unityVersion = Application.unityVersion; metrics.api = SystemInfo.graphicsDeviceType.ToString(); metrics.nativePassTag = tags[1]; metrics.graphPassTag = tags[2];
                metrics.finalKeywords = materials.Select(m => string.Join(",", m.shaderKeywords.OrderBy(k => k))).ToArray();
                metrics.forwardEnabled = Enumerable.Range(0, 3).Select(i => materials[i].GetShaderPassEnabled(tags[i])).ToArray();
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(metrics, true)); Debug.Log("NBFX_SPEC_DEBUG " + JsonUtility.ToJson(metrics));
                foreach (var material in materials)
                    Assert.That(ShaderUtil.GetShaderMessages(material.shader).Any(m => m.severity.ToString() == "Error"), Is.False, material.shader.name);
                Assert.That(metrics.finite, Is.True); Assert.That(metrics.ab.All(v => v == 0) && metrics.bc.All(v => v == 0) && metrics.repeat.All(v => v == 0), Is.True, "Strict zero AB/BC/repeat; retain all differences as failures.");
                Assert.That(metrics.visible.All(v => v > 128), Is.True, "All compared state outputs must be visible.");
                if (metrics.restore != null) Assert.That(metrics.restore.All(v => v == 0), Is.True);
            }
            internal GameObject AddProbe()
            {
                var probe = Keep(GameObject.CreatePrimitive(PrimitiveType.Quad)); probe.layer = Layer; probe.transform.position = new Vector3(0, 0, -1); probe.transform.localScale = new Vector3(3, 3, 1);
                var material = Keep(new Material(Shader.Find("Universal Render Pipeline/Unlit"))); material.SetColor("_BaseColor", Color.green); material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0); material.SetFloat("_ZTest", (float)CompareFunction.LessEqual); material.SetFloat("_Cull", 0); material.SetFloat("_SrcBlend", 1); material.SetFloat("_DstBlend", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3001;
                probe.GetComponent<MeshRenderer>().sharedMaterial = material; probe.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                SceneManager.MoveGameObjectToScene(probe, camera.scene); return probe;
            }
            public void Dispose()
            {
                if (camera) camera.targetTexture = null; if (rt) rt.Release(); RenderTexture.active = oldRT; ShaderUtil.allowAsyncCompilation = oldAsync;
                RenderSettings.fog = oldFog; RenderSettings.ambientMode = oldAmbient; RenderSettings.ambientProbe = oldProbe;
                for (int i = owned.Count - 1; i >= 0; --i) if (owned[i]) Object.DestroyImmediate(owned[i]);
            }
        }
    }
}
