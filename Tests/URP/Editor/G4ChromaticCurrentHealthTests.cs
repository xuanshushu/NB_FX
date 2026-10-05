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
    // Independent Current-only diagnostic. No Frozen/Graph instances, forced
    // import, GetShaderText, metadata enumeration, or private SRP query.
    public sealed class G4ChromaticCurrentHealthTests
    {
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        const int Size = 128;
        [Serializable] sealed class Source { public string path, sha256; }
        [Serializable] sealed class Pass { public string name; public bool enabled; }
        [Serializable] sealed class Receipt
        {
            public string scope, stage, exception, cleanupError, unity, api, gpu, shaderAssetPath,
                shaderName, actualRendererShader, rendererAssetPath;
            public string[] keywords, shaderErrors;
            public Source[] sources;
            public Pass[] passes;
            public bool orthographic, actualRendererMaterialMatches, finite, nonCDCD,
                cleanupComplete, rendererDiskRestored, noFrozenOrGraphMaterial;
            public int rawCount, visibleOn, visibleOff, responseChanged, withNoiseChanged,
                actorChanged, deniedChanged, restoredChanged, keywordRestoreChanged;
            public float caRawToggle, repeat, response,
                withNoise, actorResponse, restored, deniedError, keywordRestoreError;
            public int flagsWord0;
            public DrawState[] draws;
        }
        [Serializable] sealed class DrawState
        {
            public string label, actualShaderAssetPath;
            public string[] keywords, shaderErrors, shaderErrorsAfter;
            public Pass[] passes;
            public int flagsWord0, flagsWord1, customDataWord0;
            public float caRawToggle, withNoiseRawToggle;
            public Vector4 direction;
            public bool rendererEnabled, materialMatches, supported;
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

        [Test] public void G4CACurrentHealth_Forward_withNoise_ortho()
        {
            string evidence = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(evidence)) evidence = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXCACurrentHealth");
            string folder = Path.Combine(evidence, "ca-current-health", "Forward-with-noise-ortho");
            Directory.CreateDirectory(folder);
            var receipt = new Receipt { scope = "Current-only Forward/ortho; not ABC parity, Native SRP compatibility, other passes, Player, or old CA incident closure",
                stage = "preflight", unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(),
                gpu = SystemInfo.graphicsDeviceName, orthographic = true, noFrozenOrGraphMaterial = true };
            void Save() => File.WriteAllText(Path.Combine(folder, "receipt.json"), JsonUtility.ToJson(receipt, true));
            Save();
            var owned = new List<Object>(); Scene scene = default;
            Camera camera = null; RenderTexture rt = null;
            RenderTexture previous = RenderTexture.active;
            ScriptableRendererFeature nbFeature = null; ScriptableRendererData rendererData = null;
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
                Shader currentShader = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath);
                Assert.That(currentShader && currentShader.isSupported, Is.True);
                receipt.shaderAssetPath = AssetDatabase.GetAssetPath(currentShader); receipt.shaderName = currentShader.name;
                string expectedGuid = AssetDatabase.AssetPathToGUID(CurrentPath);
                Assert.That(expectedGuid, Is.Not.Empty); Assert.That(AssetDatabase.AssetPathToGUID(receipt.shaderAssetPath), Is.EqualTo(expectedGuid));
                string packageRoot = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Packages/NB_FX");
                receipt.sources = new[] { Hash(packageRoot, "NBShaders2/Shader/NBShader.shader"),
                    Hash(packageRoot, "NBShaders2/Shader/HLSL/NBShaderInput.hlsl"),
                    Hash(packageRoot, "NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl"),
                    Hash(packageRoot, "NBShaders2/Shader/HLSL/NBShaderChromaticV1.hlsl"),
                    Hash(packageRoot, "Tests/URP/Editor/G4GraphChromaticTests.cs") };
                receipt.shaderErrors = ShaderUtil.GetShaderMessages(currentShader).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
                Save(); Assert.That(receipt.shaderErrors, Is.Empty, "Current-only preflight shader errors; no first Native draw if errors exist.");
                scene = EditorSceneManager.NewPreviewScene();
                GameObject backdropObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                GameObject actor = new GameObject("CA Current-only ordinary Mesh", typeof(MeshFilter), typeof(MeshRenderer));
                GameObject cameraObject = new GameObject("CA Current-only camera");
                foreach (GameObject go in new[] { backdropObject, actor, cameraObject }) SceneManager.MoveGameObjectToScene(go, scene);
                Mesh mesh = (Mesh)Original("BuildMesh"); owned.Add(mesh);
                Texture2D baseMap = (Texture2D)Original("MakeBaseMap", false), height = (Texture2D)Original("MakeHeight"),
                    noise = (Texture2D)Original("MakeNoise"), backgroundMap = (Texture2D)Original("MakeBackdrop");
                owned.AddRange(new Object[] { baseMap, height, noise, backgroundMap });
                Material current = new Material(currentShader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(current);
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit"); Assert.That(unlit, Is.Not.Null);
                Material backdrop = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(backdrop);
                backdropObject.layer = 2; backdropObject.transform.position = new Vector3(0, 0, 1);
                backdropObject.transform.localScale = new Vector3(6, 6, 1);
                backdrop.SetTexture("_BaseMap", backgroundMap); backdrop.SetColor("_BaseColor", Color.white);
                backdrop.SetFloat("_Cull", 0); backdrop.renderQueue = 2000;
                backdropObject.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                actor.layer = 4; actor.transform.position = new Vector3(0, 0, 2);
                actor.transform.rotation = Quaternion.Euler(0, 27, 0); actor.transform.localScale = new Vector3(2, 2, 1);
                actor.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = actor.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                camera = cameraObject.AddComponent<Camera>(); var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                camera.scene = scene; camera.orthographic = true; camera.orthographicSize = 1.5f; camera.fieldOfView = 53.13f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.09f, .15f, .22f, .35f); camera.allowHDR = true; camera.allowMSAA = false;
                camera.cullingMask = (1 << 2) | (1 << 4); camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0); cameraData.requiresColorTexture = true; cameraData.renderPostProcessing = false;
                rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear); owned.Add(rt);
                var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true); owned.Add(readback);
                camera.targetTexture = rt; rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                nbFeature.SetActive(false);
                Original("Configure", current, false, "Forward", "with-noise", baseMap, height, noise);
                Original("Apply", current, false, "with-noise", true, true, false, -1);
                receipt.caRawToggle = current.GetFloat("_Distortion_Choraticaberrat_Toggle");
                receipt.flagsWord0 = current.GetInteger("_W9ParticleShaderFlags");
                receipt.keywords = current.shaderKeywords.OrderBy(k => k).ToArray();
                Assert.That(receipt.caRawToggle, Is.EqualTo(1));
                Assert.That(current.IsKeywordEnabled("_CHROMATIC_ABERRATION"), Is.True);
                Assert.That(current.IsKeywordEnabled("_NOISEMAP"), Is.True);
                foreach (string name in new[] { "_Distortion_Choraticaberrat_Toggle", "_Distortion_Choraticaberrat_WithNoise_Toggle", "_DistortionDirection", "_W9ParticleShaderFlags", "_W9ParticleShaderFlags1", "_W9ParticleCustomDataFlag0" })
                    Assert.That(current.HasProperty(name), Is.True, "Required actual native property: " + name);
                receipt.passes = new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward", "DepthOnly", "ShadowCaster", "NBDeferredDistortPass", "NBCameraOpaqueDistortPass" }
                    .Select(p => new Pass { name = p, enabled = current.GetShaderPassEnabled(p) }).ToArray();
                renderer.sharedMaterial = current; receipt.actualRendererMaterialMatches = renderer.sharedMaterial == current;
                receipt.actualRendererShader = renderer.sharedMaterial.shader.name; Assert.That(receipt.actualRendererMaterialMatches, Is.True);
                var frames = new List<Color[]>(); var draws = new List<DrawState>();
                Color[] Capture(string label)
                {
                    var state = new DrawState { label = label, rendererEnabled = renderer.enabled,
                        materialMatches = renderer.sharedMaterial == current, actualShaderAssetPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.shader),
                        supported = current.shader.isSupported, keywords = current.shaderKeywords.OrderBy(k => k).ToArray(),
                        shaderErrors = ShaderUtil.GetShaderMessages(current.shader).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray(),
                        flagsWord0 = current.GetInteger("_W9ParticleShaderFlags"), flagsWord1 = current.GetInteger("_W9ParticleShaderFlags1"),
                        customDataWord0 = current.GetInteger("_W9ParticleCustomDataFlag0"),
                        caRawToggle = current.GetFloat("_Distortion_Choraticaberrat_Toggle"), withNoiseRawToggle = current.GetFloat("_Distortion_Choraticaberrat_WithNoise_Toggle"),
                        direction = current.GetVector("_DistortionDirection"), passes = receipt.passes.Select(p => new Pass { name = p.name, enabled = current.GetShaderPassEnabled(p.name) }).ToArray() };
                    draws.Add(state); receipt.draws = draws.ToArray(); receipt.stage = "before-draw:" + label; Save();
                    Assert.That(state.supported && state.materialMatches, Is.True); Assert.That(state.shaderErrors, Is.Empty);

                    Color[] pixels = (Color[])Original("Capture", camera, rt, readback, Path.Combine(folder, label));
                    frames.Add(pixels); receipt.rawCount++;
                    state.shaderErrorsAfter = ShaderUtil.GetShaderMessages(current.shader).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
                    receipt.stage = "after-draw:" + label; Save(); Assert.That(state.shaderErrorsAfter, Is.Empty); return pixels;
                }
                Color[] Snap(string label, bool enabled, bool withNoise)
                { Original("Apply", current, false, "with-noise", enabled, withNoise, false, -1); renderer.sharedMaterial = current; return Capture(label); }
                renderer.enabled = false; Color[] background = Capture("background"); renderer.enabled = true;
                Capture("B-warm"); Color[] on = Snap("B-on", true, true); Color[] repeated = Snap("B-repeat", true, true);
                Color[] off = Snap("B-off", false, true); Color[] flipped = Snap("B-withnoise-flipped", true, false);
                Color[] restored = Snap("B-restored", true, true);
                // Existing Native keyword is the compiled consumer authority; raw intent stays on.
                Original("Apply", current, false, "with-noise", true, true, false, -1);
                current.DisableKeyword("_CHROMATIC_ABERRATION");
                Assert.That(current.GetFloat("_Distortion_Choraticaberrat_Toggle"), Is.EqualTo(1));
                Color[] denied = Capture("B-keyword-denied");
                current.EnableKeyword("_CHROMATIC_ABERRATION"); Color[] keywordRestored = Capture("B-keyword-restored");
                receipt.finite = (bool)Original("Finite", (object)frames.ToArray());
                receipt.nonCDCD = frames.All(f => f.All(c => Enumerable.Range(0, 4).All(i => c[i] != -23.203125f)));
                receipt.visibleOn = (int)Original("Visible", on); receipt.visibleOff = (int)Original("Visible", off);
                receipt.repeat = Max(on, repeated); receipt.response = Max(on, off); receipt.responseChanged = Changed(on, off);
                receipt.withNoise = Max(on, flipped); receipt.withNoiseChanged = Changed(on, flipped);
                receipt.actorResponse = Max(on, background); receipt.actorChanged = Changed(on, background);
                receipt.restored = Max(on, restored); receipt.restoredChanged = Changed(on, restored);
                receipt.deniedError = Max(off, denied); receipt.deniedChanged = Changed(off, denied);
                receipt.keywordRestoreError = Max(on, keywordRestored); receipt.keywordRestoreChanged = Changed(on, keywordRestored);
                receipt.stage = "assertions"; Save();
                Assert.That(receipt.finite && receipt.nonCDCD, Is.True); Assert.That(receipt.rawCount, Is.EqualTo(9));
                Assert.That(receipt.visibleOn, Is.GreaterThan(100)); Assert.That(receipt.visibleOff, Is.GreaterThan(100));
                Assert.That(receipt.repeat, Is.Zero); Assert.That(receipt.restored + receipt.keywordRestoreError + receipt.deniedError, Is.Zero);
                Assert.That(receipt.restoredChanged + receipt.keywordRestoreChanged + receipt.deniedChanged, Is.Zero);
                Assert.That(receipt.response, Is.GreaterThan(.005f)); Assert.That(receipt.responseChanged, Is.GreaterThan(20));
                Assert.That(receipt.withNoise, Is.GreaterThan(.003f)); Assert.That(receipt.withNoiseChanged, Is.GreaterThan(20));
                Assert.That(receipt.actorResponse, Is.GreaterThan(.005f)); Assert.That(receipt.actorChanged, Is.GreaterThan(20));
                receipt.stage = "semantic-pass"; Save();
            }
            catch (Exception error) { receipt.exception = error.ToString(); Save(); throw; }
            finally
            {
                try
                {
                    if (nbFeature) nbFeature.SetActive(nbWasActive);
                    if (rendererData) rendererData.SetDirty();
                    if (camera) camera.targetTexture = null;
                    RenderTexture.active = previous; if (rt) rt.Release();
                    foreach (Object o in owned.AsEnumerable().Reverse()) if (o) Object.DestroyImmediate(o);
                    if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                    receipt.rendererDiskRestored = rendererBefore == null || File.ReadAllBytes(rendererPath).SequenceEqual(rendererBefore);
                    receipt.cleanupComplete = true; Save();
                    Assert.That(receipt.rendererDiskRestored, Is.True, "Current-only CA fixture changed renderer disk bytes.");
                }
                catch (Exception cleanupError) { receipt.cleanupError = cleanupError.ToString(); Save(); throw; }
            }
        }
    }
}
