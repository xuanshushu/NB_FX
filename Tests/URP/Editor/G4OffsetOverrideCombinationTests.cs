using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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
    // Real coplanar geometry/depth rejection. The fixed board writes depth;
    // positive/negative factor or units decide whether the NB color pass wins.
    // No source-text check substitutes for GPU rendering in these twelve cases.
    public sealed class G4OffsetOverrideCombinationTests
    {
        const int Size = 128;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<Object> owned = new List<Object>();
        T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        [TearDown] public void Cleanup() { foreach (var value in owned.AsEnumerable().Reverse()) if (value) Object.DestroyImmediate(value); owned.Clear(); }

        [OneTimeSetUp]
        public void ImportExactGraphAfterInstalledTarget_OnlyInOwnedIsolation()
        {
            const string expected = "D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002";
            const string graphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
            string actual = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\', '/').TrimEnd('/');
            Assert.That(string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase), Is.True,
                "Exact Graph import is authorized only for the existing isolated project.");
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                Assert.That(!scene.isLoaded ||
                    (scene.name.IndexOf("TAI", StringComparison.OrdinalIgnoreCase) < 0 &&
                     scene.path.IndexOf("TAI", StringComparison.OrdinalIgnoreCase) < 0), Is.True,
                    "TAI scene is loaded; do not proactively import/compile. " + scene.path);
            }
            var target = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.Rendering.Universal.ShaderGraph.NBGraphUnlitSubTarget", false)).FirstOrDefault(t => t != null);
            Assert.That(target, Is.Not.Null, "Installed SubTarget type must actually be loaded.");
            Assert.That(target.GetMethod("WithNBZOffset", Static), Is.Not.Null,
                "Offset Target implementation must be loaded before its one exact Graph reimport.");
            // Existing real URP Mesh warm initializes the active pipeline and
            // Graph importer metadata before exact reimport/pass queries.
            // Keep original TAI/isolation guards and every strict assertion.
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            // Reuse the already imported, current guarded graph. No extra force import.
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(graphPath); Assert.That(shader, Is.Not.Null);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                string describe(string property)
                {
                    int index = shader.FindPropertyIndex(property);
                    return property + ":present=" + material.HasProperty(property) + ":type=" +
                        (index < 0 ? "missing" : shader.GetPropertyType(index).ToString()) + ":default=" +
                        (material.HasProperty(property) ? material.GetFloat(property).ToString(System.Globalization.CultureInfo.InvariantCulture) : "missing");
                }
                Debug.Log("NBFX_ZOFFSET_EXACT_IMPORT project=" + actual + " Target.WithNBZOffset=true " +
                    describe("_offsetFactor") + " " + describe("_offsetUnits"));
            }
            finally { Object.DestroyImmediate(material); }
        }

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string stage in new[] { "Forward" })
                foreach (string axis in new[] { "factor", "units" })
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(stage, axis, ortho).SetName("G4OffsetOverrideABC_" + axis + (ortho ? "_ortho" : "_perspective"));
        }

        [Serializable] sealed class Metrics
        {
            public string stage, axis, api, unity, scope;
            public bool orthographic, finite;
            public float magnitude;
            public float[] ab, bc, repeat;
            public float aSignResponse, bSignResponse, cSignResponse;
            public int[] visibleAgainstBoard;
        }

        [Serializable] sealed class CombinationMetrics
        {
            public string axis,api,unity,scope;
            public bool orthographic,finite;
            public float[] ab,bc,repeat,nearFarResponseABC,restoreABC;
            public int[] nearVisibleABC;
            public float[] farRejectedToBoardABC;
        }

        Shader BoardShader()
        {
            string vertex = "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n" +
                "struct A { float4 p:POSITION; }; struct V { float4 p:SV_POSITION; }; " +
                "V Vert(A i) { V o; o.p=TransformObjectToHClip(i.p.xyz); return o; } ";
            string source = "Shader \"Hidden/NBFX/ZOffsetDepthBoard\" { SubShader { Tags { \"RenderPipeline\"=\"UniversalPipeline\" \"Queue\"=\"Geometry\" } " +
                "Pass { Tags { \"LightMode\"=\"UniversalForward\" } Cull Off ZWrite On ZTest LEqual Blend One Zero HLSLPROGRAM\n#pragma vertex Vert\n#pragma fragment Frag\n" +
                vertex + "half4 Frag(V i):SV_Target { float2 uv=GetNormalizedScreenSpaceUV(i.p); uint c=(uint)floor(uv.x*16)+(uint)floor(uv.y*16); return (c&1u)!=0u?half4(.125,.75,.25,1):half4(.75,.125,.5,1); }\nENDHLSL\n} " +
                "Pass { Tags { \"LightMode\"=\"DepthOnly\" } Cull Off ZWrite On ZTest LEqual ColorMask 0 HLSLPROGRAM\n#pragma vertex Vert\n#pragma fragment Frag\n" +
                vertex + "half4 Frag(V i):SV_Target { return 0; }\nENDHLSL\n} } }";
            var shader = Keep(ShaderUtil.CreateShaderAsset(source, false)); Assert.That(shader && shader.isSupported, Is.True); shader.hideFlags = HideFlags.HideAndDontSave; return shader;
        }

        Material NewMaterial(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path); Assert.That(shader && shader.isSupported, Is.True, path);
            return Keep(new Material(shader) { hideFlags = HideFlags.HideAndDontSave });
        }

        static void Configure(Material material, bool graph, bool screen, string stage, Texture2D noise)
        {
            if (screen)
                typeof(G4GraphScreenNoiseTests).GetMethod("Configure", Static).Invoke(null, new object[] { material, graph, noise, Texture2D.whiteTexture, stage, "mask-half" });
            else
            {
                material.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } : new[] { "_FX_LIGHT_MODE_UNLIT" };
                material.SetTexture("_BaseMap", Texture2D.whiteTexture);
                material.SetColor(graph ? "_Color" : "_BaseColor", Color.red);
                material.SetColor("_ColorA", Color.white); material.SetFloat("_AlphaAll", 1); material.SetFloat("_BaseColorIntensityForTimeline", 1);
                material.SetFloat("_Cull", 0); material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
                material.SetFloat("_ZWrite", 0); material.SetFloat("_SrcBlend", 1); material.SetFloat("_DstBlend", 0); material.SetFloat("_ColorMask", 15);
                material.renderQueue = 3000;
                if (graph)
                {
                    material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlendAlpha", 1); material.SetFloat("_DstBlendAlpha", 0);
                    material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0)); material.SetFloat("_NB_DistortionMode", 0);
                    material.SetFloat("_NB_ColorChannelLo16", 3); material.SetFloat("_NB_Flags0Lo16", 0); material.SetFloat("_NB_Flags0Hi16", 0);
                    material.SetFloat("_NB_Flags1Lo16", 0); material.SetFloat("_NB_Flags1Hi16", 0);
                }
                else
                {
                    material.SetInteger("_W9ParticleShaderFlags", 0); material.SetInteger("_W9ParticleShaderFlags1", 0);
                    material.SetInteger("_W9ParticleShaderColorChannelFlag", 3); material.SetFloat("_fogintensity", 0);
                }
            }
            foreach (string property in new[] { "_Mask_Toggle", "_Dissolve_Toggle", "_RampColorToggle", "_EmissionEnabled", "_ColorBlendMap_Toggle", "_ProgramNoise_Toggle", "_VAT_Toggle", "_FlipbookBlending", "_OverrideZ_Toggle" })
                if (material.HasProperty(property)) material.SetFloat(property, 0);
            if (material.HasProperty("_ZOffset_Toggle")) material.SetFloat("_ZOffset_Toggle", 0); // Render state still consumes the two script-set values.
            material.DisableKeyword("_OVERRIDE_Z"); material.DisableKeyword("_ALPHATEST_ON"); material.SetFloat("_AlphaClip", 0);
            if (material.HasProperty("_FxLightMode")) material.SetFloat("_FxLightMode", 0);
            foreach (string pass in new[] { "Universal Forward", "UniversalForward", "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "DepthOnly", "DepthNormalsOnly", "ShadowCaster", "Universal2D", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass" })
                material.SetShaderPassEnabled(pass, false);
            // Material pass enablement addresses the LightMode tag, not the
            // Graph pass displayName "Universal Forward" / FindPass name.
            string requested = screen ? stage : graph ? "Universal Forward" : "UniversalForward";
            int passIndex = material.FindPass(requested);
            string passInventory = string.Join("|", Enumerable.Range(0, material.passCount).Select(i => i + ":" + material.GetPassName(i)));
            string shaderMessages = string.Join("|", ShaderUtil.GetShaderMessages(material.shader).Select(message =>
                message.severity + ":" + message.message));
            string diagnostic = "shader=" + material.shader.name + " graph=" + graph + " stage=" + stage +
                " requested=" + requested + " FindPass=" + passIndex + " passCount=" + material.passCount +
                " supported=" + material.shader.isSupported + " actualPasses=" + passInventory +
                " keywords=" + string.Join(",", material.shaderKeywords) + " ShaderMessages=" + shaderMessages;
            Debug.Log("NBFX_ZOFFSET_PASS_INVENTORY " + diagnostic);
            Assert.That(passIndex, Is.GreaterThanOrEqualTo(0), "Actual pass/tag lookup required; no retry. " + diagnostic);
            string actualTag = material.shader.FindPassTagValue(passIndex, new ShaderTagId("LightMode")).name;
            string lightMode = string.IsNullOrEmpty(actualTag) ? "SRPDefaultUnlit" : actualTag;
            bool before = material.GetShaderPassEnabled(lightMode);
            material.SetShaderPassEnabled(lightMode, true);
            bool after = material.GetShaderPassEnabled(lightMode);
            Debug.Log("NBFX_ZOFFSET_PASS_STATE shader=" + material.shader.name + " actualTag=" +
                (string.IsNullOrEmpty(actualTag) ? "<None>" : actualTag) + " effectiveLightMode=" + lightMode +
                " before=" + before + " after=" + after + " displayAliasEnabled=" + material.GetShaderPassEnabled("Universal Forward"));
            Assert.That(after, Is.True, "Actual color-pass LightMode must be enabled before rendering.");
        }

        [TestCaseSource(nameof(Cases))]
        public void OffsetAndConditionalSVDepth_RealOcclusionMatchesFrozen(string stage, string axis, bool ortho)
        {
            bool screen = stage != "Forward";
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; Assert.That(pipeline, Is.Not.Null);
            var data = pipeline.rendererDataList[0]; var rendererPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(data));
            byte[] rendererBytes = File.ReadAllBytes(rendererPath);
            var nb = data.rendererFeatures.First(f => f && f.GetType().FullName == "NBShader.NBPostProcess"); bool nbActive = nb.isActive;
            var board = Keep(GameObject.CreatePrimitive(PrimitiveType.Quad)); var actor = Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));
            var cameraObject = Keep(new GameObject("ZOffset coplanar depth camera")); var camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing = false; cameraData.requiresColorTexture = screen;
            var rt = Keep(new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)); var read = Keep(new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true));
            var noise = Keep(new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true)); noise.SetPixel(0, 0, new Color(.75f, .25f, 0, 1)); noise.Apply(false); noise.filterMode = FilterMode.Point;
            var a = NewMaterial("Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");
            var b = NewMaterial("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");
            var c = NewMaterial("Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph"); var materials = new[] { a, b, c };
            Assert.That(c.HasProperty("_offsetFactor") && c.HasProperty("_offsetUnits"), Is.True, "Install two Float render-state properties before GPU test.");
            foreach (string property in new[] { "_offsetFactor", "_offsetUnits" })
            {
                int index = c.shader.FindPropertyIndex(property); Assert.That(c.shader.GetPropertyType(index), Is.EqualTo(ShaderPropertyType.Float));
                Assert.That(c.GetFloat(property), Is.Zero, "New material default must preserve zero Offset behavior.");
            }
            var scene = EditorSceneManager.NewPreviewScene();
            var oldRT = RenderTexture.active; bool oldAsync = ShaderUtil.allowAsyncCompilation; G4ScreenNoiseDirectedFeature directed = null;
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXZOffset"), stage + "-" + axis + (ortho ? "-ortho" : "-perspective")); Directory.CreateDirectory(folder);
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                foreach (var go in new[] { board, actor, cameraObject }) SceneManager.MoveGameObjectToScene(go, scene);
                board.layer = 2; actor.layer = screen ? G4GraphScreenNoiseTests.ForegroundLayer : 4;
                board.transform.localScale = actor.transform.localScale = new Vector3(2, 2, 1);
                board.transform.rotation = actor.transform.rotation = axis == "factor" ? Quaternion.Euler(0, 20, 0) : Quaternion.identity;
                var boardRenderer = board.GetComponent<MeshRenderer>(); boardRenderer.shadowCastingMode = ShadowCastingMode.Off;
                boardRenderer.sharedMaterial = Keep(new Material(BoardShader()) { hideFlags = HideFlags.HideAndDontSave }); boardRenderer.sharedMaterial.renderQueue = 2000;
                var actorRenderer = actor.GetComponent<MeshRenderer>(); actorRenderer.shadowCastingMode = ShadowCastingMode.Off; actorRenderer.receiveShadows = false;
                for (int i = 0; i < 3; ++i) Configure(materials[i], i == 2, screen, stage, noise);
                camera.scene = scene; camera.enabled = false; camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 42;
                camera.nearClipPlane = .1f; camera.farClipPlane = 25; camera.transform.position = new Vector3(0, 0, 5); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f, .2f, .4f, 1); camera.allowHDR = true; camera.allowMSAA = false;
                camera.cullingMask = (1 << 2) | (1 << actor.layer); camera.targetTexture = rt; Assert.That(rt.Create() && !rt.sRGB, Is.True);
                nb.SetActive(false);
                if (screen)
                {
                    directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>(); directed.hideFlags = HideFlags.HideAndDontSave;
                    directed.targetCamera = camera; directed.selectedPass = stage; directed.Create(); directed.SetActive(true); data.rendererFeatures.Add(directed); data.SetDirty();
                }
                Color[] Capture(string name)
                {
                    for (int warm = 0; warm < 4; ++warm) camera.Render();
                    RenderTexture.active = rt; read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0, false); read.Apply(false, false); var pixels = read.GetPixels();
                    using (var stream = File.Create(Path.Combine(folder, name + ".rgba-f32.gz"))) using (var zip = new GZipStream(stream, System.IO.Compression.CompressionLevel.Optimal)) using (var writer = new BinaryWriter(zip))
                        foreach (var pixel in pixels) { writer.Write(pixel.r); writer.Write(pixel.g); writer.Write(pixel.b); writer.Write(pixel.a); }
                    return pixels;
                }
                float Delta(Color[] x, Color[] y) => x.Zip(y, (px, py) => Enumerable.Range(0, 4).Max(i => Mathf.Abs(px[i] - py[i]))).Max();
                int Visible(Color[] x, Color[] y) => x.Zip(y, (px, py) => Enumerable.Range(0, 4).Any(i => px[i] != py[i]) ? 1 : 0).Sum();
                actorRenderer.enabled = false; var background = Capture("coplanar-depth-board"); actorRenderer.enabled = true;
                float magnitude = axis == "factor" ? 16f : 512f;
                var frames = new Color[3][][]; var repeats = new Color[3][][];
                float[] values = { -magnitude, 0f, magnitude };
                for (int state = 0; state < 3; ++state)
                {
                    frames[state] = new Color[3][]; repeats[state] = new Color[3][];
                    for (int m = 0; m < 3; ++m)
                    {
                        materials[m].SetFloat("_offsetFactor", axis == "factor" ? values[state] : 0);
                        materials[m].SetFloat("_offsetUnits", axis == "units" ? values[state] : 0);
                        actorRenderer.sharedMaterial = materials[m]; frames[state][m] = Capture("ABC"[m] + "-state" + state); repeats[state][m] = Capture("ABC"[m] + "-state" + state + "-repeat");
                    }
                }
                var metrics = new Metrics { stage = stage, axis = axis, api = SystemInfo.graphicsDeviceType.ToString(), unity = Application.unityVersion, orthographic = ortho, magnitude = magnitude,
                    scope = "Three existing color Passes only; coplanar depth board then NB color depth rejection, signed slope-factor or unit bias. No BackFirst/Universal2D/DepthOnly/ShadowCaster/OverrideZ combination/VFX/Player/performance claim.",
                    finite = background.Concat(frames.SelectMany(x => x).SelectMany(x => x)).Concat(repeats.SelectMany(x => x).SelectMany(x => x)).All(p => Enumerable.Range(0, 4).All(i => !float.IsNaN(p[i]) && !float.IsInfinity(p[i]))),
                    ab = Enumerable.Range(0, 3).Select(s => Delta(frames[s][0], frames[s][1])).ToArray(), bc = Enumerable.Range(0, 3).Select(s => Delta(frames[s][1], frames[s][2])).ToArray(),
                    repeat = Enumerable.Range(0, 3).Select(s => Enumerable.Range(0, 3).Max(m => Delta(frames[s][m], repeats[s][m]))).ToArray(),
                    aSignResponse = Delta(frames[0][0], frames[2][0]), bSignResponse = Delta(frames[0][1], frames[2][1]), cSignResponse = Delta(frames[0][2], frames[2][2]),
                    visibleAgainstBoard = Enumerable.Range(0, 3).Select(m => Mathf.Max(Visible(frames[0][m], background), Visible(frames[2][m], background))).ToArray() };
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(metrics, true)); Debug.Log("NBFX_ZOFFSET_STATE " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite, Is.True); Assert.That(metrics.ab.All(v => v == 0) && metrics.bc.All(v => v == 0) && metrics.repeat.All(v => v == 0), Is.True, "Full-frame strict zero B/C and Frozen/current/repeat required.");
                Assert.That(metrics.aSignResponse, Is.GreaterThan(.01f)); Assert.That(metrics.bSignResponse, Is.GreaterThan(.01f)); Assert.That(metrics.cSignResponse, Is.GreaterThan(.01f));
                Assert.That(metrics.visibleAgainstBoard.All(v => v > 150), Is.True, "Empty/fully rejected draws must not count as matching.");
                // Existing OverrideZ contract: keyword-on depth3 vs7 changes
                // real depth rejection by >.1. Offset remains nonzero for both.
                float[] priorDepth=materials.Select(m=>m.GetFloat("_OverrideZValue")).ToArray();
                var ovFrames=new Color[3][][][];var ovRepeat=new Color[3][][][];
                for(int offset=0;offset<3;offset++)
                {
                    ovFrames[offset]=new Color[2][][];ovRepeat[offset]=new Color[2][][];
                    for(int depthState=0;depthState<2;depthState++)
                    {
                        ovFrames[offset][depthState]=new Color[3][];ovRepeat[offset][depthState]=new Color[3][];
                        for(int role=0;role<3;role++)
                        {
                            var material=materials[role];material.SetFloat("_offsetFactor",axis=="factor"?values[offset]:0);material.SetFloat("_offsetUnits",axis=="units"?values[offset]:0);
                            material.SetFloat("_OverrideZ_Toggle",1);material.SetFloat("_OverrideZValue",depthState==0?3:7);material.EnableKeyword("_OVERRIDE_Z");
                            Assert.That(material.IsKeywordEnabled("_OVERRIDE_Z"),Is.True,"Same authoritative runtime keyword for A/B/C; GUI lifecycle is separate.");
                            actorRenderer.sharedMaterial=material;string label="ABC"[role]+"-override-offset"+offset+"-d"+(depthState==0?3:7);
                            ovFrames[offset][depthState][role]=Capture(label);ovRepeat[offset][depthState][role]=Capture(label+"-repeat");
                        }
                    }
                }
                var restored=new Color[3][];var restoredRepeat=new Color[3][];
                for(int role=0;role<3;role++)
                {
                    var material=materials[role];material.SetFloat("_offsetFactor",0);material.SetFloat("_offsetUnits",0);material.SetFloat("_OverrideZ_Toggle",0);material.SetFloat("_OverrideZValue",priorDepth[role]);material.DisableKeyword("_OVERRIDE_Z");
                    actorRenderer.sharedMaterial=material;restored[role]=Capture("ABC"[role]+"-restored");restoredRepeat[role]=Capture("ABC"[role]+"-restored-repeat");
                }
                var combo=new CombinationMetrics{axis=axis,orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,
                    scope="Existing coplanar slope/units bias off-SVDepth controls unchanged; real NormalForward SVDepth depth3/7 at negative/zero/positive Offset. No NB SVDepth, Modern lifecycle, Player, perf or Gate claim.",
                    finite=ovFrames.SelectMany(x=>x).SelectMany(x=>x).Concat(ovRepeat.SelectMany(x=>x).SelectMany(x=>x)).Concat(restored).Concat(restoredRepeat).SelectMany(x=>x).All(p=>Enumerable.Range(0,4).All(i=>!float.IsNaN(p[i])&&!float.IsInfinity(p[i]))),
                    ab=Enumerable.Range(0,3).SelectMany(o=>Enumerable.Range(0,2).Select(d=>Delta(ovFrames[o][d][0],ovFrames[o][d][1]))).ToArray(),
                    bc=Enumerable.Range(0,3).SelectMany(o=>Enumerable.Range(0,2).Select(d=>Delta(ovFrames[o][d][1],ovFrames[o][d][2]))).ToArray(),
                    repeat=Enumerable.Range(0,3).SelectMany(o=>Enumerable.Range(0,2).Select(d=>Enumerable.Range(0,3).Max(m=>Delta(ovFrames[o][d][m],ovRepeat[o][d][m])))).Concat(Enumerable.Range(0,3).Select(m=>Delta(restored[m],restoredRepeat[m]))).ToArray(),
                    nearFarResponseABC=Enumerable.Range(0,3).SelectMany(o=>Enumerable.Range(0,3).Select(m=>Delta(ovFrames[o][0][m],ovFrames[o][1][m]))).ToArray(),
                    nearVisibleABC=Enumerable.Range(0,3).SelectMany(o=>Enumerable.Range(0,3).Select(m=>Visible(ovFrames[o][0][m],background))).ToArray(),
                    farRejectedToBoardABC=Enumerable.Range(0,3).SelectMany(o=>Enumerable.Range(0,3).Select(m=>Delta(ovFrames[o][1][m],background))).ToArray(),
                    restoreABC=Enumerable.Range(0,3).Select(m=>Delta(frames[1][m],restored[m])).ToArray()};
                File.WriteAllText(Path.Combine(folder,"combination-metrics.json"),JsonUtility.ToJson(combo,true));Debug.Log("NBFX_OFFSET_OVERRIDE_COMBO "+JsonUtility.ToJson(combo));
                Assert.That(combo.finite,Is.True);Assert.That(combo.ab.All(v=>v==0)&&combo.bc.All(v=>v==0)&&combo.repeat.All(v=>v==0),Is.True,"Original strict ABC/repeat zero retained for every actual SVDepth/Offset combination.");
                Assert.That(combo.nearFarResponseABC.All(v=>v>.1f),Is.True,"Original OverrideZ depth3/7 strong response must still hold at both nonzero Offset signs; no empty consistency.");
                Assert.That(combo.nearVisibleABC.All(v=>v>150),Is.True,"Original strong visible actor requirement retained.");
                Assert.That(combo.farRejectedToBoardABC.All(v=>v==0),Is.True,"Far depth7 must actually reject against valid pre-drawn depth board.");Assert.That(combo.restoreABC.All(v=>v==0),Is.True,"Zero Offset / OVZ off restored frame must be exact original baseline.");

            }
            finally
            {
                if (directed) { data.rendererFeatures.Remove(directed); data.SetDirty(); Object.DestroyImmediate(directed); }
                nb.SetActive(nbActive); ShaderUtil.allowAsyncCompilation = oldAsync; camera.targetTexture = null; RenderTexture.active = oldRT; rt.Release(); EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererPath), Is.EqualTo(rendererBytes), "Test must not save RendererData.");
            }
        }
    }
}
