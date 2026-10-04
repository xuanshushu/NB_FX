using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        bool _disableNormalBiasForDiagnostic;
        Shader _graphOverrideShader;
        // Shared directed geometry fixture; optional setup preserves old V0 cases.
        public Action<Material,bool> GeometrySetup;
        public Action<Mesh> GeometryMeshSetup;
        public Action<Material,bool,float> GeometryFrameState;
        public string GeometryCaseId;
        // Optional DN0 default-pipeline coverage hooks. Old three-argument
        // reflection entry and limited tests retain their original behavior.
        public Action<Material, bool> DefaultFullChainMaterialSetup;
        public Action<Light> DefaultFullChainLightSetup;
        public Action<Camera, GameObject, GameObject, RenderTexture> DefaultFullChainSceneSetup;
        public Action<Material[], MeshRenderer, MeshRenderer, Camera, RenderTexture, Texture2D, string> DefaultFullChainVerify;
        public Action<Material[], MeshRenderer, MeshRenderer, Camera, RenderTexture, Texture2D, string> DefaultFullChainPreAssertionsObserve;
        public void CaptureDefaultForwardDepthShadow(bool ortho)
            => CaptureVATDepthAndShadowGeometryCore(true, ortho, false, true);


        [Serializable]
        sealed class Record
        {
            public string caseId, api, unityVersion, scope;
            public bool orthographic, finite;
            public bool graphExtraDepthNormalsExcluded, ssaoActive;
            public bool defaultFullForwardDepthShadow, graphDepthNormalsRawEnabled;
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
        public void ImportGraph()
        {
            AssetDatabase.ImportAsset(GraphPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            string folder = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
                var type = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.ShaderGraph.Editor")
                    .GetType("UnityEditor.ShaderGraph.ShaderGraphImporter", true);
                var method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Single(m => m.Name == "GetShaderText" && m.GetParameters().Length == 4 && m.GetParameters()[3].IsOut);
                object[] args = { GraphPath, null, null, null };
                File.WriteAllText(Path.Combine(folder,"generated-vat-candidate.shader"), (string)method.Invoke(null,args));
            }
        }

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

        [TestCase(false, true)] [TestCase(false, false)]
        [TestCase(true, true)] [TestCase(true, false)]
        public void VATSoftBody_ActualDepthAndShadowGeometry(bool shadow, bool ortho)
            => CaptureVATDepthAndShadowGeometry(shadow,ortho,true);

        [TestCase(true, TestName = "DN0SoftBodyDefaultSSAOABC_ortho")]
        [TestCase(false, TestName = "DN0SoftBodyDefaultSSAOABC_perspective")]
        public void VATSoftBody_DefaultForwardDepthShadowWithSSAO(bool ortho)
            => CaptureVATDepthAndShadowGeometryCore(true, ortho, false, true);

        // Exact original private ABI: consumers use GetMethod with NonPublic
        // and Invoke three arguments. There is one method of this name.
        void CaptureVATDepthAndShadowGeometry(bool shadow, bool ortho, bool isolateExtraNormals)
            => CaptureVATDepthAndShadowGeometryCore(shadow, ortho, isolateExtraNormals, false);

        void CaptureVATDepthAndShadowGeometryCore(bool shadow, bool ortho, bool isolateExtraNormals,
            bool defaultFullChain)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            var data = pipeline.rendererDataList[0];
            string project = Path.GetDirectoryName(Application.dataPath);
            string rendererFile = Path.Combine(project, AssetDatabase.GetAssetPath(data));
            byte[] rendererBytes = File.ReadAllBytes(rendererFile);
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ??
                Path.Combine(project, "Temp/NBFXVATGeometry"), (defaultFullChain ? "dn0-default-fullchain-" : "") + (string.IsNullOrEmpty(GeometryCaseId)?"":GeometryCaseId+"-") + (shadow ? "shadow" : "depth") + (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(folder);
            Material a = new Material(AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath));
            Material b = new Material(AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath));
            Material c = new Material(_graphOverrideShader ? _graphOverrideShader : AssetDatabase.LoadAssetAtPath<Shader>(GraphPath));
            var map = Checker(); var position = PositionMap(); var second = Constant(Color.clear);
            var rotation = Constant(new Color(.5f,.5f,.5f,1));
            var actor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var receiver = GameObject.CreatePrimitive(shadow ? PrimitiveType.Plane : PrimitiveType.Quad);
            var cameraObject = new GameObject("VAT geometry followup camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderShadows = true;
            var lightObject = new GameObject("VAT geometry directional light", typeof(Light));
            var light = lightObject.GetComponent<Light>();
            var receiverMaterial = new Material(Shader.Find(shadow ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit"));
            var mesh = UnityEngine.Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh);
            mesh.uv2 = new[] { new Vector2(.20f,.66f),new Vector2(.80f,.66f),new Vector2(.20f,.86f),new Vector2(.80f,.86f) };
            GeometryMeshSetup?.Invoke(mesh);
            actor.GetComponent<MeshFilter>().sharedMesh = mesh;
            var writer = actor.GetComponent<MeshRenderer>();
            var rt = new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size,Size,TextureFormat.RGBAHalf,false,true);
            var oldRT = RenderTexture.active; var oldSun = RenderSettings.sun;
            var oldAmbientMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight; bool oldFog = RenderSettings.fog;
            G2DirectedDepthOnlyFeature directed = null;
            var nb = data.rendererFeatures.Find(f => f && f.GetType().FullName == "NBShader.NBPostProcess");
            bool oldNB = nb && nb.isActive;
            try
            {
                var scene = SceneManager.GetActiveScene();
                Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
                foreach (var go in new[] {actor,receiver,cameraObject,lightObject}) SceneManager.MoveGameObjectToScene(go,scene);
                camera.scene = scene;
                foreach (var m in new[] {a,b,c})
                {
                    Configure(m,m==c,"manual1",map,position,second,rotation,map,map,map);
                    GeometrySetup?.Invoke(m,m==c);
                    m.SetFloat("_Surface",0); m.SetFloat("_AlphaClip",0); m.SetFloat("_Cutoff",.5f);
                    m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.renderQueue = shadow ? 2100 : 3000;
                    m.SetFloat("_CastShadows",1); m.SetFloat("_AffectsShadows",1);
                    if (defaultFullChain)
                    {
                        // Matching opaque material state with all corresponding
                        // legacy passes available to the actual URP renderer.
                        // The Graph main has no tag and is selected as SRPDefaultUnlit.
                        if (m.HasProperty("_TransparentMode")) m.SetFloat("_TransparentMode", 0);
                        if (m.HasProperty("_ForceZWriteToggle")) m.SetFloat("_ForceZWriteToggle", 0);
                        m.SetFloat("_ZWrite", 1);
                        if (m.HasProperty("_ZWriteControl")) m.SetFloat("_ZWriteControl", 0);
                        if (m.HasProperty("_BackFirstPassToggle")) m.SetFloat("_BackFirstPassToggle", 0);
                        m.SetShaderPassEnabled("SRPDefaultUnlit", m == c);
                        m.SetShaderPassEnabled("SRPDEFAULTUNLIT", m == c);
                        m.SetShaderPassEnabled("UniversalForward", true);
                        m.SetShaderPassEnabled("DepthOnly", true);
                        m.SetShaderPassEnabled("ShadowCaster", true);
                        // Explicit True proves the shader-level default. No GUI
                        // pass hiding and no SSAO or extra normals pass suppression.
                        if (m == c) m.SetShaderPassEnabled("DepthNormalsOnly", true);
                        DefaultFullChainMaterialSetup?.Invoke(m, m == c);
                    }
                    else
                    {
                        foreach (string pass in new[] {"SRPDefaultUnlit","SRPDEFAULTUNLIT","UniversalForward","DepthOnly","ShadowCaster"}) m.SetShaderPassEnabled(pass,false);
                        m.SetShaderPassEnabled(shadow ? "ShadowCaster" : "DepthOnly",true);
                        // This fixture compares only the selected legacy pass.
                        // SSAO remains active on the receiver; Graph's additional
                        // depth-normal writer is covered separately, not folded
                        // into this ShadowCaster geometry result.
                        if(m==c && isolateExtraNormals)m.SetShaderPassEnabled("DepthNormalsOnly",false);
                    }
                }
                receiver.GetComponent<MeshRenderer>().sharedMaterial = receiverMaterial;
                receiver.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                receiver.layer = 2;
                if (shadow)
                {
                    Assert.That(pipeline.supportsMainLightShadows && pipeline.shadowDistance > 10,Is.True);
                    actor.layer=2; actor.transform.position=new Vector3(0,1,0);
                    actor.transform.rotation=Quaternion.Euler(-90,0,0); actor.transform.localScale=new Vector3(1.5f,1.5f,1);
                    receiver.transform.localScale=new Vector3(.55f,1,.55f);
                    receiverMaterial.SetColor("_BaseColor",Color.white); receiverMaterial.DisableKeyword("_RECEIVE_SHADOWS_OFF");
                    writer.shadowCastingMode=ShadowCastingMode.On;
                    light.type=LightType.Directional;light.shadows=LightShadows.Hard;light.shadowStrength=1;light.intensity=2;light.cullingMask=1<<2;
                    if (_disableNormalBiasForDiagnostic) light.shadowNormalBias = 0;
                    if (defaultFullChain) DefaultFullChainLightSetup?.Invoke(light);
                    light.transform.rotation=Quaternion.Euler(50,-30,0);
                    RenderSettings.sun=light;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.fog=false;
                    camera.transform.position=new Vector3(0,4.5f,-5.5f);camera.transform.LookAt(Vector3.zero);
                    camera.orthographicSize=3.5f;camera.cullingMask=1<<2;
                }
                else
                {
                    actor.layer=G2DepthOnlyABTests.ForegroundLayer;actor.transform.position=new Vector3(0,0,2);actor.transform.localScale=new Vector3(1.2f,1.2f,1);
                    writer.shadowCastingMode=ShadowCastingMode.Off;
                    receiver.transform.position=new Vector3(0,0,1);receiver.transform.localScale=new Vector3(3,3,1);
                    receiverMaterial.SetColor("_BaseColor",Color.green);receiverMaterial.SetFloat("_Surface",1);receiverMaterial.SetFloat("_SrcBlend",1);receiverMaterial.SetFloat("_DstBlend",0);receiverMaterial.SetFloat("_ZWrite",0);receiverMaterial.SetFloat("_Cull",0);
                    receiverMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");receiverMaterial.renderQueue=3000;
                    camera.transform.position=new Vector3(0,0,3);camera.transform.rotation=Quaternion.Euler(0,180,0);camera.orthographicSize=1;
                    camera.cullingMask=(1<<2)|(1<<G2DepthOnlyABTests.ForegroundLayer);
                    directed=ScriptableObject.CreateInstance<G2DirectedDepthOnlyFeature>();directed.hideFlags=HideFlags.HideAndDontSave;directed.targetCamera=camera;directed.Create();directed.SetActive(true);data.rendererFeatures.Add(directed);data.SetDirty();
                }
                if(nb)nb.SetActive(false);
                camera.orthographic=ortho;camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=25;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.125f,.25f,.375f,1);camera.allowHDR=false;camera.allowMSAA=false;camera.targetTexture=rt;rt.Create();Assert.That(rt.IsCreated() && !rt.sRGB,Is.True);
                if (defaultFullChain) DefaultFullChainSceneSetup?.Invoke(camera, actor, receiver, rt);
                Color[] empty = null, emptyRepeat = null;
                if (defaultFullChain)
                {
                    Assert.That(shadow && !isolateExtraNormals, Is.True);
                    Assert.That(directed, Is.Null, "Default pipeline must not use a directed test RendererFeature.");
                    Assert.That(data.rendererFeatures.Any(f => f && f.isActive && f.GetType().Name.Contains("AmbientOcclusion")), Is.True, "SSAO must stay active in the existing Renderer.");
                    Assert.That(c.GetShaderPassEnabled("DepthNormalsOnly"), Is.True);
                    writer.enabled = false; receiver.GetComponent<MeshRenderer>().enabled = false;
                    for (int i = 0; i < 4; ++i) camera.Render();
                    empty = Draw(writer, b, camera, rt, readback, folder, "empty-background");
                    emptyRepeat = Draw(writer, b, camera, rt, readback, folder, "empty-background-repeat");
                    Assert.That(Finite(empty, emptyRepeat), Is.True);
                    Assert.That(Delta(empty, emptyRepeat), Is.Zero);
                    Assert.That(empty[0].r, Is.LessThan(empty[0].g));
                    Assert.That(empty[0].g, Is.LessThan(empty[0].b));
                    Assert.That(empty[0].r, Is.GreaterThan(0f));
                    Assert.That(empty[0].a, Is.GreaterThan(0f));
                    writer.enabled = true; receiver.GetComponent<MeshRenderer>().enabled = true;
                }
                foreach (var m in new[] {a,b,c})
                { if(GeometryFrameState!=null)GeometryFrameState(m,m==c,0);else SetVAT(m,false);writer.sharedMaterial=m;for(int i=0;i<4;i++)camera.Render(); }
                Color[] Snap(Material m,string label,bool enabled,float frame)
                { if(GeometryFrameState!=null)GeometryFrameState(m,m==c,enabled?frame:0);else {SetVAT(m,enabled);m.SetFloat("_displayFrame",frame);}writer.sharedMaterial=m;for(int i=0;i<3;i++)camera.Render();return Draw(writer,m,camera,rt,readback,folder,label); }
                Color[] actorA = null, actorB = null, actorC = null;
                Color[] actorAR = null, actorBR = null, actorCR = null;
                if (defaultFullChain)
                {
                    Assert.That(c.FindPass("Universal Forward"), Is.GreaterThanOrEqualTo(0));
                    Assert.That(c.FindPass("DepthNormalsOnly"), Is.GreaterThanOrEqualTo(0));
                    foreach (var m in new[] {a,b,c})
                    {
                        Assert.That(m.GetShaderPassEnabled(m == c ? "SRPDefaultUnlit" : "UniversalForward"), Is.True);
                        Assert.That(m.GetShaderPassEnabled("DepthOnly"), Is.True);
                        Assert.That(m.GetShaderPassEnabled("ShadowCaster"), Is.True);
                    }
                    File.WriteAllText(Path.Combine(folder, "fullchain-inputs-and-state.json"),
                        JsonUtility.ToJson(DN0InputsAndState(project, data, pipeline, new[] {a,b,c}), true));
                    // Independent main-color visibility: the floor cannot hide
                    // a missing Forward behind a shadow-only response.
                    receiver.GetComponent<MeshRenderer>().enabled = false;
                    actorA = Snap(a, "A-forward-visible", true, 1);
                    actorB = Snap(b, "B-forward-visible", true, 1);
                    actorC = Snap(c, "C-forward-visible", true, 1);
                    actorAR = Snap(a, "A-forward-visible-repeat", true, 1);
                    actorBR = Snap(b, "B-forward-visible-repeat", true, 1);
                    actorCR = Snap(c, "C-forward-visible-repeat", true, 1);
                    receiver.GetComponent<MeshRenderer>().enabled = true;
                    DefaultFullChainPreAssertionsObserve?.Invoke(new[] {a,b,c}, writer,
                        receiver.GetComponent<MeshRenderer>(), camera, rt, readback, folder);
                    Assert.That(Finite(actorA, actorB, actorC, actorAR, actorBR, actorCR), Is.True);
                    Assert.That(Visible(actorA, empty), Is.GreaterThan(150));
                    Assert.That(Visible(actorB, empty), Is.GreaterThan(150));
                    Assert.That(Visible(actorC, empty), Is.GreaterThan(150));
                    Assert.That(Delta(actorA, actorB) + Delta(actorB, actorC), Is.Zero);
                    Assert.That(Delta(actorA, actorAR) + Delta(actorB, actorBR) + Delta(actorC, actorCR), Is.Zero);
                }
                var ao=Snap(a,"A-off",false,1);var bo=Snap(b,"B-off",false,1);var co=Snap(c,"C-off",false,1);
                var aa=Snap(a,"A-on",true,1);var ba=Snap(b,"B-on",true,1);var ca=Snap(c,"C-on",true,1);
                var ar=Snap(a,"A-repeat",true,1);var br=Snap(b,"B-repeat",true,1);var cr=Snap(c,"C-repeat",true,1);
                var af=Snap(a,"A-frame2",true,2);var bf=Snap(b,"B-frame2",true,2);var cf=Snap(c,"C-frame2",true,2);
                var record=new Record{caseId=(string.IsNullOrEmpty(GeometryCaseId)?"":GeometryCaseId+"-")+(shadow?"actual-shadow-geometry":"actual-depth-geometry"),orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,
                    scope=defaultFullChain ? "DN0 raw Graph DepthNormalsOnly enabled True; actual URP Forward+DepthOnly+ShadowCaster opaque material chain, existing SSAO active; no directed renderer or GUI suppression; SoftBody two cameras only, no all-state/Player/VFX/perf claim" : "Selected VAT DepthOnly/ShadowCaster only; extra Graph DepthNormalsOnly excluded where recorded; no default SSAO pipeline/allVAT/VFX/Player claim",
                    defaultFullForwardDepthShadow=defaultFullChain,
                    graphDepthNormalsRawEnabled=defaultFullChain && c.GetShaderPassEnabled("DepthNormalsOnly"),
                    aVisible=defaultFullChain ? Visible(actorA,empty) : 0,
                    bVisible=defaultFullChain ? Visible(actorB,empty) : 0,
                    cVisible=defaultFullChain ? Visible(actorC,empty) : 0,
                    graphExtraDepthNormalsExcluded=isolateExtraNormals,
                    ssaoActive=data.rendererFeatures.Any(f=>f&&f.isActive&&f.GetType().Name.Contains("AmbientOcclusion")),
                    finite=Finite(ao,bo,co,aa,ba,ca,ar,br,cr,af,bf,cf),abOff=Delta(ao,bo),bcOff=Delta(bo,co),abOn=Delta(aa,ba),bcOn=Delta(ba,ca),abFrame=Delta(af,bf),bcFrame=Delta(bf,cf),aRepeat=Delta(aa,ar),bRepeat=Delta(ba,br),cRepeat=Delta(ca,cr),aResponse=Delta(ao,aa),bResponse=Delta(bo,ba),cResponse=Delta(co,ca),bFrameResponse=Delta(ba,bf),cFrameResponse=Delta(ca,cf)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(record,true));Debug.Log("NBFX_G4_VAT_GEOMETRY "+JsonUtility.ToJson(record));
                if (defaultFullChain) DefaultFullChainVerify?.Invoke(new[] {a,b,c}, writer,
                    receiver.GetComponent<MeshRenderer>(), camera, rt, readback, folder);
                Assert.That(record.finite,Is.True);Assert.That(record.abOff+record.bcOff+record.abOn+record.bcOn+record.abFrame+record.bcFrame,Is.Zero);
                Assert.That(record.aRepeat+record.bRepeat+record.cRepeat,Is.Zero);Assert.That(record.aResponse,Is.GreaterThan(.01f));Assert.That(record.bResponse,Is.GreaterThan(.01f));Assert.That(record.cResponse,Is.GreaterThan(.01f));Assert.That(record.bFrameResponse,Is.GreaterThan(.01f));Assert.That(record.cFrameResponse,Is.GreaterThan(.01f));
                if(directed!=null)Assert.That(directed.validDepthFrames,Is.EqualTo(directed.recordedFrames).And.GreaterThan(0));
            }
            finally
            {
                if(nb)nb.SetActive(oldNB);
                if(directed){data.rendererFeatures.Remove(directed);data.SetDirty();UnityEngine.Object.DestroyImmediate(directed);}
                RenderSettings.sun=oldSun;RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientLight=oldAmbient;RenderSettings.fog=oldFog;
                camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();
                foreach(var o in new UnityEngine.Object[]{actor,receiver,cameraObject,lightObject,a,b,c,map,position,second,rotation,receiverMaterial,mesh,rt,readback})UnityEngine.Object.DestroyImmediate(o);
                Assert.That(File.ReadAllBytes(rendererFile),Is.EqualTo(rendererBytes),"Temporary VAT geometry fixture wrote renderer asset");
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void VATSoftBody_ShadowWithoutNormalBiasDiagnostic(bool ortho)
        {
            _disableNormalBiasForDiagnostic = true;
            try { CaptureVATDepthAndShadowGeometry(true,ortho,false); }
            finally { _disableNormalBiasForDiagnostic = false; }
        }

        [TestCase(true)] [TestCase(false)]
        public void VATSoftBody_ShadowWithoutGraphDepthNormalsDiagnostic(bool ortho)
        {
            CaptureVATDepthAndShadowGeometry(true,ortho,true);
        }

        [TestCase(true)] [TestCase(false)]
        public void VATSoftBody_ShadowWithoutSRPBatcherDiagnostic(bool ortho)
        {
            var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            bool original=pipeline.useSRPBatcher;
            string file=Path.Combine(Path.GetDirectoryName(Application.dataPath),AssetDatabase.GetAssetPath(pipeline));
            byte[] before=File.ReadAllBytes(file);
            try { pipeline.useSRPBatcher=false;CaptureVATDepthAndShadowGeometry(true,ortho,false); }
            finally
            {
                pipeline.useSRPBatcher=original;
                Assert.That(File.ReadAllBytes(file),Is.EqualTo(before),"SRP diagnostic saved the pipeline asset");
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void VATSoftBody_ShadowNeutralFragmentInMemoryDiagnostic(bool ortho)
        {
            string folder = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            string source = File.ReadAllText(Path.Combine(folder,"generated-vat-candidate.shader"));
            const string include = "#include \"Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/Passes/NBGraphShadowCasterPass.hlsl\"";
            Assert.That(source.Split(new[]{include},StringSplitOptions.None).Length,Is.EqualTo(2));
            source = source.Replace(include,
                "#define frag NBFXDiagnosticUnusedFrag\n#include \"Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl\"\n#undef frag\nhalf4 frag(PackedVaryings packedInput) : SV_TARGET { return 0; }\n");
            int firstQuote=source.IndexOf('"'),lastQuote=source.IndexOf('"',firstQuote+1);
            source=source.Remove(firstQuote+1,lastQuote-firstQuote-1).Insert(firstQuote+1,"Hidden/Codex/ShaderDebug/NBFXVATNeutralShadow"+(ortho?"Ortho":"Perspective"));
            Shader diagnostic = null;
            try
            {
                diagnostic=ShaderUtil.CreateShaderAsset(source,false);Assert.That(diagnostic,Is.Not.Null);
                diagnostic.hideFlags=HideFlags.HideAndDontSave;
                Assert.That(ShaderUtil.GetShaderMessages(diagnostic).Any(m=>m.severity.ToString()=="Error"),Is.False);
                Assert.That(diagnostic.isSupported,Is.True);_graphOverrideShader=diagnostic;
                CaptureVATDepthAndShadowGeometry(true,ortho,false);
            }
            finally
            {
                _graphOverrideShader=null;
                if(diagnostic)UnityEngine.Object.DestroyImmediate(diagnostic);
            }
        }

        [TestCase("NBCameraOpaqueDistortPass", true)] [TestCase("NBCameraOpaqueDistortPass", false)]
        [TestCase("NBDeferredDistortPass", true)] [TestCase("NBDeferredDistortPass", false)]
        public void VATSoftBody_ScreenPassesExcludeGeometry(string pass, bool ortho)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            var data = pipeline.rendererDataList[0];
            string project = Path.GetDirectoryName(Application.dataPath);
            string rendererFile = Path.Combine(project,AssetDatabase.GetAssetPath(data));
            byte[] before = File.ReadAllBytes(rendererFile);
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ??
                Path.Combine(project,"Temp/NBFXVATScreen"),pass+(ortho?"-ortho":"-perspective"));
            Directory.CreateDirectory(folder);
            var scene = SceneManager.GetActiveScene();
            var actor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("VAT screen exclusion camera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var a = new Material(AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath));
            var b = new Material(AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath));
            var c = new Material(AssetDatabase.LoadAssetAtPath<Shader>(GraphPath));
            var backdropMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            Texture2D checker = Checker(), noise = Constant(new Color(.75f,.25f,0,.5f)), mask = Constant(Color.white);
            Texture2D position = PositionMap(), second = Constant(Color.clear), rotation = Constant(new Color(.5f,.5f,.5f,1));
            var mesh = UnityEngine.Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh);
            mesh.uv2 = new[] {new Vector2(.20f,.66f),new Vector2(.80f,.66f),new Vector2(.20f,.86f),new Vector2(.80f,.86f)};
            var rt = new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size,Size,TextureFormat.RGBAHalf,false,true);
            var oldRT = RenderTexture.active;
            var nb = data.rendererFeatures.Find(f=>f && f.GetType().FullName=="NBShader.NBPostProcess");
            bool oldNB = nb && nb.isActive;
            G4ScreenNoiseDirectedFeature directed = null;
            try
            {
                Assert.That(scene.IsValid() && scene.isLoaded,Is.True);
                foreach(var go in new[]{actor,backdrop,cameraObject})SceneManager.MoveGameObjectToScene(go,scene);
                actor.GetComponent<MeshFilter>().sharedMesh = mesh;
                actor.layer = G4GraphScreenNoiseTests.ForegroundLayer;
                actor.transform.position=new Vector3(0,0,2);actor.transform.localScale=new Vector3(2,2,1);
                var writer = actor.GetComponent<MeshRenderer>();writer.shadowCastingMode=ShadowCastingMode.Off;
                backdrop.layer=2;backdrop.transform.position=new Vector3(0,0,1);backdrop.transform.localScale=new Vector3(6,6,1);
                backdropMaterial.SetTexture("_BaseMap",checker);backdropMaterial.SetColor("_BaseColor",Color.white);
                backdropMaterial.SetFloat("_Cull",0);backdropMaterial.renderQueue=2000;
                var backgroundRenderer=backdrop.GetComponent<MeshRenderer>();backgroundRenderer.sharedMaterial=backdropMaterial;
                backgroundRenderer.shadowCastingMode=ShadowCastingMode.Off;backgroundRenderer.enabled=pass=="NBCameraOpaqueDistortPass";
                var configureScreen=typeof(G4GraphScreenNoiseTests).GetMethod("Configure",BindingFlags.Static|BindingFlags.NonPublic);
                Assert.That(configureScreen,Is.Not.Null);
                foreach(var m in new[]{a,b,c})
                {
                    Configure(m,m==c,"manual1",checker,position,second,rotation,checker,checker,checker);
                    configureScreen.Invoke(null,new object[]{m,m==c,noise,mask,pass,"noise-a-half"});
                }
                camera.scene=scene;camera.orthographic=ortho;camera.orthographicSize=1.5f;camera.fieldOfView=45;
                camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=new Vector3(0,0,5);
                camera.transform.rotation=Quaternion.Euler(0,180,0);camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=Color.clear;camera.allowHDR=true;camera.allowMSAA=false;
                camera.cullingMask=(1<<2)|(1<<G4GraphScreenNoiseTests.ForegroundLayer);camera.targetTexture=rt;
                cameraData.requiresColorTexture=true;cameraData.renderPostProcessing=false;
                rt.Create();Assert.That(rt.IsCreated()&&!rt.sRGB,Is.True);
                if(nb)nb.SetActive(false);
                directed=ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();directed.hideFlags=HideFlags.HideAndDontSave;
                directed.targetCamera=camera;directed.selectedPass=pass;directed.Create();directed.SetActive(true);
                data.rendererFeatures.Add(directed);data.SetDirty();
                writer.enabled=false;for(int i=0;i<4;i++)camera.Render();
                var background=Draw(writer,b,camera,rt,readback,folder,"background");writer.enabled=true;
                Color[] Snap(Material m,string label,bool vat,float frame,float strength=.5f)
                {
                    SetVAT(m,vat);m.SetFloat("_displayFrame",frame);
                    m.SetFloat(m==c?"_NB_DistortionIntensity":"_ScreenDistortIntensity",strength);
                    writer.sharedMaterial=m;for(int i=0;i<3;i++)camera.Render();
                    return Draw(writer,m,camera,rt,readback,folder,label);
                }
                var ao=Snap(a,"A-vat-off",false,1);var bo=Snap(b,"B-vat-off",false,1);var co=Snap(c,"C-vat-off",false,1);
                var aa=Snap(a,"A-vat-on",true,1);var ba=Snap(b,"B-vat-on",true,1);var ca=Snap(c,"C-vat-on",true,1);
                var af=Snap(a,"A-frame2",true,2);var bf=Snap(b,"B-frame2",true,2);var cf=Snap(c,"C-frame2",true,2);
                var ar=Snap(a,"A-repeat",true,2);var br=Snap(b,"B-repeat",true,2);var cr=Snap(c,"C-repeat",true,2);
                var az=Snap(a,"A-strength0",true,2,0);var bz=Snap(b,"B-strength0",true,2,0);var cz=Snap(c,"C-strength0",true,2,0);
                var record=new Record{caseId="vat-screen-exclusion-"+pass,orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,
                    scope="Exact NB pass VAT on/off and frame invariance plus independent distortion response; no complete controller chain claim",
                    finite=Finite(background,ao,bo,co,aa,ba,ca,af,bf,cf,ar,br,cr,az,bz,cz),
                    abOff=Delta(ao,bo),bcOff=Delta(bo,co),abOn=Delta(aa,ba),bcOn=Delta(ba,ca),abFrame=Delta(af,bf),bcFrame=Delta(bf,cf),
                    aRepeat=Delta(af,ar),bRepeat=Delta(bf,br),cRepeat=Delta(cf,cr),
                    aResponse=Delta(aa,az),bResponse=Delta(ba,bz),cResponse=Delta(ca,cz),
                    aDualResponse=Delta(ao,aa),bDualResponse=Delta(bo,ba),cDualResponse=Delta(co,ca),
                    aFrameRepeat=Delta(aa,af),bFrameResponse=Delta(ba,bf),cFrameResponse=Delta(ca,cf),
                    aVisible=Different(aa,background),bVisible=Different(ba,background),cVisible=Different(ca,background)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(record,true));Debug.Log("NBFX_G4_VAT_SCREEN "+JsonUtility.ToJson(record));
                Assert.That(record.finite,Is.True);Assert.That(record.abOff+record.bcOff+record.abOn+record.bcOn+record.abFrame+record.bcFrame,Is.Zero);
                Assert.That(record.aDualResponse+record.bDualResponse+record.cDualResponse+record.aFrameRepeat+record.bFrameResponse+record.cFrameResponse,Is.Zero);
                Assert.That(record.aRepeat+record.bRepeat+record.cRepeat,Is.Zero);
                Assert.That(record.aVisible,Is.GreaterThan(128));Assert.That(record.bVisible,Is.GreaterThan(128));Assert.That(record.cVisible,Is.GreaterThan(128));
                Assert.That(record.aResponse,Is.GreaterThan(.001f));Assert.That(record.bResponse,Is.GreaterThan(.001f));Assert.That(record.cResponse,Is.GreaterThan(.001f));
            }
            finally
            {
                if(nb)nb.SetActive(oldNB);
                if(directed){data.rendererFeatures.Remove(directed);data.SetDirty();UnityEngine.Object.DestroyImmediate(directed);}
                camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();
                foreach(var obj in new UnityEngine.Object[]{actor,backdrop,cameraObject,a,b,c,backdropMaterial,checker,noise,mask,position,second,rotation,mesh,rt,readback})UnityEngine.Object.DestroyImmediate(obj);
                Assert.That(File.ReadAllBytes(rendererFile),Is.EqualTo(before),"Screen test wrote renderer asset");
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
        [Serializable] sealed class DN0SourceHash { public string path, sha256; }
        [Serializable] sealed class DN0PassInfo { public int index; public string name, lightMode; }
        [Serializable] sealed class DN0MaterialState
        {
            public string shader;
            public int renderQueue;
            public float surface, zWrite, zWriteControl, zTest, cull;
            public string[] rawQueryKeys;
            public bool[] rawQueryEnabled;
            public DN0PassInfo[] actualPasses;
        }
        [Serializable] sealed class DN0Inputs
        {
            public string unity, api, gpu, resolvedPackage, generatedShaderSHA256;
            public bool ssaoActive, graphNormalsRawEnabled;
            public DN0SourceHash[] inputFiles;
            public DN0MaterialState[] materialStates;
        }
        static string DN0SHA256(string path)
        {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        static DN0Inputs DN0InputsAndState(string project, ScriptableRendererData rendererData,
            UniversalRenderPipelineAsset pipeline, Material[] materials)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(GraphPath);
            Assert.That(package, Is.Not.Null);
            var paths = new List<string>();
            foreach (string relative in new[] {
                "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph",
                "NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs",
                "NBShaders2/ShaderGraph/Passes/NBGraphForwardPass.hlsl",
                "NBShaders2/ShaderGraph/Passes/NBGraphShadowCasterPass.hlsl",
                "NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl",
                "NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl",
                "NBShaders2/Shader/HLSL/NBShaderInput.hlsl",
                "NBShaders2/Shader/NBShader.shader",
                "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader",
                "Tests/URP/Editor/G4GraphVATTests.cs",
                "Tests/URP/Editor/G4GraphDN0DefaultCoverageTests.cs" })
                paths.Add(Path.Combine(package.resolvedPath, relative));
            paths.Add(Path.Combine(project, AssetDatabase.GetAssetPath(rendererData)));
            paths.Add(Path.Combine(project, AssetDatabase.GetAssetPath(pipeline)));
            paths.Add(Path.Combine(project, "ProjectSettings/GraphicsSettings.asset"));
            paths.Add(Path.Combine(project, "ProjectSettings/QualitySettings.asset"));
            var hashes = paths.Select(path => new DN0SourceHash { path=path, sha256=DN0SHA256(path) }).ToArray();
            var states = new List<DN0MaterialState>();
            string[] keys = { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward", "Universal Forward", "DepthOnly",
                "DepthNormalsOnly", "ShadowCaster", "SHADOWCASTER", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass",
                "MotionVectors", "MOTIONVECTORS", "UniversalGBuffer", "SceneSelectionPass", "Picking", "Universal2D" };
            foreach (Material m in materials)
            {
                var passes = new List<DN0PassInfo>();
                for (int i = 0; i < m.passCount; ++i)
                    passes.Add(new DN0PassInfo { index=i, name=m.GetPassName(i),
                        lightMode=m.shader.FindPassTagValue(0, i, new ShaderTagId("LightMode")).name });
                states.Add(new DN0MaterialState { shader=m.shader.name, renderQueue=m.renderQueue,
                    surface=m.HasProperty("_Surface")?m.GetFloat("_Surface"):-1,
                    zWrite=m.GetFloat("_ZWrite"), zWriteControl=m.HasProperty("_ZWriteControl")?m.GetFloat("_ZWriteControl"):-1,
                    zTest=m.GetFloat("_ZTest"), cull=m.GetFloat("_Cull"), rawQueryKeys=keys,
                    rawQueryEnabled=keys.Select(key => m.GetShaderPassEnabled(key)).ToArray(), actualPasses=passes.ToArray() });
            }
            string folder = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            string generated = Path.Combine(folder, "generated-vat-candidate.shader");
            Assert.That(File.Exists(generated), Is.True);
            return new DN0Inputs { unity=Application.unityVersion, api=SystemInfo.graphicsDeviceType.ToString(),
                gpu=SystemInfo.graphicsDeviceName, resolvedPackage=package.resolvedPath,
                generatedShaderSHA256=DN0SHA256(generated), inputFiles=hashes, materialStates=states.ToArray(),
                graphNormalsRawEnabled=materials[2].GetShaderPassEnabled("DepthNormalsOnly"),
                ssaoActive=rendererData.rendererFeatures.Any(f => f && f.isActive && f.GetType().Name.Contains("AmbientOcclusion")) };
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
