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
    // Existing public ApplyTier chain only. No QualitySettings changes/asset save/pipeline/coroutine.
    public sealed class G4GraphRuntimePublicTierTests
    {
        const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        readonly G4BackFirstSharedLifecycleTests owner=new G4BackFirstSharedLifecycleTests();readonly List<Object> extra=new List<Object>();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);static Type Runtime=>Find("NBShader.NBShaderFeatureRuntime");static Type TierType=>Find("NBShader.NBShaderFeatureTier");static Type SettingsType=>Find("NBShader.NBShaderFeatureRuntimeSettings");static Type NullableTier=>typeof(Nullable<>).MakeGenericType(TierType);
        static object Tier(int n)=>Enum.ToObject(TierType,n);
        static MethodInfo Unique(Type t,string n,params Type[] signature)
        {var matches=t.GetMethods(All).Where(m=>m.Name==n&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(signature)).ToArray();Assert.That(matches.Length,Is.EqualTo(1),t.FullName+"."+n+" exact signature");return matches[0];}
        Material Graph(bool modern=true)=> (Material)Unique(typeof(G4BackFirstSharedLifecycleTests),"New",typeof(bool),typeof(bool),typeof(bool)).Invoke(owner,new object[]{modern,modern,true});
        static string[] Keywords=>(string[])Find("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);static string[] Passes=>(string[])Find("NBShader.NBShaderPassFeatureCatalog").GetField("RawPassFeatureIds",All).GetValue(null);static string[] Projections=>(string[])Runtime.GetField("GraphSupportedProjectionProperties",All).GetValue(null);
        static Dictionary<string,string> Raw(Material m)=>G4SpecDebugFixture.Properties(m).Where(p=>!Projections.Contains(p.Key)&&p.Key!="_NB_BackFirstEffective").ToDictionary(p=>p.Key,p=>p.Value);
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"|"+string.Join("|",m.shaderKeywords.OrderBy(x=>x));
        static void Apply(Material value,object tier)=>Unique(Runtime,"ApplyTier",typeof(Material),NullableTier).Invoke(null,new[]{(object)value,tier});
        static void Apply(Material value,ScriptableObject settings,object tier)=>Unique(Runtime,"ApplyTier",typeof(Material),SettingsType,NullableTier).Invoke(null,new[]{(object)value,settings,tier});
        static void Apply(IEnumerable<Material> values,object tier)=>Unique(Runtime,"ApplyTier",typeof(IEnumerable<Material>),NullableTier).Invoke(null,new[]{(object)values,tier});
        static void Apply(IEnumerable<Material> values,ScriptableObject settings,object tier)=>Unique(Runtime,"ApplyTier",typeof(IEnumerable<Material>),SettingsType,NullableTier).Invoke(null,new[]{(object)values,settings,tier});
        ScriptableObject Settings()
        {
            var value=ScriptableObject.CreateInstance(SettingsType);extra.Add(value);
            string[] denied=Keywords.Except(new[]{"_MASKMAP_ON","_CHROMATIC_ABERRATION","_VAT_TYFLOW","_OVERRIDE_Z"}).ToArray();SettingsType.GetField("lowAllowedKeywords").SetValue(value,denied);SettingsType.GetField("highAllowedKeywords").SetValue(value,Keywords.ToArray());SettingsType.GetField("lowAllowedPassFeatures").SetValue(value,Array.Empty<string>());SettingsType.GetField("highAllowedPassFeatures").SetValue(value,Passes.ToArray());return value;
        }
        static void Intent(Material m)
        {m.SetFloat("_Mask_Toggle",1);m.SetFloat("_Distortion_Choraticaberrat_Toggle",1);m.SetFloat("_OverrideZ_Toggle",1);m.SetFloat("_VAT_Toggle",1);m.SetFloat("_VATMode",1);m.SetFloat("_TyFlowVATSubMode",1);m.SetFloat("_FlipbookBlending",1);}
        static void State(Material m,bool high)
        {Assert.That(m.GetFloat("_NB_TierAllowMask"),Is.EqualTo(high?1:0));Assert.That(m.GetFloat("_NB_TierAllowChromaticAberration"),Is.EqualTo(high?1:0));Assert.That(m.GetFloat("_NB_TierVATFamily"),Is.EqualTo(high?1:-2));Assert.That(m.GetFloat("_NB_TierVATSubMode"),Is.EqualTo(high?1:0));Assert.That(m.GetFloat("_NB_TierAllowFlipbook"),Is.Zero);Assert.That(m.IsKeywordEnabled("_OVERRIDE_Z"),Is.EqualTo(high));}
        [OneTimeSetUp]public void Guard()
        {Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}}
        [TearDown]public void Cleanup(){owner.Cleanup();foreach(var o in extra)if(o)Object.DestroyImmediate(o);extra.Clear();}
        [Test]public void G4RuntimeTier2_SingleBothPublicOverloadsAndSavedRawPreserved()
        {
            var value=Graph();Intent(value);var raw=Raw(value);Apply(value,Tier(0));State(value,true);Assert.That(Raw(value),Is.EquivalentTo(raw),"No-settings Low retains original all-catalog policy");var settings=Settings();Apply(value,settings,Tier(0));State(value,false);Assert.That(Raw(value),Is.EquivalentTo(raw));Apply(value,settings,Tier(2));State(value,true);Assert.That(Raw(value),Is.EquivalentTo(raw));var stable=Snapshot(value);Apply(value,settings,Tier(2));Assert.That(Snapshot(value),Is.EqualTo(stable));
        }
        [Test]public void G4RuntimeTier2_CollectionBothOverloadsNativePerItemGraphSubset()
        {
            var modern=Graph();var legacy=Graph(false);Intent(modern);Intent(legacy);legacy.SetFloat("_NB_GraphScreenPassMigrationComplete",0);string[] tags={"SRPDefaultUnlit","NBDeferredDistortPass","NBCameraOpaqueDistortPass"};for(int i=0;i<tags.Length;++i)legacy.SetShaderPassEnabled(tags[i],(i&1)==0);var passBefore=tags.Select(legacy.GetShaderPassEnabled).ToArray();
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");var native=new Material(shader){hideFlags=HideFlags.HideAndDontSave};extra.Add(native);native.SetFloat("_fresnelEnabled",1);float saved=native.GetFloat("_NBShaderFeatureTier");var rm=Raw(modern);var rl=Raw(legacy);
            Material[] values={modern,null,native,legacy,modern};Apply(values,Tier(0));State(modern,true);State(legacy,true);Assert.That(native.IsKeywordEnabled("_FRESNEL"),Is.True);var settings=Settings();SettingsType.GetField("lowAllowedKeywords").SetValue(settings,Keywords.Except(new[]{"_FRESNEL","_MASKMAP_ON","_CHROMATIC_ABERRATION","_VAT_TYFLOW","_OVERRIDE_Z"}).ToArray());Apply(values,settings,Tier(0));State(modern,false);State(legacy,false);Assert.That(native.IsKeywordEnabled("_FRESNEL"),Is.False);Assert.That(native.GetFloat("_fresnelEnabled"),Is.EqualTo(1));Assert.That(native.GetFloat("_NBShaderFeatureTier"),Is.EqualTo(saved));Assert.That(Raw(modern),Is.EquivalentTo(rm));Assert.That(Raw(legacy),Is.EquivalentTo(rl));for(int i=0;i<tags.Length;++i)Assert.That(legacy.GetShaderPassEnabled(tags[i]),Is.EqualTo(passBefore[i]));
        }
        [Test]public void G4RuntimeTier2_CurrentQualityMappingWithoutQualityMutation()
        {
            int original=QualitySettings.GetQualityLevel();string[] names=QualitySettings.names;Assert.That(names!=null&&original>=0&&original<names.Length&&!string.IsNullOrEmpty(names[original]),Is.True);string name=names[original];var settings=Settings();var mapType=SettingsType.GetNestedType("QualityTierMapping",All);Assert.That(mapType,Is.Not.Null);var entry=Activator.CreateInstance(mapType);mapType.GetField("qualityName").SetValue(entry,name.ToUpperInvariant());mapType.GetField("tier").SetValue(entry,Tier(0));var array=Array.CreateInstance(mapType,1);array.SetValue(entry,0);SettingsType.GetField("qualityTierMappings").SetValue(settings,array);
            var value=Graph();Intent(value);var raw=Raw(value);Apply(value,settings,null);State(value,false);mapType.GetField("tier").SetValue(entry,Tier(2));Apply(new[]{value},settings,null);State(value,true);Assert.That(Raw(value),Is.EquivalentTo(raw));SettingsType.GetField("qualityTierMappings").SetValue(settings,Array.CreateInstance(mapType,0));Apply(value,settings,null);State(value,true);Assert.That(QualitySettings.GetQualityLevel(),Is.EqualTo(original));Assert.That(QualitySettings.names,Is.EqualTo(names));
        }
        [Test]public void G4RuntimeTier2_InvalidGraphSubsetAtomicNativeStillPerItem()
        {
            var a=Graph();var b=Graph();Intent(a);Intent(b);var native=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader")){hideFlags=HideFlags.HideAndDontSave};extra.Add(native);native.SetFloat("_fresnelEnabled",1);var settings=Settings();SettingsType.GetField("lowAllowedKeywords").SetValue(settings,Keywords.Except(new[]{"_FRESNEL"}).ToArray());string baseline=EditorJsonUtility.ToJson(b);
            foreach(var invalid in new[]{Tuple.Create("_NB_GraphGUIStateVersion",3f),Tuple.Create("_NB_TierVATFamily",2f),Tuple.Create("_NB_TierAllowChromaticAberration",float.NaN),Tuple.Create("_NB_GraphScreenPassMigrationComplete",.5f)})
            {EditorJsonUtility.FromJsonOverwrite(baseline,b);b.SetFloat(invalid.Item1,invalid.Item2);string sa=Snapshot(a),sb=Snapshot(b);Apply(new[]{a,native,b},settings,Tier(0));Assert.That(Snapshot(a),Is.EqualTo(sa),"Graph subset first remains exact on last invalid target");Assert.That(Snapshot(b),Is.EqualTo(sb));Assert.That(native.IsKeywordEnabled("_FRESNEL"),Is.False,"Original Native processing is not a cross-host transaction");Assert.That(native.GetFloat("_fresnelEnabled"),Is.EqualTo(1));}
            string same=Snapshot(a);Apply(a,settings,Tier(-1));Assert.That(Snapshot(a),Is.EqualTo(same),"Unknown explicit Graph tier refuses writes");
        }
    }
}
