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
        const string kURPShadowPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";
        const string kPassRoot = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/Passes/";

        public NBGraphUnlitSubTarget() => displayName = "NB FX Unlit (URP)";

        protected override ShaderID shaderID => ShaderID.SG_Unlit;
        public override bool IsActive() => true;

        UniversalUnlitSubTarget Builtin() => new UniversalUnlitSubTarget { target = target };

        public override void Setup(ref TargetSetupContext context)
        {
            var urpType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            // UniversalTarget's explicit Custom Editor takes precedence. Otherwise
            // register one GUI before delegating to built-in Unlit, which then
            // observes the registration and does not add a second one.
            if (!context.HasCustomEditorForRenderPipeline(urpType))
            {
#if HAS_VFX_GRAPH
                if (TargetsVFX())
                    context.AddCustomEditorForRenderPipeline(
                        typeof(UnityEditor.Rendering.Universal.VFXShaderGraphUnlitGUI).FullName, urpType);
                else
#endif
                    context.AddCustomEditorForRenderPipeline("NBShaderEditor.NBShaderGraphGUI", urpType);
            }
            int index = context.subShaders.Count;
            Builtin().Setup(ref context);
            var subShader = context.subShaders[index];
            var passes = new PassCollection();
            bool found = false;
            foreach (var item in subShader.passes)
            {
                var pass = item.descriptor;
                bool forward = pass.referenceName == "SHADERPASS_UNLIT";
                if (forward)
                    pass = WithNBDistortionBlocks(pass);
                if (forward || UsesNBStencil(pass.lightMode))
                    pass.renderStates = WithNBRenderStates(pass.renderStates, forward);
                passes.Add(forward ? WithNBFragmentInclude(pass, "NBGraphForwardPass.hlsl") :
                    pass.lightMode == "ShadowCaster" ? WithNBFragmentInclude(pass, "NBGraphShadowCasterPass.hlsl", kURPShadowPass) :
                    pass, item.fieldConditions);
                if (!forward) continue;
                AddDistortionPass(passes, pass, "NBCameraOpaqueDistortPass", "NBGraphCameraOpaquePass.hlsl");
                AddDistortionPass(passes, pass, "NBDeferredDistortPass", "NBGraphDeferredDistortPass.hlsl");
                found = true;
            }
            if (!found)
                throw new InvalidOperationException("NB FX Graph: URP Unlit forward Pass not found.");
            subShader.passes = passes;
            // The delegated built-in Unlit instance is not this active VFX
            // SubTarget. Convert this instance only after its NB passes exist.
            context.subShaders[index] = PostProcessSubShader(subShader);
        }

        static PassDescriptor WithNBDistortionBlocks(PassDescriptor pass)
        {
            if (pass.validPixelBlocks == null)
                throw new InvalidOperationException("NB FX Graph: URP Unlit pixel blocks not found.");
            var existing = pass.validPixelBlocks;
            var blocks = new BlockFieldDescriptor[existing.Length + 2];
            Array.Copy(existing, blocks, existing.Length);
            blocks[existing.Length] = NBGraphDistortionBlocks.SurfaceDescription.SignedRG;
            blocks[existing.Length + 1] = NBGraphDistortionBlocks.SurfaceDescription.NoiseMask;
            pass.validPixelBlocks = blocks;
            return pass;
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
                RenderState.ZWrite("Off"),
                NBStencilState(),
                RenderState.ColorMask("ColorMask [_ColorMask]")
            };
            passes.Add(WithNBFragmentInclude(pass, fragmentInclude));
        }

        static PassDescriptor WithNBFragmentInclude(PassDescriptor pass, string fragmentInclude,
            string replacedInclude = kURPUnlitPass)
        {
            var includes = new IncludeCollection();
            bool replaced = false;
            foreach (var include in pass.includes)
            {
                if (include.path == replacedInclude)
                {
                    includes.Add(kPassRoot + fragmentInclude, include.location);
                    replaced = true;
                }
                else
                    includes.AddInternal(include.guid, include.path, include.location,
                        include.fieldConditions, include.shouldIncludeWithPragmas);
            }
            if (!replaced)
                throw new InvalidOperationException("NB FX Graph: URP Unlit fragment include not found.");
            pass.includes = includes;
            return pass;
        }

        // The original ShaderLab Stencil block belongs to the SubShader, so it
        // also constrains its depth and shadow passes. Keep URP's existing state
        // descriptors (notably DepthOnly R and ShadowCaster 0 ColorMask) intact.
        // Selection/Picking and XR motion-vector passes are URP editor/runtime
        // internals, not equivalents of the original NBShader passes.
        static bool UsesNBStencil(string lightMode) =>
            lightMode == "DepthOnly" || lightMode == "ShadowCaster" ||
            lightMode == "DepthNormalsOnly" || lightMode == "UniversalGBuffer" ||
            lightMode == "MotionVectors";

        static RenderStateCollection WithNBRenderStates(RenderStateCollection original,
            bool colorMask)
        {
            var states = new RenderStateCollection();
            if (original != null)
                foreach (var item in original)
                    states.Add(item.descriptor, item.fieldConditions);
            states.Add(NBStencilState());
            if (colorMask)
                states.Add(RenderState.ColorMask("ColorMask [_ColorMask]"));
            return states;
        }

        static RenderStateDescriptor NBStencilState() => RenderState.Stencil(
            new StencilDescriptor
            {
                Ref = "[_Stencil]",
                Comp = "[_StencilComp]",
                Pass = "[_StencilOp]",
                Fail = "[_StencilFail]",
                ZFail = "[_StencilZFail]",
                ReadMask = "[_StencilReadMask]",
                WriteMask = "[_StencilWriteMask]"
            });

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            Builtin().GetActiveBlocks(ref context);
            // Also active when GraphData asks without a pass: its fragment
            // context must keep the serialized NB blocks and their CF edges.
            context.AddBlock(NBGraphDistortionBlocks.SurfaceDescription.SignedRG);
            context.AddBlock(NBGraphDistortionBlocks.SurfaceDescription.NoiseMask);
        }
        public override void GetFields(ref TargetFieldContext context) => base.GetFields(ref context);
        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode mode)
        {
            Builtin().CollectShaderProperties(collector, mode);
            // ShaderLab render-state substitutions need material properties, not
            // duplicate HLSL uniforms. Match the old names/defaults exactly.
            collector.AddFloatProperty("_ColorMask", 15.0f);
            collector.AddFloatProperty("_Stencil", 0.0f);
            collector.AddFloatProperty("_StencilComp", 8.0f);
            collector.AddFloatProperty("_StencilOp", 0.0f);
            collector.AddFloatProperty("_StencilFail", 0.0f);
            collector.AddFloatProperty("_StencilZFail", 0.0f);
            collector.AddFloatProperty("_StencilReadMask", 255.0f);
            collector.AddFloatProperty("_StencilWriteMask", 255.0f);
        }
        public override void ProcessPreviewMaterial(Material material) => Builtin().ProcessPreviewMaterial(material);
        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
            => Builtin().GetPropertiesGUI(ref context, onChange, registerUndo);
    }
}
