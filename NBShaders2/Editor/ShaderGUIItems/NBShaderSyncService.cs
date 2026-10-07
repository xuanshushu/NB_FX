using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;
using NBShaders2.Editor.FeatureLevel;

namespace NBShaderEditor
{
    public class NBShaderSyncService
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";
        private const string StencilConfigAssetPath = "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/StencilConfig.asset";
        private readonly NBShaderRootItem _rootItem;
        int _graphFreshPreflightDepth;
        internal readonly struct GraphFreshPreflightScope : IDisposable
        {
            readonly NBShaderSyncService owner;
            internal GraphFreshPreflightScope(NBShaderSyncService owner,bool invalidatePass)
            {
                this.owner=owner;
                if(invalidatePass)owner._rootItem.InvalidateGraphGUIReadPass();
                ++owner._graphFreshPreflightDepth;
            }
            public void Dispose(){if(owner!=null)--owner._graphFreshPreflightDepth;}
        }
        internal GraphFreshPreflightScope BeginGraphFreshPreflightRead(bool invalidatePass=true)
        {
            if(_rootItem.GraphGUIReadPass==0)return default; // Native/backends: no Root/Context touches.
            return new GraphFreshPreflightScope(this,invalidatePass);
        }
        struct GraphSavedPaintPreflight { internal bool accepted,wouldChange; }
        readonly Dictionary<Material,GraphSavedPaintPreflight> _graphSavedPaintPreflights=new Dictionary<Material,GraphSavedPaintPreflight>();
        int _graphSavedReadPass, _graphSavedShaderRevision;
        bool CanApplyGraphSavedProjectionForPaint(Material material,out bool wouldChange)
        {
            int pass=_graphFreshPreflightDepth==0?_rootItem.EnsureGraphGUIPureReadPass():0;
            if(pass==0)return NBShaderFeatureLevelMaterialApplier.CanApplyGraphSavedSupportedGateTier(material,out wouldChange);
            if(_graphSavedReadPass!=pass||_graphSavedShaderRevision!=NBShaderRootItem.ShaderPropertyTypeCacheRevision)
            {
                _graphSavedPaintPreflights.Clear();_graphSavedReadPass=pass;
                _graphSavedShaderRevision=NBShaderRootItem.ShaderPropertyTypeCacheRevision;
            }
            GraphSavedPaintPreflight value;
            if(!_graphSavedPaintPreflights.TryGetValue(material,out value))
            {
                value.accepted=NBShaderFeatureLevelMaterialApplier.CanApplyGraphSavedSupportedGateTier(material,out value.wouldChange);
                _graphSavedPaintPreflights.Add(material,value);
            }
            wouldChange=value.wouldChange;return value.accepted;
        }

        private StencilValuesConfig _stencilValuesConfig;

        public int KeywordVersion { get; private set; }

        private static readonly FlagToggleBinding[] ToggleFlagBindings =
        {
            new FlagToggleBinding("_ColorAdjustmentOnlyAffectMainTex", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX, 0),
            new FlagToggleBinding("_HueShift_Toggle", NBShaderFlags.FLAG_BIT_HUESHIFT_ON, 0),
            new FlagToggleBinding("_ChangeSaturability_Toggle", NBShaderFlags.FLAG_BIT_SATURABILITY_ON, 0),
            new FlagToggleBinding("_Contrast_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST, 1),
            new FlagToggleBinding("_BaseMapColorRefine_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE, 1),
            new FlagToggleBinding("_ColorMultiAlpha", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA, 0),
            new FlagToggleBinding("_BaseBackColor_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_BACKCOLOR, 0),
            new FlagToggleBinding("_IgnoreVetexColor_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR, 1),
            new FlagToggleBinding("_BumpMapMaskMode", NBShaderFlags.FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE, 0),
            new FlagToggleBinding("_DistortionBothDirection_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON, 0),
            new FlagToggleBinding("_Distortion_Choraticaberrat_WithNoise_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE, 0),
            new FlagToggleBinding("_DissolveLineMaskToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK, 1),
            new FlagToggleBinding("_MaskRefineToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE, 1),
            new FlagToggleBinding("_MaskMapGradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT, 1),
            new FlagToggleBinding("_MaskMap2GradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT, 1),
            new FlagToggleBinding("_MaskMap3GradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT, 1),
            new FlagToggleBinding("_ScreenDistortAlphaRefineToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE, 1),
            new FlagToggleBinding("_InvertFresnel_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON, 0),
            new FlagToggleBinding("_FresnelColorAffectByAlpha", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA, 0),
            new FlagToggleBinding("_VertexOffset_StartFromZero", NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO, 1),
            new FlagToggleBinding("_UTwirlEnabled", NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON, 0),
            new FlagToggleBinding("_PolarCoordinatesEnabled", NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON, 0)
        };

        private static readonly FlagModeBinding[] ModeFlagBindings =
        {
            new FlagModeBinding("_EmissionBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_OVERLAY_1_MULTIPLY, 0, 1),
            new FlagModeBinding("_ColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_2_ADD, 1, 0),
            new FlagModeBinding("_EmissionAlphaMultiplyMode", NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY, 1, 1),
            new FlagModeBinding("_ColorBlendAlphaMultiplyMode", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE, 0, 1),
            new FlagModeBinding("_RampColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD, 0, 1),
            new FlagModeBinding("_DissolveRampColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY, 1, 1),
            new FlagModeBinding("_FresnelMode", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_FADE_ON, 0, 1)
        };

        private static readonly string[] HoudiniVatKeywords =
        {
            "_HOUDINI_VAT_SOFTBODY",
            "_HOUDINI_VAT_RIGIDBODY",
            "_HOUDINI_VAT_DYNAMIC_REMESH",
            "_HOUDINI_VAT_PARTICLE_SPRITE"
        };

        private static readonly string[] TyflowVatKeywords =
        {
            "_TYFLOW_VAT_ABSOLUTE",
            "_TYFLOW_VAT_RELATIVE",
            "_TYFLOW_VAT_SKIN_R",
            "_TYFLOW_VAT_SKIN_PR",
            "_TYFLOW_VAT_SKIN_PRSAVE",
            "_TYFLOW_VAT_SKIN_PRSXYZ"
        };

        public NBShaderSyncService(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
        }

        public static void SyncMaterialState(Material material)
        {
            if (material == null)
            {
                return;
            }

            SyncMaterialState(new List<Material> { material });
        }

        public static void SyncMaterialState(IList<Material> materials)
        {
            if (materials == null || materials.Count == 0)
            {
                return;
            }

            var validMaterials = new List<Material>();
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i] != null)
                {
                    validMaterials.Add(materials[i]);
                }
            }

            if (validMaterials.Count == 0)
            {
                return;
            }

            var rootItem = new NBShaderRootItem
            {
                Mats = validMaterials,
                Shader = validMaterials[0].shader
            };
            rootItem.InitFlags(validMaterials);
            new NBShaderSyncService(rootItem).SyncMaterialState();
        }

        bool HasGraphTargets()
        {
            if (_rootItem.Mats == null) return false;
            foreach (Material material in _rootItem.Mats)
                if (NBShaderGUIContext.IsGraphMaterial(material)) return true;
            return false;
        }


        // Original Tier transaction; registered gates and declared OVZ keyword.

        // Explicit first-schema initialization only. Ordinary Prepare/Sync
        // remains seed-only; a ready marker2 inspector never projects on paint.
        internal bool TryInitializeGraphSupportedGateTierState()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead(_rootItem.GraphGUIStateMayInitialize());
            if (_rootItem.Mats == null || _rootItem.Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            List<Material> initialize = null;
            List<Material> initializeVATOnly = null;
            foreach (Material material in _rootItem.Mats)
            {
                if (!NBShaderGUIContext.IsGraphMaterial(material) ||
                    !NBShaderRootItem.HasFloatProperty(material, GraphGUIStateVersionProperty) ||
                    !NBShaderRootItem.HasFloatProperty(material, FeatureTierPropertyName) ||
                    !NBShaderRootItem.HasFloatProperty(material, "_MainTexBigBlockItemFoldOut") ||
                    !NBShaderRootItem.HasFloatProperty(material, "_BaseMapFoldOut")) return false;
                float marker = material.GetFloat(GraphGUIStateVersionProperty);
                if (marker != 0f && marker != 1f && marker != 2f) return false;
                bool wouldChange;
                if (marker == 2f)
                {
                    if (!NBShaderFeatureLevelMaterialApplier.CanApplyGraphSavedSupportedGateTier(material, out wouldChange)) return false;
                    bool oldVATProjection;if(!NBShaderFeatureLevelMaterialApplier.HasGraphVATProjectionState(material,out oldVATProjection))return false;
                    if(oldVATProjection){if(initialize==null)initialize=new List<Material>();initialize.Add(material);if(initializeVATOnly==null)initializeVATOnly=new List<Material>();initializeVATOnly.Add(material);}
                    continue;
                }
                if (!GraphFlagIntentSchemaAvailable(material)) return false;
                // Probe the actual current material and existing seed routine,
                // never guess missing properties or temporarily mutate a target.
                var probe = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    SeedGraphFlagIntents(probe);
                    probe.SetFloat(GraphGUIStateVersionProperty, 2f);
                    if (!NBShaderFeatureLevelMaterialApplier.CanApplyGraphSavedSupportedGateTier(probe, out wouldChange)) return false;
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
                if (initialize == null) initialize = new List<Material>();
                initialize.Add(material);
            }
            if (initialize == null) return true;

            // Exact owned float before-images: original29 mirrors + marker
            // and registered gates plus OVZ keyword. Raw words/other keywords/passes are never written.
            var ownedNames = new List<string> { GraphGUIStateVersionProperty };
            foreach (var binding in ToggleFlagBindings) ownedNames.Add(binding.propertyName);
            foreach (var binding in ModeFlagBindings) ownedNames.Add(binding.propertyName);
            ownedNames.AddRange(NBShaderFeatureLevelMaterialApplier.GraphSupportedProjectionProperties);
            var originalOverrideDepth = new bool[initialize.Count];
            var originalDeclaredKeywords = new bool[initialize.Count][];
            var originals = new float[initialize.Count][];
            var perTargetOwnedNames = new string[initialize.Count][];
            var targets = new UnityEngine.Object[initialize.Count];
            for (int i = 0; i < initialize.Count; ++i)
            {
                originalOverrideDepth[i] = initialize[i].IsKeywordEnabled("_OVERRIDE_Z");
                originalDeclaredKeywords[i]=NBShaderFeatureLevelMaterialApplier.CaptureGraphDeclaredKeywordState(initialize[i]);
                bool vatOnly=initializeVATOnly!=null&&initializeVATOnly.Contains(initialize[i]);
                perTargetOwnedNames[i]=vatOnly?new[]{"_NB_TierAllowVAT","_NB_TierAllowFlipbook","_NB_TierVATFamily","_NB_TierVATSubMode"}:ownedNames.ToArray();
                targets[i] = initialize[i]; originals[i] = new float[perTargetOwnedNames[i].Length];
                for (int field = 0; field < perTargetOwnedNames[i].Length; ++field) originals[i][field] = initialize[i].GetFloat(perTargetOwnedNames[i][field]);
            }
            Undo.RecordObjects(targets, "Initialize NB Graph GUI state and Tier");
            PrepareGraphGUIState(); // Original seed-only implementation, same protocol.
            foreach (Material material in initialize)
            {
                bool changed;
                bool vatOnly=initializeVATOnly!=null&&initializeVATOnly.Contains(material);
                bool applied=vatOnly?NBShaderFeatureLevelMaterialApplier.ApplyGraphVATProjectionGroup(material,(NBShaderFeatureTier)(int)material.GetFloat(FeatureTierPropertyName),null,out changed):NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed);
                if(applied)continue;
                // Unexpected failure after a pure probe: restore only the
                // owned initialization fields for every new target.
                for (int i = 0; i < initialize.Count; ++i)
                {
                    for (int field = 0; field < perTargetOwnedNames[i].Length; ++field) initialize[i].SetFloat(perTargetOwnedNames[i][field], originals[i][field]);
                    if(initializeVATOnly==null||!initializeVATOnly.Contains(initialize[i]))RestoreGraphOverrideDepthKeyword(initialize[i], originalOverrideDepth[i]);
                    if(initializeVATOnly==null||!initializeVATOnly.Contains(initialize[i]))NBShaderFeatureLevelMaterialApplier.RestoreGraphDeclaredKeywordState(initialize[i],originalDeclaredKeywords[i]);
                }
                return false;
            }
            foreach (Material material in initialize) EditorUtility.SetDirty(material);
            RefreshGraphMainTexPropertyReferences();
            _rootItem.Context?.Refresh();
            return true;
        }

        internal static bool TryInitializeGraphSupportedGateTierOnAssign(Material material)
        {
            if (material == null) return false;
            var root = new NBShaderRootItem { Mats = new List<Material> { material }, Shader = material.shader };
            if (!new NBShaderSyncService(root).TryInitializeGraphSupportedGateTierState()) return false;
            // Assign owns a real shader change even when the old material was
            // already marker2; unlike a ready inspector paint, re-derive here.
            bool changed;
            return NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material, out changed);
        }

        internal bool HasGraphSupportedGateTierEditSchema()
        {
            if (_rootItem.MatEditor == null || _rootItem.Mats == null || _rootItem.Mats.Count == 0 ||
                NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            foreach (Material material in _rootItem.Mats)
            {
                bool wouldChange;
                if (!NBShaderGUIContext.IsGraphMaterial(material) || !_rootItem.PropertyInfoDic.ContainsKey(FeatureTierPropertyName) ||
                    !NBShaderFeatureLevelMaterialApplier.CanApplyGraphSupportedGateTier(material, NBShaderFeatureTier.Ultra,
                        NBShaderFeatureCatalog.RawKeywords, out wouldChange)) return false;
                foreach (string property in NBShaderFeatureLevelMaterialApplier.GraphSupportedProjectionProperties)
                    if (!_rootItem.PropertyInfoDic.ContainsKey(property)) return false;
            }
            return true;
        }

        internal bool TryApplyGraphSupportedGateTier(NBShaderFeatureTier tier, IEnumerable<string> allowedManagedKeywords = null)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if ((int)tier < 0 || (int)tier > 3 || !HasGraphSupportedGateTierEditSchema()) return false;
            var allowed = new HashSet<string>(allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier));
            bool anyChange = false;
            var allowedPasses=NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);
            foreach (Material material in _rootItem.Mats)
            {
                bool wouldChange;
                if (!NBShaderFeatureLevelMaterialApplier.CanApplyGraphSupportedGateTier(material, tier, allowed, out wouldChange)) return false;
                anyChange |= wouldChange || material.GetFloat(FeatureTierPropertyName) != (float)tier;
                NBShaderPassIntent[] screenIntent;
                if(!CanApplyGraphOwnedScreenPassState(material,tier,allowed,allowedPasses,out screenIntent))return false;
                if(screenIntent!=null)foreach(var pass in screenIntent)anyChange|=material.GetShaderPassEnabled(pass.passName)!=pass.included;
                NBShaderPassIntent shadowIntent;
                if(!NBShaderFeatureRuntime.CanApplyGraphOwnedShadowPassState(material,tier,allowed,allowedPasses,out shadowIntent))return false;
                if(shadowIntent!=null)anyChange|=material.GetFloat("_CastShadows")!=(shadowIntent.included?1f:0f)||material.GetShaderPassEnabled(shadowIntent.passName)!=shadowIntent.included;
                NBShaderPassIntent backIntent;
                if (!CanApplyGraphOwnedBackFirstPassState(material, tier, allowed, allowedPasses, out backIntent)) return false;
                if (backIntent != null) anyChange |= material.GetShaderPassEnabled(backIntent.passName) != backIntent.included ||
                    material.GetFloat("_NB_BackFirstEffective") != (backIntent.included ? 1f : 0f);
            }
            if (!anyChange) return true;
            var objects = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats) objects.Add(material);
            var originalShadowPasses=new GraphShadowCasterSnapshot[_rootItem.Mats.Count];
            var originalScreenPasses=new GraphScreenPassSnapshot[_rootItem.Mats.Count];
            var originalBackPasses = new GraphBackFirstPassSnapshot[_rootItem.Mats.Count];
            var originalOverrideDepth = new bool[_rootItem.Mats.Count];
            var originalDeclaredKeywords = new bool[_rootItem.Mats.Count][];
            var originalGates = new float[_rootItem.Mats.Count][];
            for (int i = 0; i < _rootItem.Mats.Count; ++i)
            {
                originalShadowPasses[i]=new GraphShadowCasterSnapshot(_rootItem.Mats[i]);
                originalScreenPasses[i]=new GraphScreenPassSnapshot(_rootItem.Mats[i]);
                originalBackPasses[i] = new GraphBackFirstPassSnapshot(_rootItem.Mats[i]);
                originalOverrideDepth[i] = _rootItem.Mats[i].IsKeywordEnabled("_OVERRIDE_Z");
                originalDeclaredKeywords[i]=NBShaderFeatureLevelMaterialApplier.CaptureGraphDeclaredKeywordState(_rootItem.Mats[i]);
                originalGates[i] = new float[NBShaderFeatureLevelMaterialApplier.GraphSupportedProjectionProperties.Length];
                for (int gate = 0; gate < originalGates[i].Length; ++gate)
                    originalGates[i][gate] = _rootItem.Mats[i].GetFloat(NBShaderFeatureLevelMaterialApplier.GraphSupportedProjectionProperties[gate]);
            }
            Undo.RecordObjects(objects.ToArray(), "Set NBShader Feature Tier");
            var changedMaterials = new bool[_rootItem.Mats.Count];
            for (int i = 0; i < _rootItem.Mats.Count; ++i)
            {
                bool screenChanged, backChanged,shadowChanged;
                if (NBShaderFeatureLevelMaterialApplier.ApplyGraphSupportedGateTier(_rootItem.Mats[i], tier, allowed, out changedMaterials[i]) &&
                    ApplyGraphOwnedScreenPassState(_rootItem.Mats[i],tier,allowed,allowedPasses,out screenChanged) &&
                    ApplyGraphOwnedBackFirstPassState(_rootItem.Mats[i], tier, allowed, allowedPasses, out backChanged) &&
                    NBShaderFeatureRuntime.ApplyGraphOwnedShadowPassState(_rootItem.Mats[i],tier,allowed,allowedPasses,out shadowChanged))
                {changedMaterials[i]|=screenChanged || backChanged || shadowChanged;continue;}
                // Reader is pure and gates are not its inputs, so this is an
                // unexpected post-preflight failure. Roll back registered gates and
                // the exact OVZ keyword before any Tier value is committed.
                for (int restore = 0; restore < _rootItem.Mats.Count; ++restore)
                {
                    for (int gate = 0; gate < originalGates[restore].Length; ++gate)
                        _rootItem.Mats[restore].SetFloat(NBShaderFeatureLevelMaterialApplier.GraphSupportedProjectionProperties[gate], originalGates[restore][gate]);
                    RestoreGraphOverrideDepthKeyword(_rootItem.Mats[restore], originalOverrideDepth[restore]);
                    NBShaderFeatureLevelMaterialApplier.RestoreGraphDeclaredKeywordState(_rootItem.Mats[restore],originalDeclaredKeywords[restore]);
                    originalShadowPasses[restore].Restore();
                    originalScreenPasses[restore].Restore();
                    originalBackPasses[restore].Restore();
                }
                return false;
            }
            for (int i = 0; i < _rootItem.Mats.Count; ++i)
            {
                Material material = _rootItem.Mats[i];
                if (material.GetFloat(FeatureTierPropertyName) != (float)tier)
                { material.SetFloat(FeatureTierPropertyName, (float)tier); changedMaterials[i] = true; }
                if (changedMaterials[i]) EditorUtility.SetDirty(material);
            }
            RefreshGraphMainTexPropertyReferences();
            _rootItem.Context?.Refresh();
            return true;
        }

        private static void RestoreGraphOverrideDepthKeyword(Material material, bool enabled)
        {
            // Exact transaction rollback, deliberately not a policy projection.
            if (material.IsKeywordEnabled("_OVERRIDE_Z") == enabled) return;
            if (enabled) material.EnableKeyword("_OVERRIDE_Z"); else material.DisableKeyword("_OVERRIDE_Z");
        }

        internal static readonly string[] GraphTADepthProperties = {
            "_TABigBlockItemFoldOut", "_ZOffsetBlockFoldOut", "_OverrideZBlockFoldOut",
            "_ZOffset_Toggle", "_offsetFactor", "_offsetUnits", "_OverrideZ_Toggle", "_OverrideZValue"
        };

        internal bool HasGraphTADepthEditSchema()
        {
            if (_rootItem.MatEditor == null || !HasGraphMainTexFields(GraphTADepthProperties, Array.Empty<string>())) return false;
            foreach (Material material in _rootItem.Mats)
            {
                bool effective, allowed;
                if (!NBShaderFeatureLevelMaterialApplier.TryReadGraphOverrideDepthState(material, out effective, out allowed)) return false;
            }
            return true;
        }

        private bool RunGraphTADepthEdit(string property, bool? enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if ((property != "_ZOffset_Toggle" && property != "_OverrideZ_Toggle") || !HasGraphTADepthEditSchema()) return false;
            string[] names = property == "_ZOffset_Toggle"
                ? new[] { property, "_offsetFactor", "_offsetUnits" } : new[] { property, "_OverrideZValue" };
            // Preflight actual defaults for all targets before recording/writing anything.
            if (!enabled.HasValue)
                foreach (Material material in _rootItem.Mats)
                    foreach (string name in names)
                    {
                        float value = material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex(name));
                        if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                    }
            var objects = new UnityEngine.Object[_rootItem.Mats.Count];
            for (int i = 0; i < objects.Length; ++i) objects[i] = _rootItem.Mats[i];
            Undo.RecordObjects(objects, enabled.HasValue ? "Set TA Depth Control" : "Reset TA Depth Control");
            foreach (Material material in _rootItem.Mats)
            {
                bool changed = false;
                if (enabled.HasValue)
                {
                    float value = enabled.Value ? 1f : 0f;
                    if (material.GetFloat(property) != value) { material.SetFloat(property, value); changed = true; }
                }
                else foreach (string name in names)
                {
                    float value = material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex(name));
                    if (material.GetFloat(name) != value) { material.SetFloat(name, value); changed = true; }
                }
                if (property == "_ZOffset_Toggle") changed |= TABigBlockItem.ApplyZOffsetState(material, material.GetFloat(property) > 0.5f);
                else
                {
                    bool keywordChanged;
                    NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedOverrideDepth(material, out keywordChanged);
                    changed |= keywordChanged;
                    if (keywordChanged) NotifyKeywordsMayHaveChanged();
                }
                if (changed) EditorUtility.SetDirty(material);
            }
            RefreshGraphMainTexPropertyReferences();
            _rootItem.Context?.Refresh();
            return true;
        }

        internal bool TryApplyGraphTADepthToggle(string property, bool enabled) => RunGraphTADepthEdit(property, enabled);
        internal bool TryResetGraphTADepth(string property) => RunGraphTADepthEdit(property, null);


        internal bool HasGraphOverlayEditSchema()
        {
            if (_rootItem.MatEditor == null || _rootItem.Mats == null || _rootItem.Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            if (!HasGraphMainTexUVEditSchema()) return false; // Same global UV controls/schema.
            foreach (Material material in _rootItem.Mats)
            {
                bool ignored;
                if (!NBShaderGUIContext.IsGraphMaterial(material) || !CanApplyGraphSavedProjectionForPaint(material, out ignored)) return false;
                foreach (string field in NBShaderRootItem.SharedGraphOverlayFloatProperties)
                {
                    // These two original Graph numeric inputs are serialized Range properties.
                    bool numericType = NBShaderRootItem.HasFloatProperty(material, field);
                    if (!numericType && (field == "_EmissionMapUVRotation" || field == "_EmissionAlphaIntensity"))
                    {
                        int index = material.shader.FindPropertyIndex(field);
                        numericType = index >= 0 && material.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Range;
                    }
                    if (!numericType || !_rootItem.PropertyInfoDic.ContainsKey(field)) return false;
                    float value = material.GetFloat(field); if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
                foreach (string field in NBShaderRootItem.SharedGraphOverlayVectorProperties)
                {
                    int index = material.shader.FindPropertyIndex(field);
                    if (index < 0 || material.shader.GetPropertyType(index) != UnityEngine.Rendering.ShaderPropertyType.Vector || !_rootItem.PropertyInfoDic.ContainsKey(field)) return false;
                    Vector4 value = material.GetVector(field); for (int i=0;i<4;++i) if (float.IsNaN(value[i]) || float.IsInfinity(value[i])) return false;
                }
                foreach (string field in new[] { "_EmissionMapColor", "_ColorBlendColor" })
                    if (!material.HasProperty(field) || !_rootItem.PropertyInfoDic.ContainsKey(field)) return false;
                foreach (string field in new[] { "_EmissionMap", "_ColorBlendMap" })
                    if (!material.HasProperty(field) || !_rootItem.PropertyInfoDic.ContainsKey(field)) return false;
            }
            return true;
        }
        internal bool TryApplyGraphOverlayEdit(string field, bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if ((field != "_EmissionEnabled" && field != "_ColorBlendMap_Toggle") || !HasGraphOverlayEditSchema()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                SetFloatIfExists(material, field, enabled ? 1f : 0f);
                bool changed; if (!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material, out changed)) return false;
            }
            RefreshGraphMainTexPropertyReferences(); _rootItem.Context?.Refresh(); return true;
        }
        internal bool TryApplyGraphOverlayFlagEdit(int bit, int word, bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            bool owned = word==0 && (bit==NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_OVERLAY_1_MULTIPLY || bit==NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE) ||
                word==1 && (bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_2_ADD || bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY);
            if (!owned || !HasGraphOverlayEditSchema()) return false;
            string name="_NB_Flags"+word+((bit&65535)!=0?"Lo16":"Hi16");int mask=(bit&65535)!=0?bit&65535:(int)((uint)bit>>16);
            foreach(Material material in _rootItem.Mats)
                if(!WriteGraphHalfSlice(material,name,mask,enabled?mask:0))NotifyGraphPackedFlagsEdited(material,name,mask);
            RefreshGraphMainTexPropertyReferences();return true;
        }
        internal bool TryApplyGraphOverlayUVMode(int position, NBShaderFlags.UVMode mode, string fold, bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphOverlayEditSchema() || (int)mode<0 || (int)mode>8 ||
                !(position==NBShaderFlags.FLAG_BIT_UVMODE_POS_0_EMISSION_MAP && fold=="_EmissionUVModeFoldOut" || position==NBShaderFlags.FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP && fold=="_ColorBlendUVModeFoldOut")) return false;
            int shift=position&15;int mask=3<<shift;string suffix=position<16?"Lo16":"Hi16";
            return RunGraphMainTexEdit("Overlay UV Source",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0"+suffix,mask,((int)mode&3)<<shift) |
                    WriteGraphHalfSlice(material,"_NB_UVModeFlagType0"+suffix,mask,((int)mode/4)<<shift);
                if(setFold){float value=mode==NBShaderFlags.UVMode.DefaultUVChannel || mode==NBShaderFlags.UVMode.CommonUV || mode==NBShaderFlags.UVMode.ScreenUV || mode==NBShaderFlags.UVMode.MainTex?0f:1f;
                    if(material.GetFloat(fold)!=value){material.SetFloat(fold,value);changed=true;}}
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }
        internal bool TryApplyGraphOverlayCustomData(int position, int word, NBShaderFlags.CutomDataComponent component)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphOverlayEditSchema() || word!=3 || (position!=16 && position!=20 && position!=24 && position!=28) || (int)component<0 || (int)component>8)return false;
            int[] values={0,NBShaderFlags.CustomData1XBit,NBShaderFlags.CustomData1YBit,NBShaderFlags.CustomData1ZBit,NBShaderFlags.CustomData1WBit,NBShaderFlags.CustomData2XBit,NBShaderFlags.CustomData2YBit,NBShaderFlags.CustomData2ZBit,NBShaderFlags.CustomData2WBit};
            int shift=position-16;return RunGraphMainTexEdit("Overlay Offset Custom Data",material=>WriteGraphHalfSlice(material,"_NB_CustomDataFlag3Hi16",15<<shift,values[(int)component]<<shift));
        }


        internal bool HasGraphParallaxEditSchema()
        {
            if (_rootItem.MatEditor == null || _rootItem.Mats == null || _rootItem.Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            foreach (Material material in _rootItem.Mats)
            {
                bool ignored;
                if (!NBShaderGUIContext.IsGraphMaterial(material) || !CanApplyGraphSavedProjectionForPaint(material, out ignored)) return false;
                foreach (string field in new[] { "_ParallaxBlockFoldOut", "_ParallaxMapping_Toggle", "_ParallaxMapping_Intensity", "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16" })
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, field) || !_rootItem.PropertyInfoDic.ContainsKey(field)) return false;
                    float value=material.GetFloat(field); if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                foreach (string field in new[] { "_ParallaxMapping_Vec", "_ParallaxMapping_IntensityRangeVec" })
                {
                    int index=material.shader.FindPropertyIndex(field);
                    if(index<0 || material.shader.GetPropertyType(index)!=UnityEngine.Rendering.ShaderPropertyType.Vector || !_rootItem.PropertyInfoDic.ContainsKey(field))return false;
                    Vector4 value=material.GetVector(field);for(int i=0;i<4;++i)if(float.IsNaN(value[i])||float.IsInfinity(value[i]))return false;
                }
                int map=material.shader.FindPropertyIndex("_ParallaxMapping_Map");
                if(map<0 || material.shader.GetPropertyType(map)!=UnityEngine.Rendering.ShaderPropertyType.Texture || !_rootItem.PropertyInfoDic.ContainsKey("_ParallaxMapping_Map"))return false;
            }
            return true;
        }
        internal bool TryApplyGraphParallaxEdit(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphParallaxEditSchema()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                SetFloatIfExists(material,"_ParallaxMapping_Toggle",enabled?1f:0f);
                bool changed; if (!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed)) return false;
            }
            RefreshGraphMainTexPropertyReferences(); _rootItem.Context?.Refresh(); return true;
        }
        internal bool TryFinalizeGraphParallaxLayerEdit(IList<Vector4> previous)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (previous==null || !HasGraphParallaxEditSchema() || previous.Count!=_rootItem.Mats.Count) return false;
            bool changed=false;
            for(int i=0;i<previous.Count;++i)
            {
                Material material=_rootItem.Mats[i];Vector4 current=material.GetVector("_ParallaxMapping_Vec");
                if(current.Equals(previous[i]))continue; // Foldout, passive paint and unrelated edits never normalize layers.
                SyncParallaxLayerCount(material); changed=true;
            }
            if(changed)RefreshGraphMainTexPropertyReferences();
            return true;
        }


        internal bool HasGraphNormalMapEditSchema()
        {
            if(_rootItem.MatEditor==null || !HasGraphMainTexUVEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                bool ignored;if(!CanApplyGraphSavedProjectionForPaint(material,out ignored))return false;
                foreach(string name in new[]{"_BumpMapToggle","_BumpMapMaskMode","_BumpScale","_BumpToggleFoldOut","_BumpTexFoldOut","_BumpUVModeFoldOut"})
                {
                    if(!NBShaderRootItem.HasFloatProperty(material,name) || !_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                int index=material.shader.FindPropertyIndex("BumpScaleRangeVec");
                if(index<0 || material.shader.GetPropertyType(index)!=UnityEngine.Rendering.ShaderPropertyType.Vector || !_rootItem.PropertyInfoDic.ContainsKey("BumpScaleRangeVec"))return false;
                Vector4 range=material.GetVector("BumpScaleRangeVec");for(int i=0;i<4;++i)if(float.IsNaN(range[i])||float.IsInfinity(range[i]))return false;
                index=material.shader.FindPropertyIndex("_BumpTex");
                if(index<0 || material.shader.GetPropertyType(index)!=UnityEngine.Rendering.ShaderPropertyType.Texture || !_rootItem.PropertyInfoDic.ContainsKey("_BumpTex"))return false;
            }
            return true;
        }
        internal bool TryApplyGraphNormalMapEdit(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphNormalMapEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                SetFloatIfExists(material,"_BumpMapToggle",enabled?1f:0f);
                bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed))return false;
            }
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
        }
        internal bool TryApplyGraphNormalMapMaskEdit(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphNormalMapEditSchema())return false;
            const string name="_NB_Flags0Hi16";const int mask=1<<(21-16);
            return RunGraphMainTexEdit("Normal Map Multi Channel",material=>{
                bool changed=WriteGraphHalfSlice(material,name,mask,enabled?mask:0);
                if(!changed)NotifyGraphPackedFlagsEdited(material,name,mask);
                return changed;
            });
        }
        internal bool TryApplyGraphNormalMapUVMode(NBShaderFlags.UVMode mode,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphNormalMapEditSchema() || (int)mode<0 || (int)mode>8)return false;
            const int shift=NBShaderFlags.FLAG_BIT_UVMODE_POS_0_BUMPMAP-16;const int mask=3<<shift;
            return RunGraphMainTexEdit("Normal Map UV Source",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0Hi16",mask,((int)mode&3)<<shift);
                changed|=WriteGraphHalfSlice(material,"_NB_UVModeFlagType0Hi16",mask,((int)mode/4)<<shift);
                if(setFold)
                {
                    float value=mode==NBShaderFlags.UVMode.DefaultUVChannel||mode==NBShaderFlags.UVMode.CommonUV||mode==NBShaderFlags.UVMode.ScreenUV||mode==NBShaderFlags.UVMode.MainTex?0:1;
                    if(material.GetFloat("_BumpUVModeFoldOut")!=value){material.SetFloat("_BumpUVModeFoldOut",value);changed=true;}
                }
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }

        private sealed class GraphScreenPassSnapshot
        {
            readonly Material material;readonly Dictionary<string,bool> states=new Dictionary<string,bool>();
            internal GraphScreenPassSnapshot(Material value)
            {
                material=value;
                if(!value.HasProperty(GraphScreenMigration)||value.GetFloat(GraphScreenMigration)!=1)return;
                int route;if(!NBShaderPassFeatureCatalog.TryGetGraphColorRouting(value,out route))return;
                foreach(string name in new[]{NBShaderPassFeatureCatalog.GraphMainColorPassName(route),"NBDeferredDistortPass","NBCameraOpaqueDistortPass"})states[name]=value.GetShaderPassEnabled(name);
            }
            internal void Restore(){foreach(var state in states)material.SetShaderPassEnabled(state.Key,state.Value);}
        }

        internal const string GraphScreenMigration=NBShaderFeatureRuntime.GraphScreenMigration;
        internal static readonly string[] GraphNoiseSharedPropertyNames={"_NoiseBlockFoldOut","_NoiseMapFoldOut","_NoiseUVModeFoldOut","_NoiseMaskBlockFoldOut","_NoiseMaskUVModeFoldOut","_ScreenDistortAlphaFoldOut","_NoiseIntensityRangeVec","_ScreenDistortIntensityRangeVec","_DisableMainPassToggle",GraphScreenMigration,"_noisemapEnabled","_NoiseIntensity","_NoiseMap","_NoiseMapUVRotation","_NoiseOffset","_DistortionDirection","_DistortionBothDirection_Toggle","_noiseMaskMap_Toggle","_NoiseMaskMap","_DistortMode","_RefractionIOR","_DistortPNoiseBlendOpacity","_NB_Debug_Distort","_ScreenDistortAlphaRefineToggle","_NB_DistortionMode","_NB_DistortionIntensity","_NB_DistortionAlphaPow","_NB_DistortionAlphaMultiplier","_NB_DistortionAlphaAdd"};
        internal bool HasGraphNoiseEditSchema()
        {
            if(_rootItem.MatEditor==null || !HasGraphMainTexUVEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                bool ignored;if(!CanApplyGraphSavedProjectionForPaint(material,out ignored))return false;
                foreach(string name in GraphNoiseSharedPropertyNames)if(!material.HasProperty(name)||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                foreach(string name in new[]{"_NoiseBlockFoldOut","_NoiseMapFoldOut","_NoiseUVModeFoldOut","_NoiseMaskBlockFoldOut","_NoiseMaskUVModeFoldOut","_ScreenDistortAlphaFoldOut","_DisableMainPassToggle",GraphScreenMigration,"_noisemapEnabled","_NoiseIntensity","_NoiseMapUVRotation","_DistortionBothDirection_Toggle","_noiseMaskMap_Toggle","_DistortMode","_RefractionIOR","_DistortPNoiseBlendOpacity","_NB_Debug_Distort","_ScreenDistortAlphaRefineToggle","_NB_DistortionMode","_NB_DistortionIntensity","_NB_DistortionAlphaPow","_NB_DistortionAlphaMultiplier","_NB_DistortionAlphaAdd"})
                {
                    if(!NBShaderRootItem.HasFloatProperty(material,name))return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                foreach(string name in new[]{"_NoiseIntensityRangeVec","_ScreenDistortIntensityRangeVec","_NoiseOffset","_DistortionDirection"})
                {
                    int index=material.shader.FindPropertyIndex(name);if(index<0||material.shader.GetPropertyType(index)!=UnityEngine.Rendering.ShaderPropertyType.Vector)return false;
                    Vector4 v=material.GetVector(name);for(int i=0;i<4;++i)if(float.IsNaN(v[i])||float.IsInfinity(v[i]))return false;
                }
                float migration=material.GetFloat(GraphScreenMigration);if(migration!=0&&migration!=1)return false;
                int route;if(!NBShaderPassFeatureCatalog.TryGetGraphColorRouting(material,out route))return false;
                float mode=material.GetFloat("_NB_DistortionMode");if(mode<0||mode>2||mode!=Mathf.Round(mode))return false;
                NBShaderPassIntent[] screenIntent;var tier=(NBShaderFeatureTier)(int)material.GetFloat(FeatureTierPropertyName);
                if(!CanApplyGraphOwnedScreenPassState(material,tier,NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out screenIntent))return false;
            }
            return true;
        }
        internal static bool CanApplyGraphOwnedScreenPassState(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedKeywords,IEnumerable<string> allowedPassFeatures,out NBShaderPassIntent[] intent)
        {
            return NBShaderFeatureRuntime.CanApplyGraphOwnedScreenPassState(material, tier, allowedKeywords, allowedPassFeatures, out intent);
        }
        internal static bool ApplyGraphOwnedScreenPassState(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedKeywords,IEnumerable<string> allowedPassFeatures,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphOwnedScreenPassState(material, tier, allowedKeywords, allowedPassFeatures, out changed);
        }
        internal bool TryReadGraphScreenDisableMainDisplay(out bool disabled,out bool mixed)
        {
            disabled=mixed=false;if(!HasGraphNoiseEditSchema())return false;bool first=true;
            foreach(Material material in _rootItem.Mats)
            {
                int route;NBShaderPassFeatureCatalog.TryGetGraphColorRouting(material,out route);
                bool value=material.GetFloat(GraphScreenMigration)==1?material.GetFloat("_DisableMainPassToggle")>.5f:!material.GetShaderPassEnabled(NBShaderPassFeatureCatalog.GraphMainColorPassName(route));
                if(first){disabled=value;first=false;}else mixed|=disabled!=value;
            }
            return true;
        }
        internal bool TryAdoptGraphScreenEdit(int? selectedMode,bool? selectedDisable)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphNoiseEditSchema()||(selectedMode.HasValue&&(selectedMode.Value<0||selectedMode.Value>2)))return false;
            var probes=new List<Material>();var intents=new List<NBShaderPassIntent[]>();var disabledValues=new List<float>();var chosenModes=new List<int>();
            try
            {
                // Probe every target without mutating aliases, raw passes or ownership.
                foreach(Material material in _rootItem.Mats)
                {
                    int route;NBShaderPassFeatureCatalog.TryGetGraphColorRouting(material,out route);
                    if(route==1 && (!NBShaderRootItem.HasFloatProperty(material,"_NB_GraphPassMigrationComplete")||material.GetFloat("_NB_GraphPassMigrationComplete")!=1))return false;
                    int mode=selectedMode??(int)material.GetFloat("_NB_DistortionMode");
                    bool disable=selectedDisable??(material.GetFloat(GraphScreenMigration)==1?material.GetFloat("_DisableMainPassToggle")>.5f:!material.GetShaderPassEnabled(NBShaderPassFeatureCatalog.GraphMainColorPassName(route)));
                    if(mode==0)disable=false;
                    var probe=new Material(material){hideFlags=HideFlags.HideAndDontSave};probes.Add(probe);
                    probe.SetFloat("_NB_DistortionMode",mode);probe.SetFloat("_DisableMainPassToggle",disable?1:0);probe.SetFloat(GraphScreenMigration,1);
                    var tier=(NBShaderFeatureTier)(int)material.GetFloat(FeatureTierPropertyName);NBShaderPassIntent[] intent;
                    if(!NBShaderMaterialIntentResolver.TryResolveGraphScreenPassIntent(probe,tier,NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out intent))return false;
                    intents.Add(intent);disabledValues.Add(disable?1:0);chosenModes.Add(mode);
                }
                Undo.RecordObjects(_rootItem.MatEditor.targets,"Edit NB Screen Distort State");
                for(int i=0;i<_rootItem.Mats.Count;++i)
                {
                    Material material=_rootItem.Mats[i];material.SetFloat("_NB_DistortionMode",chosenModes[i]);material.SetFloat("_DisableMainPassToggle",disabledValues[i]);
                    foreach(var pass in intents[i])material.SetShaderPassEnabled(pass.passName,pass.included);
                    material.SetFloat(GraphScreenMigration,1);EditorUtility.SetDirty(material); // Commit only after all pure probes succeeded.
                }
                RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
            }
            finally{foreach(Material probe in probes)UnityEngine.Object.DestroyImmediate(probe);}
        }
        internal bool TryApplyGraphNoiseIntentEdit()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphNoiseEditSchema())return false;
            foreach(Material material in _rootItem.Mats){bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed))return false;}
            return TryProjectGraphNoiseOwnedScreenState();
        }
        internal bool TryProjectGraphNoiseOwnedScreenState()
        {
            if(!HasGraphNoiseEditSchema())return false;var intents=new List<NBShaderPassIntent[]>();
            foreach(Material material in _rootItem.Mats)
            {
                var tier=(NBShaderFeatureTier)(int)material.GetFloat(FeatureTierPropertyName);NBShaderPassIntent[] intent;
                if(!CanApplyGraphOwnedScreenPassState(material,tier,NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out intent))return false;
                intents.Add(intent);
            }
            for(int i=0;i<intents.Count;++i)if(intents[i]!=null)foreach(var pass in intents[i])_rootItem.Mats[i].SetShaderPassEnabled(pass.passName,pass.included);
            RefreshGraphMainTexPropertyReferences();return true;
        }
        internal bool TryApplyGraphNoiseFlagEdit(int bit,bool enabled,int word)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            bool owned=word==0&&bit==NBShaderFlags.FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON||word==1&&bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE;
            if(!owned||!HasGraphNoiseEditSchema())return false;string name="_NB_Flags"+word+"Lo16";
            return RunGraphMainTexEdit("Noise Packed Toggle",material=>{bool changed=WriteGraphHalfSlice(material,name,bit,enabled?bit:0);if(!changed)NotifyGraphPackedFlagsEdited(material,name,bit);return changed;});
        }
        internal bool TryApplyGraphNoiseUVMode(int position,NBShaderFlags.UVMode mode,string fold,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphNoiseEditSchema()||(int)mode<0||(int)mode>8||!(position==8&&fold=="_NoiseUVModeFoldOut"||position==10&&fold=="_NoiseMaskUVModeFoldOut"))return false;
            int mask=3<<position;return RunGraphMainTexEdit("Noise UV Source",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0Lo16",mask,((int)mode&3)<<position)|WriteGraphHalfSlice(material,"_NB_UVModeFlagType0Lo16",mask,((int)mode/4)<<position);
                if(setFold){float value=mode==NBShaderFlags.UVMode.DefaultUVChannel||mode==NBShaderFlags.UVMode.CommonUV||mode==NBShaderFlags.UVMode.ScreenUV||mode==NBShaderFlags.UVMode.MainTex?0:1;if(material.GetFloat(fold)!=value){material.SetFloat(fold,value);changed=true;}}
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }

        public void NotifyKeywordsMayHaveChanged()
        {
            KeywordVersion++;
        }

        public void ApplyTransparentMode(TransparentMode mode)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            if (!_rootItem.PropertyInfoDic.ContainsKey("_ZWrite") || !_rootItem.PropertyInfoDic.ContainsKey("_QueueBias"))
            {
                return;
            }

            MaterialProperty zWriteProperty = _rootItem.PropertyInfoDic["_ZWrite"].Property;
            MaterialProperty queueBiasProperty = _rootItem.PropertyInfoDic["_QueueBias"].Property;
            int queueBias = Mathf.RoundToInt(queueBiasProperty.floatValue);

            switch (mode)
            {
                case TransparentMode.Opaque:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 2000 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;

                case TransparentMode.Transparent:
                    zWriteProperty.floatValue = 0;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 3000 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;

                case TransparentMode.CutOff:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 2450 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", true);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;
            }
        }

        // Explicit ordinary-Graph MainTex protocol edits; general projection guards stay intact.
        private bool HasGraphMainTexTargets()
        {
            if (_rootItem.Mats == null || _rootItem.Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            foreach (Material material in _rootItem.Mats)
            {
                if (!NBShaderGUIContext.IsGraphMaterial(material)) return false;
                foreach (string name in new[] { "_NB_DistortionMode", "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16" })
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, name)) return false;
                    float value = material.GetFloat(name);
                    if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
            }
            return true;
        }

        private bool HasGraphMainTexFields(string[] floats, string[] vectors)
        {
            if (!HasGraphMainTexTargets()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                foreach (string name in floats)
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, name) || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    float value = material.GetFloat(name);
                    if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
                foreach (string name in vectors)
                {
                    if (!_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    int index = material.shader.FindPropertyIndex(name);
                    if (index < 0 || material.shader.GetPropertyType(index) != UnityEngine.Rendering.ShaderPropertyType.Vector) return false;
                    Vector4 value = material.GetVector(name);
                    for (int channel = 0; channel < 4; ++channel)
                        if (float.IsNaN(value[channel]) || float.IsInfinity(value[channel])) return false;
                }
            }
            return true;
        }

        internal bool HasGraphMainTexCustomDataEditSchema()
            => HasGraphMainTexFields(new[] { "_NB_CustomDataFlag0Lo16", "_NB_CustomDataFlag0Hi16" }, Array.Empty<string>());

        internal bool HasGraphMainTexUVEditSchema()
            => HasGraphMainTexFields(new[] {
                "_MainTexUVModeFoldOut", "_GlobalTwirlFoldOut", "_GlobalPolarFoldOut",
                "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16",
                "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16",
                "_UTwirlEnabled", "_PolarCoordinatesEnabled", "_TWStrength", "_WorldSpaceUVModeSelector", "_ObjectSpaceUVModeSelector"
            }, new[] { "_TWParameter", "_PCCenter", "_CylinderUVRotate", "_CylinderUVPosOffset",
                "_CylinderMatrix0", "_CylinderMatrix1", "_CylinderMatrix2", "_CylinderMatrix3" });

        private static int ReadGraphHalf(Material material, string name)
            => Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(name), 0f, 65535f));

        private static bool WriteGraphHalfSlice(Material material, string name, int mask, int value)
        {
            int before = ReadGraphHalf(material, name);
            int after = (before & ~mask) | (value & mask);
            if (before == after) return false; // Preserve a finite noncanonical raw half on a decoded no-op.
            material.SetFloat(name, after);
            NotifyGraphPackedFlagsEdited(material, name, mask);
            return true;
        }

        private void RefreshGraphMainTexPropertyReferences()
        {
            if (_rootItem.MatEditor == null) return;
            foreach (MaterialProperty property in MaterialEditor.GetMaterialProperties(_rootItem.MatEditor.targets))
                if (_rootItem.PropertyInfoDic.TryGetValue(property.name, out ShaderPropertyInfo info)) info.Property = property;
        }

        private bool RunGraphMainTexEdit(string label, Func<Material, bool> edit)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexTargets()) return false;
            var objects = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats) objects.Add(material);
            Undo.RecordObjects(objects.ToArray(), label);
            foreach (Material material in _rootItem.Mats)
                if (edit(material)) EditorUtility.SetDirty(material);
            RefreshGraphMainTexPropertyReferences();
            return true;
        }

        private static bool UpdateGraphMainTexUVDerived(Material material)
        {
            var flags = new NBShaderFlags(material);
            bool changed = WriteGraphHalfSlice(material, "_NB_Flags1Hi16", 16,
                flags.CheckIsUVModeOn(NBShaderFlags.UVMode.Cylinder) ? 16 : 0);
            if (!flags.CheckIsUVModeOn(NBShaderFlags.UVMode.SpecialUVChannel))
                changed |= WriteGraphHalfSlice(material, "_NB_Flags1Hi16", 12, 0);
            return changed;
        }

        internal bool TryApplyGraphMainTexUVMode(NBShaderFlags.UVMode mode, bool setFoldFromPopup = false)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexUVEditSchema() || (int)mode < 0 || (int)mode > 8) return false;
            return RunGraphMainTexEdit("Main Texture UV Source", material => {
                bool changed = WriteGraphHalfSlice(material, "_NB_UVModeFlag0Lo16", 3, (int)mode & 3);
                changed |= WriteGraphHalfSlice(material, "_NB_UVModeFlagType0Lo16", 3, (int)mode / 4);
                if (setFoldFromPopup)
                {
                    float fold = mode == NBShaderFlags.UVMode.DefaultUVChannel || mode == NBShaderFlags.UVMode.CommonUV ||
                        mode == NBShaderFlags.UVMode.ScreenUV || mode == NBShaderFlags.UVMode.MainTex ? 0f : 1f;
                    if (material.GetFloat("_MainTexUVModeFoldOut") != fold) { material.SetFloat("_MainTexUVModeFoldOut", fold); changed = true; }
                }
                return UpdateGraphMainTexUVDerived(material) | changed;
            });
        }

        internal bool TryApplyGraphMainTexCustomData(int position, NBShaderFlags.CutomDataComponent component)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexCustomDataEditSchema() || (position != 0 && position != 4) || (int)component < 0 || (int)component > 8) return false;
            // Same public protocol constants/enum as NBShaderFlags.SetCustomDataFlag; no new meaning.
            int[] values = { 0, NBShaderFlags.CustomData1XBit, NBShaderFlags.CustomData1YBit,
                NBShaderFlags.CustomData1ZBit, NBShaderFlags.CustomData1WBit, NBShaderFlags.CustomData2XBit,
                NBShaderFlags.CustomData2YBit, NBShaderFlags.CustomData2ZBit, NBShaderFlags.CustomData2WBit };
            return RunGraphMainTexEdit("Main Texture Offset Custom Data", material =>
                WriteGraphHalfSlice(material, "_NB_CustomDataFlag0Lo16", 15 << position, values[(int)component] << position));
        }

        internal bool TryApplyGraphMainTexSpecialUV(int channel)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexUVEditSchema() || channel < 0 || channel > 1) return false;
            return RunGraphMainTexEdit("Special UV Channel", material =>
                WriteGraphHalfSlice(material, "_NB_Flags1Hi16", 12, channel == 0 ? 4 : 8));
        }

        internal bool TryApplyGraphMainTexUVToggle(int bit, bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexUVEditSchema() || (bit != NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON && bit != NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON)) return false;
            string property = bit == NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON ? "_UTwirlEnabled" : "_PolarCoordinatesEnabled";
            return RunGraphMainTexEdit("Main Texture UV Transform", material => {
                bool changed = material.GetFloat(property) != (enabled ? 1f : 0f);
                if (changed) material.SetFloat(property, enabled ? 1f : 0f);
                return WriteGraphHalfSlice(material, "_NB_Flags0Lo16", bit, enabled ? bit : 0) | changed;
            });
        }

        internal bool TryApplyGraphMainTexPlane(string property, int plane)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexUVEditSchema() || plane < 0 || plane > 2 ||
                (property != "_WorldSpaceUVModeSelector" && property != "_ObjectSpaceUVModeSelector")) return false;
            return RunGraphMainTexEdit("Coordinate Plane", material => {
                if (material.GetFloat(property) == plane) return false;
                material.SetFloat(property, plane); return true;
            });
        }

        internal bool TryApplyGraphMainTexCylinderComponent(string property, int component, float value)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexUVEditSchema() || component < 0 || component > 2 || float.IsNaN(value) || float.IsInfinity(value) ||
                (property != "_CylinderUVRotate" && property != "_CylinderUVPosOffset")) return false;
            return RunGraphMainTexEdit("Cylinder UV", material => {
                Vector4 vector = material.GetVector(property);
                if (vector[component] == value) return false;
                vector[component] = value; material.SetVector(property, vector);
                Vector4 rotation = material.GetVector("_CylinderUVRotate"), offset = material.GetVector("_CylinderUVPosOffset");
                Matrix4x4 matrix = Matrix4x4.Translate(new Vector3(offset.x, offset.y, offset.z)) *
                    Matrix4x4.Rotate(Quaternion.Euler(new Vector3(rotation.x, rotation.y, rotation.z)));
                for (int row = 0; row < 4; ++row) material.SetVector("_CylinderMatrix" + row, matrix.GetRow(row));
                return true;
            });
        }

        // Wrap original reset, including descendants, before any MaterialProperty setter runs.
        // Restore unowned raw halves and unowned decoded bits rather than normalize the whole word.
        internal bool TryRunGraphMainTexReset(Action reset, bool wholeBlock, int flags0Bits = 0)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphMainTexTargets() || (!wholeBlock && !HasGraphMainTexUVEditSchema())) return false;
            string[] names = { "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16",
                "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16",
                "_NB_CustomDataFlag0Lo16", "_NB_CustomDataFlag0Hi16", "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16",
                "_NB_ColorChannelLo16", "_NB_ColorChannelHi16", "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16" };
            var before = new List<Dictionary<string, float>>(); var snapshots = new List<string>();
            var objects = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats)
            {
                var values = new Dictionary<string, float>();
                foreach (string name in names) if (material.HasProperty(name)) values.Add(name, material.GetFloat(name));
                before.Add(values); snapshots.Add(EditorJsonUtility.ToJson(material)); objects.Add(material);
            }
            Undo.RecordObjects(objects.ToArray(), "Reset Main Texture");
            reset();
            for (int index = 0; index < _rootItem.Mats.Count; ++index)
            {
                Material material = _rootItem.Mats[index];
                if (wholeBlock && HasGraphMainTexUVEditSchema()) UpdateGraphMainTexUVDerived(material);
                if (!wholeBlock && flags0Bits != 0)
                {
                    string property = flags0Bits == NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON ? "_UTwirlEnabled" : "_PolarCoordinatesEnabled";
                    WriteGraphHalfSlice(material, "_NB_Flags0Lo16", flags0Bits, material.GetFloat(property) > .5f ? flags0Bits : 0);
                }
                foreach (var pair in before[index])
                {
                    int mask = 0;
                    if (pair.Key == "_NB_Flags0Lo16") mask = flags0Bits;
                    if (wholeBlock)
                    {
                        if (pair.Key == "_NB_Flags1Hi16") mask = 28;
                        else if (pair.Key == "_NB_UVModeFlag0Lo16" || pair.Key == "_NB_UVModeFlagType0Lo16" || pair.Key == "_NB_ColorChannelLo16") mask = 3;
                        else if (pair.Key == "_NB_CustomDataFlag0Lo16") mask = 255;
                        else if (pair.Key == "_NB_WrapFlagsLo16" || pair.Key == "_NB_WrapFlagsHi16" || pair.Key == "_NB_ForceNoMipFlagsLo16") mask = 1;
                    }
                    int previous = Mathf.RoundToInt(Mathf.Clamp(pair.Value, 0f, 65535f));
                    int merged = (ReadGraphHalf(material, pair.Key) & mask) | (previous & ~mask);
                    float value = merged == previous ? pair.Value : merged;
                    if (material.GetFloat(pair.Key) != value) material.SetFloat(pair.Key, value);
                }
                if (EditorJsonUtility.ToJson(material) != snapshots[index]) EditorUtility.SetDirty(material);
            }
            RefreshGraphMainTexPropertyReferences(); return true;
        }

        internal const string GraphGUIStateVersionProperty = "_NB_GraphGUIStateVersion";

        // GUI1B candidate: serialized UI mirrors of the EXISTING flag protocol.
        // No Graph Tier/effective projection, no periodic mirror->flags authority switch.
        internal static bool GraphFlagIntentSchemaAvailable(Material material)
        {
            if (!NBShaderGUIContext.IsGraphMaterial(material)) return false;
            for (int i = 0; i < ToggleFlagBindings.Length; ++i)
                if (!NBShaderRootItem.HasFloatProperty(material, ToggleFlagBindings[i].propertyName)) return false;
            for (int i = 0; i < ModeFlagBindings.Length; ++i)
                if (!NBShaderRootItem.HasFloatProperty(material, ModeFlagBindings[i].propertyName)) return false;
            return true;
        }

        internal static bool IsGraphIntentPackedHalf(string propertyName)
            => propertyName == "_NB_Flags0Lo16" || propertyName == "_NB_Flags0Hi16" ||
                propertyName == "_NB_Flags1Lo16" || propertyName == "_NB_Flags1Hi16";

        // Explicit native editor callback only. The caller owns the existing
        // Undo transaction and has already changed this raw slice. Ordinary
        // Validate/Sync still never guesses whether an unowned mirror is newer.
        internal static void NotifyGraphPackedFlagsEdited(Material material,
            string propertyName, int editedSliceBits = 65535)
        {
            if (!IsGraphIntentPackedHalf(propertyName) ||
                !GraphFlagIntentSchemaAvailable(material) ||
                !NBShaderRootItem.HasFloatProperty(material, GraphGUIStateVersionProperty) ||
                material.GetFloat(GraphGUIStateVersionProperty) != 2f) return;
            int word = propertyName.StartsWith("_NB_Flags0", StringComparison.Ordinal) ? 0 : 1;
            uint mask = unchecked((uint)editedSliceBits) & 65535u;
            if (propertyName.EndsWith("Hi16", StringComparison.Ordinal)) mask <<= 16;
            SeedGraphFlagIntents(material, word, mask);
        }

        private static void SeedGraphFlagIntents(Material material,
            int editedWord = -1, uint editedBits = uint.MaxValue)
        {
            var flags = new NBShaderFlags(material); // Shared Material halfword read hooks; NEVER write words here.
            for (int i = 0; i < ToggleFlagBindings.Length; ++i)
            {
                var binding = ToggleFlagBindings[i];
                if (editedWord >= 0 && (binding.flagIndex != editedWord ||
                    (unchecked((uint)binding.flagBits) & editedBits) == 0u)) continue;
                material.SetFloat(binding.propertyName,
                    flags.CheckFlagBits(binding.flagBits, index: binding.flagIndex) ? 1f : 0f);
            }
            for (int i = 0; i < ModeFlagBindings.Length; ++i)
            {
                var binding = ModeFlagBindings[i];
                if (editedWord >= 0 && (binding.flagIndex != editedWord ||
                    (unchecked((uint)binding.flagBits) & editedBits) == 0u)) continue;
                int disabledMode = binding.enabledMode == 0 ? 1 : 0;
                material.SetFloat(binding.propertyName,
                    flags.CheckFlagBits(binding.flagBits, index: binding.flagIndex)
                        ? binding.enabledMode : disabledMode);
            }
        }

        internal void PrepareGraphGUIState()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead(_rootItem.GraphGUIStateMayInitialize());
            if (_rootItem.Mats == null || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return;
            var uninitialized = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats)
            {
                if (!NBShaderGUIContext.IsGraphMaterial(material) ||
                    !NBShaderRootItem.HasFloatProperty(material, GraphGUIStateVersionProperty) ||
                    !NBShaderRootItem.HasFloatProperty(material, "_MainTexBigBlockItemFoldOut") ||
                    !NBShaderRootItem.HasFloatProperty(material, "_BaseMapFoldOut")) continue;
                float targetVersion = GraphFlagIntentSchemaAvailable(material) ? 2f : 1f;
                if (material.GetFloat(GraphGUIStateVersionProperty) < targetVersion) uninitialized.Add(material);
            }
            if (uninitialized.Count == 0) return;
            Undo.RecordObjects(uninitialized.ToArray(), "Initialize NB Graph GUI state");
            foreach (UnityEngine.Object target in uninitialized)
            {
                var material = (Material)target;
                bool seedFlagIntents = GraphFlagIntentSchemaAvailable(material);
                if (seedFlagIntents) SeedGraphFlagIntents(material);
                // Commit version LAST. Existing Graph functional properties, raw
                // halfwords (including noncanonical finite values), URP surface,
                // keywords, passes, queue, textures and prior foldouts are untouched.
                material.SetFloat(GraphGUIStateVersionProperty, seedFlagIntents ? 2f : 1f);
                EditorUtility.SetDirty(material);
            }
        }

        public void SyncMaterialState()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            // Static Material/list entry must also be protected without Context.
            if (NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return;
            PrepareGraphGUIState();
            for (int i = 0; i < _rootItem.Mats.Count; i++)
            {
                Material mat = _rootItem.Mats[i];
                if (mat == null)
                {
                    continue;
                }

                // Graph GUI1A's shared controls write their own supported words.
                // Never run legacy Mesh/Surface/Time/Tier/Pass projection here.
                if (NBShaderGUIContext.IsGraphMaterial(mat)) continue;

                NBShaderFlags flags = GetFlags(i);
                SyncMeshSourceMode(mat, flags);
                SyncCustomData(mat, flags);
                SyncUVDerivedFlags(flags);
                SyncTransparentMode(mat);
                SyncTransparentShadowFlags(mat, flags);
                SyncBlendMode(mat);
                SyncTimeMode(mat, flags);
                SyncTogglePropertyFlags(mat, flags);
                SyncParallaxLayerCount(mat);
                SyncResolvedIntentState(mat);
            }
        }

        public void ApplyToggleFlag(int flagBits, bool enabled, int flagIndex = 0)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return;
            foreach (ShaderFlagsBase flagBase in _rootItem.ShaderFlags)
            {
                if (enabled)
                {
                    SetFlag(flagBase, flagBits, true, flagIndex);
                }
                else
                {
                    SetFlag(flagBase, flagBits, false, flagIndex);
                }
            }
        }

        public void ApplyShaderPass(string passName, bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (IsResolvedIntentPass(passName) && NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetShaderPassEnabledIfNeeded(mat, passName, enabled);
            }
        }

        public void ApplyScreenDistortMode(int mode)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                    continue;

                if (mode == 0 && mat.HasProperty("_DisableMainPassToggle"))
                {
                    SetFloatIfExists(mat, "_DisableMainPassToggle", 0f);
                }

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
            }
        }

        public void ApplyDepthDecalEnabled(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) { TryApplyGraphDepthDecalEnabled(enabled); return; }
            ApplyToggleKeyword("_DEPTH_DECAL", enabled);
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                {
                    continue;
                }

                ApplyDepthDecalLinkedState(mat, enabled);
            }
        }

        public void ApplyPortalState()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                {
                    continue;
                }

                bool portal = mat.HasProperty("_Portal_Toggle") && mat.GetFloat("_Portal_Toggle") > 0.5f;
                bool mask = mat.HasProperty("_Portal_MaskToggle") && mat.GetFloat("_Portal_MaskToggle") > 0.5f;
                if (!portal)
                {
                    ApplyStencilPresetToMaterial(mat, "ParticleBaseDefault");
                    SetFloatIfExists(mat, "_CustomStencilTest", 0f);
                    SetFloatIfExists(mat, "_TransparentMode", (float)TransparentMode.Transparent);
                    SetFloatIfExists(mat, "_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    SetFloatIfExists(mat, "_ForceZWriteToggle", 0f);
                }
                else if (mask)
                {
                    ApplyStencilPresetToMaterial(mat, "ParticalBasePortalMask");
                    SetFloatIfExists(mat, "_CustomStencilTest", 1f);
                    if (mat.HasProperty("_TransparentMode") &&
                        Mathf.RoundToInt(mat.GetFloat("_TransparentMode")) == (int)TransparentMode.Transparent)
                    {
                        SetFloatIfExists(mat, "_TransparentMode", (float)TransparentMode.CutOff);
                    }

                    SetFloatIfExists(mat, "_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    SetFloatIfExists(mat, "_ForceZWriteToggle", 2f);
                }
                else
                {
                    ApplyStencilPresetToMaterial(mat, "ParticalBasePortal");
                    SetFloatIfExists(mat, "_CustomStencilTest", 1f);
                }
            }

            SyncMaterialState();
        }

        public void ApplyVatEnabled(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                SetFloatIfExists(mat, "_VAT_Toggle", enabled ? 1f : 0f);

                if (enabled)
                    DisableFlipbook(mat);

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SyncVatKeywords(mat);
            }
        }

        // Explicit shared Flipbook editor capability. General Graph Sync/Apply guards remain.
        internal bool HasGraphFlipbookEditSchema()
        {
            if (_rootItem.Mats == null || _rootItem.Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            foreach (Material material in _rootItem.Mats)
            {
                if (material == null || !NBShaderGUIContext.IsGraphMaterial(material)) return false;
                NBShaderMaterialIntentResult intent; string[] unavailable;
                if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,
                    NBShaderFeatureTier.Ultra, NBShaderFeatureCatalog.RawKeywords, out intent, out unavailable)) return false;
                foreach (string name in new[] { "_FlipbookBlending", "_VAT_Toggle", "_AnimationSheetHelperBlendIntensity" })
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, name) || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    float value = material.GetFloat(name); if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
                foreach (string name in new[] { "_BaseMap_ST", "_BaseMap_AnimationSheetBlend_ST" })
                {
                    int index = material.shader.FindPropertyIndex(name);
                    if (index < 0 || material.shader.GetPropertyType(index) != UnityEngine.Rendering.ShaderPropertyType.Vector || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    Vector4 value = material.GetVector(name);
                    for (int channel = 0; channel < 4; ++channel) if (float.IsNaN(value[channel]) || float.IsInfinity(value[channel])) return false;
                }
            }
            return true;
        }

        internal bool TryApplyGraphFlipbookEdit(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            // Same original enable-Flipbook excludes VAT; no pass/surface projection.
            return RunGraphVATTransaction("NB Flipbook",material=>{material.SetFloat("_FlipbookBlending",enabled?1:0);if(enabled)material.SetFloat("_VAT_Toggle",0);});
        }

        public void ApplyFlipbookEnabled(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                SetFloatIfExists(mat, "_FlipbookBlending", enabled ? 1f : 0f);

                if (enabled)
                {
                    DisableVat(mat);
                }

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetKeyword(mat, "_FLIPBOOKBLENDING_ON", enabled);
            }
        }

        public void ApplyBlendMode(BlendMode mode)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                bool nbShaderMaterial = NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat);
                switch (mode)
                {
                    case BlendMode.Alpha:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Premultiply:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Additive:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Multiply:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", true);
                        }
                        break;
                    case BlendMode.Opaque:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                }

                if (nbShaderMaterial)
                    SyncResolvedIntentState(mat);
            }
        }

        public void ApplyLightMode(FxLightMode mode)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetLightModeKeyword(mat, mode);
            }
        }

        public void ApplyToggleKeyword(string keyword, bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (NBShaderFeatureCatalog.IsManagedKeyword(keyword) &&
                    NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetKeyword(mat, keyword, enabled);
            }
        }

        public void ApplyStencilPreset(string key)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                {
                    continue;
                }

                ApplyStencilPresetToMaterial(mat, key);
            }
        }

        public bool AnyProgramNoiseEnabled()
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat.HasProperty("_ProgramNoise_Toggle") && mat.GetFloat("_ProgramNoise_Toggle") > 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private NBShaderFlags GetFlags(int index)
        {
            return index >= 0 &&
                   index < _rootItem.ShaderFlags.Count &&
                   _rootItem.ShaderFlags[index] is NBShaderFlags flags
                ? flags
                : null;
        }

        private StencilValuesConfig GetStencilValuesConfig()
        {
            if (_stencilValuesConfig == null)
            {
                _stencilValuesConfig = AssetDatabase.LoadAssetAtPath<StencilValuesConfig>(StencilConfigAssetPath);
            }

            return _stencilValuesConfig;
        }

        private void ApplyStencilPresetToMaterial(Material mat, string key)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            StencilValuesConfig config = GetStencilValuesConfig();
            if (config != null)
            {
                StencilTestHelper.SetMaterialStencil(mat, key, config, out _);
            }
            else
            {
                ApplyFallbackStencilPreset(mat, key);
            }
        }

        private static void ApplyFallbackStencilPreset(Material mat, string key)
        {
            int stencil = 0;
            int comp = (int)UnityEngine.Rendering.CompareFunction.Always;
            int pass = (int)UnityEngine.Rendering.StencilOp.Keep;
            int keyIndex = 0;

            switch (key)
            {
                case "ParticalBasePortal":
                    stencil = 200;
                    comp = (int)UnityEngine.Rendering.CompareFunction.Equal;
                    keyIndex = 2;
                    break;
                case "ParticalBasePortalMask":
                    stencil = 200;
                    pass = (int)UnityEngine.Rendering.StencilOp.Replace;
                    keyIndex = 3;
                    break;
                case "ParticleBaseDecal":
                    stencil = 2;
                    comp = (int)UnityEngine.Rendering.CompareFunction.GreaterEqual;
                    keyIndex = 4;
                    break;
                case "ParticleWithoutPlayer":
                    stencil = 5;
                    comp = (int)UnityEngine.Rendering.CompareFunction.Greater;
                    keyIndex = 5;
                    break;
            }

            SetFloatIfExists(mat, "_Stencil", stencil);
            SetFloatIfExists(mat, "_StencilComp", comp);
            SetFloatIfExists(mat, "_StencilOp", pass);
            SetFloatIfExists(mat, "_StencilFail", (int)UnityEngine.Rendering.StencilOp.Keep);
            SetFloatIfExists(mat, "_StencilZFail", (int)UnityEngine.Rendering.StencilOp.Keep);
            SetFloatIfExists(mat, "_StencilReadMask", 255f);
            SetFloatIfExists(mat, "_StencilWriteMask", 255f);
            SetFloatIfExists(mat, "_StencilKeyIndex", keyIndex);
        }

        private static void SetFloatIfExists(Material mat, string propertyName, float value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                !Mathf.Approximately(mat.GetFloat(propertyName), value))
            {
                mat.SetFloat(propertyName, value);
            }
        }

        private static void SetIntIfExists(Material mat, string propertyName, int value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                Mathf.RoundToInt(mat.GetFloat(propertyName)) != value)
            {
                mat.SetInt(propertyName, value);
            }
        }

        private static void SetVectorIfExists(Material mat, string propertyName, Vector4 value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                mat.GetVector(propertyName) != value)
            {
                mat.SetVector(propertyName, value);
            }
        }

        private static void SetRenderQueueIfNeeded(Material mat, int renderQueue)
        {
            if (mat != null && mat.renderQueue != renderQueue)
            {
                mat.renderQueue = renderQueue;
            }
        }

        private static void SetShaderPassEnabledIfNeeded(Material mat, string passName, bool enabled)
        {
            if (mat != null && !string.IsNullOrEmpty(passName) && mat.GetShaderPassEnabled(passName) != enabled)
            {
                mat.SetShaderPassEnabled(passName, enabled);
            }
        }

        private void SyncMeshSourceMode(Material mat, NBShaderFlags flags)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (flags == null || !mat.HasProperty("_MeshSourceMode"))
            {
                return;
            }

            MeshSourceMode mode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            bool isParticle = mode == MeshSourceMode.Particle || mode == MeshSourceMode.UIParticle;
            bool isUIEffect = mode == MeshSourceMode.UIEffectRawImage ||
                              mode == MeshSourceMode.UIEffectSprite ||
                              mode == MeshSourceMode.UIEffectBaseMap ||
                              mode == MeshSourceMode.UIParticle;
            bool useBaseMapTexture = mode == MeshSourceMode.UIEffectBaseMap ||
                                     mode == MeshSourceMode.UIParticle;

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM, isParticle, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, mode == MeshSourceMode.Mesh, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, isUIEffect, 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE, mode == MeshSourceMode.UIEffectSprite, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE, useBaseMapTexture, 1);
            if (mode == MeshSourceMode.Particle)
            {
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER, false, 1);
            }

            if (mat.HasProperty("_CustomData"))
            {
                SetFloatIfExists(mat, "_CustomData", isParticle ? 1f : 0f);
            }

            if (isParticle)
            {
                SetKeyword(mat, "_CUSTOMDATA", true);
            }
            else
            {
                SetKeyword(mat, "_CUSTOMDATA", false);
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON, false, 0);
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON, false, 0);
            }
        }

        private void SyncTimeMode(Material mat, NBShaderFlags flags)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (flags == null || !mat.HasProperty("_TimeMode"))
            {
                return;
            }

            TimeMode mode = (TimeMode)Mathf.RoundToInt(mat.GetFloat("_TimeMode"));
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON, mode == TimeMode.UnScaleTime, 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON, mode == TimeMode.ScriptableTime, 0);
        }

        private void SyncTogglePropertyFlags(Material mat, NBShaderFlags flags)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (flags == null || mat == null)
            {
                return;
            }

            for (int i = 0; i < ToggleFlagBindings.Length; i++)
            {
                var binding = ToggleFlagBindings[i];
                if (!mat.HasProperty(binding.propertyName))
                {
                    continue;
                }

                SetFlag(flags, binding.flagBits, mat.GetFloat(binding.propertyName) > 0.5f, binding.flagIndex);
            }

            for (int i = 0; i < ModeFlagBindings.Length; i++)
            {
                var binding = ModeFlagBindings[i];
                if (!mat.HasProperty(binding.propertyName))
                {
                    continue;
                }

                SetFlag(flags, binding.flagBits, Mathf.RoundToInt(mat.GetFloat(binding.propertyName)) == binding.enabledMode, binding.flagIndex);
            }
        }

        private bool IsKeywordAllowed(string keyword)
        {
            return _rootItem.Context == null || _rootItem.Context.IsKeywordAllowed(keyword);
        }

        private void SetKeyword(Material mat, string keyword, bool enabled)
        {
            if (mat == null || string.IsNullOrEmpty(keyword))
            {
                return;
            }

            bool shouldEnable = enabled && IsKeywordAllowed(keyword);
            if (mat.IsKeywordEnabled(keyword) == shouldEnable)
            {
                return;
            }

            if (shouldEnable)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }

            KeywordVersion++;
        }

        private void SyncResolvedIntentState(Material mat)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            var tier = ResolveMaterialTier(mat);
            var allowedKeywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForReadOnlyUse(tier);
            var allowedPassFeatures = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForReadOnlyUse(tier);
            var result = NBShaderMaterialIntentResolver.Resolve(mat, tier, allowedKeywords, allowedPassFeatures);
            var effectiveKeywords = new HashSet<string>(result.effectiveKeywords);

            for (int i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                string keyword = NBShaderFeatureCatalog.RawKeywords[i];
                SetKeyword(mat, keyword, effectiveKeywords.Contains(keyword));
            }

            for (int i = 0; i < result.passes.Length; i++)
            {
                NBShaderPassIntent pass = result.passes[i];
                if (!string.IsNullOrEmpty(pass.passName))
                {
                    SetShaderPassEnabledIfNeeded(mat, pass.passName, pass.included);
                }
            }

            SetKeyword(mat, "EVALUATE_SH_VERTEX", effectiveKeywords.Contains("_FX_LIGHT_MODE_SIX_WAY"));
        }

        private void SyncResolvedIntentStateIfNBShader(Material mat)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                SyncResolvedIntentState(mat);
        }

        private NBShaderFeatureTier ResolveMaterialTier(Material mat)
        {
            if (mat != null && mat.HasProperty(FeatureTierPropertyName))
            {
                int value = Mathf.RoundToInt(mat.GetFloat(FeatureTierPropertyName));
                if (value >= (int)NBShaderFeatureTier.Low && value <= (int)NBShaderFeatureTier.Ultra)
                {
                    return (NBShaderFeatureTier)value;
                }
            }

            if (_rootItem.Context != null && !_rootItem.Context.CurrentTierMixed)
            {
                return _rootItem.Context.CurrentTier;
            }

            return NBShaderFeatureTier.Ultra;
        }

        private static bool IsResolvedIntentPass(string passName)
        {
            if (string.IsNullOrEmpty(passName))
                return false;

            if (string.Equals(passName, NBShaderPassFeatureCatalog.MainForwardPassName, StringComparison.Ordinal))
                return true;

            string passFeatureId;
            return NBShaderFeatureLevelCatalog.TryGetManagedPassFeatureByPassName(passName, out passFeatureId);
        }

        private void SyncCustomData(Material mat, NBShaderFlags flags)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (flags == null || !mat.HasProperty("_MeshSourceMode"))
            {
                return;
            }

            MeshSourceMode mode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            bool isParticle = mode == MeshSourceMode.Particle || mode == MeshSourceMode.UIParticle;
            if (!isParticle)
            {
                return;
            }

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON, flags.IsCustomData1On(), 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON, flags.IsCustomData2On(), 0);
        }

        private static void SyncUVDerivedFlags(NBShaderFlags flags)
        {
            if (flags == null)
            {
                return;
            }

            if (!flags.CheckIsUVModeOn(NBShaderFlags.UVMode.SpecialUVChannel))
            {
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, false, 1);
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, false, 1);
            }

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE, flags.CheckIsUVModeOn(NBShaderFlags.UVMode.Cylinder), 1);
        }

        private void SyncTransparentMode(Material mat)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!mat.HasProperty("_TransparentMode"))
            {
                return;
            }

            TransparentMode mode = (TransparentMode)Mathf.RoundToInt(mat.GetFloat("_TransparentMode"));

            if (mode != TransparentMode.Transparent)
            {
                SetFloatIfExists(mat, "_TransparentShadowDitherToggle", 0f);
            }

            switch (mode)
            {
                case TransparentMode.Opaque:
                    SetIntIfExists(mat, "_ZWrite", 1);
                    SetFloatIfExists(mat, "_Blend", (float)BlendMode.Opaque);
                    break;
                case TransparentMode.Transparent:
                    SetIntIfExists(mat, "_ZWrite", 0);
                    if (mat.HasProperty("_Blend") && (BlendMode)Mathf.RoundToInt(mat.GetFloat("_Blend")) == BlendMode.Opaque)
                    {
                        SetFloatIfExists(mat, "_Blend", (float)BlendMode.Alpha);
                    }

                    break;
                case TransparentMode.CutOff:
                    SetIntIfExists(mat, "_ZWrite", 1);
                    SetFloatIfExists(mat, "_Blend", (float)BlendMode.Opaque);
                    break;
            }

            if (mat.HasProperty("_ForceZWriteToggle"))
            {
                float forceZWrite = mat.GetFloat("_ForceZWriteToggle");
                if (forceZWrite > 0.5f && forceZWrite < 1.5f)
                {
                    SetIntIfExists(mat, "_ZWrite", 1);
                }
                else if (forceZWrite > 1.5f)
                {
                    SetIntIfExists(mat, "_ZWrite", 0);
                }
            }
        }

        private static void SyncTransparentShadowFlags(Material mat, NBShaderFlags flags)
        {
            TransparentMode mode = mat != null && mat.HasProperty("_TransparentMode")
                ? (TransparentMode)Mathf.RoundToInt(mat.GetFloat("_TransparentMode"))
                : TransparentMode.UnKnowOrMixed;
            bool isTransparent = mode == TransparentMode.Transparent;
            bool useTransparentShadowDither = isTransparent &&
                                              mat.HasProperty("_TransparentShadowDitherToggle") &&
                                              mat.GetFloat("_TransparentShadowDitherToggle") > 0.5f;

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_MODE, isTransparent, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_SHADOW_DITHER, useTransparentShadowDither, 1);
        }

        private static bool IsUIEffectMode(Material mat)
        {
            if (mat == null || !mat.HasProperty("_MeshSourceMode"))
            {
                return false;
            }

            MeshSourceMode meshSourceMode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            return meshSourceMode == MeshSourceMode.UIEffectRawImage ||
                   meshSourceMode == MeshSourceMode.UIEffectSprite ||
                   meshSourceMode == MeshSourceMode.UIEffectBaseMap ||
                   meshSourceMode == MeshSourceMode.UIParticle;
        }

        private static void SyncParallaxLayerCount(Material mat)
        {
            if (mat == null ||
                !mat.HasProperty("_ParallaxMapping_Toggle") ||
                !mat.HasProperty("_ParallaxMapping_Vec") ||
                mat.GetFloat("_ParallaxMapping_Toggle") <= 0.5f)
            {
                return;
            }

            Vector4 value = mat.GetVector("_ParallaxMapping_Vec");
            if (value.y < value.x + 1f)
            {
                value.y = value.x + 1f;
                SetVectorIfExists(mat, "_ParallaxMapping_Vec", value);
            }
        }

        private void SyncBlendMode(Material mat)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!mat.HasProperty("_Blend"))
            {
                return;
            }

            switch ((BlendMode)Mathf.RoundToInt(mat.GetFloat("_Blend")))
            {
                case BlendMode.Alpha:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Premultiply:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Additive:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Multiply:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Opaque:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;
            }
        }

        private void SetLightModeKeyword(Material mat, FxLightMode mode)
        {
            SetKeyword(mat, "_FX_LIGHT_MODE_UNLIT", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_BLINN_PHONG", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_HALF_LAMBERT", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_PBR", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_SIX_WAY", false);
            SetKeyword(mat, "EVALUATE_SH_VERTEX", false);

            switch (mode)
            {
                case FxLightMode.UnLit:
                    SetKeyword(mat, "_FX_LIGHT_MODE_UNLIT", true);
                    break;
                case FxLightMode.BlinnPhong:
                    SetKeyword(mat, "_FX_LIGHT_MODE_BLINN_PHONG", true);
                    break;
                case FxLightMode.HalfLambert:
                    SetKeyword(mat, "_FX_LIGHT_MODE_HALF_LAMBERT", true);
                    break;
                case FxLightMode.PBR:
                    SetKeyword(mat, "_FX_LIGHT_MODE_PBR", true);
                    break;
                case FxLightMode.SixWay:
                    SetKeyword(mat, "_FX_LIGHT_MODE_SIX_WAY", true);
                    SetKeyword(mat, "EVALUATE_SH_VERTEX", true);
                    break;
            }
        }

        private void SyncVatKeywords(Material mat)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!mat.HasProperty("_VAT_Toggle") || mat.GetFloat("_VAT_Toggle") <= 0.5f)
            {
                ClearVatKeywords(mat);
                return;
            }

            DisableFlipbook(mat);
            SetKeyword(mat, "_VAT", true);
            int vatMode = mat.HasProperty("_VATMode") ? Mathf.RoundToInt(mat.GetFloat("_VATMode")) : 0;
            if (vatMode == (int)VATMode.Tyflow)
            {
                SetKeyword(mat, "_VAT_HOUDINI", false);
                SetKeyword(mat, "_VAT_TYFLOW", true);
                SetHoudiniVATKeyword(mat, -1);
                SetTyflowVATKeyword(mat, mat.HasProperty("_TyFlowVATSubMode") ? Mathf.RoundToInt(mat.GetFloat("_TyFlowVATSubMode")) : 0);
            }
            else
            {
                SetKeyword(mat, "_VAT_HOUDINI", true);
                SetKeyword(mat, "_VAT_TYFLOW", false);
                SetHoudiniVATKeyword(mat, mat.HasProperty("_HoudiniVATSubMode") ? Mathf.RoundToInt(mat.GetFloat("_HoudiniVATSubMode")) : 0);
                SetTyflowVATKeyword(mat, -1);
            }
        }

        private void DisableVat(Material mat)
        {
            SetFloatIfExists(mat, "_VAT_Toggle", 0f);

            ClearVatKeywords(mat);
        }

        private void DisableFlipbook(Material mat)
        {
            SetFloatIfExists(mat, "_FlipbookBlending", 0f);

            SetKeyword(mat, "_FLIPBOOKBLENDING_ON", false);
        }

        private void ClearVatKeywords(Material mat)
        {
            SetKeyword(mat, "_VAT", false);
            SetKeyword(mat, "_VAT_HOUDINI", false);
            SetKeyword(mat, "_VAT_TYFLOW", false);
            SetHoudiniVATKeyword(mat, -1);
            SetTyflowVATKeyword(mat, -1);
        }

        private void SetHoudiniVATKeyword(Material mat, int enabledIndex)
        {
            SetExclusiveKeyword(mat, HoudiniVatKeywords, enabledIndex);
        }

        private void SetTyflowVATKeyword(Material mat, int enabledIndex)
        {
            SetExclusiveKeyword(mat, TyflowVatKeywords, enabledIndex);
        }

        private void SetExclusiveKeyword(Material mat, string[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                SetKeyword(mat, keywords[i], false);
            }

            if (enabledIndex >= 0 && enabledIndex < keywords.Length)
            {
                SetKeyword(mat, keywords[enabledIndex], true);
            }
        }

        private static void SetFlag(ShaderFlagsBase flags, int flagBits, bool enabled, int index)
        {
            if (flags == null ||
                flags.material == null ||
                flags.CheckFlagBits(flagBits, index: index) == enabled)
            {
                return;
            }

            if (enabled)
            {
                flags.SetFlagBits(flagBits, index: index);
            }
            else
            {
                flags.ClearFlagBits(flagBits, index: index);
            }
        }

        private struct FlagToggleBinding
        {
            public readonly string propertyName;
            public readonly int flagBits;
            public readonly int flagIndex;

            public FlagToggleBinding(string propertyName, int flagBits, int flagIndex)
            {
                this.propertyName = propertyName;
                this.flagBits = flagBits;
                this.flagIndex = flagIndex;
            }
        }

        private struct FlagModeBinding
        {
            public readonly string propertyName;
            public readonly int flagBits;
            public readonly int flagIndex;
            public readonly int enabledMode;

            public FlagModeBinding(string propertyName, int flagBits, int flagIndex, int enabledMode)
            {
                this.propertyName = propertyName;
                this.flagBits = flagBits;
                this.flagIndex = flagIndex;
                this.enabledMode = enabledMode;
            }
        }

        // Explicit one-time migration only. Caller captures legacy main state
        // BEFORE switching the Graph/Shader route; no automatic asset write.
        internal static bool TryMigrateGraphColorPassState(Material material, bool capturedLegacyMainEnabled)
            => TryCommitGraphColorPassState(material, capturedLegacyMainEnabled);

        private static bool TryCommitGraphColorPassState(Material material, bool selectedMainEnabled)
        {
            int route;
            if (!NBShaderPassFeatureCatalog.TryGetGraphColorRouting(material, out route) || route != 1 ||
                !NBShaderRootItem.HasFloatProperty(material, "_NB_GraphPassMigrationComplete") ||
                !NBShaderRootItem.HasFloatProperty(material, "_NB_BackFirstEffective")) return false;
            float migrationState = material.GetFloat("_NB_GraphPassMigrationComplete");
            if (migrationState != 0f && migrationState != 1f) return false;
            if (migrationState == 1f) return false;
            material.SetShaderPassEnabled("UniversalForward", selectedMainEnabled);
            material.SetShaderPassEnabled("SRPDefaultUnlit", false);
            material.SetFloat("_NB_BackFirstEffective", 0f);
            material.SetFloat("_NB_GraphPassMigrationComplete", 1f);
            return true;
        }

        internal static bool TryApplyGraphBackFirstPassIntent(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPassFeatureIds)
        {
            return NBShaderFeatureRuntime.TryApplyGraphBackFirstPassIntent(material, tier, allowedKeywords, allowedPassFeatureIds);
        }

        internal bool HasGraphFresnelEditSchema()
        {
            if (!HasGraphMainTexTargets() || _rootItem.MatEditor == null) return false;
            foreach (Material material in _rootItem.Mats)
            {
                foreach (string name in new[] { GraphGUIStateVersionProperty, "_FresnelBlockFoldOut", "_fresnelEnabled",
                    "_FresnelMode", "_InvertFresnel_Toggle", "_FresnelColorAffectByAlpha", "_NB_Debug_Fresnel",
                    "_NB_TierAllowFresnel", "_NBShaderFeatureTier", "_NB_CustomDataFlag0Lo16", "_NB_CustomDataFlag0Hi16" })
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, name) || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    float value = material.GetFloat(name); if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
                if (material.GetFloat(GraphGUIStateVersionProperty) != 2f) return false;
                float mode = material.GetFloat("_FresnelMode");
                if (mode != 0f && mode != 1f) return false;
                foreach (string name in new[] { "_FresnelUnit", "_FresnelColor", "_FresnelRotation" })
                {
                    int index = material.shader.FindPropertyIndex(name);
                    if (index < 0 || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    var type = material.shader.GetPropertyType(index);
                    if (type != UnityEngine.Rendering.ShaderPropertyType.Vector && type != UnityEngine.Rendering.ShaderPropertyType.Color) return false;
                    Vector4 value = material.GetVector(name);
                    for (int channel = 0; channel < 4; ++channel) if (float.IsNaN(value[channel]) || float.IsInfinity(value[channel])) return false;
                }
                bool wouldChange;
                if (!CanApplyGraphSavedProjectionForPaint(material, out wouldChange)) return false;
            }
            return true;
        }

        internal bool TryApplyGraphFresnelEdit(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphFresnelEditSchema()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                SetFloatIfExists(material, "_fresnelEnabled", enabled ? 1f : 0f);
                bool changed;
                if (!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material, out changed)) return false;
            }
            RefreshGraphMainTexPropertyReferences();
            _rootItem.Context?.Refresh();
            return true;
        }

        internal bool TryApplyGraphFresnelFlagEdit(int bit, bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (bit != NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_FADE_ON &&
                bit != NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON &&
                bit != NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA) return false;
            if (!HasGraphFresnelEditSchema()) return false;
            string halfName = (bit & 65535) != 0 ? "_NB_Flags0Lo16" : "_NB_Flags0Hi16";
            int halfMask = (bit & 65535) != 0 ? bit & 65535 : (int)((uint)bit >> 16);
            foreach (Material material in _rootItem.Mats)
            {
                if (!WriteGraphHalfSlice(material, halfName, halfMask, enabled ? halfMask : 0))
                    NotifyGraphPackedFlagsEdited(material, halfName, halfMask);
            }
            RefreshGraphMainTexPropertyReferences();
            return true;
        }

        internal static readonly string[] GraphDissolveSharedPropertyNames = { "Dissolve2XRangeVec", "Dissolve2YRangeVec", "DissolveXRangeVec", "_Dissolve", "_DissolveBlockFoldOut", "_DissolveLineColor", "_DissolveLineFoldOut", "_DissolveLineMaskToggle", "_DissolveMap", "_DissolveMapFoldOut", "_DissolveMaskFoldOut", "_DissolveMaskMap", "_DissolveMaskMode", "_DissolveMaskUVModeFoldOut", "_DissolveMask_Toggle", "_DissolveOffsetRotateDistort", "_DissolvePNoiseBlendOpacity", "_DissolveRampAlpha0", "_DissolveRampAlpha1", "_DissolveRampAlpha2", "_DissolveRampColor", "_DissolveRampColor0", "_DissolveRampColor1", "_DissolveRampColor2", "_DissolveRampColor3", "_DissolveRampColor4", "_DissolveRampColor5", "_DissolveRampColorBlendMode", "_DissolveRampCount", "_DissolveRampFoldOut", "_DissolveRampMap", "_DissolveRampSourceMode", "_DissolveUVModeFoldOut", "_Dissolve_Toggle", "_Dissolve_Vec2", "_Dissolve_useRampMap_Toggle", "_NB_Debug_Dissolve" };

        internal bool TryApplyGraphDissolveUVMode(int position,NBShaderFlags.UVMode mode,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(position!=NBShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP && position!=NBShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MASK_MAP)return false;
            if(!HasGraphDissolveEditSchema() || !HasGraphMainTexUVEditSchema() || (int)mode<0 || (int)mode>8)return false;
            string fold=position==NBShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP?"_DissolveUVModeFoldOut":"_DissolveMaskUVModeFoldOut";
            string suffix=position<16?"Lo16":"Hi16";int shift=position<16?position:position-16;int mask=3<<shift;
            return RunGraphMainTexEdit("Dissolve UV Source",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0"+suffix,mask,((int)mode&3)<<shift);
                changed|=WriteGraphHalfSlice(material,"_NB_UVModeFlagType0"+suffix,mask,((int)mode/4)<<shift);
                if(setFold)
                {
                    float value=mode==NBShaderFlags.UVMode.DefaultUVChannel||mode==NBShaderFlags.UVMode.CommonUV||mode==NBShaderFlags.UVMode.ScreenUV||mode==NBShaderFlags.UVMode.MainTex?0:1;
                    if(material.GetFloat(fold)!=value){material.SetFloat(fold,value);changed=true;}
                }
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }

        internal bool HasGraphDissolveEditSchema()
        {
            if (!HasGraphMainTexTargets() || _rootItem.MatEditor == null) return false;
            foreach (Material material in _rootItem.Mats)
            {
                foreach (string name in GraphDissolveSharedPropertyNames)
                    if (!_rootItem.PropertyInfoDic.ContainsKey(name) || !material.HasProperty(name)) return false;
                foreach (string name in new[] { "_Dissolve_Toggle", "_DissolveMask_Toggle", "_Dissolve_useRampMap_Toggle", "_DissolveMaskMode", "_DissolveRampSourceMode", "_DissolveRampColorBlendMode", "_DissolveLineMaskToggle", "_NB_GraphGUIStateVersion" })
                {
                    if (!NBShaderRootItem.HasFloatProperty(material,name)) return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                if(material.GetFloat(GraphGUIStateVersionProperty)!=2f)return false;
                foreach(string name in new[]{"_DissolveMaskMode","_DissolveRampSourceMode","_DissolveRampColorBlendMode"})
                {float mode=material.GetFloat(name);if(mode!=0f&&mode!=1f)return false;}
                bool wouldChange;
                if(!CanApplyGraphSavedProjectionForPaint(material,out wouldChange))return false;
            }
            return true;
        }

        internal bool TryApplyGraphDissolveIntentEdit()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphDissolveEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed))return false;}
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
        }

        internal bool TryApplyGraphDissolveFlagEdit(int bit,bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(bit!=NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK && bit!=NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY)return false;
            if(!HasGraphDissolveEditSchema())return false;
            string name=(bit&65535)!=0?"_NB_Flags1Lo16":"_NB_Flags1Hi16";int mask=(bit&65535)!=0?bit&65535:(int)((uint)bit>>16);
            foreach(Material material in _rootItem.Mats)
                if(!WriteGraphHalfSlice(material,name,mask,enabled?mask:0))NotifyGraphPackedFlagsEdited(material,name,mask);
            RefreshGraphMainTexPropertyReferences();return true;
        }

        internal static readonly string[] GraphMaskProgramSharedPropertyNames={"MaskDistortionIntensityRangeVec","_DissolveVoronoi_Vec","_DissolveVoronoi_Vec2","_DissolveVoronoi_Vec3","_DissolveVoronoi_Vec4","_Mask2BlockFoldOut","_Mask2UVModeFoldOut","_Mask2_Toggle","_Mask3BlockFoldOut","_Mask3UVModeFoldOut","_Mask3_Toggle","_MaskBlockFoldOut","_MaskDistortion_intensity","_MaskMap","_MaskMap2","_MaskMap2GradientCount","_MaskMap2GradientFloat0","_MaskMap2GradientFloat1","_MaskMap2GradientFloat2","_MaskMap2GradientToggle","_MaskMap3","_MaskMap3GradientCount","_MaskMap3GradientFloat0","_MaskMap3GradientFloat1","_MaskMap3GradientFloat2","_MaskMap3GradientToggle","_MaskMap3OffsetAnition","_MaskMapFoldOut","_MaskMapGradientCount","_MaskMapGradientFloat0","_MaskMapGradientFloat1","_MaskMapGradientFloat2","_MaskMapGradientToggle","_MaskMapOffsetAnition","_MaskMapRotationSpeed","_MaskMapUVRotation","_MaskMapVec","_MaskPNoiseBlendOpacity","_MaskRefineFoldOut","_MaskRefineToggle","_MaskRefineVec","_MaskRotationFoldOut","_MaskUVModeFoldOut","_Mask_RotationToggle","_Mask_Toggle","_NB_Debug_Mask","_NB_Debug_PNoise","_ProgramNoiseBaseBlendOpacity","_ProgramNoiseBlockFoldOut","_ProgramNoiseSimpleFoldOut","_ProgramNoiseUVModeFoldOut","_ProgramNoiseVoronoiFoldOut","_ProgramNoise_Rotate","_ProgramNoise_Simple_Toggle","_ProgramNoise_Toggle","_ProgramNoise_Voronoi_Toggle"};

        internal bool HasGraphMaskProgramEditSchema()
        {
            if(!HasGraphMainTexTargets()||_rootItem.MatEditor==null)return false;
            foreach(Material material in _rootItem.Mats)
            {
                foreach(string n in GraphMaskProgramSharedPropertyNames)if(!material.HasProperty(n)||!_rootItem.PropertyInfoDic.ContainsKey(n))return false;
                foreach(string n in new[]{"_Mask_Toggle","_Mask2_Toggle","_Mask3_Toggle","_MaskRefineToggle","_MaskMapGradientToggle","_MaskMap2GradientToggle","_MaskMap3GradientToggle","_Mask_RotationToggle","_ProgramNoise_Toggle","_ProgramNoise_Simple_Toggle","_ProgramNoise_Voronoi_Toggle",GraphGUIStateVersionProperty})
                {if(!NBShaderRootItem.HasFloatProperty(material,n))return false;float v=material.GetFloat(n);if(float.IsNaN(v)||float.IsInfinity(v))return false;}
                if(material.GetFloat(GraphGUIStateVersionProperty)!=2)return false;
                foreach(string n in new[]{"_MaskMapGradientToggle","_MaskMap2GradientToggle","_MaskMap3GradientToggle"}){float v=material.GetFloat(n);if(v!=0&&v!=1)return false;}
                bool change;if(!CanApplyGraphSavedProjectionForPaint(material,out change))return false;
            }
            return true;
        }
        internal bool TryApplyGraphMaskProgramIntentEdit()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphMaskProgramEditSchema())return false;
            foreach(Material material in _rootItem.Mats){bool change;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out change))return false;}
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
        }
        internal bool TryApplyGraphMaskProgramFlagEdit(int bit,bool enabled,int word)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            bool owned=word==0&&bit==NBShaderFlags.FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON || word==1&&(bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE||bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT||bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT||bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT);
            if(!owned||!HasGraphMaskProgramEditSchema())return false;
            string name="_NB_Flags"+word+((bit&65535)!=0?"Lo16":"Hi16");int mask=(bit&65535)!=0?bit&65535:(int)((uint)bit>>16);
            return RunGraphMainTexEdit("Mask Flags",material=>{
                bool changed=WriteGraphHalfSlice(material,name,mask,enabled?mask:0);
                // New UI mirror is Graph explicit-action-only. Do not add it to
                // the shared Native projection table or seed marker2 on paint.
                if(word==0&&bit==NBShaderFlags.FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON)
                {float value=enabled?1:0;if(material.GetFloat("_Mask_RotationToggle")!=value){material.SetFloat("_Mask_RotationToggle",value);changed=true;}}
                if(!changed)NotifyGraphPackedFlagsEdited(material,name,mask);return changed;
            });
        }
        internal bool TryApplyGraphMaskProgramUVMode(int position,NBShaderFlags.UVMode mode,string fold,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            bool owned=position==2&&fold=="_MaskUVModeFoldOut" || position==4&&fold=="_Mask2UVModeFoldOut" || position==6&&fold=="_Mask3UVModeFoldOut" || position==28&&fold=="_ProgramNoiseUVModeFoldOut";
            if(!owned||!HasGraphMaskProgramEditSchema()||!HasGraphMainTexUVEditSchema()||(int)mode<0||(int)mode>8)return false;
            string suffix=position<16?"Lo16":"Hi16";int shift=position&15,mask=3<<shift;
            return RunGraphMainTexEdit("Mask/Program Noise UV",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0"+suffix,mask,((int)mode&3)<<shift)|WriteGraphHalfSlice(material,"_NB_UVModeFlagType0"+suffix,mask,((int)mode/4)<<shift);
                if(setFold){float v=mode==NBShaderFlags.UVMode.DefaultUVChannel||mode==NBShaderFlags.UVMode.CommonUV||mode==NBShaderFlags.UVMode.ScreenUV||mode==NBShaderFlags.UVMode.MainTex?0:1;if(material.GetFloat(fold)!=v){material.SetFloat(fold,v);changed=true;}}
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }
        internal bool TryApplyGraphSharedFeatureUVMode(int position,NBShaderFlags.UVMode mode,string fold,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(position==NBShaderFlags.FLAG_BIT_UVMODE_POS_0_SHAREDUV && fold=="_SharedUVModeFoldOut")return TryApplyGraphSharedUVMode(mode,setFold);
            if(position==26 && fold=="_RampColorUVModeFoldOut")return TryApplyGraphColorRampUVMode(mode,setFold);
            if(position==8||position==10)return TryApplyGraphNoiseUVMode(position,mode,fold,setFold);
            if(position==20||position==22)return TryApplyGraphVertexOffsetUV(position,mode,fold,setFold);
            if(position==14||position==16)return TryApplyGraphDissolveUVMode(position,mode,setFold);
            return TryApplyGraphMaskProgramUVMode(position,mode,fold,setFold);
        }

        internal static readonly string[] GraphColorAdjustmentFloats = { "_ColorAdjustmentOnlyAffectMainTex", "_HueShift_Toggle", "_ChangeSaturability_Toggle", "_Contrast_Toggle", "_BaseMapColorRefine_Toggle", "_ColorMultiAlpha", "_HueShift", "_Contrast", "_Saturability", "_BaseColorAdjustmentFoldOut", "_HueShiftFoldOut", "_SaturabilityFoldOut", "_ContrastFoldOut", "_BaseMapColorRefineFoldOut" };
        internal static readonly string[] GraphColorAdjustmentColors = { "_ContrastMidColor" };
        internal static readonly string[] GraphColorAdjustmentVectors = { "_BaseMapColorRefine", "SaturabilityRangeVec" };
        internal static readonly string[] GraphColorRampFloats = { "_RampColorToggle", "_RampColorSourceMode", "_RampColorBlendMode", "_RampColorCount", "_RampColorBlockFoldOut", "_RampColorUVModeFoldOut" };
        internal static readonly string[] GraphColorRampColors = { "_RampColor0", "_RampColor1", "_RampColor2", "_RampColor3", "_RampColor4", "_RampColor5", "_RampColorBlendColor" };
        internal static readonly string[] GraphColorRampVectors = { "_RampColorAlpha0", "_RampColorAlpha1", "_RampColorAlpha2", "_RampColorMapOffset" };
        internal static readonly string[] GraphColorAdjustmentSharedPropertyNames = { "_ColorAdjustmentOnlyAffectMainTex", "_HueShift_Toggle", "_ChangeSaturability_Toggle", "_Contrast_Toggle", "_BaseMapColorRefine_Toggle", "_ColorMultiAlpha", "_HueShift", "_Contrast", "_Saturability", "_BaseColorAdjustmentFoldOut", "_HueShiftFoldOut", "_SaturabilityFoldOut", "_ContrastFoldOut", "_BaseMapColorRefineFoldOut", "_ContrastMidColor", "_BaseMapColorRefine", "SaturabilityRangeVec" };
        internal static readonly string[] GraphColorRampSharedPropertyNames = { "_RampColorToggle", "_RampColorSourceMode", "_RampColorBlendMode", "_RampColorCount", "_RampColorBlockFoldOut", "_RampColorUVModeFoldOut", "_RampColor0", "_RampColor1", "_RampColor2", "_RampColor3", "_RampColor4", "_RampColor5", "_RampColorBlendColor", "_RampColorAlpha0", "_RampColorAlpha1", "_RampColorAlpha2", "_RampColorMapOffset", "_RampColorMap" };

        // These leaves reuse existing property types and packed protocol. Unknown
        // schemas decline ownership; passive paint never seeds or normalizes them.
        bool HasGraphColorLeafSchema(string[] floats, string[] colors, string[] vectors, bool ramp)
        {
            if (!HasGraphMainTexTargets() || _rootItem.MatEditor == null) return false;
            foreach (Material material in _rootItem.Mats)
            {
                if (!GraphFlagIntentSchemaAvailable(material) || material.GetFloat(GraphGUIStateVersionProperty) != 2f) return false;
                foreach (string name in floats)
                {
                    bool numericType = NBShaderRootItem.HasFloatProperty(material,name);
                    // The original Saturability input is compiled as Range.
                    if (!numericType && name == "_Saturability")
                    {
                        int index = material.shader.FindPropertyIndex(name);
                        numericType = index >= 0 && material.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Range;
                    }
                    if (!_rootItem.PropertyInfoDic.ContainsKey(name) || !numericType) return false;
                    float value=material.GetFloat(name); if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                foreach (string name in colors)
                    if (!HasGraphColorLeafPropertyType(material,name,UnityEngine.Rendering.ShaderPropertyType.Color)) return false;
                foreach (string name in vectors)
                    if (!HasGraphColorLeafPropertyType(material,name,UnityEngine.Rendering.ShaderPropertyType.Vector)) return false;
                if (ramp)
                {
                    if (!HasGraphColorLeafPropertyType(material,"_RampColorMap",UnityEngine.Rendering.ShaderPropertyType.Texture)) return false;
                    foreach (string name in new[]{"_RampColorSourceMode","_RampColorBlendMode"})
                    { float value=material.GetFloat(name); if(value!=0f&&value!=1f)return false; }
                    bool changed; if(!CanApplyGraphSavedProjectionForPaint(material,out changed))return false;
                }
            }
            return true;
        }
        bool HasGraphColorLeafPropertyType(Material material,string name,UnityEngine.Rendering.ShaderPropertyType type)
        {
            if (!_rootItem.PropertyInfoDic.ContainsKey(name) || !material.HasProperty(name)) return false;
            int index=material.shader.FindPropertyIndex(name);
            return index>=0 && material.shader.GetPropertyType(index)==type;
        }
        internal bool HasGraphColorAdjustmentEditSchema()
            => HasGraphColorLeafSchema(GraphColorAdjustmentFloats,GraphColorAdjustmentColors,GraphColorAdjustmentVectors,false);
        internal bool HasGraphColorRampEditSchema()
            => HasGraphColorLeafSchema(GraphColorRampFloats,GraphColorRampColors,GraphColorRampVectors,true);
        internal bool TryApplyGraphColorAdjustmentFlagEdit(int bit,int word,bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            bool owned=word==0 && (bit==NBShaderFlags.FLAG_BIT_SATURABILITY_ON || bit==NBShaderFlags.FLAG_BIT_HUESHIFT_ON ||
                bit==NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX || bit==NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) ||
                word==1 && (bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST || bit==NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE);
            if(!owned || !HasGraphColorAdjustmentEditSchema())return false;
            return RunGraphMainTexEdit("Color Adjustment Flag",material=>WriteGraphColorLeafFlag(material,bit,word,enabled));
        }
        bool WriteGraphColorLeafFlag(Material material,int bit,int word,bool enabled)
        {
            string name="_NB_Flags"+word+((bit&65535)!=0?"Lo16":"Hi16");
            int mask=(bit&65535)!=0?bit&65535:(int)((uint)bit>>16);
            bool changed=WriteGraphHalfSlice(material,name,mask,enabled?mask:0);
            if(!changed)NotifyGraphPackedFlagsEdited(material,name,mask);
            return changed;
        }
        internal bool TryApplyGraphColorRampBlendEdit(bool add)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphColorRampEditSchema())return false;
            return RunGraphMainTexEdit("Color Ramp Blend",material=>WriteGraphColorLeafFlag(material,NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD,0,add));
        }
        internal bool TryApplyGraphColorRampIntentEdit()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphColorRampEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed))return false;}
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
        }
        internal bool TryApplyGraphColorRampUVMode(NBShaderFlags.UVMode mode,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphColorRampEditSchema() || !HasGraphMainTexUVEditSchema() || (int)mode<0 || (int)mode>8)return false;
            const int shift=10, mask=3<<shift; // Original word0/type0 position26, high half only.
            return RunGraphMainTexEdit("Color Ramp UV Source",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0Hi16",mask,((int)mode&3)<<shift);
                changed|=WriteGraphHalfSlice(material,"_NB_UVModeFlagType0Hi16",mask,((int)mode/4)<<shift);
                if(setFold)
                {
                    float value=mode==NBShaderFlags.UVMode.DefaultUVChannel||mode==NBShaderFlags.UVMode.CommonUV||mode==NBShaderFlags.UVMode.ScreenUV||mode==NBShaderFlags.UVMode.MainTex?0:1;
                    if(material.GetFloat("_RampColorUVModeFoldOut")!=value){material.SetFloat("_RampColorUVModeFoldOut",value);changed=true;}
                }
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }

        internal static readonly string[] GraphQCMProperties={"_QueueOffset","_QueueControl","_ColorMask","_Stencil","_StencilComp","_StencilOp","_StencilFail","_StencilZFail","_StencilReadMask","_StencilWriteMask","_StencilKeyIndex","_CustomStencilTest","_CustomStencilTestFoldOut"};
        internal bool HasGraphQCMEditSchema()
        {
            if(_rootItem.MatEditor==null||!HasGraphMainTexTargets())return false;
            foreach(Material material in _rootItem.Mats)
            {
                foreach(string name in GraphQCMProperties)
                {
                    int index=material.shader.FindPropertyIndex(name);if(index<0||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                    var type=material.shader.GetPropertyType(index);if(type!=UnityEngine.Rendering.ShaderPropertyType.Float&&type!=UnityEngine.Rendering.ShaderPropertyType.Range)return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                foreach(string name in new[]{"_Surface","_AlphaClip"})
                {if(!material.HasProperty(name))return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;}
            }
            return true;
        }
        internal static int GetGraphQCMBaseQueue(Material material)
            => material.GetFloat("_Surface")>.5f?3000:material.GetFloat("_AlphaClip")>.5f?2450:2000;
        internal bool TryApplyGraphQCMQueue(float bias,bool reset=false)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphQCMEditSchema()||float.IsNaN(bias)||float.IsInfinity(bias)||bias<-5000||bias>5000)return false;
            int rounded=Mathf.RoundToInt(bias);
            foreach(Material material in _rootItem.Mats)
            {int queue=GetGraphQCMBaseQueue(material)+(reset?0:rounded);if(queue<0||queue>5000)return false;}
            return RunGraphMainTexEdit(reset?"Reset NB Queue":"Edit NB Queue",material=>{
                float value=reset?0:rounded,control=reset?0:1;int queue=GetGraphQCMBaseQueue(material)+(int)value;bool changed=false;
                if(material.GetFloat("_QueueOffset")!=value){material.SetFloat("_QueueOffset",value);changed=true;}
                if(material.GetFloat("_QueueControl")!=control){material.SetFloat("_QueueControl",control);changed=true;}
                if(material.rawRenderQueue!=queue){material.renderQueue=queue;changed=true;}
                return changed;
            });
        }
        internal bool TryApplyGraphQCMStencilToggle(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphQCMEditSchema())return false;
            StencilValuesConfig config=null;
            if(!enabled)
            {
                config=GetStencilValuesConfig();if(config==null||!config.ContainsKey("ParticleBaseDefault"))return false;
                var values=config.GetStencilValues("ParticleBaseDefault");if(values==null)return false;
            }
            return RunGraphMainTexEdit(enabled?"Enable NB Stencil":"Reset NB Stencil",material=>{
                if(!enabled)StencilTestHelper.SetMaterialStencil(material,"ParticleBaseDefault",config,out _);
                float value=enabled?1:0;bool changed=material.GetFloat("_CustomStencilTest")!=value;
                if(changed)material.SetFloat("_CustomStencilTest",value);return changed||!enabled;
            });
        }
        internal bool TryGetGraphQCMStencilDisplay(out bool enabled,out bool mixed)
        {
            enabled=mixed=false;if(!_rootItem.CanDisplayGraphQCMStencil)return false;
            var config=GetStencilValuesConfig();if(config==null||!config.ContainsKey("ParticleBaseDefault"))return false;
            var values=config.GetStencilValues("ParticleBaseDefault");if(values==null)return false;
            bool first=true;
            foreach(Material material in _rootItem.Mats)
            {
                bool current=material.GetFloat("_CustomStencilTest")>.5f||material.GetFloat("_Stencil")!=values.Ref||material.GetFloat("_StencilComp")!=(float)values.Comp||material.GetFloat("_StencilOp")!=(float)values.Pass||material.GetFloat("_StencilFail")!=(float)values.Fail||material.GetFloat("_StencilZFail")!=(float)values.ZFail||material.GetFloat("_StencilReadMask")!=values.ReadMask||material.GetFloat("_StencilWriteMask")!=values.WriteMask;
                if(first){enabled=current;first=false;}else mixed|=enabled!=current;
            }
            return true;
        }

        internal static readonly string[] GraphMatCapSharedPropertyNames={"_MatCapToggle","_MatCapTex","_MatCapColor","_MatCapInfo","_MatCapFoldOut"};
        internal bool HasGraphMatCapEditSchema()
        {
            if(!HasGraphMainTexTargets() || _rootItem.MatEditor==null)return false;
            foreach(Material material in _rootItem.Mats)
            {
                bool ignored;if(!CanApplyGraphSavedProjectionForPaint(material,out ignored))return false;
                foreach(string name in new[]{"_MatCapToggle","_MatCapFoldOut","_NB_ForceNoMipFlagsLo16","_NB_ForceNoMipFlagsHi16"})
                {
                    if(!_rootItem.PropertyInfoDic.ContainsKey(name)||!NBShaderRootItem.HasFloatProperty(material,name))return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                foreach(string name in new[]{"_MatCapTex","_MatCapColor","_MatCapInfo"})
                {
                    if(!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                    int index=material.shader.FindPropertyIndex(name);
                    var expected=name=="_MatCapTex"?UnityEngine.Rendering.ShaderPropertyType.Texture:
                        name=="_MatCapColor"?UnityEngine.Rendering.ShaderPropertyType.Color:UnityEngine.Rendering.ShaderPropertyType.Vector;
                    if(index<0||material.shader.GetPropertyType(index)!=expected)return false;
                }
            }
            return true;
        }
        internal bool TryApplyGraphMatCapEdit(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphMatCapEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                SetFloatIfExists(material,"_MatCapToggle",enabled?1f:0f);
                bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed))return false;
            }
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
        }

        internal static readonly string[] GraphDepthSharedPropertyNames={"_DistanceFade_Toggle","_Fade","_DistanceFadeFoldOut",
            "_SoftParticlesEnabled","_SoftParticleFadeParams","_SoftParticlesFoldOut",
            "_DepthOutline_Toggle","_DepthOutline_Color","_DepthOutline_Vec","_DepthOutlineBlockFoldOut"};
        internal bool HasGraphDepthFeaturesEditSchema()
        {
            if(!HasGraphMainTexTargets()||_rootItem.MatEditor==null)return false;
            foreach(Material material in _rootItem.Mats)
            {
                bool ignored;if(!CanApplyGraphSavedProjectionForPaint(material,out ignored))return false;
                foreach(string name in new[]{"_DistanceFade_Toggle","_SoftParticlesEnabled","_DepthOutline_Toggle","_DistanceFadeFoldOut","_SoftParticlesFoldOut","_DepthOutlineBlockFoldOut"})
                {
                    if(!_rootItem.PropertyInfoDic.ContainsKey(name)||!NBShaderRootItem.HasFloatProperty(material,name))return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                foreach(string name in new[]{"_Fade","_SoftParticleFadeParams","_DepthOutline_Vec","_DepthOutline_Color"})
                {
                    if(!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                    int index=material.shader.FindPropertyIndex(name);
                    var expected=name=="_DepthOutline_Color"?UnityEngine.Rendering.ShaderPropertyType.Color:UnityEngine.Rendering.ShaderPropertyType.Vector;
                    if(index<0||material.shader.GetPropertyType(index)!=expected)return false;
                }
            }
            return true;
        }
        internal bool TryApplyGraphDepthFeaturesIntentEdit()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphDepthFeaturesEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed))return false;}
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
        }
        internal static readonly string[] GraphSharedUVPropertyNames={"_SharedUVToggle","_SharedUVBlockFoldOut","_SharedUVModeFoldOut","_SharedUV_ST","_SharedUV_Vec"};
        internal bool HasGraphSharedUVEditSchema()
        {
            if(!HasGraphMainTexUVEditSchema()||_rootItem.MatEditor==null)return false;
            foreach(Material material in _rootItem.Mats)
            {
                if(!GraphFlagIntentSchemaAvailable(material)||material.GetFloat(GraphGUIStateVersionProperty)!=2f)return false;
                foreach(string name in new[]{"_SharedUVToggle","_SharedUVBlockFoldOut","_SharedUVModeFoldOut","_NB_CustomDataFlag3Lo16","_NB_CustomDataFlag3Hi16"})
                {
                    if(!NBShaderRootItem.HasFloatProperty(material,name)||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
                foreach(string name in new[]{"_SharedUV_ST","_SharedUV_Vec"})
                {
                    int index=material.shader.FindPropertyIndex(name);
                    if(index<0||material.shader.GetPropertyType(index)!=UnityEngine.Rendering.ShaderPropertyType.Vector||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                    Vector4 value=material.GetVector(name);for(int i=0;i<4;++i)if(float.IsNaN(value[i])||float.IsInfinity(value[i]))return false;
                }
            }
            return true;
        }
        internal bool TryApplyGraphSharedUVMode(NBShaderFlags.UVMode mode,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphSharedUVEditSchema()||(int)mode<0||(int)mode>8)return false;
            const int shift=14,mask=3<<shift; // Original word0 position30 -> high-half bits14/15.
            return RunGraphMainTexEdit("Shared UV Source",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0Hi16",mask,((int)mode&3)<<shift)|
                    WriteGraphHalfSlice(material,"_NB_UVModeFlagType0Hi16",mask,((int)mode/4)<<shift);
                if(setFold)
                {
                    float value=mode==NBShaderFlags.UVMode.DefaultUVChannel||mode==NBShaderFlags.UVMode.CommonUV||mode==NBShaderFlags.UVMode.ScreenUV||mode==NBShaderFlags.UVMode.MainTex?0:1;
                    if(material.GetFloat("_SharedUVModeFoldOut")!=value){material.SetFloat("_SharedUVModeFoldOut",value);changed=true;}
                }
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }
        internal bool TryApplyGraphSharedUVCustomData(int position,int word,NBShaderFlags.CutomDataComponent component)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphSharedUVEditSchema()||word!=3||(position!=8&&position!=12)||(int)component<0||(int)component>8)return false;
            int[] values={0,NBShaderFlags.CustomData1XBit,NBShaderFlags.CustomData1YBit,NBShaderFlags.CustomData1ZBit,NBShaderFlags.CustomData1WBit,NBShaderFlags.CustomData2XBit,NBShaderFlags.CustomData2YBit,NBShaderFlags.CustomData2ZBit,NBShaderFlags.CustomData2WBit};
            return RunGraphMainTexEdit("Shared UV Offset Custom Data",material=>WriteGraphHalfSlice(material,"_NB_CustomDataFlag3Lo16",15<<position,values[(int)component]<<position));
        }


        internal static readonly string[] GraphVertexOffsetSharedProperties={"_IgnoreVetexColor_Toggle","_NB_Debug_VertexOffset","_VertexOffsetBlockFoldOut","_VertexOffsetMaskBlockFoldOut","_VertexOffsetMaskUVModeFoldOut","_VertexOffsetUVModeFoldOut","_VertexOffset_CustomDir","_VertexOffset_DirectionSpace","_VertexOffset_Map","_VertexOffset_MaskMap","_VertexOffset_MaskMap_Vec","_VertexOffset_Mask_Toggle","_VertexOffset_NormalDir_Toggle","_VertexOffset_StartFromZero","_VertexOffset_Toggle","_VertexOffset_Vec"};

        internal bool HasGraphVertexOffsetEditSchema()
        {
            if(_rootItem.MatEditor==null||!HasGraphMainTexUVEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                foreach(string name in GraphVertexOffsetSharedProperties)if(!material.HasProperty(name)||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                foreach(string name in new[]{"_NB_CustomDataFlag1Lo16","_NB_CustomDataFlag1Hi16","_NB_CustomDataFlag3Lo16","_NB_CustomDataFlag3Hi16"})
                {if(!NBShaderRootItem.HasFloatProperty(material,name)||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;}
                foreach(string name in new[]{"_VertexOffset_Toggle","_VertexOffset_Mask_Toggle","_VertexOffset_NormalDir_Toggle","_VertexOffset_DirectionSpace","_VertexOffset_StartFromZero",GraphGUIStateVersionProperty})
                {if(!NBShaderRootItem.HasFloatProperty(material,name))return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;}
                float direction=material.GetFloat("_VertexOffset_NormalDir_Toggle"),space=material.GetFloat("_VertexOffset_DirectionSpace");if(direction!=Mathf.Round(direction)||direction<0||direction>3||space!=Mathf.Round(space)||space<0||space>1||material.GetFloat(GraphGUIStateVersionProperty)!=2)return false;
                bool changed;if(!CanApplyGraphSavedProjectionForPaint(material,out changed))return false;
            }
            return true;
        }
        internal bool TryApplyGraphVertexOffsetIntentEdit()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphVertexOffsetEditSchema())return false;
            foreach(Material material in _rootItem.Mats){bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed))return false;}
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
        }
        internal bool TryApplyGraphVertexOffsetStart(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphVertexOffsetEditSchema())return false;const string name="_NB_Flags1Hi16";const int mask=1<<(25-16);
            return RunGraphMainTexEdit("Vertex Offset From Zero",material=>{bool changed=WriteGraphHalfSlice(material,name,mask,enabled?mask:0);if(!changed)NotifyGraphPackedFlagsEdited(material,name,mask);return changed;});
        }
        internal bool TryRepairGraphVertexOffsetColor()
        {
            if(!HasGraphVertexOffsetEditSchema())return false;
            return RunGraphMainTexEdit("Vertex Offset Ignore Vertex Color",material=>{
                if(material.GetFloat("_VertexOffset_Toggle")<=.5f||material.GetFloat("_VertexOffset_NormalDir_Toggle")!=2)return false;
                const string name="_NB_Flags1Lo16";const int mask=1<<9;bool changed=WriteGraphHalfSlice(material,name,mask,mask);if(!changed)NotifyGraphPackedFlagsEdited(material,name,mask);return changed;
            });
        }
        internal bool TryApplyGraphVertexOffsetUV(int position,NBShaderFlags.UVMode mode,string fold,bool setFold)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphVertexOffsetEditSchema()||(int)mode<0||(int)mode>8||!(position==20&&fold=="_VertexOffsetUVModeFoldOut"||position==22&&fold=="_VertexOffsetMaskUVModeFoldOut"))return false;
            int shift=position-16,mask=3<<shift;
            return RunGraphMainTexEdit("Vertex Offset UV",material=>{
                bool changed=WriteGraphHalfSlice(material,"_NB_UVModeFlag0Hi16",mask,((int)mode&3)<<shift)|WriteGraphHalfSlice(material,"_NB_UVModeFlagType0Hi16",mask,((int)mode/4)<<shift);
                if(setFold){float value=mode==NBShaderFlags.UVMode.DefaultUVChannel||mode==NBShaderFlags.UVMode.CommonUV||mode==NBShaderFlags.UVMode.ScreenUV||mode==NBShaderFlags.UVMode.MainTex?0:1;if(material.GetFloat(fold)!=value){material.SetFloat(fold,value);changed=true;}}
                return UpdateGraphMainTexUVDerived(material)|changed;
            });
        }
        internal bool TryApplyGraphVertexOffsetCustomData(int position,int word,NBShaderFlags.CutomDataComponent component)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphVertexOffsetEditSchema()||(int)component<0||(int)component>8||!(word==1&&(position==16||position==20||position==24)||word==3&&(position==0||position==4)))return false;
            int[] values={0,NBShaderFlags.CustomData1XBit,NBShaderFlags.CustomData1YBit,NBShaderFlags.CustomData1ZBit,NBShaderFlags.CustomData1WBit,NBShaderFlags.CustomData2XBit,NBShaderFlags.CustomData2YBit,NBShaderFlags.CustomData2ZBit,NBShaderFlags.CustomData2WBit};int shift=position&15;string name="_NB_CustomDataFlag"+word+(position<16?"Lo16":"Hi16");
            return RunGraphMainTexEdit("Vertex Offset Custom Data",material=>WriteGraphHalfSlice(material,name,15<<shift,values[(int)component]<<shift));
        }
        internal bool HasGraphStencilWithoutPlayerEditSchema()
        {
            if(!HasGraphQCMEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                if(!NBShaderRootItem.HasFloatProperty(material,"_StencilWithoutPlayerToggle")||!_rootItem.PropertyInfoDic.ContainsKey("_StencilWithoutPlayerToggle")||
                    !NBShaderRootItem.HasFloatProperty(material,GraphGUIStateVersionProperty)||material.GetFloat(GraphGUIStateVersionProperty)!=2f)return false;
                float value=material.GetFloat("_StencilWithoutPlayerToggle");if(float.IsNaN(value)||float.IsInfinity(value))return false;
            }
            return true;
        }
        internal bool TryApplyGraphStencilWithoutPlayer(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphStencilWithoutPlayerEditSchema())return false;
            string key=enabled?"ParticleWithoutPlayer":"ParticleBaseDefault";var config=GetStencilValuesConfig();
            if(config==null||!config.ContainsKey(key)||config.GetStencilValues(key)==null)return false;
            return RunGraphMainTexEdit(enabled?"NB Stencil Without Player":"Reset NB Stencil Without Player",material=>{
                // The same original helper returns its default queue; this transaction keeps the saved queue.
                StencilTestHelper.SetMaterialStencil(material,key,config,out _);
                material.SetFloat("_CustomStencilTest",enabled?1:0);material.SetFloat("_StencilWithoutPlayerToggle",enabled?1:0);
                return true;
            });
        }


        // The original Native linked-state contract, shared without queue writes.
        private void ApplyDepthDecalLinkedState(Material mat, bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            ApplyStencilPresetToMaterial(mat, enabled ? "ParticleBaseDecal" : "ParticleBaseDefault");
            SetFloatIfExists(mat, "_CustomStencilTest", enabled ? 1f : 0f);
            SetFloatIfExists(mat, "_Cull", enabled ? (float)RenderFace.Back : (float)RenderFace.Front);
            SetFloatIfExists(mat, "_ZTest", enabled
                ? (float)UnityEngine.Rendering.CompareFunction.GreaterEqual
                : (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        }

        internal bool HasGraphDepthDecalEditSchema()
        {
            if(!HasGraphQCMEditSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                bool ignored;if(!CanApplyGraphSavedProjectionForPaint(material,out ignored))return false;
                if(!NBShaderRootItem.HasFloatProperty(material,"_DepthDecal_Toggle")||!_rootItem.PropertyInfoDic.ContainsKey("_DepthDecal_Toggle"))return false;
                float toggle=material.GetFloat("_DepthDecal_Toggle");if(float.IsNaN(toggle)||float.IsInfinity(toggle))return false;
                foreach(string name in new[]{"_Cull","_ZTest"})
                {
                    int index=material.shader.FindPropertyIndex(name);if(index<0||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
                    var type=material.shader.GetPropertyType(index);if(type!=UnityEngine.Rendering.ShaderPropertyType.Float&&type!=UnityEngine.Rendering.ShaderPropertyType.Range)return false;
                    float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
                }
            }
            return true;
        }
        internal bool TryApplyGraphDepthDecalEnabled(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphDepthDecalEditSchema())return false;
            string key=enabled?"ParticleBaseDecal":"ParticleBaseDefault";
            var config=GetStencilValuesConfig();
            // A missing config keeps the original fallback. A malformed existing
            // preset declines the Graph transaction before any linked-state write.
            if(config!=null&&(!config.ContainsKey(key)||config.GetStencilValues(key)==null))return false;
            var probes=new List<Material>();var effective=new List<float>();
            try
            {
                foreach(Material material in _rootItem.Mats)
                {
                    var probe=new Material(material){hideFlags=HideFlags.HideAndDontSave};probes.Add(probe);
                    probe.SetFloat("_DepthDecal_Toggle",enabled?1:0);
                    var tier=(NBShaderFeatureTier)(int)material.GetFloat(FeatureTierPropertyName);bool changed;
                    if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphDepthDecalGroup(probe,tier,null,out changed))return false;
                    effective.Add(probe.GetFloat("_NB_TierAllowDepthDecal"));
                }
                // The only explicit-edit writer owns the original preset/Cull/ZTest.
                // Tier filtering never invokes this method or restores a preset.
                Undo.RecordObjects(_rootItem.MatEditor.targets,"Edit NB Depth Decal");
                for(int i=0;i<_rootItem.Mats.Count;++i)
                {
                    Material material=_rootItem.Mats[i];SetFloatIfExists(material,"_DepthDecal_Toggle",enabled?1:0);
                    ApplyDepthDecalLinkedState(material,enabled);
                    SetFloatIfExists(material,"_NB_TierAllowDepthDecal",effective[i]);EditorUtility.SetDirty(material);
                }
                RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
            }
            finally{foreach(Material probe in probes)UnityEngine.Object.DestroyImmediate(probe);}
        }
        // Only an explicitly migrated, compiled modern route owns this back pass.
        // Legacy SRPDefaultUnlit remains the main color pass and is never adopted.
        internal static bool CanApplyGraphOwnedBackFirstPassState(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPasses, out NBShaderPassIntent intent)
        {
            return NBShaderFeatureRuntime.CanApplyGraphOwnedBackFirstPassState(material, tier, allowedKeywords, allowedPasses, out intent);
        }

        internal static bool ApplyGraphOwnedBackFirstPassState(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPasses, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphOwnedBackFirstPassState(material, tier, allowedKeywords, allowedPasses, out changed);
        }

        internal static bool TryReadGraphSavedOwnedBackFirstPassIntent(Material material, out NBShaderPassIntent intent)
        {
            intent = null;
            if (!NBShaderRootItem.HasFloatProperty(material, FeatureTierPropertyName)) return false;
            float saved = material.GetFloat(FeatureTierPropertyName);
            if (float.IsNaN(saved) || float.IsInfinity(saved) || saved < 0 || saved > 3 || saved != Mathf.Round(saved)) return false;
            var tier = (NBShaderFeatureTier)(int)saved;
            return CanApplyGraphOwnedBackFirstPassState(material, tier,
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier), out intent) && intent != null;
        }

        internal static bool ApplyGraphSavedOwnedBackFirstPassState(Material material, out bool changed)
        {
            changed = false;
            NBShaderPassIntent intent;
            if (!TryReadGraphSavedOwnedBackFirstPassIntent(material, out intent)) return false;
            var tier = (NBShaderFeatureTier)(int)material.GetFloat(FeatureTierPropertyName);
            return ApplyGraphOwnedBackFirstPassState(material, tier,
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier), out changed);
        }

        private sealed class GraphBackFirstPassSnapshot
        {
            readonly Material material;
            readonly bool owned, enabled;
            readonly float effective;
            internal GraphBackFirstPassSnapshot(Material value)
            {
                material = value;
                int route;
                owned = NBShaderPassFeatureCatalog.TryGetGraphColorRouting(value, out route) && route == 1 &&
                    value.HasProperty("_NB_GraphPassMigrationComplete") && value.GetFloat("_NB_GraphPassMigrationComplete") == 1f;
                if (!owned) return;
                enabled = value.GetShaderPassEnabled("SRPDefaultUnlit");
                effective = value.GetFloat("_NB_BackFirstEffective");
            }
            internal void Restore()
            {
                if (!owned) return;
                material.SetShaderPassEnabled("SRPDefaultUnlit", enabled);
                material.SetFloat("_NB_BackFirstEffective", effective);
            }
        }

        internal bool HasGraphBackFirstUIHost()
        {
            if (_rootItem.MatEditor == null || !HasGraphMainTexTargets()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                int route;
                if (!NBShaderPassFeatureCatalog.TryGetGraphColorRouting(material, out route) || route != 1 ||
                    !NBShaderRootItem.HasFloatProperty(material, "_BackFirstPassToggle") ||
                    !_rootItem.PropertyInfoDic.ContainsKey("_BackFirstPassToggle")) return false;
            }
            return true;
        }

        internal bool HasGraphBackFirstAdoptionSchema()
        {
            if (!HasGraphBackFirstUIHost()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                if (!NBShaderRootItem.HasFloatProperty(material, "_NB_GraphPassMigrationComplete") ||
                    !_rootItem.PropertyInfoDic.ContainsKey("_NB_GraphPassMigrationComplete") ||
                    material.GetFloat("_NB_GraphPassMigrationComplete") != 0f ||
                    !NBShaderRootItem.HasFloatProperty(material, "_NB_BackFirstEffective") ||
                    !NBShaderRootItem.HasFloatProperty(material, "_Cull") ||
                    !_rootItem.PropertyInfoDic.ContainsKey("_Cull")) return false;
                float effective = material.GetFloat("_NB_BackFirstEffective"), cull = material.GetFloat("_Cull");
                if (float.IsNaN(effective) || float.IsInfinity(effective) || float.IsNaN(cull) || float.IsInfinity(cull) ||
                    cull < 0 || cull > 2 || cull != Mathf.Round(cull)) return false;
                var probe = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    // Explicitly adopt this modern material's CURRENT main state.
                    // This does not recover or guess an earlier Legacy main state.
                    bool currentMain = material.GetShaderPassEnabled("UniversalForward");
                    NBShaderPassIntent intent;
                    if (!TryCommitGraphColorPassState(probe, currentMain) ||
                        !TryReadGraphSavedOwnedBackFirstPassIntent(probe, out intent)) return false;
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }
            return true;
        }

        internal bool TryAdoptGraphBackFirstCurrentMain()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphBackFirstAdoptionSchema()) return false;
            var materials = _rootItem.Mats;
            var targets = new UnityEngine.Object[materials.Count];
            var main = new bool[materials.Count]; var back = new bool[materials.Count];
            var effective = new float[materials.Count]; var migration = new float[materials.Count];
            for (int i = 0; i < materials.Count; ++i)
            {
                targets[i] = materials[i]; main[i] = materials[i].GetShaderPassEnabled("UniversalForward");
                back[i] = materials[i].GetShaderPassEnabled("SRPDefaultUnlit");
                effective[i] = materials[i].GetFloat("_NB_BackFirstEffective");
                migration[i] = materials[i].GetFloat("_NB_GraphPassMigrationComplete");
            }
            Undo.RecordObjects(targets, "Adopt current NB Modern main state");
            for (int i = 0; i < materials.Count; ++i)
            {
                bool changed;
                if (TryCommitGraphColorPassState(materials[i], main[i]) &&
                    ApplyGraphSavedOwnedBackFirstPassState(materials[i], out changed)) continue;
                for (int restore = 0; restore < materials.Count; ++restore)
                {
                    materials[restore].SetShaderPassEnabled("UniversalForward", main[restore]);
                    materials[restore].SetShaderPassEnabled("SRPDefaultUnlit", back[restore]);
                    materials[restore].SetFloat("_NB_BackFirstEffective", effective[restore]);
                    materials[restore].SetFloat("_NB_GraphPassMigrationComplete", migration[restore]);
                }
                return false;
            }
            foreach (Material material in materials) EditorUtility.SetDirty(material);
            RefreshGraphMainTexPropertyReferences(); _rootItem.Context?.Refresh();
            return true;
        }

        internal bool HasGraphBackFirstEditSchema()
        {
            if (!HasGraphBackFirstUIHost()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                NBShaderPassIntent intent;
                if (!TryReadGraphSavedOwnedBackFirstPassIntent(material, out intent) ||
                    !NBShaderRootItem.HasFloatProperty(material, "_Cull") || !_rootItem.PropertyInfoDic.ContainsKey("_Cull")) return false;
                float cull = material.GetFloat("_Cull");
                if (float.IsNaN(cull) || float.IsInfinity(cull) || cull < 0 || cull > 2 || cull != Mathf.Round(cull)) return false;
            }
            return true;
        }

        internal bool TryApplyGraphBackFirstToggle(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphBackFirstEditSchema()) return false;
            // Preflight selected intent on copies before writing any target.
            foreach (Material material in _rootItem.Mats)
            {
                var probe = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    probe.SetFloat("_BackFirstPassToggle", enabled ? 1f : 0f);
                    NBShaderPassIntent intent;
                    if (!TryReadGraphSavedOwnedBackFirstPassIntent(probe, out intent)) return false;
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }
            return RunGraphMainTexEdit("Edit NB Back First", material =>
            {
                bool changed = false;
                float value = enabled ? 1f : 0f;
                if (material.GetFloat("_BackFirstPassToggle") != value) { material.SetFloat("_BackFirstPassToggle", value); changed = true; }
                // Match the original Native control: enabling leaves the main front-facing.
                if (enabled && material.GetFloat("_Cull") != (float)RenderFace.Front)
                { material.SetFloat("_Cull", (float)RenderFace.Front); changed = true; }
                bool passChanged;
                ApplyGraphSavedOwnedBackFirstPassState(material, out passChanged);
                return changed || passChanged;
            });
        }

        internal static readonly string[] GraphVATSharedPropertyNames = {"_VATBlockFoldOut","_VAT_Toggle","_VATMode","_HoudiniVATSubMode","_B_autoPlayback","_displayFrame","_gameTimeAtFirstFrame","_playbackSpeed","_houdiniFPS","_B_interpolate","_animateFirstFrame","_frameCount","_boundMinX","_boundMinY","_boundMinZ","_boundMaxX","_boundMaxY","_boundMaxZ","_posTexture","_posTexture2","_B_LOAD_POS_TWO_TEX","_rotTexture","_colTexture","_B_LOAD_COL_TEX","_lookupTable","_B_LOAD_LOOKUP_TABLE","_globalPscaleMul","_B_pscaleAreInPosA","_widthBaseScale","_heightBaseScale","_B_hideOverlappingOrigin","_originRadius","_B_CAN_SPIN","_B_spinFromHeading","_spinPhase","_scaleByVelAmount","_particleTexUScale","_particleTexVScale","_B_UNLOAD_ROT_TEX","_VATTex","_ImportScale","_TyFlowVATSubMode","_DeformingSkin","_SkinBoneCount","_RGBAEncoded","_RGBAHalf","_LinearToGamma","_VATIncludesNormals","_Frame","_Frames","_FrameInterpolation","_Loop","_InterpolateLoop","_Autoplay","_AutoplaySpeed"};
        internal bool HasGraphVATEditSchema()
        {
            if(_rootItem.MatEditor==null||!HasGraphMainTexTargets())return false;
            foreach(Material material in _rootItem.Mats)
            {
                bool changed;if(!CanApplyGraphSavedProjectionForPaint(material,out changed))return false;
                foreach(string name in GraphVATSharedPropertyNames)
                {
                    if(!_rootItem.PropertyInfoDic.ContainsKey(name)||!material.HasProperty(name))return false;
                    int index=material.shader.FindPropertyIndex(name);var type=material.shader.GetPropertyType(index);
                    bool texture=name=="_posTexture"||name=="_posTexture2"||name=="_rotTexture"||name=="_colTexture"||name=="_lookupTable"||name=="_VATTex";
                    if(texture){if(type!=UnityEngine.Rendering.ShaderPropertyType.Texture)return false;}
                    else {if(type!=UnityEngine.Rendering.ShaderPropertyType.Float)return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;}
                }
                foreach(string name in new[]{"_NB_CustomDataFlag2Lo16","_NB_CustomDataFlag2Hi16","_FlipbookBlending"})
                {if(!NBShaderRootItem.HasFloatProperty(material,name)||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;}
            }
            return true;
        }
        bool RunGraphVATTransaction(string label,Action<Material> edit,Action reset=null)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphVATEditSchema())return false;
            var snapshots=new List<Material>();var targets=new List<UnityEngine.Object>();
            try
            {
                // Probe every proposed scalar/CD change before first actual write.
                foreach(Material material in _rootItem.Mats)
                {
                    var copy=new Material(material){hideFlags=HideFlags.HideAndDontSave};snapshots.Add(copy);targets.Add(material);
                    if(edit!=null)
                    {
                        var probe=new Material(material){hideFlags=HideFlags.HideAndDontSave};
                        try{edit(probe);bool change;if(!NBShaderFeatureLevelMaterialApplier.CanApplyGraphSavedSupportedGateTier(probe,out change))return false;}
                        finally{UnityEngine.Object.DestroyImmediate(probe);}
                    }
                }
                Undo.RecordObjects(targets.ToArray(),label);
                try
                {
                    if(reset!=null)reset();else foreach(Material material in _rootItem.Mats)edit(material);
                    foreach(Material material in _rootItem.Mats)
                    {bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material,out changed)){RestoreGraphVATTransaction(snapshots);return false;}EditorUtility.SetDirty(material);}
                    RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();return true;
                }
                catch {RestoreGraphVATTransaction(snapshots);throw;}
            }
            finally{foreach(Material material in snapshots)UnityEngine.Object.DestroyImmediate(material);}
        }
        void RestoreGraphVATTransaction(List<Material> snapshots)
        {
            for(int i=0;i<snapshots.Count;++i)
            {
                Material value=_rootItem.Mats[i],old=snapshots[i];
                foreach(string name in GraphVATSharedPropertyNames)
                {
                    int index=value.shader.FindPropertyIndex(name);
                    if(value.shader.GetPropertyType(index)==UnityEngine.Rendering.ShaderPropertyType.Texture)
                    {value.SetTexture(name,old.GetTexture(name));value.SetTextureScale(name,old.GetTextureScale(name));value.SetTextureOffset(name,old.GetTextureOffset(name));}
                    else value.SetFloat(name,old.GetFloat(name));
                }
                foreach(string name in NBShaderFeatureLevelMaterialApplier.GraphSupportedProjectionProperties)value.SetFloat(name,old.GetFloat(name));
                value.SetFloat("_NB_CustomDataFlag2Hi16",old.GetFloat("_NB_CustomDataFlag2Hi16"));value.SetFloat("_FlipbookBlending",old.GetFloat("_FlipbookBlending"));
                RestoreGraphOverrideDepthKeyword(value,old.IsKeywordEnabled("_OVERRIDE_Z"));
                NBShaderFeatureLevelMaterialApplier.RestoreGraphDeclaredKeywordState(value,NBShaderFeatureLevelMaterialApplier.CaptureGraphDeclaredKeywordState(old));
            }
            RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();
        }
        internal bool TryApplyGraphVATToggle(bool enabled)
            =>RunGraphVATTransaction("NB VAT",material=>{material.SetFloat("_VAT_Toggle",enabled?1:0);if(enabled)material.SetFloat("_FlipbookBlending",0);});
        internal bool TryApplyGraphVATIntentEdit()
            =>RunGraphVATTransaction("NB VAT Mode",material=>{});
        internal bool TryRunGraphVATReset(Action reset)
            =>reset!=null&&RunGraphVATTransaction("Reset NB VAT",null,reset);
        internal bool TryApplyGraphVATFrameCustomData(NBShaderFlags.CutomDataComponent component)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphVATEditSchema()||(int)component<0||(int)component>8)return false;
            int[] values={0,NBShaderFlags.CustomData1XBit,NBShaderFlags.CustomData1YBit,NBShaderFlags.CustomData1ZBit,NBShaderFlags.CustomData1WBit,NBShaderFlags.CustomData2XBit,NBShaderFlags.CustomData2YBit,NBShaderFlags.CustomData2ZBit,NBShaderFlags.CustomData2WBit};
            return RunGraphMainTexEdit("VAT Frame Custom Data",material=>WriteGraphHalfSlice(material,"_NB_CustomDataFlag2Hi16",15<<12,values[(int)component]<<12));
        }

        internal static readonly string[] GraphPortalSharedProperties = { "_PortalBlockFoldOut", "_Portal_Toggle", "_Portal_MaskToggle" };
        internal bool HasGraphPortalEditSchema()
        {
            if (_rootItem.MatEditor == null || !HasGraphMainTexTargets()) return false;
            var config = GetStencilValuesConfig();
            if (config == null) return false;
            foreach (string key in new[] { "ParticleBaseDefault", "ParticalBasePortal", "ParticalBasePortalMask" })
            {
                if (!config.ContainsKey(key)) return false;
                var value = config.GetStencilValues(key);
                if (value == null || value.Ref < 0 || value.Ref > 255 || value.ReadMask < 0 || value.ReadMask > 255 ||
                    value.WriteMask < 0 || value.WriteMask > 255 || (int)value.Comp < 0 || (int)value.Comp > 8 ||
                    (int)value.Pass < 0 || (int)value.Pass > 7 || (int)value.Fail < 0 || (int)value.Fail > 7 ||
                    (int)value.ZFail < 0 || (int)value.ZFail > 7) return false;
            }
            foreach (Material material in _rootItem.Mats)
            {
                bool ignored;
                if (!CanApplyGraphSavedProjectionForPaint(material, out ignored)) return false;
                if(material.HasProperty("_TransparentShadowDitherToggle")&&!HasPortalFiniteFloat(material,"_TransparentShadowDitherToggle"))return false;
                foreach (string n in GraphPortalSharedProperties)
                    if (!HasPortalFiniteFloat(material, n)) return false;
                foreach (string n in new[] { "_Surface", "_AlphaClip", "_Blend", "_ZWriteControl", "_ZTest", "_QueueControl", "_QueueOffset",
                    "_CustomStencilTest", "_Stencil", "_StencilComp", "_StencilOp", "_StencilFail", "_StencilZFail", "_StencilReadMask", "_StencilWriteMask", "_StencilKeyIndex", "_NB_Flags1Lo16", "_NB_Flags1Hi16" })
                    if (!HasPortalFiniteFloat(material, n)) return false;
                if (!PortalEnum(material, "_Surface", 0, 1) || !PortalEnum(material, "_Blend", 0, 3) ||
                    !PortalEnum(material, "_ZWriteControl", 0, 2) || !PortalEnum(material, "_ZTest", 0, 8) ||
                    !PortalEnum(material, "_QueueControl", -1, 1)) return false;
                if (material.rawRenderQueue < -1 || material.rawRenderQueue > 5000) return false;
                float offset = material.GetFloat("_QueueOffset");
                if (offset < -5000 || offset > 5000 || offset != Mathf.Round(offset)) return false;
                if (material.HasProperty("_MeshSourceMode") && !PortalEnum(material, "_MeshSourceMode", 0, 1)) return false;
            }
            return true;
        }

        private bool HasPortalFiniteFloat(Material material, string name)
        {
            if (!NBShaderRootItem.HasFloatProperty(material, name) || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
            float value = material.GetFloat(name);
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
        private static bool PortalEnum(Material material, string name, int minimum, int maximum)
        {
            float value = material.GetFloat(name);
            return value >= minimum && value <= maximum && value == Mathf.Round(value);
        }

        private void ApplyGraphPortalPreset(Material material)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            bool portal = material.GetFloat("_Portal_Toggle") > .5f;
            bool mask = material.GetFloat("_Portal_MaskToggle") > .5f;
            bool wasTransparent = material.GetFloat("_Surface") == 1f;
            int rawQueue = material.rawRenderQueue;
            float queueOffset = material.GetFloat("_QueueOffset");
            string key = !portal ? "ParticleBaseDefault" : mask ? "ParticalBasePortalMask" : "ParticalBasePortal";
            // The original helper returns DefaultQueue, but Portal ignores it.
            StencilTestHelper.SetMaterialStencil(material, key, GetStencilValuesConfig(), out _);
            material.SetFloat("_CustomStencilTest", portal ? 1f : 0f);
            if (!portal)
            {
                material.SetFloat("_Surface", 1f); material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                material.SetFloat("_ZWriteControl", 0f);
                if (!wasTransparent) material.SetFloat("_Blend", 0f);
            }
            else if (mask)
            {
                if (wasTransparent) { material.SetFloat("_Surface", 0f); material.SetFloat("_AlphaClip", 1f); }
                material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                material.SetFloat("_ZWriteControl", 2f);
            }
            if (material.GetFloat("_Surface") == 0f) material.SetFloat("_Blend", 0f);
            // Existing Native surface sync owns only these two coverage bits.
            // A mask clears dither; a later Off never revives that cleared bit.
            int coverage = material.GetFloat("_Surface") == 1f ? 1 | (ReadGraphHalf(material, "_NB_Flags1Lo16") & 2) : 0;
            if (WriteGraphHalfSlice(material, "_NB_Flags1Lo16", 3, coverage))
                NotifyGraphPackedFlagsEdited(material, "_NB_Flags1Lo16", 3);
            if(material.HasProperty("_TransparentShadowDitherToggle")&&(coverage&2)==0)material.SetFloat("_TransparentShadowDitherToggle",0f);
            // This explicit preset holds CURRENT queue, matching Native Portal.
            // Official Advanced remains available for a subsequent Auto choice.
            material.SetFloat("_QueueControl", 1f);
            new NBShaderGraphGUI().ValidateMaterial(material);
            material.SetFloat("_QueueOffset", queueOffset);
            if (material.rawRenderQueue != rawQueue) material.renderQueue = rawQueue;
        }

        internal bool TryApplyGraphPortalState()
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if (!HasGraphPortalEditSchema()) return false;
            // Validate the complete projected state on copies before any target write.
            foreach (Material material in _rootItem.Mats)
            {
                var probe = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    ApplyGraphPortalPreset(probe);
                    NBShader.NBShaderPassIntent back; NBShader.NBShaderPassIntent[] screen;
                    var tier = (NBShaderFeatureTier)(int)probe.GetFloat(FeatureTierPropertyName);
                    var keywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
                    var passes = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);
                    if (!CanApplyGraphOwnedBackFirstPassState(probe, tier, keywords, passes, out back) ||
                        !CanApplyGraphOwnedScreenPassState(probe, tier, keywords, passes, out screen)) return false;
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }
            return RunGraphMainTexEdit("Apply NB Portal preset (keep current queue)", material =>
            {
                string before = EditorJsonUtility.ToJson(material);
                ApplyGraphPortalPreset(material);
                return EditorJsonUtility.ToJson(material) != before;
            });
        }

        internal bool TryApplyGraphPortalToggle(string name, bool enabled, bool resetBoth = false)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if ((name != "_Portal_Toggle" && name != "_Portal_MaskToggle") || !HasGraphPortalEditSchema()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                var probe = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    // Before ANY intent setter can notify MaterialEditor, hold
                    // the current queue using the original official control.
                    probe.SetFloat("_QueueControl", 1f);
                    probe.SetFloat(name, enabled ? 1f : 0f);
                    if (resetBoth) probe.SetFloat("_Portal_MaskToggle", 0f);
                    ApplyGraphPortalPreset(probe);
                    NBShaderPassIntent back; NBShaderPassIntent[] screen;
                    var tier = (NBShaderFeatureTier)(int)probe.GetFloat(FeatureTierPropertyName);
                    var keywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
                    var passes = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);
                    if (!CanApplyGraphOwnedBackFirstPassState(probe, tier, keywords, passes, out back) ||
                        !CanApplyGraphOwnedScreenPassState(probe, tier, keywords, passes, out screen)) return false;
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }
            return RunGraphMainTexEdit("Edit NB Portal preset (keep current queue)", material =>
            {
                int rawQueue = material.rawRenderQueue;
                float offset = material.GetFloat("_QueueOffset");
                string before = EditorJsonUtility.ToJson(material);
                material.SetFloat("_QueueControl", 1f);
                material.SetFloat(name, enabled ? 1f : 0f);
                if (resetBoth) material.SetFloat("_Portal_MaskToggle", 0f);
                ApplyGraphPortalPreset(material);
                material.SetFloat("_QueueOffset", offset);
                if (material.rawRenderQueue != rawQueue) material.renderQueue = rawQueue;
                return EditorJsonUtility.ToJson(material) != before;
            });
        }


        internal static readonly string[] GraphLightSubSharedProperties = {
            "_BlinnPhongSpecularToggle", "_SpecularColor", "_MaterialInfo", "_RigRTBk", "_RigLBtF",
            "_SixWayColorAbsorptionToggle", "_SixWayInfo", "_SixWayEmissionRamp", "_SixWayEmissionColor"
        };
        internal bool HasGraphLightSubControlsSchema()
        {
            if (!_rootItem.IsGraphLightModeSchemaReady()) return false;
            foreach (Material value in _rootItem.Mats)
            {
                foreach (string name in GraphLightSubSharedProperties)
                {
                    if (!_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    int index=value.shader.FindPropertyIndex(name); if(index<0)return false;
                    var type=value.shader.GetPropertyType(index);
                    if(name=="_BlinnPhongSpecularToggle" || name=="_SixWayColorAbsorptionToggle")
                    {if(type!=UnityEngine.Rendering.ShaderPropertyType.Float)return false;float x=value.GetFloat(name);if(float.IsNaN(x)||float.IsInfinity(x))return false;}
                    else if(name=="_SpecularColor" || name=="_SixWayEmissionColor")
                    {if(type!=UnityEngine.Rendering.ShaderPropertyType.Color)return false;Color c=value.GetColor(name);for(int ch=0;ch<4;++ch)if(float.IsNaN(c[ch])||float.IsInfinity(c[ch]))return false;}
                    else if(name=="_MaterialInfo" || name=="_SixWayInfo")
                    {if(type!=UnityEngine.Rendering.ShaderPropertyType.Vector)return false;Vector4 v=value.GetVector(name);for(int ch=0;ch<4;++ch)if(float.IsNaN(v[ch])||float.IsInfinity(v[ch]))return false;}
                    else if(type!=UnityEngine.Rendering.ShaderPropertyType.Texture || value.shader.GetPropertyTextureDimension(index)!=UnityEngine.Rendering.TextureDimension.Tex2D)return false;
                }
                foreach(string name in new[]{"_NB_Flags1Hi16","_NB_ForceNoMipFlagsLo16","_NB_ForceNoMipFlagsHi16"})
                {if(!NBShaderRootItem.HasFloatProperty(value,name))return false;float x=value.GetFloat(name);if(float.IsNaN(x)||float.IsInfinity(x))return false;}
            }
            return true;
        }
        internal bool TryApplyGraphLightSubToggle(string name, bool? enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if ((name!="_BlinnPhongSpecularToggle" && name!="_SixWayColorAbsorptionToggle") || !HasGraphLightSubControlsSchema()) return false;
            int count=_rootItem.Mats.Count;var targets=new UnityEngine.Object[count];var oldRaw=new float[count];var oldAllow=new float[count];var next=new float[count];var oldDeclared=new bool[count][];
            for(int i=0;i<count;++i)
            {
                Material value=_rootItem.Mats[i];targets[i]=value;oldRaw[i]=value.GetFloat(name);oldAllow[i]=value.GetFloat("_NB_TierAllowLighting");oldDeclared[i]=NBShaderFeatureLevelMaterialApplier.CaptureGraphDeclaredKeywordState(value);
                next[i]=enabled.HasValue?(enabled.Value?1f:0f):value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex(name));
                Material probe=null;
                try {probe=new Material(value){hideFlags=HideFlags.HideAndDontSave};probe.SetFloat(name,next[i]);bool changed;if(!NBShaderFeatureLevelMaterialApplier.CanApplyGraphSavedSupportedGateTier(probe,out changed))return false;}
                finally {if(probe)UnityEngine.Object.DestroyImmediate(probe);}
            }
            Undo.RecordObjects(targets,"NB Lighting Intent");
            bool completed=false;
            try
            {
                for(int i=0;i<count;++i)
                {
                    Material value=_rootItem.Mats[i];value.SetFloat(name,next[i]);bool changed;
                    if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedLightingGroup(value,out changed))return false;
                }
                completed=true;foreach(Material value in _rootItem.Mats)EditorUtility.SetDirty(value);
                return true;
            }
            finally
            {
                if(!completed)for(int i=0;i<count;++i)
                {Material value=_rootItem.Mats[i];value.SetFloat(name,oldRaw[i]);value.SetFloat("_NB_TierAllowLighting",oldAllow[i]);NBShaderFeatureLevelMaterialApplier.RestoreGraphDeclaredKeywordState(value,oldDeclared[i]);}
                RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();
            }
        }
        internal bool TryFinalizeGraphLightRampEdit(IList<Texture> before)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(before==null || before.Count!=_rootItem.Mats.Count || !HasGraphLightSubControlsSchema())return false;
            const int mask=1<<(29-16);
            for(int i=0;i<before.Count;++i)
            {
                Material value=_rootItem.Mats[i];Texture after=value.GetTexture("_SixWayEmissionRamp");
                if(before[i]==after)continue;
                if(WriteGraphHalfSlice(value,"_NB_Flags1Hi16",mask,after?mask:0))EditorUtility.SetDirty(value);
            }
            RefreshGraphMainTexPropertyReferences();return true;
        }


        internal static readonly string[] GraphBaseBackColorProperties={"_BaseBackColorFoldOut","_BaseBackColor_Toggle","_BaseBackColor"};
        internal bool HasGraphBaseBackColorEditSchema()
        {
            if(!HasGraphMainTexTargets() || _rootItem.ShaderFlags==null || _rootItem.ShaderFlags.Count!=_rootItem.Mats.Count)return false;
            foreach(Material value in _rootItem.Mats)
            {
                foreach(string name in new[]{GraphGUIStateVersionProperty,"_BaseBackColorFoldOut","_BaseBackColor_Toggle"})
                {if(!NBShaderRootItem.HasFloatProperty(value,name)||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;float x=value.GetFloat(name);if(float.IsNaN(x)||float.IsInfinity(x))return false;}
                if(value.GetFloat(GraphGUIStateVersionProperty)!=2)return false;
                int index=value.shader.FindPropertyIndex("_BaseBackColor");if(index<0||value.shader.GetPropertyType(index)!=UnityEngine.Rendering.ShaderPropertyType.Color||!_rootItem.PropertyInfoDic.ContainsKey("_BaseBackColor"))return false;
                Color color=value.GetColor("_BaseBackColor");for(int channel=0;channel<4;++channel)if(float.IsNaN(color[channel])||float.IsInfinity(color[channel]))return false;
            }
            return true;
        }
        internal bool TryGetGraphBaseBackColorDisplay(out bool enabled,out bool mixed)
        {
            enabled=mixed=false;if(!_rootItem.CanDisplayGraphBaseBackColor)return false;bool first=true;
            foreach(ShaderFlagsBase flags in _rootItem.ShaderFlags)
            {bool current=flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_BACKCOLOR,index:0);if(first){enabled=current;first=false;}else mixed|=enabled!=current;}
            return true;
        }
        internal bool TryApplyGraphBaseBackColorToggle(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphBaseBackColorEditSchema())return false;
            const int mask=1<<(28-16);
            return RunGraphMainTexEdit("NB Back Color",value=>{
                float mirrorBefore=value.GetFloat("_BaseBackColor_Toggle");
                bool changed=WriteGraphHalfSlice(value,"_NB_Flags0Hi16",mask,enabled?mask:0);
                if(!changed)NotifyGraphPackedFlagsEdited(value,"_NB_Flags0Hi16",mask);
                return changed||System.BitConverter.ToInt32(System.BitConverter.GetBytes(mirrorBefore),0)!=System.BitConverter.ToInt32(System.BitConverter.GetBytes(value.GetFloat("_BaseBackColor_Toggle")),0);
            });
        }
        internal bool TryRunGraphBaseBackColorReset(System.Action reset)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(reset==null||!HasGraphBaseBackColorEditSchema())return false;
            bool resetApplied=false;const int mask=1<<(28-16);
            return RunGraphMainTexEdit("Reset NB Back Color",value=>{
                if(!resetApplied){reset();resetApplied=true;}
                bool changed=WriteGraphHalfSlice(value,"_NB_Flags0Hi16",mask,value.GetFloat("_BaseBackColor_Toggle")>0.5f?mask:0);
                if(!changed)NotifyGraphPackedFlagsEdited(value,"_NB_Flags0Hi16",mask);
                return true;
            });
        }

        internal static readonly string[] GraphChromaticSharedProperties={"_ChromaticAberrationFoldOut","_Distortion_Choraticaberrat_Toggle","_Distortion_Choraticaberrat_WithNoise_Toggle"};
        internal bool HasGraphChromaticEditSchema()
        {
            if(_rootItem.MatEditor==null || !HasGraphMainTexTargets())return false;
            foreach(Material material in _rootItem.Mats)
            {
                bool ignored;if(!CanApplyGraphSavedProjectionForPaint(material,out ignored))return false;
                foreach(string name in new[]{"_ChromaticAberrationFoldOut","_Distortion_Choraticaberrat_Toggle","_Distortion_Choraticaberrat_WithNoise_Toggle","_NB_Flags0Lo16","_NB_Flags0Hi16","_NB_CustomDataFlag0Lo16","_NB_CustomDataFlag0Hi16"})
                {if(!NBShaderRootItem.HasFloatProperty(material,name)||!_rootItem.PropertyInfoDic.ContainsKey(name))return false;float v=material.GetFloat(name);if(float.IsNaN(v)||float.IsInfinity(v))return false;}
                int index=material.shader.FindPropertyIndex("_DistortionDirection");
                if(index<0||material.shader.GetPropertyType(index)!=UnityEngine.Rendering.ShaderPropertyType.Vector||!_rootItem.PropertyInfoDic.ContainsKey("_DistortionDirection"))return false;
                Vector4 direction=material.GetVector("_DistortionDirection");for(int i=0;i<4;++i)if(float.IsNaN(direction[i])||float.IsInfinity(direction[i]))return false;
            }
            return true;
        }
        internal bool TryApplyGraphChromaticToggle(bool enabled)=>TryCommitGraphChromaticOwned(enabled,false);
        internal bool TryApplyGraphChromaticNoiseFlag(bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphChromaticEditSchema())return false;
            return RunGraphMainTexEdit("Edit NB Chromatic With Noise",material=>
            {
                string before=EditorJsonUtility.ToJson(material);WriteGraphHalfSlice(material,"_NB_Flags0Lo16",2,enabled?2:0);
                NotifyGraphPackedFlagsEdited(material,"_NB_Flags0Lo16",2);return before!=EditorJsonUtility.ToJson(material);
            });
        }
        internal bool TryApplyGraphChromaticIntensity(float value)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(float.IsNaN(value)||float.IsInfinity(value)||!HasGraphChromaticEditSchema())return false;
            return RunGraphMainTexEdit("Edit NB Chromatic Intensity",material=>
            {Vector4 v=material.GetVector("_DistortionDirection");if(v.z==value)return false;v.z=value;material.SetVector("_DistortionDirection",v);return true;});
        }
        internal bool TryApplyGraphChromaticCustomData(NBShaderFlags.CutomDataComponent component)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if((int)component<0||(int)component>8||!HasGraphChromaticEditSchema())return false;
            int[] values={0,NBShaderFlags.CustomData1XBit,NBShaderFlags.CustomData1YBit,NBShaderFlags.CustomData1ZBit,NBShaderFlags.CustomData1WBit,NBShaderFlags.CustomData2XBit,NBShaderFlags.CustomData2YBit,NBShaderFlags.CustomData2ZBit,NBShaderFlags.CustomData2WBit};
            return RunGraphMainTexEdit("Edit NB Chromatic Custom Data",material=>WriteGraphHalfSlice(material,"_NB_CustomDataFlag0Hi16",15<<12,values[(int)component]<<12));
        }
        internal bool TryResetGraphChromatic()=>TryCommitGraphChromaticOwned(false,true);
        private bool TryCommitGraphChromaticOwned(bool enabled,bool reset)
        {
            if(!HasGraphChromaticEditSchema())return false;
            void WriteIntent(Material material)
            {
                material.SetFloat("_Distortion_Choraticaberrat_Toggle",enabled?1f:0f);
                if(!reset)return;
                WriteGraphHalfSlice(material,"_NB_Flags0Lo16",2,0);NotifyGraphPackedFlagsEdited(material,"_NB_Flags0Lo16",2);
                WriteGraphHalfSlice(material,"_NB_CustomDataFlag0Hi16",15<<12,0);
                Vector4 direction=material.GetVector("_DistortionDirection");direction.z=material.shader.GetPropertyDefaultVectorValue(material.shader.FindPropertyIndex("_DistortionDirection")).z;material.SetVector("_DistortionDirection",direction);
            }
            foreach(Material material in _rootItem.Mats)
            {
                var probe=new Material(material){hideFlags=HideFlags.HideAndDontSave};
                try{WriteIntent(probe);bool changed;if(!NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedChromaticGroup(probe,out changed))return false;}
                finally{UnityEngine.Object.DestroyImmediate(probe);}
            }
            string[] fields={"_Distortion_Choraticaberrat_Toggle","_NB_TierAllowChromaticAberration","_NB_Flags0Lo16","_Distortion_Choraticaberrat_WithNoise_Toggle","_NB_CustomDataFlag0Hi16"};
            var before=new float[_rootItem.Mats.Count][];var z=new float[_rootItem.Mats.Count];var objects=new UnityEngine.Object[_rootItem.Mats.Count];var states=new string[_rootItem.Mats.Count];
            for(int i=0;i<objects.Length;++i){Material material=_rootItem.Mats[i];objects[i]=material;before[i]=new float[fields.Length];for(int f=0;f<fields.Length;++f)before[i][f]=material.GetFloat(fields[f]);z[i]=material.GetVector("_DistortionDirection").z;states[i]=EditorJsonUtility.ToJson(material);}
            Undo.RecordObjects(objects,reset?"Reset NB Chromatic":"Edit NB Chromatic");
            for(int i=0;i<objects.Length;++i)
            {
                Material material=_rootItem.Mats[i];WriteIntent(material);bool changed;
                if(NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedChromaticGroup(material,out changed))continue;
                for(int r=0;r<objects.Length;++r){Material target=_rootItem.Mats[r];for(int f=0;f<fields.Length;++f)target.SetFloat(fields[f],before[r][f]);Vector4 direction=target.GetVector("_DistortionDirection");direction.z=z[r];target.SetVector("_DistortionDirection",direction);}
                RefreshGraphMainTexPropertyReferences();return false;
            }
            for(int i=0;i<objects.Length;++i)if(states[i]!=EditorJsonUtility.ToJson(_rootItem.Mats[i]))EditorUtility.SetDirty(_rootItem.Mats[i]);
            RefreshGraphMainTexPropertyReferences();return true;
        }


        internal static readonly string[] GraphBaseNumericProperties={"_BaseOptionBigBlockItemFoldOut","_BaseColorIntensityForTimeline","_AlphaAll","AlphaAllRangeVec","_fogintensity"};
        internal bool HasGraphBaseNumericSchema()
        {
            if(!HasGraphMainTexTargets())return false;
            foreach(Material value in _rootItem.Mats)
            {
                foreach(string name in new[]{GraphGUIStateVersionProperty,"_BaseOptionBigBlockItemFoldOut","_BaseColorIntensityForTimeline","_AlphaAll","_fogintensity"})
                {
                    if(!_rootItem.PropertyInfoDic.ContainsKey(name))return false;int index=value.shader.FindPropertyIndex(name);if(index<0)return false;
                    var type=value.shader.GetPropertyType(index);if(type!=UnityEngine.Rendering.ShaderPropertyType.Float&&!(name=="_BaseColorIntensityForTimeline"&&type==UnityEngine.Rendering.ShaderPropertyType.Range))return false;
                    float x=value.GetFloat(name);if(float.IsNaN(x)||float.IsInfinity(x))return false;
                }
                if(value.GetFloat(GraphGUIStateVersionProperty)!=2)return false;
                int ri=value.shader.FindPropertyIndex("AlphaAllRangeVec");if(ri<0||value.shader.GetPropertyType(ri)!=UnityEngine.Rendering.ShaderPropertyType.Vector||!_rootItem.PropertyInfoDic.ContainsKey("AlphaAllRangeVec"))return false;
                Vector4 range=value.GetVector("AlphaAllRangeVec");for(int i=0;i<4;++i)if(float.IsNaN(range[i])||float.IsInfinity(range[i]))return false;
            }
            return true;
        }
        internal bool TryWriteGraphBaseNumeric(string name,float next)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if((name!="_BaseColorIntensityForTimeline"&&name!="_AlphaAll"&&name!="_fogintensity")||float.IsNaN(next)||float.IsInfinity(next)||!HasGraphBaseNumericSchema())return false;
            return RunGraphMainTexEdit("NB Base Numeric",value=>{if(value.GetFloat(name)==next)return false;value.SetFloat(name,next);return true;});
        }
        internal bool TryWriteGraphAlphaRange(float min,float max,bool minEdited,bool maxEdited)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if((!minEdited&&!maxEdited)||float.IsNaN(min)||float.IsInfinity(min)||float.IsNaN(max)||float.IsInfinity(max)||!HasGraphBaseNumericSchema())return false;
            return RunGraphMainTexEdit("NB Overall Alpha Range",value=>{
                Vector4 before=value.GetVector("AlphaAllRangeVec"),next=before;if(minEdited)next.x=min;if(maxEdited)next.y=max;
                bool changed=next!=before;if(changed)value.SetVector("AlphaAllRangeVec",next);
                float alpha=value.GetFloat("_AlphaAll"),clamped=Mathf.Clamp(alpha,Mathf.Min(next.x,next.y),Mathf.Max(next.x,next.y));if(clamped!=alpha){value.SetFloat("_AlphaAll",clamped);changed=true;}return changed;
            });
        }
        internal bool TryResetGraphBaseNumeric(string name)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if((name!="_BaseColorIntensityForTimeline"&&name!="_AlphaAll"&&name!="_fogintensity")||!HasGraphBaseNumericSchema())return false;
            return RunGraphMainTexEdit("Reset NB Base Numeric",value=>{
                value.SetFloat(name,value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex(name)));
                if(name=="_AlphaAll")value.SetVector("AlphaAllRangeVec",value.shader.GetPropertyDefaultVectorValue(value.shader.FindPropertyIndex("AlphaAllRangeVec")));return true;
            });
        }


        internal static readonly string[] GraphBaseShadowProperties={"_AffectsShadows","_TransparentShadowDitherToggle","_IgnoreVetexColor_Toggle"};
        internal bool HasGraphBaseShadowSchema()
        {
            if(!HasGraphMainTexTargets())return false;
            foreach(Material value in _rootItem.Mats)
            {
                foreach(string name in GraphBaseShadowProperties)
                {
                    if(!_rootItem.PropertyInfoDic.ContainsKey(name)||!NBShaderMaterialIntentResolver.HasFloatShaderProperty(value,name))return false;
                    float x=value.GetFloat(name);if(float.IsNaN(x)||float.IsInfinity(x))return false;
                }
                NBShaderFeatureTier tier;NBShaderMaterialIntentResult intent;
                if(!NBShaderFeatureLevelMaterialApplier.TryReadGraphSavedSupportedGateTier(value,out intent))return false;
                tier=intent.tier;NBShaderPassIntent shadow;
                if(!NBShaderFeatureRuntime.CanApplyGraphOwnedShadowPassState(value,tier,
                    NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),
                    NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out shadow)||shadow==null)return false;
            }
            return true;
        }
        // Explicit Surface action shares the original pure reader and Shadow capability.
        // It never projects Cast or writes on paint/Validate.
        internal static bool CanApplyGraphShadowCoverageForSurface(Material material)
        {
            NBShaderMaterialIntentResult intent;
            if(!NBShaderFeatureLevelMaterialApplier.TryReadGraphSavedSupportedGateTier(material,out intent))return false;
            NBShaderPassIntent shadow;
            return NBShaderFeatureRuntime.CanApplyGraphOwnedShadowPassState(material,intent.tier,
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(intent.tier),
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(intent.tier),out shadow);
        }
        internal static bool TryApplyGraphShadowCoverageForSurface(IList<Material> materials,out bool changed)
        {
            changed=false;if(materials==null||materials.Count==0)return false;
            // Check the complete selection before the first NB write, including
            // unchanged-surface targets. Official URP Surface retains its own authority.
            foreach(Material material in materials)if(!CanApplyGraphShadowCoverageForSurface(material))return false;
            var before=new Dictionary<Material,Vector2>();
            foreach(Material material in materials)
                if(material.HasProperty("_TransparentShadowDitherToggle")&&!before.ContainsKey(material))
                    before.Add(material,new Vector2(material.GetFloat("_NB_Flags1Lo16"),material.GetFloat("_TransparentShadowDitherToggle")));
            try
            {
                foreach(Material material in before.Keys)changed|=ApplyGraphShadowCoverageForSurface(material);
                return true;
            }
            catch
            {
                foreach(var row in before){row.Key.SetFloat("_NB_Flags1Lo16",row.Value.x);row.Key.SetFloat("_TransparentShadowDitherToggle",row.Value.y);}
                changed=false;throw;
            }
        }
        internal static bool ApplyGraphShadowCoverageForSurface(Material material)
        {
            if(!CanApplyGraphShadowCoverageForSurface(material)||!material.HasProperty("_TransparentShadowDitherToggle"))return false;
            float dither=material.GetFloat("_TransparentShadowDitherToggle");
            bool transparent=material.GetFloat("_Surface")==1f;
            int coverage=transparent?1|(ReadGraphHalf(material,"_NB_Flags1Lo16")&2):0;
            bool changed=WriteGraphHalfSlice(material,"_NB_Flags1Lo16",3,coverage);
            if(!transparent&&dither!=0f){material.SetFloat("_TransparentShadowDitherToggle",0f);changed=true;}
            return changed;
        }
        internal bool TryWriteGraphBaseShadow(string name,bool enabled)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(Array.IndexOf(GraphBaseShadowProperties,name)<0||!HasGraphBaseShadowSchema())return false;
            // Schema/enum preflight precedes the existing pre-write Undo owner.
            var images=new GraphBaseShadowSnapshot[_rootItem.Mats.Count];
            for(int i=0;i<images.Length;++i)images[i]=new GraphBaseShadowSnapshot(_rootItem.Mats[i]);
            try
            {
                bool accepted=true;
                bool result=RunGraphMainTexEdit("NB Base Shadow/Vertex Color",material=>{
                    bool next=enabled;
                    if(name=="_TransparentShadowDitherToggle"&&material.GetFloat("_Surface")!=1f)next=false;
                    bool changed=material.GetFloat(name)!=(next?1f:0f);if(changed)material.SetFloat(name,next?1f:0f);
                    if(name=="_IgnoreVetexColor_Toggle")
                        return WriteGraphHalfSlice(material,"_NB_Flags1Lo16",NBShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR,next?NBShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR:0)||changed;
                    if(name=="_TransparentShadowDitherToggle")
                        changed|=WriteGraphHalfSlice(material,"_NB_Flags1Lo16",2,next?2:0);
                    changed|=ApplyGraphShadowCoverageForSurface(material);
                    var tier=(NBShaderFeatureTier)(int)material.GetFloat(FeatureTierPropertyName);bool castChanged;
                    if(!NBShaderFeatureRuntime.ApplyGraphOwnedShadowPassState(material,tier,
                        NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),
                        NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out castChanged)){accepted=false;return changed;}
                    return changed||castChanged;
                });
                if(!result||!accepted){foreach(var image in images)image.Restore();RefreshGraphMainTexPropertyReferences();return false;}
                return true;
            }
            catch{foreach(var image in images)image.Restore();RefreshGraphMainTexPropertyReferences();throw;}
        }
        private sealed class GraphBaseShadowSnapshot
        {
            readonly Material material;readonly float affects,dither,ignore,word,cast;readonly bool pass;
            internal GraphBaseShadowSnapshot(Material value)
            {
                material=value;affects=value.GetFloat("_AffectsShadows");dither=value.GetFloat("_TransparentShadowDitherToggle");ignore=value.GetFloat("_IgnoreVetexColor_Toggle");word=value.GetFloat("_NB_Flags1Lo16");cast=value.GetFloat("_CastShadows");pass=value.GetShaderPassEnabled("ShadowCaster");
            }
            internal void Restore()
            {
                material.SetFloat("_AffectsShadows",affects);material.SetFloat("_TransparentShadowDitherToggle",dither);material.SetFloat("_IgnoreVetexColor_Toggle",ignore);material.SetFloat("_NB_Flags1Lo16",word);material.SetFloat("_CastShadows",cast);material.SetShaderPassEnabled("ShadowCaster",pass);
            }
        }
        private sealed class GraphShadowCasterSnapshot
        {
            readonly Material material;readonly bool owned,pass;readonly float cast;
            internal GraphShadowCasterSnapshot(Material value)
            {material=value;owned=value.HasProperty("_TransparentShadowDitherToggle");if(owned){cast=value.GetFloat("_CastShadows");pass=value.GetShaderPassEnabled("ShadowCaster");}}
            internal void Restore(){if(owned){material.SetFloat("_CastShadows",cast);material.SetShaderPassEnabled("ShadowCaster",pass);}}
        }
        int _graphResetPaintPass, _graphResetPaintShaderRevision;
        bool _graphResetPaintAccepted;
        internal bool HasGraphSharedResetSchema()
        {
            int pass=_graphFreshPreflightDepth==0?_rootItem.EnsureGraphGUIPureReadPass():0;
            if(pass==0)return HasGraphSharedResetSchemaFresh();
            if(_graphResetPaintPass!=pass||_graphResetPaintShaderRevision!=NBShaderRootItem.ShaderPropertyTypeCacheRevision)
            {
                _graphResetPaintAccepted=HasGraphSharedResetSchemaFresh();_graphResetPaintPass=pass;
                _graphResetPaintShaderRevision=NBShaderRootItem.ShaderPropertyTypeCacheRevision;
            }
            return _graphResetPaintAccepted;
        }
        private bool HasGraphSharedResetSchemaFresh()
        {
            if(!HasGraphSupportedGateTierEditSchema()||!HasGraphBaseNumericSchema()||!HasGraphBaseShadowSchema()||
                !HasGraphBaseBackColorEditSchema()||!HasGraphLightSubControlsSchema()||!HasGraphTADepthEditSchema()||
                !HasGraphQCMEditSchema()||!HasGraphMainTexUVEditSchema()||!HasGraphMainTexCustomDataEditSchema()||
                !HasGraphNormalMapEditSchema()||!HasGraphMatCapEditSchema()||!HasGraphOverlayEditSchema()||
                !HasGraphParallaxEditSchema()||!HasGraphFresnelEditSchema()||!HasGraphDissolveEditSchema()||
                !HasGraphMaskProgramEditSchema()||!HasGraphColorAdjustmentEditSchema()||!HasGraphColorRampEditSchema()||
                !HasGraphDepthFeaturesEditSchema()||!HasGraphDepthDecalEditSchema()||!HasGraphNoiseEditSchema()||
                !HasGraphSharedUVEditSchema()||!HasGraphVertexOffsetEditSchema()||!HasGraphStencilWithoutPlayerEditSchema()||
                !HasGraphVATEditSchema()||!HasGraphFlipbookEditSchema()||!HasGraphPortalEditSchema()||!HasGraphChromaticEditSchema())return false;
            foreach(Material value in _rootItem.Mats)
            {
                // List discovery must not seed/initialize a legacy capability as a side effect.
                if(value.GetFloat(GraphGUIStateVersionProperty)!=2f||value.GetFloat("_NB_TierVATFamily")==-1f||value.GetFloat("_NB_TierVATSubMode")==-1f)return false;
                var tier=(NBShaderFeatureTier)(int)value.GetFloat(FeatureTierPropertyName);bool change;
                if(!NBShaderFeatureRuntime.CanApplyGraphOwnedProjection(value,tier,
                    NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),
                    NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out change))return false;
            }
            return true;
        }
        internal bool TryResetGraphSpecialUVValues(string kind)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if((kind!="special"&&kind!="twirl"&&kind!="polar"&&kind!="all")||!HasGraphMainTexUVEditSchema())return false;
            return RunGraphMainTexEdit("Reset NB Special UV",value=>{
                bool changed=false;
                if(kind=="special"||kind=="all")
                {
                    int channel=value.HasProperty("_SpecialUVChannelMode")?(int)value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex("_SpecialUVChannelMode")):0;
                    if(value.HasProperty("_SpecialUVChannelMode"))value.SetFloat("_SpecialUVChannelMode",channel);
                    changed|=WriteGraphHalfSlice(value,"_NB_Flags1Hi16",12,channel==0?4:8);
                }
                if(kind=="twirl"||kind=="all")
                {
                    float enabled=value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex("_UTwirlEnabled"));value.SetFloat("_UTwirlEnabled",enabled);
                    value.SetVector("_TWParameter",value.shader.GetPropertyDefaultVectorValue(value.shader.FindPropertyIndex("_TWParameter")));value.SetFloat("_TWStrength",value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex("_TWStrength")));
                    changed|=WriteGraphHalfSlice(value,"_NB_Flags0Lo16",NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON,enabled>.5f?NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON:0);
                }
                if(kind=="polar"||kind=="all")
                {
                    float enabled=value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex("_PolarCoordinatesEnabled"));value.SetFloat("_PolarCoordinatesEnabled",enabled);
                    value.SetVector("_PCCenter",value.shader.GetPropertyDefaultVectorValue(value.shader.FindPropertyIndex("_PCCenter")));
                    changed|=WriteGraphHalfSlice(value,"_NB_Flags0Lo16",NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON,enabled>.5f?NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON:0);
                }
                return true;
            });
        }
        internal bool TryRunGraphSharedReset(Action reset,bool resetSpecial)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            return TryRunGraphKnownMaterialEdit(reset,rampBefore=>{
                if(resetSpecial&&!TryResetGraphSpecialUVValues("all"))return false;
                // Original Native final sync derives this existing ramp bit after Light reset.
                if(resetSpecial)foreach(Material value in _rootItem.Mats)
                    WriteGraphHalfSlice(value,"_NB_Flags1Hi16",1<<(29-16),value.GetTexture("_SixWayEmissionRamp")?1<<(29-16):0);
                else if(!TryFinalizeGraphLightRampEdit(rampBefore))return false;
                foreach(Material value in _rootItem.Mats)
                {
                    var tier=(NBShaderFeatureTier)(int)value.GetFloat(FeatureTierPropertyName);bool changed;
                    if(!NBShaderFeatureRuntime.TryApplyGraphOwnedProjection(value,tier,
                        NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),
                        NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out changed))return false;
                }
                return true;
            },"Reset NB shared Graph controls");
        }
        internal bool TryRunGraphKnownMaterialEdit(Action reset,Func<IList<Texture>,bool> finish,string label)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(reset==null||!HasGraphSharedResetSchema())return false;
            var originals=new List<string>();var originalObjectReferences=new List<Dictionary<string,UnityEngine.Object>>();var objects=new List<UnityEngine.Object>();
            var rampBefore=new List<Texture>();var passState=new List<Dictionary<string,bool>>();int group=Undo.GetCurrentGroup();bool completed=false;
            try
            {
                foreach(Material value in _rootItem.Mats)
                {
                    originals.Add(EditorJsonUtility.ToJson(value));objects.Add(value);rampBefore.Add(value.GetTexture("_SixWayEmissionRamp"));
                    // JSON restores structure/value state, but cannot retain builtin or
                    // transient Object references. Hold EVERY serialized reference strongly.
                    var refs=new Dictionary<string,UnityEngine.Object>();
                    using(var serialized=new SerializedObject(value))
                    {
                        SerializedProperty iterator=serialized.GetIterator();
                        while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                            refs[iterator.propertyPath]=iterator.objectReferenceValue;
                    }
                    originalObjectReferences.Add(refs);
                    var passes=new Dictionary<string,bool>();
                    foreach(string tag in new[]{"SRPDefaultUnlit","UniversalForward","Universal Forward","DepthOnly","DepthNormalsOnly","ShadowCaster","NBCameraOpaqueDistortPass","NBDeferredDistortPass","Universal2D"})passes[tag]=value.GetShaderPassEnabled(tag);
                    passState.Add(passes);
                }
                Undo.RegisterCompleteObjectUndo(objects.ToArray(),label);
                reset();
                if(finish!=null&&!finish(rampBefore))return false;
                if(!HasGraphSharedResetSchema()||(_rootItem.Mats[0].HasProperty("_ShaderKeywordFoldOut")&&!HasGraphRemainingSharedUISchema()))return false;
                completed=true;foreach(Material value in _rootItem.Mats)EditorUtility.SetDirty(value);Undo.CollapseUndoOperations(group);return true;
            }
            finally
            {
                if(!completed)for(int i=0;i<originals.Count;++i)
                {
                    Material value=_rootItem.Mats[i];EditorJsonUtility.FromJsonOverwrite(originals[i],value);
                    using(var serialized=new SerializedObject(value))
                    {
                        serialized.Update();
                        foreach(var reference in originalObjectReferences[i])
                        {
                            SerializedProperty property=serialized.FindProperty(reference.Key);
                            if(property==null||property.propertyType!=SerializedPropertyType.ObjectReference)
                                throw new InvalidOperationException("NB Graph Reset rollback lost serialized Object reference path: "+reference.Key);
                            property.objectReferenceValue=reference.Value;
                        }
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    foreach(var pass in passState[i])value.SetShaderPassEnabled(pass.Key,pass.Value);
                }
                RefreshGraphMainTexPropertyReferences();_rootItem.Context?.Refresh();
            }
        }

        internal static readonly string[] GraphRemainingSharedUIProperties={"_BigBlockModeSettingFoldOut","_AdditiveToPreMultiplyAlphaLerp","_ShaderKeywordFoldOut"};
        internal static bool HasGraphRemainingSharedUIMaterialSchema(Material material)
        {
            NBShaderMaterialIntentResult intent;
            if(!NBShaderFeatureLevelMaterialApplier.TryReadGraphSavedSupportedGateTier(material,out intent))return false;
            foreach(string name in GraphRemainingSharedUIProperties)
            {
                if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;
                float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;
            }
            bool change;
            return NBShaderFeatureRuntime.CanApplyGraphOwnedProjection(material,intent.tier,
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(intent.tier),
                NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(intent.tier),out change);
        }
        int _graphRemainingPaintPass, _graphRemainingPaintShaderRevision;
        bool _graphRemainingPaintAccepted;
        internal bool HasGraphRemainingSharedUISchema()
        {
            int pass=_graphFreshPreflightDepth==0?_rootItem.EnsureGraphGUIPureReadPass():0;
            if(pass==0)return HasGraphRemainingSharedUISchemaFresh();
            if(_graphRemainingPaintPass!=pass||_graphRemainingPaintShaderRevision!=NBShaderRootItem.ShaderPropertyTypeCacheRevision)
            {
                _graphRemainingPaintAccepted=HasGraphRemainingSharedUISchemaFresh();_graphRemainingPaintPass=pass;
                _graphRemainingPaintShaderRevision=NBShaderRootItem.ShaderPropertyTypeCacheRevision;
            }
            return _graphRemainingPaintAccepted;
        }
        private bool HasGraphRemainingSharedUISchemaFresh()
        {
            if(!HasGraphSharedResetSchema())return false;
            foreach(Material material in _rootItem.Mats)
            {
                if(!HasGraphRemainingSharedUIMaterialSchema(material))return false;
                foreach(string name in GraphRemainingSharedUIProperties)if(!_rootItem.PropertyInfoDic.ContainsKey(name))return false;
            }
            return true;
        }
        internal bool TryWriteGraphAdditiveBlend(float value,bool reset=false)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphRemainingSharedUISchema()||float.IsNaN(value)||float.IsInfinity(value)||(!reset&&(value<0||value>1)))return false;
            return TryRunGraphKnownMaterialEdit(()=>{
                foreach(Material material in _rootItem.Mats)material.SetFloat("_AdditiveToPreMultiplyAlphaLerp",reset?(material.GetFloat("_Blend")==1f?1f:0f):value);
            },null,"NB Additive To Premultiply");
        }
        internal static bool TryApplyGraphBlendPresetForOfficialEdit(IList<Material> materials,IDictionary<Material,float> beforeBlend,out bool changed)
        {
            changed=false;if(materials==null||materials.Count==0||beforeBlend==null)return false;
            foreach(Material material in materials)if(!HasGraphRemainingSharedUIMaterialSchema(material)||!beforeBlend.ContainsKey(material))return false;
            foreach(Material material in materials)
            {
                float mode=material.GetFloat("_Blend");if(mode==beforeBlend[material]||(mode!=1f&&mode!=2f))continue;
                float next=mode==1f?1f:0f;
                if(material.GetFloat("_AdditiveToPreMultiplyAlphaLerp")!=next){material.SetFloat("_AdditiveToPreMultiplyAlphaLerp",next);changed=true;}
            }
            return true;
        }
        internal bool TryRunGraphKnownToolbarEdit(Action edit)
        {
            using var graphFreshPreflight=BeginGraphFreshPreflightRead();
            if(!HasGraphRemainingSharedUISchema())return false;
            return TryRunGraphKnownMaterialEdit(edit,null,"NB Graph Toolbar");
        }

    }

    public enum VATMode
    {
        Houdini = 0,
        Tyflow = 1,
        UnKnownOrMixed = -1
    }

    public enum TimeMode
    {
        Default = 0,
        UnScaleTime = 1,
        ScriptableTime = 2
    }
}
