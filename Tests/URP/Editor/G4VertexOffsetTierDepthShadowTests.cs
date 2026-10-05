using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NBFX.Baseline.Tests
{
    // Ordinary Mesh only. Directed DepthOnly and a real URP main-light shadow;
    // not evidence for automatic depth-prepass selection, VFX, GUI or Player.
    public sealed class G4VertexOffsetTierDepthShadowTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const int Size = 128;
        const int DepthLayer = G2DepthOnlyABTests.ForegroundLayer;
        const int ReceiverLayer = 2;

        [OneTimeSetUp] public void ReimportGraphAfterAssemblyReload()
        {
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        [Serializable] sealed class Metrics
        {
            public string caseId, stage, api, unityVersion, note;
            public bool orthographic, finite;
            public float abMax, bcMax, offMax, legacyRepeat, graphRepeat;
            public float legacyResponse, graphResponse, controlResponse, invariantLegacy, invariantGraph;
            public int bcDifferent, abDifferent, validDepthFrames, recordedDepthFrames;
        }

        static IEnumerable<TestCaseData> DepthCases()
        {yield return new TestCaseData("vertexoffset",.5f,true).SetName("G4VOTierAux_DepthOnly_parent_restore_ortho");}
        [TestCaseSource(nameof(DepthCases))]
        public void DirectedDepthOnlyAlphaMatchesFrozenAndGraph(string feature, float cutoff, bool ortho)
        {
            RequirePipeline();
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False);
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var rendererData = pipeline.rendererDataList[0];
            Assert.That(rendererData, Is.Not.Null);
            string rendererFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(rendererData));
            byte[] before = File.ReadAllBytes(rendererFile);
            var graph = new Material(Load(GraphPath));
            var current = new Material(Load(CurrentPath));
            var frozen = new Material(Load(FrozenPath));
            var probe = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var foreground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var farProbe = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX G4 directed DepthOnly");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var alphaMap = AlphaMap();
            var oldActive = RenderTexture.active;
            G2DirectedDepthOnlyFeature directed = null;
            Material post = null;
            int oldPostFlags = 0;
            string id = "depth-" + feature + "-" + cutoff + (ortho ? "-ortho" : "-perspective");
            string output = Output(id);
            try
            {
                Assert.That(probe.shader && probe.shader.isSupported, Is.True);
                foreach (var m in new[] { frozen, current, graph })
                {
                    Configure(m, m == graph, feature, cutoff, false, false, alphaMap);
                    m.SetShaderPassEnabled("DepthOnly", true);
                }
                foreground.layer = DepthLayer;
                foreground.transform.position = new Vector3(0, 0, 2);
                foreground.transform.localScale = new Vector3(1.2f, 1.2f, 1);
                var writer = foreground.GetComponent<MeshRenderer>();
                writer.shadowCastingMode = ShadowCastingMode.Off;
                farProbe.layer = ReceiverLayer;
                farProbe.transform.position = new Vector3(0, 0, 1);
                farProbe.transform.localScale = new Vector3(3, 3, 1);
                probe.SetColor("_BaseColor", Color.green);
                probe.SetFloat("_Surface", 1);
                probe.SetFloat("_SrcBlend", (float)BlendMode.One);
                probe.SetFloat("_DstBlend", (float)BlendMode.Zero);
                probe.SetFloat("_ZWrite", 0);
                probe.SetFloat("_Cull", 0);
                probe.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                probe.renderQueue = 3000;
                var probeRenderer = farProbe.GetComponent<MeshRenderer>();
                probeRenderer.sharedMaterial = probe;
                probeRenderer.shadowCastingMode = ShadowCastingMode.Off;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.125f, .25f, .375f, 1);
                camera.orthographic = ortho; camera.orthographicSize = 1;
                camera.fieldOfView = 45; camera.nearClipPlane = .1f; camera.farClipPlane = 10;
                camera.allowHDR = false; camera.allowMSAA = false;
                camera.cullingMask = (1 << DepthLayer) | (1 << ReceiverLayer);
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;
                target.filterMode = FilterMode.Point; target.Create();
                directed = ScriptableObject.CreateInstance<G2DirectedDepthOnlyFeature>();
                directed.hideFlags = HideFlags.HideAndDontSave;
                directed.targetCamera = camera; directed.Create(); directed.SetActive(true);
                rendererData.rendererFeatures.Add(directed); rendererData.SetDirty();
                // The existing renderer feature is unrelated to this directed
                // depth test; suppress only its in-memory runtime material flags.
                foreach (var f in rendererData.rendererFeatures)
                    if (f != null && f.GetType().FullName == "NBShader.NBPostProcess" && f.isActive)
                    {
                        var field = f.GetType().GetField("NBPostProcessMaterial", BindingFlags.Public | BindingFlags.Static);
                        post = field?.GetValue(null) as Material;
                        break;
                    }
                if (post != null && post.HasProperty("_NBPostProcessFlags"))
                {
                    oldPostFlags = post.GetInteger("_NBPostProcessFlags");
                    post.SetInteger("_NBPostProcessFlags", 0);
                }
                writer.enabled = false;
                var empty = Capture(camera, target, readback, output, "empty");
                writer.enabled = true;
                var fo = DepthCapture(writer, frozen, false, camera, target, readback, output, "A-off");
                var bo = DepthCapture(writer, current, false, camera, target, readback, output, "B-off");
                var co = DepthCapture(writer, graph, false, camera, target, readback, output, "C-off");
                var fa = DepthCapture(writer, frozen, true, camera, target, readback, output, "A");
                var ba = DepthCapture(writer, current, true, camera, target, readback, output, "B");
                var ca = DepthCapture(writer, graph, true, camera, target, readback, output, "C");
                var br = DepthCapture(writer, current, true, camera, target, readback, output, "B-repeat");
                var cr = DepthCapture(writer, graph, true, camera, target, readback, output, "C-repeat");
                foreach (var material in new[]{frozen,current,graph}) VerifyPass(material,"DepthOnly");
                string rawVOIntent=EditorJsonUtility.ToJson(graph);
                Assert.That(ApplyVOPolicy(graph,false),Is.True);frozen.DisableKeyword("_VERTEX_OFFSET");current.DisableKeyword("_VERTEX_OFFSET");
                var voDeny=new Color[3][];var voDenyRepeat=new Color[3][];var voRestore=new Color[3][];var voRestoreRepeat=new Color[3][];var voMaterials=new[]{frozen,current,graph};
                for(int role=0;role<3;role++){voDeny[role]=DepthCapture(writer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-denied");voDenyRepeat[role]=DepthCapture(writer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-denied-repeat");}
                Assert.That(ApplyVOPolicy(graph,true),Is.True);frozen.EnableKeyword("_VERTEX_OFFSET");current.EnableKeyword("_VERTEX_OFFSET");
                for(int role=0;role<3;role++){voRestore[role]=DepthCapture(writer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-restored");voRestoreRepeat[role]=DepthCapture(writer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-restored-repeat");}
                Assert.That(EditorJsonUtility.ToJson(graph),Is.EqualTo(rawVOIntent));
                var voMetric=new VOAuxMetrics{stage="DepthOnly",finite=Finite(voDeny.Concat(voDenyRepeat).Concat(voRestore).Concat(voRestoreRepeat).ToArray()),ab=Delta(voDeny[0],voDeny[1]),bc=Delta(voDeny[1],voDeny[2]),repeatA=Delta(voDeny[0],voDenyRepeat[0])+Delta(voRestore[0],voRestoreRepeat[0]),repeatB=Delta(voDeny[1],voDenyRepeat[1])+Delta(voRestore[1],voRestoreRepeat[1]),repeatC=Delta(voDeny[2],voDenyRepeat[2])+Delta(voRestore[2],voRestoreRepeat[2]),responseA=Delta(fa,voDeny[0]),responseB=Delta(ba,voDeny[1]),responseC=Delta(ca,voDeny[2]),restoreA=Delta(fa,voRestore[0]),restoreB=Delta(ba,voRestore[1]),restoreC=Delta(ca,voRestore[2])};
                File.WriteAllText(Path.Combine(output,"vo-tier-metrics.json"),JsonUtility.ToJson(voMetric,true));Debug.Log("NBFX_VO_AUX "+JsonUtility.ToJson(voMetric));
                Assert.That(voMetric.finite,Is.True);Assert.That(voMetric.ab+voMetric.bc+voMetric.repeatA+voMetric.repeatB+voMetric.repeatC+voMetric.restoreA+voMetric.restoreB+voMetric.restoreC,Is.Zero);
                Assert.That(voMetric.responseA,Is.GreaterThan(.01f));Assert.That(voMetric.responseB,Is.GreaterThan(.01f));Assert.That(voMetric.responseC,Is.GreaterThan(.01f),"Actual geometry displacement must alter real depth/shadow response.");

                var metrics = new Metrics { caseId = id, stage = "directed DepthOnly", api = SystemInfo.graphicsDeviceType.ToString(),
                    unityVersion = Application.unityVersion, orthographic = ortho,
                    note = "Real RendererList writing active depth; alpha .25/.75; full-frame raw linear RGBAHalf; ordinary Mesh only." };
                metrics.finite = Finite(empty, fo, bo, co, fa, ba, ca, br, cr);
                metrics.abMax = Delta(fa, ba, out metrics.abDifferent);
                metrics.bcMax = Delta(ba, ca, out metrics.bcDifferent);
                metrics.offMax = Math.Max(Delta(empty, bo), Delta(empty, co));
                metrics.legacyRepeat = Delta(ba, br); metrics.graphRepeat = Delta(ca, cr);
                metrics.legacyResponse = Delta(bo, ba); metrics.graphResponse = Delta(co, ca);
                metrics.recordedDepthFrames = directed.recordedFrames; metrics.validDepthFrames = directed.validDepthFrames;
                if (cutoff > .75f)
                {
                    foreach (var m in new[] { frozen, current, graph }) SetCutoff(m, .5f);
                    var fb = DepthCapture(writer, frozen, true, camera, target, readback, output, "A-control");
                    var bb = DepthCapture(writer, current, true, camera, target, readback, output, "B-control");
                    var cb = DepthCapture(writer, graph, true, camera, target, readback, output, "C-control");
                    Assert.That(Delta(fb, bb), Is.Zero); Assert.That(Delta(bb, cb), Is.Zero);
                    metrics.controlResponse = Delta(bo, cb);
                    metrics.finite &= Finite(fb, bb, cb);
                    foreach (var m in new[] { frozen, current, graph }) SetCutoff(m, cutoff);
                }
                if (feature == "forward-only")
                {
                    foreach (var m in new[] { frozen, current, graph }) ForwardOnly(m, false);
                    var plainB = DepthCapture(writer, current, true, camera, target, readback, output, "B-no-forward-only");
                    var plainC = DepthCapture(writer, graph, true, camera, target, readback, output, "C-no-forward-only");
                    metrics.invariantLegacy = Delta(ba, plainB); metrics.invariantGraph = Delta(ca, plainC);
                }
                SaveMetrics(output, metrics);
                Assert.That(metrics.finite, Is.True);
                Assert.That(metrics.recordedDepthFrames, Is.GreaterThan(0));
                Assert.That(metrics.validDepthFrames, Is.EqualTo(metrics.recordedDepthFrames));
                Assert.That(metrics.offMax, Is.Zero, "Pass-off must restore the green farther probe.");
                Assert.That(Delta(fo, bo), Is.Zero); Assert.That(Delta(bo, co), Is.Zero);
                Assert.That(metrics.abMax, Is.Zero); Assert.That(metrics.bcMax, Is.Zero);
                Assert.That(metrics.legacyRepeat, Is.Zero); Assert.That(metrics.graphRepeat, Is.Zero);
                if (cutoff > .75f) Assert.That(metrics.controlResponse, Is.GreaterThan(.1f));
                else { Assert.That(metrics.legacyResponse, Is.GreaterThan(.1f)); Assert.That(metrics.graphResponse, Is.GreaterThan(.1f)); }
                if (feature == "forward-only")
                { Assert.That(metrics.invariantLegacy, Is.Zero); Assert.That(metrics.invariantGraph, Is.Zero); }
            }
            finally
            {
                if (post != null && post.HasProperty("_NBPostProcessFlags")) post.SetInteger("_NBPostProcessFlags", oldPostFlags);
                if (directed != null)
                { rendererData.rendererFeatures.Remove(directed); rendererData.SetDirty(); UnityEngine.Object.DestroyImmediate(directed); }
                camera.targetTexture = null; RenderTexture.active = oldActive; target.Release();
                foreach (var o in new UnityEngine.Object[] { foreground, farProbe, cameraObject, frozen, current, graph, probe, target, readback, alphaMap })
                    UnityEngine.Object.DestroyImmediate(o);
                Assert.That(File.ReadAllBytes(rendererFile), Is.EqualTo(before), "Directed feature changed renderer asset on disk.");
            }
        }

        static IEnumerable<TestCaseData> ShadowCases()
        {yield return new TestCaseData("vertexoffset",false).SetName("G4VOTierAux_ShadowCaster_parent_restore_perspective");}
        [TestCaseSource(nameof(ShadowCases))]
        public void MainLightShadowCasterMatchesFrozenAndGraph(string mode, bool ortho)
        {
            RequirePipeline();
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            Assert.That(pipeline.supportsMainLightShadows && pipeline.shadowDistance > 10, Is.True);
            Assert.That(QualitySettings.shadows, Is.Not.EqualTo(UnityEngine.ShadowQuality.Disable));
            var graph = new Material(Load(GraphPath)); var current = new Material(Load(CurrentPath)); var frozen = new Material(Load(FrozenPath));
            var receiverMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var alphaMap = AlphaMap();
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var caster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var lightObject = new GameObject("NBFX G4 main light"); var light = lightObject.AddComponent<Light>();
            var cameraObject = new GameObject("NBFX G4 shadow camera"); var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderShadows = true;
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var oldActive = RenderTexture.active; var oldSun = RenderSettings.sun;
            var oldAmbientMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight; var oldFog = RenderSettings.fog;
            string id = "shadow-" + mode + (ortho ? "-ortho" : "-perspective"); string output = Output(id);
            try
            {
                Assert.That(receiverMaterial.shader && receiverMaterial.shader.isSupported, Is.True);
                bool transparent = mode.StartsWith("transparent", StringComparison.Ordinal);
                bool dither = mode == "transparent-dither";
                float cutoff = mode == "cutoff25" ? .25f : mode == "cutoff50" ? .5f : mode == "cutoff80" ? .8f : -1;
                foreach (var m in new[] { frozen, current, graph })
                {
                    Configure(m, m == graph, mode == "vertexoffset"?"vertexoffset":mode == "forward-only" ? "forward-only" : "base", cutoff, transparent, dither, alphaMap);
                    m.SetShaderPassEnabled("ShadowCaster", true);
                }
                receiverMaterial.SetColor("_BaseColor", Color.white);
                receiverMaterial.DisableKeyword("_RECEIVE_SHADOWS_OFF"); receiver.layer = ReceiverLayer;
                receiver.transform.position = Vector3.zero; receiver.transform.localScale = new Vector3(.55f, 1, .55f);
                var receiverRenderer = receiver.GetComponent<MeshRenderer>();
                receiverRenderer.sharedMaterial = receiverMaterial; receiverRenderer.shadowCastingMode = ShadowCastingMode.Off;
                caster.layer = ReceiverLayer; caster.transform.position = new Vector3(0, 1, 0);
                caster.transform.rotation = Quaternion.Euler(-90, 0, 0); caster.transform.localScale = new Vector3(1.5f, 1.5f, 1);
                var casterRenderer = caster.GetComponent<MeshRenderer>(); casterRenderer.shadowCastingMode = ShadowCastingMode.On;
                light.type = LightType.Directional; light.shadows = LightShadows.Hard; light.shadowStrength = 1;
                light.intensity = 2; light.color = Color.white; light.cullingMask = 1 << ReceiverLayer;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                RenderSettings.sun = light; RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black; RenderSettings.fog = false;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .13f, .19f, 1);
                camera.orthographic = ortho; camera.orthographicSize = 3.5f; camera.fieldOfView = 45;
                camera.nearClipPlane = .1f; camera.farClipPlane = 25; camera.allowHDR = false; camera.allowMSAA = false;
                camera.cullingMask = 1 << ReceiverLayer;
                camera.transform.position = new Vector3(0, 4.5f, -5.5f); camera.transform.LookAt(Vector3.zero);
                camera.targetTexture = target; target.filterMode = FilterMode.Point; target.Create();
                casterRenderer.enabled = false; var empty = Capture(camera, target, readback, output, "empty");
                casterRenderer.enabled = true;
                var fo = ShadowCapture(casterRenderer, frozen, false, camera, target, readback, output, "A-off");
                var bo = ShadowCapture(casterRenderer, current, false, camera, target, readback, output, "B-off");
                var co = ShadowCapture(casterRenderer, graph, false, camera, target, readback, output, "C-off");
                var fa = ShadowCapture(casterRenderer, frozen, true, camera, target, readback, output, "A");
                var ba = ShadowCapture(casterRenderer, current, true, camera, target, readback, output, "B");
                var ca = ShadowCapture(casterRenderer, graph, true, camera, target, readback, output, "C");
                var br = ShadowCapture(casterRenderer, current, true, camera, target, readback, output, "B-repeat");
                var cr = ShadowCapture(casterRenderer, graph, true, camera, target, readback, output, "C-repeat");
                foreach (var material in new[]{frozen,current,graph}) VerifyPass(material,"ShadowCaster");
                string rawVOIntent=EditorJsonUtility.ToJson(graph);
                Assert.That(ApplyVOPolicy(graph,false),Is.True);frozen.DisableKeyword("_VERTEX_OFFSET");current.DisableKeyword("_VERTEX_OFFSET");
                var voDeny=new Color[3][];var voDenyRepeat=new Color[3][];var voRestore=new Color[3][];var voRestoreRepeat=new Color[3][];var voMaterials=new[]{frozen,current,graph};
                for(int role=0;role<3;role++){voDeny[role]=ShadowCapture(casterRenderer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-denied");voDenyRepeat[role]=ShadowCapture(casterRenderer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-denied-repeat");}
                Assert.That(ApplyVOPolicy(graph,true),Is.True);frozen.EnableKeyword("_VERTEX_OFFSET");current.EnableKeyword("_VERTEX_OFFSET");
                for(int role=0;role<3;role++){voRestore[role]=ShadowCapture(casterRenderer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-restored");voRestoreRepeat[role]=ShadowCapture(casterRenderer,voMaterials[role],true,camera,target,readback,output,"ABC"[role]+"-vo-restored-repeat");}
                Assert.That(EditorJsonUtility.ToJson(graph),Is.EqualTo(rawVOIntent));
                var voMetric=new VOAuxMetrics{stage="ShadowCaster",finite=Finite(voDeny.Concat(voDenyRepeat).Concat(voRestore).Concat(voRestoreRepeat).ToArray()),ab=Delta(voDeny[0],voDeny[1]),bc=Delta(voDeny[1],voDeny[2]),repeatA=Delta(voDeny[0],voDenyRepeat[0])+Delta(voRestore[0],voRestoreRepeat[0]),repeatB=Delta(voDeny[1],voDenyRepeat[1])+Delta(voRestore[1],voRestoreRepeat[1]),repeatC=Delta(voDeny[2],voDenyRepeat[2])+Delta(voRestore[2],voRestoreRepeat[2]),responseA=Delta(fa,voDeny[0]),responseB=Delta(ba,voDeny[1]),responseC=Delta(ca,voDeny[2]),restoreA=Delta(fa,voRestore[0]),restoreB=Delta(ba,voRestore[1]),restoreC=Delta(ca,voRestore[2])};
                File.WriteAllText(Path.Combine(output,"vo-tier-metrics.json"),JsonUtility.ToJson(voMetric,true));Debug.Log("NBFX_VO_AUX "+JsonUtility.ToJson(voMetric));
                Assert.That(voMetric.finite,Is.True);Assert.That(voMetric.ab+voMetric.bc+voMetric.repeatA+voMetric.repeatB+voMetric.repeatC+voMetric.restoreA+voMetric.restoreB+voMetric.restoreC,Is.Zero);
                Assert.That(voMetric.responseA,Is.GreaterThan(.01f));Assert.That(voMetric.responseB,Is.GreaterThan(.01f));Assert.That(voMetric.responseC,Is.GreaterThan(.01f),"Actual geometry displacement must alter real depth/shadow response.");

                var metrics = new Metrics { caseId = id, stage = "real URP main-light ShadowCaster", api = SystemInfo.graphicsDeviceType.ToString(),
                    unityVersion = Application.unityVersion, orthographic = ortho,
                    note = "Alpha .25/.75; fixed .5/dither independent of alpha-test; full-frame raw linear RGBAHalf; ordinary Mesh only." };
                metrics.finite = Finite(empty, fo, bo, co, fa, ba, ca, br, cr);
                metrics.abMax = Delta(fa, ba, out metrics.abDifferent);
                metrics.bcMax = Delta(ba, ca, out metrics.bcDifferent);
                metrics.offMax = Math.Max(Delta(empty, bo), Delta(empty, co));
                metrics.legacyRepeat = Delta(ba, br); metrics.graphRepeat = Delta(ca, cr);
                metrics.legacyResponse = Delta(bo, ba); metrics.graphResponse = Delta(co, ca);
                if (mode == "cutoff80")
                {
                    foreach (var m in new[] { frozen, current, graph }) SetCutoff(m, .5f);
                    var fb = ShadowCapture(casterRenderer, frozen, true, camera, target, readback, output, "A-control");
                    var bb = ShadowCapture(casterRenderer, current, true, camera, target, readback, output, "B-control");
                    var cb = ShadowCapture(casterRenderer, graph, true, camera, target, readback, output, "C-control");
                    Assert.That(Delta(fb, bb), Is.Zero); Assert.That(Delta(bb, cb), Is.Zero);
                    metrics.controlResponse = Delta(bo, cb);
                }
                if (mode == "transparent-dither")
                {
                    foreach (var m in new[] { frozen, current, graph }) SetDither(m, m == graph, false);
                    var fixedB = ShadowCapture(casterRenderer, current, true, camera, target, readback, output, "B-fixed-control");
                    var fixedC = ShadowCapture(casterRenderer, graph, true, camera, target, readback, output, "C-fixed-control");
                    Assert.That(Delta(fixedB, fixedC), Is.Zero);
                    metrics.controlResponse = Delta(ba, fixedB);
                    Assert.That(Delta(ca, fixedC), Is.GreaterThan(.01f));
                }
                if (mode == "forward-only")
                {
                    foreach (var m in new[] { frozen, current, graph }) ForwardOnly(m, false);
                    var plainB = ShadowCapture(casterRenderer, current, true, camera, target, readback, output, "B-no-forward-only");
                    var plainC = ShadowCapture(casterRenderer, graph, true, camera, target, readback, output, "C-no-forward-only");
                    metrics.invariantLegacy = Delta(ba, plainB); metrics.invariantGraph = Delta(ca, plainC);
                }
                SaveMetrics(output, metrics);
                Assert.That(metrics.finite, Is.True);
                Assert.That(metrics.offMax, Is.Zero); Assert.That(Delta(fo, bo), Is.Zero); Assert.That(Delta(bo, co), Is.Zero);
                Assert.That(metrics.abMax, Is.Zero); Assert.That(metrics.bcMax, Is.Zero);
                Assert.That(metrics.legacyRepeat, Is.Zero); Assert.That(metrics.graphRepeat, Is.Zero);
                if (mode == "cutoff80") Assert.That(metrics.controlResponse, Is.GreaterThan(.01f));
                else { Assert.That(metrics.legacyResponse, Is.GreaterThan(.01f)); Assert.That(metrics.graphResponse, Is.GreaterThan(.01f)); }
                if (mode == "transparent-dither") Assert.That(metrics.controlResponse, Is.GreaterThan(.01f));
                if (mode == "forward-only")
                { Assert.That(metrics.invariantLegacy, Is.Zero); Assert.That(metrics.invariantGraph, Is.Zero); }
            }
            finally
            {
                RenderSettings.sun = oldSun; RenderSettings.ambientMode = oldAmbientMode;
                RenderSettings.ambientLight = oldAmbient; RenderSettings.fog = oldFog;
                camera.targetTexture = null; RenderTexture.active = oldActive; target.Release();
                foreach (var o in new UnityEngine.Object[] { receiver, caster, lightObject, cameraObject, graph, current, frozen, receiverMaterial, alphaMap, target, readback })
                    UnityEngine.Object.DestroyImmediate(o);
            }
        }

        static void RequirePipeline()
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
        }
        static Shader Load(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader && shader.isSupported, Is.True, path);
            return shader;
        }
        static Texture2D AlphaMap()
        {
            var t = new Texture2D(8, 8, TextureFormat.RGBA32, false, true);
            var pixels = new Color[64];
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                pixels[y * 8 + x] = new Color(1, 1, 1, x < 4 ? .25f : .75f);
            t.SetPixels(pixels); t.Apply(false); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }
        static void Configure(Material m, bool graph, string feature, float cutoff, bool transparent, bool dither, Texture2D map)
        {
            m.SetTexture("_BaseMap", map);
            m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            if (graph) m.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_Cull", 0); m.SetFloat("_ZTest", 4); m.SetFloat("_ZWrite", 1);
            if (m.HasProperty("_AffectsShadows")) m.SetFloat("_AffectsShadows", 1);
            if (m.HasProperty("_CastShadows")) m.SetFloat("_CastShadows", 1);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", transparent ? 1 : 0);
            if (m.HasProperty("_TransparentMode")) m.SetFloat("_TransparentMode", transparent ? 1 : cutoff >= 0 ? 2 : 0);
            m.SetFloat("_AlphaClip", cutoff >= 0 ? 1 : 0);
            SetCutoff(m, cutoff);
            Keyword(m, "_ALPHATEST_ON", cutoff >= 0);
            Keyword(m, "_SURFACE_TYPE_TRANSPARENT", transparent);
            m.renderQueue = 2100; // Explicitly keep all casters in the shadow renderer range.
            uint flags1 = transparent ? (dither ? 3u : 1u) : 0u;
            if (graph)
            {
                m.SetFloat("_NB_Flags1Lo16", flags1);
                m.SetFloat("_NB_Flags1Hi16", 0);
                m.SetFloat("_NB_Flags0Lo16", 0); m.SetFloat("_NB_Flags0Hi16", 0);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                m.SetFloat("_NB_DistortionMode", 0);
                m.SetFloat("_SrcBlend", 1); m.SetFloat("_DstBlend", 0);
            }
            else
            {
                m.SetInteger("_W9ParticleShaderFlags", 0);
                m.SetInteger("_W9ParticleShaderFlags1", (int)flags1);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
            }
            m.SetShaderPassEnabled("SRPDefaultUnlit", false);
            m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            m.SetShaderPassEnabled("UniversalForward", false);
            foreach (string pass in new[] { "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" })
                m.SetShaderPassEnabled(pass, false);
            if (feature == "mask")
            {
                m.SetFloat("_Mask_Toggle", 1); m.SetTexture("_MaskMap", map);
                m.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0));
                if (!graph) m.EnableKeyword("_MASKMAP_ON");
                uint channels = 3u | (3u << 2);
                if (graph) m.SetFloat("_NB_ColorChannelLo16", channels);
                else m.SetInteger("_W9ParticleShaderColorChannelFlag", (int)channels);
            }
            if (feature == "dissolve")
            {
                m.SetFloat("_Dissolve_Toggle", 1); m.SetTexture("_DissolveMap", map);
                m.SetVector("_Dissolve", new Vector4(.5f, 1, 0, .5f));
                if (!graph) m.EnableKeyword("_DISSOLVE");
                uint channels = 3u | (3u << 10);
                if (graph) m.SetFloat("_NB_ColorChannelLo16", channels);
                else m.SetInteger("_W9ParticleShaderColorChannelFlag", (int)channels);
            }
            if(feature=="vertexoffset")
            {
                m.SetFloat("_VertexOffset_Toggle",1);m.SetFloat("_VertexOffset_Mask_Toggle",1);m.SetFloat("_VertexOffset_NormalDir_Toggle",0);m.SetFloat("_VertexOffset_DirectionSpace",0);
                m.SetTexture("_VertexOffset_Map",Texture2D.whiteTexture);m.SetTexture("_VertexOffset_MaskMap",Texture2D.whiteTexture);
                m.SetVector("_VertexOffset_CustomDir",new Vector4(1,0,0,0));m.SetVector("_VertexOffset_Vec",new Vector4(0,0,.65f,0));m.SetVector("_VertexOffset_MaskMap_Vec",new Vector4(0,0,1,0));
                if(graph){InitializeGraphMaterial(m);Assert.That(ApplyVOPolicy(m,true),Is.True);}else{m.EnableKeyword("_VERTEX_OFFSET");m.EnableKeyword("_VERTEX_OFFSET_MASKMAP");}
            }
            if (feature == "forward-only") ForwardOnly(m, true);
        }
        static void InitializeGraphMaterial(Material material)
        {
            var syncType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("NBShaderEditor.NBShaderSyncService", false)).First(t => t != null);
            var initialize = syncType.GetMethod("TryInitializeGraphSupportedGateTierOnAssign",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Assert.That(initialize, Is.Not.Null, "Use the existing Graph assignment initializer.");
            float savedTier = material.GetFloat("_NBShaderFeatureTier");
            Assert.That((bool)initialize.Invoke(null, new object[] { material }), Is.True,
                "Fresh Graph material must initialize its real marker and registered Tier gates before policy projection.");
            Assert.That(material.GetFloat("_NB_GraphGUIStateVersion"), Is.EqualTo(2f));
            Assert.That(material.GetFloat("_NBShaderFeatureTier"), Is.EqualTo(savedTier), "Initialization preserves the saved Tier.");
        }

        static bool ApplyVOPolicy(Material graph,bool enabled)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier",false)).First(t=>t!=null);var tier=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShader.NBShaderFeatureTier",false)).First(t=>t!=null);
            object[] args={graph,Enum.ToObject(tier,3),enabled?new[]{"_VERTEX_OFFSET","_VERTEX_OFFSET_MASKMAP"}:new[]{"_VERTEX_OFFSET_MASKMAP"},false};return (bool)type.GetMethod("ApplyGraphVertexOffsetGroup",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,args);
        }
        [Serializable]sealed class VOAuxMetrics{public string stage;public bool finite;public float ab,bc,repeatA,repeatB,repeatC,responseA,responseB,responseC,restoreA,restoreB,restoreC;}
        static void ForwardOnly(Material m, bool on)
        {
            m.SetFloat("_BaseColorIntensityForTimeline", on ? .25f : 1);
            m.SetFloat("_EmissionEnabled", on ? 1 : 0);
            m.SetTexture("_EmissionMap", Texture2D.blackTexture);
            m.SetColor("_EmissionMapColor", Color.white);
            m.SetFloat("_EmissionAlphaIntensity", 1);
            m.SetFloat("_ColorBlendMap_Toggle", on ? 1 : 0);
            m.SetTexture("_ColorBlendMap", Texture2D.blackTexture);
            m.SetColor("_ColorBlendColor", Color.white);
            m.SetFloat("_RampColorToggle", on ? 1 : 0);
            if (m.HasProperty("_NB_Flags1Lo16")) m.SetFloat("_RampColorCount", 131074);
            else m.SetInteger("_RampColorCount", 131074);
            m.SetColor("_RampColor0", new Color(1, 1, 1, 0));
            m.SetColor("_RampColor1", Color.white);
            m.SetVector("_RampColorAlpha0", new Vector4(0, 0, 1, 1));
            m.SetColor("_RampColorBlendColor", Color.white);
            m.SetFloat("_fresnelEnabled", on ? 1 : 0);
            m.SetFloat("_DistanceFade_Toggle", on ? 1 : 0);
            m.SetFloat("_SoftParticlesEnabled", on ? 1 : 0);
            m.SetFloat("_DepthOutline_Toggle", on ? 1 : 0);
            if (m.HasProperty("_MatCapToggle")) m.SetFloat("_MatCapToggle", on ? 1 : 0);
            if (m.HasProperty("_MatCapTex")) m.SetTexture("_MatCapTex", Texture2D.blackTexture);
            if (m.HasProperty("_BumpMapToggle")) m.SetFloat("_BumpMapToggle", on ? 1 : 0);
            if (m.HasProperty("_BumpTex")) m.SetTexture("_BumpTex", Texture2D.whiteTexture);
            if (m.HasProperty("_NB_Flags0Lo16"))
            {
                m.SetFloat("_NB_Flags0Lo16", 0);
                m.SetFloat("_NB_Flags0Hi16", on ? (1 << 9) | (1 << 13) : 0); // overlay2 alpha / color-adjust alpha
                m.SetFloat("_NB_Flags1Hi16", on ? 1 << 15 : 0); // overlay1 alpha multiply
            }
            else
            {
                m.SetInteger("_W9ParticleShaderFlags", on ? (1 << 25) | (1 << 29) : 0);
                m.SetInteger("_W9ParticleShaderFlags1", on ? unchecked((int)(1u << 31)) : 0);
            }
            if (!m.HasProperty("_NB_Flags1Lo16"))
            {
                Keyword(m, "_EMISSION", on); Keyword(m, "_COLORMAPBLEND", on);
                Keyword(m, "_COLOR_RAMP", on); Keyword(m, "_FRESNEL", on);
                Keyword(m, "_DISTANCE_FADE", on); Keyword(m, "_SOFTPARTICLES_ON", on);
                Keyword(m, "_DEPTH_OUTLINE", on);
                Keyword(m, "_MATCAP", on); Keyword(m, "_NORMALMAP", on);
            }
        }
        static void SetDither(Material m, bool graph, bool on)
        {
            if (m.HasProperty("_TransparentShadowDitherToggle")) m.SetFloat("_TransparentShadowDitherToggle", on ? 1 : 0);
            if (graph) m.SetFloat("_NB_Flags1Lo16", on ? 3 : 1);
            else m.SetInteger("_W9ParticleShaderFlags1", on ? 3 : 1);
        }
        static void SetCutoff(Material m, float cutoff) { m.SetFloat("_Cutoff", cutoff >= 0 ? cutoff : .5f); }
        static void Keyword(Material m, string name, bool on) { if (on) m.EnableKeyword(name); else m.DisableKeyword(name); }
        static void VerifyPass(Material m, string lightMode)
        {
            int index = m.FindPass(lightMode);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), m.shader.name + " lacks " + lightMode);
            Assert.That(m.shader.FindPassTagValue(0, index, new ShaderTagId("LightMode")).name,
                Is.EqualTo(lightMode).IgnoreCase);
        }
        static Color[] DepthCapture(MeshRenderer renderer, Material m, bool on, Camera camera,
            RenderTexture target, Texture2D readback, string output, string name)
        { renderer.sharedMaterial = m; m.SetShaderPassEnabled("DepthOnly", on); return Capture(camera, target, readback, output, name); }
        static Color[] ShadowCapture(MeshRenderer renderer, Material m, bool on, Camera camera,
            RenderTexture target, Texture2D readback, string output, string name)
        { renderer.sharedMaterial = m; m.SetShaderPassEnabled("ShadowCaster", on); return Capture(camera, target, readback, output, name); }
        static Color[] Capture(Camera camera, RenderTexture target, Texture2D readback, string output, string name)
        {
            for (int i = 0; i < 3; i++) camera.Render();
            var old = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); readback.Apply(false);
                var pixels = readback.GetPixels();
                File.WriteAllBytes(Path.Combine(output, name + ".png"), readback.EncodeToPNG());
                using (var stream = File.Create(Path.Combine(output, name + ".rgba-f32.gz")))
                using (var zip = new GZipStream(stream, CompressionMode.Compress))
                using (var writer = new BinaryWriter(zip))
                    foreach (var pixel in pixels) { writer.Write(pixel.r); writer.Write(pixel.g); writer.Write(pixel.b); writer.Write(pixel.a); }
                return pixels;
            }
            finally { RenderTexture.active = old; }
        }
        static bool Finite(params Color[][] all)
        {
            foreach (var pixels in all)
            {
                if (pixels == null || pixels.Length != Size * Size) return false;
                foreach (var p in pixels)
                    for (int channel = 0; channel < 4; channel++)
                        if (float.IsNaN(p[channel]) || float.IsInfinity(p[channel])) return false;
            }
            return true;
        }
        static float Delta(Color[] a, Color[] b) { return Delta(a, b, out _); }
        static float Delta(Color[] a, Color[] b, out int different)
        {
            Assert.That(a.Length, Is.EqualTo(b.Length)); different = 0; float max = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float d = Math.Max(Math.Max(Math.Abs(a[i].r - b[i].r), Math.Abs(a[i].g - b[i].g)),
                    Math.Max(Math.Abs(a[i].b - b[i].b), Math.Abs(a[i].a - b[i].a)));
                if (d != 0) different++; max = Math.Max(max, d);
            }
            return max;
        }
        static string Output(string id)
        {
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4DepthShadow");
            var output = Path.Combine(root, id); Directory.CreateDirectory(output); return output;
        }
        static void SaveMetrics(string output, Metrics metrics)
        { File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true)); Debug.Log("NBFX_G4_DEPTH_SHADOW " + JsonUtility.ToJson(metrics)); }
    }
}
