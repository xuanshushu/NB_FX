#if !UNITY_6000_3_OR_NEWER || (URP_COMPATIBILITY_MODE && !UNITY_6000_4_OR_NEWER)
#define NB_URP_COMPATIBILITY_PATH
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;
#if UNIVERSAL_RP_17_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

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
        private static readonly int ScreenColorCopy = Shader.PropertyToID("_ScreenColorCopy1");

        public ScreenColorRenderPass(Material material, Downsampling downSampling)
        {
            _material = material;
            _downSampling = downSampling;
#if UNIVERSAL_RP_17_0_OR_NEWER
            requiresIntermediateTexture = true;
#endif
        }

#if UNIVERSAL_RP_17_0_OR_NEWER
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
            if (!IsSupportedCamera(cameraData.cameraType) || _material == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid())
                return;

            TextureDesc descriptor = renderGraph.GetTextureDesc(source);
            descriptor.name = "CopyColorRT";
            descriptor.clearBuffer = false;
            descriptor.depthBufferBits = DepthBits.None;
            descriptor.autoGenerateMips = true;
            descriptor.useMipMap = true;
            ApplyDownsampling(ref descriptor);

            TextureHandle destination = renderGraph.CreateTexture(descriptor);

            if (_downSampling == Downsampling._4xBox)
                _material.SetFloat(SampleOffset, 2);

            RenderGraphUtils.BlitMaterialParameters blitParameters =
                new RenderGraphUtils.BlitMaterialParameters(source, destination, _material, GetDownsamplePassIndex());
            blitParameters.sourceTexturePropertyID = CameraTexture;

            renderGraph.AddBlitPass(blitParameters, "ScreenColorRender");
            SetGlobalTextureAfterPass(renderGraph, destination, ScreenColorCopy, "Set Screen Color Copy");
        }
#endif
        

#if UNIVERSAL_RP_13_1_2_OR_NEWER
#if NB_URP_COMPATIBILITY_PATH
#pragma warning disable CS0618, CS0672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(_screenColorHandle);
           
        }
#pragma warning restore CS0618, CS0672
#endif
        
        public void SetUp(RTHandle colorHandle)
        {
#if NB_URP_COMPATIBILITY_PATH
#pragma warning disable CS0618
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
#pragma warning restore CS0618
#endif
        }
        
        
#else
#if NB_URP_COMPATIBILITY_PATH

        FieldInfo  cameraColorAttachment = typeof(UniversalRenderer).GetField("m_ActiveCameraColorAttachment", BindingFlags.NonPublic|BindingFlags.Instance);
            
#pragma warning disable CS0618, CS0672
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
#pragma warning restore CS0618, CS0672

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
#endif

#if NB_URP_COMPATIBILITY_PATH
#pragma warning disable CS0618, CS0672
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
                cmd.SetGlobalTexture(ScreenColorCopy, _tempRTHandle);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618, CS0672
#endif

        public void Dispose()
        {
            #if UNIVERSAL_RP_13_1_2_OR_NEWER && NB_URP_COMPATIBILITY_PATH
                _tempRTHandle?.Release();
            #endif
        }
    }
}
