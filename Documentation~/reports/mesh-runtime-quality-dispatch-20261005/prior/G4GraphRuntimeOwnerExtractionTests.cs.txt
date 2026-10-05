using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Extraction acceptance only: public Runtime ApplyTier still rejects Graph.
    // No quality switch, renderer, scene edits or synthetic material protocol.
    public sealed class G4GraphRuntimeOwnerExtractionTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        readonly G4BackFirstSharedLifecycleTests owner=new G4BackFirstSharedLifecycleTests();
        readonly List<Object> extra=new List<Object>();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);
        static Type Runtime=>Find("NBShader.NBShaderFeatureRuntime");
        static Type EditorApplier=>Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
        static Type Sync=>Find("NBShaderEditor.NBShaderSyncService");
        static object Tier(int n)=>Enum.ToObject(Find("NBShader.NBShaderFeatureTier"),n);
        static MethodInfo Unique(Type type,string name,params Type[] signature)
        {var methods=type.GetMethods(All).Where(m=>m.Name==name&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(signature)).ToArray();Assert.That(methods.Length,Is.EqualTo(1),type.FullName+"."+name+" signature unique");return methods[0];}
        static object Call(Type t,string n,params object[] a)
        {
            Type[] sig;
            if(n=="IsNBShaderMaterial"||n=="CanMutateMaterial")sig=new[]{typeof(Material)};
            else if(n=="ApplyGraphSupportedGateTier"||n=="ApplyGraphMaskGroup")sig=new[]{typeof(Material),Find("NBShader.NBShaderFeatureTier"),typeof(IEnumerable<string>),typeof(bool).MakeByRefType()};
            else if(n=="ApplyGraphOwnedScreenPassState"||n=="ApplyGraphOwnedBackFirstPassState")sig=new[]{typeof(Material),Find("NBShader.NBShaderFeatureTier"),typeof(IEnumerable<string>),typeof(IEnumerable<string>),typeof(bool).MakeByRefType()};
            else{Assert.Fail("Unreviewed reflection signature "+n);return null;}
            var method=Unique(t,n,sig);Assert.That(method.IsStatic,Is.True);return method.Invoke(null,a);
        }
        static string[] Keywords=>(string[])Find("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static string[] Passes=>(string[])Find("NBShader.NBShaderPassFeatureCatalog").GetField("RawPassFeatureIds",All).GetValue(null);
        static string[] Gates=>(string[])Runtime.GetField("GraphSupportedGateProperties",All).GetValue(null);
        static string[] Projections=>(string[])Runtime.GetField("GraphSupportedProjectionProperties",All).GetValue(null);
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"|"+string.Join("|",m.shaderKeywords.OrderBy(k=>k));
        Material New(bool modern=true,bool adopted=true)
            =>(Material)Unique(typeof(G4BackFirstSharedLifecycleTests),"New",typeof(bool),typeof(bool),typeof(bool)).Invoke(owner,new object[]{modern,adopted,true});
        [OneTimeSetUp] public void Guard()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
        }
        [TearDown] public void Cleanup(){owner.Cleanup();foreach(var o in extra)if(o)Object.DestroyImmediate(o);extra.Clear();}
        static bool RuntimeApply(Material[] materials,int level,string[] keywords,string[] passes,out bool changed)
        {
            var method=Unique(Runtime,"TryApplyGraphOwnedProjection",typeof(IEnumerable<Material>),Find("NBShader.NBShaderFeatureTier"),typeof(IEnumerable<string>),typeof(IEnumerable<string>),typeof(bool).MakeByRefType());
            object[] args={materials,Tier(level),keywords,passes,false};bool accepted=(bool)method.Invoke(null,args);changed=(bool)args[4];return accepted;
        }
        static Dictionary<string,string> Raw(Material m)
        {
            var result=G4SpecDebugFixture.Properties(m).Where(p=>!Projections.Contains(p.Key)&&p.Key!="_NB_BackFirstEffective").ToDictionary(p=>p.Key,p=>p.Value);
            result["#queue"]=m.renderQueue.ToString();return result;
        }
        static void EditorComposite(Material m,int level,string[] keywords,string[] passes)
        {
            object[] args={m,Tier(level),keywords,false};Assert.That(Call(EditorApplier,"ApplyGraphSupportedGateTier",args),Is.True);
            foreach(string method in new[]{"ApplyGraphOwnedScreenPassState","ApplyGraphOwnedBackFirstPassState"})
            {object[] a={m,Tier(level),keywords,passes,false};Assert.That(Call(Sync,method,a),Is.True);}
        }
        [Test] public void G4RuntimeOwner1_EditorRuntimeCompositeEqualAndRawPreserved()
        {
            Assert.That(Gates.Length,Is.EqualTo(31));Assert.That(Projections.Length,Is.EqualTo(33));
            foreach(string field in new[]{"GraphSupportedGateProperties","GraphSupportedTypedProjectionProperties","GraphSupportedProjectionProperties"})
                Assert.That(ReferenceEquals(Runtime.GetField(field,All).GetValue(null),EditorApplier.GetField(field,All).GetValue(null)),Is.True,"One registry instance: "+field);
            var e=New();var r=new Material(e){hideFlags=HideFlags.HideAndDontSave};extra.Add(r);
            foreach(var m in new[]{e,r})
            {
                m.SetFloat("_FxLightMode",4);m.SetFloat("_SixWayColorAbsorptionToggle",1);m.SetFloat("_BlinnPhongSpecularToggle",1);m.SetFloat("_OverrideZ_Toggle",1);
                m.SetFloat("_Mask_Toggle",1);m.SetFloat("_fresnelEnabled",1);m.SetFloat("_NB_Debug_Fresnel",1);m.SetFloat("_BackFirstPassToggle",1);
                m.SetFloat("_NB_CustomDataFlag3Hi16",53214.125f);m.SetFloat("_NB_Flags0Lo16",65536.25f);
                m.SetFloat("_VAT_Toggle",1);m.SetFloat("_FlipbookBlending",1);m.SetFloat("_VATMode",0);m.SetFloat("_HoudiniVATSubMode",3);
                m.SetFloat("_NB_TierVATFamily",-1);m.SetFloat("_NB_TierVATSubMode",-1);
            }
            var beforeE=Raw(e);var beforeR=Raw(r);var deny=Keywords.Except(new[]{"_FX_LIGHT_MODE_SIX_WAY","_SPECULAR_COLOR","NB_DEBUG_FRESNEL","_OVERRIDE_Z","_VAT_HOUDINI"}).ToArray();
            foreach(bool allow in new[]{false,true})
            {
                var kw=allow?Keywords:deny;var passes=allow?Passes:Array.Empty<string>();EditorComposite(e,allow?3:0,kw,passes);
                bool changed;Assert.That(RuntimeApply(new[]{r},allow?3:0,kw,passes,out changed),Is.True);
                Assert.That(G4SpecDebugFixture.Properties(r),Is.EquivalentTo(G4SpecDebugFixture.Properties(e)));Assert.That(r.shaderKeywords.OrderBy(k=>k),Is.EqualTo(e.shaderKeywords.OrderBy(k=>k)));
                foreach(string tag in new[]{"UniversalForward","SRPDefaultUnlit","NBDeferredDistortPass","NBCameraOpaqueDistortPass"})Assert.That(r.GetShaderPassEnabled(tag),Is.EqualTo(e.GetShaderPassEnabled(tag)),tag);
                Assert.That(Raw(e),Is.EquivalentTo(beforeE));Assert.That(Raw(r),Is.EquivalentTo(beforeR));Assert.That(r.GetFloat("_NBShaderFeatureTier"),Is.EqualTo(3));
                Assert.That(r.GetFloat("_NB_TierAllowLighting"),Is.EqualTo(allow?1:0));Assert.That(r.IsKeywordEnabled("_OVERRIDE_Z"),Is.EqualTo(allow));Assert.That(r.IsKeywordEnabled("EVALUATE_SH_VERTEX"),Is.EqualTo(allow));
                Assert.That(r.GetFloat("_NB_TierVATFamily"),Is.EqualTo(allow?0:-2));Assert.That(r.GetFloat("_NB_TierVATSubMode"),Is.EqualTo(allow?3:0));Assert.That(r.GetFloat("_NB_TierAllowFlipbook"),Is.Zero,"Raw VAT priority survives family filtering");
                string stable=Snapshot(r);Assert.That(RuntimeApply(new[]{r},allow?3:0,kw,passes,out changed),Is.True);Assert.That(changed,Is.False);Assert.That(Snapshot(r),Is.EqualTo(stable));
            }
            foreach(string denied in new[]{"_HOUDINI_VAT_PARTICLE_SPRITE","_VAT"})
            {
                var kw=Keywords.Except(new[]{denied}).ToArray();EditorComposite(e,0,kw,Passes);bool changed;Assert.That(RuntimeApply(new[]{r},0,kw,Passes,out changed),Is.True);
                Assert.That(G4SpecDebugFixture.Properties(r),Is.EquivalentTo(G4SpecDebugFixture.Properties(e)));Assert.That(Raw(r),Is.EquivalentTo(beforeR));
                Assert.That(r.GetFloat("_NB_TierAllowVAT"),Is.EqualTo(denied=="_VAT"?0:1));Assert.That(r.GetFloat("_NB_TierVATFamily"),Is.EqualTo(denied=="_VAT"?-2:0));Assert.That(r.GetFloat("_NB_TierVATSubMode"),Is.Zero);Assert.That(r.GetFloat("_NB_TierAllowFlipbook"),Is.Zero);
            }
        }
        [Test] public void G4RuntimeOwner1_GroupScopeAndUnownedPassesRemainExact()
        {
            var m=New(false);m.SetFloat("_NB_GraphScreenPassMigrationComplete",0);m.SetFloat("_Mask_Toggle",1);m.SetFloat("_NB_TierAllowLighting",float.NaN);
            var before=G4SpecDebugFixture.Properties(m);object[] group={m,Tier(0),new[]{"_MASKMAP_ON"},false};Assert.That(Call(EditorApplier,"ApplyGraphMaskGroup",group),Is.True);
            var after=G4SpecDebugFixture.Properties(m);foreach(var pair in before.Where(p=>!new[]{"_NB_TierAllowMask","_NB_TierAllowMask2","_NB_TierAllowMask3"}.Contains(p.Key)))Assert.That(after[pair.Key],Is.EqualTo(pair.Value),pair.Key);
            m.SetFloat("_NB_TierAllowLighting",1);string[] tags={"SRPDefaultUnlit","NBDeferredDistortPass","NBCameraOpaqueDistortPass"};m.SetShaderPassEnabled(tags[0],false);m.SetShaderPassEnabled(tags[1],true);m.SetShaderPassEnabled(tags[2],false);var enabled=tags.Select(m.GetShaderPassEnabled).ToArray();
            bool changed;Assert.That(RuntimeApply(new[]{m},0,Array.Empty<string>(),Array.Empty<string>(),out changed),Is.True);for(int i=0;i<tags.Length;++i)Assert.That(m.GetShaderPassEnabled(tags[i]),Is.EqualTo(enabled[i]));Assert.That(m.GetFloat("_NB_GraphScreenPassMigrationComplete"),Is.Zero);
        }
        [Test] public void G4RuntimeOwner1_InvalidLastTargetAtomicRefusal()
        {
            var a=New();var b=New();a.SetFloat("_Mask_Toggle",1);a.SetFloat("_NB_TierAllowMask",.25f);string baseline=EditorJsonUtility.ToJson(b);
            foreach(var invalid in new[]{Tuple.Create("_NB_GraphGUIStateVersion",3f),Tuple.Create("_NB_TierAllowLighting",float.NaN),Tuple.Create("_NB_GraphScreenPassMigrationComplete",.5f),Tuple.Create("_NB_GraphPassMigrationComplete",.5f)})
            {
                EditorJsonUtility.FromJsonOverwrite(baseline,b);b.SetFloat(invalid.Item1,invalid.Item2);string sa=Snapshot(a),sb=Snapshot(b);bool changed;
                Assert.That(RuntimeApply(new[]{a,b},0,Array.Empty<string>(),Array.Empty<string>(),out changed),Is.False,invalid.Item1);Assert.That(changed,Is.False);Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));
            }
            foreach(var pair in new[]{new Vector2(-1,0),new Vector2(-2,1),new Vector2(0,4),new Vector2(1,6),new Vector2(float.NaN,0)})
            {
                EditorJsonUtility.FromJsonOverwrite(baseline,b);b.SetFloat("_NB_TierVATFamily",pair.x);b.SetFloat("_NB_TierVATSubMode",pair.y);string sa=Snapshot(a),sb=Snapshot(b);bool changed;
                Assert.That(RuntimeApply(new[]{a,b},0,Array.Empty<string>(),Array.Empty<string>(),out changed),Is.False,"Invalid typed VAT pair");Assert.That(changed,Is.False);Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));
            }
        }
        [Test] public void G4RuntimeOwner1_TypedTyflowSelectionFallbackAndOwnedBeforeImageExact()
        {
            Assert.That(Gates.Length,Is.EqualTo(31));Assert.That(Projections.Length,Is.EqualTo(33));
            var value=New();value.SetFloat("_VAT_Toggle",1);value.SetFloat("_FlipbookBlending",1);value.SetFloat("_VATMode",1);
            string[] sub={"_TYFLOW_VAT_ABSOLUTE","_TYFLOW_VAT_RELATIVE","_TYFLOW_VAT_SKIN_R","_TYFLOW_VAT_SKIN_PR","_TYFLOW_VAT_SKIN_PRSAVE","_TYFLOW_VAT_SKIN_PRSXYZ"};
            for(int mode=0;mode<6;++mode)
            {
                value.SetFloat("_TyFlowVATSubMode",mode);var raw=Raw(value);bool changed;
                Assert.That(RuntimeApply(new[]{value},3,Keywords,Passes,out changed),Is.True);Assert.That(value.GetFloat("_NB_TierVATFamily"),Is.EqualTo(1));Assert.That(value.GetFloat("_NB_TierVATSubMode"),Is.EqualTo(mode));Assert.That(value.GetFloat("_NB_TierAllowVAT"),Is.EqualTo(1));Assert.That(value.GetFloat("_NB_TierAllowFlipbook"),Is.Zero);Assert.That(Raw(value),Is.EquivalentTo(raw));
                Assert.That(RuntimeApply(new[]{value},0,Keywords.Except(new[]{sub[mode]}).ToArray(),Passes,out changed),Is.True);Assert.That(value.GetFloat("_NB_TierVATFamily"),Is.EqualTo(1));Assert.That(value.GetFloat("_NB_TierVATSubMode"),Is.Zero,"Retained Tyflow family uses Native implicit Absolute0");Assert.That(Raw(value),Is.EquivalentTo(raw));
            }
            for(int i=0;i<Gates.Length;++i)value.SetFloat(Gates[i],i+.25f);
            value.SetFloat("_NB_TierVATFamily",1);value.SetFloat("_NB_TierVATSubMode",5);
            var declared=(string[])Runtime.GetField("GraphDeclaredKeywordNames",All).GetValue(null);for(int i=0;i<declared.Length;++i){if((i&1)==0)value.EnableKeyword(declared[i]);else value.DisableKeyword(declared[i]);}value.EnableKeyword("_OVERRIDE_Z");
            string[] tags={"UniversalForward","SRPDefaultUnlit","NBDeferredDistortPass","NBCameraOpaqueDistortPass"};for(int i=0;i<tags.Length;++i)value.SetShaderPassEnabled(tags[i],(i&1)==0);
            value.SetFloat("_NB_GraphScreenPassMigrationComplete",1);value.SetFloat("_NB_DistortionMode",1);value.SetFloat("_DisableMainPassToggle",1);value.SetFloat("_noisemapEnabled",1);
            string before=Snapshot(value);var states=tags.Select(value.GetShaderPassEnabled).ToArray();var imageType=Runtime.GetNestedType("GraphOwnedProjectionBeforeImage",All);Assert.That(imageType,Is.Not.Null);
            var ctor=imageType.GetConstructor(All,null,new[]{typeof(Material),Find("NBShader.NBShaderFeatureTier"),typeof(IEnumerable<string>),typeof(IEnumerable<string>)},null);Assert.That(ctor,Is.Not.Null);var image=ctor.Invoke(new[]{(object)value,Tier(3),Keywords,Passes});
            bool altered;Assert.That(RuntimeApply(new[]{value},0,Array.Empty<string>(),Array.Empty<string>(),out altered),Is.True);Assert.That(altered,Is.True);Unique(imageType,"Restore",Type.EmptyTypes).Invoke(image,null);Assert.That(Snapshot(value),Is.EqualTo(before),"Before-image restores union32, declared9, OVZ and owned Passes without raw/savedTier mutation");for(int i=0;i<tags.Length;++i)Assert.That(value.GetShaderPassEnabled(tags[i]),Is.EqualTo(states[i]),tags[i]);
        }
        [Test] public void G4RuntimeOwner1_PublicNativeAndBuildBoundariesUnchanged()
        {
            var graph=New();var snapshot=Snapshot(graph);var apply=Runtime.GetMethods(All).Single(m=>m.Name=="ApplyTier"&&m.GetParameters().Length==2&&m.GetParameters()[0].ParameterType==typeof(Material));apply.Invoke(null,new[]{(object)graph,Tier(0)});Assert.That(Snapshot(graph),Is.EqualTo(snapshot));
            Assert.That(Call(Runtime,"IsNBShaderMaterial",graph),Is.False);Assert.That(Call(Find("NBShader.NBShaderMaterialIntentResolver"),"IsNBShaderMaterial",graph),Is.False);
            Assert.That(Call(Find("NBShaders2.Editor.FeatureLevel.NBShaderEditorQualityTierWatcher"),"CanMutateMaterial",graph),Is.False);
            var api=Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelEditorAPI");var info=api.GetMethods(All).Single(m=>m.Name=="GetBuildInfo"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(Material));Assert.That(info.Invoke(null,new[]{(object)graph,Tier(3),Enum.ToObject(info.GetParameters()[2].ParameterType,0)}),Is.Null);
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");var native=new Material(shader){hideFlags=HideFlags.HideAndDontSave};extra.Add(native);native.SetFloat("_fresnelEnabled",1);float saved=native.GetFloat("_NBShaderFeatureTier");apply.Invoke(null,new[]{(object)native,Tier(0)});Assert.That(native.IsKeywordEnabled("_FRESNEL"),Is.True,"Existing no-settings Runtime keeps all catalog capabilities even with explicit Low");Assert.That(native.GetFloat("_NBShaderFeatureTier"),Is.EqualTo(saved));
            var settingsType=Find("NBShader.NBShaderFeatureRuntimeSettings");var settings=ScriptableObject.CreateInstance(settingsType);extra.Add(settings);
            settingsType.GetField("lowAllowedKeywords").SetValue(settings,Keywords.Except(new[]{"_FRESNEL"}).ToArray());settingsType.GetField("highAllowedKeywords").SetValue(settings,Keywords.ToArray());
            var explicitRuntime=Runtime.GetMethods(All).Single(m=>m.Name=="ApplyTier"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(Material));
            explicitRuntime.Invoke(null,new[]{(object)native,settings,Tier(0)});Assert.That(native.IsKeywordEnabled("_FRESNEL"),Is.False);Assert.That(native.GetFloat("_fresnelEnabled"),Is.EqualTo(1));Assert.That(native.GetFloat("_NBShaderFeatureTier"),Is.EqualTo(saved));
            explicitRuntime.Invoke(null,new[]{(object)native,settings,Tier(2)});Assert.That(native.IsKeywordEnabled("_FRESNEL"),Is.True);Assert.That(native.GetFloat("_NBShaderFeatureTier"),Is.EqualTo(saved));
            var collectionRuntime=Runtime.GetMethods(All).Single(m=>m.Name=="ApplyTier"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(IEnumerable<Material>));
            collectionRuntime.Invoke(null,new object[]{new[]{native,graph},settings,Tier(0)});Assert.That(native.IsKeywordEnabled("_FRESNEL"),Is.False);Assert.That(Snapshot(graph),Is.EqualTo(snapshot));Assert.That(native.GetFloat("_NBShaderFeatureTier"),Is.EqualTo(saved));
        }
    }
}
