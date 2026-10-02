if(UnityEngine.Application.dataPath.Replace('\\','/')!="D:/UnityProject/NBUnityProject/Assets")throw new System.InvalidOperationException("Wrong project");
var shader=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
var guiType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShaderEditor.NBShaderGraphGUI",false)).FirstOrDefault(t=>t!=null);
var data=new System.Collections.Generic.List<object>();
if(shader)foreach(string name in new[]{"_OverrideZ_Toggle","_OverrideZValue","_NB_GraphGUIStateVersion"}){int i=shader.FindPropertyIndex(name);data.Add(new{name,index=i,type=i<0?"missing":shader.GetPropertyType(i).ToString()});}
var scenes=new System.Collections.Generic.List<object>();for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);scenes.Add(new{name=s.name,path=s.path,loaded=s.isLoaded,dirty=s.isDirty});}
bool keywordOn=false,keywordOff=false,assignmentImmediate=false;
if(shader&&guiType!=null)
{
    var gui=(UnityEditor.ShaderGUI)System.Activator.CreateInstance(guiType);var m=new UnityEngine.Material(shader);
    var legacy=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");var old=new UnityEngine.Material(legacy);
    try{m.SetFloat("_OverrideZ_Toggle",1);gui.ValidateMaterial(m);keywordOn=m.IsKeywordEnabled("_OVERRIDE_Z");m.SetFloat("_OverrideZ_Toggle",0);gui.ValidateMaterial(m);keywordOff=!m.IsKeywordEnabled("_OVERRIDE_Z");old.SetFloat("_OverrideZ_Toggle",1);gui.AssignNewShaderToMaterial(old,legacy,shader);assignmentImmediate=old.IsKeywordEnabled("_OVERRIDE_Z");}
    finally{UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(old);}
}
return new {project=UnityEngine.Application.dataPath,unity=UnityEngine.Application.unityVersion,compiling=UnityEditor.EditorApplication.isCompiling,updating=UnityEditor.EditorApplication.isUpdating,
    shaderSupported=shader&&shader.isSupported,props=data.ToArray(),keywordOn,keywordOff,assignmentImmediate,scenes=scenes.ToArray(),
    shaderMessages=shader?UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>new{severity=m.severity.ToString(),m.message}).ToArray():null};
