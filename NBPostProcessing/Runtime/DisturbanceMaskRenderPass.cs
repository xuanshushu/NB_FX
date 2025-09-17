using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

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

    #if UNIVERSAL_RP_13_1_2_OR_NEWER
        
        public void SetUp ( RTHandle cameraRTHandle )
        {

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
            RenderingUtils.ReAllocateIfNeeded(ref _DownRT, descrip, name:"MaskDownCopyRT");
        }
    #else
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
            cmd.GetTemporaryRT(_DownRTID, descrip,FilterMode.Bilinear);
        }
    #endif
        
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

        private readonly Color _clearDisturbanceMaskColor = new Color(0f, 0f, 0f, 1f);
        
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
                    default:
                        Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, Vector2.one, _renderMaskMat, 0);
                        break;
                }

#else

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
                    default:
                        // Blitter.BlitTexture(cmd, _DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 0);  
                        // cmd.Blit(_DisturbanceMaskRTHandle, _DownRT, _renderMaskMat, 0);
                        // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _renderMaskMat, 0, 2);
                        Blit(cmd, _DisturbanceMaskRTHandle,_DownRT,_renderMaskMat,0);
                        break;
                }
#endif
                cmd.SetGlobalTexture("_DisturbanceMaskTex", _DownRT);

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        #if !UNIVERSAL_RP_13_1_2_OR_NEWER
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_DisturbanceMaskRTID);
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