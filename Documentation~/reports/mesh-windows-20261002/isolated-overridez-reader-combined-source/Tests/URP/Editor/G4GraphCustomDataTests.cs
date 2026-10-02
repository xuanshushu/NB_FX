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
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphCustomDataTests
    {
        const string GraphPath="Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath="Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath="Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const BindingFlags Static=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        Shader graphShader,legacyShader,frozenShader;
        Type flagsType,componentType;
        readonly List<Object> owned=new List<Object>();

        [OneTimeSetUp] public void WarmActualGraph()
        {
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            graphShader=AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            legacyShader=AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            frozenShader=AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            flagsType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShader.NBShaderFlags",false)).First(t=>t!=null);
            componentType=flagsType.GetNestedType("CutomDataComponent",BindingFlags.Public|BindingFlags.NonPublic);
            Assert.That(componentType,Is.Not.Null);
        }
        [TearDown] public void Cleanup()
        {for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();}
        static IEnumerable<TestCaseData> WordPositions()
        {
            for(int word=0;word<4;word++)for(int nibble=0;nibble<8;nibble++)
                yield return new TestCaseData(word,nibble).SetName("G4CustomData_Storage_w"+word+"_n"+nibble);
        }
        Material NewMaterial(Shader shader)
        {var material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(material);return material;}
        static string Prefix(int word)=>"_NB_CustomDataFlag"+word;
        static uint ReadWord(Material material,int word)
        {
            string prefix=Prefix(word);
            uint lo=(uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix+"Lo16"),0,65535));
            uint hi=(uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix+"Hi16"),0,65535));return lo|(hi<<16);
        }
        static void SetWord(Material material,int word,uint value)
        {string prefix=Prefix(word);material.SetFloat(prefix+"Lo16",value&65535);material.SetFloat(prefix+"Hi16",value>>16);}
        static uint ReadLegacyWord(Material material,int word)=>unchecked((uint)material.GetInteger("_W9ParticleCustomDataFlag"+word));
        object Flags(Material material)=>Activator.CreateInstance(flagsType,new object[]{material});
        object Call(object target,string method,params object[] args)=>flagsType.GetMethod(method,Instance).Invoke(target,args);

        [TestCaseSource(nameof(WordPositions))]
        public void FourCustomWords_ExistingSetterReaderPreserveEveryUnownedNibble(int word,int nibble)
        {
            var graph=NewMaterial(graphShader);var legacy=NewMaterial(legacyShader);
            uint[] original={0xE1234567u,0xC89ABCDFu,0x8FEDCBA9u,0xBA987654u};
            for(int w=0;w<4;w++)
            {
                string prefix=Prefix(w);int lo=graphShader.FindPropertyIndex(prefix+"Lo16"),hi=graphShader.FindPropertyIndex(prefix+"Hi16");
                Assert.That(lo,Is.GreaterThanOrEqualTo(0));Assert.That(hi,Is.GreaterThanOrEqualTo(0));
                Assert.That(graphShader.GetPropertyType(lo),Is.EqualTo(ShaderPropertyType.Float));Assert.That(graphShader.GetPropertyType(hi),Is.EqualTo(ShaderPropertyType.Float));
                Assert.That(graph.HasProperty("_W9ParticleCustomDataFlag"+w),Is.False,"No shadow legacy integer protocol on Graph");
                SetWord(graph,w,original[w]);legacy.SetInteger("_W9ParticleCustomDataFlag"+w,unchecked((int)original[w]));
            }
            var graphFlags=Flags(graph);var legacyFlags=Flags(legacy);
            uint mask=15u<<(nibble*4);int[] codes={0,15,14,13,12,11,10,9,8};
            var keywords=graph.shaderKeywords.OrderBy(k=>k).ToArray();int queue=graph.renderQueue;
            float surface=graph.GetFloat("_Surface"),version=graph.GetFloat("_NB_GraphGUIStateVersion");
            for(int enumValue=0;enumValue<codes.Length;enumValue++)
            {
                object component=Enum.ToObject(componentType,enumValue);uint before=ReadWord(graph,word);
                Call(graphFlags,"SetCustomDataFlag",component,nibble*4,word);Call(legacyFlags,"SetCustomDataFlag",component,nibble*4,word);
                uint expected=(before&~mask)|unchecked((uint)codes[enumValue])<<(nibble*4);
                Assert.That(ReadWord(graph,word),Is.EqualTo(expected));Assert.That(ReadLegacyWord(legacy,word),Is.EqualTo(expected));
                Assert.That(Convert.ToInt32(Call(graphFlags,"GetCustomDataFlag",nibble*4,word)),Is.EqualTo(enumValue));
                Assert.That(Convert.ToInt32(Call(legacyFlags,"GetCustomDataFlag",nibble*4,word)),Is.EqualTo(enumValue));
                for(int w=0;w<4;w++)if(w!=word){Assert.That(ReadWord(graph,w),Is.EqualTo(original[w]));Assert.That(ReadLegacyWord(legacy,w),Is.EqualTo(original[w]));}
                Assert.That(graph.GetFloat(Prefix(word)+"Lo16"),Is.InRange(0f,65535f));Assert.That(graph.GetFloat(Prefix(word)+"Hi16"),Is.InRange(0f,65535f));
                Assert.That(graph.shaderKeywords.OrderBy(k=>k).ToArray(),Is.EqualTo(keywords));Assert.That(graph.renderQueue,Is.EqualTo(queue));
                Assert.That(graph.GetFloat("_Surface"),Is.EqualTo(surface));Assert.That(graph.GetFloat("_NB_GraphGUIStateVersion"),Is.EqualTo(version));
                Assert.That(Call(graphFlags,"IsCustomDataOn"),Is.EqualTo(Call(legacyFlags,"IsCustomDataOn")));
                Assert.That(Call(graphFlags,"IsCustomData1On"),Is.EqualTo(Call(legacyFlags,"IsCustomData1On")));
                Assert.That(Call(graphFlags,"IsCustomData2On"),Is.EqualTo(Call(legacyFlags,"IsCustomData2On")));
            }
        }

        [Serializable] sealed class GPUMetrics
        {
            public string consumer,api,unityVersion,scope;
            public bool orthographic,varying,finite;
            public float[] abMax,bcMax;
            public float aResponse,bResponse,cResponse,aRepeat,bRepeat,cRepeat;
            public int aVisible,bVisible,cVisible;
        }
        static readonly string[][] ConsumerNames={
            new[]{"base-x","base-y","dissolve-strength","hue","mask-x","mask-y","fresnel","chromatic"},
            new[]{"dissolve-x","dissolve-y","noise-intensity","saturation","vertex-x","vertex-y","vertex-intensity","dissolve-mask"},
            new[]{"simple-x","simple-y","voronoi-x","voronoi-y","noise-direction-x","noise-direction-y","contrast","vat-frame"},
            new[]{"vertex-mask-x","vertex-mask-y","shared-x","shared-y","emission-x","emission-y","overlay-x","overlay-y"}};
        static IEnumerable<TestCaseData> SurfaceConsumers()
        {
            for(int word=0;word<4;word++)for(int nibble=0;nibble<8;nibble++)
            {
                string name=ConsumerNames[word][nibble];
                if(name=="chromatic")continue; // Prior native Frozen-A crashes retained; do not repeatedly replay this branch.
                foreach(bool ortho in new[]{true,false})foreach(bool varying in new[]{false,true})
                    yield return new TestCaseData(word,nibble,name,ortho,varying)
                        .SetName("G4CustomData_GPU_"+name+(ortho?"_ortho":"_perspective")+(varying?"_varying":"_constant"));
            }
        }
        static void Flag(Material material,bool graph,int word,uint bits)
        {
            if(graph)
            {material.SetFloat("_NB_Flags"+word+"Lo16",bits&65535);material.SetFloat("_NB_Flags"+word+"Hi16",bits>>16);}
            else material.SetInteger(word==0?"_W9ParticleShaderFlags":"_W9ParticleShaderFlags1",unchecked((int)bits));
        }
        uint Bit(string name)=>unchecked((uint)(int)flagsType.GetField(name,Static).GetValue(null));
        Texture2D Map(bool alpha,bool twoDimensionalRed=false)
        {
            var texture=new Texture2D(32,32,TextureFormat.RGBAHalf,false,true);owned.Add(texture);
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float u=x/31f,v=y/31f;
                float red=twoDimensionalRed?.15f+.3f*u+.35f*v:.15f+.65f*u;
                texture.SetPixel(x,y,new Color(red,.2f+.6f*v,.25f+.5f*(u+v)*.5f,alpha?.2f+.6f*(u+v)*.5f:1));
            }
            texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Bilinear;texture.Apply(false);return texture;
        }
        Texture2D Constant(Color value)
        {var texture=new Texture2D(2,2,TextureFormat.RGBAHalf,false,true);owned.Add(texture);for(int y=0;y<2;y++)for(int x=0;x<2;x++)texture.SetPixel(x,y,value);texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;texture.Apply(false);return texture;}
        void ConfigureConsumer(Material material,bool graph,string name,Texture2D map,Texture2D noise,Texture2D mask)
        {
            string kind=name.StartsWith("mask-")?"mask":name.StartsWith("emission-")?"emission":name.StartsWith("overlay-")?"overlay2":name.StartsWith("dissolve")?"dissolve":"base";
            typeof(G4GraphTextureNoiseTests).GetMethod("Configure",Static).Invoke(null,new object[]{material,graph,kind,map,noise,mask});
            material.SetFloat("_noisemapEnabled",0);material.SetFloat("_noiseMaskMap_Toggle",0);
            for(int word=0;word<4;word++){if(graph)SetWord(material,word,0);else material.SetInteger("_W9ParticleCustomDataFlag"+word,0);}
            Flag(material,graph,0,0);Flag(material,graph,1,0);
            material.SetShaderPassEnabled("UniversalForward",true);material.SetShaderPassEnabled("SRPDefaultUnlit",graph);
            material.SetShaderPassEnabled("SRPDEFAULTUNLIT",graph);material.SetShaderPassEnabled("DepthNormalsOnly",false);
            if(name.StartsWith("shared-"))
            {
                material.SetVector("_SharedUV_ST",new Vector4(.75f,1.25f,.125f,.25f));material.SetVector("_SharedUV_Vec",Vector4.zero);
                if(graph){material.SetFloat("_NB_UVModeFlag0Lo16",0);material.SetFloat("_NB_UVModeFlagType0Lo16",2);}
                else{material.SetInteger("_UVModeFlag0",0);material.SetInteger("_UVModeFlagType0",2);material.EnableKeyword("_SHARED_UV");}
            }
            if(name=="hue"||name=="saturation"||name=="contrast")
            {
                if(name=="contrast")Flag(material,graph,1,Bit("FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST"));
                else Flag(material,graph,0,Bit(name=="hue"?"FLAG_BIT_HUESHIFT_ON":"FLAG_BIT_SATURABILITY_ON"));
                material.SetFloat("_HueShift",.03125f);material.SetFloat("_Saturability",.25f);material.SetFloat("_Contrast",.25f);
                material.SetColor("_ContrastMidColor",new Color(.5f,.5f,.5f,1));
            }
            if(name.StartsWith("noise-"))
            {
                material.SetFloat("_noisemapEnabled",1);material.SetFloat("_NoiseIntensity",.125f);
                material.SetFloat("_TexDistortion_intensity",.5f);material.SetVector("_DistortionDirection",new Vector4(.25f,.375f,0,0));
                if(!graph)material.EnableKeyword("_NOISEMAP");
            }
            if(name.StartsWith("dissolve"))
            {
                material.SetVector("_Dissolve",new Vector4(.0625f,1,.25f,.5f));
                if(name!="dissolve-strength")material.SetVector("_Dissolve",new Vector4(.5f,1,.25f,.25f));
                material.SetFloat("_DissolveMaskMode",1);material.SetTexture("_DissolveMaskMap",mask);
                material.SetFloat("_DissolveMask_Toggle",name=="dissolve-mask"?1:0);
                if(!graph&&name=="dissolve-mask")material.EnableKeyword("_DISSOLVE_MASK");
            }
            if(name=="fresnel")
            {
                material.SetFloat("_fresnelEnabled",1);material.SetVector("_FresnelUnit",new Vector4(.125f,2,.5f,0));
                material.SetColor("_FresnelColor",new Color(.2f,.6f,.8f,1));material.SetVector("_FresnelRotation",Vector4.zero);
                if(!graph)material.EnableKeyword("_FRESNEL");
            }
            if(name.StartsWith("vertex-"))
            {
                typeof(G4GraphVertexOffsetTests).GetMethod("Configure",Static).Invoke(null,new object[]{material,graph,map,mask});
                material.SetFloat("_VertexOffset_NormalDir_Toggle",3);material.SetVector("_VertexOffset_CustomDir",new Vector4(1,0,0,0));
                material.SetVector("_VertexOffset_Vec",new Vector4(0,0,.2f,0));
                material.SetFloat("_VertexOffset_Mask_Toggle",1);material.SetVector("_VertexOffset_MaskMap_Vec",new Vector4(0,0,1,0));
                Flag(material,graph,1,Bit("FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO"));
                if(!graph){material.EnableKeyword("_VERTEX_OFFSET");material.EnableKeyword("_VERTEX_OFFSET_MASKMAP");}
            }
            if(name.StartsWith("simple-")||name.StartsWith("voronoi-"))
            {
                material.SetFloat("_Mask_Toggle",1);material.SetTexture("_MaskMap",Texture2D.whiteTexture);
                material.SetFloat("_MaskPNoiseBlendOpacity",.75f);
                material.SetFloat("_ProgramNoise_Toggle",1);material.SetFloat("_ProgramNoise_Simple_Toggle",name.StartsWith("simple-")?1:0);
                material.SetFloat("_ProgramNoise_Voronoi_Toggle",name.StartsWith("voronoi-")?1:0);
                material.SetFloat("_ProgramNoiseBaseBlendOpacity",.75f);material.SetFloat("_ProgramNoise_Rotate",0);
                material.SetVector("_DissolveVoronoi_Vec",new Vector4(2,3,2,3));material.SetVector("_DissolveVoronoi_Vec2",new Vector4(1,1,0,0));
                material.SetVector("_DissolveVoronoi_Vec3",Vector4.zero);material.SetVector("_DissolveVoronoi_Vec4",Vector4.zero);
                if(graph)material.SetFloat("_NB_PNoiseBlendLo16",1u|(1u<<3));else{material.SetInteger("_W9ParticleShaderPNoiseBlendFlag",1|(1<<3));material.EnableKeyword("_MASKMAP_ON");material.EnableKeyword("_PROGRAM_NOISE");material.EnableKeyword(name.StartsWith("simple-")?"_PROGRAM_NOISE_SIMPLE":"_PROGRAM_NOISE_VORONOI");}
            }
            if(name=="vat-frame")
            {
                var pos=(Texture2D)typeof(G4GraphVATTests).GetMethod("PositionMap",Static).Invoke(null,null);owned.Add(pos);
                var rot=Constant(new Color(.5f,.5f,.5f,1));var second=Constant(Color.clear);
                typeof(G4GraphVATTests).GetMethod("Configure",Static).Invoke(null,new object[]{material,graph,"manual1",map,pos,second,rot,map,map,map});
                material.SetFloat("_VAT_Toggle",1);Flag(material,graph,1,Bit("FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM"));
                if(!graph){material.EnableKeyword("_VAT");material.EnableKeyword("_VAT_HOUDINI");material.EnableKeyword("_HOUDINI_VAT_SOFTBODY");}
            }
        }
        [TestCaseSource(nameof(SurfaceConsumers))]
        public void CustomDataFunctionalConsumerABC(int word,int nibble,string name,bool ortho,bool varying)
        {
            const int size=96;
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXCD"),name+(ortho?"-ortho":"-perspective")+(varying?"-varying":"-constant"));Directory.CreateDirectory(folder);
            var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;var data=pipeline.rendererDataList[0];
            string rendererFile=Path.Combine(Path.GetDirectoryName(Application.dataPath),AssetDatabase.GetAssetPath(data));byte[] rendererBefore=File.ReadAllBytes(rendererFile);
            var nb=data.rendererFeatures.FirstOrDefault(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");Assert.That(nb,Is.Not.Null);bool oldNB=nb.isActive;
            var a=NewMaterial(frozenShader);var b=NewMaterial(legacyShader);var c=NewMaterial(graphShader);
            bool twoDimensional=name=="mask-y"||name=="vertex-mask-y"||name.StartsWith("dissolve");
            var map=Map(name.StartsWith("dissolve"),twoDimensional);var noise=Constant(new Color(.625f,.375f,0,1));var mask=Map(true,twoDimensional);
            var scene=EditorSceneManager.NewPreviewScene();var go=GameObject.CreatePrimitive(PrimitiveType.Quad);var cameraObject=new GameObject("CustomData controlled Mesh camera");
            var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var mesh=Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);owned.Add(mesh);
            var target=new RenderTexture(size,size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);var read=new Texture2D(size,size,TextureFormat.RGBAHalf,false,true);
            RenderTexture old=RenderTexture.active;
            try
            {
                SceneManager.MoveGameObjectToScene(go,scene);SceneManager.MoveGameObjectToScene(cameraObject,scene);go.layer=4;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.transform.localScale=new Vector3(2,2,1);go.transform.position=new Vector3(0,0,2);
                Vector4 cd1=new Vector4(.125f,.25f,.5f,.75f),cd2=new Vector4(.25f,.375f,.625f,.875f);
                if(name=="vat-frame"){cd1=new Vector4(0,.25f,.75f,1);cd2=new Vector4(1,.75f,.25f,0);}
                mesh.SetUVs(1,Enumerable.Range(0,4).Select(i=>varying?cd1+new Vector4(.03125f*i,.015625f*i,-.03125f*i,-.015625f*i):cd1).ToList());
                mesh.SetUVs(2,Enumerable.Range(0,4).Select(i=>varying?cd2+new Vector4(-.015625f*i,.03125f*i,.015625f*i,-.03125f*i):cd2).ToList());
                if(name=="vat-frame")mesh.SetUVs(0,new List<Vector4>{new Vector4(0,0,.2f,.66f),new Vector4(1,0,.8f,.66f),new Vector4(0,1,.2f,.86f),new Vector4(1,1,.8f,.86f)});
                var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
                ConfigureConsumer(a,false,name,map,noise,mask);ConfigureConsumer(b,false,name,map,noise,mask);ConfigureConsumer(c,true,name,map,noise,mask);
                camera.scene=scene;camera.orthographic=ortho;camera.orthographicSize=1.5f;camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=20;
                camera.transform.position=new Vector3(0,0,5);camera.transform.rotation=Quaternion.Euler(0,180,0);camera.cullingMask=1<<4;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=true;camera.allowMSAA=false;camera.targetTexture=target;
                target.Create();Assert.That(target.IsCreated()&&!target.sRGB,Is.True);nb.SetActive(false);
                Color[] Snap(Material material,string label)
                {
                    renderer.sharedMaterial=material;for(int i=0;i<3;i++)camera.Render();RenderTexture.active=target;
                    read.ReadPixels(new Rect(0,0,size,size),0,0,false);read.Apply(false,false);var pixels=read.GetPixels();
                    using(var stream=File.Create(Path.Combine(folder,label+".rgba-f32.gz")))
                    using(var zip=new System.IO.Compression.GZipStream(stream,System.IO.Compression.CompressionLevel.Optimal))
                    using(var writer=new BinaryWriter(zip))foreach(var px in pixels){writer.Write(px.r);writer.Write(px.g);writer.Write(px.b);writer.Write(px.a);}return pixels;
                }
                float Delta(Color[] x,Color[] y){float max=0;for(int i=0;i<x.Length;i++)for(int channel=0;channel<4;channel++)max=Mathf.Max(max,Mathf.Abs(x[i][channel]-y[i][channel]));return max;}
                bool Finite(Color[] frame)=>frame.All(px=>Enumerable.Range(0,4).All(ch=>!float.IsNaN(px[ch])&&!float.IsInfinity(px[ch])));
                int Visible(Color[] frame)=>frame.Count(px=>Mathf.Abs(px.r)+Mathf.Abs(px.g)+Mathf.Abs(px.b)+Mathf.Abs(px.a)>.01f);
                int[] codes={0,15,14,13,12,11,10,9,8};var frames=new Color[codes.Length][][];
                var metrics=new GPUMetrics{consumer=name,orthographic=ortho,varying=varying,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,
                    scope="Controlled existing custom-data nibble consumer, Mesh TEXCOORD1/2; all9 normal selector codes; no complete combinations/VFX/Player claim",abMax=new float[codes.Length],bcMax=new float[codes.Length],finite=true};
                for(int i=0;i<codes.Length;i++)
                {
                    uint value=unchecked((uint)codes[i])<<(nibble*4);SetWord(c,word,value);
                    a.SetInteger("_W9ParticleCustomDataFlag"+word,unchecked((int)value));b.SetInteger("_W9ParticleCustomDataFlag"+word,unchecked((int)value));
                    frames[i]=new[]{Snap(a,"A-code"+codes[i]),Snap(b,"B-code"+codes[i]),Snap(c,"C-code"+codes[i])};
                    metrics.abMax[i]=Delta(frames[i][0],frames[i][1]);metrics.bcMax[i]=Delta(frames[i][1],frames[i][2]);metrics.finite&=frames[i].All(Finite);
                    metrics.aResponse=Mathf.Max(metrics.aResponse,Delta(frames[0][0],frames[i][0]));metrics.bResponse=Mathf.Max(metrics.bResponse,Delta(frames[0][1],frames[i][1]));metrics.cResponse=Mathf.Max(metrics.cResponse,Delta(frames[0][2],frames[i][2]));
                }
                metrics.aRepeat=Delta(frames[codes.Length-1][0],Snap(a,"A-repeat"));metrics.bRepeat=Delta(frames[codes.Length-1][1],Snap(b,"B-repeat"));metrics.cRepeat=Delta(frames[codes.Length-1][2],Snap(c,"C-repeat"));
                metrics.aVisible=frames.Max(s=>Visible(s[0]));metrics.bVisible=frames.Max(s=>Visible(s[1]));metrics.cVisible=frames.Max(s=>Visible(s[2]));
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(metrics,true));Debug.Log("NBFX_CD_GPU "+JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite,Is.True);Assert.That(metrics.abMax.All(v=>v==0)&&metrics.bcMax.All(v=>v==0),Is.True,"Full-frame strict ABC");
                Assert.That(metrics.aRepeat+metrics.bRepeat+metrics.cRepeat,Is.Zero);Assert.That(metrics.aVisible,Is.GreaterThan(128));Assert.That(metrics.bVisible,Is.GreaterThan(128));Assert.That(metrics.cVisible,Is.GreaterThan(128));
                Assert.That(metrics.aResponse,Is.GreaterThan(.001f));Assert.That(metrics.bResponse,Is.GreaterThan(.001f));Assert.That(metrics.cResponse,Is.GreaterThan(.001f));
            }
            finally
            {nb.SetActive(oldNB);camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(read);EditorSceneManager.ClosePreviewScene(scene);Assert.That(File.ReadAllBytes(rendererFile),Is.EqualTo(rendererBefore));}
        }
    }
}
