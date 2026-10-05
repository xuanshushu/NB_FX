// Read-only eval body. Generator replaces D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002. Does not create/close scenes or touch dirty flags.
string project=System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath).Replace('\\','/');
if(project!="D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002")throw new System.InvalidOperationException("Wrong isolated project");
if(UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating)throw new System.InvalidOperationException("Editor busy");
var scenes=new System.Collections.Generic.List<object>();
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){
 var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
 if(s.isDirty||(s.name+" "+s.path).IndexOf("TAI",System.StringComparison.OrdinalIgnoreCase)>=0)throw new System.InvalidOperationException("Dirty/TAI scene");
 var roots=new System.Collections.Generic.List<object>();
 foreach(var go in s.GetRootGameObjects())roots.Add(new{name=go.name,active=go.activeSelf,components=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(go.GetComponentsInChildren<UnityEngine.Component>(true),c=>c?c.GetType().FullName:"MissingScript"))});
 scenes.Add(new{name=s.name,path=s.path,loaded=s.isLoaded,dirty=s.isDirty,roots=roots.ToArray()});
}
// This is the existing readonly NPC recognizer: no scene/lifecycle call.
string bootstrapSource="Tests/URP/Editor/G4NBPostCinemachinePersistenceTests.cs",bootstrapSourceSHA=null,bootstrapFingerprint=null;
using(var h=System.Security.Cryptography.SHA256.Create())bootstrapSourceSHA=System.BitConverter.ToString(h.ComputeHash(System.IO.File.ReadAllBytes(System.IO.Path.Combine(project,"Packages/NB_FX",bootstrapSource)))).Replace("-","").ToLowerInvariant();
if(bootstrapSourceSHA!="7f561abc60772c9a0273a4e64366544ac0a4be9e70711a7bb70837d9602269bb")throw new System.InvalidOperationException("Bootstrap recognizer source changed");
UnityEngine.Light bootstrapLight=null;
var bootstrapIDs=new System.Collections.Generic.List<object>();
if(UnityEngine.SceneManagement.SceneManager.sceneCount==1){
 var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(0);
 if(string.IsNullOrEmpty(s.path)&&s.GetRootGameObjects().Length==2){
  var type=System.Linq.Enumerable.Single(System.Linq.Enumerable.Where(System.Linq.Enumerable.Select(System.AppDomain.CurrentDomain.GetAssemblies(),a=>a.GetType("NBFX.Baseline.Tests.G4NBPostCinemachinePersistenceTests",false)),x=>x!=null));
  var method=type.GetMethod("ExactRunnerBootstrapFingerprint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  if(method==null)throw new System.InvalidOperationException("Missing exact NPC bootstrap recognizer");
  bootstrapFingerprint=(string)method.Invoke(null,new object[]{s});
  foreach(var go in s.GetRootGameObjects()){
   bootstrapIDs.Add(new{key=go.name,id=go.GetInstanceID()});
   foreach(var c in go.GetComponents<UnityEngine.Component>())bootstrapIDs.Add(new{key=go.name+"/"+c.GetType().FullName,id=c.GetInstanceID()});
   if(go.name=="Directional Light")bootstrapLight=go.GetComponent<UnityEngine.Light>();
  }
 }
}
var sun=UnityEngine.RenderSettings.sun;
bool exactBootstrapSun=bootstrapLight&&sun==bootstrapLight&&!string.IsNullOrEmpty(bootstrapFingerprint);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
if(!pipeline)throw new System.InvalidOperationException("Expected URP");
var assets=new System.Collections.Generic.List<UnityEngine.Object>{pipeline};
var features=new System.Collections.Generic.List<object>();
var materialOwners=new System.Collections.Generic.Dictionary<UnityEngine.Object,System.Collections.Generic.List<string>>();
var materialOwned=new System.Collections.Generic.Dictionary<UnityEngine.Object,bool>();
int rendererIndex=-1;
foreach(var rd in pipeline.rendererDataList){
 rendererIndex++;int featureIndex=-1;
 if(!rd)continue;assets.Add(rd);
 foreach(var f in rd.rendererFeatures){
  featureIndex++;
  if(!f)throw new System.InvalidOperationException("Missing feature");assets.Add(f);
  features.Add(new{id=f.GetInstanceID(),type=f.GetType().FullName,active=f.isActive,json=UnityEditor.EditorJsonUtility.ToJson(f)});
  for(var t=f.GetType();t!=null;t=t.BaseType)foreach(var field in t.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly))
   if(typeof(UnityEngine.Material).IsAssignableFrom(field.FieldType)){
    var m=field.GetValue(f) as UnityEngine.Material;if(!m)continue;assets.Add(m);
    string slot=field.DeclaringType.FullName+"."+field.Name;
    bool known=(slot=="UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion.m_Material"&&f.GetType().FullName=="UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion")||(f.GetType().FullName=="NBShader.NBPostProcess"&&(slot=="NBShader.NBPostProcess._disturbanceDownSampleMat"||slot=="NBShader.NBPostProcess._screenColorDownSampleMat"));
    if(!materialOwners.ContainsKey(m)){materialOwners[m]=new System.Collections.Generic.List<string>();materialOwned[m]=true;}
    materialOwners[m].Add(UnityEditor.AssetDatabase.GetAssetPath(rd)+"/renderer["+rendererIndex+"]/feature["+featureIndex+"]/"+f.GetType().FullName+"/"+slot);
    materialOwned[m]=materialOwned[m]&&known;
   }
 }
}
var serialized=new System.Collections.Generic.List<object>();
foreach(var o in System.Linq.Enumerable.Distinct(assets)){
 string path=UnityEditor.AssetDatabase.GetAssetPath(o),disk=null;
 if(!string.IsNullOrEmpty(path)&&System.IO.File.Exists(path))using(var hash=System.Security.Cryptography.SHA256.Create())disk=System.BitConverter.ToString(hash.ComputeHash(System.IO.File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
 var mat=o as UnityEngine.Material;string shaderPath=null,shaderName=null,shaderGuid=null;long shaderLocalID=0;
 if(mat&&mat.shader){shaderPath=UnityEditor.AssetDatabase.GetAssetPath(mat.shader);shaderName=mat.shader.name;UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat.shader,out shaderGuid,out shaderLocalID);}
 string[] owners=materialOwners.ContainsKey(o)?materialOwners[o].ToArray():System.Array.Empty<string>();System.Array.Sort(owners,System.StringComparer.Ordinal);
 bool slotOwned=mat&&string.IsNullOrEmpty(path)&&materialOwned.ContainsKey(o)&&materialOwned[o]&&owners.Length>0;
 serialized.Add(new{id=o.GetInstanceID(),identityKind=slotOwned?"owned-feature-material":"strict-instance",owners=owners,type=o.GetType().FullName,path=path,json=UnityEditor.EditorJsonUtility.ToJson(o),diskSHA256=disk,shader=new{name=shaderName,path=shaderPath,guid=shaderGuid,localFileID=shaderLocalID}});
}
var components=new System.Collections.Generic.List<string>();
foreach(var c in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Component>())if(c&&!UnityEditor.EditorUtility.IsPersistent(c)&&(c.GetType().FullName=="NBShader.PostProcessingController"||c.GetType().FullName=="NBShader.PostProcessingManager"||c.GetType().FullName=="AnimationSheetHelper"))components.Add(c.GetType().FullName+":"+c.GetInstanceID());
var updateField=typeof(UnityEditor.EditorApplication).GetField("update",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
if(updateField==null)throw new System.InvalidOperationException("Cannot inspect Editor update callback field");
var update=updateField.GetValue(null) as System.Delegate;
var callbacks=new System.Collections.Generic.List<string>();
if(update!=null)foreach(var d in update.GetInvocationList())if(d.Method.DeclaringType!=null&&d.Method.DeclaringType.FullName.StartsWith("NBShader.PostProcessing"))callbacks.Add(d.Method.DeclaringType.FullName+"."+d.Method.Name);
var errors=new System.Collections.Generic.List<string>();
foreach(var path in new[]{"Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader","Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader","Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph","Packages/com.xuanxuan.nb.fx/Tests/URP/Graphs/NBBackFirstModern.shadergraph","Packages/com.xuanxuan.nb.fx/Tests/URP/Shaders/DefaultSSAOReadback.shader"}){
 var shader=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(path);if(!shader)throw new System.InvalidOperationException("Missing shader "+path);
 foreach(var e in UnityEditor.ShaderUtil.GetShaderMessages(shader))if(e.severity.ToString()=="Error")errors.Add(path+":"+e.message);
}
var probe=new float[27];for(int rgb=0;rgb<3;rgb++)for(int k=0;k<9;k++)probe[rgb*9+k]=UnityEngine.RenderSettings.ambientProbe[rgb,k];
var tempAssets=new System.Collections.Generic.List<string>();
foreach(var p in UnityEditor.AssetDatabase.GetAllAssetPaths())if(p.StartsWith("Assets/__NBFX_GUI1A_TEMP_")||p.StartsWith("Assets/ResTemp/EditorTemp/NBFX_Tier_"))tempAssets.Add(p);
var ambient=UnityEngine.RenderSettings.ambientLight;
return Newtonsoft.Json.JsonConvert.SerializeObject(new{project,unity=UnityEngine.Application.unityVersion,api=UnityEngine.SystemInfo.graphicsDeviceType.ToString(),gpu=UnityEngine.SystemInfo.graphicsDeviceName,
 scenes=scenes.ToArray(),bootstrap=new{recognized=bootstrapFingerprint!=null,fingerprint=bootstrapFingerprint,source=bootstrapSource,sourceSHA256=bootstrapSourceSHA},bootstrapObjectIDs=bootstrapIDs.ToArray(),previewSceneCount=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount,features=features.ToArray(),assets=serialized.ToArray(),components=components.ToArray(),callbacks=callbacks.ToArray(),shaderErrors=errors.ToArray(),tempAssets=tempAssets.ToArray(),
 render=new{sun=sun?sun.GetInstanceID():0,sunIdentity=new{kind=exactBootstrapSun?"exact-runner-bootstrap-light":"strict-instance",key=exactBootstrapSun?"Directional Light/UnityEngine.Light":null},ambientMode=UnityEngine.RenderSettings.ambientMode.ToString(),ambientLight=new{r=ambient.r,g=ambient.g,b=ambient.b,a=ambient.a},ambientProbe=probe,fog=UnityEngine.RenderSettings.fog,activeRT=UnityEngine.RenderTexture.active?UnityEngine.RenderTexture.active.GetInstanceID():0,asyncCompilation=UnityEditor.ShaderUtil.allowAsyncCompilation},
 evidenceEnvironment=System.Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR"),isolationEnvironment=System.Environment.GetEnvironmentVariable("NBFX_ISOLATED_PROJECT_DIR")});
