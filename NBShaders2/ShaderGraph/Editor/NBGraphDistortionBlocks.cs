using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // The existing asmref builds this into the URP Editor assembly, where
    // Shader Graph's internal block-generation API is already accessible.
    internal static class NBGraphDistortionBlocks
    {
        [GenerateBlocks("Universal Render Pipeline/NB FX")]
        public struct SurfaceDescription
        {
            public static BlockFieldDescriptor SignedRG = new BlockFieldDescriptor(
                "SurfaceDescription", "NBDistortionSignedRG", "NB Distortion Signed RG",
                "SURFACEDESCRIPTION_NB_DISTORTION_SIGNED_RG",
                new Vector2Control(Vector2.zero), ShaderStage.Fragment);

            public static BlockFieldDescriptor NoiseMask = new BlockFieldDescriptor(
                "SurfaceDescription", "NBDistortionNoiseMask", "NB Distortion Noise Mask",
                "SURFACEDESCRIPTION_NB_DISTORTION_NOISE_MASK",
                new FloatControl(1.0f), ShaderStage.Fragment);
        }
    }
}
