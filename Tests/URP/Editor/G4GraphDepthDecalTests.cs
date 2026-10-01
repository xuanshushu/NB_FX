using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// Ordinary Mesh only: real opaque depth writer beneath a transparent
    /// cube proxy. Frozen A/current B/Graph C are compared on actual camera
    /// RT for Forward and both exact NB distortion lists. No VFX/Player claim.
    /// </summary>
    public sealed class G4GraphDepthDecalTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string Deferred = "NBDeferredDistortPass", Opaque = "NBCameraOpaqueDistortPass";
        const int Size = 128, FloorLayer = 4, DirectedLayer = G4GraphScreenNoiseTests.ForegroundLayer;
        const int RoiMin = 28, RoiMax = 100;

        [OneTimeSetUp]
        public void ForceGraphImportAfterReload() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (bool ortho in new[] { true, false })
            {
                foreach (float proxyY in new[] { 0f, .25f })
                    yield return new TestCaseData("Forward", ortho, proxyY).SetName(
                        $"G4DepthDecal_Forward_{(ortho ? "ortho" : "perspective")}_y{proxyY}");
                foreach (string pass in new[] { Deferred, Opaque })
                    yield return new TestCaseData(pass, ortho, .25f).SetName(
                        $"G4DepthDecal_{pass}_{(ortho ? "ortho" : "perspective")}");
            }
        }

        [Serializable]
        sealed class Metrics
        {
            public string route, api, unityVersion, note;
            public bool orthographic, finite, depthTextureRequested;
            public float proxyCenterY, abOff, abOn, bcOff, bcOn,
                abFloorShift, bcFloorShift, abOutside, bcOutside,
                legacyRepeat, graphRepeat, frozenResponse, currentResponse,
                graphResponse, floorDepthResponse, outsideResponse,
                frozenShiftResponse, currentShiftResponse, graphShiftResponse,
                depthOnlyCausalResponse;
            public int abOnDifferentRGBA, bcOnDifferentRGBA,
                currentVisible, graphVisible, distinctFloorColors;
        }

        [TestCaseSource(nameof(Cases))]
        public void ProjectedMeshDecalMatchesFrozenAndGraph(string route, bool ortho, float proxyY)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader graphShader = Load(GraphPath), currentShader = Load(CurrentPath), frozenShader = Load(FrozenPath);
            var rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0];
            Assert.That(rendererData, Is.Not.Null);
            var urd = rendererData as UniversalRendererData;
            Assert.That(urd, Is.Not.Null);
            Assert.That(urd.opaqueLayerMask.value & (1 << FloorLayer), Is.Not.Zero);
            if (route == "Forward") Assert.That(urd.transparentLayerMask.value & (1 << FloorLayer), Is.Not.Zero);
            ScriptableRendererFeature nbFeature = FindNBPostProcess(rendererData);
            Assert.That(nbFeature, Is.Not.Null);
            bool nbWasActive = nbFeature.isActive;
            string rendererAsset = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                AssetDatabase.GetAssetPath(rendererData));
            byte[] rendererBefore = File.ReadAllBytes(rendererAsset);
            string evidence = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(evidence))
                evidence = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4DepthDecal");
            string folder = Path.Combine(evidence, route + (ortho ? "-ortho" : "-perspective") + "-y" + proxyY);
            Directory.CreateDirectory(folder);

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject projector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject cameraObject = new GameObject("DepthDecal Mesh camera");
            Material a = new Material(frozenShader), b = new Material(currentShader), c = new Material(graphShader);
            Shader floorShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(floorShader, Is.Not.Null);
            Material floorMaterial = new Material(floorShader);
            Texture2D gradient = Gradient(), noise = Constant(new Color(.65f, .35f, 0, 1));
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            Camera camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            RenderTexture previousActive = RenderTexture.active;
            G4ScreenNoiseDirectedFeature directed = null;
            try
            {
                foreach (GameObject go in new[] { floor, projector, cameraObject })
                    SceneManager.MoveGameObjectToScene(go, scene);
                floor.layer = FloorLayer;
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(.3f, 1, .3f);
                floorMaterial.SetTexture("_BaseMap", gradient);
                floorMaterial.SetColor("_BaseColor", new Color(.6f, .65f, .7f, 1));
                floorMaterial.SetFloat("_Surface", 0);
                floorMaterial.SetFloat("_ZWrite", 1);
                floorMaterial.SetFloat("_Cull", 0);
                floorMaterial.renderQueue = 2000;
                var floorRenderer = floor.GetComponent<MeshRenderer>();
                floorRenderer.sharedMaterial = floorMaterial;
                floorRenderer.shadowCastingMode = ShadowCastingMode.Off;
                projector.layer = route == "Forward" ? FloorLayer : DirectedLayer;
                projector.transform.position = new Vector3(.11f, proxyY, -.07f);
                projector.transform.rotation = Quaternion.Euler(0, 23, 0);
                projector.transform.localScale = Vector3.one;
                var projectorRenderer = projector.GetComponent<MeshRenderer>();
                projectorRenderer.shadowCastingMode = ShadowCastingMode.Off;
                projectorRenderer.receiveShadows = false;
                foreach (Material hostMaterial in new[] { a, b, c }) Configure(hostMaterial, hostMaterial == c, route, gradient, noise);

                camera.scene = scene;
                camera.orthographic = ortho;
                camera.orthographicSize = 1.25f;
                camera.fieldOfView = 44;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.03f, .06f, .09f, 1);
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.cullingMask = (1 << FloorLayer) | (1 << DirectedLayer);
                camera.transform.position = new Vector3(.28f, 1.8f, 2.6f);
                camera.transform.LookAt(new Vector3(.05f, .05f, 0));
                camera.targetTexture = target;
                cameraData.requiresDepthTexture = true;
                cameraData.requiresColorTexture = true;
                cameraData.renderPostProcessing = false;
                target.Create();
                Assert.That(target.IsCreated() && !target.sRGB, Is.True);

                nbFeature.SetActive(false);
                if (route != "Forward")
                {
                    directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();
                    directed.hideFlags = HideFlags.HideAndDontSave;
                    directed.targetCamera = camera;
                    directed.selectedPass = route;
                    directed.Create(); directed.SetActive(true);
                    rendererData.rendererFeatures.Add(directed); rendererData.SetDirty();
                }
                projectorRenderer.enabled = false;
                camera.Render();
                Color[] floorOnly = Capture(camera, target, readback, Path.Combine(folder, "floor-only"));
                projectorRenderer.enabled = true;
                projectorRenderer.sharedMaterial = c;
                Capture(camera, target, readback, Path.Combine(folder, "C-warm"));
                bool hasMetadata = false;
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(GraphPath))
                    if (asset && asset.GetType().FullName ==
                        "UnityEditor.Rendering.Universal.ShaderGraph.UniversalMetadata") hasMetadata = true;
                Assert.That(hasMetadata, Is.True, "URP Graph importer metadata absent after real Graph warm draw.");
                Assert.That(c.HasProperty("_DepthDecal_Toggle"), Is.True,
                    "Graph DepthDecal property absent after real Graph warm draw.");

                SetDecal(a, false, false); SetDecal(b, false, false); SetDecal(c, true, false);
                Color[] aOff = Draw(projectorRenderer, a, camera, target, readback, folder, "A-off");
                Color[] bOff = Draw(projectorRenderer, b, camera, target, readback, folder, "B-off");
                Color[] cOff = Draw(projectorRenderer, c, camera, target, readback, folder, "C-off");
                SetDecal(a, false, true); SetDecal(b, false, true); SetDecal(c, true, true);
                Color[] aOn = Draw(projectorRenderer, a, camera, target, readback, folder, "A-on");
                Color[] bOn = Draw(projectorRenderer, b, camera, target, readback, folder, "B-on");
                Color[] cOn = Draw(projectorRenderer, c, camera, target, readback, folder, "C-on");
                Color[] bRepeat = Draw(projectorRenderer, b, camera, target, readback, folder, "B-repeat");
                Color[] cRepeat = Draw(projectorRenderer, c, camera, target, readback, folder, "C-repeat");

                // Physical-depth control: the depth writer moves while the
                // projector and all material properties remain fixed.
                floor.transform.position = new Vector3(0, .32f, 0);
                Color[] aShift = Draw(projectorRenderer, a, camera, target, readback, folder, "A-floor-shift");
                Color[] bShift = Draw(projectorRenderer, b, camera, target, readback, folder, "B-floor-shift");
                Color[] cShift = Draw(projectorRenderer, c, camera, target, readback, folder, "C-floor-shift");
                SetDecal(a, false, false); SetDecal(b, false, false); SetDecal(c, true, false);
                Color[] aShiftOff = Draw(projectorRenderer, a, camera, target, readback, folder, "A-floor-shift-off");
                Color[] bShiftOff = Draw(projectorRenderer, b, camera, target, readback, folder, "B-floor-shift-off");
                Color[] cShiftOff = Draw(projectorRenderer, c, camera, target, readback, folder, "C-floor-shift-off");
                SetDecal(a, false, true); SetDecal(b, false, true); SetDecal(c, true, true);
                floor.transform.position = Vector3.zero;

                // Cube-volume negative control. The reconstructed floor point
                // is outside the projector's ±.5 object-space Y bound.
                projector.transform.position = new Vector3(.11f, .72f, -.07f);
                Color[] aOutside = Draw(projectorRenderer, a, camera, target, readback, folder, "A-outside");
                Color[] bOutside = Draw(projectorRenderer, b, camera, target, readback, folder, "B-outside");
                Color[] cOutside = Draw(projectorRenderer, c, camera, target, readback, folder, "C-outside");

                var m = new Metrics
                {
                    route = route, api = SystemInfo.graphicsDeviceType.ToString(),
                    unityVersion = Application.unityVersion,
                    orthographic = ortho, proxyCenterY = proxyY,
                    depthTextureRequested = cameraData.requiresDepthTexture,
                    note = "Ordinary Mesh only; Frozen A/current B/Graph C. Opaque depth-writing tilted-view floor and cube projector; exact NB tags use directed RendererList. Full-frame RGBAHalf raw, repeat, no first-frame/VFX/Player claim.",
                    finite = Finite(floorOnly, aOff, bOff, cOff, aOn, bOn, cOn,
                        bRepeat, cRepeat, aShift, bShift, cShift,
                        aShiftOff, bShiftOff, cShiftOff, aOutside, bOutside, cOutside),
                    abOff = Delta(aOff, bOff), abOn = Delta(aOn, bOn),
                    bcOff = Delta(bOff, cOff), bcOn = Delta(bOn, cOn),
                    abFloorShift = Delta(aShift, bShift), bcFloorShift = Delta(bShift, cShift),
                    abOutside = Delta(aOutside, bOutside), bcOutside = Delta(bOutside, cOutside),
                    abOnDifferentRGBA = CountDifferent(aOn, bOn),
                    bcOnDifferentRGBA = CountDifferent(bOn, cOn),
                    legacyRepeat = FrameDelta(bOn, bRepeat), graphRepeat = FrameDelta(cOn, cRepeat),
                    frozenResponse = Delta(aOff, aOn), currentResponse = Delta(bOff, bOn),
                    graphResponse = Delta(cOff, cOn),
                    floorDepthResponse = Delta(bOn, bShift),
                    frozenShiftResponse = Delta(aShift, aShiftOff),
                    currentShiftResponse = Delta(bShift, bShiftOff),
                    graphShiftResponse = Delta(cShift, cShiftOff),
                    depthOnlyCausalResponse = DeltaOfDeltas(bOn, bOff, bShift, bShiftOff),
                    outsideResponse = Delta(bOn, bOutside),
                    currentVisible = CountVisible(bOn, floorOnly),
                    graphVisible = CountVisible(cOn, floorOnly),
                    distinctFloorColors = CountDistinctRGB(floorOnly),
                };
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(m, true));
                Debug.Log("NBFX_G4_DEPTH_DECAL " + JsonUtility.ToJson(m));
                Assert.That(m.finite && m.depthTextureRequested, Is.True);
                Assert.That(m.currentVisible, Is.GreaterThan(100));
                Assert.That(m.graphVisible, Is.GreaterThan(100));
                Assert.That(m.distinctFloorColors, Is.GreaterThan(32),
                    "The opaque depth writer must also be a nonuniform visible geometry control.");
                Assert.That(m.legacyRepeat, Is.Zero); Assert.That(m.graphRepeat, Is.Zero);
                Assert.That(m.abOff, Is.Zero); Assert.That(m.abOn, Is.Zero);
                Assert.That(m.bcOff, Is.Zero); Assert.That(m.bcOn, Is.Zero);
                Assert.That(m.abFloorShift, Is.Zero); Assert.That(m.bcFloorShift, Is.Zero);
                Assert.That(Delta(aShiftOff, bShiftOff), Is.Zero);
                Assert.That(Delta(bShiftOff, cShiftOff), Is.Zero);
                Assert.That(m.abOutside, Is.Zero); Assert.That(m.bcOutside, Is.Zero);
                Assert.That(m.currentResponse, Is.GreaterThan(.005f));
                Assert.That(m.graphResponse, Is.GreaterThan(.005f));
                Assert.That(m.frozenResponse, Is.GreaterThan(.005f));
                Assert.That(m.floorDepthResponse, Is.GreaterThan(.005f));
                Assert.That(m.frozenShiftResponse, Is.GreaterThan(.005f));
                Assert.That(m.currentShiftResponse, Is.GreaterThan(.005f));
                Assert.That(m.graphShiftResponse, Is.GreaterThan(.005f));
                Assert.That(m.depthOnlyCausalResponse, Is.GreaterThan(.005f),
                    "Changing depth changed only background color, not the decal contribution.");
                Assert.That(m.outsideResponse, Is.GreaterThan(.005f));
            }
            finally
            {
                if (directed != null)
                { rendererData.rendererFeatures.Remove(directed); UnityEngine.Object.DestroyImmediate(directed); }
                nbFeature.SetActive(nbWasActive); rendererData.SetDirty();
                camera.targetTexture = null; RenderTexture.active = previousActive; target.Release();
                foreach (UnityEngine.Object obj in new UnityEngine.Object[] { floor, projector, cameraObject,
                    a, b, c, floorMaterial, gradient, noise, target, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererAsset), Is.EqualTo(rendererBefore),
                    "Temporary directed feature changed renderer asset on disk.");
            }
        }

        static Shader Load(string path)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader && shader.isSupported, Is.True, path);
            return shader;
        }
        static ScriptableRendererFeature FindNBPostProcess(ScriptableRendererData data)
        {
            foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess") return feature;
            return null;
        }
        static void Configure(Material material, bool graph, string route,
            Texture2D baseMap, Texture2D noise)
        {
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_NoiseMap", noise);
            material.SetColor(graph ? "_Color" : "_BaseColor", new Color(.95f, .8f, .7f, 1));
            material.SetColor("_ColorA", Color.white);
            material.SetFloat("_AlphaAll", 1);
            material.SetFloat("_BaseColorIntensityForTimeline", 1);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_TexDistortion_intensity", 0);
            material.SetFloat("_fogintensity", 0);
            material.SetFloat("_noisemapEnabled", route == "Forward" ? 0 : 1);
            material.SetFloat("_NoiseIntensity", .5f);
            material.SetVector("_DistortionDirection", new Vector4(.55f, .35f, 0, 0));
            if (graph)
            {
                material.SetFloat("_Surface", 1);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                // ShaderLab Forward uses one Blend[_SrcBlend][_DstBlend] for
                // RGBA; Graph has separate alpha controls, so match both.
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                material.SetFloat("_NB_DistortionMode", route == Deferred ? 1 : route == Opaque ? 2 : 0);
                material.SetFloat("_NB_DistortionIntensity", 2);
                material.SetFloat("_NB_DistortionAlphaPow", 1);
                material.SetFloat("_NB_DistortionAlphaMultiplier", 1);
                material.SetFloat("_NB_DistortionAlphaAdd", 0);
                SetWord(material, "_NB_Flags0Lo16", "_NB_Flags0Hi16", 0);
                SetWord(material, "_NB_Flags1Lo16", "_NB_Flags1Hi16", 1u << 9);
                material.SetFloat("_NB_ColorChannelLo16", 3);
            }
            else
            {
                material.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                if (route != "Forward") material.EnableKeyword("_NOISEMAP");
                material.SetInteger("_W9ParticleShaderFlags", 0);
                material.SetInteger("_W9ParticleShaderFlags1", 1 << 9);
                material.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                material.SetFloat("_ColorMask", 15);
                material.SetFloat("_ScreenDistortIntensity", 2);
            }
            material.renderQueue = 3000;
            foreach (string pass in new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward",
                "DepthOnly", "ShadowCaster", "Universal2D", Deferred, Opaque })
                material.SetShaderPassEnabled(pass, false);
            if (route == "Forward")
            {
                material.SetShaderPassEnabled(graph ? "SRPDefaultUnlit" : "UniversalForward", true);
                if (graph) material.SetShaderPassEnabled("SRPDEFAULTUNLIT", true);
            }
            else material.SetShaderPassEnabled(route, true);
        }
        static void SetDecal(Material material, bool graph, bool enabled)
        {
            material.SetFloat("_DepthDecal_Toggle", enabled ? 1 : 0);
            if (!graph)
            {
                if (enabled) material.EnableKeyword("_DEPTH_DECAL");
                else material.DisableKeyword("_DEPTH_DECAL");
            }
        }
        static void SetWord(Material m, string lo, string hi, uint value)
        { m.SetFloat(lo, value & 65535u); m.SetFloat(hi, value >> 16); }
        static Color[] Draw(MeshRenderer renderer, Material material, Camera camera,
            RenderTexture rt, Texture2D readback, string folder, string name)
        {
            renderer.sharedMaterial = material;
            return Capture(camera, rt, readback, Path.Combine(folder, name));
        }
        static Texture2D Constant(Color value)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            tex.SetPixel(0, 0, value); tex.Apply(false);
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }
        static Texture2D Gradient()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                pixels[y * n + x] = new Color(.1f + 1.1f * x / (n - 1f),
                    .08f + 1.05f * y / (n - 1f), .13f + .4f * (x + y) / (2f * n - 2f), 1);
            tex.SetPixels(pixels); tex.Apply(false);
            tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
                Color[] result = readback.GetPixels();
                using (var file = File.Create(path + ".rgba-f32.gz"))
                using (var zip = new GZipStream(file, CompressionMode.Compress))
                using (var writer = new BinaryWriter(zip))
                    foreach (Color color in result)
                    { writer.Write(color.r); writer.Write(color.g); writer.Write(color.b); writer.Write(color.a); }
                var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                try { png.SetPixels(result); png.Apply(false); File.WriteAllBytes(path + ".png", png.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(png); }
                return result;
            }
            finally { RenderTexture.active = previous; }
        }
        static bool Finite(params Color[][] frames)
        {
            foreach (Color[] frame in frames) foreach (Color color in frame)
                for (int channel = 0; channel < 4; channel++)
                    if (float.IsNaN(color[channel]) || float.IsInfinity(color[channel])) return false;
            return true;
        }
        static float FrameDelta(Color[] a, Color[] b)
        {
            float max = 0;
            for (int i = 0; i < a.Length; i++) for (int c = 0; c < 4; c++)
                max = Mathf.Max(max, Mathf.Abs(a[i][c] - b[i][c]));
            return max;
        }
        static float Delta(Color[] a, Color[] b)
        {
            float max = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
                for (int channel = 0; channel < 4; channel++)
                    max = Mathf.Max(max, Mathf.Abs(a[y * Size + x][channel] - b[y * Size + x][channel]));
            return max;
        }
        static float DeltaOfDeltas(Color[] on0, Color[] off0, Color[] on1, Color[] off1)
        {
            float max = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x;
                for (int channel = 0; channel < 4; channel++)
                    max = Mathf.Max(max, Mathf.Abs((on0[i][channel] - off0[i][channel]) -
                        (on1[i][channel] - off1[i][channel])));
            }
            return max;
        }
        static int CountDistinctRGB(Color[] image)
        {
            var values = new HashSet<int>();
            foreach (Color color in image)
                values.Add((Mathf.RoundToInt(color.r * 512) << 20) ^
                    (Mathf.RoundToInt(color.g * 512) << 10) ^ Mathf.RoundToInt(color.b * 512));
            return values.Count;
        }
        static int CountDifferent(Color[] a, Color[] b)
        {
            int count = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x;
                for (int c = 0; c < 4; c++) if (a[i][c] != b[i][c]) { count++; break; }
            }
            return count;
        }
        static int CountVisible(Color[] image, Color[] floorOnly)
        {
            int count = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x;
                if (Mathf.Abs(image[i].r - floorOnly[i].r) +
                    Mathf.Abs(image[i].g - floorOnly[i].g) +
                    Mathf.Abs(image[i].b - floorOnly[i].b) > .01f) count++;
            }
            return count;
        }
    }
}
