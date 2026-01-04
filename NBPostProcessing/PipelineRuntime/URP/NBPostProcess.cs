// using ConfigSystem.MConfig;
// using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NBShader
{
    public class NBPostProcess : ScriptableRendererFeature
    {
        private NBPostProcessRenderPass _renderPass;
        private DisturbanceMaskRenderPass _disturbanceMaskRenderPass;
        private ScreenColorRenderPass _screenColorRenderPass;
        private RenderCameraOpaqueDistortObjectPass _renderCameraOpaqueDistortObjectPass;


        public static Material NBPostProcessMaterial;
        
        //public MaskFormat maskFormat = MaskFormat.RG32;
        public Downsampling downSampling = Downsampling.None;
        
        private Material _disturbanceDownSampleMat;
        private Material _screenColorDownSampleMat;
        private float _screenHeight;
        private ProfilingSampler _profilingSampler;
        
        // private PostProcessingManager manager;-+
        static Mesh s_FullscreenTriangle;
        /// <summary>
        /// A fullscreen triangle mesh.抄自Unity的后处理包,拿到一个全屏的Triangle。
        /// </summary>
        static Mesh fullscreenTriangle;


        private bool canFind = false;
        public override void Create()
        {
            
            if (Shader.Find("XuanXuan/ColorBlit") == null || 
                Shader.Find("XuanXuan/Postprocess/NBPostProcessUber") == null)
            {
                canFind = false;
                return;
            }
            else
            {
                canFind = true;
            }
            
            #if UNIVERSAL_RP_13_1_2_OR_NEWER
                _screenColorDownSampleMat = CoreUtils.CreateEngineMaterial(Shader.Find("XuanXuan/ColorBlit"));
            #else                
                _screenColorDownSampleMat = CoreUtils.CreateEngineMaterial(Shader.Find("XuanXuan/ColorBufferBlit"));
            #endif
            _screenColorRenderPass = new ScreenColorRenderPass(_screenColorDownSampleMat, downSampling);
            _screenColorRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            
            _profilingSampler = new ProfilingSampler("DisturbanceRender");
            _disturbanceDownSampleMat = CoreUtils.CreateEngineMaterial(Shader.Find("XuanXuan/ColorBlit"));
            _disturbanceMaskRenderPass = new DisturbanceMaskRenderPass(_profilingSampler,_disturbanceDownSampleMat,downSampling);
            _disturbanceMaskRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            _renderCameraOpaqueDistortObjectPass = new RenderCameraOpaqueDistortObjectPass();
            _renderCameraOpaqueDistortObjectPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            
            #if !UNIVERSAL_RP_13_1_2_OR_NEWER
            _profilingSampler = new ProfilingSampler("DisturbanceDownRTBlit");
            #endif
            
            if (fullscreenTriangle == null)
            {
                /*UNITY_NEAR_CLIP_VALUE*/
                float nearClipZ = -1;
                if (SystemInfo.usesReversedZBuffer)
                    nearClipZ = 1;
                
                fullscreenTriangle = new Mesh();
                fullscreenTriangle.vertices = GetFullScreenTriangleVertexPosition(nearClipZ);
                fullscreenTriangle.uv = GetFullScreenTriangleTexCoord();
                fullscreenTriangle.triangles = new int[3] { 0, 1, 2 };
            }

            // Shader shader = Shader.Find("XuanXuan/Postprocess/NBPostProcessUber");
            NBPostProcessMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("XuanXuan/Postprocess/NBPostProcessUber"));

            PostProcessingManager.InitMat();
     
      
            _renderPass = new NBPostProcessRenderPass(NBPostProcessMaterial,fullscreenTriangle);
            _renderPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
           
        }
        
        #if UNIVERSAL_RP_13_1_2_OR_NEWER
        
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if ((renderingData.cameraData.cameraType == CameraType.Game ||
                renderingData.cameraData.cameraType == CameraType.SceneView) && canFind)
            {
                
                _disturbanceMaskRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
                _disturbanceMaskRenderPass.SetUp(renderer.cameraColorTargetHandle);
                
                _screenColorRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
                _screenColorRenderPass.SetUp(renderer.cameraColorTargetHandle);
            }
        }

        #endif

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if ((renderingData.cameraData.cameraType == CameraType.Game ||
                renderingData.cameraData.cameraType == CameraType.SceneView) && canFind)
            {
                
                #if !UNIVERSAL_RP_13_1_2_OR_NEWER
                    _screenColorRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
                    _screenColorRenderPass.SetUp(renderer);
                #endif
                renderer.EnqueuePass(_renderCameraOpaqueDistortObjectPass);
                renderer.EnqueuePass(_screenColorRenderPass);
                renderer.EnqueuePass(_disturbanceMaskRenderPass);
                renderer.EnqueuePass(_renderPass);
            }
        }
        
        // Should match Common.hlsl
        static Vector3[] GetFullScreenTriangleVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
        {
            var r = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                Vector2 uv = new Vector2((i << 1) & 2, i & 2);
                r[i] = new Vector3(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, z);
            }
            return r;
        }

        // Should match Common.hlsl
        static Vector2[] GetFullScreenTriangleTexCoord()
        {
            var r = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                if (SystemInfo.graphicsUVStartsAtTop)
                    r[i] = new Vector2((i << 1) & 2, 1.0f - (i & 2));
                else
                    r[i] = new Vector2((i << 1) & 2, i & 2);
            }
            return r;
        }
        
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_disturbanceDownSampleMat);
            //CoreUtils.Destroy(NBPostProcessMaterial);
            _disturbanceMaskRenderPass?.Dispose();
            CoreUtils.Destroy(_screenColorDownSampleMat);
            _screenColorRenderPass?.Dispose();


        }
    }
}
