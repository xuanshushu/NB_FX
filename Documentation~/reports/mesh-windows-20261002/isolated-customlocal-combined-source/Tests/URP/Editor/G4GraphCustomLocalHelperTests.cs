using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphCustomLocalHelperTests
    {
        Shader graph,legacy;Type helperType;
        [OneTimeSetUp] public void Warm()
        {
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            graph=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");legacy=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");
            helperType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShader.NBParticleLocalTransformHelper",false)).First(t=>t!=null);
        }
        static void Rows(Material material,Transform transform)
        {
            for(int row=0;row<4;row++){Assert.That(material.GetVector("_NB_CustomLocalToWorld"+row),Is.EqualTo(transform.localToWorldMatrix.GetRow(row)));Assert.That(material.GetVector("_NB_CustomWorldToLocal"+row),Is.EqualTo(transform.worldToLocalMatrix.GetRow(row)));}
        }
        void Apply(Component helper)=>helperType.GetMethod("ApplyCustomTransform",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(helper,null);
        [TestCase(false)] [TestCase(true)]
        public void GraphHelper_WritesExactRows_DisableAndReenablePreserveIntent(bool negative)
        {
            var go=new GameObject("CustomLocal helper lifecycle",typeof(ParticleSystem));var a=new Material(graph);var b=new Material(graph);
            try
            {
                var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=a;go.transform.position=new Vector3(.5f,-.25f,.125f);go.transform.rotation=Quaternion.Euler(17,23,11);go.transform.localScale=new Vector3(negative?-1.25f:1.25f,.75f,1.5f);
                a.SetFloat("_NB_Flags0Lo16",123);a.SetFloat("_Surface",1);a.renderQueue=3123;string[] keywords=a.shaderKeywords.OrderBy(k=>k).ToArray();
                var helper=go.AddComponent(helperType) as Behaviour;Assert.That(helper,Is.Not.Null);Apply(helper);Rows(a,go.transform);Assert.That(a.GetFloat("_NB_CustomLocalTransform"),Is.EqualTo(1));Assert.That(a.shaderKeywords.OrderBy(k=>k),Is.EqualTo(keywords));Assert.That(a.GetFloat("_NB_Flags0Lo16"),Is.EqualTo(123));Assert.That(a.GetFloat("_Surface"),Is.EqualTo(1));Assert.That(a.renderQueue,Is.EqualTo(3123));
                renderer.sharedMaterial=b;Apply(helper);Assert.That(a.GetFloat("_NB_CustomLocalTransform"),Is.Zero);Assert.That(b.GetFloat("_NB_CustomLocalTransform"),Is.EqualTo(1));Rows(b,go.transform);
                helper.enabled=false;Assert.That(b.GetFloat("_NB_CustomLocalTransform"),Is.Zero);Rows(b,go.transform);helper.enabled=true;Apply(helper);Assert.That(b.GetFloat("_NB_CustomLocalTransform"),Is.EqualTo(1));
                Object.DestroyImmediate(helper);Assert.That(b.GetFloat("_NB_CustomLocalTransform"),Is.Zero);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
        }
        [Test]
        public void ExistingHelper_LegacyMatrixAndKeywordPath_RemainsExact()
        {
            var go=new GameObject("Legacy CustomLocal helper",typeof(ParticleSystem));var material=new Material(legacy);
            try
            {
                var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.simulationSpace=ParticleSystemSimulationSpace.World;go.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;go.transform.position=new Vector3(.5f,-.25f,.125f);go.transform.rotation=Quaternion.Euler(17,23,11);go.transform.localScale=new Vector3(-1.25f,.75f,1.5f);
                var helper=go.AddComponent(helperType) as Behaviour;Apply(helper);Assert.That(material.GetMatrix("_CustomLocalTransformLocalToWorld"),Is.EqualTo(go.transform.localToWorldMatrix));Assert.That(material.GetMatrix("_CustomLocalTransformWorldToLocal"),Is.EqualTo(go.transform.worldToLocalMatrix));Assert.That(material.IsKeywordEnabled("_CUSTOM_LOCAL_TRANSFORM"),Is.True);helper.enabled=false;Assert.That(material.IsKeywordEnabled("_CUSTOM_LOCAL_TRANSFORM"),Is.False);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(material);}
        }
    }
}
