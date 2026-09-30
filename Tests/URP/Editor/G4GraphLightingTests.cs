using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    /// <summary>Ordinary Mesh lighting and explicit SH-keyword evaluation; root owns Unity verification.
    /// A=Frozen, B=current ShaderLab, C=Graph. No LM/APV/vertex-light/SixWay/VFX/Player claim.</summary>
    public sealed class G4GraphLightingTests
    {
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size = 96, Layer = 2, Min = 28, Max = 68;
        static readonly string[] Modes = { "unlit", "blinn", "half-lambert", "pbr" };
        [Serializable] sealed class Metrics
        {
            public string caseId, unityVersion, api, limitation;
            public int mode, pixels, abDiff, bcDiff, controlBCDiff, bRepeatDiff, repeatDiff, controlRepeatDiff, strongBPixels, strongCPixels;
            public float abMax, bcMax, controlBCMax, bRepeatMax, repeatMax, controlRepeatMax, strongBMax, strongCMax;
            public bool finite, mainPositive, shPositive, additionalPositive, legacyForwardShadowInvariant, normalMaskPositive;
        }
        [OneTimeSetUp]
        public void OneTimeSetupForceGraphImport()
        {
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ForceSynchronousImport);
        }
        static IEnumerable<TestCaseData> Cases()
        {
            for (int mode = 0; mode < Modes.Length; mode++)
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData(mode, ortho).SetName("G4LightingABC_" + Modes[mode] + (ortho ? "_ortho" : "_perspective"));
        }
        static IEnumerable<TestCaseData> SHCases()
        {
            for (int mode = 1; mode < Modes.Length; mode++)
                foreach (bool ortho in new[] { true, false })
                    foreach (string keyword in new[] { "EVALUATE_SH_VERTEX", "EVALUATE_SH_MIXED" })
                        yield return new TestCaseData(mode, ortho, keyword).SetName("G4LightingSHABC_" + Modes[mode] + (ortho ? "_ortho_" : "_perspective_") + keyword);
        }
        [TestCaseSource(nameof(SHCases))]
        public void OrdinaryMeshSHEvaluation(int mode, bool ortho, string shKeyword)
            => Replay(mode, ortho, shKeyword);
        [TestCaseSource(nameof(Cases))]
        public void OrdinaryMeshLightingL0(int mode, bool ortho)
            => Replay(mode, ortho, null);
        void Replay(int mode, bool ortho, string shKeyword)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var frozenShader = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            var currentShader = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath);
            var graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(frozenShader && currentShader && graphShader &&
                frozenShader.isSupported && currentShader.isSupported && graphShader.isSupported, Is.True);
            Assert.That(frozenShader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            string id = Modes[mode] + (ortho ? "-ortho" : "-perspective") + (shKeyword == null ? "" : "-" + shKeyword);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4Lighting");
            string output = Path.Combine(root, id); Directory.CreateDirectory(output);
            Scene previousScene = SceneManager.GetActiveScene();
            var oldMode = RenderSettings.ambientMode;
            var oldProbe = RenderSettings.ambientProbe;
            // The batch test runner owns an untitled scene. EditMode forbids
            // CreateScene, and NewScene(Additive) rejects that untitled scene.
            // Use only this isolated runner scene in memory and restore its SH.
            // No scene/settings asset is saved; all created objects are destroyed.
            Scene scene = previousScene;
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cameraGO = new GameObject("L0 camera");
            var sunGO = new GameObject("L0 main light", typeof(Light));
            var pointGO = new GameObject("L0 additional light", typeof(Light));
            var blockerShader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(blockerShader && blockerShader.isSupported, Is.True);
            var blockerMaterial = new Material(blockerShader);
            var a = new Material(frozenShader); var b = new Material(currentShader); var c = new Material(graphShader);
            foreach (string p in new[] { "_FxLightMode", "_MaterialInfo", "_SpecularColor" })
                Assert.That(c.HasProperty(p), Is.True, "L0 missing Graph property " + p);
            var baseMap = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            baseMap.SetPixel(0, 0, new Color(.55f, .35f, .18f, 1)); baseMap.Apply(false);
            var bump = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            bump.SetPixel(0, 0, new Color(.7f, .3f, .45f, .2f)); bump.Apply(false);
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var read = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            RenderTexture previousRT = RenderTexture.active;
            try
            {
                foreach (var go in new[] { quad, occluder, cameraGO, sunGO, pointGO }) SceneManager.MoveGameObjectToScene(go, scene);
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene.handle),
                    "Isolated test-runner Scene must remain active for SH probe assignment.");
                Debug.Log("NBFX L0 runner scene: name=" + scene.name + " path=" + scene.path +
                    " loaded=" + scene.isLoaded + " handle=" + scene.handle);
                var mesh = quad.GetComponent<MeshRenderer>();
                quad.layer = Layer; quad.transform.localScale = new Vector3(2, 2, 1);
                quad.transform.rotation = Quaternion.Euler(0, 18, 0);
                mesh.shadowCastingMode = ShadowCastingMode.Off; mesh.receiveShadows = true;
                occluder.layer = Layer; occluder.transform.position = new Vector3(.25f, 0, .55f);
                occluder.transform.localScale = new Vector3(.45f, 1.35f, .45f);
                occluder.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                occluder.GetComponent<MeshRenderer>().sharedMaterial = blockerMaterial;
                occluder.GetComponent<MeshRenderer>().enabled = false; // shadow-only, no camera occlusion.
                var camera = cameraGO.AddComponent<Camera>();
                camera.scene = scene; camera.orthographic = ortho; camera.orthographicSize = 1.5f;
                camera.fieldOfView = 43; camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                camera.cullingMask = 1 << Layer; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.0625f, .125f, .1875f, 1);
                camera.allowHDR = true; camera.allowMSAA = false; camera.targetTexture = rt;
                cameraGO.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                var sun = sunGO.GetComponent<Light>(); sun.type = LightType.Directional;
                sun.color = new Color(.9f, .8f, .7f); sun.intensity = shKeyword == null ? 1.4f : 4.2f; sun.shadows = LightShadows.None;
                sun.transform.rotation = Quaternion.LookRotation(new Vector3(-.4f, -.2f, -1).normalized);
                var point = pointGO.GetComponent<Light>(); point.type = LightType.Point;
                point.color = Color.blue; point.intensity = 4; point.range = 7;
                point.transform.position = new Vector3(.7f, .4f, 1.3f); point.enabled = false;
                var sh = new SphericalHarmonicsL2(); sh.AddDirectionalLight(Vector3.forward, new Color(.25f, .05f, .02f), 1);
                // Explicit SH variants need stronger controls: the old vertex SH
                // path weakens this normal/probe fixture below its response threshold.
                // Keep L0 input values and all strict parity thresholds unchanged.
                if (shKeyword != null) sh.AddAmbientLight(new Color(.18f, .06f, .02f));
                RenderSettings.ambientMode = AmbientMode.Custom; RenderSettings.ambientProbe = sh;
                foreach (var m in new[] { a, b, c }) Configure(m, m == c, mode, baseMap);
                if (shKeyword != null) foreach (var m in new[] { a, b, c }) m.EnableKeyword(shKeyword);
                rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                mesh.sharedMaterial = a; var aa = Capture(camera, rt, read, output, "A-frozen");
                mesh.sharedMaterial = b; var bb = Capture(camera, rt, read, output, "B-current");
                var br = Capture(camera, rt, read, output, "B-repeat");
                mesh.sharedMaterial = c; var cc = Capture(camera, rt, read, output, "C-graph");
                var cr = Capture(camera, rt, read, output, "C-repeat");
                var metrics = new Metrics { caseId = id, unityVersion = Application.unityVersion,
                    api = SystemInfo.graphicsDeviceType.ToString(), mode = mode,
                    limitation = "Mesh SH=" + (shKeyword ?? "pixel(default)") + "; pixel additional and legacy Forward shadow receiving invariance. SH GUI selection, curved Mesh, LM/APV/vertex additional/SixWay/Forward+/VFX/Player not claimed here." };
                Compare(aa, bb, out metrics.abDiff, out metrics.abMax);
                Compare(bb, cc, out metrics.bcDiff, out metrics.bcMax);
                Compare(bb, br, out metrics.bRepeatDiff, out metrics.bRepeatMax);
                Compare(cc, cr, out metrics.repeatDiff, out metrics.repeatMax);
                metrics.pixels = (Max-Min)*(Max-Min);
                metrics.finite = Finite(aa) && Finite(bb) && Finite(br) && Finite(cc) && Finite(cr);
                // Strong main-light counterfactual: flip the directional light away.
                sun.transform.rotation = Quaternion.LookRotation(new Vector3(.4f, .2f, 1).normalized);
                mesh.sharedMaterial = b; var bn = Capture(camera, rt, read, output, "B-main-away");
                var bnRepeat = Capture(camera, rt, read, output, "B-main-away-repeat");
                mesh.sharedMaterial = c; var cn = Capture(camera, rt, read, output, "C-main-away");
                var cnRepeat = Capture(camera, rt, read, output, "C-main-away-repeat");
                AddControlRepeat(metrics, bn, bnRepeat);
                AddControlRepeat(metrics, cn, cnRepeat);
                int bCount, cCount; float bDelta, cDelta;
                AddControlBC(metrics, bn, cn);
                Compare(bb, bn, out bCount, out bDelta, .01f); Compare(cc, cn, out cCount, out cDelta, .01f);
                metrics.mainPositive = mode == 0 || bCount >= 64 && cCount >= 64;
                metrics.strongBPixels = bCount; metrics.strongCPixels = cCount;
                metrics.strongBMax = bDelta; metrics.strongCMax = cDelta;
                sun.transform.rotation = Quaternion.LookRotation(new Vector3(-.4f, -.2f, -1).normalized);
                // Controlled SH probe change, with state restored in finally.
                var sh2 = new SphericalHarmonicsL2(); sh2.AddDirectionalLight(Vector3.forward, new Color(.02f, .05f, .25f), 1);
                if (shKeyword != null) sh2.AddAmbientLight(new Color(.02f, .06f, .2f));
                RenderSettings.ambientProbe = sh2;
                mesh.sharedMaterial = b; var bs = Capture(camera, rt, read, output, "B-SH-blue");
                var bsRepeat = Capture(camera, rt, read, output, "B-SH-blue-repeat");
                mesh.sharedMaterial = c; var cs = Capture(camera, rt, read, output, "C-SH-blue");
                var csRepeat = Capture(camera, rt, read, output, "C-SH-blue-repeat");
                AddControlRepeat(metrics, bs, bsRepeat);
                AddControlRepeat(metrics, cs, csRepeat);
                AddControlBC(metrics, bs, cs);
                Compare(bb, bs, out bCount, out bDelta, .005f); Compare(cc, cs, out cCount, out cDelta, .005f);
                metrics.shPositive = mode == 0 || bCount >= 64 && cCount >= 64;
                RenderSettings.ambientProbe = sh;
                if (pipeline.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
                {
                    point.enabled = true;
                    mesh.sharedMaterial = b; var bp = Capture(camera, rt, read, output, "B-point-on");
                    var bpRepeat = Capture(camera, rt, read, output, "B-point-on-repeat");
                    mesh.sharedMaterial = c; var cp = Capture(camera, rt, read, output, "C-point-on");
                    var cpRepeat = Capture(camera, rt, read, output, "C-point-on-repeat");
                    AddControlRepeat(metrics, bp, bpRepeat);
                    AddControlRepeat(metrics, cp, cpRepeat);
                    AddControlBC(metrics, bp, cp);
                    Compare(bb, bp, out bCount, out bDelta, .01f); Compare(cc, cp, out cCount, out cDelta, .01f);
                    metrics.additionalPositive = mode == 0 || bCount >= 64 && cCount >= 64;
                    point.enabled = false;
                }
                else metrics.additionalPositive = false;
                if (pipeline.supportsMainLightShadows && pipeline.shadowDistance > 5)
                {
                    sun.shadows = LightShadows.Hard; occluder.GetComponent<MeshRenderer>().enabled = true;
                    occluder.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    mesh.sharedMaterial = b; var bh = Capture(camera, rt, read, output, "B-shadow-on");
                    var bhRepeat = Capture(camera, rt, read, output, "B-shadow-on-repeat");
                    mesh.sharedMaterial = c; var ch = Capture(camera, rt, read, output, "C-shadow-on");
                    var chRepeat = Capture(camera, rt, read, output, "C-shadow-on-repeat");
                    AddControlRepeat(metrics, bh, bhRepeat);
                    AddControlRepeat(metrics, ch, chRepeat);
                    AddControlBC(metrics, bh, ch);
                    Compare(bb, bh, out bCount, out bDelta); Compare(cc, ch, out cCount, out cDelta);
                    // Existing NB Forward has no shadow keyword axes. Casting (DS0)
                    // is separate from receiving: this must remain invariant.
                    metrics.legacyForwardShadowInvariant = bCount == 0 && cCount == 0;
                }
                else metrics.legacyForwardShadowInvariant = false;
                // N0's one normal-map sample must also carry the mask-mode
                // metallic/smoothness weights into the L0 lighting call.
                sun.shadows = LightShadows.None; occluder.GetComponent<MeshRenderer>().enabled = false;
                foreach (var m in new[] { b, c })
                {
                    m.SetFloat("_BumpMapToggle", 1); m.SetTexture("_BumpTex", bump);
                    m.SetFloat("_BumpScale", .8f);
                }
                b.SetInteger("_W9ParticleShaderFlags", 1 << 21); b.EnableKeyword("_NORMALMAP");
                c.SetFloat("_NB_Flags0Hi16", 1 << (21 - 16));
                mesh.sharedMaterial = b; var bm = Capture(camera, rt, read, output, "B-normal-mask");
                var bmRepeat = Capture(camera, rt, read, output, "B-normal-mask-repeat");
                mesh.sharedMaterial = c; var cm = Capture(camera, rt, read, output, "C-normal-mask");
                var cmRepeat = Capture(camera, rt, read, output, "C-normal-mask-repeat");
                AddControlRepeat(metrics, bm, bmRepeat);
                AddControlRepeat(metrics, cm, cmRepeat);
                AddControlBC(metrics, bm, cm);
                Compare(bb, bm, out bCount, out bDelta, .01f);
                Compare(cc, cm, out cCount, out cDelta, .01f);
                metrics.normalMaskPositive = mode == 0 || bCount >= 64 && cCount >= 64;
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Assert.That(metrics.finite, Is.True);
                Assert.That(metrics.abDiff, Is.Zero, "strict raw Frozen A / current B differs; see evidence");
                Assert.That(metrics.bRepeatDiff, Is.Zero, "raw B repeat differs; see evidence");
                Assert.That(metrics.repeatDiff, Is.Zero, "raw C repeat differs; see evidence");
                Assert.That(metrics.controlRepeatDiff, Is.Zero, "raw B/C control repeat differs; see evidence");
                Assert.That(metrics.bcDiff, Is.Zero, "strict raw B/C L0 differs; see metrics/raw RGBA");
                Assert.That(metrics.controlBCDiff, Is.Zero, "strict raw B/C L0 controls differ; see metrics/raw RGBA");
                if (mode != 0)
                {
                    Assert.That(metrics.mainPositive && metrics.shPositive && metrics.normalMaskPositive,
                        Is.True, "main/SH/normal-mask control ineffective");
                    if (pipeline.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
                        Assert.That(metrics.additionalPositive, Is.True, "point-light control ineffective");
                    if (pipeline.supportsMainLightShadows && pipeline.shadowDistance > 5)
                        Assert.That(metrics.legacyForwardShadowInvariant, Is.True, "old Forward shadow invariance changed");
                }
            }
            finally
            {
                RenderSettings.ambientMode = oldMode; RenderSettings.ambientProbe = oldProbe;
                RenderTexture.active = previousRT;
                var ownedCamera = cameraGO.GetComponent<Camera>();
                if (ownedCamera) ownedCamera.targetTexture = null;
                foreach (var o in new UnityEngine.Object[] { a, b, c, blockerMaterial, baseMap, bump, rt, read }) if (o) UnityEngine.Object.DestroyImmediate(o);
                foreach (var go in new[] { quad, occluder, cameraGO, sunGO, pointGO }) if (go) UnityEngine.Object.DestroyImmediate(go);
            }
        }
        static void Configure(Material m, bool graph, int mode, Texture2D baseMap)
        {
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } :
                new[] { mode == 0 ? "_FX_LIGHT_MODE_UNLIT" : mode == 1 ? "_FX_LIGHT_MODE_BLINN_PHONG" :
                    mode == 2 ? "_FX_LIGHT_MODE_HALF_LAMBERT" : "_FX_LIGHT_MODE_PBR" };
            m.SetTexture("_BaseMap", baseMap); m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white); m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_FxLightMode", mode); m.SetVector("_MaterialInfo", new Vector4(.6f, .75f, 0, 0));
            m.SetColor("_SpecularColor", new Color(.8f, .7f, .6f, 1));
            m.SetFloat("_Cull", (float)CullMode.Off); m.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite", 0); m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_BaseMapUVRotation", 0); m.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
            if (graph)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_ColorChannelLo16", 3); m.SetFloat("_NB_Flags1Lo16", 1 + (1 << 9));
                m.SetFloat("_NB_DistortionMode", 0);
            }
            else
            {
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetInteger("_W9ParticleShaderFlags", 0); m.SetInteger("_W9ParticleShaderFlags1", 1 + (1 << 9));
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                foreach (var p in new[] { "_W9ParticleCustomDataFlag0", "_W9ParticleCustomDataFlag1", "_W9ParticleCustomDataFlag2", "_W9ParticleCustomDataFlag3" }) m.SetInteger(p, 0);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false); m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
                m.SetShaderPassEnabled("UniversalForward", true);
            }
            foreach (string pass in new[] { "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" })
                m.SetShaderPassEnabled(pass, false);
            m.renderQueue = 3000;
        }
        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D read, string dir, string name)
        {
            for (int i = 0; i < 3; i++) camera.Render();
            var old = RenderTexture.active;
            try
            {
                RenderTexture.active = rt; read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); read.Apply(false);
                var px = read.GetPixels();
                using (var f = File.Create(Path.Combine(dir, name + ".rgba-f32.gz")))
                using (var z = new GZipStream(f, CompressionMode.Compress))
                using (var w = new BinaryWriter(z)) foreach (var c in px) { w.Write(c.r); w.Write(c.g); w.Write(c.b); w.Write(c.a); }
                return px;
            }
            finally { RenderTexture.active = old; }
        }
        static void Compare(Color[] a, Color[] b, out int count, out float maximum, float threshold = 0)
        {
            count = 0; maximum = 0;
            for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++)
            {
                int i = y*Size+x; float d = 0;
                for (int k = 0; k < 4; k++) d = Mathf.Max(d, Mathf.Abs(a[i][k]-b[i][k]));
                maximum = Mathf.Max(maximum, d); if (d > threshold) count++;
            }
        }
        static void AddControlRepeat(Metrics m, Color[] first, Color[] repeated)
        {
            int count; float maximum; Compare(first, repeated, out count, out maximum);
            m.controlRepeatDiff += count;
            m.controlRepeatMax = Mathf.Max(m.controlRepeatMax, maximum);
            m.finite &= Finite(first) && Finite(repeated);
        }
        static void AddControlBC(Metrics m, Color[] b, Color[] c)
        {
            int count; float maximum; Compare(b, c, out count, out maximum);
            m.controlBCDiff += count; m.controlBCMax = Mathf.Max(m.controlBCMax, maximum);
            m.finite &= Finite(b) && Finite(c);
        }
        static bool Finite(Color[] px)
        { foreach (var c in px) for (int k = 0; k < 4; k++) if (float.IsNaN(c[k]) || float.IsInfinity(c[k])) return false; return true; }
    }
}
