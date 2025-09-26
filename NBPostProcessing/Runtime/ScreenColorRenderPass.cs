using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace NBShader
{
    public class ScreenColorRenderPass : ScriptableRenderPass
    {
#if UNIVERSAL_RP_13_1_2_OR_NEWER
        private RTHandle _screenColorHandle;
        private RTHandle _tempRTHandle;
#else
        private RenderTargetIdentifier _screenColorHandle;
        private static readonly int _screenColorRTID = Shader.PropertyToID("_screenColorRT");
        private RenderTargetIdentifier _tempRTHandle = new RenderTargetIdentifier(_tempRTID);
        private static readonly int _tempRTID = Shader.PropertyToID("CopyColorRT");
#endif
        private ProfilingSampler _profilingSampler;
        private readonly Downsampling _downSampling;
        readonly Material _material;
        private static readonly int CameraTexture = Shader.PropertyToID("_CameraTexture");
        private static readonly int SampleOffset = Shader.PropertyToID("_SampleOffset");

        public ScreenColorRenderPass(Material material, Downsampling downSampling)
        {
            _material = material;
            _downSampling = downSampling;
        }
        

#if UNIVERSAL_RP_13_1_2_OR_NEWER
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(_screenColorHandle);
           
        }
        
        public void SetUp(RTHandle colorHandle)
        {
            _profilingSampler ??= new ProfilingSampler("ScreenColorRender");
            _screenColorHandle = colorHandle;
            RenderTextureDescriptor descriptor = _screenColorHandle.rt.descriptor;
            descriptor.autoGenerateMips = true;
            descriptor.useMipMap = true;
            switch (_downSampling)
            {
                case Downsampling._2xBilinear:
                    descriptor.width /= 2;
                    descriptor.height /= 2;
                    break;
                case Downsampling._4xBilinear:
                    descriptor.width /= 4;
                    descriptor.height /= 4;
                    break;
                case Downsampling._4xBox:
                    descriptor.width /= 4;
                    descriptor.height /= 4;
                    break;
            }
            RenderingUtils.ReAllocateIfNeeded(ref _tempRTHandle, descriptor,name:"CopyColorRT");
        }
        
        
#else
      
        FieldInfo  cameraColorAttachment = typeof(UniversalRenderer).GetField("m_ActiveCameraColorAttachment", BindingFlags.NonPublic|BindingFlags.Instance);
            
      
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
          
            var renderer = (UniversalRenderer)renderingData.cameraData.renderer;
  
            // cmd.SetGlobalTexture(_screenColorRTID,_screenColorHandle);
            SetUpCopyColorRT(renderer,renderingData.cameraData.cameraTargetDescriptor,cmd);
            ConfigureTarget(_tempRTHandle);
            ConfigureClear(ClearFlag.Color, Color.clear);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        public void SetUp(ScriptableRenderer renderer)
        {
            RenderTargetHandle value = (RenderTargetHandle)cameraColorAttachment.GetValue(renderer);
            _screenColorHandle = value.Identifier();
            // _screenColorHandle = colorTarget;
        }

        public void SetUpCopyColorRT(ScriptableRenderer renderer,RenderTextureDescriptor descriptor ,CommandBuffer cmd)
        {
            descriptor.autoGenerateMips = true;
            descriptor.useMipMap = true;
            switch (_downSampling)
            {
                case Downsampling._2xBilinear:
                    descriptor.width /= 2;
                    descriptor.height /= 2;
                    break;
                case Downsampling._4xBilinear:
                    descriptor.width /= 4;
                    descriptor.height /= 4;
                    break;
                case Downsampling._4xBox:
                    descriptor.width /= 4;
                    descriptor.height /= 4;
                    break;
            }
            cmd.GetTemporaryRT( _tempRTID, descriptor,FilterMode.Bilinear);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_tempRTID);
        }
#endif

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!(renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView))
                return;
            if (_material == null)
                return;
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _profilingSampler))
            {
#if UNIVERSAL_RP_13_1_2_OR_NEWER

                _material.SetTexture(CameraTexture, _screenColorHandle);
                cmd.SetRenderTarget((RenderTargetIdentifier)_tempRTHandle);
                switch (_downSampling)
                {
                    case Downsampling._2xBilinear:
                        
                        Blitter.BlitTexture(cmd, _screenColorHandle, Vector2.one, _material, 0);
                        break;
                    case Downsampling._4xBilinear:
                        Blitter.BlitTexture(cmd, _screenColorHandle, Vector2.one, _material, 0);
                        break;
                    case Downsampling._4xBox:
                        _material.SetFloat(SampleOffset,2);
                        Blitter.BlitTexture(cmd, _screenColorHandle, Vector2.one, _material, 1);
                        break;
                    default:
                        Blitter.BlitTexture(cmd, _screenColorHandle, Vector2.one, _material, 0);  
                        break;
                }
#else
                cmd.SetGlobalTexture(_screenColorRTID,_screenColorHandle);

                _material.SetTexture(CameraTexture,Shader.GetGlobalTexture(_screenColorRTID));
                cmd.SetRenderTarget(_tempRTHandle);
                switch (_downSampling)
                {
                    case Downsampling._2xBilinear:
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0,0);
                        break;
                    case Downsampling._4xBilinear:
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0,0);
                        break;
                    case Downsampling._4xBox:
                        _material.SetFloat(SampleOffset,2);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0,1);
                        break;
                    default:
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0,0);
                        break;
                }
                
#endif
                cmd.SetGlobalTexture("_ScreenColorCopy1", _tempRTHandle);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            #if UNIVERSAL_RP_13_1_2_OR_NEWER
                _tempRTHandle?.Release();
            #endif
        }
    }
}