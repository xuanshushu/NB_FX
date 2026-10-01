using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBFX.Baseline.Tests
{
    // Clone-only observer: no renderer, shader, material, texture or asset mutation.
    [InitializeOnLoad]
    static class NBFXRenderDiagnostic
    {
        static int beginCount, pomBegin, pomEnd;
        static NBFXRenderDiagnostic()
        {
            if (System.Environment.GetEnvironmentVariable("NBFX_RT_DIAGNOSTIC") != "1") return;
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }
        static void Begin(ScriptableRenderContext context, Camera camera)
        {
            ++beginCount;
            if (!camera || camera.name != "POM1 camera" || ++pomBegin > 64) return;
            Log("begin", camera, pomBegin);
        }
        static void End(ScriptableRenderContext context, Camera camera)
        {
            if (!camera || camera.name != "POM1 camera" || ++pomEnd > 64) return;
            Log("end", camera, pomEnd);
        }
        static void Log(string stage, Camera c, int local)
        {
            var rt = c.targetTexture;
            Debug.Log("NBFX_RT_DIAGNOSTIC stage=" + stage + " globalBegins=" + beginCount +
                " local=" + local + " frame=" + Time.frameCount + " camera=" + c.GetInstanceID() +
                " scene=" + c.scene.handle + " sceneValid=" + c.scene.IsValid() +
                " active=" + c.gameObject.activeInHierarchy + " enabled=" + c.enabled +
                " target=" + (rt ? rt.GetInstanceID().ToString() : "null") +
                " created=" + (rt && rt.IsCreated()) + " clear=" + c.clearFlags +
                " bg=" + c.backgroundColor + " cull=" + c.cullingMask +
                " activeRT=" + (RenderTexture.active ? RenderTexture.active.GetInstanceID().ToString() : "null"));
        }
    }
}
