using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Real Native Houdini four-mode compatibility after manual-frame camera draw.
    // No Graph load/warm/import and no simulated Helper or MPB contract.
    public sealed class G4NativeHoudiniSrpDrawDiagnostics
    {
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Modes = { "_HOUDINI_VAT_SOFTBODY", "_HOUDINI_VAT_RIGIDBODY", "_HOUDINI_VAT_DYNAMIC_REMESH", "_HOUDINI_VAT_PARTICLE_SPRITE" };
        readonly List<Object> owned = new List<Object>();
        UnityEngine.SceneManagement.Scene scene;
        G4GraphHoudiniModesTests helper;
        T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        static object Call(MethodInfo m, object obj, params object[] args)
        {
            Assert.That(m, Is.Not.Null);
            try { return m.Invoke(obj, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); throw; }
        }
        static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Instance).GetValue(value);
        [Serializable] sealed class NativeStatus { public bool apiKnown, known, supported; public int code; public string reason, error; }
        static NativeStatus Query(Shader shader)
        {
            var raw = Call(typeof(G4NativeSrpVatDrawDiagnostics).GetMethod("Query", Static), null, shader);
            return new NativeStatus { apiKnown = Field<bool>(raw, "apiKnown"), known = Field<bool>(raw, "known"), supported = Field<bool>(raw, "supported"), code = Field<int>(raw, "code"), reason = Field<string>(raw, "reason"), error = Field<string>(raw, "error") };
        }
        [Serializable] sealed class State
        {
            public string role, state, shaderPath, shaderName, rendererShaderPath;
            public string[] keywords, enabledPasses, shaderErrors;
            public float vatToggle, vatMode, houdiniMode, displayFrame, autoPlayback, firstFrameTime, playbackSpeed, fps;
            public int materialInstance, rendererMaterialInstance, visible;
            public bool finite, nonCDCD;
            public float repeat;
            public NativeStatus before, after;
        }
        [Serializable] sealed class Metric { public string role; public float response, frameResponse, restoredOnDelta; }
        [Serializable] sealed class SourceHash { public string path, sha256; }
        [Serializable] sealed class Report
        {
            public string scope, unity, api, gpu, resolvedPackage;
            public int mode;
            public bool noGraphLoaded, semanticValid, currentBatcherFailed;
            public State[] states;
            public Metric[] metrics;
            public SourceHash[] sourceHashes;
        }
        static float Delta(Color[] a, Color[] b)
        { float d = 0; for (int i = 0; i < a.Length; i++) for (int c = 0; c < 4; c++) d = Mathf.Max(d, Mathf.Abs(a[i][c] - b[i][c])); return d; }
        static bool Finite(Color[] a) => a.All(p => Enumerable.Range(0, 4).All(c => !float.IsNaN(p[c]) && !float.IsInfinity(p[c])));
        static bool NonCDCD(Color[] a) => !a.All(p => Enumerable.Range(0, 4).All(c => p[c] == -23.203125f));
        static int Visible(Color[] a, Color[] b) => Enumerable.Range(0, a.Length).Count(i => Mathf.Max(Mathf.Abs(a[i].r - b[i].r), Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b)) > .001f);
        Texture2D Texture(int size, Func<int, int, Color> pixel)
            => (Texture2D)Call(typeof(G4GraphHoudiniModesTests).GetMethod("Texture", Instance), helper, size, pixel);
        [TearDown] public void Clean()
        {
            if (helper != null) { helper.Clean(); helper = null; }
            if (scene.IsValid()) { EditorSceneManager.ClosePreviewScene(scene); scene = default(UnityEngine.SceneManagement.Scene); }
            for (int i = owned.Count - 1; i >= 0; --i) if (owned[i]) Object.DestroyImmediate(owned[i]); owned.Clear();
        }
        [TestCase(0, TestName = "G4NativeHoudiniSRP_softbody_manual_off_on")]
        [TestCase(1, TestName = "G4NativeHoudiniSRP_rigidbody_manual_off_on")]
        [TestCase(2, TestName = "G4NativeHoudiniSRP_remesh_manual_off_on")]
        [TestCase(3, TestName = "G4NativeHoudiniSRP_sprite_manual_off_on")]
        public void ActualNativeHoudiniModesAfterManualDraw(int mode)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; Assert.That(pipeline, Is.Not.Null);
            var data = pipeline.rendererDataList[0] as UniversalRendererData; Assert.That(data, Is.Not.Null);
            Assert.That(data.transparentLayerMask.value & (1 << 4), Is.Not.Zero);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            string project = Path.GetDirectoryName(Application.dataPath), path = Path.Combine(project, AssetDatabase.GetAssetPath(data)); byte[] beforeData = File.ReadAllBytes(path);
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ?? Path.Combine(project, "Temp/NBFXNativeHoudiniSRP"), "mode" + mode); Directory.CreateDirectory(folder);
            scene = EditorSceneManager.NewPreviewScene(); helper = new G4GraphHoudiniModesTests();
            var actor = Keep(GameObject.CreatePrimitive(PrimitiveType.Quad)); actor.layer = 4;
            var mesh = Keep(Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh)); actor.GetComponent<MeshFilter>().sharedMesh = mesh;
            mesh.SetUVs(0, new List<Vector4> { new Vector4(.12f, .25f, 0, 0), new Vector4(.88f, .25f, 0, 0), new Vector4(.12f, .9f, 0, 0), new Vector4(.88f, .9f, 0, 0) });
            mesh.SetUVs(1, mode == 3 ? Enumerable.Repeat(new Vector4(.4f, .7f, 0, 0), 4).ToList() : new List<Vector4> { new Vector4(.2f, .66f, 0, 0), new Vector4(.8f, .66f, 0, 0), new Vector4(.2f, .86f, 0, 0), new Vector4(.8f, .86f, 0, 0) });
            mesh.SetUVs(2, Enumerable.Repeat(Vector4.zero, 4).ToList()); mesh.SetUVs(4, Enumerable.Repeat(new Vector4(0, 1, 0, 0), 4).ToList());
            actor.transform.rotation = Quaternion.Euler(4, 17, 3); actor.transform.localScale = new Vector3(1.65f, 1.45f, 1.2f);
            var renderer = actor.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var cameraObject = Keep(new GameObject("Native Houdini manual SRP camera")); var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            foreach (var go in new[] { actor, cameraObject }) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene); camera.scene = scene;
            camera.orthographic = true; camera.orthographicSize = 1.4f; camera.fieldOfView = 42; camera.nearClipPlane = .1f; camera.farClipPlane = 20;
            camera.transform.position = new Vector3(.08f, .04f, 4); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
            camera.cullingMask = 1 << 4; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.0625f, .125f, .1875f, 1); camera.allowHDR = true; camera.allowMSAA = false;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var map = Texture(8, (x, y) => ((x / 2 + y / 2) & 1) == 0 ? new Color(.85f, .32f, .12f, 1) : new Color(.18f, .72f, .83f, 1));
            var pos = mode == 0 ? Keep((Texture2D)Call(typeof(G4GraphVATTests).GetMethod("PositionMap", Static), null)) : Texture(8, (x, y) => new Color(.3f + .055f * x, .27f + .06f * y, .46f + .015f * ((x + 2 * y) % 5), .1f));
            var second = Texture(2, (x, y) => new Color(1, .25f, .75f, 1)); var rot = Texture(2, (x, y) => mode == 1 ? new Color(.58f, .55f, .48f, 1) : new Color(.5f, .5f, .5f, 1));
            var heading = Texture(2, (x, y) => new Color(.4f, .7f, .1f, 1)); var lookup = Texture(8, (x, y) => new Color(.08f + .12f * x, 0, 1 - (.08f + .12f * y), 0));
            string[] shaderPaths = { Package + "NBShaders2/Shader/NBShader.shader", Package + "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader" };
            var materials = shaderPaths.Select(p => Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(p)) { hideFlags = HideFlags.HideAndDontSave })).ToArray();
            var rt = Keep(new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)); var read = Keep(new Texture2D(128, 128, TextureFormat.RGBAHalf, false, true)); camera.targetTexture = rt;
            var old = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation; var nb = data.rendererFeatures.FirstOrDefault(f => f && f.GetType().FullName == "NBShader.NBPostProcess"); bool active = nb && nb.isActive;
            var configure = typeof(G4GraphVATTests).GetMethod("Configure", Static); var draw = typeof(G4GraphVATTests).GetMethod("Draw", Static);
            var rows = new List<State>(); var metrics = new List<Metric>();
            Color[] Capture(Material m, string label)
            {
                renderer.sharedMaterial = m; for (int i = 0; i < 3; i++) camera.Render();
                var pixels = (Color[])Call(draw, null, renderer, m, camera, rt, read, folder, label); Assert.That(renderer.sharedMaterial, Is.SameAs(m)); return pixels;
            }
            try
            {
                ShaderUtil.allowAsyncCompilation = false; if (nb) nb.SetActive(false); Assert.That(rt.Create() && !rt.sRGB, Is.True);
                foreach (var m in materials)
                {
                    Call(configure, null, m, false, "manual2", map, pos, second, rot, map, map, map);
                    m.SetFloat("_VATMode", 0); m.SetFloat("_HoudiniVATSubMode", mode); m.SetTexture("_colTexture", heading); m.SetTexture("_lookupTable", lookup);
                    m.SetFloat("_B_autoPlayback", 0); m.SetFloat("_gameTimeAtFirstFrame", 0); m.SetFloat("_playbackSpeed", 1); m.SetFloat("_houdiniFPS", 24);
                    m.SetFloat("_B_interpolate", 0); m.SetFloat("_animateFirstFrame", 1); m.SetFloat("_globalPscaleMul", 1); m.SetFloat("_B_pscaleAreInPosA", 0);
                    m.SetFloat("_widthBaseScale", 1.1f); m.SetFloat("_heightBaseScale", .95f); m.SetFloat("_B_hideOverlappingOrigin", 0); m.SetFloat("_originRadius", .02f);
                    m.SetFloat("_B_CAN_SPIN", 0); m.SetFloat("_B_spinFromHeading", 0); m.SetFloat("_B_LOAD_COL_TEX", 0); m.SetFloat("_spinPhase", .17f); m.SetFloat("_scaleByVelAmount", 1.3f);
                }
                renderer.enabled = false; var background = Capture(materials[0], "background"); var br = Capture(materials[0], "background-repeat"); renderer.enabled = true;
                for (int index = 0; index < 2; index++)
                {
                    var m = materials[index]; string role = index == 0 ? "current" : "frozen"; var frames = new List<Color[]>();
                    foreach (string state in new[] { "off", "on", "frame1", "on-restored" })
                    {
                        foreach (string k in Modes.Concat(new[] { "_VAT", "_VAT_HOUDINI", "_VAT_TYFLOW" })) m.DisableKeyword(k);
                        bool enabled = state != "off"; m.SetFloat("_VAT_Toggle", enabled ? 1 : 0); m.SetFloat("_displayFrame", state == "frame1" ? 1 : 2);
                        if (enabled) { m.EnableKeyword("_VAT"); m.EnableKeyword("_VAT_HOUDINI"); m.EnableKeyword(Modes[mode]); }
                        renderer.sharedMaterial = m;
                        var row = new State { role = role, state = state, shaderPath = AssetDatabase.GetAssetPath(m.shader), shaderName = m.shader.name, materialInstance = m.GetInstanceID(), keywords = m.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
                            vatToggle = m.GetFloat("_VAT_Toggle"), vatMode = m.GetFloat("_VATMode"), houdiniMode = m.GetFloat("_HoudiniVATSubMode"), displayFrame = m.GetFloat("_displayFrame"), autoPlayback = m.GetFloat("_B_autoPlayback"), firstFrameTime = m.GetFloat("_gameTimeAtFirstFrame"), playbackSpeed = m.GetFloat("_playbackSpeed"), fps = m.GetFloat("_houdiniFPS"),
                            enabledPasses = Enumerable.Range(0, m.passCount).Select(i => m.GetPassName(i)).Where(p => m.GetShaderPassEnabled(p)).ToArray(), before = Query(m.shader) };
                        var f = Capture(m, role + "-" + state); var repeat = Capture(m, role + "-" + state + "-repeat"); frames.Add(f);
                        row.after = Query(m.shader); row.rendererMaterialInstance = renderer.sharedMaterial.GetInstanceID(); row.rendererShaderPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.shader);
                        row.shaderErrors = ShaderUtil.GetShaderMessages(m.shader).Where(s => s.severity.ToString() == "Error").Select(s => s.message).ToArray(); row.finite = Finite(f) && Finite(repeat); row.nonCDCD = NonCDCD(f) && NonCDCD(repeat); row.repeat = Delta(f, repeat); row.visible = Visible(f, background); rows.Add(row);
                    }
                    metrics.Add(new Metric { role = role, response = Delta(frames[0], frames[1]), frameResponse = Delta(frames[1], frames[2]), restoredOnDelta = Delta(frames[1], frames[3]) });
                }
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(shaderPaths[0]); Assert.That(packageInfo, Is.Not.Null);
                string[] sourcePaths = { "NBShaders2/Shader/HLSL/NBShaderInput.hlsl", "NBShaders2/Shader/NBShader.shader", "XuanXuanRenderUtility/Shader/HLSL/HoudiniVAT.hlsl", "XuanXuanRenderUtility/Shader/HLSL/HoudiniVATKernelV1.hlsl", "XuanXuanRenderUtility/Shader/HLSL/HoudiniVATMathV1.hlsl", "Tests/URP/Editor/G4GraphVATTests.cs", "Tests/URP/Editor/G4GraphHoudiniModesTests.cs", "Tests/URP/Editor/G4NativeSrpVatDrawDiagnostics.cs" };
                string HashFile(string file) { using (var hash = SHA256.Create()) return string.Concat(hash.ComputeHash(File.ReadAllBytes(file)).Select(b => b.ToString("x2"))); }
                var report = new Report { scope = "Native current/Frozen fourHoudini mode manual/stable clocks actualcamera draw; subShader0 compatibility global, not perpass or measured batching/performance.", unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName, mode = mode, noGraphLoaded = true, states = rows.ToArray(), metrics = metrics.ToArray(), resolvedPackage = packageInfo.resolvedPath,
                    sourceHashes = sourcePaths.Select(p => new SourceHash { path = p, sha256 = HashFile(Path.Combine(packageInfo.resolvedPath, p)) }).ToArray() };
                report.semanticValid = Finite(background) && NonCDCD(background) && Delta(background, br) == 0 && rows.All(r => r.finite && r.nonCDCD && r.repeat == 0 && r.visible > 150) && metrics.All(m => m.response > .01f && m.frameResponse > .005f && m.restoredOnDelta == 0);
                report.currentBatcherFailed = rows.Where(r => r.role == "current").Any(r => !r.after.apiKnown || !r.after.known || !r.after.supported || r.after.code != 0);
                File.WriteAllText(Path.Combine(folder, "native-houdini-srp-draw.json"), JsonUtility.ToJson(report, true));
                Debug.Log("NBFX_NATIVE_HOUDINI_SRP mode=" + mode + " semantic=" + report.semanticValid + " currentBatcherFailed=" + report.currentBatcherFailed);
                Assert.That(report.semanticValid, Is.True, "RealHoudini draws must be finite/visible/repeatable and independently respond to VAT and frame control.");
                foreach (var m in metrics) Assert.That(m.restoredOnDelta, Is.Zero, m.role + " original on-frame must restore exactly after frame1 control.");
                foreach (var r in rows) { Assert.That(r.materialInstance, Is.EqualTo(r.rendererMaterialInstance)); Assert.That(r.shaderPath, Is.EqualTo(r.rendererShaderPath)); Assert.That(r.shaderErrors, Is.Empty); Assert.That(r.after.apiKnown && r.after.known && r.after.supported, Is.True, r.after.error); }
                Assert.That(rows.Where(r => r.role == "frozen").All(r => r.after.code == 0 || (r.after.code == 10 && !string.IsNullOrEmpty(r.after.reason))), Is.True);
                Assert.That(report.currentBatcherFailed, Is.False, "Strict initialized current Houdini compatibility must be0; actualnonzero native state retained.");
            }
            finally { if (nb) nb.SetActive(active); ShaderUtil.allowAsyncCompilation = async; camera.targetTexture = null; RenderTexture.active = old; rt.Release(); Clean(); Assert.That(File.ReadAllBytes(path), Is.EqualTo(beforeData)); }
        }
    }
}
