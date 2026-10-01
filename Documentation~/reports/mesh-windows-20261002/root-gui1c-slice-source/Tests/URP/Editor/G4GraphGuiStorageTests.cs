using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// GUI-0 storage/backend tests on real ordinary-Mesh Materials. Calls the existing
    /// NBShaderFlags and shared Gradient/Texture items by reflection because this URP
    /// fixture assembly deliberately has no NB editor/runtime assembly dependency.
    /// No second flags/GUI implementation, VFX/Graph-MPB claim, visible Inspector,
    /// Tier/keyword/Pass integration, asset persistence, or render-parity proof here.
    /// Apply /tmp/nbfx-gui0-candidate before running this fixture.
    /// </summary>
    public sealed class G4GraphGuiStorageTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<Material> materials = new List<Material>();
        Type flagsType;
        Shader graphShader;
        Shader legacyShader;

        sealed class Word
        {
            public readonly int Slot;
            public readonly string Legacy, Lo, Hi;
            public Word(int slot, string legacy, string prefix)
            {
                Slot = slot; Legacy = legacy;
                Lo = prefix == null ? null : prefix + "Lo16";
                Hi = prefix == null ? null : prefix + "Hi16";
            }
        }

        static readonly Word[] MappedWords =
        {
            new Word(0, "_W9ParticleShaderFlags", "_NB_Flags0"),
            new Word(1, "_W9ParticleShaderFlags1", "_NB_Flags1"),
            new Word(2, "_W9ParticleShaderWrapFlags", "_NB_WrapFlags"),
            new Word(6, "_W9ParticleShaderColorChannelFlag", "_NB_ColorChannel"),
            new Word(7, "_W9ParticleShaderPNoiseBlendFlag", "_NB_PNoiseBlend"),
            new Word(8, "_NBShaderForceNoMipFlags", "_NB_ForceNoMipFlags")
        };
        static readonly Word UV = new Word(-1, "_UVModeFlag0", "_NB_UVModeFlag0");
        static readonly Word UVType = new Word(-1, "_UVModeFlagType0", "_NB_UVModeFlagType0");
        static readonly string[] LegacyNames =
        {
            "_W9ParticleShaderFlags", "_W9ParticleShaderFlags1", "_W9ParticleShaderWrapFlags",
            "_NBShaderGUIFoldToggle", "_NBShaderGUIFoldToggle1", "_NBShaderGUIFoldToggle2",
            "_W9ParticleShaderColorChannelFlag", "_W9ParticleShaderPNoiseBlendFlag", "_NBShaderForceNoMipFlags"
        };

        [SetUp]
        public void SetUp()
        {
            flagsType = FindType("NBShader.NBShaderFlags");
            graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader, Is.Not.Null, GraphPath);
            Assert.That(legacyShader, Is.Not.Null, LegacyPath);
            Assert.That(flagsType.GetMethod("ReadWord", InstanceMembers), Is.Not.Null,
                "GUI-0 candidate hooks must be integrated before this test run.");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var material in materials) Object.DestroyImmediate(material);
            materials.Clear();
        }

        Material NewMaterial(bool graph)
        {
            var result = new Material(graph ? graphShader : legacyShader) { hideFlags = HideFlags.HideAndDontSave };
            materials.Add(result);
            return result;
        }
        object Flags(Material material) => Activator.CreateInstance(flagsType, new object[] { material });
        object EnumValue(string name, int value) => Enum.ToObject(flagsType.GetNestedType(name), value);
        static Type FindType(string name)
        {
            var result = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            Assert.That(result, Is.Not.Null, name + " must be loaded.");
            return result;
        }
        static object Call(object target, string name, params object[] arguments)
        {
            var method = target.GetType().GetMethod(name, InstanceMembers);
            Assert.That(method, Is.Not.Null, target.GetType().FullName + "." + name);
            return method.Invoke(target, arguments);
        }
        static object Field(object target, string name)
        {
            var field = target.GetType().GetField(name, InstanceMembers);
            Assert.That(field, Is.Not.Null, target.GetType().FullName + "." + name);
            return field.GetValue(target);
        }
        static uint U(int value) => unchecked((uint)value);
        static int I(uint value) => unchecked((int)value);
        static uint ReadStorage(Material material, Word word, bool graph)
        {
            if (!graph) return U(material.GetInteger(word.Legacy));
            // Test oracle reads the canonical physical transport, not a production replacement.
            return (uint)material.GetFloat(word.Lo) | ((uint)material.GetFloat(word.Hi) << 16);
        }
        static void WriteStorage(Material material, Word word, uint value, bool graph)
        {
            if (!graph) { material.SetInteger(word.Legacy, I(value)); return; }
            Assert.That(material.HasProperty(word.Lo) && material.HasProperty(word.Hi), Is.True, word.Lo);
            material.SetFloat(word.Lo, value & 0xFFFFu);
            material.SetFloat(word.Hi, value >> 16);
        }
        static uint ReadPublicBits(object flags, int slot, MaterialPropertyBlock block = null)
        {
            uint result = 0;
            for (int bit = 0; bit < 32; bit++)
                if ((bool)Call(flags, "CheckFlagBits", I(1u << bit), block, slot)) result |= 1u << bit;
            return result;
        }

        static IEnumerable<TestCaseData> MappedCases()
        {
            foreach (bool graph in new[] { false, true })
                foreach (var word in MappedWords)
                    yield return new TestCaseData(graph, word.Slot).SetName("GUI0_" + (graph ? "Graph" : "Legacy") + "_All32Bits_Word" + word.Slot);
        }
        [TestCaseSource(nameof(MappedCases))]
        public void All32Bits_PreserveUnrelatedBits_AndSignedBit31(bool graph, int slot)
        {
            var word = MappedWords.Single(w => w.Slot == slot);
            var material = NewMaterial(graph);
            var flags = Flags(material);
            const uint seed = 0xA569C39Eu;
            for (int bit = 0; bit < 32; bit++)
            {
                uint mask = 1u << bit;
                WriteStorage(material, word, seed, graph);
                Call(flags, "SetFlagBits", I(mask), null, slot);
                Assert.That(ReadStorage(material, word, graph), Is.EqualTo(seed | mask), "set bit " + bit);
                Assert.That(ReadPublicBits(flags, slot), Is.EqualTo(seed | mask));
                Call(flags, "ClearFlagBits", I(mask), null, slot);
                Assert.That(ReadStorage(material, word, graph), Is.EqualTo(seed & ~mask), "clear bit " + bit);
                Assert.That(ReadPublicBits(flags, slot), Is.EqualTo(seed & ~mask));
                // An idempotent repeat must not change any neighboring/unknown bit.
                Call(flags, "ClearFlagBits", I(mask), null, slot);
                Assert.That(ReadStorage(material, word, graph), Is.EqualTo(seed & ~mask));
            }
        }

        [Test]
        public void LegacyMaterial_AllNineIntegerWords_NoFloatReinterpretation()
        {
            var material = NewMaterial(false);
            var flags = Flags(material);
            for (int slot = 0; slot < LegacyNames.Length; slot++)
            {
                string name = LegacyNames[slot];
                Assert.That(material.HasProperty(name), Is.True, name);
                const uint seed = 0x89ABCDEFu;
                material.SetInteger(name, I(seed));
                Assert.That(ReadPublicBits(flags, slot), Is.EqualTo(seed));
                Call(flags, "SetFlagBits", I(0x70100000u), null, slot);
                Assert.That(U(material.GetInteger(name)), Is.EqualTo(seed | 0x70100000u));
                Call(flags, "ClearFlagBits", I(0x80002000u), null, slot);
                Assert.That(U(material.GetInteger(name)), Is.EqualTo((seed | 0x70100000u) & ~0x80002000u));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MPB_AllNineWords_AlwaysLegacyIntegerEvenWithGraphOwner(bool graphOwner)
        {
            var material = NewMaterial(graphOwner);
            var flags = Flags(material);
            var block = new MaterialPropertyBlock();
            var graphBefore = new List<uint>();
            if (graphOwner)
                foreach (var word in MappedWords)
                {
                    WriteStorage(material, word, 0x87654321u, true);
                    graphBefore.Add(ReadStorage(material, word, true));
                }
            for (int slot = 0; slot < LegacyNames.Length; slot++)
            {
                int id = Shader.PropertyToID(LegacyNames[slot]);
                block.SetInteger(id, I(0xCDEF8765u));
                Assert.That(ReadPublicBits(flags, slot, block), Is.EqualTo(0xCDEF8765u));
                Call(flags, "SetFlagBits", I(0x10200080u), block, slot);
                Assert.That(U(block.GetInteger(id)), Is.EqualTo(0xCDEF8765u | 0x10200080u));
                Call(flags, "ClearFlagBits", I(0x80001004u), block, slot);
                Assert.That(U(block.GetInteger(id)), Is.EqualTo((0xCDEF8765u | 0x10200080u) & ~0x80001004u));
            }
            if (graphOwner)
                for (int index = 0; index < MappedWords.Length; index++)
                    Assert.That(ReadStorage(material, MappedWords[index], true), Is.EqualTo(graphBefore[index]), "MPB must not write Graph Material halves");
        }

        [Test]
        public void NullMaterial_BitSetClearNoOp_CheckThrows_MPBStillWorks()
        {
            var flags = Flags(null);
            Call(flags, "SetFlagBits", int.MinValue, null, 0);
            Call(flags, "ClearFlagBits", int.MinValue, null, 0);
            var error = Assert.Throws<TargetInvocationException>(() => Call(flags, "CheckFlagBits", 1, null, 0));
            Assert.That(error.InnerException, Is.TypeOf<NullReferenceException>());
            var block = new MaterialPropertyBlock();
            Call(flags, "SetFlagBits", int.MinValue, block, 0);
            Assert.That(block.GetInteger(LegacyNames[0]), Is.EqualTo(int.MinValue));
            Assert.That((bool)Call(flags, "CheckFlagBits", int.MinValue, block, 0), Is.True);
        }

        [Test]
        public void SetMaterial_ReevaluatesPhysicalStorage_NoCachedShaderNameRouting()
        {
            var graph = NewMaterial(true); var legacy = NewMaterial(false);
            var flags = Flags(graph);
            WriteStorage(graph, MappedWords[0], 0x12345678u, true);
            Call(flags, "SetFlagBits", int.MinValue, null, 0);
            Assert.That(ReadStorage(graph, MappedWords[0], true), Is.EqualTo(0x92345678u));
            legacy.SetInteger(LegacyNames[0], 0x123);
            Call(flags, "SetMaterial", legacy);
            Call(flags, "SetFlagBits", int.MinValue, null, 0);
            Assert.That(legacy.GetInteger(LegacyNames[0]), Is.EqualTo(I(0x80000123u)));
            Assert.That(ReadStorage(graph, MappedWords[0], true), Is.EqualTo(0x92345678u));
        }

        static IEnumerable<TestCaseData> HalfwordCases()
        {
            foreach (float value in new[] { -17f, -0.5f, 0f, 0.49f, 0.5f, 0.51f, 1.5f, 2.5f, 32767.5f, 65534.5f, 65535f, 65536f, 100000f })
                yield return new TestCaseData(value);
        }
        [TestCaseSource(nameof(HalfwordCases))]
        public void HalfwordFiniteIllegalValues_ClampRound_ReadDoesNotMutate(float value)
        {
            var material = NewMaterial(true); var flags = Flags(material);
            var word = MappedWords[0];
            // Keep signed high-word control while testing each half separately.
            material.SetFloat(word.Lo, value); material.SetFloat(word.Hi, 49153f);
            uint expectedLo = (uint)Mathf.RoundToInt(Mathf.Clamp(value, 0f, 65535f));
            uint expected = expectedLo | (49153u << 16);
            Assert.That(ReadPublicBits(flags, 0), Is.EqualTo(expected));
            Assert.That(material.GetFloat(word.Lo), Is.EqualTo(value), "read must not silently rewrite user storage");
            Assert.That(material.GetFloat(word.Hi), Is.EqualTo(49153f));
            Call(flags, "ClearFlagBits", 0, null, 0);
            Assert.That(ReadStorage(material, word, true), Is.EqualTo(expected));
            Assert.That(material.GetFloat(word.Lo), Is.EqualTo((float)expectedLo));
            material.SetFloat(word.Lo, 21931f); material.SetFloat(word.Hi, value);
            uint expectedHi = expectedLo;
            expected = 21931u | (expectedHi << 16);
            Assert.That(ReadPublicBits(flags, 0), Is.EqualTo(expected));
            Assert.That(material.GetFloat(word.Hi), Is.EqualTo(value));
            Call(flags, "SetFlagBits", 0, null, 0);
            Assert.That(ReadStorage(material, word, true), Is.EqualTo(expected));
            Assert.That(material.GetFloat(word.Hi), Is.EqualTo((float)expectedHi));
            // CPU backend tests the established finite clamp/round rule. This is not a GPU
            // NaN-cast or shader-halfway-tie proof; NBGraphDecodeUInt32's NaN cast is undefined.
        }

        [Test]
        public void HalfwordMeshDecodeProbe_ActualSharedHLSL_AgreesForFiniteEdgesAndTies()
        {
            const string source = "Shader \"Hidden/NBFX/GUI0WordDecodeProbe\" { " +
                "Properties { _InputLo(\"Low\",Float)=0 _InputHi(\"High\",Float)=0 } " +
                "SubShader { Pass { Cull Off ZWrite Off ZTest Always Blend One Zero " +
                "HLSLPROGRAM\n#pragma target 3.5\n#pragma vertex Vert\n#pragma fragment Frag\n" +
                "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n" +
                "#include \"Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl\"\n" +
                "float _InputLo, _InputHi; " +
                "struct A { float4 p:POSITION; }; struct V { float4 p:SV_POSITION; }; " +
                "V Vert(A i) { V o; o.p=float4(i.p.xyz,1); return o; } " +
                "float4 Frag(V i):SV_Target { uint word=NBGraphDecodeUInt32(_InputLo,_InputHi); " +
                "return float4(word&65535u,word>>16u,word>>31u,word&1u); }\nENDHLSL\n} } }";
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "GPU decode must actually run, not silently skip.");
            var shader = ShaderUtil.CreateShaderAsset(source, false);
            var probe = shader == null ? null : new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            var commands = new CommandBuffer { name = "NBFX GUI0 actual HLSL word decode Mesh probe" };
            var previous = RenderTexture.active;
            try
            {
                Assert.That(shader, Is.Not.Null); shader.hideFlags = HideFlags.HideAndDontSave;
                Assert.That(shader.isSupported, Is.True, "HLSL helper must compile for the real device.");
                Assert.That(target.Create(), Is.True);
                mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                var material = NewMaterial(true); var flags = Flags(material); var word = MappedWords[0];
                var pairs = new List<Vector2>();
                foreach (float value in new[] { -17f, -.5f, 0f, .49f, .5f, .51f, 1.5f, 2.5f, 32767.5f, 65534.5f, 65535f, 65536f, 100000f })
                {
                    pairs.Add(new Vector2(value, 49153f)); pairs.Add(new Vector2(21931f, value));
                }
                pairs.Add(new Vector2(0, 0)); pairs.Add(new Vector2(65535, 65535));
                pairs.Add(new Vector2(65535, 32767)); pairs.Add(new Vector2(0, 32768));
                pairs.Add(new Vector2(1, 1)); pairs.Add(new Vector2(32768, 32768));
                Color prior = Color.clear; bool hasPrior = false; bool hasDistinctGPUResponse = false;
                foreach (var pair in pairs)
                {
                    material.SetFloat(word.Lo, pair.x); material.SetFloat(word.Hi, pair.y);
                    uint expected = ReadPublicBits(flags, 0);
                    probe.SetFloat("_InputLo", pair.x); probe.SetFloat("_InputHi", pair.y);
                    commands.Clear(); commands.SetRenderTarget(target); commands.ClearRenderTarget(false, true, Color.magenta);
                    commands.DrawMesh(mesh, Matrix4x4.identity, probe, 0, 0);
                    Graphics.ExecuteCommandBuffer(commands);
                    RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false); readback.Apply(false, false);
                    Color pixel = readback.GetPixel(0, 0);
                    AssertVector(pixel, new Vector4(expected & 65535u, expected >> 16, expected >> 31, expected & 1u), "actual HLSL decode lo=" + pair.x + " hi=" + pair.y);
                    if (hasPrior && pixel != prior) hasDistinctGPUResponse = true;
                    prior = pixel; hasPrior = true;
                    Debug.Log("NBFX_GUI0_MESH_HLSL_WORD lo=" + pair.x + " hi=" + pair.y + " cpu=" + expected + " gpuLo=" + pixel.r + " gpuHi=" + pixel.g + " gpuBit31=" + pixel.b + " gpuBit0=" + pixel.a);
                    // Re-run identical input: steady strict float RGBA and immutable CPU halves.
                    Graphics.ExecuteCommandBuffer(commands);
                    readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false); readback.Apply(false, false);
                    AssertVector(readback.GetPixel(0, 0), pixel, "HLSL repeat");
                    Assert.That(material.GetFloat(word.Lo), Is.EqualTo(pair.x)); Assert.That(material.GetFloat(word.Hi), Is.EqualTo(pair.y));
                }
                Assert.That(hasDistinctGPUResponse, Is.True, "A stuck/inactive GPU fixture is not decode evidence.");
            }
            finally
            {
                RenderTexture.active = previous;
                commands.Release(); target.Release();
                foreach (var owned in new Object[] { mesh, readback, target, probe, shader }) if (owned != null) Object.DestroyImmediate(owned);
            }
            // Only the physical word helper is probed on a Mesh. It is not full Graph surface,
            // MPB-Graph, Controller/Manager, Player, or VFX rendering proof. NaN is deliberately out of scope.
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UV_All16Positions_AllNineModes_PreserveBothWords_AndCheckIsOn(bool graph)
        {
            var material = NewMaterial(graph); var flags = Flags(material);
            for (int position = 0; position < 32; position += 2)
                for (int mode = 0; mode <= 8; mode++)
                {
                    const uint seedLow = 0xA5A55A5Au, seedType = 0xD3E9876Fu;
                    WriteStorage(material, UV, seedLow, graph); WriteStorage(material, UVType, seedType, graph);
                    Call(flags, "SetUVMode", EnumValue("UVMode", mode), position, 0);
                    uint keep = ~(3u << position);
                    Assert.That(ReadStorage(material, UV, graph), Is.EqualTo((seedLow & keep) | ((uint)(mode % 4) << position)));
                    Assert.That(ReadStorage(material, UVType, graph), Is.EqualTo((seedType & keep) | ((uint)(mode / 4) << position)));
                    Assert.That(Convert.ToInt32(Call(flags, "GetUVMode", position, 0)), Is.EqualTo(mode));
                    Assert.That((bool)Call(flags, "CheckIsUVModeOn", EnumValue("UVMode", mode)), Is.True);
                }
            // A genuinely absent nonzero mode must be false, not just a positive getter check.
            WriteStorage(material, UV, 0, graph); WriteStorage(material, UVType, 0, graph);
            Assert.That((bool)Call(flags, "CheckIsUVModeOn", EnumValue("UVMode", 8)), Is.False);
            Call(flags, "SetUVMode", EnumValue("UVMode", 8), 30, 0);
            Assert.That((bool)Call(flags, "CheckIsUVModeOn", EnumValue("UVMode", 8)), Is.True);
            Assert.That(ReadStorage(material, UVType, graph), Is.EqualTo(0x80000000u));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Channel_AllTenConsumerFields_AndWrap_All16UVPairs_NoMip18Bits(bool graph)
        {
            var material = NewMaterial(graph); var flags = Flags(material);
            var channel = MappedWords.Single(w => w.Slot == 6);
            var wrap = MappedWords.Single(w => w.Slot == 2);
            var noMip = MappedWords.Single(w => w.Slot == 8);
            const uint seed = 0xB7C39A65u;
            for (int position = 0; position <= 18; position += 2)
                for (int value = 0; value < 4; value++)
                {
                    WriteStorage(material, channel, seed, graph);
                    Call(flags, "SetColorChanel", EnumValue("ColorChannel", value), position);
                    Assert.That(ReadStorage(material, channel, graph), Is.EqualTo((seed & ~(3u << position)) | ((uint)value << position)));
                    Assert.That(Convert.ToInt32(Call(flags, "GetColorChanel", position)), Is.EqualTo(value));
                }
            // Wrap uses low16 U and high16 V, not adjacent two-bit channel fields.
            for (int index = 0; index < 16; index++)
                for (int mode = 0; mode < 4; mode++)
                {
                    uint u = 1u << index, v = 1u << (index + 16);
                    WriteStorage(material, wrap, seed, graph);
                    Call(flags, (mode & 1) != 0 ? "SetFlagBits" : "ClearFlagBits", I(u), null, 2);
                    Call(flags, (mode & 2) != 0 ? "SetFlagBits" : "ClearFlagBits", I(v), null, 2);
                    uint expected = seed & ~(u | v);
                    if ((mode & 1) != 0) expected |= u;
                    if ((mode & 2) != 0) expected |= v;
                    Assert.That(ReadStorage(material, wrap, graph), Is.EqualTo(expected));
                }
            for (int index = 0; index < 18; index++)
            {
                uint mask = 1u << index;
                WriteStorage(material, noMip, seed, graph);
                Call(flags, "SetFlagBits", I(mask), null, 8);
                Assert.That(ReadStorage(material, noMip, graph), Is.EqualTo(seed | mask));
                Call(flags, "ClearFlagBits", I(mask), null, 8);
                Assert.That(ReadStorage(material, noMip, graph), Is.EqualTo(seed & ~mask));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PNoise_AllFiveThreeBitFields_ZeroThroughFive_NegativeSentinelNoOp(bool graph)
        {
            var material = NewMaterial(graph); var flags = Flags(material);
            var word = MappedWords.Single(w => w.Slot == 7);
            const uint seed = 0xFEDCBA98u;
            for (int position = 0; position <= 12; position += 3)
                for (int mode = 0; mode <= 5; mode++)
                {
                    WriteStorage(material, word, seed, graph);
                    Call(flags, "SetPNoiseBlendMode", EnumValue("PNoiseBlendMode", mode), position);
                    uint expected = (seed & ~(7u << position)) | ((uint)mode << position);
                    Assert.That(ReadStorage(material, word, graph), Is.EqualTo(expected));
                    Assert.That(Convert.ToInt32(Call(flags, "GetPNoiseBlendMode", position)), Is.EqualTo(mode));
                    Call(flags, "SetPNoiseBlendMode", EnumValue("PNoiseBlendMode", -1), position);
                    Assert.That(ReadStorage(material, word, graph), Is.EqualTo(expected));
                }
            // PN1 already supplies the Graph halfword pair. Actual named enum modes remain
            // 0..3; casts 4/5 are storage boundaries with original BlendPNoise default fallback,
            // not new GUI choices or newly implemented effects. High bits (incl.31) stay intact.
        }

        object SharedRoot(params Material[] targets)
        {
            var type = FindType("NBShaderEditor.ShaderGUIRootItem");
            var root = Activator.CreateInstance(type);
            type.GetField("Shader").SetValue(root, targets[0].shader);
            type.GetField("Mats").SetValue(root, new List<Material>(targets));
            var dictionary = (IDictionary)type.GetField("PropertyInfoDic").GetValue(root);
            var infoType = FindType("NBShaderEditor.ShaderPropertyInfo");
            foreach (var property in MaterialEditor.GetMaterialProperties(targets.Cast<Object>().ToArray()))
            {
                var info = Activator.CreateInstance(infoType);
                infoType.GetField("Property").SetValue(info, property);
                infoType.GetField("Name").SetValue(info, property.name);
                int index = -1;
                for (int i = 0; i < targets[0].shader.GetPropertyCount(); i++)
                    if (targets[0].shader.GetPropertyName(i) == property.name) { index = i; break; }
                Assert.That(index, Is.GreaterThanOrEqualTo(0), property.name);
                infoType.GetField("Index").SetValue(info, index);
                dictionary.Add(property.name, info);
            }
            return root;
        }
        static object NewGradientItem(object root, string count, string[] colors, string[] alphas)
        {
            return Activator.CreateInstance(FindType("NBShaderEditor.GradientItem"),
                new object[] { root, null, count, 6, colors, alphas, (Func<GUIContent>)(() => GUIContent.none), false, ColorSpace.Gamma, null });
        }
        static Gradient MakeGradient(int colorCount, int alphaCount)
        {
            var gradient = new Gradient();
            var colors = new GradientColorKey[colorCount]; var alphas = new GradientAlphaKey[alphaCount];
            for (int i = 0; i < colorCount; i++)
            {
                float time = i / (float)(colorCount - 1);
                colors[i] = new GradientColorKey(new Color(time, 1f - time, 0.2f + time * 0.3f), time);
            }
            for (int i = 0; i < alphaCount; i++)
            {
                float time = i / (float)(alphaCount - 1);
                alphas[i] = new GradientAlphaKey(0.15f + 0.7f * time, time);
            }
            gradient.SetKeys(colors, alphas);
            return gradient;
        }
        static void AssertVector(Vector4 actual, Vector4 expected, string message)
        {
            for (int i = 0; i < 4; i++) Assert.That(actual[i], Is.EqualTo(expected[i]), message + " component " + i);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedGradient_PackedRampAndBWCounts_WriteReadReset_IntegerOrFloat(bool graph)
        {
            var material = NewMaterial(graph); var defaults = NewMaterial(graph);
            var root = SharedRoot(material);
            var colors = Enumerable.Range(0, 6).Select(i => "_RampColor" + i).ToArray();
            var alphas = Enumerable.Range(0, 3).Select(i => "_RampColorAlpha" + i).ToArray();
            var ramp = NewGradientItem(root, "_RampColorCount", colors, alphas);
            Assert.That(material.shader.GetPropertyType((int)Field(Field(ramp, "PropertyInfo"), "Index")),
                Is.EqualTo(graph ? ShaderPropertyType.Float : ShaderPropertyType.Int));
            for (int colorCount = 2; colorCount <= 6; colorCount++)
                for (int alphaCount = 2; alphaCount <= 6; alphaCount++)
                {
                    var gradient = MakeGradient(colorCount, alphaCount);
                    Call(ramp, "WriteGradient", gradient);
                    int count = graph ? Mathf.RoundToInt(material.GetFloat("_RampColorCount")) : material.GetInteger("_RampColorCount");
                    Assert.That(count, Is.EqualTo(colorCount | (alphaCount << 16)));
                    var arguments = new object[] { 0, 0 };
                    Call(ramp, "GetGradientKeyCounts", arguments);
                    Assert.That((int)arguments[0], Is.EqualTo(colorCount)); Assert.That((int)arguments[1], Is.EqualTo(alphaCount));
                    var key = gradient.colorKeys[colorCount - 1];
                    AssertVector(material.GetColor(colors[colorCount - 1]), new Vector4(key.color.r, key.color.g, key.color.b, key.time), "ramp last color");
                    Call(ramp, "ReadGradient");
                    var read = (Gradient)Field(ramp, "_gradient");
                    Assert.That(read.colorKeys.Length, Is.EqualTo(colorCount)); Assert.That(read.alphaKeys.Length, Is.EqualTo(alphaCount));
                    Assert.That(read.alphaKeys[alphaCount - 1].alpha, Is.EqualTo(gradient.alphaKeys[alphaCount - 1].alpha));
                }
            Call(ramp, "ExecuteReset", false);
            Assert.That((int)Call(ramp, "ReadCountValue"), Is.EqualTo(graph ? Mathf.RoundToInt(defaults.GetFloat("_RampColorCount")) : defaults.GetInteger("_RampColorCount")));
            Assert.That((bool)Call(ramp, "IsCountDefault"), Is.True);
            foreach (var color in colors) AssertVector(material.GetColor(color), defaults.GetColor(color), "ramp reset " + color);
            foreach (var alpha in alphas) AssertVector(material.GetVector(alpha), defaults.GetVector(alpha), "ramp reset " + alpha);

            var maskAlphas = Enumerable.Range(0, 3).Select(i => "_MaskMapGradientFloat" + i).ToArray();
            var mask = NewGradientItem(root, "_MaskMapGradientCount", Array.Empty<string>(), maskAlphas);
            for (int count = 2; count <= 6; count++)
            {
                Call(mask, "WriteGradient", MakeGradient(count, 2));
                Assert.That((int)Call(mask, "ReadCountValue"), Is.EqualTo(count));
                var arguments = new object[] { 0, 0 };
                Call(mask, "GetGradientKeyCounts", arguments);
                Assert.That((int)arguments[0], Is.EqualTo(count)); Assert.That((int)arguments[1], Is.EqualTo(2));
            }
            Call(mask, "ExecuteReset", false);
            Assert.That((int)Call(mask, "ReadCountValue"), Is.EqualTo(2));
            foreach (var alpha in maskAlphas) AssertVector(material.GetVector(alpha), defaults.GetVector(alpha), "BW reset " + alpha);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedGradient_CountHelper_BoundariesAndPartialPackedPreservation(bool graph)
        {
            var material = NewMaterial(graph); var root = SharedRoot(material);
            var colors = Enumerable.Range(0, 6).Select(i => "_RampColor" + i).ToArray();
            var ramp = NewGradientItem(root, "_RampColorCount", colors, Array.Empty<string>());
            foreach (int count in new[] { 0, 1, 2, 6, 65535, 65536, 131074, 393222 })
            {
                Call(ramp, "WriteCountValue", count);
                Assert.That((int)Call(ramp, "ReadCountValue"), Is.EqualTo(count));
                Assert.That(graph ? material.GetFloat("_RampColorCount") : material.GetInteger("_RampColorCount"), Is.EqualTo((float)count));
            }
            if (graph)
            {
                material.SetFloat("_RampColorCount", 393221.75f);
                Assert.That((int)Call(ramp, "ReadCountValue"), Is.EqualTo(393222));
                Assert.That(material.GetFloat("_RampColorCount"), Is.EqualTo(393221.75f), "count read also must not mutate");
            }
            Call(ramp, "WriteCountValue", 6 << 16 | 3);
            Call(ramp, "WriteGradient", MakeGradient(4, 2));
            Assert.That((int)Call(ramp, "ReadCountValue"), Is.EqualTo(6 << 16 | 4), "color-only writer preserves packed alpha count");
            if (!graph)
            {
                Call(ramp, "WriteCountValue", I(0xFEDCBA98u));
                Assert.That(U((int)Call(ramp, "ReadCountValue")), Is.EqualTo(0xFEDCBA98u), "legacy Integer path must not decode via float");
            }
        }

        static object NewTextureItem(object root, string texture, bool drawST, string vectorST)
        {
            return Activator.CreateInstance(FindType("NBShaderEditor.TextureItem"), new object[]
            { root, null, texture, (Func<GUIContent>)(() => GUIContent.none), null, drawST, null, null, null, null, vectorST });
        }
        [TestCase(false)]
        [TestCase(true)]
        public void SharedTexture_DefaultNativeST_OrOptionalGraphVectorST_Reset_NoMatCapST(bool graph)
        {
            var material = NewMaterial(graph); var root = SharedRoot(material);
            var texture = NewTextureItem(root, "_BaseMap", true, graph ? "_BaseMap_ST" : null);
            var group = Field(texture, "_groupItem"); var st = Field(group, "_scaleOffsetItem");
            Assert.That(st, Is.Not.Null);
            Assert.That((string)Field(st, "PropertyName"), Is.EqualTo(graph ? "_BaseMap_ST" : "_BaseMap"));
            Assert.That((bool)Field(st, "_isVectorProperty"), Is.EqualTo(graph));
            var value = new Vector4(-2.25f, 1.375f, 0.42f, -0.31f);
            Call(st, "Apply", value);
            if (graph) AssertVector(material.GetVector("_BaseMap_ST"), value, "explicit BaseMap vector ST");
            else
            {
                Assert.That(material.GetTextureScale("_BaseMap"), Is.EqualTo(new Vector2(value.x, value.y)));
                Assert.That(material.GetTextureOffset("_BaseMap"), Is.EqualTo(new Vector2(value.z, value.w)));
            }
            Call(st, "ExecuteReset", false);
            if (graph) AssertVector(material.GetVector("_BaseMap_ST"), new Vector4(1, 1, 0, 0), "ST reset");
            else
            {
                Assert.That(material.GetTextureScale("_BaseMap"), Is.EqualTo(Vector2.one));
                Assert.That(material.GetTextureOffset("_BaseMap"), Is.EqualTo(Vector2.zero));
            }
            var noST = NewTextureItem(root, "_MatCapTex", false, null);
            Assert.That(Field(Field(noST, "_groupItem"), "_scaleOffsetItem"), Is.Null, "MatCap intentionally does not apply ST");
            // Even on Graph, a native-ST texture still defaults to the same shared old path.
            var mask = NewTextureItem(root, "_MaskMap", true, null);
            var native = Field(Field(mask, "_groupItem"), "_scaleOffsetItem");
            Assert.That((bool)Field(native, "_isVectorProperty"), Is.False);
            Call(native, "Apply", value);
            Assert.That(material.GetTextureScale("_MaskMap"), Is.EqualTo(new Vector2(value.x, value.y)));
            Assert.That(material.GetTextureOffset("_MaskMap"), Is.EqualTo(new Vector2(value.z, value.w)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MultiMaterial_StorageTransaction_UndoRedo_RestoresIndependentWords(bool graph)
        {
            var a = NewMaterial(graph); var b = NewMaterial(graph);
            var word = MappedWords[0];
            WriteStorage(a, word, 0x12345678u, graph); WriteStorage(b, word, 0x6ABCDE01u, graph);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Undo.SetCurrentGroupName("NBFX GUI0 storage multi-edit test");
                Undo.RecordObjects(new Object[] { a, b }, "NBFX GUI0 storage multi-edit test");
                foreach (var material in new[] { a, b }) Call(Flags(material), "SetFlagBits", int.MinValue, null, 0);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(ReadStorage(a, word, graph), Is.EqualTo(0x92345678u));
                Assert.That(ReadStorage(b, word, graph), Is.EqualTo(0xEABCDE01u));
                Undo.PerformUndo();
                Assert.That(ReadStorage(a, word, graph), Is.EqualTo(0x12345678u));
                Assert.That(ReadStorage(b, word, graph), Is.EqualTo(0x6ABCDE01u));
                Undo.PerformRedo();
                Assert.That(ReadStorage(a, word, graph), Is.EqualTo(0x92345678u));
                Assert.That(ReadStorage(b, word, graph), Is.EqualTo(0xEABCDE01u));
            }
            finally { Undo.RevertAllDownToGroup(group); }
            // This proves caller-owned storage Undo only. Shared control callbacks and sync
            // transaction recording still need GUI-1/2/3; no automatic Undo is invented here.
        }

        [Test]
        public void MultiMaterial_SharedFloatCountAndVectorST_BackendWritesBothTargets()
        {
            var a = NewMaterial(true); var b = NewMaterial(true);
            a.SetFloat("_RampColorCount", 2 | (2 << 16)); b.SetFloat("_RampColorCount", 3 | (4 << 16));
            a.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0)); b.SetVector("_BaseMap_ST", new Vector4(2, 3, .1f, .2f));
            var root = SharedRoot(a, b);
            var colors = Enumerable.Range(0, 6).Select(i => "_RampColor" + i).ToArray();
            var alphas = Enumerable.Range(0, 3).Select(i => "_RampColorAlpha" + i).ToArray();
            var ramp = NewGradientItem(root, "_RampColorCount", colors, alphas);
            var texture = NewTextureItem(root, "_BaseMap", true, "_BaseMap_ST");
            var st = Field(Field(texture, "_groupItem"), "_scaleOffsetItem");
            Call(ramp, "WriteGradient", MakeGradient(5, 6));
            Call(st, "Apply", new Vector4(-3, 4, .15f, -.25f));
            foreach (var material in new[] { a, b })
            {
                Assert.That(material.GetFloat("_RampColorCount"), Is.EqualTo((float)(5 | (6 << 16))));
                AssertVector(material.GetVector("_BaseMap_ST"), new Vector4(-3, 4, .15f, -.25f), "multi vector ST");
            }
        }
    }
}
