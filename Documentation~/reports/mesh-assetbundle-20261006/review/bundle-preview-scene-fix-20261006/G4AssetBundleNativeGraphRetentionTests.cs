using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Counts only at this callback point. Loaded-material draws prove this
    // bounded Editor route; neither these counts nor Editor loading prove Player loading.
    public sealed class G4AssetBundleRetentionShaderObserver : IPreprocessShaders
    {
        [Serializable] public sealed class Row { public string shader, pass, stage, platform; public int observedVariants; }
        internal static bool Active;
        internal static readonly List<Row> Rows = new List<Row>();
        public int callbackOrder => int.MaxValue;
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (!Active || !shader) return;
            string path = AssetDatabase.GetAssetPath(shader);
            if (!path.EndsWith("/NBShaders2/Shader/NBShader.shader", StringComparison.Ordinal) && !path.EndsWith("/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph", StringComparison.Ordinal)) return;
            foreach (var group in data.GroupBy(v => v.shaderCompilerPlatform))
                Rows.Add(new Row { shader = shader.name, pass = snippet.passName, stage = snippet.shaderType.ToString(), platform = group.Key.ToString(), observedVariants = group.Count() });
        }
    }

    public sealed class G4AssetBundleNativeGraphRetentionTests
    {
        const string Project = "D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002";
        const string Staging = "Assets/ResTemp/EditorTemp/NBFXPlayerValidation-meshfinal1/";
        const string BundleName = "nbfx-g4-native-graph-one";
        static readonly string[] Assets = { Staging + "B-Forward-on.mat", Staging + "C-Forward-on.mat", Staging + "RuntimeAllow.asset", "Assets/ResTemp/EditorTemp/NBFXSvc-meshfinal1/NBShader_Ultra.shadervariants" };
        const BindingFlags StaticPublic = BindingFlags.Public | BindingFlags.Static;
        [Serializable] sealed class FileState { public string path, sha256, serialized; public Object asset; public bool dirty; }
        [Serializable] sealed class GlobalProfileState { public string path, shaBefore, shaAfter, jsonBefore, jsonAfter; public bool dirtyBefore, dirtyAfter; }
        [Serializable] sealed class DrawState { public string role, materialName, shaderName, materialAssetPath; public int materialInstance, shaderInstance, actualRendererMaterial; public bool actualLoadedMaterial, shaderSameAsSource; public string[] keywords; }
        [Serializable] sealed class Host
        {
            public string role; public bool finite, shaderSupported, noShaderErrors;
            public int visible; public float sourceVsLoaded, repeat, response, restore;
        }
        [Serializable] sealed class Receipt
        {
            public string status = "FAILED", error, project, unity, api, gpu, output, scope, cleanupStage;
            public string[] cleanupErrors; public GlobalProfileState[] globalProfile; public bool globalProfileExact;
            public string[] assetNames, bundleDependencies, loadedAssetTypes, graphicsAPIs;
            public bool manifestBuilt, actualBundleLoaded, sourceAssetBytesExact, sourceMaterialMemoryExact, settingsBytesExact;
            public bool scenesRestored, callbackStopped, overrideRestored, bundleUnloaded, cleanupComplete;
            public bool loadedSettingsUsed, loadedSVCWarmed;
            public int loadedSVCCount, loadedSVCMissing, observedNativeVariants, observedGraphVariants;
            public Host[] hosts; public DrawState[] draws; public G4AssetBundleRetentionShaderObserver.Row[] shaderCallbacks;
        }
        static string SHA(string path) { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        static Type ProductType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static bool Accepts(Type type, object value) => value == null || type.IsInstanceOfType(value) || (Nullable.GetUnderlyingType(type)?.IsInstanceOfType(value) ?? false);
        static object Call(string type, string method, params object[] args)
        {
            var matches = ProductType(type).GetMethods(StaticPublic).Where(m => m.Name == method && m.GetParameters().Length == args.Length)
                .Where(m => m.GetParameters().Select((p, i) => Accepts(p.ParameterType, args[i])).All(v => v)).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1), "Exact existing API overload required: " + method);
            try { return matches[0].Invoke(null, args); } catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); throw; }
        }
        static float Delta(Color[] a, Color[] b) { float max = 0; for (int i = 0; i < a.Length; ++i) { var d = a[i] - b[i]; max = Mathf.Max(max, Mathf.Abs(d.r), Mathf.Abs(d.g), Mathf.Abs(d.b), Mathf.Abs(d.a)); } return max; }
        static bool Finite(IEnumerable<Color> frames) => frames.All(p => new[] { p.r, p.g, p.b, p.a }.All(v => !float.IsNaN(v) && !float.IsInfinity(v)));
        static int Visible(Color[] a, Color[] b) { int n = 0; for (int i = 0; i < a.Length; ++i) { var d = a[i] - b[i]; if (Mathf.Max(Mathf.Abs(d.r), Mathf.Abs(d.g), Mathf.Abs(d.b)) > .001f) n++; } return n; }
        static string Scenes() => string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i => { var s = SceneManager.GetSceneAt(i); return s.path + "|" + s.name + "|" + s.isDirty + "|" + string.Join(",", s.GetRootGameObjects().Select(o => o.GetInstanceID()).OrderBy(v => v)); }));
        [Test]
        public void G4AssetBundleNativeGraphRetention_Forward_ortho()
        {
            Assert.That(Application.dataPath.Replace('\\', '/'), Is.EqualTo(Project + "/Assets"));
            Assert.That(EditorApplication.isCompiling || EditorApplication.isUpdating, Is.False);
            Assert.That(SystemInfo.graphicsDeviceType, Is.EqualTo(GraphicsDeviceType.Direct3D11));
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
            for (int i = 0; i < SceneManager.sceneCount; ++i) { var s = SceneManager.GetSceneAt(i); Assert.That(s.isDirty, Is.False); Assert.That(s.name + "/" + s.path, Does.Not.Contain("TAI").IgnoreCase); }
            string output = Environment.GetEnvironmentVariable("NBFX_BUNDLE_EVIDENCE_DIR");
            Assert.That(string.IsNullOrEmpty(output), Is.False, "Root supplies fresh private output, no default asset output.");
            output = Path.GetFullPath(output);
            string privateRoot = Path.GetFullPath(Path.Combine(Project, "..")) + Path.DirectorySeparatorChar;
            Assert.That(output.StartsWith(privateRoot, StringComparison.OrdinalIgnoreCase), Is.True, "Only new private .utmp output allowed.");
            Assert.That(output.StartsWith(Path.GetFullPath(Project) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.False, "Never write bundle output into isolation Assets/project.");
            Assert.That(Directory.Exists(output), Is.False, "Keep existing evidence/output untouched."); Directory.CreateDirectory(output);
            string absoluteBundleOutput = Path.Combine(output, "bundle"); Directory.CreateDirectory(absoluteBundleOutput);
            var receipt = new Receipt { project = Project, unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName, output = output,
                scope = "One explicit Windows64 AssetBundle build/load, original Native Ultra stripping scope and loaded Runtime settings/SVC, real loaded B/C Forward draws in Editor. No Player dynamic loading, all variants, lower tiers or new build of frozen Player." };
            void Save() => File.WriteAllText(Path.Combine(output, "receipt.json"), JsonUtility.ToJson(receipt, true));
            var source = Assets.Select(p => new FileState { path = p, sha256 = SHA(Path.Combine(Project, p)), asset = AssetDatabase.LoadMainAssetAtPath(p) }).ToArray();
            Assert.That(source.All(s => s.asset), Is.True); foreach (var s in source) s.serialized = EditorJsonUtility.ToJson(s.asset);
            var stagedMaterials = AssetDatabase.FindAssets("t:Material", new[] { Staging.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => new FileState { path = p, sha256 = SHA(Path.Combine(Project, p)), asset = AssetDatabase.LoadMainAssetAtPath(p) }).ToArray();
            foreach (var s in stagedMaterials) s.serialized = EditorJsonUtility.ToJson(s.asset);
            // Exact original inputs, no Save/automatic acceptance/restoration of Global changes.
            string[] globalProfilePaths = { "Assets/UniversalRenderPipelineGlobalSettings.asset", "Assets/DefaultVolumeProfile.asset", "Assets/DefaultVolumeProfile.asset.meta" };
            var settings = Directory.GetFiles(Path.Combine(Project, "ProjectSettings"), "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(Project, "Assets/Settings"), "*", SearchOption.AllDirectories))
                .Concat(globalProfilePaths.Select(p => Path.Combine(Project, p))).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(p => new FileState { path = p, sha256 = SHA(p) }).ToArray();
            var settingsMemory = Directory.GetFiles(Path.Combine(Project, "Assets/Settings"), "*.asset", SearchOption.AllDirectories)
                .Select(p => p.Substring(Project.Length + 1).Replace('\\', '/')).Concat(globalProfilePaths.Where(p => p.EndsWith(".asset", StringComparison.Ordinal)))
                .Distinct(StringComparer.OrdinalIgnoreCase).Select(p => new FileState { path = p, asset = AssetDatabase.LoadMainAssetAtPath(p) }).Where(s => s.asset).ToArray();
            foreach (var state in settingsMemory) { state.serialized = EditorJsonUtility.ToJson(state.asset); state.dirty = EditorUtility.IsDirty(state.asset); }
            receipt.globalProfile = globalProfilePaths.Select(p =>
            {
                var memory = settingsMemory.SingleOrDefault(v => v.path == p);
                if (p.EndsWith(".asset", StringComparison.Ordinal)) Assert.That(memory, Is.Not.Null, "Actual Global/Profile asset must exist.");
                return new GlobalProfileState { path = p, shaBefore = SHA(Path.Combine(Project, p)), jsonBefore = memory?.serialized, dirtyBefore = memory != null && memory.dirty };
            }).ToArray();
            void CaptureGlobalProfileAfter()
            {
                foreach (var state in receipt.globalProfile)
                {
                    state.shaAfter = File.Exists(Path.Combine(Project, state.path)) ? SHA(Path.Combine(Project, state.path)) : "missing";
                    var memory = settingsMemory.SingleOrDefault(v => v.path == state.path);
                    if (memory != null) { state.jsonAfter = EditorJsonUtility.ToJson(memory.asset); state.dirtyAfter = EditorUtility.IsDirty(memory.asset); }
                }
                receipt.globalProfileExact = receipt.globalProfile.All(v => v.shaBefore == v.shaAfter && v.jsonBefore == v.jsonAfter && v.dirtyBefore == v.dirtyAfter);
            }
            Save();
            var tierType = ProductType("NBShader.NBShaderFeatureTier"); object ultra = Enum.Parse(tierType, "Ultra");
            var overrideType = ProductType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelBuildStripOverride");
            bool beforeOverride = (bool)overrideType.GetProperty("hasOverride", StaticPublic).GetValue(null);
            Assert.That(beforeOverride, Is.False, "No parallel build/override owner allowed.");
            string originalScenes = Scenes(); var previousActive = SceneManager.GetActiveScene(); Scene ownedScene = default;
            AssetBundle bundle = null; var objects = new List<Object>(); var drawStates = new List<DrawState>();
            var oldRT = RenderTexture.active; RenderTexture rt = null; Camera camera = null;
            try
            {
                receipt.graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Select(v => v.ToString()).ToArray();
                G4AssetBundleRetentionShaderObserver.Rows.Clear(); G4AssetBundleRetentionShaderObserver.Active = true;
                AssetBundleManifest manifest;
                using ((IDisposable)Call("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelEditorAPI", "OverrideBuildStripExplicitTier", ultra))
                    manifest = BuildPipeline.BuildAssetBundles(absoluteBundleOutput, new[] { new AssetBundleBuild { assetBundleName = BundleName, assetNames = Assets } }, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                G4AssetBundleRetentionShaderObserver.Active = false; receipt.shaderCallbacks = G4AssetBundleRetentionShaderObserver.Rows.ToArray();
                receipt.observedNativeVariants = receipt.shaderCallbacks.Where(v => v.shader == ((Material)source[0].asset).shader.name).Sum(v => v.observedVariants);
                receipt.observedGraphVariants = receipt.shaderCallbacks.Where(v => v.shader == ((Material)source[1].asset).shader.name).Sum(v => v.observedVariants);
                receipt.manifestBuilt = manifest; Assert.That(manifest, Is.Not.Null); objects.Add(manifest);
                Assert.That(manifest.GetAllAssetBundles(), Is.EqualTo(new[] { BundleName })); receipt.bundleDependencies = manifest.GetAllDependencies(BundleName);
                Assert.That(receipt.bundleDependencies, Is.Empty, "One self-contained bundle, no omitted dependency load."); Save();
                bundle = AssetBundle.LoadFromFile(Path.Combine(absoluteBundleOutput, BundleName)); receipt.actualBundleLoaded = bundle;
                Assert.That(bundle, Is.Not.Null); receipt.assetNames = bundle.GetAllAssetNames();
                foreach (string asset in Assets) Assert.That(receipt.assetNames, Does.Contain(asset.ToLowerInvariant()));
                receipt.loadedAssetTypes = bundle.LoadAllAssets().Select(v => v.GetType().FullName + ":" + v.name).ToArray();
                var loaded = new[] { bundle.LoadAsset<Material>(Assets[0].ToLowerInvariant()), bundle.LoadAsset<Material>(Assets[1].ToLowerInvariant()) };
                var runtimeSettings = bundle.LoadAsset(Assets[2].ToLowerInvariant(), ProductType("NBShader.NBShaderFeatureRuntimeSettings"));
                var svc = bundle.LoadAsset<ShaderVariantCollection>(Assets[3].ToLowerInvariant());
                Assert.That(runtimeSettings, Is.Not.Null); Assert.That(svc, Is.Not.Null); receipt.loadedSVCCount = svc.variantCount; Assert.That(receipt.loadedSVCCount, Is.EqualTo(((ShaderVariantCollection)source[3].asset).variantCount));
                Assert.That(receipt.loadedSVCCount, Is.GreaterThan(0));
                for (int i = 0; i < 2; ++i)
                {
                    Assert.That(loaded[i], Is.Not.Null); Assert.That(loaded[i], Is.Not.SameAs(source[i].asset));
                    Assert.That(AssetDatabase.GetAssetPath(loaded[i]), Is.Empty, "Must render actual bundle Material, never swap back to original asset.");
                    Assert.That(loaded[i].shader.name, Is.EqualTo(((Material)source[i].asset).shader.name));
                    Assert.That(loaded[i].shader.isSupported, Is.True);
                    Assert.That(ShaderUtil.GetShaderMessages(loaded[i].shader).All(m => m.severity.ToString() != "Error"), Is.True);
                    Call("NBShader.NBShaderFeatureRuntime", "ApplyTier", loaded[i], runtimeSettings, ultra);
                }
                receipt.loadedSettingsUsed = true;
                ownedScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
                var actor = GameObject.CreatePrimitive(PrimitiveType.Quad); objects.Add(actor); actor.layer = 4; SceneManager.MoveGameObjectToScene(actor, ownedScene);
                var renderer = actor.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                var cameraObject = new GameObject("Owned Bundle real URP camera"); objects.Add(cameraObject); SceneManager.MoveGameObjectToScene(cameraObject, ownedScene);
                camera = cameraObject.AddComponent<Camera>(); camera.scene = ownedScene; camera.orthographic = true; camera.orthographicSize = 1; camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.transform.position = new Vector3(0, 0, 3); camera.transform.rotation = Quaternion.LookRotation(Vector3.back); camera.cullingMask = 1 << 4; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.0625f, .125f, .1875f, 1); camera.allowHDR = true; camera.allowMSAA = false; cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear); objects.Add(rt); Assert.That(rt.Create(), Is.True); camera.targetTexture = rt;
                var read = new Texture2D(128, 128, TextureFormat.RGBAHalf, false, true); objects.Add(read);
                var draw = typeof(G4GraphVATTests).GetMethod("Draw", BindingFlags.Static | BindingFlags.NonPublic); Assert.That(draw, Is.Not.Null);
                Color[] Snap(Material mat, string role, bool actualLoaded, int host)
                {
                    var pixels = (Color[])draw.Invoke(null, new object[] { renderer, mat, camera, rt, read, output, role });
                    Assert.That(renderer.sharedMaterial, Is.SameAs(mat));
                    drawStates.Add(new DrawState { role = role, materialName = mat.name, shaderName = mat.shader.name, materialAssetPath = AssetDatabase.GetAssetPath(mat), materialInstance = mat.GetInstanceID(), shaderInstance = mat.shader.GetInstanceID(), actualRendererMaterial = renderer.sharedMaterial.GetInstanceID(), actualLoadedMaterial = actualLoaded, shaderSameAsSource = mat.shader == ((Material)source[host].asset).shader, keywords = mat.shaderKeywords.OrderBy(v => v, StringComparer.Ordinal).ToArray() });
                    receipt.draws = drawStates.ToArray(); Save(); return pixels;
                }
                renderer.enabled = false; var clear = Snap(loaded[1], "clear", true, 1); renderer.enabled = true;
                var hosts = new List<Host>();
                for (int i = 0; i < 2; ++i)
                {
                    string role = i == 0 ? "B" : "C"; var material = loaded[i];
                    Assert.That(material.HasProperty("_BaseColorIntensityForTimeline"), Is.True); float intensity = material.GetFloat("_BaseColorIntensityForTimeline"); Assert.That(intensity, Is.GreaterThan(0));
                    var original = Snap((Material)source[i].asset, role + "-source", false, i);
                    var on = Snap(material, role + "-loaded-on", true, i); var repeat = Snap(material, role + "-loaded-repeat", true, i);
                    material.SetFloat("_BaseColorIntensityForTimeline", 0); var control = Snap(material, role + "-loaded-control", true, i);
                    material.SetFloat("_BaseColorIntensityForTimeline", intensity); var restored = Snap(material, role + "-loaded-restored", true, i);
                    var row = new Host { role = role, finite = Finite(clear.Concat(original).Concat(on).Concat(repeat).Concat(control).Concat(restored)), visible = Visible(on, clear), sourceVsLoaded = Delta(original, on), repeat = Delta(on, repeat), response = Delta(on, control), restore = Delta(on, restored), shaderSupported = material.shader.isSupported, noShaderErrors = ShaderUtil.GetShaderMessages(material.shader).All(m => m.severity.ToString() != "Error") };
                    hosts.Add(row); receipt.hosts = hosts.ToArray(); Save();
                    Assert.That(row.finite, Is.True); Assert.That(row.visible, Is.GreaterThan(150)); Assert.That(row.sourceVsLoaded, Is.Zero); Assert.That(row.repeat, Is.Zero); Assert.That(row.response, Is.GreaterThan(.01f)); Assert.That(row.restore, Is.Zero); Assert.That(row.shaderSupported && row.noShaderErrors, Is.True);
                }
                // ShaderVariantCollection remains Native-only. Check the actual
                // loaded material's existing exact BuildInfo membership before warm.
                object mode = Enum.Parse(ProductType("NBShaders2.Editor.FeatureLevel.NBShaderBuildInfoMode"), "ExactMaterialVariants");
                var buildInfo = Call("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelEditorAPI", "GetBuildInfo", loaded[0], ultra, mode);
                var variants = (System.Collections.IEnumerable)buildInfo.GetType().GetProperty("variants").GetValue(buildInfo);
                int checkedVariants = 0, missing = 0;
                foreach (object variant in variants)
                {
                    var type = variant.GetType(); var shader = (Shader)type.GetField("shader").GetValue(variant); var pass = (PassType)type.GetField("passType").GetValue(variant); var keywords = (string[])type.GetProperty("keywords").GetValue(variant);
                    checkedVariants++; if (!svc.Contains(new ShaderVariantCollection.ShaderVariant(shader, pass, keywords))) missing++;
                }
                receipt.loadedSVCMissing = missing; Assert.That(checkedVariants, Is.GreaterThan(0)); Assert.That(missing, Is.Zero);
                svc.WarmUp(); receipt.loadedSVCWarmed = svc.isWarmedUp; Assert.That(receipt.loadedSVCWarmed, Is.True);
                receipt.status = "DRAW_AND_BUNDLE_SUBSTEPS_COMPLETED";
            }
            catch (Exception error) { receipt.error = error.ToString(); receipt.status = "FAILED"; throw; }
            finally
            {
                // Save completed draw/build phases before Unload/EditorJsonUtility can fail.
                receipt.cleanupStage = "entered-finally"; Save();
                var cleanupErrors = new List<Exception>();
                void Attempt(string stage, Action action)
                {
                    receipt.cleanupStage = stage; Save();
                    try { action(); }
                    catch (Exception error)
                    {
                        cleanupErrors.Add(error); receipt.status = "FAILED_CLEANUP";
                        receipt.error = (receipt.error ?? "") + "\n" + stage + ": " + error;
                        receipt.cleanupErrors = cleanupErrors.Select(e => e.ToString()).ToArray(); Save();
                    }
                }
                G4AssetBundleRetentionShaderObserver.Active = false; receipt.callbackStopped = true;
                Attempt("Global-Profile-before-Unload", CaptureGlobalProfileAfter);
                Attempt("detach-camera-and-release-RT", () => { if (camera) camera.targetTexture = null; RenderTexture.active = oldRT; if (rt) rt.Release(); });
                for (int i = objects.Count - 1; i >= 0; --i)
                { Object owned = objects[i]; Attempt("destroy-owned-object-" + i, () => { if (owned) Object.DestroyImmediate(owned); }); }
                Attempt("close-only-owned-scene", () => { if (ownedScene.IsValid() && ownedScene.isLoaded) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(ownedScene); });
                Attempt("restore-original-active-scene", () => { if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive); });
                Attempt("Unload-actual-bundle", () => { if (bundle) { bundle.Unload(true); receipt.bundleUnloaded = true; } });
                Attempt("restore-override-proof", () => receipt.overrideRestored = (bool)overrideType.GetProperty("hasOverride", StaticPublic).GetValue(null) == beforeOverride);
                Attempt("source-file-and-material-memory-proof", () =>
                {
                    receipt.sourceAssetBytesExact = source.All(v => SHA(Path.Combine(Project, v.path)) == v.sha256);
                    receipt.sourceMaterialMemoryExact = stagedMaterials.All(v => SHA(Path.Combine(Project, v.path)) == v.sha256 && EditorJsonUtility.ToJson(v.asset) == v.serialized);
                });
                Attempt("settings-full-json-and-file-proof", () =>
                {
                    receipt.settingsBytesExact = settings.All(v => File.Exists(v.path) && SHA(v.path) == v.sha256) && settingsMemory.All(v => EditorJsonUtility.ToJson(v.asset) == v.serialized);
                    CaptureGlobalProfileAfter();
                });
                Attempt("scene-full-proof", () => receipt.scenesRestored = Scenes() == originalScenes);
                receipt.cleanupComplete = cleanupErrors.Count == 0 && receipt.callbackStopped && receipt.overrideRestored && receipt.sourceAssetBytesExact && receipt.sourceMaterialMemoryExact && receipt.settingsBytesExact && receipt.globalProfileExact && receipt.scenesRestored && (!receipt.actualBundleLoaded || receipt.bundleUnloaded);
                if (!receipt.cleanupComplete) { receipt.status = "FAILED_CLEANUP"; receipt.error = (receipt.error ?? "") + "\nExact bundle input/settings/scene/override cleanup failed. No source/settings changes automatically restored or accepted."; }
                else if (receipt.status == "DRAW_AND_BUNDLE_SUBSTEPS_COMPLETED") receipt.status = "PASSED_LIMITED_EDITOR_BUNDLE";
                receipt.cleanupStage = "finally-proof-complete"; Save();
                if (cleanupErrors.Count > 0) throw new AggregateException("Bundle cleanup exceptions retained in receipt; remaining cleanup attempted.", cleanupErrors);
                Assert.That(receipt.cleanupComplete, Is.True, "Keep source-state failure receipt; never accept drawing alone.");
            }
        }
    }
}
