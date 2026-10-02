using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // DN0 bounded default-pipeline coverage, not selected-pass geometry tests.
    // Actual Forward/Depth/Shadow remain available, Graph normals raw True,
    // SSAO stays active. The shared core owns serial URP drawing/restoration.
    public sealed class G4GraphDN0DefaultCoverageTests
    {
        readonly List<Object> owned = new List<Object>();
        G4GraphVATTests fixture;
        G4GraphTyflowVATTests tyflow;
        Material probeMaterial, writerMaterial;
        MeshRenderer probeRenderer, stencilWriter;
        string probeSource, writerSource;
        string currentVariant;
        MeshBasisProof meshProof;
        bool framingChanged;
        Light actualDirectionalLight;
        [Serializable] sealed class LightInputProof
        {
            public string type, shadows;
            public float intensity, shadowBias, shadowNormalBias;
            public Color color;
            public Vector3 eulerAngles, forward;
            public int cullingMask;
        }

        [Serializable] sealed class MeshBasisProof
        {
            public int vertices, originalTangentCount;
            public Vector4[] originalTangents, effectiveTangents;
            public Vector3[] originalNormals;
            public bool basisRepaired;
        }

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] Variants = {
            "softbody-normal-bias0", "softbody-clip-bias1",
            "tyabsolute-vatnormal-bias0", "tyrelative-clip-bias1",
            "customlocal-hpositive-bias0", "customlocal-tnegative-clip-bias1",
            "stencil-softbody-fail-zfail", "stencil-tyrelative-fail-zfail" };
        T Keep<T>(T value) where T : Object { owned.Add(value); return value; }

        [OneTimeSetUp]
        public void WarmAndCheckOriginalReflectionABI()
        {
            new G4GraphVATTests().ImportGraph();
            var old = typeof(G4GraphVATTests).GetMethod("CaptureVATDepthAndShadowGeometry", Private);
            Assert.That(old, Is.Not.Null);
            Assert.That(old.IsPrivate, Is.True);
            Assert.That(old.GetParameters().Select(p => p.ParameterType),
                Is.EqualTo(new[] {typeof(bool), typeof(bool), typeof(bool)}));
            Assert.That(typeof(G4GraphVATTests).GetMethods(Private).Count(m =>
                m.Name == "CaptureVATDepthAndShadowGeometry"), Is.EqualTo(1));
        }

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string variant in Variants)
                foreach (bool ortho in new[] {true, false})
                    yield return new TestCaseData(variant, ortho).SetName(
                        "DN0DefaultCoverageABC_" + variant + (ortho ? "_ortho" : "_perspective"));
        }

        Texture2D Map(Func<int, int, Color> pixel)
        {
            var texture = Keep(new Texture2D(8, 8, TextureFormat.RGBAHalf, false, true));
            for (int y=0; y<8; ++y) for (int x=0; x<8; ++x) texture.SetPixel(x,y,pixel(x,y));
            texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Repeat;
            texture.Apply(false); return texture;
        }

        [TestCaseSource(nameof(Cases))]
        public void DefaultSSAOPipelineWithGeometryClipNormalsAndStencil(string variant, bool ortho)
        {
            currentVariant=variant;
            meshProof=null; framingChanged=false; actualDirectionalLight=null;
            bool custom=variant.StartsWith("customlocal", StringComparison.Ordinal);
            bool ty=variant.StartsWith("ty", StringComparison.Ordinal) ||
                variant.Contains("tnegative") || variant.StartsWith("stencil-ty", StringComparison.Ordinal);
            bool absolute=variant.StartsWith("tyabsolute", StringComparison.Ordinal);
            bool clip=variant.Contains("clip");
            bool normalMap=variant.Contains("softbody-normal");
            bool vatNormals=variant.Contains("vatnormal");
            bool stencil=variant.StartsWith("stencil-", StringComparison.Ordinal);
            float bias=variant.Contains("bias1") ? 1f : 0f;
            var mask=clip ? Map((x,y) => new Color(((x/2+y/2)&1)==0 ? .125f : 1f,
                .7f,.3f,((x/2+y/2)&1)==0 ? .125f : 1f)) : null;
            var bump=normalMap ? Map((x,y) => new Color(.2f,.7f,1,1)) : null;
            var localMatrix=Matrix4x4.TRS(new Vector3(.125f,1,.125f),
                Quaternion.Euler(0,17,11), new Vector3(variant.Contains("tnegative") ? -1.25f : 1.25f,.75f,1.5f));
            Texture2D vat=null;
            fixture=new G4GraphVATTests { GeometryCaseId="dn0-"+variant };
            tyflow=ty ? new G4GraphTyflowVATTests() : null;
            fixture.GeometryMeshSetup=mesh => {
                if(normalMap)
                {
                    // The previous fixture did not record the built-in Quad
                    // tangent stream. Record it; supply a real tangent only if
                    // that native stream is absent/zero/degenerate.
                    var tangents=mesh.tangents; var normals=mesh.normals;
                    meshProof=new MeshBasisProof { vertices=mesh.vertexCount,
                        originalTangentCount=tangents.Length, originalTangents=tangents,
                        originalNormals=normals };
                    bool invalid=tangents.Length!=mesh.vertexCount;
                    for(int i=0;i<tangents.Length&&!invalid;++i)
                    {
                        var tangent=new Vector3(tangents[i].x,tangents[i].y,tangents[i].z);
                        invalid=tangent.sqrMagnitude==0 || tangents[i].w==0 ||
                            normals.Length!=mesh.vertexCount || Vector3.Cross(normals[i],tangent).sqrMagnitude==0;
                    }
                    if(invalid)
                    {
                        mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,-1),mesh.vertexCount).ToArray();
                        meshProof.basisRepaired=true;
                    }
                    meshProof.effectiveTangents=mesh.tangents;
                    Assert.That(meshProof.effectiveTangents.Length,Is.EqualTo(mesh.vertexCount));
                    foreach(var tangent in meshProof.effectiveTangents)
                        Assert.That(new Vector3(tangent.x,tangent.y,tangent.z).sqrMagnitude,Is.GreaterThan(0));
                }
                if(custom)
                {
                    var vertices=mesh.vertices;
                    var worldSimulation=Matrix4x4.TRS(new Vector3(0,1,0),Quaternion.identity,Vector3.one);
                    for(int i=0;i<vertices.Length;i++) vertices[i]=worldSimulation.MultiplyPoint3x4(vertices[i]);
                    mesh.vertices=vertices;
                    mesh.bounds=new Bounds(Vector3.zero,Vector3.one*20);
                }
                if(ty)
                {
                    for(int channel=1;channel<8;++channel)
                        mesh.SetUVs(channel,channel==1 ? Enumerable.Range(0,4).Select(v =>
                            new Vector4(v,4,0,0)).ToList() : Enumerable.Repeat(Vector4.zero,4).ToList());
                    var build=typeof(G4GraphTyflowVATTests).GetMethod("BuildVAT",Private);
                    Assert.That(build,Is.Not.Null);
                    vat=(Texture2D)build.Invoke(tyflow,new object[]{absolute ? 0 : 1,vatNormals ? "normals" : "raw",mesh});
                }
            };
            fixture.GeometrySetup=(m,graph) => {
                if(custom)
                {
                    if(graph) for(int row=0;row<4;++row)
                    {
                        m.SetVector("_NB_CustomLocalToWorld"+row,localMatrix.GetRow(row));
                        m.SetVector("_NB_CustomWorldToLocal"+row,localMatrix.inverse.GetRow(row));
                    }
                    else
                    {
                        m.SetMatrix("_CustomLocalTransformLocalToWorld",localMatrix);
                        m.SetMatrix("_CustomLocalTransformWorldToLocal",localMatrix.inverse);
                    }
                }
                if(ty)
                {
                    m.SetFloat("_VATMode",1);m.SetFloat("_TyFlowVATSubMode",absolute ? 0 : 1);
                    m.SetTexture("_VATTex",vat);m.SetFloat("_ImportScale",1);m.SetFloat("_Frames",2);
                    m.SetFloat("_Autoplay",0);m.SetFloat("_FrameInterpolation",0);m.SetFloat("_Loop",0);
                    m.SetFloat("_RGBAEncoded",0);m.SetFloat("_LinearToGamma",0);
                    m.SetFloat("_VATIncludesNormals",vatNormals ? 1 : 0);
                }
            };
            if(ty || custom) fixture.GeometryFrameState=(m,graph,frame) => {
                bool enabled=frame>0;
                // CustomLocal cases isolate its transform control while VAT
                // stays active. Other cases isolate the actual VAT toggle.
                m.SetFloat("_VAT_Toggle",custom || enabled ? 1 : 0);
                m.SetFloat(ty ? "_Frame" : "_displayFrame",ty ? Mathf.Max(frame-1,0) : Mathf.Max(frame,1));
                if(graph)
                {
                    if(custom) m.SetFloat("_NB_CustomLocalTransform",enabled ? 1 : 0);
                    return;
                }
                foreach(string k in new[]{"_VAT","_VAT_TYFLOW","_VAT_HOUDINI","_HOUDINI_VAT_SOFTBODY",
                    "_TYFLOW_VAT_ABSOLUTE","_TYFLOW_VAT_RELATIVE","_CUSTOM_LOCAL_TRANSFORM"}) m.DisableKeyword(k);
                if(custom && enabled) m.EnableKeyword("_CUSTOM_LOCAL_TRANSFORM");
                if(custom || enabled)
                {
                    m.EnableKeyword("_VAT");m.EnableKeyword(ty ? "_VAT_TYFLOW" : "_VAT_HOUDINI");
                    m.EnableKeyword(ty ? (absolute ? "_TYFLOW_VAT_ABSOLUTE" : "_TYFLOW_VAT_RELATIVE") : "_HOUDINI_VAT_SOFTBODY");
                }
            };
            fixture.DefaultFullChainMaterialSetup=(m,graph) => {
                if(clip)
                {
                    m.SetTexture("_MaskMap",mask);m.SetFloat("_Mask_Toggle",1);
                    m.SetVector("_MaskMapVec",new Vector4(1,0,0,0));m.SetFloat("_Cutoff",.5f);
                    if(m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip",1);
                    m.EnableKeyword("_ALPHATEST_ON");if(!graph)m.EnableKeyword("_MASKMAP_ON");
                }
                if(normalMap || vatNormals)
                {
                    m.SetFloat("_fresnelEnabled",1);m.SetVector("_FresnelUnit",new Vector4(.5f,1,.75f,0));
                    m.SetColor("_FresnelColor",new Color(.8f,.2f,.1f,1));m.SetVector("_FresnelRotation",Vector4.zero);
                    if(!graph)m.EnableKeyword("_FRESNEL");
                }
                if(normalMap)
                {
                    m.SetTexture("_BumpTex",bump);m.SetFloat("_BumpMapToggle",1);m.SetFloat("_BumpScale",1);
                    if(!graph)m.EnableKeyword("_NORMALMAP");
                    // Use a proven normal-dependent lighting consumer in
                    // addition to Fresnel. Float and legacy keyword must agree.
                    m.SetFloat("_FxLightMode",1);m.SetVector("_MaterialInfo",new Vector4(.6f,.75f,0,0));
                    m.SetColor("_SpecularColor",new Color(.8f,.7f,.6f,1));
                    if(!graph)
                    {
                        foreach(string k in new[]{"_FX_LIGHT_MODE_UNLIT","_FX_LIGHT_MODE_BLINN_PHONG",
                            "_FX_LIGHT_MODE_HALF_LAMBERT","_FX_LIGHT_MODE_PBR","_FX_LIGHT_MODE_SIX_WAY"})m.DisableKeyword(k);
                        m.EnableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                    }
                }
                if(stencil)
                {
                    m.SetFloat("_Stencil",200);m.SetFloat("_StencilComp",(float)CompareFunction.Always);
                    m.SetFloat("_StencilOp",(float)StencilOp.Keep);
                    m.SetFloat("_StencilFail",(float)StencilOp.Replace);m.SetFloat("_StencilZFail",(float)StencilOp.Replace);
                    m.SetFloat("_StencilReadMask",255);m.SetFloat("_StencilWriteMask",255);
                }
                if(graph)Assert.That(m.GetShaderPassEnabled("DepthNormalsOnly"),Is.True);
            };
            fixture.DefaultFullChainLightSetup=light => {light.shadowNormalBias=bias;actualDirectionalLight=light;};
            if(stencil) fixture.DefaultFullChainSceneSetup=(camera,actor,receiver,target) => CreateStencilOracles(camera,target);
            if(custom && variant.Contains("tnegative") && clip && ortho)
                fixture.DefaultFullChainSceneSetup=(camera,actor,receiver,target) => {
                    // Preserve the >150 visibility assertion. Make this small,
                    // clipped actor occupy enough pixels using camera framing.
                    camera.orthographicSize=3.0f; framingChanged=true;
                };
            fixture.DefaultFullChainVerify=(materials,writer,receiver,camera,target,readback,folder) => {
                WriteVariantInputs(materials,folder,localMatrix,bias);
                if(stencil) CheckStencilNoContribution(materials,writer,camera,target,readback,folder);
                else if(clip || normalMap || vatNormals) CheckFeatureResponse(materials,writer,camera,target,readback,folder,clip,normalMap,vatNormals);
            };
            try { fixture.CaptureDefaultForwardDepthShadow(ortho); }
            finally
            {
                if(tyflow!=null)tyflow.Cleanup();
                for(int i=owned.Count-1;i>=0;--i)if(owned[i])Object.DestroyImmediate(owned[i]);
                owned.Clear();fixture=null;tyflow=null;probeRenderer=stencilWriter=null;
            }
        }

        [Serializable] sealed class VariantInput
        {
            public string variant;
            public float normalBias;
            public Vector4[] customLocalRows;
            public float[] alphaClip, bumpScale, vatNormals, stencilFail, stencilZFail, stencilWriteMask;
            public float[] fxLightMode;
            public string[] lightKeywordState;
            public MeshBasisProof nativeMeshBasis;
            public bool cameraFramingChanged;
            public float originalOrthographicSize, effectiveOrthographicSize;
            public LightInputProof actualLight;
        }
        void WriteVariantInputs(Material[] m,string folder,Matrix4x4 matrix,float bias)
        {
            Func<string,float[]> values=p => m.Select(x => x.HasProperty(p) ? x.GetFloat(p) : -1).ToArray();
            var r=new VariantInput{variant=currentVariant,normalBias=bias,
                customLocalRows=Enumerable.Range(0,4).Select(matrix.GetRow).ToArray(),alphaClip=values("_AlphaClip"),
                bumpScale=values("_BumpScale"),vatNormals=values("_VATIncludesNormals"),stencilFail=values("_StencilFail"),
                stencilZFail=values("_StencilZFail"),stencilWriteMask=values("_StencilWriteMask"),
                fxLightMode=values("_FxLightMode"),
                lightKeywordState=m.Select(x => string.Join(";",new[]{"_FX_LIGHT_MODE_UNLIT",
                    "_FX_LIGHT_MODE_BLINN_PHONG","_FX_LIGHT_MODE_HALF_LAMBERT","_FX_LIGHT_MODE_PBR","_FX_LIGHT_MODE_SIX_WAY"}
                    .Select(k=>k+"="+x.IsKeywordEnabled(k)))).ToArray(),nativeMeshBasis=meshProof,
                cameraFramingChanged=framingChanged,originalOrthographicSize=3.5f,
                effectiveOrthographicSize=framingChanged?3.0f:3.5f,
                actualLight=actualDirectionalLight ? new LightInputProof {
                    type=actualDirectionalLight.type.ToString(), shadows=actualDirectionalLight.shadows.ToString(),
                    intensity=actualDirectionalLight.intensity, color=actualDirectionalLight.color,
                    shadowBias=actualDirectionalLight.shadowBias, shadowNormalBias=actualDirectionalLight.shadowNormalBias,
                    eulerAngles=actualDirectionalLight.transform.eulerAngles, forward=actualDirectionalLight.transform.forward,
                    cullingMask=actualDirectionalLight.cullingMask } : null};
            File.WriteAllText(Path.Combine(folder,"coverage-inputs.json"),JsonUtility.ToJson(r,true));
        }

        Color[] Capture(Material m,MeshRenderer actor,Camera camera,RenderTexture target,
            Texture2D readback,string folder,string label)
        {
            if(fixture.GeometryFrameState!=null)fixture.GeometryFrameState(m,m.shader.name!="Effects/NBShader" && m.shader.name!="Effects/NBShader_T00_Frozen",1);
            else m.SetFloat("_displayFrame",1);
            actor.sharedMaterial=m;for(int i=0;i<4;++i)camera.Render();
            return (Color[])typeof(G4GraphVATTests).GetMethod("Draw",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{actor,m,camera,target,readback,folder,label});
        }
        static float Delta(Color[] a,Color[] b)
        {float result=0;for(int i=0;i<a.Length;++i)for(int c=0;c<4;++c)result=Mathf.Max(result,Mathf.Abs(a[i][c]-b[i][c]));return result;}
        static int Changed(Color[] a,Color[] b)
        {int n=0;for(int i=0;i<a.Length;++i)if(a[i].r!=b[i].r||a[i].g!=b[i].g||a[i].b!=b[i].b||a[i].a!=b[i].a)++n;return n;}
        static bool Finite(IEnumerable<Color[]> frames)
        {foreach(var a in frames)foreach(var p in a)for(int c=0;c<4;++c)if(float.IsNaN(p[c])||float.IsInfinity(p[c]))return false;return true;}
        [Serializable] sealed class FeatureRecord
        {public string scope;public bool finite;public float[] response,repeat,abBC;public int[] responsePixels;}
        void CheckFeatureResponse(Material[] m,MeshRenderer actor,Camera camera,RenderTexture rt,
            Texture2D read,string folder,bool clip,bool normalMap,bool vatNormals)
        {
            var on=new Color[3][];var repeat=new Color[3][];var off=new Color[3][];
            var controls=m.Select(x => x.GetFloat(normalMap ? "_BumpScale" : vatNormals ? "_VATIncludesNormals" : "_Cutoff")).ToArray();
            try
            {
                for(int i=0;i<3;++i)
                {
                    on[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-feature-on");
                    repeat[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-feature-repeat");
                    if(clip){if(m[i].HasProperty("_AlphaClip"))m[i].SetFloat("_AlphaClip",0);m[i].DisableKeyword("_ALPHATEST_ON");}
                    else m[i].SetFloat(normalMap ? "_BumpScale" : "_VATIncludesNormals",0);
                    off[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-feature-control");
                    if(clip){if(m[i].HasProperty("_AlphaClip"))m[i].SetFloat("_AlphaClip",1);m[i].EnableKeyword("_ALPHATEST_ON");}
                    else m[i].SetFloat(normalMap ? "_BumpScale" : "_VATIncludesNormals",controls[i]);
                }
                var r=new FeatureRecord{scope="Actual default chain feature-on vs independent clip/normal-input control; no extraNormals or SSAO suppression",finite=Finite(on.Concat(repeat).Concat(off)),
                    response=Enumerable.Range(0,3).Select(i=>Delta(on[i],off[i])).ToArray(),repeat=Enumerable.Range(0,3).Select(i=>Delta(on[i],repeat[i])).ToArray(),
                    abBC=new[]{Delta(on[0],on[1]),Delta(on[1],on[2]),Delta(off[0],off[1]),Delta(off[1],off[2])},
                    responsePixels=Enumerable.Range(0,3).Select(i=>Changed(on[i],off[i])).ToArray()};
                File.WriteAllText(Path.Combine(folder,"feature-response.json"),JsonUtility.ToJson(r,true));
                Assert.That(r.finite,Is.True);foreach(float d in r.repeat)Assert.That(d,Is.Zero);
                foreach(float d in r.response)Assert.That(d,Is.GreaterThan(.01f));foreach(int n in r.responsePixels)Assert.That(n,Is.GreaterThan(128));
                foreach(float d in r.abBC)Assert.That(d,Is.Zero);
            }
            finally
            {
                for(int i=0;i<3;++i)
                    if(clip){if(m[i].HasProperty("_AlphaClip"))m[i].SetFloat("_AlphaClip",1);m[i].EnableKeyword("_ALPHATEST_ON");}
                    else m[i].SetFloat(normalMap ? "_BumpScale" : "_VATIncludesNormals",controls[i]);
            }
        }

        Shader OracleShader(string suffix,bool writer)
        {
            string stencil=writer ? "Stencil { Ref [_WriterRef] Comp Always Pass Replace Fail Keep ZFail Keep ReadMask 255 WriteMask 255 }" :
                "Stencil { Ref 200 Comp Equal Pass Keep Fail Keep ZFail Keep ReadMask 255 WriteMask 0 }";
            string s="Shader \"Hidden/NBFX/DN0"+suffix+Guid.NewGuid().ToString("N")+"\" { Properties { _WriterRef(\"Writer Ref\", Float)=200 } " +
                "SubShader { Tags { \"RenderPipeline\"=\"UniversalPipeline\" \"Queue\"=\"Geometry\" \"RenderType\"=\"Opaque\" } " +
                "Pass { Tags { \"LightMode\"=\"UniversalForward\" } Cull Off ZWrite Off ZTest Always Blend One Zero ColorMask " +
                (writer ? "0 " : "RGBA ") + stencil + " HLSLPROGRAM\n#pragma target 4.5\n#pragma vertex Vert\n#pragma fragment Frag\n"+
                "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n"+
                "struct Attributes { float4 positionOS:POSITION; }; struct Varyings {float4 positionCS:SV_POSITION;};"+
                "Varyings Vert(Attributes a){Varyings o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);return o;}"+
                "half4 Frag(Varyings v):SV_Target{return half4(1,0,1,1);}\nENDHLSL\n} } }";
            if(writer)writerSource=s;else probeSource=s;
            var shader=Keep(ShaderUtil.CreateShaderAsset(s,false));Assert.That(shader,Is.Not.Null);
            shader.hideFlags=HideFlags.HideAndDontSave;Assert.That(shader.isSupported,Is.True);
            Assert.That(ShaderUtil.GetShaderMessages(shader).Any(x=>x.severity.ToString()=="Error"),Is.False);return shader;
        }
        void CreateStencilOracles(Camera camera,RenderTexture target)
        {
            GraphicsFormat stencilFormat=GraphicsFormat.None;
            foreach(var f in new[]{GraphicsFormat.D24_UNorm_S8_UInt,GraphicsFormat.D32_SFloat_S8_UInt})
                if(SystemInfo.IsFormatSupported(f,GraphicsFormatUsage.Render)){stencilFormat=f;break;}
            Assert.That(stencilFormat,Is.Not.EqualTo(GraphicsFormat.None));
            var descriptor=target.descriptor;descriptor.depthStencilFormat=stencilFormat;
            target.Release();target.descriptor=descriptor;target.Create();
            Assert.That(target.IsCreated()&&!target.sRGB,Is.True);
            Assert.That(target.descriptor.depthStencilFormat,Is.EqualTo(stencilFormat));
            probeMaterial=Keep(new Material(OracleShader("StencilProbe",false)));probeMaterial.renderQueue=2450;
            writerMaterial=Keep(new Material(OracleShader("StencilWriter",true)));writerMaterial.renderQueue=2400;
            var probe=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var writer=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));
            foreach(var go in new[]{probe,writer})
            {
                SceneManager.MoveGameObjectToScene(go,camera.scene);go.layer=2;
                go.transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*.5f,camera.transform.rotation);
                float height=camera.orthographic ? 2*camera.orthographicSize : 2*.5f*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
                go.transform.localScale=new Vector3(height*camera.aspect*1.05f,height*1.05f,1);
                var mr=go.GetComponent<MeshRenderer>();mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;mr.enabled=false;
            }
            probeRenderer=probe.GetComponent<MeshRenderer>();probeRenderer.sharedMaterial=probeMaterial;
            stencilWriter=writer.GetComponent<MeshRenderer>();stencilWriter.sharedMaterial=writerMaterial;
        }
        [Serializable] sealed class StencilRecord
        {public string scope;public bool finite,graphNormalsRawTrue;public float[] leak,response,repeat,abBC;public int[] responsePixels;public string depthStencilFormat;}
        void CheckStencilNoContribution(Material[] m,MeshRenderer actor,Camera camera,RenderTexture rt,Texture2D read,string folder)
        {
            File.WriteAllText(Path.Combine(folder,"stencil-probe-source.shader"),probeSource);
            File.WriteAllText(Path.Combine(folder,"stencil-writer-source.shader"),writerSource);
            var baseline=new Color[3][];var noLeak=new Color[3][];var positive=new Color[3][];var repeat=new Color[3][];var zero=new Color[3][];
            try
            {
                for(int i=0;i<3;++i)
                {
                    probeRenderer.enabled=false;stencilWriter.enabled=false;
                    baseline[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-stencil-no-probe");
                    probeRenderer.enabled=true;
                    noLeak[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-stencil-no-writer");
                    stencilWriter.enabled=true;writerMaterial.SetFloat("_WriterRef",200);
                    positive[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-stencil-positive-writer");
                    repeat[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-stencil-positive-repeat");
                    writerMaterial.SetFloat("_WriterRef",0);
                    zero[i]=Capture(m[i],actor,camera,rt,read,folder,"ABC"[i]+"-stencil-wrong-ref-control");
                }
                var r=new StencilRecord{scope="Raw actor Fail/ZFail=Replace/write255; Graph DepthNormals rawTrue. Default chain no-writer must not light stencilEqual200 probe. Independent native URP writerRef200 proves probe response; wrongRef0 restores original output. No directed RendererFeature.",
                    finite=Finite(baseline.Concat(noLeak).Concat(positive).Concat(repeat).Concat(zero)),graphNormalsRawTrue=m[2].GetShaderPassEnabled("DepthNormalsOnly"),depthStencilFormat=rt.descriptor.depthStencilFormat.ToString(),
                    leak=Enumerable.Range(0,3).Select(i=>Delta(baseline[i],noLeak[i])+Delta(baseline[i],zero[i])).ToArray(),
                    response=Enumerable.Range(0,3).Select(i=>Delta(noLeak[i],positive[i])).ToArray(),repeat=Enumerable.Range(0,3).Select(i=>Delta(positive[i],repeat[i])).ToArray(),
                    abBC=new[]{Delta(noLeak[0],noLeak[1]),Delta(noLeak[1],noLeak[2]),Delta(positive[0],positive[1]),Delta(positive[1],positive[2])},
                    responsePixels=Enumerable.Range(0,3).Select(i=>Changed(noLeak[i],positive[i])).ToArray()};
                File.WriteAllText(Path.Combine(folder,"stencil-response.json"),JsonUtility.ToJson(r,true));
                Assert.That(r.finite&&r.graphNormalsRawTrue,Is.True);
                foreach(float d in r.leak)Assert.That(d,Is.Zero);foreach(float d in r.repeat)Assert.That(d,Is.Zero);
                foreach(float d in r.response)Assert.That(d,Is.GreaterThan(.1f));foreach(int n in r.responsePixels)Assert.That(n,Is.GreaterThan(150));
                foreach(float d in r.abBC)Assert.That(d,Is.Zero);
            }
            finally {probeRenderer.enabled=false;stencilWriter.enabled=false;}
        }
    }
}
