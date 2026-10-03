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
    // Native only: actual current/Frozen Forward Mesh draws, VAT off/on/control.
    // ShaderUtil code is subShader0 metadata, never a pass/variant timing result.
    public sealed class G4NativeSrpVatDrawDiagnostics
    {
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        const string CurrentPath = Package + "NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = Package + "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] VATKeywords = { "_VAT", "_VAT_HOUDINI", "_VAT_TYFLOW", "_HOUDINI_VAT_SOFTBODY", "_HOUDINI_VAT_RIGIDBODY", "_HOUDINI_VAT_DYNAMIC_REMESH", "_HOUDINI_VAT_PARTICLE_SPRITE", "_TYFLOW_VAT_ABSOLUTE", "_TYFLOW_VAT_RELATIVE", "_TYFLOW_VAT_SKIN_R", "_TYFLOW_VAT_SKIN_PR", "_TYFLOW_VAT_SKIN_PRSAVE", "_TYFLOW_VAT_SKIN_PRSXYZ" };
        readonly List<Object> owned = new List<Object>();
        UnityEngine.SceneManagement.Scene previewScene;
        G4GraphTyflowVATTests vatHelper;
        T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        static object Call(MethodInfo method, object target, object[] args)
        {
            Assert.That(method, Is.Not.Null);
            try { return method.Invoke(target, args); }
            catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
        }
        static string SHA(byte[] bytes)
        { using (var h = SHA256.Create()) return string.Concat(h.ComputeHash(bytes).Select(x => x.ToString("x2"))); }
        static string PixelSHA(Texture2D texture)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            { foreach (var p in texture.GetPixels()) { writer.Write(p.r); writer.Write(p.g); writer.Write(p.b); writer.Write(p.a); } writer.Flush(); return SHA(stream.ToArray()); }
        }
        [TearDown] public void EmergencyCleanup()
        {
            if (vatHelper != null) { vatHelper.Cleanup(); vatHelper = null; }
            if (previewScene.IsValid()) { EditorSceneManager.ClosePreviewScene(previewScene); previewScene = default(UnityEngine.SceneManagement.Scene); }
            for (int i = owned.Count - 1; i >= 0; --i) if (owned[i]) Object.DestroyImmediate(owned[i]); owned.Clear();
        }
        [Serializable] sealed class SourceHash { public string path, sha256; }
        [Serializable] sealed class NativeStatus
        {
            public bool apiKnown, known, supported;
            public int code = -1, subShader = 0;
            public string codeAPI, reasonAPI, reason, error;
        }
        [Serializable] sealed class PassState { public string name, lightMode; public int index; public bool enabled; }
        [Serializable] sealed class IntegerState { public string name; public int value; }
        [Serializable] sealed class DrawState
        {
            public string role, state, shaderName, shaderAssetPath, materialName, actualRendererShader, actualRendererShaderAssetPath;
            public string[] keywords, shaderErrors;
            public int materialInstance, rendererMaterialInstance, vatWidth, vatHeight;
            public float vatToggle, vatMode, houdiniSubMode, tyflowSubMode, frame, frames, autoplay, frameInterpolation, importScale;
            public bool actualRendererMaterialMatches, finite, nonCDCD, nativeBatcherFailed;
            public float repeat, deltaFromBackground;
            public int visible;
            public NativeStatus beforeDraw, afterDraw;
            public PassState[] passes;
            public IntegerState[] protocolIntegers;
        }
        [Serializable] sealed class RoleMetrics
        {
            public string role;
            public float vatOnOffResponse, frameResponse;
            public bool currentAllDrawStatesCode0, frozenKnownCompatibleOrOriginal10;
        }
        [Serializable] sealed class Report
        {
            public string scope, unity, api, gpu, resolvedPackage, vatTextureSHA256;
            public bool orthographic, srpBatcherEnabled, noGraphWarmOrImport, semanticEvidenceValid, currentBatcherFailed;
            public string currentBatcherFailure;
            public SourceHash[] sourceHashes;
            public DrawState[] draws;
            public RoleMetrics[] metrics;
        }
        static NativeStatus Query(Shader shader)
        {
            var result = new NativeStatus { supported = shader && shader.isSupported };
            var code = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", Static, null, new[] { typeof(Shader), typeof(int) }, null);
            var reason = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", Static, null, new[] { typeof(Shader), typeof(int), typeof(int) }, null);
            result.codeAPI = code == null ? "missing" : code.ToString(); result.reasonAPI = reason == null ? "missing" : reason.ToString();
            result.apiKnown = code != null && code.ReturnType == typeof(int) && reason != null && reason.ReturnType == typeof(string);
            try
            {
                if (!result.apiKnown || !shader) throw new InvalidOperationException("Native ShaderUtil API/shader unavailable.");
                result.code = (int)Call(code, null, new object[] { shader, 0 });
                result.reason = result.code == 0 ? "" : (string)Call(reason, null, new object[] { shader, 0, result.code });
                result.known = result.code >= 0 && (result.code == 0 || !string.IsNullOrEmpty(result.reason));
            }
            catch (Exception error) { result.error = error.ToString(); }
            return result;
        }
        static PassState[] Passes(Material material)
        {
            return Enumerable.Range(0, material.passCount).Select(i => new PassState {
                index = i, name = material.GetPassName(i),
                lightMode = material.shader.FindPassTagValue(0, i, new ShaderTagId("LightMode")).name,
                enabled = material.GetShaderPassEnabled(material.GetPassName(i)) }).ToArray();
        }
        static float Delta(Color[] a, Color[] b)
        { float d = 0; for (int i = 0; i < a.Length; ++i) for (int c = 0; c < 4; ++c) d = Mathf.Max(d, Mathf.Abs(a[i][c] - b[i][c])); return d; }
        static bool Finite(Color[] a) => a.All(p => Enumerable.Range(0, 4).All(c => !float.IsNaN(p[c]) && !float.IsInfinity(p[c])));
        static bool NonCDCD(Color[] a)
        { return !a.All(p => Enumerable.Range(0, 4).All(c => p[c] == -23.203125f)); } // Half 0xCDCD, exact float readback.
        static int Visible(Color[] a, Color[] background)
        { return Enumerable.Range(0, a.Length).Count(i => Mathf.Max(Mathf.Abs(a[i].r - background[i].r), Mathf.Abs(a[i].g - background[i].g), Mathf.Abs(a[i].b - background[i].b)) > .001f); }
        static void NativeState(Material m, bool enabled, float frame)
        {
            foreach (string keyword in VATKeywords) m.DisableKeyword(keyword);
            m.SetFloat("_VAT_Toggle", enabled ? 1 : 0); m.SetFloat("_Frame", frame);
            if (enabled) { m.EnableKeyword("_VAT"); m.EnableKeyword("_VAT_TYFLOW"); m.EnableKeyword("_TYFLOW_VAT_ABSOLUTE"); }
        }

        [TestCase(true, TestName = "G4NativeSRP_VATDraw_current_frozen_off_on_ortho")]
        [TestCase(false, TestName = "G4NativeSRP_VATDraw_current_frozen_off_on_perspective")]
        public void NativeCurrentAndFrozenRealDrawOffOn(bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var data = pipeline.rendererDataList[0] as UniversalRendererData; Assert.That(data, Is.Not.Null);
            Assert.That(data.transparentLayerMask.value & (1 << 4), Is.Not.Zero);
            string project = Path.GetDirectoryName(Application.dataPath);
            string rendererFile = Path.Combine(project, AssetDatabase.GetAssetPath(data)); byte[] rendererBytes = File.ReadAllBytes(rendererFile);
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(CurrentPath); Assert.That(packageInfo, Is.Not.Null);
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ?? Path.Combine(project, "Temp/NBFXNativeSRP"), ortho ? "ortho" : "perspective"); Directory.CreateDirectory(folder);
            var report = new Report { scope = "Actual Native Forward Mesh draw states. ShaderUtil subShader0 code is global metadata, not pass-specific or runtime/performance timing. Current nonzero after draw is a strict batcher failure; Frozen known10 is retained.",
                unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName,
                resolvedPackage = packageInfo.resolvedPath, orthographic = ortho, srpBatcherEnabled = pipeline.useSRPBatcher, noGraphWarmOrImport = true };
            string[] relativeSources = { "NBShaders2/Shader/NBShader.shader", "NBShaders2/Shader/HLSL/NBShaderInput.hlsl", "NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl", "XuanXuanRenderUtility/Shader/HLSL/VAT.hlsl", "XuanXuanRenderUtility/Shader/HLSL/TyflowVAT.hlsl", "XuanXuanRenderUtility/Shader/HLSL/TyflowVATKernelV1.hlsl", "XuanXuanRenderUtility/Shader/HLSL/HoudiniVAT.hlsl",
                "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader", "Tests/Baseline/Frozen/NBShaders2/Shader/HLSL/NBShaderInput.hlsl", "Tests/Baseline/Frozen/NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl", "Tests/Baseline/Frozen/XuanXuanRenderUtility/Shader/HLSL/VAT.hlsl", "Tests/Baseline/Frozen/XuanXuanRenderUtility/Shader/HLSL/TyflowVAT.hlsl",
                "Tests/URP/Editor/G4GraphVATTests.cs", "Tests/URP/Editor/G4GraphTyflowVATTests.cs" };
            report.sourceHashes = relativeSources.Select(path => new SourceHash { path = path, sha256 = SHA(File.ReadAllBytes(Path.Combine(packageInfo.resolvedPath, path))) }).ToArray();
            var scene = previewScene = EditorSceneManager.NewPreviewScene();
            var actor = Keep(GameObject.CreatePrimitive(PrimitiveType.Quad)); actor.layer = 4;
            var mesh = Keep(Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh)); actor.GetComponent<MeshFilter>().sharedMesh = mesh;
            for (int channel = 1; channel < 8; ++channel) mesh.SetUVs(channel, channel == 1 ? Enumerable.Range(0, 4).Select(v => new Vector4(v, 4, 0, 0)).ToList() : Enumerable.Repeat(Vector4.zero, 4).ToList());
            var cameraObject = Keep(new GameObject("Native SRP VAT diagnostic camera")); var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor, scene); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene); camera.scene = scene;
            actor.transform.rotation = Quaternion.Euler(4, 17, 3); actor.transform.localScale = new Vector3(1.65f, 1.45f, 1.2f);
            var renderer = actor.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            camera.orthographic = ortho; camera.orthographicSize = 1.4f; camera.fieldOfView = 42; camera.nearClipPlane = .1f; camera.farClipPlane = 20;
            camera.transform.position = new Vector3(.08f, .04f, 4); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
            camera.cullingMask = 1 << 4; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.0625f, .125f, .1875f, 1); camera.allowHDR = true; camera.allowMSAA = false;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var helper = vatHelper = new G4GraphTyflowVATTests();
            var vat = (Texture2D)Call(typeof(G4GraphTyflowVATTests).GetMethod("BuildVAT", Instance), helper, new object[] { 0, "raw", mesh });
            var map = Keep((Texture2D)Call(typeof(G4GraphVATTests).GetMethod("Checker", Static), null, null));
            report.vatTextureSHA256 = PixelSHA(vat); // Decoded linear RGBAf32 pixel bytes.
            var current = Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath)) { hideFlags = HideFlags.HideAndDontSave, name = "current-native-srp" });
            var frozen = Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath)) { hideFlags = HideFlags.HideAndDontSave, name = "frozen-native-srp" });
            Assert.That(current.shader && frozen.shader, Is.True); Assert.That(frozen.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            var rt = Keep(new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)); var read = Keep(new Texture2D(128, 128, TextureFormat.RGBAHalf, false, true));
            var oldRT = RenderTexture.active; camera.targetTexture = rt;
            var nb = data.rendererFeatures.FirstOrDefault(f => f && f.GetType().FullName == "NBShader.NBPostProcess"); bool nbWasActive = nb && nb.isActive;
            bool async = ShaderUtil.allowAsyncCompilation; var draws = new List<DrawState>(); var roleMetrics = new List<RoleMetrics>();
            var configure = typeof(G4GraphVATTests).GetMethod("Configure", Static); var draw = typeof(G4GraphVATTests).GetMethod("Draw", Static);
            Color[] Capture(Material material, string label)
            {
                renderer.sharedMaterial = material; Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                for (int i = 0; i < 3; ++i) camera.Render();
                var pixels = (Color[])Call(draw, null, new object[] { renderer, material, camera, rt, read, folder, label });
                Assert.That(renderer.sharedMaterial, Is.SameAs(material)); return pixels;
            }
            try
            {
                ShaderUtil.allowAsyncCompilation = false; if (nb) nb.SetActive(false);
                Assert.That(rt.Create() && !rt.sRGB, Is.True);
                foreach (var material in new[] { current, frozen })
                {
                    Call(configure, null, new object[] { material, false, "manual2", map, map, map, map, map, map, map });
                    material.SetFloat("_VATMode", 1); material.SetFloat("_TyFlowVATSubMode", 0); material.SetTexture("_VATTex", vat);
                    material.SetFloat("_ImportScale", 1); material.SetFloat("_Frames", 2); material.SetFloat("_Autoplay", 0); material.SetFloat("_AutoplaySpeed", 1);
                    material.SetFloat("_Loop", 0); material.SetFloat("_InterpolateLoop", 0); material.SetFloat("_FrameInterpolation", 0);
                    material.SetFloat("_LinearToGamma", 0); material.SetFloat("_RGBAEncoded", 0); material.SetFloat("_RGBAHalf", 0);
                    material.SetFloat("_DeformingSkin", 0); material.SetFloat("_SkinBoneCount", 1); material.SetFloat("_VATIncludesNormals", 0); material.SetFloat("_AffectsShadows", 1);
                    NativeState(material, false, 1);
                }
                renderer.enabled = false; var background = Capture(current, "background"); var backgroundRepeat = Capture(current, "background-repeat"); renderer.enabled = true;
                foreach (var material in new[] { current, frozen })
                {
                    string role = material == current ? "current" : "frozen"; var frames = new List<Color[]>();
                    foreach (string state in new[] { "off", "on", "frame0" })
                    {
                        NativeState(material, state != "off", state == "frame0" ? 0 : 1); renderer.sharedMaterial = material;
                        var row = new DrawState { role = role, state = state, shaderName = material.shader.name, shaderAssetPath = AssetDatabase.GetAssetPath(material.shader), materialName = material.name, materialInstance = material.GetInstanceID(),
                            keywords = material.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal).ToArray(), vatToggle = material.GetFloat("_VAT_Toggle"), vatMode = material.GetFloat("_VATMode"), houdiniSubMode = material.GetFloat("_HoudiniVATSubMode"), tyflowSubMode = material.GetFloat("_TyFlowVATSubMode"),
                            frame = material.GetFloat("_Frame"), frames = material.GetFloat("_Frames"), autoplay = material.GetFloat("_Autoplay"), frameInterpolation = material.GetFloat("_FrameInterpolation"), importScale = material.GetFloat("_ImportScale"), vatWidth = vat.width, vatHeight = vat.height, passes = Passes(material), beforeDraw = Query(material.shader),
                            protocolIntegers = new[] { "_W9ParticleShaderFlags", "_W9ParticleShaderFlags1", "_W9ParticleCustomDataFlag0", "_W9ParticleCustomDataFlag1", "_W9ParticleCustomDataFlag2", "_W9ParticleCustomDataFlag3" }.Select(name => new IntegerState { name = name, value = material.GetInteger(name) }).ToArray() };
                        var frame = Capture(material, role + "-" + state); var repeat = Capture(material, role + "-" + state + "-repeat"); frames.Add(frame);
                        row.afterDraw = Query(material.shader); row.actualRendererMaterialMatches = renderer.sharedMaterial == material; row.rendererMaterialInstance = renderer.sharedMaterial.GetInstanceID(); row.actualRendererShader = renderer.sharedMaterial.shader.name;
                        row.actualRendererShaderAssetPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.shader);
                        row.shaderErrors = ShaderUtil.GetShaderMessages(material.shader).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
                        row.finite = Finite(frame) && Finite(repeat); row.nonCDCD = NonCDCD(frame) && NonCDCD(repeat); row.repeat = Delta(frame, repeat); row.visible = Visible(frame, background); row.deltaFromBackground = Delta(frame, background);
                        row.nativeBatcherFailed = !row.afterDraw.apiKnown || !row.afterDraw.known || row.afterDraw.code != 0; draws.Add(row);
                    }
                    var states = draws.Where(r => r.role == role).ToArray();
                    roleMetrics.Add(new RoleMetrics { role = role, vatOnOffResponse = Delta(frames[0], frames[1]), frameResponse = Delta(frames[1], frames[2]), currentAllDrawStatesCode0 = states.All(r => r.afterDraw.code == 0 && r.afterDraw.known),
                        frozenKnownCompatibleOrOriginal10 = states.All(r => r.afterDraw.known && (r.afterDraw.code == 0 || (r.afterDraw.code == 10 && !string.IsNullOrEmpty(r.afterDraw.reason)))) });
                }
                report.draws = draws.ToArray(); report.metrics = roleMetrics.ToArray();
                report.semanticEvidenceValid = Finite(background) && NonCDCD(background) && Delta(background, backgroundRepeat) == 0 && draws.All(r => r.finite && r.nonCDCD && r.repeat == 0 && r.visible > 150) && roleMetrics.All(r => r.vatOnOffResponse > .01f && r.frameResponse > .005f);
                report.currentBatcherFailed = draws.Any(r => r.role == "current" && r.nativeBatcherFailed);
                report.currentBatcherFailure = string.Join("; ", draws.Where(r => r.role == "current" && r.nativeBatcherFailed).Select(r => r.state + " code=" + r.afterDraw.code + " " + r.afterDraw.reason + " " + r.afterDraw.error));
                File.WriteAllText(Path.Combine(folder, "native-srp-draw.json"), JsonUtility.ToJson(report, true));
                Debug.Log("NBFX_NATIVE_SRP_DRAW ortho=" + ortho + " semanticEvidenceValid=" + report.semanticEvidenceValid + " currentBatcherFailed=" + report.currentBatcherFailed);
                Assert.That(report.semanticEvidenceValid, Is.True, "Invalid/invisible/zero-response output cannot prove native initialization.");
                foreach (var row in draws)
                {
                    Assert.That(row.actualRendererMaterialMatches, Is.True); Assert.That(row.shaderErrors, Is.Empty);
                    Assert.That(row.afterDraw.apiKnown && row.afterDraw.known && row.afterDraw.supported, Is.True, row.role + " " + row.state + " unknown/unsupported native status " + row.afterDraw.error);
                }
                Assert.That(roleMetrics.Single(r => r.role == "frozen").frozenKnownCompatibleOrOriginal10, Is.True, "Frozen must retain compatible or known original10 state.");
                Assert.That(report.currentBatcherFailed, Is.False, "Strict batcherFailed after actual current draws: " + report.currentBatcherFailure);
            }
            finally
            {
                if (nb) nb.SetActive(nbWasActive); ShaderUtil.allowAsyncCompilation = async;
                camera.targetTexture = null; RenderTexture.active = oldRT; rt.Release(); helper.Cleanup(); vatHelper = null;
                EditorSceneManager.ClosePreviewScene(scene); previewScene = default(UnityEngine.SceneManagement.Scene);
                for (int i = owned.Count - 1; i >= 0; --i) if (owned[i]) Object.DestroyImmediate(owned[i]); owned.Clear();
                Assert.That(File.ReadAllBytes(rendererFile), Is.EqualTo(rendererBytes), "Native diagnostic saved renderer asset.");
            }
        }
    }
}
