using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// V0 candidate only: ordinary Mesh Houdini SoftBody Forward geometry.
    /// Other Houdini/Tyflow modes, particle streams, CustomLocal, VFX, Player,
    /// Depth/Shadow and full normal-lighting matrix are NOT proved here;
    /// one SixWay SH/animated-normal contrast is included as a candidate.
    /// A=Frozen ShaderLab, B=current ShaderLab, C=package Graph.
    /// </summary>
    public sealed class G4GraphVATTests
    {
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size = 128, Layer = 4;

        [Serializable]
        sealed class Record
        {
            public string caseId, api, unityVersion, scope;
            public bool orthographic, finite;
            public float abOn, bcOn, abOff, bcOff, aRepeat, bRepeat, cRepeat;
            public float aResponse, bResponse, cResponse, bFrameResponse, cFrameResponse;
            public float abFrame, bcFrame, aFrameRepeat, bFrameRepeat, cFrameRepeat;
            public float abDualOff, bcDualOff, aDualResponse, bDualResponse, cDualResponse;
            public float aDualRepeat, bDualRepeat, cDualRepeat;
            public float abNormalControl, bcNormalControl;
            public float aNormalResponse, bNormalResponse, cNormalResponse;
            public int aVisible, bVisible, cVisible, abOnPixels, bcOnPixels;
        }

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (bool ortho in new[] { true, false })
                foreach (string variant in new[] { "manual1", "manual2", "dualpos", "rawbounds", "normal-sixway" })
                    yield return new TestCaseData(variant, ortho).SetName(
                        "G4VATSoftBodyABC_" + variant + (ortho ? "_ortho" : "_perspective"));
        }

        [OneTimeSetUp]
        public void ImportGraph() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        [TestCaseSource(nameof(Cases))]
        public void HoudiniSoftBodyForwardABC(string variant, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader frozen = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            Shader current = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath);
            Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(frozen && current && graphShader && frozen.isSupported && current.isSupported, Is.True);
            Assert.That(frozen.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            string folder = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(folder))
                folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4VAT");
            folder = Path.Combine(folder, variant + (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(folder);
            Scene scene = SceneManager.GetActiveScene();
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject cameraGO = new GameObject("VAT ordinary Mesh camera");
            GameObject lightGO = new GameObject("VAT normal control light",typeof(Light));
            var oldAmbientMode = RenderSettings.ambientMode;
            var oldAmbientProbe = RenderSettings.ambientProbe;
            Mesh mesh = UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
            Material a = new Material(frozen), b = new Material(current), c = new Material(graphShader);
            Texture2D position = variant == "normal-sixway" ?
                Constant(new Color(.5f,.5f,.5f,.5f)) : PositionMap();
            Texture2D position2 = Constant(new Color(1, .25f, .75f, 1));
            Texture2D rotation = Constant(new Color(.5f, .5f, .5f, 1));
            Texture2D baseMap = Checker();
            Texture2D rigP = Constant(new Color(.85f,.32f,.16f,.8f));
            Texture2D rigN = Constant(new Color(.13f,.74f,.36f,.8f));
            Texture2D ramp = Constant(new Color(.45f,.53f,.62f,1));
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            RenderTexture oldRT = RenderTexture.active;
            try
            {
                SceneManager.MoveGameObjectToScene(go, scene);
                SceneManager.MoveGameObjectToScene(cameraGO, scene);
                SceneManager.MoveGameObjectToScene(lightGO, scene);
                var light = lightGO.GetComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(42,31,0);
                light.color = Color.white; light.intensity = 1.35f;
                light.shadows = LightShadows.None;
                var probe = new SphericalHarmonicsL2();
                probe.AddDirectionalLight(new Vector3(.4f,.5f,1).normalized,
                    new Color(.35f,.24f,.14f),1);
                RenderSettings.ambientMode = AmbientMode.Custom;
                RenderSettings.ambientProbe = probe;
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                mesh.uv2 = new[] {
                    new Vector2(.20f,.66f), new Vector2(.80f,.66f),
                    new Vector2(.20f,.86f), new Vector2(.80f,.86f) };
                go.layer = Layer;
                go.transform.rotation = Quaternion.Euler(4, 17, 3);
                go.transform.localScale = new Vector3(1.65f, 1.45f, 1.2f);
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var camera = cameraGO.AddComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = ortho;
                camera.orthographicSize = 1.4f;
                camera.fieldOfView = 42;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20;
                camera.transform.position = new Vector3(.08f, .04f, 4);
                camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                camera.cullingMask = 1 << Layer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.0625f, .125f, .1875f, 1);
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.targetTexture = rt;
                cameraGO.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                rt.Create();
                Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                foreach (var mat in new[] { a, b, c })
                    Configure(mat, mat == c, variant, baseMap, position, position2,
                        rotation, rigP, rigN, ramp);
                renderer.enabled = false;
                Color[] clear = Draw(renderer, c, camera, rt, readback, folder, "clear");
                renderer.enabled = true;
                renderer.sharedMaterial = c;
                for (int i = 0; i < 3; i++) camera.Render();
                Assert.That(graphShader.isSupported && c.HasProperty("_VAT_Toggle") &&
                    c.HasProperty("_posTexture") && c.HasProperty("_HoudiniVATSubMode"), Is.True,
                    "Query Graph material only after real GPU warm draw, not pre-import placeholder.");
                // A material configured against a pre-warm placeholder may not
                // retain new Graph properties. Reapply after actual import.
                foreach (var mat in new[] { a, b, c })
                    Configure(mat, mat == c, variant, baseMap, position, position2,
                        rotation, rigP, rigN, ramp);

                SetVAT(a, false); SetVAT(b, false); SetVAT(c, false);
                Color[] aOff = Draw(renderer, a, camera, rt, readback, folder, "A-off");
                Color[] bOff = Draw(renderer, b, camera, rt, readback, folder, "B-off");
                Color[] cOff = Draw(renderer, c, camera, rt, readback, folder, "C-off");
                SetVAT(a, true); SetVAT(b, true); SetVAT(c, true);
                Color[] aOn = Draw(renderer, a, camera, rt, readback, folder, "A-on");
                Color[] bOn = Draw(renderer, b, camera, rt, readback, folder, "B-on");
                Color[] cOn = Draw(renderer, c, camera, rt, readback, folder, "C-on");
                Color[] aRepeat = Draw(renderer, a, camera, rt, readback, folder, "A-repeat");
                Color[] bRepeat = Draw(renderer, b, camera, rt, readback, folder, "B-repeat");
                Color[] cRepeat = Draw(renderer, c, camera, rt, readback, folder, "C-repeat");
                Color[] aDualOff = null, bDualOff = null, cDualOff = null;
                Color[] aDualRepeat = null, bDualRepeat = null, cDualRepeat = null;
                if (variant == "dualpos")
                {
                    foreach (Material mat in new[] { a, b, c }) mat.SetFloat("_B_LOAD_POS_TWO_TEX",0);
                    aDualOff = Draw(renderer,a,camera,rt,readback,folder,"A-dual-off");
                    bDualOff = Draw(renderer,b,camera,rt,readback,folder,"B-dual-off");
                    cDualOff = Draw(renderer,c,camera,rt,readback,folder,"C-dual-off");
                    aDualRepeat = Draw(renderer,a,camera,rt,readback,folder,"A-dual-repeat");
                    bDualRepeat = Draw(renderer,b,camera,rt,readback,folder,"B-dual-repeat");
                    cDualRepeat = Draw(renderer,c,camera,rt,readback,folder,"C-dual-repeat");
                    foreach (Material mat in new[] { a, b, c }) mat.SetFloat("_B_LOAD_POS_TWO_TEX",1);
                }
                Color[] aNormal = null,bNormal = null,cNormal = null;
                if (variant == "normal-sixway")
                {
                    foreach (Material mat in new[] { a, b, c }) mat.SetFloat("_B_UNLOAD_ROT_TEX",1);
                    aNormal = Draw(renderer,a,camera,rt,readback,folder,"A-compressed-normal");
                    bNormal = Draw(renderer,b,camera,rt,readback,folder,"B-compressed-normal");
                    cNormal = Draw(renderer,c,camera,rt,readback,folder,"C-compressed-normal");
                    foreach (Material mat in new[] { a, b, c }) mat.SetFloat("_B_UNLOAD_ROT_TEX",0);
                }
                // Independent actual frame control, manual case 1 <-> 2.
                foreach (Material mat in new[] { a, b, c })
                {
                    mat.SetFloat("_B_autoPlayback", 0);
                    mat.SetFloat("_displayFrame", variant == "manual1" ? 2 : 1);
                }
                Color[] aFrame = Draw(renderer, a, camera, rt, readback, folder, "A-frame-control");
                Color[] bFrame = Draw(renderer, b, camera, rt, readback, folder, "B-frame-control");
                Color[] cFrame = Draw(renderer, c, camera, rt, readback, folder, "C-frame-control");
                Color[] aFrameRepeat = Draw(renderer, a, camera, rt, readback, folder, "A-frame-repeat");
                Color[] bFrameRepeat = Draw(renderer, b, camera, rt, readback, folder, "B-frame-repeat");
                Color[] cFrameRepeat = Draw(renderer, c, camera, rt, readback, folder, "C-frame-repeat");
                var m = new Record {
                    caseId = variant, orthographic = ortho,
                    api = SystemInfo.graphicsDeviceType.ToString(), unityVersion = Application.unityVersion,
                    scope = "Houdini SoftBody ordinary Mesh Forward position, manual frames, dual-position, raw/decoded bounds, one SixWay animated-normal control; auto time, full normal matrix, depth/shadow, other modes, VFX and Player pending.",
                    finite = Finite(clear,aOff,bOff,cOff,aOn,bOn,cOn,aRepeat,bRepeat,cRepeat,
                        aFrame,bFrame,cFrame,aFrameRepeat,bFrameRepeat,cFrameRepeat) &&
                        (variant != "dualpos" || Finite(aDualOff,bDualOff,cDualOff,aDualRepeat,bDualRepeat,cDualRepeat)) &&
                        (variant != "normal-sixway" || Finite(aNormal,bNormal,cNormal)),
                    abOff = Delta(aOff,bOff), bcOff = Delta(bOff,cOff),
                    abOn = Delta(aOn,bOn), bcOn = Delta(bOn,cOn),
                    aRepeat = Delta(aOn,aRepeat), bRepeat = Delta(bOn,bRepeat), cRepeat = Delta(cOn,cRepeat),
                    aResponse = Delta(aOff,aOn), bResponse = Delta(bOff,bOn), cResponse = Delta(cOff,cOn),
                    bFrameResponse = Delta(bOn,bFrame), cFrameResponse = Delta(cOn,cFrame),
                    abFrame = Delta(aFrame,bFrame), bcFrame = Delta(bFrame,cFrame),
                    aFrameRepeat = Delta(aFrame,aFrameRepeat),
                    bFrameRepeat = Delta(bFrame,bFrameRepeat),
                    cFrameRepeat = Delta(cFrame,cFrameRepeat),
                    aVisible = Visible(aOn,clear), bVisible = Visible(bOn,clear),
                    cVisible = Visible(cOn,clear),
                    abDualOff = variant == "dualpos" ? Delta(aDualOff,bDualOff) : 0,
                    bcDualOff = variant == "dualpos" ? Delta(bDualOff,cDualOff) : 0,
                    aDualResponse = variant == "dualpos" ? Delta(aOn,aDualOff) : 0,
                    bDualResponse = variant == "dualpos" ? Delta(bOn,bDualOff) : 0,
                    cDualResponse = variant == "dualpos" ? Delta(cOn,cDualOff) : 0,
                    aDualRepeat = variant == "dualpos" ? Delta(aDualOff,aDualRepeat) : 0,
                    bDualRepeat = variant == "dualpos" ? Delta(bDualOff,bDualRepeat) : 0,
                    cDualRepeat = variant == "dualpos" ? Delta(cDualOff,cDualRepeat) : 0,
                    abNormalControl = variant == "normal-sixway" ? Delta(aNormal,bNormal) : 0,
                    bcNormalControl = variant == "normal-sixway" ? Delta(bNormal,cNormal) : 0,
                    aNormalResponse = variant == "normal-sixway" ? Delta(aOn,aNormal) : 0,
                    bNormalResponse = variant == "normal-sixway" ? Delta(bOn,bNormal) : 0,
                    cNormalResponse = variant == "normal-sixway" ? Delta(cOn,cNormal) : 0,
                    abOnPixels = Different(aOn,bOn), bcOnPixels = Different(bOn,cOn)
                };
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(m,true));
                Debug.Log("NBFX_G4_VAT " + JsonUtility.ToJson(m));
                Assert.That(m.finite, Is.True);
                Assert.That(m.aVisible, Is.GreaterThan(150));
                Assert.That(m.bVisible, Is.GreaterThan(150));
                Assert.That(m.cVisible, Is.GreaterThan(150));
                Assert.That(m.abOff, Is.Zero); Assert.That(m.bcOff, Is.Zero);
                Assert.That(m.abOn, Is.Zero); Assert.That(m.bcOn, Is.Zero);
                Assert.That(m.abFrame, Is.Zero); Assert.That(m.bcFrame, Is.Zero);
                Assert.That(m.aFrameRepeat, Is.Zero);
                Assert.That(m.bFrameRepeat, Is.Zero);
                Assert.That(m.cFrameRepeat, Is.Zero);
                Assert.That(m.aRepeat, Is.Zero); Assert.That(m.bRepeat, Is.Zero); Assert.That(m.cRepeat, Is.Zero);
                Assert.That(m.aResponse, Is.GreaterThan(.01f));
                Assert.That(m.bResponse, Is.GreaterThan(.01f));
                Assert.That(m.cResponse, Is.GreaterThan(.01f));
                if (variant != "normal-sixway")
                {
                    Assert.That(m.bFrameResponse, Is.GreaterThan(.005f));
                    Assert.That(m.cFrameResponse, Is.GreaterThan(.005f));
                }
                else
                {
                    Assert.That(m.abNormalControl, Is.Zero);
                    Assert.That(m.bcNormalControl, Is.Zero);
                    Assert.That(m.aNormalResponse, Is.GreaterThan(.01f));
                    Assert.That(m.bNormalResponse, Is.GreaterThan(.01f));
                    Assert.That(m.cNormalResponse, Is.GreaterThan(.01f));
                }
                if (variant == "dualpos")
                {
                    Assert.That(m.abDualOff, Is.Zero); Assert.That(m.bcDualOff, Is.Zero);
                    Assert.That(m.aDualRepeat, Is.Zero); Assert.That(m.bDualRepeat, Is.Zero);
                    Assert.That(m.cDualRepeat, Is.Zero);
                    Assert.That(m.aDualResponse, Is.GreaterThan(.005f));
                    Assert.That(m.bDualResponse, Is.GreaterThan(.005f));
                    Assert.That(m.cDualResponse, Is.GreaterThan(.005f));
                }
            }
            finally
            {
                RenderSettings.ambientMode = oldAmbientMode;
                RenderSettings.ambientProbe = oldAmbientProbe;
                cameraGO.GetComponent<Camera>().targetTexture = null;
                RenderTexture.active = oldRT;
                rt.Release();
                foreach (UnityEngine.Object obj in new UnityEngine.Object[] {
                    go,cameraGO,lightGO,mesh,a,b,c,position,position2,rotation,
                    baseMap,rigP,rigN,ramp,rt,readback })
                    UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        static void Configure(Material mat, bool graph, string variant, Texture2D baseMap,
            Texture2D position, Texture2D position2, Texture2D rotation,
            Texture2D rigP, Texture2D rigN, Texture2D ramp)
        {
            mat.SetTexture("_BaseMap",baseMap);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color",Color.white);
            mat.SetColor("_ColorA",Color.white);
            mat.SetFloat("_AlphaAll",1);
            mat.SetFloat("_BaseColorIntensityForTimeline",1);
            mat.SetFloat("_Cull",(float)CullMode.Off);
            mat.SetFloat("_ZTest",(float)CompareFunction.LessEqual);
            mat.SetFloat("_ZWrite",0);
            mat.SetFloat("_SrcBlend",(float)BlendMode.One);
            mat.SetFloat("_DstBlend",(float)BlendMode.Zero);
            mat.SetFloat("_fogintensity",0);
            mat.SetTexture("_posTexture",position);
            mat.SetTexture("_posTexture2",position2);
            mat.SetTexture("_rotTexture",rotation);
            if (variant == "normal-sixway")
            {
                mat.SetTexture("_RigRTBk",rigP);
                mat.SetTexture("_RigLBtF",rigN);
                mat.SetTexture("_SixWayEmissionRamp",ramp);
                mat.SetColor("_SixWayEmissionColor",new Color(.8f,.42f,.17f,.75f));
                mat.SetVector("_SixWayInfo",new Vector4(.55f,1.4f,0,0));
                mat.SetFloat("_SixWayColorAbsorptionToggle",0);
                mat.SetFloat("_FxLightMode",4);
                mat.EnableKeyword("EVALUATE_SH_VERTEX");
            }
            mat.SetFloat("_VATMode",0);
            mat.SetFloat("_HoudiniVATSubMode",0);
            mat.SetFloat("_frameCount",2);
            mat.SetFloat("_B_autoPlayback",0);
            mat.SetFloat("_gameTimeAtFirstFrame",0);
            mat.SetFloat("_houdiniFPS",24);
            mat.SetFloat("_playbackSpeed",1);
            mat.SetFloat("_displayFrame",variant == "manual1" ? 1 : 2);
            mat.SetFloat("_B_LOAD_POS_TWO_TEX",variant == "dualpos" ? 1 : 0);
            mat.SetFloat("_B_UNLOAD_ROT_TEX",0);
            mat.SetFloat("_boundMinX",-1); mat.SetFloat("_boundMinY",-1); mat.SetFloat("_boundMinZ",-1);
            mat.SetFloat("_boundMaxX",1); mat.SetFloat("_boundMaxY",1);
            mat.SetFloat("_boundMaxZ",variant == "rawbounds" ? 1.05f : 1);
            mat.renderQueue = 3000;
            foreach (string pass in new[] { "SRPDefaultUnlit","SRPDEFAULTUNLIT","UniversalForward",
                "DepthOnly","ShadowCaster","Universal2D","NBCameraOpaqueDistortPass","NBDeferredDistortPass" })
                mat.SetShaderPassEnabled(pass,false);
            mat.SetShaderPassEnabled(graph ? "SRPDefaultUnlit" : "UniversalForward",true);
            if (graph)
            {
                mat.SetFloat("_Surface",1);
                mat.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha",(float)BlendMode.Zero);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetFloat("_NB_Flags1Lo16",0);
                mat.SetFloat("_NB_Flags1Hi16",0);
            }
            else
            {
                mat.EnableKeyword(variant == "normal-sixway" ?
                    "_FX_LIGHT_MODE_SIX_WAY" : "_FX_LIGHT_MODE_UNLIT");
                mat.SetInteger("_W9ParticleShaderFlags",0);
                mat.SetInteger("_W9ParticleShaderFlags1",0);
                mat.SetFloat("_ColorMask",15);
            }
        }
        static void SetVAT(Material mat, bool on)
        {
            mat.SetFloat("_VAT_Toggle",on ? 1 : 0);
            if (mat.shader.name != "Effects/NBShader_T00_Frozen" &&
                mat.shader.name != "Effects/NBShader") return;
            if (on)
            {
                mat.EnableKeyword("_VAT"); mat.EnableKeyword("_VAT_HOUDINI");
                mat.EnableKeyword("_HOUDINI_VAT_SOFTBODY");
            }
            else
            {
                mat.DisableKeyword("_VAT"); mat.DisableKeyword("_VAT_HOUDINI");
                mat.DisableKeyword("_HOUDINI_VAT_SOFTBODY");
            }
        }
        static Texture2D Constant(Color value)
        {
            var tex = new Texture2D(2,2,TextureFormat.RGBAHalf,false,true);
            for(int y=0;y<2;y++)for(int x=0;x<2;x++)tex.SetPixel(x,y,value);
            tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Point;tex.Apply(false);
            return tex;
        }
        static Texture2D PositionMap()
        {
            var tex = new Texture2D(8,8,TextureFormat.RGBAHalf,false,true);
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)
                tex.SetPixel(x,y,new Color(.50f+.045f*x-.015f*y,
                    .50f+.018f*y-.012f*x,.50f+.025f*((x+2*y)%5),.5f));
            tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Point;tex.Apply(false);
            return tex;
        }
        static Texture2D Checker()
        {
            var tex = new Texture2D(8,8,TextureFormat.RGBAHalf,false,true);
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)
                tex.SetPixel(x,y,((x/2+y/2)&1)==0 ? new Color(.85f,.32f,.12f,1) :
                    new Color(.18f,.72f,.83f,1));
            tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Point;tex.Apply(false);
            return tex;
        }
        static Color[] Draw(MeshRenderer renderer, Material material, Camera camera,
            RenderTexture rt, Texture2D readback, string folder, string name)
        {
            renderer.sharedMaterial=material;
            camera.Render();
            RenderTexture.active=rt;
            readback.ReadPixels(new Rect(0,0,Size,Size),0,0,false);
            readback.Apply(false,false);
            Color[] frame=readback.GetPixels();
            using(var stream=File.Create(Path.Combine(folder,name+".rgba32f")))
            using(var writer=new BinaryWriter(stream))
                foreach(Color px in frame) { writer.Write(px.r);writer.Write(px.g);writer.Write(px.b);writer.Write(px.a); }
            return frame;
        }
        static bool Finite(params Color[][] frames)
        {
            foreach(Color[] frame in frames)foreach(Color p in frame)
                if(float.IsNaN(p.r)||float.IsNaN(p.g)||float.IsNaN(p.b)||float.IsNaN(p.a)||
                    float.IsInfinity(p.r)||float.IsInfinity(p.g)||float.IsInfinity(p.b)||float.IsInfinity(p.a)) return false;
            return true;
        }
        static float Delta(Color[] a,Color[] b)
        {
            float max=0;
            for(int i=0;i<a.Length;i++)
            {
                max=Mathf.Max(max,Mathf.Abs(a[i].r-b[i].r));
                max=Mathf.Max(max,Mathf.Abs(a[i].g-b[i].g));
                max=Mathf.Max(max,Mathf.Abs(a[i].b-b[i].b));
                max=Mathf.Max(max,Mathf.Abs(a[i].a-b[i].a));
            }
            return max;
        }
        static int Different(Color[] a,Color[] b)
        {
            int count=0;
            for(int i=0;i<a.Length;i++)
                if(a[i].r!=b[i].r||a[i].g!=b[i].g||a[i].b!=b[i].b||a[i].a!=b[i].a)count++;
            return count;
        }
        static int Visible(Color[] frame,Color[] clear)
        {
            int count=0;
            for(int i=0;i<frame.Length;i++)
                if(Mathf.Abs(frame[i].r-clear[i].r)+Mathf.Abs(frame[i].g-clear[i].g)+
                    Mathf.Abs(frame[i].b-clear[i].b)>.07f)count++;
            return count;
        }
    }
}
