using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
#if UNIVERSAL_RP_17_0_OR_NEWER
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

namespace NBShader
{
    public class DisturbanceMaskRenderPass : ScriptableRenderPass
    {
        private ProfilingSampler _profilingSampler;
        #if UNIVERSAL_RP_13_1_2_OR_NEWER
            private RTHandle _DisturbanceMaskRTHandle;
            private RTHandle _cameraDepthRTHandle;
            private RTHandle _DownRT;
        #else
            private static readonly int _DisturbanceMaskRTID = Shader.PropertyToID("_DisturbanceMaskRT"); 
            private static readonly int _DownRTID = Shader.PropertyToID("_DisturbanceMaskTex"); 
            private RenderTargetIdentifier  _DisturbanceMaskRTHandle = new RenderTargetIdentifier (_DisturbanceMaskRTID);
            private RenderTargetIdentifier  _cameraDepthRTHandle;
            private RenderTargetIdentifier  _DownRT = new RenderTargetIdentifier (_DownRTID);
        #endif
        private Material tempMat;
        
        private readonly Downsampling _downSampling;

        private Material _renderMaskMat ;
        // public LayerMask _DisturbanceMaskLayer = 1 << 25;
        private FilteringSettings _Filtering;
        private static readonly int CameraTexture = Shader.PropertyToID("_CameraTexture");
        private static readonly int SampleOffset = Shader.PropertyToID("_SampleOffset");
        private static readonly int DisturbanceMaskTex = Shader.PropertyToID("_DisturbanceMaskTex");
        private readonly Color _clearDisturbanceMaskColor = new Color(0f, 0f, 0f, 1f);
        
        private readonly List<ShaderTagId> _shaderTag = new List<ShaderTagId>()
        {
            // new ShaderTagId("UniversalForward"),
            // new ShaderTagId("SRPDefaultUnlit"),
            // new ShaderTagId("UniversalForwardOnly")
            new ShaderTagId("NBDeferredDistortPass")
        };

        public DisturbanceMaskRenderPass(ProfilingSampler profilingSampler,Material DisturbanceMaskMat, Downsampling downSampling)
        {
            _profilingSampler = profilingSampler;
            _renderMaskMat = DisturbanceMaskMat;
            _downSampling = downSampling;
        }

#if UNIVERSAL_RP_17_0_OR_NEWER
        private class RenderGraphMaskPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private class GlobalTexturePassData
        {
            public TextureHandle texture;
            public int nameID;
        }

        private static bool IsSupportedCamera(CameraType cameraType)
        {
            return cameraType == CameraType.Game || cameraType == CameraType.SceneView;
        }

        private static void SetGlobalTextureAfterPass(RenderGraph renderGraph, TextureHandle texture, int nameID, string passName)
        {
            using (var builder = renderGraph.AddRasterRenderPass<GlobalTexturePassData>(passName, out var passData))
            {
                passData.texture = texture;
                passData.nameID = nameID;
                builder.UseTexture(texture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(texture, nameID);
                builder.SetRenderFunc(static (GlobalTexturePassData data, RasterGraphContext context) =>
                {
                });
            }
        }

        private void ApplyDownsampling(ref TextureDesc descriptor)
        {
            switch (_downSampling)
            {
                case Downsampling._2xBilinear:
                    descriptor.width = Mathf.Max(1, descriptor.width / 2);
                    descriptor.height = Mathf.Max(1, descriptor.height / 2);
                    break;
                case Downsampling._4xBilinear:
                case Downsampling._4xBox:
                    descriptor.width = Mathf.Max(1, descriptor.width / 4);
                    descriptor.height = Mathf.Max(1, descriptor.height / 4);
                    break;
            }
        }

        private int GetDownsamplePassIndex()
        {
            return _downSampling == Downsampling._4xBox ? 1 : 0;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!IsSupportedCamera(cameraData.cameraType) || _renderMaskMat == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (!resourceData.activeColorTexture.IsValid())
                return;

            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            TextureDesc maskDescriptor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            maskDescriptor.name = "DisturbanceMaskRT";
            maskDescriptor.colorFormat = GraphicsFormat.R16G16_SFloat;
            maskDescriptor.depthBufferBits = DepthBits.None;
            maskDescriptor.clearBuffer = true;
            maskDescriptor.clearColor = _clearDisturbanceMaskColor;

            TextureHandle maskTexture = renderGraph.CreateTexture(maskDescriptor);
            using (var builder = renderGraph.AddRasterRenderPass<RenderGraphMaskPassData>("DisturbanceRender", out var passData))
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
                builder.SetRenderAttachment(maskTexture, 0, AccessFlags.ReadWrite);

                if (resourceData.activeDepthTexture.IsValid())
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                if (_downSampling == Downsampling.None)
                    builder.SetGlobalTextureAfterPass(maskTexture, DisturbanceMaskTex);

                builder.SetRenderFunc(static (RenderGraphMaskPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }

            if (_downSampling == Downsampling.None)
                return;

            TextureDesc downsampleDescriptor = maskDescriptor;
            downsampleDescriptor.name = "MaskDownCopyRT";
            downsampleDescriptor.clearBuffer = false;
            ApplyDownsampling(ref downsampleDescriptor);

            TextureHandle downsampleTexture = renderGraph.CreateTexture(downsampleDescriptor);

            if (_downSampling == Downsampling._4xBox)
                _renderMaskMat.SetFloat(SampleOffset, 2);

            RenderGraphUtils.BlitMaterialParameters blitParameters =
                new RenderGraphUtils.BlitMaterialParameters(maskTexture, downsampleTexture, _renderMaskMat, GetDownsamplePassIndex());
            blitParameters.sourceTexturePropertyID = CameraTexture;

            renderGraph.AddBlitPass(blitParameters, "DisturbanceMaskDownsample");
            SetGlobalTextureAfterPass(renderGraph, downsampleTexture, DisturbanceMaskTex, "Set Disturbance Mask");
        }
#endif

    #if UNIVERSAL_RP_13_1_2_OR_NEWER
        
        public void SetUp ( RTHandle cameraRTHandle )
        {
#if !UNITY_6000_3_OR_NEWER || (URP_COMPATIBILITY_MODE && !UNITY_6000_4_OR_NEWER)
#pragma warning disable CS0618

            RenderTextureDescriptor descrip = cameraRTHandle.rt.descriptor;
            
            descrip.colorFormat = RenderTextureFormat.RGHalf;
            descrip.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _DisturbanceMaskRTHandle, descrip, name: "DisturbanceMaskRT");
            
            
            switch (_downSampling)
            {
                case Downsampling._2xBilinear:
                    descrip.width /= 2;
                    descrip.height /= 2;
                    break;
                case Downsampling._4xBilinear:
                    descrip.width /= 4;
                    descrip.height /= 4;
                    break;
                case Downsampling._4xBox:
                    descrip.width /= 4;
                    descrip.height /= 4;
                    break;
            }
            if (_downSampling != Downsampling.None)
                RenderingUtils.ReAllocateIfNeeded(ref _DownRT, descrip, name:"MaskDownCopyRT");
#pragma warning restore CS0618
#endif
        }
    #else
#if !UNITY_6000_3_OR_NEWER || (URP_COMPATIBILITY_MODE && !UNITY_6000_4_OR_NEWER)
        //在AddPass之前触发

        public void SetUpDisturbanceMask(RenderTextureDescriptor descrip ,CommandBuffer cmd)
        {
            
            descrip.colorFormat = RenderTextureFormat.RGHalf;
            descrip.depthBufferBits = 0;
            cmd.GetTemporaryRT(_DisturbanceMaskRTID, descrip,FilterMode.Bilinear);
            
            switch (_downSampling)
            {
                case Downsampling._2xBilinear:
                    descrip.width /= 2;
                    descrip.height /= 2;
                    break;
                case Downsampling._4xBilinear:
                    descrip.width /= 4;
                    descrip.height /= 4;
                    break;
                case Downsampling._4xBox:
                    descrip.width /= 4;
                    descrip.height /= 4;
                    break;
            }
            if (_downSampling != Downsampling.None)
                cmd.GetTemporaryRT(_DownRTID, descrip,FilterMode.Bilinear);
        }
#endif
    #endif

#if !UNITY_6000_3_OR_NEWER || (URP_COMPATIBILITY_MODE && !UNITY_6000_4_OR_NEWER)
#pragma warning disable CS0618, CS0672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _Filtering = new FilteringSettings(RenderQueueRange.all);
            #if UNIVERSAL_RP_13_1_2_OR_NEWER
                _cameraDepthRTHandle = renderingData.cameraData.renderer.cameraDepthTargetHandle;
            #else
                _cameraDepthRTHandle = renderingData.cameraData.renderer.cameraDepthTarget;
                SetUpDisturbanceMask(renderingData.cameraData.cameraTargetDescriptor,cmd);
            #endif
            
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
#if UNIVERSAL_RP_13_1_2_OR_NEWER
                ConfigureTarget(_DisturbanceMaskRTHandle, _cameraDepthRTHandle);
#else
                ConfigureTarget(_DisturbanceMaskRTID, _cameraDepthRTHandle);
            
#endif
            //将RT清空
            ConfigureClear(ClearFlag.Color, _clearDisturbanceMaskColor);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            if (!(renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView))
                return;

            if (!_renderMaskMat)
            {
                return;
            }
            
            var DisturbanceDraw = CreateDrawingSettings(_shaderTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.DrawRenderers(renderingData.cullResults, ref DisturbanceDraw, ref _Filtering);

#if UNIVERSAL_RP_13_1_2_OR_NEWER
                if (_downSampling == Downsampling.None)
                {
                    cmd.SetGlobalTexture(DisturbanceMaskTex, _DisturbanceMaskRTHandle);
                }
                else
                {
                    _renderMaskMat.SetTexture(CameraTexture, _DisturbanceMaskRTHandle);
                    cmd.SetRenderTarget((RenderTargetIdentifier)_DownRT);

                    switch (_downSampling)
                    {
                        case Downsampling._2xBilinear:
                            Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, Vector2.one, _renderMaskMat, 0);
                            break;
                        case Downsampling._4xBilinear:
                            Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, Vector2.one, _renderMaskMat, 0);
                            break;
                        case Downsampling._4xBox:
                            _renderMaskMat.SetFloat(SampleOffset, 2);
                            Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, Vector2.one, _renderMaskMat, 1);
                            break;
                    }

                    cmd.SetGlobalTexture(DisturbanceMaskTex, _DownRT);
                }

#else

                if (_downSampling == Downsampling.None)
                {
                    cmd.SetGlobalTexture(DisturbanceMaskTex, _DisturbanceMaskRTHandle);
                }
                else
                {
                    // cmd.SetGlobalTexture(_DisturbanceMaskRTID, _DisturbanceMaskRTHandle);
                    //Bug:在非播放状态时，_DisturbanceMaskRTID在这里会经常丢失。造成画面闪烁。但是，只要有一个DistortObject在Scene中，并LockInspector，就不会闪烁，非常奇怪。
                    cmd.SetGlobalTexture(CameraTexture, _DisturbanceMaskRTHandle);
                    // _renderMaskMat.SetTexture(CameraTexture, Shader.GetGlobalTexture(_DisturbanceMaskRTID));
                    cmd.SetRenderTarget(_DownRT);
                    switch (_downSampling)
                    {
                        case Downsampling._2xBilinear:
                            // Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 0);
                            // cmd.Blit(_DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 0);
                            // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _renderMaskMat, 0, 2);
                            Blit(cmd, _DisturbanceMaskRTHandle,_DownRT,_renderMaskMat,0);
                            break;
                        case Downsampling._4xBilinear:
                            // Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 0);
                            // cmd.Blit(_DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 0);
                            // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _renderMaskMat, 0, 2);
                            Blit(cmd, _DisturbanceMaskRTHandle,_DownRT,_renderMaskMat,0);
                            break;
                        case Downsampling._4xBox:
                            _renderMaskMat.SetFloat(SampleOffset, 2);
                            // Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 1);
                            // cmd.Blit(_DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 1);
                            // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _renderMaskMat, 0, 3);
                            Blit(cmd, _DisturbanceMaskRTHandle,_DownRT,_renderMaskMat,1);
                            break;
                    }

                    cmd.SetGlobalTexture(DisturbanceMaskTex, _DownRT);
                }
#endif

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618, CS0672
#endif

        #if !UNIVERSAL_RP_13_1_2_OR_NEWER && (!UNITY_6000_3_OR_NEWER || (URP_COMPATIBILITY_MODE && !UNITY_6000_4_OR_NEWER))
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_DisturbanceMaskRTID);
            if (_downSampling != Downsampling.None)
                cmd.ReleaseTemporaryRT(_DownRTID);
        }

        //JustForSimple
        public void Dispose()
        {
        }
        #else

        public void Dispose()
        {
            _DisturbanceMaskRTHandle?.Release();
            _DownRT?.Release();
        }
        #endif
    }
}
