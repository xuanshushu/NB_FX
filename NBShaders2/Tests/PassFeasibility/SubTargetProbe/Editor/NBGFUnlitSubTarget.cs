// GF-only probe. This is deliberately not a production Shader Graph implementation.
using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Compiled into the URP Editor assembly by the neighboring .asmref, not by
    // changing any file in the official URP package.
    sealed class NBGFUnlitSubTarget : UniversalSubTarget
    {
        const string kURPUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl";
        const string kProbeRoot = "Packages/com.xuanxuan.nb.fx/NBShaders2/Tests/PassFeasibility/SubTargetProbe/";

        public NBGFUnlitSubTarget()
        {
            displayName = "NB GF Unlit (test only)";
        }

        protected override ShaderID shaderID => ShaderID.SG_Unlit;
        public override bool IsActive() => true;

        UniversalUnlitSubTarget Builtin()
        {
            return new UniversalUnlitSubTarget { target = target };
        }

        public override void Setup(ref TargetSetupContext context)
        {
            // Reuse the installed URP version's Unlit implementation; do not
            // copy its pass template, pragmas, keywords, or HLSL into NB_FX.
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                context.AddCustomEditorForRenderPipeline(
                    typeof(UnityEditor.Rendering.Universal.VFXShaderGraphUnlitGUI).FullName,
                    typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset));
#endif
            int subShaderIndex = context.subShaders.Count;
            Builtin().Setup(ref context);
            var subShader = context.subShaders[subShaderIndex];

            var passes = new PassCollection();
            bool found = false;
            foreach (var item in subShader.passes)
            {
                passes.Add(item.descriptor, item.fieldConditions);
                if (item.descriptor.referenceName == "SHADERPASS_UNLIT")
                {
                    AddProbePass(passes, item.descriptor, "NBCameraOpaqueDistortPass", "NBGFCameraOpaquePass.hlsl");
                    AddProbePass(passes, item.descriptor, "NBDeferredDistortPass", "NBGFDeferredDistortPass.hlsl");
                    found = true;
                }
            }
            if (!found)
                throw new InvalidOperationException("GF probe: URP Unlit forward pass not found.");

            subShader.passes = passes;
            // The inner Unlit instance is not the active VFX SubTarget, so VFX
            // conversion must run on this instance after the probe passes exist.
            context.subShaders[subShaderIndex] = PostProcessSubShader(subShader);
        }

        static void AddProbePass(PassCollection passes, PassDescriptor forward, string lightMode, string probeInclude)
        {
            var pass = forward;
            pass.displayName = lightMode;
            pass.lightMode = lightMode;
            pass.useInPreview = false;
            // Keep URP's own pre/post-graph includes. Replace only the final
            // fragment entry point in this isolated GF probe, not the Forward pass.
            var includes = new IncludeCollection();
            bool replaced = false;
            foreach (var include in forward.includes)
            {
                if (include.path == kURPUnlitPass)
                {
                    includes.Add(kProbeRoot + probeInclude, include.location);
                    replaced = true;
                }
                else
                {
                    includes.AddInternal(include.guid, include.path, include.location,
                        include.fieldConditions, include.shouldIncludeWithPragmas);
                }
            }
            if (!replaced)
                throw new InvalidOperationException("GF probe: URP Unlit fragment include not found.");
            pass.includes = includes;
            passes.Add(pass);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context) => Builtin().GetActiveBlocks(ref context);
        public override void GetFields(ref TargetFieldContext context) => base.GetFields(ref context);
        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
            => Builtin().CollectShaderProperties(collector, generationMode);
        public override void ProcessPreviewMaterial(Material material) => Builtin().ProcessPreviewMaterial(material);
        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
            => Builtin().GetPropertiesGUI(ref context, onChange, registerUndo);
    }
}
