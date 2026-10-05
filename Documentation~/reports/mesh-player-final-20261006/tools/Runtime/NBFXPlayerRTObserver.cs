using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NBFX.PlayerValidation
{
    // Runtime test-only port of NBPostFullRTObserver. No input request,
    // production object draw, global assignment, or camera attachment write.
    public sealed class NBFXPlayerRTObserver : ScriptableRendererFeature
    {
        public Material copyMaterial;
        public Camera targetCamera;
        readonly RTHandle[] outputs=new RTHandle[3];
        readonly int[] copiedTokens=new int[3];
        readonly int[] copiedFrames=new int[3];
        int token;bool armed;ObserverPass pass;
        public int graphRecords,queuedPasses,lastCameraID;
        public static readonly string[] Names={"_DisturbanceMaskTex","_ScreenColorCopy1","_CameraOpaqueTexture"};
        [Serializable] sealed class Evidence {public int token,frame,camera,queuedPasses,graphRecords;public int[] copiedTokens,copiedFrames;public string[] globals;public bool cameraOutputUntouched;}
        public void Initialize(Camera camera,int size,Material material)
        {
            targetCamera=camera;copyMaterial=material;
            for(int i=0;i<3;i++){
                var rt=new RenderTexture(size,size,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear){name="NBFX Player observed "+Names[i],filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                rt.Create();if(!rt.IsCreated())throw new InvalidOperationException("Observer RT allocation failed.");outputs[i]=RTHandles.Alloc(rt,transferOwnership:true);
            }
            Create();
        }
        public int Request(){token++;armed=true;Array.Clear(copiedTokens,0,3);Array.Clear(copiedFrames,0,3);return token;}
        public void Cancel(){armed=false;}
        public RenderTexture Readable(string global,int expectedToken)
        {
            int index=Array.IndexOf(Names,global);
            if(index<0||expectedToken<=0||token!=expectedToken||copiedTokens[index]!=expectedToken||copiedFrames[index]!=Time.frameCount||lastCameraID!=targetCamera.GetInstanceID())throw new InvalidOperationException("Fresh same-camera in-graph copy unavailable: "+global);
            return outputs[index]?.rt;
        }
        public string EvidenceJSON()=>JsonUtility.ToJson(new Evidence{token=token,frame=Time.frameCount,camera=lastCameraID,queuedPasses=queuedPasses,graphRecords=graphRecords,copiedTokens=(int[])copiedTokens.Clone(),copiedFrames=(int[])copiedFrames.Clone(),globals=Names,cameraOutputUntouched=true},true);
        public override void Create(){pass=new ObserverPass(this){renderPassEvent=RenderPassEvent.AfterRenderingTransparents+1};}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {if(armed&&data.cameraData.camera==targetCamera){queuedPasses++;renderer.EnqueuePass(pass);}}
        protected override void Dispose(bool disposing){armed=false;foreach(var h in outputs)h?.Release();Array.Clear(outputs,0,3);}
        void OnDestroy(){Dispose(true);}
        sealed class ObserverPass:ScriptableRenderPass
        {
            readonly NBFXPlayerRTObserver owner;
            static readonly int[] IDs={Shader.PropertyToID(Names[0]),Shader.PropertyToID(Names[1]),Shader.PropertyToID(Names[2])};
            sealed class Data {public NBFXPlayerRTObserver owner;public Material material;public int view,token,camera,frame;}
            public ObserverPass(NBFXPlayerRTObserver owner){this.owner=owner;}
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
            {
                var camera=frame.Get<UniversalCameraData>().camera;if(!owner.armed||camera!=owner.targetCamera)return;
                int requested=owner.token;owner.armed=false;owner.graphRecords++;
                for(int i=0;i<3;i++){
                    var output=graph.ImportTexture(owner.outputs[i]);
                    using(var b=graph.AddRasterRenderPass<Data>("NBFX Player copy existing "+Names[i],out var data)){
                        data.owner=owner;data.material=owner.copyMaterial;data.view=i;data.token=requested;data.camera=camera.GetInstanceID();data.frame=Time.frameCount;
                        b.UseGlobalTexture(IDs[i],AccessFlags.Read);b.SetRenderAttachment(output,0,AccessFlags.Write);b.AllowPassCulling(false);
                        b.SetRenderFunc(static(Data d,RasterGraphContext ctx)=>{ctx.cmd.DrawProcedural(Matrix4x4.identity,d.material,d.view,MeshTopology.Triangles,3,1);d.owner.copiedTokens[d.view]=d.token;d.owner.copiedFrames[d.view]=d.frame;d.owner.lastCameraID=d.camera;});
                    }
                }
            }
        }
    }
}
