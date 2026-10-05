using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Directed actual generated MotionVectors entry with explicit prev uniforms.
    // Not automatic frame-history/Skinned/Alembic/VFX/TAA lifecycle evidence.
    public sealed class G4GraphMotionVectorInputBridgeTests
    {
        const string GraphPath="Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string IncludeLine="#include \"Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/Passes/NBGraphMotionVectorPass.hlsl\"";
        const int Size=96;readonly List<Object> owned=new List<Object>();string generated;Shader actualGraph;string folder;
        [OneTimeSetUp]public void ImportCurrentGraphAndGeneratedEntryProof()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            Assert.That(SystemInfo.graphicsDeviceType,Is.EqualTo(GraphicsDeviceType.Direct3D11));
            AssetDatabase.ImportAsset(GraphPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            actualGraph=AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);Assert.That(actualGraph&&actualGraph.isSupported,Is.True);
            var type=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Unity.ShaderGraph.Editor").GetType("UnityEditor.ShaderGraph.ShaderGraphImporter",true);
            var methods=type.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Where(m=>m.Name=="GetShaderText"&&m.GetParameters().Length==4&&m.GetParameters()[0].ParameterType==typeof(string)&&m.GetParameters()[1].IsOut&&m.GetParameters()[3].IsOut).ToArray();Assert.That(methods.Length,Is.EqualTo(1));object[] args={GraphPath,null,null,null};generated=(string)methods[0].Invoke(null,args);
            Assert.That(generated,Does.Contain(IncludeLine));Assert.That(generated,Does.Contain("float4 uv4 : TEXCOORD4"));Assert.That(generated,Does.Contain("ATTRIBUTES_NEED_TEXCOORD4"));Assert.That(generated,Does.Contain("Name \"MotionVectors\""));
            folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXMotionInput"),"motion-controlled-ortho");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"generated-current-graph.shader"),generated);
            using(var hash=System.Security.Cryptography.SHA256.Create())File.WriteAllText(Path.Combine(folder,"generated-source-sha.txt"),BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(generated))).Replace("-","").ToLowerInvariant());
        }
        T Keep<T>(T value)where T:Object{owned.Add(value);return value;}
        Material Configure(Shader shader)
        {
            var m=Keep(new Material(shader){hideFlags=HideFlags.HideAndDontSave});m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",3);m.SetFloat("_Surface",0);m.SetFloat("_AlphaClip",0);m.SetFloat("_Blend",0);m.SetFloat("_Cull",0);m.SetColor("_Color",Color.white);m.SetColor("_ColorA",Color.white);m.SetFloat("_AlphaAll",1);m.SetFloat("_VAT_Toggle",0);m.SetFloat("_FxLightMode",0);G4SpecDebugFixture.Validate(m);m.SetShaderPassEnabled("MotionVectors",true);return m;
        }
        static Color[] Draw(Material material,Mesh mesh,RenderTexture target,Texture2D read,Matrix4x4 current,Matrix4x4 previous,bool motion,bool controlled=false)
        {
            Matrix4x4 view=Matrix4x4.TRS(new Vector3(0,0,-4),Quaternion.identity,Vector3.one).inverse;view=Matrix4x4.Scale(new Vector3(1,1,-1))*view;
            Matrix4x4 projection=Matrix4x4.Ortho(-2,2,-2,2,.1f,10);var vp=GL.GetGPUProjectionMatrix(projection,true)*view;
            if(controlled){material.SetMatrix("NBFX_TestPreviousModel",previous);material.SetMatrix("NBFX_TestCurrentVP",vp);material.SetMatrix("NBFX_TestPreviousVP",vp);material.SetVector("NBFX_TestMotionParams",new Vector4(0,1,0,0));}
            int pass=material.FindPass(motion?"MotionVectors":"Universal Forward");Assert.That(pass,Is.GreaterThanOrEqualTo(0));var before=RenderTexture.active;GL.PushMatrix();
            try{RenderTexture.active=target;GL.Clear(true,true,Color.clear);GL.Viewport(new Rect(0,0,Size,Size));GL.modelview=view;GL.LoadProjectionMatrix(projection);Assert.That(material.SetPass(pass),Is.True);Graphics.DrawMeshNow(mesh,current);read.ReadPixels(new Rect(0,0,Size,Size),0,0,false);read.Apply(false,false);return read.GetPixels();}
            finally{GL.PopMatrix();RenderTexture.active=before;}
        }
        static float Delta(Color[]a,Color[]b){float max=0;for(int i=0;i<a.Length;++i)for(int c=0;c<4;++c)max=Math.Max(max,Mathf.Abs(a[i][c]-b[i][c]));return max;}
        static void Finite(params Color[][]frames){foreach(var frame in frames)foreach(var pixel in frame)for(int c=0;c<4;++c)Assert.That(!float.IsNaN(pixel[c])&&!float.IsInfinity(pixel[c]),Is.True);}
        void Save(string name,Color[] pixels)
        {using(var fs=File.Create(Path.Combine(folder,name+".rgba-f32.gz")))using(var gzip=new GZipStream(fs,System.IO.Compression.CompressionLevel.Optimal))using(var writer=new BinaryWriter(gzip))foreach(var p in pixels){writer.Write(p.r);writer.Write(p.g);writer.Write(p.b);writer.Write(p.a);}}
        [Test]public void G4MotionInputBridge_DirectedRealGeneratedPass_ControlledPrevMatrixFiniteVisibleResponseRepeat()
        {
            bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try
            {
                // ONLY replace uniform sources for the same official Motion formula.
                string uniform="\nfloat4x4 NBFX_TestPreviousModel;float4x4 NBFX_TestCurrentVP;float4x4 NBFX_TestPreviousVP;float4 NBFX_TestMotionParams;\n#undef UNITY_PREV_MATRIX_M\n#define UNITY_PREV_MATRIX_M NBFX_TestPreviousModel\n#define _NonJitteredViewProjMatrix NBFX_TestCurrentVP\n#define _PrevViewProjMatrix NBFX_TestPreviousVP\n#define unity_MotionVectorsParams NBFX_TestMotionParams\n";
                string originalName="Shader \"NB FX/Shader Graph/NBShaderGraph\"";string controlName="Shader \"Hidden/NBFX_MotionUniformControl_"+Guid.NewGuid().ToString("N")+"\"";
                int Count(string value)=>System.Text.RegularExpressions.Regex.Matches(generated,System.Text.RegularExpressions.Regex.Escape(value)).Count;
                Assert.That(Count(originalName),Is.EqualTo(1));int motionIncludes=Count("Name \"MotionVectors\"")+Count("Name \"XRMotionVectors\"");Assert.That(motionIncludes,Is.GreaterThanOrEqualTo(1));Assert.That(Count(IncludeLine),Is.EqualTo(motionIncludes),"Only exact actual motion pass includes may get uniform sources");
                string source=generated.Replace(originalName,controlName).Replace(IncludeLine,uniform+IncludeLine);
                Assert.That(source.Replace(uniform+IncludeLine,IncludeLine).Replace(controlName,originalName),Is.EqualTo(generated),"Every other generated byte and motion formula unchanged");
                Assert.That(source,Is.Not.EqualTo(generated));var shader=Keep(ShaderUtil.CreateShaderAsset(source,false));Assert.That(shader&&shader.isSupported,Is.True);File.WriteAllText(Path.Combine(folder,"generated-motion-uniform-control.shader"),source);
                var mesh=Keep(new Mesh());mesh.vertices=new[]{new Vector3(-.8f,-.8f,0),new Vector3(.8f,-.8f,0),new Vector3(-.8f,.8f,0),new Vector3(.8f,.8f,0)};mesh.triangles=new[]{0,2,1,2,3,1};mesh.normals=Enumerable.Repeat(Vector3.back,4).ToArray();mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,1),4).ToArray();mesh.colors=Enumerable.Repeat(Color.white,4).ToArray();for(int channel=0;channel<8;++channel)mesh.SetUVs(channel,Enumerable.Repeat(new Vector4(.14f,.78f,.31f,.42f),4).ToList());mesh.RecalculateBounds();var uv4=new List<Vector4>();mesh.GetUVs(4,uv4);Assert.That(uv4,Is.EqualTo(Enumerable.Repeat(new Vector4(.14f,.78f,.31f,.42f),4).ToArray()));
                var actual=Configure(actualGraph);var controlled=Configure(shader);var rt=Keep(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear));rt.Create();var read=Keep(new Texture2D(Size,Size,TextureFormat.RGBAFloat,false,true));var start=Matrix4x4.Translate(new Vector3(-.35f,0,0));var moved=Matrix4x4.Translate(new Vector3(.35f,0,0));
                var color=Draw(actual,mesh,rt,read,start,start,false);var actualMotion=Draw(actual,mesh,rt,read,start,start,true);var stationary=Draw(controlled,mesh,rt,read,start,start,true,true);var moving=Draw(controlled,mesh,rt,read,moved,start,true,true);var repeat=Draw(controlled,mesh,rt,read,moved,start,true,true);var negative=Draw(controlled,mesh,rt,read,start,moved,true,true);
                foreach(var pair in new[]{Tuple.Create("actualGraph-visible",color),Tuple.Create("actualGraph-motion-entry-compile-draw",actualMotion),Tuple.Create("stationary-negative-control",stationary),Tuple.Create("moving-positive",moving),Tuple.Create("moving-repeat",repeat),Tuple.Create("moving-negative",negative)})Save(pair.Item1,pair.Item2);Finite(color,actualMotion,stationary,moving,repeat,negative);
                int visible=color.Count(p=>Math.Max(p.r,Math.Max(p.g,p.b))>.001f),movingPixels=moving.Count(p=>Math.Max(Mathf.Abs(p.r),Mathf.Abs(p.g))>.001f);float response=Delta(stationary,moving),repeatError=Delta(moving,repeat),zero=stationary.Max(p=>Math.Max(Mathf.Abs(p.r),Mathf.Abs(p.g)));float positive=moving.Max(p=>p.r),negativeValue=negative.Min(p=>p.r);
                File.WriteAllText(Path.Combine(folder,"metrics.json"),"{\"scope\":\"actual generated MotionVectors entry, controlled previous uniform matrices; not automatic history\",\"visible\":"+visible+",\"movingPixels\":"+movingPixels+",\"response\":"+response+",\"repeat\":"+repeatError+",\"stationaryRG\":"+zero+",\"positiveR\":"+positive+",\"negativeR\":"+negativeValue+"}");
                Assert.That(visible,Is.GreaterThan(32));Assert.That(movingPixels,Is.GreaterThan(32));Assert.That(response,Is.GreaterThan(.01f));Assert.That(repeatError,Is.Zero);Assert.That(zero,Is.Zero,"Stationary Motion negative control is intentionally zero, separate from real color visibility");Assert.That(positive,Is.GreaterThan(.01f));Assert.That(negativeValue,Is.LessThan(-.01f));
                Assert.That(ShaderUtil.GetShaderMessages(actualGraph).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),Is.False);Assert.That(ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),Is.False);
            }
            finally{ShaderUtil.allowAsyncCompilation=async;}
        }
        [TearDown]public void Cleanup(){foreach(var o in owned.AsEnumerable().Reverse())if(o){if(o is RenderTexture t)t.Release();Object.DestroyImmediate(o);}owned.Clear();}
    }
}
