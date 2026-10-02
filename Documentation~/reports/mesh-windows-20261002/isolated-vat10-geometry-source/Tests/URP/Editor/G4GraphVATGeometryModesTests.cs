using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphVATGeometryModesTests
    {
        readonly List<Object> owned=new List<Object>();
        [OneTimeSetUp] public void Warm()=>new G4GraphVATTests().ImportGraph();
        static IEnumerable<TestCaseData> Cases()
        {foreach(bool ty in new[]{false,true})for(int mode=0;mode<(ty?6:4);mode++)foreach(bool shadow in new[]{false,true})foreach(bool ortho in new[]{true,false})yield return new TestCaseData(ty,mode,shadow,ortho).SetName("G4VATGeometry_"+(ty?"t":"h")+mode+(shadow?"_shadow":"_depth")+(ortho?"_ortho":"_perspective"));}
        Texture2D Map(int size,Func<int,int,Color> pixel)
        {var t=new Texture2D(size,size,TextureFormat.RGBAHalf,false,true);owned.Add(t);for(int y=0;y<size;y++)for(int x=0;x<size;x++)t.SetPixel(x,y,pixel(x,y));t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.Apply(false);return t;}
        [TestCaseSource(nameof(Cases))]
        public void AllVATModes_SelectedActualDepthAndShadowABC(bool ty,int mode,bool shadow,bool ortho)
        {
            var fixture=new G4GraphVATTests{GeometryCaseId=(ty?"t":"h")+mode};var tyflow=new G4GraphTyflowVATTests();Mesh actualMesh=null;Texture2D vat=null;
            var build=typeof(G4GraphTyflowVATTests).GetMethod("BuildVAT",BindingFlags.Instance|BindingFlags.NonPublic);
            var lookup=ty?null:Map(8,(x,y)=>new Color(.08f+.12f*x,0,1-(.08f+.12f*y),0));
            fixture.GeometryMeshSetup=mesh=>
            {
                actualMesh=mesh;
                if(ty)
                {
                    for(int channel=1;channel<8;channel++)mesh.SetUVs(channel,mode<2?(channel==1?Enumerable.Range(0,4).Select(v=>new Vector4(v,4,0,0)).ToList():Enumerable.Repeat(Vector4.zero,4).ToList()):Enumerable.Repeat(new Vector4(channel-1,1f/7f,0,0),4).ToList());
                    vat=(Texture2D)build.Invoke(tyflow,new object[]{mode,mode<2?"raw":"deform7",mesh});
                }
                else
                {
                    mesh.SetUVs(0,new List<Vector4>{new Vector4(.12f,.25f,0,0),new Vector4(.88f,.25f,0,0),new Vector4(.12f,.9f,0,0),new Vector4(.88f,.9f,0,0)});
                    if(mode==3)mesh.SetUVs(1,Enumerable.Repeat(new Vector4(.4f,.7f,0,0),4).ToList());
                    mesh.SetUVs(2,Enumerable.Repeat(Vector4.zero,4).ToList());mesh.SetUVs(4,Enumerable.Repeat(new Vector4(0,1,0,0),4).ToList());
                }
            };
            fixture.GeometrySetup=(m,graph)=>
            {
                Assert.That(actualMesh,Is.Not.Null);m.SetFloat("_VATMode",ty?1:0);
                if(ty)
                {
                    m.SetTexture("_VATTex",vat);m.SetFloat("_TyFlowVATSubMode",mode);m.SetFloat("_ImportScale",1);m.SetFloat("_Frames",2);m.SetFloat("_Autoplay",0);m.SetFloat("_FrameInterpolation",0);m.SetFloat("_Loop",0);m.SetFloat("_LinearToGamma",0);m.SetFloat("_RGBAEncoded",0);m.SetFloat("_DeformingSkin",mode<2?0:1);m.SetFloat("_SkinBoneCount",mode<2?1:7);m.SetFloat("_VATIncludesNormals",0);
                }
                else
                {
                    m.SetFloat("_HoudiniVATSubMode",mode);m.SetTexture("_lookupTable",lookup);m.SetFloat("_B_interpolate",0);m.SetFloat("_animateFirstFrame",1);m.SetFloat("_globalPscaleMul",1);m.SetFloat("_B_pscaleAreInPosA",0);m.SetFloat("_widthBaseScale",.9f);m.SetFloat("_heightBaseScale",.8f);m.SetFloat("_B_hideOverlappingOrigin",0);m.SetFloat("_B_CAN_SPIN",0);m.SetFloat("_B_LOAD_POS_TWO_TEX",0);m.SetFloat("_B_UNLOAD_ROT_TEX",0);
                }
            };
            fixture.GeometryFrameState=(m,graph,frame)=>
            {
                bool enabled=frame>0;m.SetFloat("_VAT_Toggle",enabled?1:0);m.SetFloat(ty?"_Frame":"_displayFrame",ty?Mathf.Max(frame-1,0):frame);
                if(graph)return;
                foreach(string k in new[]{"_VAT","_VAT_TYFLOW","_VAT_HOUDINI","_HOUDINI_VAT_SOFTBODY","_HOUDINI_VAT_RIGIDBODY","_HOUDINI_VAT_DYNAMIC_REMESH","_HOUDINI_VAT_PARTICLE_SPRITE","_TYFLOW_VAT_ABSOLUTE","_TYFLOW_VAT_RELATIVE","_TYFLOW_VAT_SKIN_R","_TYFLOW_VAT_SKIN_PR","_TYFLOW_VAT_SKIN_PRSAVE","_TYFLOW_VAT_SKIN_PRSXYZ"})m.DisableKeyword(k);
                if(enabled){m.EnableKeyword("_VAT");m.EnableKeyword(ty?"_VAT_TYFLOW":"_VAT_HOUDINI");m.EnableKeyword(ty?new[]{"_TYFLOW_VAT_ABSOLUTE","_TYFLOW_VAT_RELATIVE","_TYFLOW_VAT_SKIN_R","_TYFLOW_VAT_SKIN_PR","_TYFLOW_VAT_SKIN_PRSAVE","_TYFLOW_VAT_SKIN_PRSXYZ"}[mode]:new[]{"_HOUDINI_VAT_SOFTBODY","_HOUDINI_VAT_RIGIDBODY","_HOUDINI_VAT_DYNAMIC_REMESH","_HOUDINI_VAT_PARTICLE_SPRITE"}[mode]);}
            };
            var capture=typeof(G4GraphVATTests).GetMethod("CaptureVATDepthAndShadowGeometry",BindingFlags.Instance|BindingFlags.NonPublic);
            try {capture.Invoke(fixture,new object[]{shadow,ortho,true});}
            catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
            finally{tyflow.Cleanup();foreach(var t in owned)if(t)Object.DestroyImmediate(t);owned.Clear();}
        }
    }
}
