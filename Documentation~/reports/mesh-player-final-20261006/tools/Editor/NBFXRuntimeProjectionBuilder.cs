using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using NBShader;

namespace NBFX.PlayerValidation.Editor
{
    public static class NBFXRuntimeProjectionBuilder
    {
        static void Need(bool b,string m){if(!b)throw new InvalidOperationException(m);}
        static Type Type(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
        public static void Fill(NBFXRuntimeProjectionProbe probe,NBFXMeshPlayerConfig config,string staging)
        {
            probe.config=config;
            NBShaderFeatureRuntimeSettings Policy(string name,params string[] denied)
            {var s=ScriptableObject.CreateInstance<NBShaderFeatureRuntimeSettings>();s.lowAllowedKeywords=NBShaderFeatureCatalog.RawKeywords.Except(denied).ToArray();AssetDatabase.CreateAsset(s,staging+"/"+name+".asset");return s;}
            probe.allow=Policy("RuntimeAllow");probe.denyMask=Policy("RuntimeDenyMask","_MASKMAP_ON");probe.denyMaskAndOVZ=Policy("RuntimeDenyMaskOVZ","_MASKMAP_ON","_OVERRIDE_Z");
            var white=new Texture2D(2,2,TextureFormat.RGBAHalf,false,true);white.SetPixels(Enumerable.Repeat(Color.white,4).ToArray());white.Apply(false);AssetDatabase.CreateAsset(white,staging+"/RuntimeWhite.asset");
            var mask=new Texture2D(2,2,TextureFormat.RGBAHalf,false,true);mask.SetPixels(Enumerable.Repeat(new Color(.5f,.5f,.5f,.5f),4).ToArray());mask.Apply(false);AssetDatabase.CreateAsset(mask,staging+"/RuntimeHalfMask.asset");
            var configure=Type("NBFX.Baseline.Tests.G4GraphOverrideDepthTests").GetMethod("ConfigureBase",BindingFlags.Static|BindingFlags.NonPublic);Need(configure!=null,"Original OVZ input helper missing.");
            var initialize=Type("NBShaderEditor.NBShaderSyncService").GetMethod("TryInitializeGraphSupportedGateTierOnAssign",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);Need(initialize!=null,"Actual Graph initialization entry missing.");
            // Read the actual official enum by name: it is internal to URP Editor.
            float forceDepthWrite=Convert.ToSingle(Enum.Parse(Type("UnityEditor.Rendering.Universal.ShaderGraph.ZWriteControl"),"ForceEnabled"));
            Need(forceDepthWrite==1f,"Current official ForceEnabled enum contract changed.");
            Material Make(bool graph)
            {
                var m=new Material(graph?config.graphShader:config.currentShader);configure.Invoke(null,new object[]{m,graph,white});
                // Explicit ordinary Mesh/alpha/unlit intent before the real Runtime resolver.
                foreach(var input in new[]{Tuple.Create("_MeshSourceMode",1f),Tuple.Create("_TransparentMode",1f),Tuple.Create("_Blend",0f),Tuple.Create("_FxLightMode",0f),Tuple.Create("_TimeMode",0f)})if(m.HasProperty(input.Item1))m.SetFloat(input.Item1,input.Item2);
                m.SetFloat("_VAT_Toggle",0);m.SetFloat("_Mask_Toggle",1);m.SetTexture("_MaskMap",mask);m.SetFloat("_OverrideZ_Toggle",1);m.SetFloat("_OverrideZValue",7);m.SetFloat("_NBShaderFeatureTier",3);
                m.SetColor("_BaseColor",Color.red);m.SetColor("_Color",Color.red);m.SetFloat("_ZWrite",1);m.SetFloat("_ZTest",(float)CompareFunction.LessEqual);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_SrcBlendAlpha",1);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_AlphaClip",0);m.DisableKeyword("_ALPHATEST_ON");m.renderQueue=3000;
                if(graph){Need(m.HasProperty("_ZWriteControl"),"Official Graph depth-write control missing.");m.SetFloat("_ZWriteControl",forceDepthWrite);Need((bool)initialize.Invoke(null,new object[]{m}),"Real Graph schema initialization refused.");}
                NBShaderFeatureRuntime.ApplyTier(m,probe.allow,NBShaderFeatureTier.Low);Need(m.IsKeywordEnabled("_OVERRIDE_Z")&&(graph?m.GetFloat("_NB_TierAllowMask")==1:m.IsKeywordEnabled("_MASKMAP_ON")),"Explicit allow policy did not retain intended real consumers.");
                if(graph)Need(m.GetFloat("_NB_TierAllowMask")==1,"Graph gate not initialized.");
                AssetDatabase.CreateAsset(m,staging+"/"+(graph?"C":"B")+"-RuntimeMaskOVZ.mat");return m;
            }
            probe.native=Make(false);probe.graph=Make(true);
            probe.retainedVariants=new[]{probe.native,probe.graph}.SelectMany(m=>new[]{probe.allow,probe.denyMask,probe.denyMaskAndOVZ}.Select((s,i)=>{var v=new Material(m);NBShaderFeatureRuntime.ApplyTier(v,s,NBShaderFeatureTier.Low);AssetDatabase.CreateAsset(v,staging+"/"+(m==probe.graph?"C":"B")+"-RuntimeRetained-"+i+".mat");return v;})).ToArray();
            probe.graphProjectionProperties=((string[])typeof(NBShaderFeatureRuntime).GetField("GraphSupportedProjectionProperties",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)).ToArray();
            var green=new Material(Shader.Find("Universal Render Pipeline/Unlit"));green.SetColor("_BaseColor",Color.green);green.SetFloat("_Surface",1);green.SetFloat("_ZWrite",0);green.SetFloat("_ZTest",(float)CompareFunction.LessEqual);green.SetFloat("_Cull",0);green.SetFloat("_SrcBlend",1);green.SetFloat("_DstBlend",0);green.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");Need(green.HasProperty("_QueueOffset"),"Original URP probe queue offset missing.");green.SetFloat("_QueueOffset",1);green.renderQueue=3001;AssetDatabase.CreateAsset(green,staging+"/RuntimeDepthGreenProbe.mat");probe.greenProbe=green;
        }
    }
}
