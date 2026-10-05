using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Explicit Root-invoked transitions. Nothing runs on domain load or by itself.
    public static class NBPostCompatibilityControl
    {
        const string Symbol="URP_COMPATIBILITY_MODE";
        const string Clone="D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002";
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        [Serializable] sealed class Journal
        {
            public string project,unity,target,defines,augmentedDefines,globalPath,scene,oldEvidenceEnvironment;
            public bool runtimeCompatibility,rawCompatibility;
            public string[] paths,SHA256,backups;
        }
        [Serializable] sealed class Check
        {
            public string phase,project,unity,target,defines,scene,urpMVID,nbMVID,useRenderGraphEvidence;
            public bool compiling,updating,compatibility,rawCompatibility,useRenderGraph,setterAvailable,noNBComponents,noNBCallbacks,assetsRestored,sceneRestored,definesRestored,environmentRestored,ownedPlayerSettingsDirty,ownedGlobalSettingsDirty;
            public string[] declaredMethods,paths,SHA256;
        }
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static string Root=>Path.GetDirectoryName(Application.dataPath);
        static NamedBuildTarget Named=>NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        static RenderGraphSettings Settings=>GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
        static bool Raw=>(bool)typeof(RenderGraphSettings).GetField("m_EnableRenderCompatibilityMode",All).GetValue(Settings);
        static string SceneState()=>string.Join("\n",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var s=SceneManager.GetSceneAt(i);return s.name+"|"+s.path+"|"+s.isLoaded+"|"+s.isDirty+"|"+(s==SceneManager.GetActiveScene())+"|roots="+s.GetRootGameObjects().Length;}));
        internal static string RunnerFingerprint(Scene scene)
        {
            // Reuse the already validated exact bootstrap recognizer; no Cine lifecycle is invoked.
            var m=typeof(G4NBPostCinemachinePersistenceTests).GetMethod("ExactRunnerBootstrapFingerprint",BindingFlags.Static|BindingFlags.NonPublic);
            Require(m!=null,"Original bootstrap recognizer changed.");
            try{return(string)m.Invoke(null,new object[]{scene});}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}
        }
        static bool SetterAvailable
        {
            get{var a=(ObsoleteAttribute)Attribute.GetCustomAttribute(typeof(RenderGraphSettings).GetProperty("enableRenderCompatibilityMode").SetMethod,typeof(ObsoleteAttribute));return a==null||!a.IsError;}
        }
        static bool NoComponents=>!new[]{"NBShader.PostProcessingManager","NBShader.PostProcessingController"}.Any(n=>Resources.FindObjectsOfTypeAll(NBPostPathScope.Find(n)).OfType<Component>().Any(c=>c&&c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded));
        static bool NoCallbacks=>!(EditorApplication.update?.GetInvocationList()??Array.Empty<Delegate>()).Any(d=>d.Method.DeclaringType?.FullName=="NBShader.PostProcessingManager"||d.Method.DeclaringType?.FullName=="NBShader.PostProcessingController");
        internal static void Guard()
        {
            Require(string.Equals(Path.GetFullPath(Root).TrimEnd('\\','/'),Path.GetFullPath(Clone).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase),"This control is restricted to the existing isolated clone.");
            Require(Application.unityVersion.StartsWith("6000.3.",StringComparison.Ordinal),"Only the reviewed 6000.3 opt-in epoch is in scope.");
            Require(!Application.isPlaying&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,"Editor must be idle in EditMode.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);Require((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase)<0,"Loaded TAI scene forbids proactive reload.");Require(!s.isDirty,"Do not reload an unsaved dirty scene.");}
        }
        internal static void RequireCompiledPath()
        {
            Require(SetterAvailable,"Official Compatibility setting setter is not compiled. Add the define and wait for a completed recompile first.");
            Require(PlayerSettings.GetScriptingDefineSymbols(Named).Split(';').Contains(Symbol),"Active build target lacks opt-in define.");
            foreach(var n in new[]{"NBShader.NBPostProcess","NBShader.RenderCameraOpaqueDistortObjectPass","NBShader.ScreenColorRenderPass","NBShader.DisturbanceMaskRenderPass","NBShader.NBPostProcessRenderPass"})
            {
                var t=NBPostPathScope.Find(n);var method=n=="NBShader.NBPostProcess"?"SetupRenderPasses":"Execute";
                Require(t.GetMethods(All|BindingFlags.DeclaredOnly).Any(m=>m.Name==method),n+" does not declare compiled "+method+"; inherited legacy stub is insufficient.");
            }
        }
        static void TransitionGuard(){Guard();Require(!NBPostPathScope.HasActive&&NoComponents&&NoCallbacks,"Active fixture/NB lifecycle state prohibits mode/define transition.");}
        static Object OwnedPlayerSettings()
        {
            // Same exact native singleton target used by Unity 6000.3's PlayerSettings
            // serialized inspector. Read-only internal accessor; no private setter.
            var getter=typeof(PlayerSettings).GetMethod("GetSerializedObject",BindingFlags.Static|BindingFlags.NonPublic);
            Require(getter!=null,"Reviewed PlayerSettings singleton accessor unavailable.");
            var serialized=getter.Invoke(null,null)as SerializedObject;
            Require(serialized!=null&&serialized.targetObject&&serialized.targetObject.GetType()==typeof(PlayerSettings),"Unexpected PlayerSettings singleton target.");
            return serialized.targetObject;
        }
        static void GuardNoPlayerSettingsOverride()
        {
            // Unity's public define setter itself saves an active profile with a
            // PlayerSettings override. That separate asset is outside this journal.
            var profile=UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
            if(!profile)return;
            var property=profile.GetType().GetProperty("playerSettings",All);
            Require(property!=null,"Reviewed BuildProfile override accessor unavailable.");
            Require(!(property.GetValue(profile)as Object),"Active BuildProfile has PlayerSettings override; refuse its automatic save outside the owned global settings scope.");
        }
        static Journal Read(string folder)
        {
            var j=JsonUtility.FromJson<Journal>(File.ReadAllText(Path.Combine(folder,"journal.json")));
            Require(j.project==Root&&j.target==EditorUserBuildSettings.activeBuildTarget.ToString(),"Journal project/active build target changed.");return j;
        }
        static void SaveCheck(string folder,string phase,Journal j)
        {
            Directory.CreateDirectory(folder);string p=Path.Combine(folder,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+phase+".json");
            if(phase=="requested-add-define"||phase=="requested-restore-defines")
                File.WriteAllText(p,JsonUtility.ToJson(new DefineRequest{phase=phase,defines=PlayerSettings.GetScriptingDefineSymbols(Named)},true));
            else File.WriteAllText(p,JsonUtility.ToJson(InspectRecord(phase,j,folder),true));
        }
        static bool ReadUseRenderGraph(out string evidence)
        {
            var field=typeof(UniversalRenderPipeline).GetField("useRenderGraph",All);
            if(field!=null){evidence="actual compiled URP useRenderGraph field";return(bool)field.GetValue(null);}
            bool defined=PlayerSettings.GetScriptingDefineSymbols(Named).Split(';').Contains(Symbol);
            Require(!defined&&!SetterAvailable&&!Settings.enableRenderCompatibilityMode&&!Raw,
                "URP field absent outside verified no-define/false/false RG-only compilation; wait for actual recompile.");
            evidence="compiledRGOnly: define absent, official setter unavailable, getter/raw false, conditional URP field absent";
            return true;
        }
        [Serializable] sealed class DefineRequest
        {public string phase,defines;public bool actualCompileAndRouteReadbackPending=true;}
        static string OriginalEvidenceEnvironment(string folder)
        {
            string value=File.ReadAllText(Path.Combine(folder,"evidence-env-before.txt"));
            if(value=="NULL")return null;
            Require(value.StartsWith("VALUE",StringComparison.Ordinal),"Invalid original environment receipt.");
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(5)));
        }
        static Check InspectRecord(string phase,Journal j,string folder)
        {
            var nb=NBPostPathScope.Find("NBShader.NBPostProcess");var urp=typeof(UniversalRenderPipeline);
            string routeEvidence;bool actualUseRenderGraph=ReadUseRenderGraph(out routeEvidence);
            var declared=new[]{"NBShader.NBPostProcess","NBShader.RenderCameraOpaqueDistortObjectPass","NBShader.ScreenColorRenderPass","NBShader.DisturbanceMaskRenderPass","NBShader.NBPostProcessRenderPass"}.SelectMany(n=>NBPostPathScope.Find(n).GetMethods(All|BindingFlags.DeclaredOnly).Where(m=>m.Name=="Execute"||m.Name=="SetupRenderPasses"||m.Name=="RecordRenderGraph").Select(m=>n+"."+m.Name)).ToArray();
            return new Check{phase=phase,project=Root,unity=Application.unityVersion,target=EditorUserBuildSettings.activeBuildTarget.ToString(),defines=PlayerSettings.GetScriptingDefineSymbols(Named),scene=SceneState(),urpMVID=urp.Module.ModuleVersionId.ToString(),nbMVID=nb.Module.ModuleVersionId.ToString(),compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating,compatibility=Settings.enableRenderCompatibilityMode,rawCompatibility=Raw,useRenderGraph=actualUseRenderGraph,useRenderGraphEvidence=routeEvidence,setterAvailable=SetterAvailable,ownedPlayerSettingsDirty=EditorUtility.IsDirty(OwnedPlayerSettings()),ownedGlobalSettingsDirty=EditorUtility.IsDirty(UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>()),noNBComponents=NoComponents,noNBCallbacks=NoCallbacks,declaredMethods=declared,paths=j.paths,SHA256=j.paths.Select(p=>NBPostPathScope.SHA(File.ReadAllBytes(Path.Combine(Root,p)))).ToArray(),assetsRestored=j.paths.Select((p,i)=>NBPostPathScope.SHA(File.ReadAllBytes(Path.Combine(Root,p)))==j.SHA256[i]).All(v=>v),sceneRestored=SceneState()==j.scene,definesRestored=PlayerSettings.GetScriptingDefineSymbols(Named)==j.defines,environmentRestored=Environment.GetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR")==OriginalEvidenceEnvironment(folder)};
        }
        public static string Prepare(string folder)
        {
            TransitionGuard();Require(!File.Exists(Path.Combine(folder,"journal.json")),"Use a new journal directory; earlier evidence is immutable.");
            Require(Settings!=null,"Missing official URP RenderGraphSettings.");
            Require(SceneManager.sceneCount==1&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path)&&SceneManager.GetActiveScene().GetRootGameObjects().Length==0,"Begin this bounded reload experiment in a clean dedicated EmptyScene; do not replace unknown scene content.");
            GuardNoPlayerSettingsOverride();
            Require(!EditorUtility.IsDirty(OwnedPlayerSettings()),"Owned PlayerSettings already dirty; do not serialize prior unrelated setting edits.");
            var ownedGlobal=UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
            Require(ownedGlobal&&!EditorUtility.IsDirty(ownedGlobal),"Owned URP GlobalSettings already dirty; do not save prior setting edits.");
            string global=AssetDatabase.GetAssetPath(UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>());Require(!string.IsNullOrEmpty(global),"Global settings must resolve to an actual asset.");
            string definitions=PlayerSettings.GetScriptingDefineSymbols(Named);
            var j=new Journal{project=Root,unity=Application.unityVersion,target=EditorUserBuildSettings.activeBuildTarget.ToString(),defines=definitions,augmentedDefines=definitions.Split(';').Contains(Symbol)?definitions:(string.IsNullOrEmpty(definitions)?Symbol:definitions+";"+Symbol),globalPath=global,runtimeCompatibility=Settings.enableRenderCompatibilityMode,rawCompatibility=Raw,scene=SceneState(),oldEvidenceEnvironment=Environment.GetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR"),paths=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset",global}};
            // This bounded experiment starts in a clean RG epoch; do not "repair" a hidden true value.
            Require(!j.runtimeCompatibility&&!j.rawCompatibility,"Expected original clean RG false/false state.");
            Directory.CreateDirectory(folder);j.SHA256=new string[j.paths.Length];j.backups=new string[j.paths.Length];
            for(int i=0;i<j.paths.Length;i++){byte[] b=File.ReadAllBytes(Path.Combine(Root,j.paths[i]));j.SHA256[i]=NBPostPathScope.SHA(b);j.backups[i]=i+"-before.bytes";File.WriteAllBytes(Path.Combine(folder,j.backups[i]),b);}
            File.WriteAllText(Path.Combine(folder,"journal.json"),JsonUtility.ToJson(j,true));
            File.WriteAllText(Path.Combine(folder,"defines-before.txt"),j.defines);File.WriteAllText(Path.Combine(folder,"target-before.txt"),j.target);
            File.WriteAllText(Path.Combine(folder,"evidence-env-before.txt"),j.oldEvidenceEnvironment==null?"NULL":"VALUE"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(j.oldEvidenceEnvironment)));
            SaveCheck(folder,"prepared",j);Environment.SetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR",Path.Combine(folder,"cases"));
            return "Prepared durable before journal and bounded evidence directory; no Unity setting changed.";
        }
        public static string ResumePrepared(string folder)
        {
            TransitionGuard();GuardNoPlayerSettingsOverride();var j=Read(folder);
            Require(SceneState()==j.scene,"Scene differs from prepared journal.");
            Require(PlayerSettings.GetScriptingDefineSymbols(Named)==j.defines&&
                Settings.enableRenderCompatibilityMode==j.runtimeCompatibility&&Raw==j.rawCompatibility,
                "Settings changed since original Prepare; this resume cannot mutate them.");
            Require(Environment.GetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR")==OriginalEvidenceEnvironment(folder),
                "Original evidence environment does not match journal.");
            for(int i=0;i<j.paths.Length;i++)
            {
                Require(NBPostPathScope.SHA(File.ReadAllBytes(Path.Combine(folder,j.backups[i])))==j.SHA256[i],
                    "Original journal backup changed.");
                Require(NBPostPathScope.SHA(File.ReadAllBytes(Path.Combine(Root,j.paths[i])))==j.SHA256[i],
                    "Current settings bytes differ from original journal.");
            }
            Require(!EditorUtility.IsDirty(OwnedPlayerSettings())&&
                !EditorUtility.IsDirty(UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>()),
                "Owned settings became dirty; do not overwrite prior work.");
            SaveCheck(folder,"resume-prepared-before-env",j);
            Environment.SetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR",Path.Combine(folder,"cases"));
            return "Existing journal/backups/current settings/scene/original env verified; evidence env set. No settings or journal rewritten.";
        }
        public static string AddDefine(string folder)
        {
            TransitionGuard();GuardNoPlayerSettingsOverride();var j=Read(folder);string current=PlayerSettings.GetScriptingDefineSymbols(Named);
            if(current==j.augmentedDefines){SaveCheck(folder,"already-requested-define",j);return "Define already present; inspect completion instead of repeating mutation.";}
            Require(current==j.defines,"Defines changed since journal preparation.");
            SaveCheck(folder,"before-add-define",j);
            PlayerSettings.SetScriptingDefineSymbols(Named,j.augmentedDefines);
            SaveCheck(folder,"requested-add-define",j);return "Requested official opt-in define. Wait for actual compile/domain reload; running/timeout is not success.";
        }
        public static string SetMode(string folder,bool compatibility)
        {
            TransitionGuard();GuardNoPlayerSettingsOverride();var j=Read(folder);RequireCompiledPath();Require(PlayerSettings.GetScriptingDefineSymbols(Named)==j.augmentedDefines,"Unexpected define epoch.");
            Require(!EditorUtility.IsDirty(UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>()),"Owned URP settings became dirty before this transition.");
            SaveCheck(folder,compatibility?"before-compatibility":"before-rg",j);
            // Invoke the public setter only after proving it is the real compiled API.
            // No private serialized write is used to bypass the official opt-in guard.
            typeof(RenderGraphSettings).GetProperty("enableRenderCompatibilityMode").SetValue(Settings,compatibility);
            var global=UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();EditorUtility.SetDirty(global);AssetDatabase.SaveAssetIfDirty(global);
            SaveCheck(folder,compatibility?"requested-compatibility":"requested-rg",j);
            return "Mode set through public API. Wait for actual pipeline recreation and inspect useRenderGraph before running a case.";
        }
        public static string RestoreMode(string folder)
        {
            TransitionGuard();var j=Read(folder);
            if(!SetterAvailable){Require(Settings.enableRenderCompatibilityMode==j.runtimeCompatibility&&Raw==j.rawCompatibility,"Opt-in code was removed before setting restore; restore define first.");return "Original mode already restored.";}
            return SetMode(folder,j.runtimeCompatibility);
        }
        public static string RestoreScene(string folder)
        {
            TransitionGuard();var j=Read(folder);if(SceneState()==j.scene)return "Original empty scene already restored.";
            Require(SceneManager.sceneCount==1,"Do not close unknown extra scenes.");var scene=SceneManager.GetActiveScene();
            Require(string.IsNullOrEmpty(scene.path)&&!scene.isDirty&&scene.GetRootGameObjects().Length==2,"Only the exact clean runner bootstrap can be removed.");
            string fingerprint=RunnerFingerprint(scene);string cases=Path.Combine(folder,"cases");
            var receipts=Directory.Exists(cases)?Directory.GetFiles(cases,"before.json",SearchOption.AllDirectories):Array.Empty<string>();
            Require(receipts.Any(p=>JsonUtility.FromJson<NBPostPathScope.Before>(File.ReadAllText(p)).runnerBootstrapFingerprint==fingerprint),"Current component serialization does not match any recorded pre-test runner bootstrap.");
            File.WriteAllText(Path.Combine(folder,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-bootstrap-before-empty.txt"),fingerprint);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Require(SceneState()==j.scene,"Original clean EmptyScene was not restored.");SaveCheck(folder,"scene-restored",j);return "Exact recorded runner bootstrap replaced by original EmptyScene.";
        }
        public static string RestoreDefines(string folder)
        {
            TransitionGuard();GuardNoPlayerSettingsOverride();var j=Read(folder);Require(Settings.enableRenderCompatibilityMode==j.runtimeCompatibility&&Raw==j.rawCompatibility,"Restore public mode before removing define.");
            string current=PlayerSettings.GetScriptingDefineSymbols(Named);Require(current==j.augmentedDefines||current==j.defines,"Unrelated define changes must not be overwritten.");
            if(current==j.defines){SaveCheck(folder,"already-restored-defines",j);return "Defines already restored; inspect completion instead of repeating mutation.";}
            SaveCheck(folder,"before-restore-defines",j);PlayerSettings.SetScriptingDefineSymbols(Named,j.defines);SaveCheck(folder,"requested-restore-defines",j);
            return "Original defines requested. Wait for completed compile/domain reload before exact byte restoration.";
        }
        public static string RestoreExactBytes(string folder)
        {
            TransitionGuard();var j=Read(folder);Require(PlayerSettings.GetScriptingDefineSymbols(Named)==j.defines&&Settings.enableRenderCompatibilityMode==j.runtimeCompatibility&&Raw==j.rawCompatibility,"Semantic settings must be restored first.");
            // Permit only serializer representation of the owned define block / one bool.
            // Refuse all other disk changes, even inside the isolated project.
            var writes=new System.Collections.Generic.List<Tuple<int,string,byte[],byte[]>>();
            for(int i=0;i<j.paths.Length;i++)
            {
                string path=Path.Combine(Root,j.paths[i]);byte[] before=File.ReadAllBytes(Path.Combine(folder,j.backups[i])),now=File.ReadAllBytes(path);
                Require(NBPostPathScope.SHA(before)==j.SHA256[i],"Backup changed.");if(now.SequenceEqual(before))continue;
                string a=System.Text.Encoding.UTF8.GetString(before),b=System.Text.Encoding.UTF8.GetString(now);
                if(i==0){const string pattern=@"(?ms)^  scriptingDefineSymbols:.*?(?=^  \S|\z)";Require(Regex.Matches(a,pattern).Count==1&&Regex.Matches(b,pattern).Count==1,"Unexpected define YAML layout.");a=Regex.Replace(a,pattern,"  scriptingDefineSymbols: <owned>\n");b=Regex.Replace(b,pattern,"  scriptingDefineSymbols: <owned>\n");}
                else if(j.paths[i]==j.globalPath){const string pattern=@"(?m)^(\s*m_EnableRenderCompatibilityMode:) [01]\s*$";Require(Regex.Matches(a,pattern).Count==1&&Regex.Matches(b,pattern).Count==1,"Unexpected compatibility YAML layout.");a=Regex.Replace(a,pattern,"$1 <owned>");b=Regex.Replace(b,pattern,"$1 <owned>");}
                Require(a==b,"Unexpected asset changes: "+j.paths[i]+". Preserve evidence; do not overwrite.");
                writes.Add(Tuple.Create(i,path,now,before));
            }
            Require(writes.All(w=>File.ReadAllBytes(w.Item2).SequenceEqual(w.Item3)),"Files changed after restore validation.");
            foreach(var w in writes){File.WriteAllBytes(Path.Combine(folder,w.Item1+"-serializer-restored.bytes"),w.Item3);File.WriteAllBytes(w.Item2,w.Item4);}
            string env=Environment.GetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR");Require(env==Path.Combine(folder,"cases")||env==OriginalEvidenceEnvironment(folder),"Unrelated evidence environment changes must not be overwritten.");
            Environment.SetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR",OriginalEvidenceEnvironment(folder));
            SaveCheck(folder,"exact-bytes-restored",j);return Inspect(folder,true);
        }
        public static string Inspect(string folder,bool requireRestored=false)
        {
            Guard();var j=Read(folder);var c=InspectRecord(requireRestored?"restoration":"inspection",j,folder);SaveCheck(folder,c.phase,j);
            if(requireRestored)Require(c.assetsRestored&&c.sceneRestored&&c.definesRestored&&c.environmentRestored&&c.compatibility==j.runtimeCompatibility&&c.rawCompatibility==j.rawCompatibility&&c.useRenderGraph&&!NBPostPathScope.HasActive&&c.noNBComponents&&c.noNBCallbacks,"Actual environment restoration is incomplete.");
            return JsonUtility.ToJson(c,true);
        }
    }
}
