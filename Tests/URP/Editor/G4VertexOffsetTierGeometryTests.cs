using System;
using System.Linq;
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
    /// <summary>
    /// Ordinary Mesh VertexOffset B/C only: four direction modes, independent
    /// map/mask UV sources 0/1/2/8, packed channels/wrap, and rotated,
    /// non-uniformly scaled Mesh. No CustomData, VAT, CustomLocal or VFX claim.
    /// </summary>
    public sealed class G4VertexOffsetTierGeometryTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 2;

        struct OffsetCase
        {
            public string name;
            public int directionMode, mapUVMode, maskUVMode, sharedUVMode, mapChannel, maskChannel;
            public uint flags0, flags1, wrapFlags;
            public bool mask, worldDirection, startFromZero;
            public Vector4 sharedST, sharedVec, twirl, polar;
            public float twirlStrength;
        }

        [Serializable]
        sealed class Metrics
        {
            public string caseId, unityVersion, api, target, note;
            public bool orthographic, finite;
            public int pixels, differentRGBA, graphVisible, legacyVisible;
            public float maxRGBA, offMaxRGBA, graphRepeat, legacyRepeat;
            public int graphOnOffPixels, legacyOnOffPixels, graphMaskPixels, legacyMaskPixels;
            public int graphPasses, legacyPasses;
        }

        [Serializable]sealed class TierMetric{public bool finite;public float parentBC,maskBC,restoreB,restoreC;public int parentChangedB,parentChangedC,maskChangedB,maskChangedC;}
        static IEnumerable<TestCaseData> Cases()
        {foreach(bool ortho in new[]{true,false})yield return new TestCaseData("texture-world-map0-mask1-uv0zw",ortho).SetName("G4VertexOffsetTierBC_parent_mask_restore_"+(ortho?"ortho":"perspective"));}
        [TestCaseSource(nameof(Cases))]
        public void OrdinaryMeshVertexOffsetMatchesShaderLab(string name, bool orthographic)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Shader legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            OffsetCase test = Define(name);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4VertexOffset");
            string output = Path.Combine(root, name + (orthographic ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(output);

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject cameraObject = new GameObject("NBFX ordinary Mesh VertexOffset B/C");
            Mesh ownedMesh = UnityEngine.Object.Instantiate(front.GetComponent<MeshFilter>().sharedMesh);
            front.GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            Material graph = new Material(graphShader), legacy = new Material(legacyShader);
            Texture2D map = MakeMap(), mask = MakeMask();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var camera = cameraObject.AddComponent<Camera>();
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                SetMeshStreams(ownedMesh);
                SceneManager.MoveGameObjectToScene(front, scene);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                front.layer = Layer;
                front.transform.position = new Vector3(0, 0, 0);
                front.transform.rotation = Quaternion.Euler(7, 34, 3);
                front.transform.localScale = new Vector3(1.2f, 1.1f, 1.7f);
                var renderer = front.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.orthographic = orthographic;
                camera.orthographicSize = 1.15f;
                camera.fieldOfView = 40f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20f;
                camera.cullingMask = 1 << Layer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f, .06f, .09f, 1);
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.targetTexture = target;
                target.Create();
                Assert.That(target.IsCreated() && !target.sRGB, Is.True);

                Configure(graph, true, map, mask);
                Configure(legacy, false, map, mask);
                SetProtocol(graph, legacy, test);
                InitializeGraphMaterial(graph);

                var applier=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier",false)).First(t=>t!=null);
                var apply=applier.GetMethod("ApplyGraphVertexOffsetGroup",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                var tierType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShader.NBShaderFeatureTier",false)).First(t=>t!=null);
                bool Policy(string[] allowed)
                {object[] args={graph,Enum.ToObject(tierType,3),allowed,false};return (bool)apply.Invoke(null,args);}
                Assert.That(Policy(new[]{"_VERTEX_OFFSET","_VERTEX_OFFSET_MASKMAP"}),Is.True);
                renderer.sharedMaterial = legacy;
                Color[] b = Capture(camera, target, readback, Path.Combine(output, "B-on"));
                Color[] br = Capture(camera, target, readback, Path.Combine(output, "B-repeat"));
                renderer.sharedMaterial = graph;
                Color[] c = Capture(camera, target, readback, Path.Combine(output, "C-on"));
                Color[] cr = Capture(camera, target, readback, Path.Combine(output, "C-repeat"));


                string rawBefore=UnityEditor.EditorJsonUtility.ToJson(graph);
                Vector4 rawVec=graph.GetVector("_VertexOffset_Vec");float rawToggle=graph.GetFloat("_VertexOffset_Toggle"),rawMask=graph.GetFloat("_VertexOffset_Mask_Toggle");
                Assert.That(Policy(new[]{"_VERTEX_OFFSET_MASKMAP"}),Is.True);legacy.DisableKeyword("_VERTEX_OFFSET");
                renderer.sharedMaterial=legacy;var bDenied=Capture(camera,target,readback,Path.Combine(output,"B-tier-parent-denied"));var bDeniedRepeat=Capture(camera,target,readback,Path.Combine(output,"B-tier-parent-denied-repeat"));renderer.sharedMaterial=graph;var cDenied=Capture(camera,target,readback,Path.Combine(output,"C-tier-parent-denied"));var cDeniedRepeat=Capture(camera,target,readback,Path.Combine(output,"C-tier-parent-denied-repeat"));
                Assert.That(graph.GetVector("_VertexOffset_Vec"),Is.EqualTo(rawVec));Assert.That(graph.GetFloat("_VertexOffset_Toggle"),Is.EqualTo(rawToggle));Assert.That(graph.GetFloat("_VertexOffset_Mask_Toggle"),Is.EqualTo(rawMask));
                legacy.EnableKeyword("_VERTEX_OFFSET");legacy.DisableKeyword("_VERTEX_OFFSET_MASKMAP");Assert.That(Policy(new[]{"_VERTEX_OFFSET"}),Is.True);
                renderer.sharedMaterial=legacy;var bMaskDenied=Capture(camera,target,readback,Path.Combine(output,"B-tier-mask-denied"));var bMaskDeniedRepeat=Capture(camera,target,readback,Path.Combine(output,"B-tier-mask-denied-repeat"));renderer.sharedMaterial=graph;var cMaskDenied=Capture(camera,target,readback,Path.Combine(output,"C-tier-mask-denied"));var cMaskDeniedRepeat=Capture(camera,target,readback,Path.Combine(output,"C-tier-mask-denied-repeat"));
                legacy.EnableKeyword("_VERTEX_OFFSET_MASKMAP");Assert.That(Policy(new[]{"_VERTEX_OFFSET","_VERTEX_OFFSET_MASKMAP"}),Is.True);
                renderer.sharedMaterial=legacy;var bRestored=Capture(camera,target,readback,Path.Combine(output,"B-tier-restored"));var bRestoredRepeat=Capture(camera,target,readback,Path.Combine(output,"B-tier-restored-repeat"));renderer.sharedMaterial=graph;var cRestored=Capture(camera,target,readback,Path.Combine(output,"C-tier-restored"));var cRestoredRepeat=Capture(camera,target,readback,Path.Combine(output,"C-tier-restored-repeat"));
                Assert.That(UnityEditor.EditorJsonUtility.ToJson(graph),Is.EqualTo(rawBefore),"All original Graph material intent/gates/flags restored.");
                var tierMetrics=new[]{Compare(bDenied,cDenied),Compare(bMaskDenied,cMaskDenied),Compare(bRestored,cRestored)};
                File.WriteAllText(Path.Combine(output,"tier-metrics.json"),JsonUtility.ToJson(new TierMetric{finite=tierMetrics.All(m=>m.finite),parentBC=Compare(bDenied,cDenied).maxRGBA,maskBC=Compare(bMaskDenied,cMaskDenied).maxRGBA,restoreB=Compare(b,bRestored).maxRGBA,restoreC=Compare(c,cRestored).maxRGBA,parentChangedB=ChangedPixels(b,bDenied),parentChangedC=ChangedPixels(c,cDenied),maskChangedB=ChangedPixels(b,bMaskDenied),maskChangedC=ChangedPixels(c,cMaskDenied)},true));

                Assert.That(Compare(bDenied,bDeniedRepeat).finite,Is.True);Assert.That(Compare(bDenied,bDeniedRepeat).maxRGBA,Is.Zero);
                Assert.That(Compare(cDenied,cDeniedRepeat).finite,Is.True);Assert.That(Compare(cDenied,cDeniedRepeat).maxRGBA,Is.Zero);
                Assert.That(Compare(bMaskDenied,bMaskDeniedRepeat).finite,Is.True);Assert.That(Compare(bMaskDenied,bMaskDeniedRepeat).maxRGBA,Is.Zero);
                Assert.That(Compare(cMaskDenied,cMaskDeniedRepeat).finite,Is.True);Assert.That(Compare(cMaskDenied,cMaskDeniedRepeat).maxRGBA,Is.Zero);
                Assert.That(Compare(bRestored,bRestoredRepeat).finite,Is.True);Assert.That(Compare(bRestored,bRestoredRepeat).maxRGBA,Is.Zero);
                Assert.That(Compare(cRestored,cRestoredRepeat).finite,Is.True);Assert.That(Compare(cRestored,cRestoredRepeat).maxRGBA,Is.Zero);
                Assert.That(tierMetrics.All(m=>m.finite),Is.True);Assert.That(tierMetrics.All(m=>m.differentRGBA==0),Is.True);Assert.That(Compare(b,bRestored).maxRGBA+Compare(c,cRestored).maxRGBA,Is.Zero);
                Assert.That(ChangedPixels(b,bDenied),Is.GreaterThan(50));Assert.That(ChangedPixels(c,cDenied),Is.GreaterThan(50));Assert.That(ChangedPixels(b,bMaskDenied),Is.GreaterThan(30));Assert.That(ChangedPixels(c,cMaskDenied),Is.GreaterThan(30));
                graph.SetVector("_VertexOffset_Vec", Vector4.zero);
                legacy.SetVector("_VertexOffset_Vec", Vector4.zero);
                renderer.sharedMaterial = legacy;
                Color[] bOff = Capture(camera, target, readback, Path.Combine(output, "B-zeroIntensity"));
                renderer.sharedMaterial = graph;
                Color[] cOff = Capture(camera, target, readback, Path.Combine(output, "C-zeroIntensity"));

                int bMaskPixels = 0, cMaskPixels = 0;
                if (test.mask)
                {
                    graph.SetVector("_VertexOffset_Vec", new Vector4(0, 0, .43f, 0));
                    legacy.SetVector("_VertexOffset_Vec", new Vector4(0, 0, .43f, 0));
                    graph.SetFloat("_VertexOffset_Mask_Toggle", 0);
                    legacy.DisableKeyword("_VERTEX_OFFSET_MASKMAP");
                    renderer.sharedMaterial = legacy;
                    Color[] bNoMask = Capture(camera, target, readback, Path.Combine(output, "B-noMask"));
                    renderer.sharedMaterial = graph;
                    Color[] cNoMask = Capture(camera, target, readback, Path.Combine(output, "C-noMask"));
                    bMaskPixels = ChangedPixels(b, bNoMask);
                    cMaskPixels = ChangedPixels(c, cNoMask);
                    Assert.That(Compare(bNoMask, cNoMask).differentRGBA, Is.Zero,
                        "Mask-off control must also be strict B/C.");
                }
                Metrics metrics = Compare(b, c);
                metrics.caseId = name + (orthographic ? "-ortho" : "-perspective");
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.target = "128x128 RGBAHalf linear, full-frame strict B/C";
                metrics.note = "Mesh only; four warm-up renders, distinct float4 UV0/1/2, vertex RGB, nonuniform rotated transform, nonidentity independent map/mask ST. CustomData/VAT/CustomLocal/VFX and first frame not tested.";
                metrics.orthographic = orthographic;
                metrics.graphPasses = graph.passCount;
                metrics.legacyPasses = legacy.passCount;
                metrics.legacyRepeat = Compare(b, br).maxRGBA;
                metrics.graphRepeat = Compare(c, cr).maxRGBA;
                metrics.offMaxRGBA = Compare(bOff, cOff).maxRGBA;
                metrics.finite &= Compare(bOff, cOff).finite && Compare(b, br).finite && Compare(c, cr).finite;
                metrics.legacyOnOffPixels = ChangedPixels(b, bOff);
                metrics.graphOnOffPixels = ChangedPixels(c, cOff);
                metrics.legacyMaskPixels = bMaskPixels;
                metrics.graphMaskPixels = cMaskPixels;
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_VERTEX_OFFSET_BC " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite, Is.True, "Non-finite geometry output.");
                Assert.That(metrics.legacyVisible, Is.GreaterThan(400), "ShaderLab Mesh absent.");
                Assert.That(metrics.graphVisible, Is.GreaterThan(400), "Graph Mesh absent.");
                Assert.That(metrics.legacyRepeat, Is.Zero);
                Assert.That(metrics.graphRepeat, Is.Zero);
                Assert.That(metrics.legacyOnOffPixels, Is.GreaterThan(50), "ShaderLab displacement did not move enough pixels.");
                Assert.That(metrics.graphOnOffPixels, Is.GreaterThan(50), "Graph displacement did not move enough pixels.");
                if (test.mask)
                {
                    Assert.That(metrics.legacyMaskPixels, Is.GreaterThan(30), "ShaderLab mask control had no effect.");
                    Assert.That(metrics.graphMaskPixels, Is.GreaterThan(30), "Graph mask control had no effect.");
                }
                Assert.That(metrics.differentRGBA, Is.Zero, "Strict displaced Mesh B/C mismatch.");
                Assert.That(Compare(bOff, cOff).differentRGBA, Is.Zero, "Strict zero-intensity Mesh B/C mismatch.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                foreach (UnityEngine.Object obj in new UnityEngine.Object[] {
                    ownedMesh, graph, legacy, map, mask, target, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static OffsetCase Define(string name)
        {
            var c = new OffsetCase {
                name = name, directionMode = 0, mapChannel = 1, maskChannel = 2,
                flags1 = 1u << 9, sharedST = new Vector4(.83f, .91f, .09f, -.07f),
                sharedVec = new Vector4(0, 0, 31, 0),
                twirl = new Vector4(.39f, .61f, 0, 0),
                polar = new Vector4(.43f, .55f, 1, 0), twirlStrength = 2.2f
            };
            switch (name)
            {
                case "custom-map0": c.startFromZero = true; break;
                case "normal-map1-uv1":
                    c.directionMode = 1; c.mapUVMode = 1;
                    c.flags1 |= (1u << 21) | (1u << 18); c.startFromZero = true; break;
                case "color-map2-twirl":
                    c.directionMode = 2; c.mapUVMode = 2; c.flags0 = 1u << 9; break;
                case "color-world-map8-shared-uv2":
                    c.directionMode = 2; c.worldDirection = true; c.mapUVMode = 8;
                    c.sharedUVMode = 1; c.flags1 |= (1u << 21) | (1u << 19); break;
                case "texture-world-map0-mask1-uv0zw":
                    c.directionMode = 3; c.worldDirection = true; c.mask = true;
                    c.maskUVMode = 1; c.startFromZero = true; c.wrapFlags = (1u << 9) | (1u << (13 + 16)); break;
                case "custom-map8-shared-twirl-mask2":
                    c.mapUVMode = 8; c.sharedUVMode = 2; c.maskUVMode = 2;
                    c.flags0 = (1u << 8) | (1u << 9); c.mask = true; c.startFromZero = true; break;
                case "normal-map1-uv2-mask8-shared":
                    c.directionMode = 1; c.mapUVMode = 1; c.maskUVMode = 8;
                    c.sharedUVMode = 0; c.mask = true; c.startFromZero = true;
                    c.flags1 |= (1u << 21) | (1u << 19); c.wrapFlags = 1u << (9 + 16); break;
                default: throw new ArgumentException(name);
            }
            if (c.startFromZero) c.flags1 |= 1u << 25;
            return c;
        }

        static void SetMeshStreams(Mesh mesh)
        {
            Vector2[] uv = mesh.uv;
            var uv0 = new List<Vector4>(); var uv1 = new List<Vector4>(); var uv2 = new List<Vector4>();
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                float x = uv[i].x, y = uv[i].y;
                uv0.Add(new Vector4(.11f + .76f*x, .13f + .70f*y, .82f - .49f*x, .18f + .57f*y));
                uv1.Add(new Vector4(.16f + .47f*x, .80f - .43f*y, .13f + .33f*x, .12f + .27f*y));
                uv2.Add(new Vector4(.78f - .52f*x, .10f + .54f*y, .89f - .58f*x, .73f - .39f*y));
                colors[i] = new Color(.91f, .46f, .72f, 1);
            }
            mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1); mesh.SetUVs(2, uv2); mesh.colors = colors;
        }

        static Texture2D MakeMap()
        {
            const int n = 16;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            var pixels = new Color[n*n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                pixels[y*n+x] = new Color(.25f+.68f*x/(n-1f), .33f+.61f*y/(n-1f),
                    .19f+.73f*(x+y)/(2f*n-2f), .45f+.45f*x/(n-1f));
            texture.SetPixels(pixels); texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat; texture.Apply(false); return texture;
        }

        static Texture2D MakeMask()
        {
            const int n = 16;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            var pixels = new Color[n*n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                pixels[y*n+x] = new Color(.12f+.80f*x/(n-1f), .14f+.73f*y/(n-1f),
                    .09f+.86f*(x+y)/(2f*n-2f), 1);
            texture.SetPixels(pixels); texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat; texture.Apply(false); return texture;
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

        static void Configure(Material m, bool graph, Texture2D map, Texture2D mask)
        {
            m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            m.SetColor(graph ? "_Color" : "_BaseColor", new Color(.9f, .25f, .13f, 1));
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", 0); m.SetFloat("_ZTest", 4); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.renderQueue = 3000;
            m.SetTexture("_VertexOffset_Map", map);
            m.SetTextureScale("_VertexOffset_Map", new Vector2(.79f, 1.17f));
            m.SetTextureOffset("_VertexOffset_Map", new Vector2(.13f, -.09f));
            m.SetTexture("_VertexOffset_MaskMap", mask);
            m.SetTextureScale("_VertexOffset_MaskMap", new Vector2(1.19f, .72f));
            m.SetTextureOffset("_VertexOffset_MaskMap", new Vector2(-.08f, .15f));
            m.SetVector("_VertexOffset_Vec", new Vector4(0, 0, .43f, 0));
            m.SetVector("_VertexOffset_MaskMap_Vec", new Vector4(0, 0, .9f, 0));
            m.SetVector("_VertexOffset_CustomDir", new Vector4(.9f, .1f, 0, 0));
            m.SetFloat("_VertexOffset_Toggle", 1);
            if (graph)
            {
                m.SetFloat("_Surface", 1); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                foreach (string property in new[] { "_VertexOffset_Map", "_VertexOffset_MaskMap",
                    "_VertexOffset_Vec", "_VertexOffset_MaskMap_Vec", "_VertexOffset_Toggle",
                    "_VertexOffset_Mask_Toggle", "_NB_ColorChannelHi16" })
                    Assert.That(m.HasProperty(property), Is.True, "Graph missing " + property);
            }
            else
            {
                m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                m.EnableKeyword("_VERTEX_OFFSET");
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false);
                m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            }
            foreach(string pass in new[]{"DepthOnly","ShadowCaster","NBCameraOpaqueDistortPass","NBDeferredDistortPass","Universal2D"})m.SetShaderPassEnabled(pass,false);
        }

        static void SetProtocol(Material graph, Material legacy, OffsetCase c)
        {
            uint modeBits = ((uint)c.mapUVMode & 3u) << 20;
            modeBits |= ((uint)c.maskUVMode & 3u) << 22;
            modeBits |= ((uint)c.sharedUVMode & 3u) << 30;
            uint typeBits = ((uint)c.mapUVMode >> 2) << 20;
            typeBits |= ((uint)c.maskUVMode >> 2) << 22;
            typeBits |= ((uint)c.sharedUVMode >> 2) << 30;
            uint channels = 3u | ((uint)c.mapChannel << 16) | ((uint)c.maskChannel << 18);
            SetWord(graph, "_NB_Flags0Lo16", "_NB_Flags0Hi16", c.flags0);
            SetWord(graph, "_NB_Flags1Lo16", "_NB_Flags1Hi16", c.flags1);
            SetWord(graph, "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", modeBits);
            SetWord(graph, "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", typeBits);
            SetWord(graph, "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", c.wrapFlags);
            SetWord(graph, "_NB_ColorChannelLo16", "_NB_ColorChannelHi16", channels);
            graph.SetVector("_SharedUV_ST", c.sharedST); graph.SetVector("_SharedUV_Vec", c.sharedVec);
            graph.SetVector("_TWParameter", c.twirl); graph.SetFloat("_TWStrength", c.twirlStrength);
            graph.SetVector("_PCCenter", c.polar);
            graph.SetFloat("_VertexOffset_NormalDir_Toggle", c.directionMode);
            graph.SetFloat("_VertexOffset_DirectionSpace", c.worldDirection ? 1 : 0);
            graph.SetFloat("_VertexOffset_Mask_Toggle", c.mask ? 1 : 0);
            legacy.SetInteger("_W9ParticleShaderFlags", unchecked((int)c.flags0));
            legacy.SetInteger("_W9ParticleShaderFlags1", unchecked((int)c.flags1));
            legacy.SetInteger("_UVModeFlag0", unchecked((int)modeBits));
            legacy.SetInteger("_UVModeFlagType0", unchecked((int)typeBits));
            legacy.SetInteger("_W9ParticleShaderWrapFlags", unchecked((int)c.wrapFlags));
            legacy.SetInteger("_W9ParticleShaderColorChannelFlag", unchecked((int)channels));
            legacy.SetVector("_SharedUV_ST", c.sharedST); legacy.SetVector("_SharedUV_Vec", c.sharedVec);
            legacy.SetVector("_TWParameter", c.twirl); legacy.SetFloat("_TWStrength", c.twirlStrength);
            legacy.SetVector("_PCCenter", c.polar);
            legacy.SetFloat("_VertexOffset_NormalDir_Toggle", c.directionMode);
            legacy.SetFloat("_VertexOffset_DirectionSpace", c.worldDirection ? 1 : 0);
            if (c.mask) legacy.EnableKeyword("_VERTEX_OFFSET_MASKMAP");
            else legacy.DisableKeyword("_VERTEX_OFFSET_MASKMAP");
        }

        static void SetWord(Material m, string lo, string hi, uint value)
        { m.SetFloat(lo, value & 65535u); m.SetFloat(hi, value >> 16); }

        static Color[] Capture(Camera camera, RenderTexture target, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
                Color[] pixels = readback.GetPixels();
                if (path != null)
                {
                    using (var file = File.Create(path + ".rgba-f32.gz"))
                    using (var gzip = new GZipStream(file, CompressionMode.Compress))
                    using (var writer = new BinaryWriter(gzip))
                        foreach (Color color in pixels)
                        { writer.Write(color.r); writer.Write(color.g); writer.Write(color.b); writer.Write(color.a); }
                    var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                    try { png.SetPixels(pixels); png.Apply(false); File.WriteAllBytes(path + ".png", png.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(png); }
                }
                return pixels;
            }
            finally { RenderTexture.active = previous; }
        }

        static Metrics Compare(Color[] b, Color[] c)
        {
            var metrics = new Metrics { finite = true };
            for (int i = 0; i < b.Length; i++)
            {
                metrics.pixels++;
                float difference = 0;
                for (int channel = 0; channel < 4; channel++)
                {
                    float bv = b[i][channel], cv = c[i][channel];
                    if (float.IsNaN(bv) || float.IsInfinity(bv) || float.IsNaN(cv) || float.IsInfinity(cv))
                        metrics.finite = false;
                    difference = Mathf.Max(difference, Mathf.Abs(bv - cv));
                }
                if (b[i].r > .3f) metrics.legacyVisible++;
                if (c[i].r > .3f) metrics.graphVisible++;
                if (difference > 0) metrics.differentRGBA++;
                metrics.maxRGBA = Mathf.Max(metrics.maxRGBA, difference);
            }
            return metrics;
        }

        static int ChangedPixels(Color[] a, Color[] b) => Compare(a, b).differentRGBA;
    }
}
