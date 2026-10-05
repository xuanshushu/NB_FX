using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NBFX.Baseline.Tests
{
    // One real component-order check; reuse the validated NPC scope/cleanup.
    // It intentionally adds no rendering matrix or manual NB lifecycle invocation.
    public sealed class G4NBPostCinemachineOwnerTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        object scope;string previousEnv;bool environmentChanged;string folder;
        readonly List<Row> rows=new List<Row>();
        [Serializable] sealed class Row
        {
            public string phase;public bool ownerIsA,ownerIsB,bindingIsCamera,cacheIsPerlin,soloIsCamera;
            public int aIndex,bIndex,activeMask,shakeMask;public float amplitude;
        }
        [Serializable] sealed class Evidence{public string scope="Original last-bound owner, same camera two Controllers, actual Editor ticks, state-only";public Row[] states;public bool environmentRestored;}
        static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
        static object Get(object o,string n){var t=o as Type??o.GetType();return t.GetField(n,All)?.GetValue(o is Type?null:o)??t.GetProperty(n,All)?.GetValue(o is Type?null:o);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,All).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,All).Invoke(o,args);
        void Save(bool restored=false){if(folder!=null)File.WriteAllText(Path.Combine(folder,"owner-states.json"),JsonUtility.ToJson(new Evidence{states=rows.ToArray(),environmentRestored=restored},true));}
        [TearDown] public void Cleanup()
        {
            try{if(scope!=null)((IDisposable)scope).Dispose();}
            finally{scope=null;if(environmentChanged){Environment.SetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR",previousEnv);environmentChanged=false;}Save(true);}
        }
        [UnityTest] public IEnumerator NBPostCine3_SameCamera_LastOwner_RealEnableDisable()
        {
            previousEnv=Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");var root=previousEnv??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXCineOwner");
            var target=Path.Combine(root,"owner-boundary");Assert.That(Directory.Exists(target),Is.False,"Keep prior owner evidence");Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target,"before-env.txt"),previousEnv??"<null>");Environment.SetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR",target);environmentChanged=true;
            try
            {
                var original=Find("NBFX.Baseline.Tests.G4NBPostCinemachinePersistenceTests");var type=original.GetNestedType("Scope",BindingFlags.NonPublic);Assert.That(type,Is.Not.Null);
                scope=Activator.CreateInstance(type,All,null,new object[]{true,false},System.Globalization.CultureInfo.InvariantCulture);folder=(string)Get(scope,"folder");
                var manager=(Component)Get(scope,"manager");var a=(Component)Get(scope,"controller");var camera=(Component)Get(scope,"vcam");var perlin=(Component)Get(scope,"perlin");var scene=(Scene)Get(scope,"scene");
                var bgo=new GameObject("NBFX_Cine_SecondOwner");bgo.SetActive(false);SceneManager.MoveGameObjectToScene(bgo,scene);var b=bgo.AddComponent(a.GetType());Call(scope,"RememberComponent",b);
                foreach(string n in new[]{"chromaticAberrationToggle","distortSpeedToggle","radialBlurToggle","vignetteToggle","overlayTextureToggle","flashToggle"})Set(b,n,false);
                Set(b,"cinemachineCamera",camera);Set(b,"cameraShakeToggle",true);Set(b,"cameraShakeIntensity",2f);
                Row State(string phase)
                {
                    var row=new Row{phase=phase,ownerIsA=ReferenceEquals(Get(manager,"_cameraShakeOwner"),a),ownerIsB=ReferenceEquals(Get(manager,"_cameraShakeOwner"),b),bindingIsCamera=ReferenceEquals(Get(manager,"currentVirtualCamera"),camera),cacheIsPerlin=ReferenceEquals(Get(manager,"_perlin"),perlin),soloIsCamera=ReferenceEquals(Get(Find("Unity.Cinemachine.CinemachineCore"),"SoloCamera"),camera),aIndex=(int)Get(a,"index"),bIndex=(int)Get(b,"index"),activeMask=(int)Get(manager,"_controllerIndexFlags"),shakeMask=(int)Get(manager.GetType(),"cameraShakeToggles"),amplitude=(float)Get(perlin,"AmplitudeGain")};rows.Add(row);Save();return row;
                }
                Set(a,"cameraShakeToggle",true);yield return (IEnumerator)Call(scope,"Wait");var onlyA=State("A active owner");
                bgo.SetActive(true);yield return (IEnumerator)Call(scope,"Wait");var both=State("B enabled last on same camera");
                ((Behaviour)a).enabled=false;yield return (IEnumerator)Call(scope,"Wait");var nonOwnerOff=State("A non-owner disabled");
                ((Behaviour)b).enabled=false;yield return (IEnumerator)Call(scope,"Wait");var ownerOff=State("B owner disabled");
                // All actual states are durable before ownership assertions.
                Assert.That(onlyA.ownerIsA&&onlyA.cacheIsPerlin&&onlyA.bindingIsCamera,Is.True);Assert.That(onlyA.amplitude,Is.EqualTo(4));
                Assert.That(both.ownerIsB&&both.cacheIsPerlin&&both.bindingIsCamera&&both.soloIsCamera,Is.True);Assert.That(both.aIndex,Is.Not.EqualTo(both.bIndex));Assert.That(both.amplitude,Is.EqualTo(4),"Original max aggregation is preserved");
                Assert.That(nonOwnerOff.ownerIsB&&nonOwnerOff.cacheIsPerlin&&nonOwnerOff.bindingIsCamera&&nonOwnerOff.soloIsCamera,Is.True,"Non-owner with the same camera may not clear the current binding");Assert.That(nonOwnerOff.amplitude,Is.EqualTo(2));Assert.That(nonOwnerOff.activeMask,Is.EqualTo(1<<nonOwnerOff.bIndex));Assert.That(nonOwnerOff.shakeMask,Is.EqualTo(nonOwnerOff.activeMask));
                Assert.That(ownerOff.ownerIsA||ownerOff.ownerIsB||ownerOff.cacheIsPerlin||ownerOff.bindingIsCamera||ownerOff.soloIsCamera,Is.False);Assert.That(ownerOff.amplitude,Is.Zero);Assert.That(ownerOff.activeMask,Is.Zero);Assert.That(ownerOff.shakeMask,Is.Zero);
            }
            finally{Cleanup();}
        }
    }
}
