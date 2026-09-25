using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NBFX.GF.URP.Tests
{
    // Test-only renderer feature. It is appended to RendererData in memory,
    // then removed in finally; no product RendererFeature or asset is saved.
    internal sealed class NBGFMaskCaptureFeature : ScriptableRendererFeature
    {
        public Material material;
        MaskCapturePass pass;

        public override void Create()
        {
            pass = new MaskCapturePass(material)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
                renderer.EnqueuePass(pass);
        }

        sealed class MaskCapturePass : ScriptableRenderPass
        {
            static readonly int MaskId = Shader.PropertyToID("_DisturbanceMaskTex");
            readonly Material material;

            sealed class PassData { public Material material; }

            public MaskCapturePass(Material material) { this.material = material; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (!resources.activeColorTexture.IsValid()) return;

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("NBGF Mask View", out var data))
                {
                    data.material = material;
                    builder.UseGlobalTexture(MaskId, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData d, RasterGraphContext context) =>
                    {
                        context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0,
                            MeshTopology.Triangles, 3, 1);
                    });
                }
            }
        }
    }
}
