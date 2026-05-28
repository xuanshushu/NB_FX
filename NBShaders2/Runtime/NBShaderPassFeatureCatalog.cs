using System.Collections.Generic;

namespace NBShader
{
    /// <summary>
    /// Catalog of NBShader passes that can be controlled by feature tiering.
    /// UniversalForward is the primary pass and is intentionally not listed as a tier feature.
    /// </summary>
    internal static class NBShaderPassFeatureCatalog
    {
        public const string MainForwardPassName = "UniversalForward";

        public const string BackFirstPassId = "pass.backFirst";
        public const string CameraOpaqueDistortPassId = "pass.screenDistort.cameraOpaque";
        public const string DeferredDistortPassId = "pass.screenDistort.deferred";
        public const string DepthOnlyPassId = "pass.depthOnly";
        public const string ShadowCasterPassId = "pass.shadowCaster";
        public const string Universal2DPassId = "pass.universal2D";

        public static readonly NBShaderPassFeatureInfo[] RawPassFeatures =
        {
            new NBShaderPassFeatureInfo(BackFirstPassId, "SRPDefaultUnlit", "Back First Pass"),
            new NBShaderPassFeatureInfo(CameraOpaqueDistortPassId, "NBCameraOpaqueDistortPass", "Camera Opaque Distort Pass"),
            new NBShaderPassFeatureInfo(DeferredDistortPassId, "NBDeferredDistortPass", "Deferred Distort Pass"),
            new NBShaderPassFeatureInfo(DepthOnlyPassId, "DepthOnly", "Depth Only Pass"),
            new NBShaderPassFeatureInfo(ShadowCasterPassId, "ShadowCaster", "Shadow Caster Pass"),
            new NBShaderPassFeatureInfo(Universal2DPassId, "Universal2D", "Universal 2D Pass")
        };

        public static readonly string[] RawPassFeatureIds = BuildRawPassFeatureIds();

        private static readonly HashSet<string> RawPassFeatureIdSet = new HashSet<string>(RawPassFeatureIds);
        private static readonly Dictionary<string, NBShaderPassFeatureInfo> PassFeaturesById = BuildPassFeaturesById();
        private static readonly Dictionary<string, NBShaderPassFeatureInfo> PassFeaturesByPassName = BuildPassFeaturesByPassName();

        public static bool IsManagedPassFeature(string featureId)
        {
            return !string.IsNullOrEmpty(featureId) && RawPassFeatureIdSet.Contains(featureId);
        }

        public static bool TryGetPassFeature(string featureId, out NBShaderPassFeatureInfo feature)
        {
            if (string.IsNullOrEmpty(featureId))
            {
                feature = null;
                return false;
            }

            return PassFeaturesById.TryGetValue(featureId, out feature);
        }

        public static bool TryGetPassFeatureByPassName(string passName, out NBShaderPassFeatureInfo feature)
        {
            if (string.IsNullOrEmpty(passName))
            {
                feature = null;
                return false;
            }

            return PassFeaturesByPassName.TryGetValue(passName, out feature);
        }

        public static string[] GetDefaultAllowedPassFeatures(NBShaderFeatureTier tier)
        {
            return (string[])RawPassFeatureIds.Clone();
        }

        private static string[] BuildRawPassFeatureIds()
        {
            var result = new string[RawPassFeatures.Length];
            for (var i = 0; i < RawPassFeatures.Length; i++)
                result[i] = RawPassFeatures[i].id;
            return result;
        }

        private static Dictionary<string, NBShaderPassFeatureInfo> BuildPassFeaturesById()
        {
            var result = new Dictionary<string, NBShaderPassFeatureInfo>();
            for (var i = 0; i < RawPassFeatures.Length; i++)
                result[RawPassFeatures[i].id] = RawPassFeatures[i];
            return result;
        }

        private static Dictionary<string, NBShaderPassFeatureInfo> BuildPassFeaturesByPassName()
        {
            var result = new Dictionary<string, NBShaderPassFeatureInfo>();
            for (var i = 0; i < RawPassFeatures.Length; i++)
                result[RawPassFeatures[i].passName] = RawPassFeatures[i];
            return result;
        }
    }

    internal sealed class NBShaderPassFeatureInfo
    {
        public readonly string id;
        public readonly string passName;
        public readonly string displayName;

        public NBShaderPassFeatureInfo(string id, string passName, string displayName)
        {
            this.id = id;
            this.passName = passName;
            this.displayName = displayName;
        }
    }
}
