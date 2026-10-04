using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    public sealed class G4FresnelUneditedHalfTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        G4FresnelSharedGUIServiceTests helper;
        [OneTimeSetUp]public void Preflight()=>G4SpecDebugFixture.PreflightImport();
        [TearDown]public void Cleanup(){helper?.Cleanup();helper=null;}
        [TestCase("FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON","_NB_Flags0Lo16",130.125f,"_InvertFresnel_Toggle",TestName="G4Fresnel_UneditedLoFractionBits_InvertHiEdit")]
        [TestCase("FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA","_NB_Flags0Hi16",65536.25f,"_FresnelColorAffectByAlpha",TestName="G4Fresnel_UneditedHiOver16Bits_AlphaLoEdit")]
        public void UneditedHalfBits(string name,string other,float value,string mirror)
        {
            helper=new G4FresnelSharedGUIServiceTests();var type=helper.GetType();
            var material=(Material)type.GetMethod("New",All).Invoke(helper,null);
            object[] args={new[]{material},"_fresnelEnabled",null,null};
            var host=(NBFXMainTexGUIEventHost)type.GetMethod("Host",All).Invoke(helper,args);host.Draw=null;
            var root=args[2];var sync=root.GetType().GetProperty("SyncService",All).GetValue(root);
            material.SetFloat(other,value);int before=BitConverter.ToInt32(BitConverter.GetBytes(material.GetFloat(other)),0);
            int bit=(int)G4SpecDebugFixture.FindType("NBShader.NBShaderFlags").GetField(name,All).GetValue(null);
            Assert.That((bool)sync.GetType().GetMethod("TryApplyGraphFresnelFlagEdit",All).Invoke(sync,new object[]{bit,true}),Is.True);
            Assert.That(material.GetFloat(mirror),Is.EqualTo(1));
            Assert.That(BitConverter.ToInt32(BitConverter.GetBytes(material.GetFloat(other)),0),Is.EqualTo(before),"Unedited finite noncanonical half must retain identical float bits.");
            Assert.That((bool)sync.GetType().GetMethod("TryApplyGraphFresnelFlagEdit",All).Invoke(sync,new object[]{bit,false}),Is.True);
            Assert.That(material.GetFloat(mirror),Is.Zero);
            Assert.That(BitConverter.ToInt32(BitConverter.GetBytes(material.GetFloat(other)),0),Is.EqualTo(before));
        }
    }
}
