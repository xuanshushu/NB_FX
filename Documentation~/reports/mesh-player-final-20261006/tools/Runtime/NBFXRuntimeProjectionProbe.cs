using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NBShader;
using Object=UnityEngine.Object;

namespace NBFX.PlayerValidation
{
    // One independent actual-Player identity; never replaces or renames frozen Player8.
    public sealed class NBFXRuntimeProjectionProbe:MonoBehaviour
    {
        public NBFXMeshPlayerConfig config;
        public Material native,graph,greenProbe;
        public Material[] retainedVariants;
        public NBShaderFeatureRuntimeSettings allow,denyMask,denyMaskAndOVZ;
        public string[] graphProjectionProperties;
        const string Identity="PlayerBC_RuntimeGraphMaskOVZ_ortho";
        string folder;new Camera camera;RenderTexture target;Texture2D read;MeshRenderer writer,probe;
        Material b,c;readonly List<Object> owned=new List<Object>();readonly List<Stage> stages=new List<Stage>();
        [Serializable] sealed class Stage{public string name;public float bc,bRepeat,cRepeat;public int bActor,cActor,bProbe,cProbe;public bool bOVZ,cOVZ,graphGate,rawPreserved,declaredKeyword;public float bZWrite,cZWrite,cZWriteControl,bOverrideValue,cOverrideValue,actorEyeDepth,probeEyeDepth;public int bQueue,cQueue,probeQueue,overlapX,overlapY,overlapWidth,overlapHeight,bActorOverlap,cActorOverlap,bProbeOverlap,cProbeOverlap;}
        [Serializable] sealed class Receipt{public string identity,status,error,scope,buildIdentity,sourceLockSHA256,unity,api,platform;public int pointerBytes,rawCount;public bool applicationIsEditor;public Stage[] stages;public float maskResponseB,maskResponseC,ovzResponseB,ovzResponseC,maskRestoreB,maskRestoreC,ovzRestoreB,ovzRestoreC;}
        readonly Receipt result=new Receipt{identity=Identity,status="FAILED",scope="One orthographic Player Runtime.ApplyTier Mask float gate and OVZ keyword/depth-occlusion proof. Exact scene-referenced on/off material variants only; not all-variants/AlwaysIncluded/SVC/AssetBundle coverage."};
        static string Arg(string n){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,n);return i>=0&&i+1<a.Length?a[i+1]:null;}
        static void Need(bool v,string why){if(!v)throw new InvalidOperationException(why);}
        T Keep<T>(T o)where T:Object{owned.Add(o);return o;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Entry()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"--nbfx-runtime-proof")>=0)UnityEngine.SceneManagement.SceneManager.sceneLoaded+=Select;}
        static void Select(UnityEngine.SceneManagement.Scene s,UnityEngine.SceneManagement.LoadSceneMode mode)
        {UnityEngine.SceneManagement.SceneManager.sceneLoaded-=Select;foreach(var h in Object.FindObjectsByType<NBFXMeshPlayerHarness>(FindObjectsInactive.Include,FindObjectsSortMode.None))h.enabled=false;UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("NBFXRuntimeProjection",UnityEngine.SceneManagement.LoadSceneMode.Single);}
        void Start()
        {
            folder=Arg("--nbfx-runtime-proof-evidence");if(string.IsNullOrEmpty(folder)){Application.Quit(2);return;}Directory.CreateDirectory(folder);
            try
            {
                Need(!Application.isEditor&&Application.platform==RuntimePlatform.WindowsPlayer&&IntPtr.Size==8&&SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D11,"Actual Windows64 D3D11 Player required.");
                Need(config&&native&&graph&&greenProbe&&allow&&denyMask&&denyMaskAndOVZ&&retainedVariants.Length==6,"Serialized exact on/off retention inputs absent.");
                QualitySettings.renderPipeline=config.pipeline;Need(!GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode,"This proof is RG only.");
                b=Keep(new Material(native));c=Keep(new Material(graph));
                var actor=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));actor.layer=4;actor.transform.localScale=new Vector3(1.5f,1.5f,1);writer=actor.GetComponent<MeshRenderer>();writer.shadowCastingMode=ShadowCastingMode.Off;writer.receiveShadows=false;
                var p=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));p.layer=4;p.transform.position=new Vector3(0,0,-1);p.transform.localScale=new Vector3(2,2,1);probe=p.GetComponent<MeshRenderer>();probe.sharedMaterial=greenProbe;probe.shadowCastingMode=ShadowCastingMode.Off;
                var host=Keep(new GameObject("Original OVZ orthographic camera"));camera=host.AddComponent<Camera>();camera.enabled=false;host.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
                camera.orthographic=true;camera.orthographicSize=1.4f;camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=25;camera.transform.position=new Vector3(0,0,5);camera.transform.rotation=Quaternion.LookRotation(Vector3.back);camera.cullingMask=1<<4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.blue;camera.allowHDR=true;camera.allowMSAA=false;
                target=Keep(new RenderTexture(128,128,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));target.Create();camera.targetTexture=target;read=Keep(new Texture2D(128,128,TextureFormat.RGBAHalf,false,true));
                StartCoroutine(Run());
            }
            catch(Exception e){Finish(e);}
        }
        Dictionary<string,string> Raw(Material m,bool isGraph)
        {
            var r=new Dictionary<string,string>();var shader=m.shader;
            for(int i=0;i<shader.GetPropertyCount();i++)
            {
                string n=shader.GetPropertyName(i);if(isGraph&&(graphProjectionProperties.Contains(n)||n=="_NB_BackFirstEffective"))continue;
                switch(shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Float:case ShaderPropertyType.Range:r[n]=m.GetFloat(n).ToString("R",System.Globalization.CultureInfo.InvariantCulture);break;
                    case ShaderPropertyType.Int:r[n]=m.GetInteger(n).ToString();break;
                    case ShaderPropertyType.Color:r[n]=m.GetColor(n).ToString("F9");break;
                    case ShaderPropertyType.Vector:r[n]=m.GetVector(n).ToString("F9");break;
                    case ShaderPropertyType.Texture:var t=m.GetTexture(n);r[n]=(t?t.GetInstanceID():0)+"|"+m.GetTextureScale(n).ToString("F9")+"|"+m.GetTextureOffset(n).ToString("F9");break;
                }
            }
            return r;
        }
        IEnumerator Run()
        {
            var rawB=Raw(b,false);var rawC=Raw(c,true);var frames=new List<Color[][]>();
            RectInt overlap=OverlapPixelRect();Need(overlap.width*overlap.height>150,"Real actor/probe overlap positive control absent.");
            string[] names={"mask-on","mask-denied","mask-restored","ovz-on","ovz-denied","ovz-restored"};
            var policies=new[]{allow,denyMask,allow,denyMask,denyMaskAndOVZ,denyMask};
            for(int stage=0;stage<names.Length;stage++)
            {
                Exception error=null;
                try{probe.enabled=stage>=3;NBShaderFeatureRuntime.ApplyTier(new[]{b,c},policies[stage],NBShaderFeatureTier.Low);}catch(Exception e){error=e;}
                if(error!=null){Finish(error);yield break;}
                yield return new WaitForEndOfFrame();
                try
                {
                    var bb=Snap(b,names[stage]+"-B");var br=Snap(b,names[stage]+"-Br");var cc=Snap(c,names[stage]+"-C");var cr=Snap(c,names[stage]+"-Cr");frames.Add(new[]{bb,cc});
                    var rb=Raw(b,false);var rc=Raw(c,true);
                    bool raw=rb.Count==rawB.Count&&rc.Count==rawC.Count&&rawB.All(p=>rb[p.Key]==p.Value)&&rawC.All(p=>rc[p.Key]==p.Value);
                    var row=new Stage{name=names[stage],bc=Delta(bb,cc),bRepeat=Delta(bb,br),cRepeat=Delta(cc,cr),bActor=bb.Count(p=>p.r>.25f&&p.g<.1f),cActor=cc.Count(p=>p.r>.25f&&p.g<.1f),bProbe=bb.Count(p=>p.g>.5f&&p.r<.1f),cProbe=cc.Count(p=>p.g>.5f&&p.r<.1f),bOVZ=b.IsKeywordEnabled("_OVERRIDE_Z"),cOVZ=c.IsKeywordEnabled("_OVERRIDE_Z"),graphGate=c.GetFloat("_NB_TierAllowMask")==1,rawPreserved=raw,declaredKeyword=b.shader.keywordSpace.FindKeyword("_OVERRIDE_Z").isValid&&c.shader.keywordSpace.FindKeyword("_OVERRIDE_Z").isValid};
                    row.bZWrite=b.GetFloat("_ZWrite");row.cZWrite=c.GetFloat("_ZWrite");row.cZWriteControl=c.GetFloat("_ZWriteControl");row.bOverrideValue=b.GetFloat("_OverrideZValue");row.cOverrideValue=c.GetFloat("_OverrideZValue");row.actorEyeDepth=Vector3.Dot(writer.bounds.center-camera.transform.position,camera.transform.forward);row.probeEyeDepth=Vector3.Dot(probe.bounds.center-camera.transform.position,camera.transform.forward);row.bQueue=b.renderQueue;row.cQueue=c.renderQueue;row.probeQueue=greenProbe.renderQueue;row.overlapX=overlap.x;row.overlapY=overlap.y;row.overlapWidth=overlap.width;row.overlapHeight=overlap.height;
                    row.bActorOverlap=CountOverlap(bb,overlap,false);row.cActorOverlap=CountOverlap(cc,overlap,false);row.bProbeOverlap=CountOverlap(bb,overlap,true);row.cProbeOverlap=CountOverlap(cc,overlap,true);stages.Add(row);Save();
                    Need(row.bZWrite==1&&row.cZWrite==1&&row.cZWriteControl==1,"Legal saved/runtime actor depth-write inputs must remain enabled.");Need(row.bQueue==3000&&row.cQueue==3000&&row.probeQueue==3001&&greenProbe.GetFloat("_QueueOffset")==1,"Saved/runtime probe must render after actor through legal queue offset.");
                    int overlapArea=overlap.width*overlap.height;
                    if(stage<3||stage==4)Need(row.bActorOverlap==overlapArea&&row.cActorOverlap==overlapArea&&row.bProbeOverlap==0&&row.cProbeOverlap==0,"Actor must fill real overlap when OVZ deny or probe disabled.");
                    else Need(row.bProbeOverlap==overlapArea&&row.cProbeOverlap==overlapArea&&row.bActorOverlap==0&&row.cActorOverlap==0,"OVZ on/restored must cause actual center occlusion; green border alone is insufficient.");
                    Need(row.bc==0&&row.bRepeat==0&&row.cRepeat==0,"Strict Player B/C/repeat mismatch.");Need(raw,"Runtime policy changed serialized raw intent/SavedTier/marker.");Need(row.declaredKeyword,"Declared keyword missing; this alone is not variant-retention proof.");
                    Need(row.graphGate==(stage==0||stage==2),"Actual Graph mask projection not applied.");Need(row.bOVZ==(stage!=4)&&row.cOVZ==(stage!=4),"Actual Runtime OVZ keyword state mismatch.");
                    if(stage<3||stage==4)Need(row.bActor>150&&row.cActor>150,"Actor must remain visible.");else Need(row.bProbe>150&&row.cProbe>150,"Original green depth probe must occlude the actor.");
                }
                catch(Exception e){error=e;}
                if(error!=null){Finish(error);yield break;}
            }
            try
            {
                result.maskResponseB=Delta(frames[0][0],frames[1][0]);result.maskResponseC=Delta(frames[0][1],frames[1][1]);result.ovzResponseB=Delta(frames[3][0],frames[4][0]);result.ovzResponseC=Delta(frames[3][1],frames[4][1]);result.maskRestoreB=Delta(frames[0][0],frames[2][0]);result.maskRestoreC=Delta(frames[0][1],frames[2][1]);result.ovzRestoreB=Delta(frames[3][0],frames[5][0]);result.ovzRestoreC=Delta(frames[3][1],frames[5][1]);Save();
                Need(result.maskResponseB>.1f&&result.maskResponseC>.1f&&result.ovzResponseB>.1f&&result.ovzResponseC>.1f,"Actual gate and depth effects require strong on/deny response.");Need(result.maskRestoreB+result.maskRestoreC+result.ovzRestoreB+result.ovzRestoreC==0,"Runtime restore changed the original images.");result.status="PASSED";Finish(null);
            }
            catch(Exception e){Finish(e);}
        }
        Rect ViewportBounds(Bounds bounds)
        {
            Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            for(int i=0;i<8;i++){var p=camera.WorldToViewportPoint(new Vector3((i&1)==0?bounds.min.x:bounds.max.x,(i&2)==0?bounds.min.y:bounds.max.y,(i&4)==0?bounds.min.z:bounds.max.z));Need(p.z>0,"Occlusion geometry is behind camera.");min=Vector2.Min(min,new Vector2(p.x,p.y));max=Vector2.Max(max,new Vector2(p.x,p.y));}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        RectInt OverlapPixelRect()
        {
            var a=ViewportBounds(writer.bounds);var b=ViewportBounds(probe.bounds);
            int minX=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.xMin,b.xMin)*128)+1,0,127),minY=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.yMin,b.yMin)*128)+1,0,127),maxX=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.xMax,b.xMax)*128)-1,0,127),maxY=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.yMax,b.yMax)*128)-1,0,127);
            Need(maxX>=minX&&maxY>=minY,"Actor/probe have no real interior screen overlap.");return new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        static int CountOverlap(Color[] pixels,RectInt rect,bool green)
        {int count=0;for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++){var p=pixels[y*128+x];if(green?(p.g>.5f&&p.r<.1f):(p.r>.25f&&p.g<.1f))count++;}return count;}
        Color[] Snap(Material m,string label)
        {
            Need(m.shader.isSupported,"Player shader unsupported.");writer.sharedMaterial=m;for(int i=0;i<4;i++)camera.Render();var previous=RenderTexture.active;
            try{RenderTexture.active=target;read.ReadPixels(new Rect(0,0,128,128),0,0,false);read.Apply(false,false);var pixels=read.GetPixels();using(var w=new BinaryWriter(File.Create(Path.Combine(folder,label+".rgba32f"))))foreach(var p in pixels){w.Write(p.r);w.Write(p.g);w.Write(p.b);w.Write(p.a);}result.rawCount++;Need(pixels.All(p=>new[]{p.r,p.g,p.b,p.a}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v!=-23.203125f&&v!=-431602080f)),"Invalid GPU readback.");return pixels;}
            finally{RenderTexture.active=previous;}
        }
        static float Delta(Color[] a,Color[] b){float d=0;for(int i=0;i<a.Length;i++)for(int k=0;k<4;k++)d=Mathf.Max(d,Mathf.Abs(a[i][k]-b[i][k]));return d;}
        void Save(){result.buildIdentity=config?config.buildIdentity:null;result.sourceLockSHA256=config?config.sourceLockSHA256:null;result.unity=Application.unityVersion;result.api=SystemInfo.graphicsDeviceType.ToString();result.platform=Application.platform.ToString();result.pointerBytes=IntPtr.Size;result.applicationIsEditor=Application.isEditor;result.stages=stages.ToArray();File.WriteAllText(Path.Combine(folder,"runtime-projection-result.json"),JsonUtility.ToJson(result,true));}
        void Finish(Exception error){if(error!=null){result.status="FAILED";result.error=error.ToString();}Save();if(camera)camera.targetTexture=null;foreach(var o in owned)if(o)Object.Destroy(o);Application.Quit(error==null&&result.status=="PASSED"?0:1);}
    }
}
