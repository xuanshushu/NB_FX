// Current URP 17.3 product candidate. The asmref compiles this file into the
// URP Editor assembly; URP-absent installation is deferred to T10/D20.
using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class NBGraphUnlitSubTarget : UniversalSubTarget
    {
        const string kURPUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl";
        const string kPassRoot = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/Passes/";

        public NBGraphUnlitSubTarget() => displayName = "NB FX Unlit (URP)";

        protected override ShaderID shaderID => ShaderID.SG_Unlit;
        public override bool IsActive() => true;

        UniversalUnlitSubTarget Builtin() => new UniversalUnlitSubTarget { target = target };

        public override void Setup(ref TargetSetupContext context)
        {
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                context.AddCustomEditorForRenderPipeline(
                    typeof(UnityEditor.Rendering.Universal.VFXShaderGraphUnlitGUI).FullName,
                    typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset));
#endif
            int index = context.subShaders.Count;
            Builtin().Setup(ref context);
            var subShader = context.subShaders[index];
            var passes = new PassCollection();
            bool found = false;
            foreach (var item in subShader.passes)
            {
                passes.Add(item.descriptor, item.fieldConditions);
                if (item.descriptor.referenceName != "SHADERPASS_UNLIT") continue;
                AddDistortionPass(passes, item.descriptor, "NBCameraOpaqueDistortPass", "NBGraphCameraOpaquePass.hlsl");
                AddDistortionPass(passes, item.descriptor, "NBDeferredDistortPass", "NBGraphDeferredDistortPass.hlsl");
                found = true;
            }
            if (!found)
                throw new InvalidOperationException("NB FX Graph: URP Unlit forward Pass not found.");
            subShader.passes = passes;
            // The delegated built-in Unlit instance is not this active VFX
            // SubTarget. Convert this instance only after its NB passes exist.
            context.subShaders[index] = PostProcessSubShader(subShader);
        }

        static void AddDistortionPass(PassCollection passes, PassDescriptor forward,
            string lightMode, string fragmentInclude)
        {
            var pass = forward;
            pass.displayName = lightMode;
            pass.lightMode = lightMode;
            pass.useInPreview = false;
            // NBPostprocess expects the same source-alpha accumulation as the
            // ShaderLab distortion passes, regardless of the Graph's Forward
            // blend mode. In particular the deferred mask must not overwrite
            // earlier particles when the Graph material uses One/Zero.
            pass.renderStates = new RenderStateCollection
            {
                RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha),
                RenderState.Cull("[_Cull]"),
                RenderState.ZTest("[_ZTest]"),
                RenderState.ZWrite("Off")
            };
            var includes = new IncludeCollection();
            bool replaced = false;
            foreach (var include in forward.includes)
            {
                if (include.path == kURPUnlitPass)
                {
                    includes.Add(kPassRoot + fragmentInclude, include.location);
                    replaced = true;
                }
                else
                {
                    includes.AddInternal(include.guid, include.path, include.location,
                        include.fieldConditions, include.shouldIncludeWithPragmas);
                }
            }
            if (!replaced)
                throw new InvalidOperationException("NB FX Graph: URP Unlit fragment include not found.");
            pass.includes = includes;
            passes.Add(pass);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context) => Builtin().GetActiveBlocks(ref context);
        public override void GetFields(ref TargetFieldContext context) => base.GetFields(ref context);
        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode mode)
            => Builtin().CollectShaderProperties(collector, mode);
        public override void ProcessPreviewMaterial(Material material) => Builtin().ProcessPreviewMaterial(material);
        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
            => Builtin().GetPropertiesGUI(ref context, onChange, registerUndo);
    }
}
