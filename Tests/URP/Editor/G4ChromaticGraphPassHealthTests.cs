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
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Independent Graph-only diagnostic. No Frozen/Native instances, forced
    // import, GetShaderText, metadata enumeration, or private SRP query.
    public sealed class G4ChromaticGraphPassHealthTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        const int Size = 128;
        [Serializable] sealed class Source { public string path, sha256; }
        [Serializable] sealed class Pass { public string name; public bool enabled; }
        [Serializable] sealed class Receipt
        {
            public string scope, stage, exception, cleanupError, unity, api, gpu, shaderAssetPath,
                shaderName, actualRendererShader, rendererAssetPath;
            public string[] keywords;
            public Source[] sources;
            public Pass[] passes;
            public bool orthographic, actualRendererMaterialMatches, finite, nonCDCD,
                cleanupComplete, rendererDiskRestored, noFrozenOrNativeMaterial;
            public int rawCount, visibleOn, visibleOff, responseChanged, withNoiseChanged,
                actorChanged, deniedChanged, restoredChanged, gateRestoreChanged;
            public float marker, savedTier, caAllow, caRawToggle, flagsLo, repeat, response,
                withNoise, actorResponse, restored, deniedError, gateRestoreError;
        }
        static object Original(string name, params object[] args)
        {
            var method = typeof(G4GraphChromaticTests).GetMethods(Static)
                .Single(m => m.Name == name && m.GetParameters().Length == args.Length);
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException e)
            { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); throw; }
        }
        static float Max(Color[] a, Color[] b)
        { var d = Original("Compare", a, b); return (float)d.GetType().GetField("max").GetValue(d); }
        static int Changed(Color[] a, Color[] b)
        { var d = Original("Compare", a, b); return (int)d.GetType().GetField("changed").GetValue(d); }
        static Source Hash(string packageRoot, string relative)
        {
            string path = Path.Combine(packageRoot, relative);
            Assert.That(File.Exists(path), Is.True, "Required real source absent: " + path);
            using (var sha = SHA256.Create())
                return new Source { path = relative, sha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant() };
        }

        static IEnumerable<TestCaseData> Cases()
        {
            yield return new TestCaseData("Forward", "with-noise", false).SetName("G4CAGraphPassHealth_Forward_with-noise_perspective");
            yield return new TestCaseData("NBDeferredDistortPass", "with-noise", true).SetName("G4CAGraphPassHealth_NBDeferredDistortPass_with-noise_ortho");
            yield return new TestCaseData("NBCameraOpaqueDistortPass", "with-noise", true).SetName("G4CAGraphPassHealth_NBCameraOpaqueDistortPass_with-noise_ortho");
            yield return new TestCaseData("Forward", "custom1x", true).SetName("G4CAGraphPassHealth_Forward_custom1x_ortho");
            yield return new TestCaseData("Forward", "pom", true).SetName("G4CAGraphPassHealth_Forward_pom_ortho");
        }
        [TestCaseSource(nameof(Cases))] public void SingleHostPassProjectionAndCombo(string route, string kind, bool ortho)
        {
            string evidence = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(evidence)) evidence = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXCAGraphHealth");
            string folder = Path.Combine(evidence, "ca-graph-pass-health", route+"-"+kind+(ortho?"-ortho":"-perspective"));
            Directory.CreateDirectory(folder);
            var receipt = new Receipt { scope = "Graph-only selected exact pass/projection/combo; not ABC parity, other passes, Player, or old CA incident closure",
                stage = "preflight", unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(),
                gpu = SystemInfo.graphicsDeviceName, orthographic = ortho, noFrozenOrNativeMaterial = true };
            void Save() => File.WriteAllText(Path.Combine(folder, "receipt.json"), JsonUtility.ToJson(receipt, true));
            Save();
            var owned = new List<Object>(); Scene scene = default;
            Camera camera = null; RenderTexture rt = null;
            RenderTexture previous = RenderTexture.active;
            ScriptableRendererFeature nbFeature = null; ScriptableRendererData rendererData = null;
            G4ScreenNoiseDirectedFeature directed = null;
            bool nbWasActive = false; byte[] rendererBefore = null; string rendererPath = null;
            try
            {
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
                Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode, Is.False);
                Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
                rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0];
                var urd = rendererData as UniversalRendererData;
                Assert.That(urd, Is.Not.Null); Assert.That(urd.transparentLayerMask.value & (1 << 4), Is.Not.Zero);
                rendererPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(rendererData));
                rendererBefore = File.ReadAllBytes(rendererPath); receipt.rendererAssetPath = AssetDatabase.GetAssetPath(rendererData);
                nbFeature = (ScriptableRendererFeature)Original("FindNBPostProcess", rendererData);
                Assert.That(nbFeature, Is.Not.Null); nbWasActive = nbFeature.isActive;
                Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
                Assert.That(graphShader && graphShader.isSupported, Is.True);
                receipt.shaderAssetPath = AssetDatabase.GetAssetPath(graphShader); receipt.shaderName = graphShader.name;
                string expectedGuid = AssetDatabase.AssetPathToGUID(GraphPath);
                Assert.That(expectedGuid, Is.Not.Empty); Assert.That(AssetDatabase.AssetPathToGUID(receipt.shaderAssetPath), Is.EqualTo(expectedGuid));
                string packageRoot = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Packages/NB_FX");
                receipt.sources = new[] { Hash(packageRoot, "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph"),
                    Hash(packageRoot, "NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl"),
                    Hash(packageRoot, "NBShaders2/Shader/HLSL/NBShaderChromaticV1.hlsl"),
                    Hash(packageRoot, "Tests/URP/Editor/G4GraphChromaticTests.cs") };
                scene = EditorSceneManager.NewPreviewScene();
                GameObject backdropObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                GameObject actor = new GameObject("CA Graph-only ordinary Mesh", typeof(MeshFilter), typeof(MeshRenderer));
                GameObject cameraObject = new GameObject("CA Graph-only camera");
                foreach (GameObject go in new[] { backdropObject, actor, cameraObject }) SceneManager.MoveGameObjectToScene(go, scene);
                Mesh mesh = (Mesh)Original("BuildMesh"); owned.Add(mesh);
                Texture2D baseMap = (Texture2D)Original("MakeBaseMap", false), height = (Texture2D)Original("MakeHeight"),
                    noise = (Texture2D)Original("MakeNoise"), backgroundMap = (Texture2D)Original("MakeBackdrop");
                owned.AddRange(new Object[] { baseMap, height, noise, backgroundMap });
                Material graph = new Material(graphShader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(graph);
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit"); Assert.That(unlit, Is.Not.Null);
                Material backdrop = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(backdrop);
                backdropObject.layer = 2; backdropObject.transform.position = new Vector3(0, 0, 1);
                backdropObject.transform.localScale = new Vector3(6, 6, 1);
                backdrop.SetTexture("_BaseMap", backgroundMap); backdrop.SetColor("_BaseColor", Color.white);
                backdrop.SetFloat("_Cull", 0); backdrop.renderQueue = 2000;
                backdropObject.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                backdropObject.GetComponent<MeshRenderer>().enabled = route != "NBDeferredDistortPass";
                actor.layer = route == "Forward" ? 4 : G4GraphScreenNoiseTests.ForegroundLayer; actor.transform.position = new Vector3(0, 0, 2);
                actor.transform.rotation = Quaternion.Euler(0, 27, 0); actor.transform.localScale = new Vector3(2, 2, 1);
                actor.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = actor.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                camera = cameraObject.AddComponent<Camera>(); var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                camera.scene = scene; camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 53.13f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.09f, .15f, .22f, .35f); camera.allowHDR = true; camera.allowMSAA = false;
                camera.cullingMask = (1 << 2) | (1 << actor.layer); camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0); cameraData.requiresColorTexture = true; cameraData.renderPostProcessing = false;
                rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear); owned.Add(rt);
                var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true); owned.Add(readback);
                camera.targetTexture = rt; rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                nbFeature.SetActive(false);
                if (route != "Forward")
                {
                    directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();
                    directed.hideFlags = HideFlags.HideAndDontSave; directed.targetCamera = camera; directed.selectedPass = route;
                    directed.Create(); directed.SetActive(true); rendererData.rendererFeatures.Add(directed); rendererData.SetDirty();
                }
                bool originalWithNoise = kind == "with-noise" || kind == "pom";
                bool originalPom = kind == "pom";
                Original("Configure", graph, true, route, kind, baseMap, height, noise);
                Original("Apply", graph, true, kind, true, originalWithNoise, originalPom, -1);
                graph.SetFloat("_NBShaderFeatureTier", 3);
                var sync = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("NBShaderEditor.NBShaderSyncService", false)).First(t => t != null);
                var initialize = sync.GetMethod("TryInitializeGraphSupportedGateTierOnAssign", Static, null, new[] { typeof(Material) }, null);
                Assert.That(initialize, Is.Not.Null); Assert.That(initialize.Invoke(null, new object[] { graph }), Is.EqualTo(true));
                receipt.marker = graph.GetFloat("_NB_GraphGUIStateVersion"); receipt.savedTier = graph.GetFloat("_NBShaderFeatureTier");
                receipt.caAllow = graph.GetFloat("_NB_TierAllowChromaticAberration"); receipt.caRawToggle = graph.GetFloat("_Distortion_Choraticaberrat_Toggle");
                receipt.flagsLo = graph.GetFloat("_NB_Flags0Lo16"); receipt.keywords = graph.shaderKeywords.OrderBy(k => k).ToArray();
                receipt.passes = new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward", "DepthOnly", "ShadowCaster", "NBDeferredDistortPass", "NBCameraOpaqueDistortPass" }
                    .Select(p => new Pass { name = p, enabled = graph.GetShaderPassEnabled(p) }).ToArray();
                Assert.That(receipt.marker, Is.EqualTo(2)); Assert.That(receipt.savedTier, Is.EqualTo(3)); Assert.That(receipt.caAllow, Is.EqualTo(1));
                Assert.That(receipt.caRawToggle, Is.EqualTo(1)); Assert.That(graph.GetFloat("_NB_TierAllowNoise"), Is.EqualTo(kind == "custom1x" ? 0 : 1));
                renderer.sharedMaterial = graph; receipt.actualRendererMaterialMatches = renderer.sharedMaterial == graph;
                receipt.actualRendererShader = renderer.sharedMaterial.shader.name; Assert.That(receipt.actualRendererMaterialMatches, Is.True);
                var frames = new List<Color[]>();
                Color[] Capture(string label)
                {
                    receipt.stage = "before-draw:" + label; Save();
                    Color[] pixels = (Color[])Original("Capture", camera, rt, readback, Path.Combine(folder, label));
                    frames.Add(pixels); receipt.rawCount++; receipt.stage = "after-draw:" + label; Save(); return pixels;
                }
                Color[] Snap(string label, bool enabled, bool withNoise)
                { Original("Apply", graph, true, kind, enabled, withNoise, originalPom, -1); renderer.sharedMaterial = graph; return Capture(label); }
                renderer.enabled = false; Color[] background = Capture("background"); renderer.enabled = true;
                Capture("C-warm"); Color[] on = Snap("C-on", true, originalWithNoise); Color[] repeated = Snap("C-repeat", true, originalWithNoise);
                Color[] off = Snap("C-off", false, originalWithNoise); Color[] flipped = Snap("C-withnoise-flipped", true, !originalWithNoise);
                Color[] restored = Snap("C-restored", true, originalWithNoise);
                graph.SetFloat("_NB_TierAllowChromaticAberration", 0); Color[] denied = Snap("C-gate-denied", true, originalWithNoise);
                graph.SetFloat("_NB_TierAllowChromaticAberration", 1); Color[] gateRestored = Snap("C-gate-restored", true, originalWithNoise);
                receipt.finite = (bool)Original("Finite", (object)frames.ToArray());
                receipt.nonCDCD = frames.All(f => f.All(c => Enumerable.Range(0, 4).All(i => c[i] != -23.203125f)));
                receipt.visibleOn = (int)Original("Visible", on); receipt.visibleOff = (int)Original("Visible", off);
                receipt.repeat = Max(on, repeated); receipt.response = Max(on, off); receipt.responseChanged = Changed(on, off);
                receipt.withNoise = Max(on, flipped); receipt.withNoiseChanged = Changed(on, flipped);
                receipt.actorResponse = Max(on, background); receipt.actorChanged = Changed(on, background);
                receipt.restored = Max(on, restored); receipt.restoredChanged = Changed(on, restored);
                receipt.deniedError = Max(off, denied); receipt.deniedChanged = Changed(off, denied);
                receipt.gateRestoreError = Max(on, gateRestored); receipt.gateRestoreChanged = Changed(on, gateRestored);
                receipt.stage = "assertions"; Save();
                Assert.That(receipt.finite && receipt.nonCDCD, Is.True); Assert.That(receipt.rawCount, Is.EqualTo(9));
                Assert.That(receipt.visibleOn, Is.GreaterThan(100)); Assert.That(receipt.visibleOff, Is.GreaterThan(100));
                Assert.That(receipt.repeat, Is.Zero); Assert.That(receipt.restored + receipt.gateRestoreError + receipt.deniedError, Is.Zero);
                Assert.That(receipt.restoredChanged + receipt.gateRestoreChanged + receipt.deniedChanged, Is.Zero);
                Assert.That(receipt.response, Is.GreaterThan(.005f)); Assert.That(receipt.responseChanged, Is.GreaterThan(20));
                if (originalWithNoise) { Assert.That(receipt.withNoise, Is.GreaterThan(.003f)); Assert.That(receipt.withNoiseChanged, Is.GreaterThan(20)); } // Original custom1x requires on/off strong; it does not require this separate WithNoise threshold.
                Assert.That(receipt.actorResponse, Is.GreaterThan(.005f)); Assert.That(receipt.actorChanged, Is.GreaterThan(20));
                receipt.stage = "semantic-pass"; Save();
            }
            catch (Exception error) { receipt.exception = error.ToString(); Save(); throw; }
            finally
            {
                try
                {
                    if (directed) { rendererData.rendererFeatures.Remove(directed); Object.DestroyImmediate(directed); }
                    if (nbFeature) nbFeature.SetActive(nbWasActive);
                    if (rendererData) rendererData.SetDirty();
                    if (camera) camera.targetTexture = null;
                    RenderTexture.active = previous; if (rt) rt.Release();
                    foreach (Object o in owned.AsEnumerable().Reverse()) if (o) Object.DestroyImmediate(o);
                    if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                    receipt.rendererDiskRestored = rendererBefore == null || File.ReadAllBytes(rendererPath).SequenceEqual(rendererBefore);
                    receipt.cleanupComplete = true; Save();
                    Assert.That(receipt.rendererDiskRestored, Is.True, "Graph-only CA fixture changed renderer disk bytes.");
                }
                catch (Exception cleanupError) { receipt.cleanupError = cleanupError.ToString(); Save(); throw; }
            }
        }
    }
}
