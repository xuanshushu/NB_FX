using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Public original flags/CD operations on actual Graph Material storage.
    public sealed class G4GraphHalfwordWriterPreservationTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly List<Object> owned=new List<Object>();
        [OneTimeSetUp] public void Preflight()=>G4SpecDebugFixture.PreflightImport();
        static Type FlagsType=>G4SpecDebugFixture.FindType("NBShader.NBShaderFlags");
        Material Material(){var m=G4SpecDebugFixture.NewGraph();owned.Add(m);return m;}
        static int Bits(float value)=>BitConverter.ToInt32(BitConverter.GetBytes(value),0);
        static void Same(float actual,float expected,string label)=>Assert.That(Bits(actual),Is.EqualTo(Bits(expected)),label+" exact stored bits");
        sealed class Snapshot
        {
            readonly object value;
            static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}
            public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void AssertSame(Material m,string label,params string[] allowed)=>Shared.GetMethod("AssertSame",All).Invoke(value,new object[]{m,label,allowed});
        }
        [TearDown] public void Cleanup(){foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        static IEnumerable<TestCaseData> Cases()
        {
            foreach(string kind in new[]{"flags","wrap","cd3"})foreach(string raw in new[]{"fraction","over16"})foreach(string operation in new[]{"editLo","editHi","noop"})
                yield return new TestCaseData(kind,raw,operation).SetName("G4Halfword_"+kind+"_"+raw+"_"+operation);
        }
        [TestCaseSource(nameof(Cases))]
        public void UneditedHalfAndDecodedNoOpPreserveRaw(string kind,string raw,string operation)
        {
            var m=Material();string prefix=kind=="flags"?"_NB_Flags0":kind=="wrap"?"_NB_WrapFlags":"_NB_CustomDataFlag3";
            float lo=raw=="over16"?-7.25f:kind=="flags"?32.25f:kind=="wrap"?16.25f:15.25f;
            float hi=raw=="over16"?70000.25f:64.25f;m.SetFloat(prefix+"Lo16",lo);m.SetFloat(prefix+"Hi16",hi);
            var before=Snapshot.Read(m);var flags=Activator.CreateInstance(FlagsType,new object[]{m});bool over=raw=="over16";
            if(kind=="cd3")
            {
                Type component=FlagsType.GetNestedType("CutomDataComponent",All);object value=Enum.Parse(component,(operation=="editLo"&&!over)||operation=="noop"&&over||operation=="editHi"&&over?"Off":"CustomData1X");
                // Fraction no-op already holds the actual Custom1X nibble15.
                if(operation=="noop"&&!over)value=Enum.Parse(component,"CustomData1X");
                FlagsType.GetMethod("SetCustomDataFlag",All).Invoke(flags,new object[]{value,operation=="editHi"?16:0,3});
            }
            else
            {
                int low=kind=="flags"?1<<5:1<<4;int high=kind=="flags"?1<<25:1<<20;int bit=operation=="editHi"?high:low;
                string method=operation=="editLo"?over?"SetFlagBits":"ClearFlagBits":operation=="editHi"?over?"ClearFlagBits":"SetFlagBits":over?"ClearFlagBits":"SetFlagBits";
                FlagsType.GetMethod(method,All).Invoke(flags,new object[]{bit,null,kind=="wrap"?2:0});
            }
            if(operation=="noop")before.AssertSame(m,"Decoded no-op keeps every property/raw bit/keyword/pass");
            else if(operation=="editLo")
            {
                Same(m.GetFloat(prefix+"Hi16"),hi,"Unedited Hi");
                Assert.That(m.GetFloat(prefix+"Lo16"),Is.EqualTo(over?kind=="cd3"?15:kind=="flags"?32:16:0));
                before.AssertSame(m,"Only requested physical Lo half may change",prefix+"Lo16");
            }
            else
            {
                Same(m.GetFloat(prefix+"Lo16"),lo,"Unedited Lo");
                int expected=over?kind=="cd3"?65520:kind=="flags"?65023:65519:kind=="cd3"?79:kind=="flags"?576:80;
                Assert.That(m.GetFloat(prefix+"Hi16"),Is.EqualTo(expected));before.AssertSame(m,"Only requested physical Hi half may change",prefix+"Hi16");
            }
            var stable=Snapshot.Read(m);
            // Repeat the same public operation: native word semantics unchanged,
            // now every already-correct stored half must be an exact no-op.
            if(kind=="cd3")
            {
                Type component=FlagsType.GetNestedType("CutomDataComponent",All);object current=FlagsType.GetMethod("GetCustomDataFlag",All).Invoke(flags,new object[]{operation=="editHi"?16:0,3});
                FlagsType.GetMethod("SetCustomDataFlag",All).Invoke(flags,new object[]{current,operation=="editHi"?16:0,3});
            }
            else
            {
                int bit=operation=="editHi"?kind=="flags"?1<<25:1<<20:kind=="flags"?1<<5:1<<4;bool set=(bool)FlagsType.GetMethod("CheckFlagBits",All).Invoke(flags,new object[]{bit,null,kind=="wrap"?2:0});
                FlagsType.GetMethod(set?"SetFlagBits":"ClearFlagBits",All).Invoke(flags,new object[]{bit,null,kind=="wrap"?2:0});
            }
            stable.AssertSame(m,"Repeated real public writer is fully stable");
        }
        [Test] public void G4Halfword_NativeIntegerBranch_RemainsOriginal()
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");Assert.That(shader,Is.Not.Null);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);
            m.SetInteger("_W9ParticleShaderFlags",unchecked((int)0x80000004));var flags=Activator.CreateInstance(FlagsType,new object[]{m});
            FlagsType.GetMethod("SetFlagBits",All).Invoke(flags,new object[]{32,null,0});Assert.That(m.GetInteger("_W9ParticleShaderFlags"),Is.EqualTo(unchecked((int)0x80000024)));
        }
        [Test] public void G4Halfword_MPBIntegerBranch_DoesNotTouchGraphMaterial()
        {
            var m=Material();m.SetFloat("_NB_Flags0Lo16",-7.25f);m.SetFloat("_NB_Flags0Hi16",70000.25f);var before=Snapshot.Read(m);var flags=Activator.CreateInstance(FlagsType,new object[]{m});var block=new MaterialPropertyBlock();block.SetInteger("_W9ParticleShaderFlags",4);
            FlagsType.GetMethod("SetFlagBits",All).Invoke(flags,new object[]{32,block,0});Assert.That(block.GetInteger("_W9ParticleShaderFlags"),Is.EqualTo(36));before.AssertSame(m,"MPB branch retains original integer seam and never edits Material halves");
        }
    }
}
