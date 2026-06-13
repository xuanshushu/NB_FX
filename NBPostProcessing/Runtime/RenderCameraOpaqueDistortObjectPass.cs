using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
#if UNIVERSAL_RP_17_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace NBShader
{
    public class RenderCameraOpaqueDistortObjectPass: ScriptableRenderPass
    {
        private ProfilingSampler _profilingSampler = new ProfilingSampler("RenderCameraOpaqueDistortObject");
        
        private FilteringSettings _Filtering;
        private readonly List<ShaderTagId> _shaderTag = new List<ShaderTagId>()
        {
            // new ShaderTagId("UniversalForward"),
            // new ShaderTagId("SRPDefaultUnlit"),
            // new ShaderTagId("UniversalForwardOnly")
            new ShaderTagId("NBCameraOpaqueDistortPass")
        };

#if UNIVERSAL_RP_17_0_OR_NEWER
        private class RenderGraphPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private static bool IsSupportedCamera(CameraType cameraType)
        {
            return cameraType == CameraType.Game || cameraType == CameraType.SceneView;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!IsSupportedCamera(cameraData.cameraType))
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (!resourceData.activeColorTexture.IsValid())
                return;

            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<RenderGraphPassData>("RenderCameraOpaqueDistortObject", out var passData))
            {
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    _shaderTag,
                    universalRenderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags);
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
                RendererListParams rendererListParameters = new RendererListParams(
                    universalRenderingData.cullResults,
                    drawingSettings,
                    filteringSettings);

                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);

                if (resourceData.activeDepthTexture.IsValid())
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (RenderGraphPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }
#endif

#if !UNITY_6000_3_OR_NEWER || (URP_COMPATIBILITY_MODE && !UNITY_6000_4_OR_NEWER)
#pragma warning disable CS0618, CS0672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _Filtering = new FilteringSettings(RenderQueueRange.all);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!(renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView))
                return;
            var DisturbanceDraw = CreateDrawingSettings(_shaderTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.DrawRenderers(renderingData.cullResults, ref DisturbanceDraw, ref _Filtering);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618, CS0672
#endif
    }
}
