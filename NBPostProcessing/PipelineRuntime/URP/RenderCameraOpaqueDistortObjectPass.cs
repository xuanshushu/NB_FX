using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

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
    }
}