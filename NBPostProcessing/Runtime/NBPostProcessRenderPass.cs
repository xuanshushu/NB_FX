using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
namespace NBShader
{
    public class NBPostProcessRenderPass : ScriptableRenderPass
    {
        private ProfilingSampler _profilingSampler;
        public static Material _material;
        public Mesh _fullScreenMesh;
        private static readonly int ScreenColorCopy = Shader.PropertyToID("_ScreenColorCopy1");
        private static readonly int DisturbanceMaskTex = Shader.PropertyToID("_DisturbanceMaskTex");

        public NBPostProcessFlags _shaderFlag => PostProcessingManager.flags;

        private Vector4 _lastOutlineProps;
        public Vector4 outLinePorps = Vector4.one;

#if UNITY_6000_0_OR_NEWER
        private class RenderGraphPassData
        {
            public TextureHandle activeColorTexture;
            public Material material;
            public Mesh fullscreenMesh;
        }

        private static bool IsSupportedCamera(CameraType cameraType)
        {
            return cameraType == CameraType.Game || cameraType == CameraType.SceneView;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!IsSupportedCamera(cameraData.cameraType) || _material == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (!resourceData.activeColorTexture.IsValid())
                return;

            using (var builder = renderGraph.AddRasterRenderPass<RenderGraphPassData>("NBPostProcess", out var passData))
            {
                passData.activeColorTexture = resourceData.activeColorTexture;
                passData.material = _material;
                passData.fullscreenMesh = _fullScreenMesh != null ? _fullScreenMesh : RenderingUtils.fullscreenMesh;

                builder.SetRenderAttachment(passData.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.UseGlobalTexture(ScreenColorCopy, AccessFlags.Read);
                builder.UseGlobalTexture(DisturbanceMaskTex, AccessFlags.Read);
                builder.SetRenderFunc(static (RenderGraphPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawMesh(data.fullscreenMesh, Matrix4x4.identity, data.material, 0, 0);
                });
            }
        }
#endif

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!(renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView))
                return;
            if(_material == null) return;
   
            // if(!_shaderFlag.CheckFlagBits(NBPostProcessFlags.FLAG_BIT_NB_POSTPROCESS_ON))return;//Disturbance需要执行
            
            //ConfigureTarget()
            CommandBuffer cmdBuffer = CommandBufferPool.Get();
            cmdBuffer.Clear();
            // cmdBuffer.name = "NBPostProcess";
          
            using (new ProfilingScope(cmdBuffer,_profilingSampler))
            {
                Mesh fullscreenMesh = _fullScreenMesh != null ? _fullScreenMesh : RenderingUtils.fullscreenMesh;
                cmdBuffer.DrawMesh(fullscreenMesh, Matrix4x4.identity, _material, 0, 0);
            }
            
            context.ExecuteCommandBuffer(cmdBuffer);
            CommandBufferPool.Release(cmdBuffer);
        }

        public  NBPostProcessRenderPass(Material mat,Mesh mesh)
        {
            _material = mat;
            _fullScreenMesh = mesh;
            _profilingSampler ??= new ProfilingSampler("NBPostProcess");
#if UNITY_6000_0_OR_NEWER
            requiresIntermediateTexture = true;
#endif

        }
    }
}
