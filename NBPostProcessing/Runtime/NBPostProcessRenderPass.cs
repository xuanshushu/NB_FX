using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
namespace NBShader
{
    public class NBPostProcessRenderPass : ScriptableRenderPass
    {
        private ProfilingSampler _profilingSampler;
        public static Material _material;
        public Mesh _fullScreenMesh;

        public NBPostProcessFlags _shaderFlag => PostProcessingManager.flags;

        private Vector4 _lastOutlineProps;
        public Vector4 outLinePorps = Vector4.one;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!(renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView))
                return;
   
            // if(!_shaderFlag.CheckFlagBits(NBPostProcessFlags.FLAG_BIT_NB_POSTPROCESS_ON))return;//Disturbance需要执行
            
            //ConfigureTarget()
            CommandBuffer cmdBuffer = CommandBufferPool.Get();
            cmdBuffer.Clear();
            // cmdBuffer.name = "NBPostProcess";
          
            using (new ProfilingScope(cmdBuffer,_profilingSampler))
            {
                Camera camera = renderingData.cameraData.camera;
                cmdBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
             
                if(_material == null) return;
                cmdBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 0);
                
                cmdBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                
            }
            
            context.ExecuteCommandBuffer(cmdBuffer);
            CommandBufferPool.Release(cmdBuffer);
        }

        public  NBPostProcessRenderPass(Material mat,Mesh mesh)
        {
            _material = mat;
            _fullScreenMesh = mesh;
            _profilingSampler ??= new ProfilingSampler("NBPostProcess");

        }
    }
}