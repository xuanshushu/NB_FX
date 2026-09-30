using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    // Ordinary Forward output order, not shadow/dither, VFX, GUI or postprocess.
    public sealed class G4GraphBlendClipTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 2;
        Shader _background;
        [OneTimeSetUp] public void Setup()
        {
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            _background = ShaderUtil.CreateShaderAsset("Shader \"Hidden/NBFX/G4BlendClipBoard\" { SubShader { Tags { \"RenderPipeline\"=\"UniversalPipeline\" \"Queue\"=\"Geometry\" } Pass { Tags { \"LightMode\"=\"UniversalForward\" } Cull Off ZWrite Off ZTest Always Blend One Zero HLSLPROGRAM\n#pragma vertex Vert\n#pragma fragment Frag\n#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\nstruct A { float4 p:POSITION; }; struct V { float4 p:SV_POSITION; }; V Vert(A i) { V o; o.p=TransformObjectToHClip(i.p.xyz); return o; } half4 Frag(V i):SV_Target { return half4(.125,.25,.5,.25); }\nENDHLSL\n} } }", false);
            Assert.That(_background && _background.isSupported, Is.True);
            _background.hideFlags = HideFlags.HideAndDontSave;
        }
        [OneTimeTearDown] public void Teardown() { if (_background) UnityEngine.Object.DestroyImmediate(_background); }
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string mode in new[] { "opaque", "alpha", "premultiply", "additive", "additive-mix", "multiply" })
                foreach (float cutoff in new[] { -1f, .25f, .5f, .8f })
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(mode, cutoff, ortho).SetName("G4BlendClipBC_" + mode + "_" + cutoff.ToString(System.Globalization.CultureInfo.InvariantCulture) + (ortho ? "_ortho" : "_perspective"));
        }
        [Serializable] sealed class Metrics
        {
            public string caseId, unityVersion, api, note, blend;
            public bool orthographic, finite, alphaTest;
            public float frozenCurrentMax, frozenCurrentControlMax;
            public float cutoff, additiveAlpha, maxRGBA, controlMaxRGBA, legacyRepeat, graphRepeat, legacyResponse, graphResponse;
            public int differingRGBA, controlDifferingRGBA;
        }
        [TestCaseSource(nameof(Cases))]
        public void FinalForwardBlendAndCutoffMatch(string mode, float cutoff, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            var graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            var legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            var frozenShader = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            Assert.That(frozenShader && frozenShader.isSupported && frozenShader.name == "Effects/NBShader_T00_Frozen", Is.True);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            var scene = EditorSceneManager.NewPreviewScene();
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var board = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX ordinary Forward Blend/Clip");
            var camera = cameraObject.AddComponent<Camera>();
            var f = new Material(frozenShader); var g = new Material(graphShader); var b = new Material(legacyShader); var backdrop = new Material(_background);
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var texture = new Texture2D(8, 8, TextureFormat.RGBAHalf, false, true);
            var previous = RenderTexture.active;
            string id = mode + "-" + cutoff.ToString(System.Globalization.CultureInfo.InvariantCulture) + (ortho ? "-ortho" : "-perspective");
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4BlendClip");
            string output = Path.Combine(root, "blend-clip-" + id); Directory.CreateDirectory(output);
            try
            {
                var pixels = new Color[64];
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) pixels[y * 8 + x] = new Color(.5f, .25f, .75f, x < 4 ? .25f : .75f);
                texture.SetPixels(pixels); texture.Apply(false); texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                foreach (var go in new[] { front, board, cameraObject }) SceneManager.MoveGameObjectToScene(go, scene);
                Configure(f, false, mode, cutoff, texture); Configure(g, true, mode, cutoff, texture); Configure(b, false, mode, cutoff, texture);
                Assert.That(g.HasProperty("_Cutoff") && g.HasProperty("_AdditiveToPreMultiplyAlphaLerp"), Is.True, "Missing Graph final-output properties.");
                front.layer = board.layer = Layer;
                front.transform.position = Vector3.zero; front.transform.localScale = new Vector3(2, 2, 1);
                board.transform.position = new Vector3(0, 0, -.1f); board.transform.localScale = new Vector3(10, 10, 1);
                board.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                var renderer = front.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                camera.scene = scene; camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 45;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20; camera.transform.position = new Vector3(0, 0, 4); camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.allowHDR = true; camera.allowMSAA = false;
                target.Create(); Assert.That(target.IsCreated() && !target.sRGB, Is.True); camera.targetTexture = target;
                renderer.sharedMaterial = f; var fv = Capture(camera, target, readback, Path.Combine(output, "A-frozen"));
                renderer.sharedMaterial = b;
                var bv = Capture(camera, target, readback, Path.Combine(output, "B")); var br = Capture(camera, target, readback, Path.Combine(output, "B-repeat"));
                renderer.sharedMaterial = g;
                var gv = Capture(camera, target, readback, Path.Combine(output, "C")); var gr = Capture(camera, target, readback, Path.Combine(output, "C-repeat"));
                // Clip's zero-effect states get a nonzero alternative threshold;
                // clipped-all states get clip-off. The output order must respond.
                float controlCutoff = cutoff >= .5f || mode == "additive" && cutoff > 0 ? -1 : .8f;
                Configure(f, false, mode, controlCutoff, texture); Configure(b, false, mode, controlCutoff, texture); Configure(g, true, mode, controlCutoff, texture);
                renderer.sharedMaterial = f; var fc = Capture(camera, target, readback, Path.Combine(output, "A-frozen-control"));
                renderer.sharedMaterial = b; var bc = Capture(camera, target, readback, Path.Combine(output, "B-control"));
                renderer.sharedMaterial = g; var gc = Capture(camera, target, readback, Path.Combine(output, "C-control"));
                var metrics = new Metrics { caseId=id,unityVersion=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),blend=mode,cutoff=cutoff,alphaTest=cutoff>=0,orthographic=ortho,additiveAlpha=mode=="additive"?0:mode=="additive-mix"?.5f:1,finite=Finite(bv)&&Finite(gv)&&Finite(br)&&Finite(gr)&&Finite(bc)&&Finite(gc),note="Ordinary Forward B/C only; explicit equal material blend factors; strict linearRGBAHalf ROI x/y32..95; 4 warm-up renders; not shadow/depth/dither/GUI/Player/VFX." };
                Compare(bv, gv, out metrics.maxRGBA, out metrics.differingRGBA); Compare(bc,gc,out metrics.controlMaxRGBA,out metrics.controlDifferingRGBA);
                metrics.frozenCurrentMax=Delta(fv,bv); metrics.frozenCurrentControlMax=Delta(fc,bc);
                metrics.finite &= Finite(fv)&&Finite(fc);
                metrics.legacyRepeat=Delta(bv,br); metrics.graphRepeat=Delta(gv,gr); metrics.legacyResponse=Delta(bv,bc);metrics.graphResponse=Delta(gv,gc);
                File.WriteAllText(Path.Combine(output,"metrics.json"),JsonUtility.ToJson(metrics,true)); Debug.Log("NBFX_G4_BLEND_CLIP "+JsonUtility.ToJson(metrics));
                Assert.That(metrics.frozenCurrentMax,Is.Zero,"Shared blend extraction changed Frozen behavior.");Assert.That(metrics.frozenCurrentControlMax,Is.Zero);
                Assert.That(metrics.finite,Is.True);Assert.That(metrics.legacyRepeat,Is.Zero);Assert.That(metrics.graphRepeat,Is.Zero);
                Assert.That(metrics.legacyResponse,Is.GreaterThan(.04f),"Legacy clip counterfactual inactive.");Assert.That(metrics.graphResponse,Is.GreaterThan(.04f),"Graph clip counterfactual inactive.");
                Assert.That(metrics.controlDifferingRGBA,Is.Zero,"Counterfactual strict B/C mismatch.");Assert.That(metrics.differingRGBA,Is.Zero,"Final Forward output strict B/C mismatch.");
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=null;target.Release();
                foreach(var o in new UnityEngine.Object[]{texture,readback,target,g,b,f,backdrop})UnityEngine.Object.DestroyImmediate(o);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        static void Configure(Material m, bool graph, string mode, float cutoff, Texture2D texture)
        {
            m.SetTexture("_BaseMap",texture);m.SetColor(graph?"_Color":"_BaseColor",Color.white);m.SetColor("_ColorA",Color.white);m.SetFloat("_AlphaAll",1);m.SetFloat("_BaseColorIntensityForTimeline",1);
            m.SetFloat("_Cull",0);m.SetFloat("_ZTest",4);m.SetFloat("_ZWrite",0);m.SetFloat("_ColorMask",15);
            bool opaque=mode=="opaque",premul=mode=="premultiply"||mode.StartsWith("additive",StringComparison.Ordinal),multiply=mode=="multiply";
            if(graph)m.SetFloat("_Surface",opaque?0:1);m.SetFloat("_SrcBlend",opaque||premul?(float)BlendMode.One:multiply?(float)BlendMode.DstColor:(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",opaque?(float)BlendMode.Zero:(float)BlendMode.OneMinusSrcAlpha);
            if(graph){m.SetFloat("_SrcBlendAlpha",m.GetFloat("_SrcBlend"));m.SetFloat("_DstBlendAlpha",m.GetFloat("_DstBlend"));m.SetFloat("_NB_DistortionMode",0);m.SetFloat("_NB_ColorChannelLo16",3);m.SetFloat("_NB_Flags1Lo16",opaque?0:1);}
            else{m.SetInteger("_W9ParticleShaderColorChannelFlag",3);m.SetInteger("_W9ParticleShaderFlags",0);m.SetInteger("_W9ParticleShaderFlags1",opaque?0:1);m.SetFloat("_fogintensity",0);m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");m.SetShaderPassEnabled("SRPDefaultUnlit",false);m.SetShaderPassEnabled("SRPDEFAULTUNLIT",false);}
            m.SetFloat("_Cutoff",cutoff<0?.5f:cutoff);m.SetFloat("_AlphaClip",cutoff>=0?1:0);m.SetFloat("_AdditiveToPreMultiplyAlphaLerp",mode=="additive"?0:mode=="additive-mix"?.5f:1);
            Keyword(m,"_SURFACE_TYPE_TRANSPARENT",!opaque);Keyword(m,"_ALPHAPREMULTIPLY_ON",premul);Keyword(m,"_ALPHAMODULATE_ON",multiply);Keyword(m,"_ALPHATEST_ON",cutoff>=0);m.renderQueue=3000;
            // This fixture measures Forward only. NB's additional passes do
            // not self-discard when _SCREEN_DISTORT_MODE is absent; leaving
            // them enabled would redraw this Mesh via the project's feature.
            foreach(string pass in new[]{"DepthOnly","ShadowCaster","NBCameraOpaqueDistortPass","NBDeferredDistortPass","Universal2D"})m.SetShaderPassEnabled(pass,false);
        }
        static void Keyword(Material m,string name,bool on){if(on)m.EnableKeyword(name);else m.DisableKeyword(name);}
        static Color[] Capture(Camera camera,RenderTexture target,Texture2D readback,string path)
        {
            for(int i=0;i<4;i++)camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,Size,Size),0,0);readback.Apply(false);var p=readback.GetPixels();
            File.WriteAllBytes(path+".png",readback.EncodeToPNG());using(var s=File.Create(path+".rgba-f32.gz"))using(var z=new GZipStream(s,CompressionMode.Compress))using(var w=new BinaryWriter(z))foreach(var c in p){w.Write(c.r);w.Write(c.g);w.Write(c.b);w.Write(c.a);}return p;
        }
        static bool Finite(Color[] a){foreach(var p in a)for(int c=0;c<4;c++)if(float.IsNaN(p[c])||float.IsInfinity(p[c]))return false;return true;}
        static void Compare(Color[] a,Color[] b,out float max,out int different){max=0;different=0;for(int y=32;y<96;y++)for(int x=32;x<96;x++){bool d=false;for(int c=0;c<4;c++){float e=Mathf.Abs(a[y*Size+x][c]-b[y*Size+x][c]);max=Mathf.Max(max,e);d|=e!=0;}if(d)different++;}}
        static float Delta(Color[] a,Color[] b){Compare(a,b,out float max,out _);return max;}
    }
}
