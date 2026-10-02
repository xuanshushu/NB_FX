using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphCustomLocalGeometryTests
    {
        readonly List<Object> owned=new List<Object>();
        [OneTimeSetUp] public void Warm()=>new G4GraphVATTests().ImportGraph();
        static IEnumerable<TestCaseData> Cases()
        {foreach(bool ty in new[]{false,true})foreach(bool negative in new[]{false,true})foreach(bool shadow in new[]{false,true})foreach(bool ortho in new[]{true,false})yield return new TestCaseData(ty,negative,shadow,ortho).SetName("G4CustomLocalGeometry_"+(ty?"tyrelative":"houdini")+(negative?"_negative":"_positive")+(shadow?"_shadow":"_depth")+(ortho?"_ortho":"_perspective"));}
        Texture2D Map(Func<int,int,Color> pixel)
        {var t=new Texture2D(8,8,TextureFormat.RGBAHalf,false,true);owned.Add(t);for(int y=0;y<8;y++)for(int x=0;x<8;x++)t.SetPixel(x,y,pixel(x,y));t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.Apply(false);return t;}
        [TestCaseSource(nameof(Cases))]
        public void CustomSpaceAndVAT_SelectedActualDepthShadowABC(bool ty,bool negative,bool shadow,bool ortho)
        {
            var fixture=new G4GraphVATTests{GeometryCaseId="customlocal-"+(ty?"tyrelative":"houdini")+(negative?"-negative":"-positive")};
            var matrix=Matrix4x4.TRS(shadow?new Vector3(.125f,1,.125f):new Vector3(.125f,0,2),Quaternion.Euler(0,17,11),new Vector3(negative?-1.25f:1.25f,.75f,1.5f));
            var tyMap=Map((x,y)=>new Color((y>=4?.1875f:.0625f),.0625f,.0625f,1));
            fixture.GeometryMeshSetup=mesh=>
            {
                var raw=mesh.vertices;var n=mesh.normals;var t=mesh.tangents;var location=Matrix4x4.TRS(shadow?new Vector3(0,1,0):new Vector3(0,0,2),Quaternion.identity,Vector3.one);
                for(int i=0;i<raw.Length;i++){raw[i]=location.MultiplyPoint3x4(raw[i]);}
                mesh.vertices=raw;mesh.normals=n;mesh.tangents=t;
                // A world-space simulation host supplies its own conservative
                // bounds. Cover on/off poses to avoid mesh-local culling bias.
                mesh.bounds=new Bounds(Vector3.zero,Vector3.one*20);
                if(ty)mesh.SetUVs(1,Enumerable.Range(0,4).Select(i=>new Vector4(i,4,0,0)).ToList());
            };
            fixture.GeometrySetup=(m,graph)=>
            {
                if(graph)for(int row=0;row<4;row++){m.SetVector("_NB_CustomLocalToWorld"+row,matrix.GetRow(row));m.SetVector("_NB_CustomWorldToLocal"+row,matrix.inverse.GetRow(row));}
                else{m.SetMatrix("_CustomLocalTransformLocalToWorld",matrix);m.SetMatrix("_CustomLocalTransformWorldToLocal",matrix.inverse);}
                if(ty){m.SetFloat("_VATMode",1);m.SetFloat("_TyFlowVATSubMode",1);m.SetTexture("_VATTex",tyMap);m.SetFloat("_ImportScale",1);m.SetFloat("_Frames",2);m.SetFloat("_Autoplay",0);m.SetFloat("_FrameInterpolation",0);m.SetFloat("_Loop",0);m.SetFloat("_RGBAEncoded",0);m.SetFloat("_LinearToGamma",0);}
            };
            fixture.GeometryFrameState=(m,graph,frame)=>
            {
                bool enabled=frame>0;m.SetFloat("_VAT_Toggle",1);m.SetFloat(ty?"_Frame":"_displayFrame",ty?Mathf.Max(frame-1,0):Mathf.Max(frame,1));
                if(graph){m.SetFloat("_NB_CustomLocalTransform",enabled?1:0);return;}
                if(enabled)m.EnableKeyword("_CUSTOM_LOCAL_TRANSFORM");else m.DisableKeyword("_CUSTOM_LOCAL_TRANSFORM");m.EnableKeyword("_VAT");
                m.DisableKeyword("_HOUDINI_VAT_SOFTBODY");m.DisableKeyword("_VAT_HOUDINI");m.DisableKeyword("_VAT_TYFLOW");m.DisableKeyword("_TYFLOW_VAT_ABSOLUTE");m.DisableKeyword("_TYFLOW_VAT_RELATIVE");
                m.EnableKeyword(ty?"_VAT_TYFLOW":"_VAT_HOUDINI");m.EnableKeyword(ty?"_TYFLOW_VAT_RELATIVE":"_HOUDINI_VAT_SOFTBODY");
            };
            try{typeof(G4GraphVATTests).GetMethod("CaptureVATDepthAndShadowGeometry",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fixture,new object[]{shadow,ortho,true});}
            catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
            finally{foreach(var t in owned)if(t)Object.DestroyImmediate(t);owned.Clear();}
        }
    }
}
