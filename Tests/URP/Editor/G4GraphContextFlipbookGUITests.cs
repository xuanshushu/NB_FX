using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphContextFlipbookGUITests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string Graph = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string VAT = "_VAT_Toggle", Flipbook = "_FlipbookBlending", Marker = "_NB_GraphGUIStateVersion";
        readonly List<Object> owned = new List<Object>(); string assetFolder;
        Type Type(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        [OneTimeSetUp] public void Preflight() { Guard(); AssetDatabase.ImportAsset(Graph, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); }
        static void Guard()
        {
            Assert.That(Path.GetFullPath(Application.dataPath), Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for (int i = 0; i < SceneManager.sceneCount; ++i) { var scene = SceneManager.GetSceneAt(i); Assert.That((scene.name + "/" + scene.path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0)); }
        }
        Material Material()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Graph); Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material); material.SetFloat(Marker, 2); return material;
        }
        Material Probe(string omitted, bool integerVAT = false)
        {
            string source = "Shader \"Hidden/NBFX/ContextSchema\" { Properties { ";
            foreach (string name in new[] { "_NB_DistortionMode", "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16", VAT, Flipbook })
                if (name != omitted) source += name + "(\"" + name + "\"," + (integerVAT && name == VAT ? "Integer" : "Float") + ")=0 ";
            source += "} SubShader { Pass { } } }"; var shader = ShaderUtil.CreateShaderAsset(source, false); shader.hideFlags = HideFlags.HideAndDontSave; owned.Add(shader);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material); return material;
        }
        object Root(params Material[] materials)
        {
            var root = Activator.CreateInstance(Type("NBShaderEditor.NBShaderRootItem")); var type = root.GetType(); var editor = (MaterialEditor)Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)); owned.Add(editor);
            type.GetField("MatEditor", All).SetValue(root, editor); type.GetField("Mats", All).SetValue(root, materials.ToList()); type.GetField("Shader", All).SetValue(root, materials[0].shader); type.GetMethod("InitFlags", All).Invoke(root, new object[] { materials.ToList() });
            var dictionary = (IDictionary)type.GetField("PropertyInfoDic", All).GetValue(root);
            foreach (var property in MaterialEditor.GetMaterialProperties(materials.Cast<Object>().ToArray()))
            { var info = Activator.CreateInstance(Type("NBShaderEditor.ShaderPropertyInfo")); var it = info.GetType(); it.GetField("Property").SetValue(info, property); it.GetField("Name").SetValue(info, property.name); it.GetField("Index").SetValue(info, materials[0].shader.FindPropertyIndex(property.name)); dictionary.Add(property.name, info); }
            return root;
        }
        object Context(object root)
        {
            var property = root.GetType().GetProperty("Context", All); if (property.GetValue(root) == null) property.SetValue(root, Activator.CreateInstance(Type("NBShaderEditor.NBShaderGUIContext"), root));
            var context = property.GetValue(root); context.GetType().GetMethod("Refresh", All).Invoke(context, null); return context;
        }
        int State(object root, string name) { var context = Context(root); return Convert.ToInt32(context.GetType().GetProperty(name).GetValue(context)); }
        bool Ready(object root) => (bool)root.GetType().GetMethod("InitializeGraphFlipbookInputs", All).Invoke(root, null);
        object Item(object root) => root.GetType().GetField("_graphFlipbookItem", All).GetValue(root);
        bool Select(object root, bool enabled)
        { var item = Item(root); bool changed = (bool)item.GetType().GetMethod("CommitSelectedFlipbook", All).Invoke(item, new object[] { enabled }); if (changed) item.GetType().GetMethod("OnEndChange", All).Invoke(item, null); return changed; }
        string Snapshot(Material material) => EditorJsonUtility.ToJson(material) + "\n" + string.Join("|", material.shaderKeywords.OrderBy(k => k));
        [TearDown] public void Cleanup()
        {
            foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear();
            if (assetFolder != null) { Guard(); string path = Path.GetFullPath(Path.Combine(Application.dataPath, assetFolder.Substring("Assets/".Length))); Assert.That(path.StartsWith(Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True); AssetDatabase.DeleteAsset(assetFolder); assetFolder = null; }
        }
        [TestCase(false, false, TestName = "G4ContextAnimation_Saved_v0_f0")]
        [TestCase(false, true, TestName = "G4ContextAnimation_Saved_v0_f1")]
        [TestCase(true, false, TestName = "G4ContextAnimation_Saved_v1_f0")]
        [TestCase(true, true, TestName = "G4ContextAnimation_Saved_v1_f1")]
        public void ContextReadsActualIntentWithoutMutation(bool vat, bool flipbook)
        {
            var material = Material(); material.SetFloat(VAT, vat ? 1 : 0); material.SetFloat(Flipbook, flipbook ? 1 : 0); material.SetFloat("_NB_Flags1Lo16", 32768); string before = Snapshot(material); var root = Root(material);
            Assert.That(State(root, "VatEnabled"), Is.EqualTo(vat ? 1 : 0)); Assert.That(State(root, "FlipbookEnabled"), Is.EqualTo(flipbook ? 1 : 0)); Assert.That(State(root, "MeshSourceMode"), Is.EqualTo(1)); Assert.That(State(root, "ParticleMode"), Is.Zero);
            Assert.That(Snapshot(material), Is.EqualTo(before), "Refresh may not normalize both-on or treat Helper bit15 as the Flipbook toggle.");
        }
        [TestCase(TestName = "G4ContextAnimation_MixedActualMaterials")]
        public void ContextMixed() { var a = Material(); var b = Material(); a.SetFloat(VAT, 1); b.SetFloat(Flipbook, 1); var root = Root(a, b); Assert.That(State(root, "VatEnabled"), Is.EqualTo(-1)); Assert.That(State(root, "FlipbookEnabled"), Is.EqualTo(-1)); }
        [TestCase(VAT, TestName = "G4ContextAnimation_MissingVAT_IsUnknown")]
        [TestCase(Flipbook, TestName = "G4ContextAnimation_MissingFlipbook_IsUnknown")]
        public void MissingContextSchemaIsUnknown(string name) { var material = Probe(name); var root = Root(material); Assert.That(State(root, name == VAT ? "VatEnabled" : "FlipbookEnabled"), Is.EqualTo(-1)); }
        [TestCase(TestName = "G4ContextAnimation_IntegerVAT_IsUnknown")]
        public void WrongContextTypeIsUnknown() { var material = Probe(null, true); Assert.That(State(Root(material), "VatEnabled"), Is.EqualTo(-1)); }
        [TestCase(TestName = "G4SharedFlipbook_ExistingItem_ReadOnlyFactory")]
        public void ExistingItemFactory()
        { var material = Material(); string before = Snapshot(material); var root = Root(material); Assert.That(Ready(root), Is.True); Assert.That(Item(root).GetType().FullName, Is.EqualTo("NBShaderEditor.FlipbookFeatureItem")); Assert.That(Snapshot(material), Is.EqualTo(before)); }
        [TestCase(TestName = "G4SharedFlipbook_EnableDisablesVAT_CompleteUndoRedo_V2DeclaredKeywordBaseline")]
        public void OriginalMutualExclusionIsOneUserTransaction()
        {
            var material = Material();
            material.SetFloat(VAT, 1); material.SetFloat("_NB_CustomDataFlag2Lo16", 32769);
            var vatKeyword = material.shader.keywordSpace.FindKeyword("_VAT");
            bool beforeEnable = material.IsKeywordEnabled("_VAT");
            material.EnableKeyword("_VAT");
            bool originalKeywordBaseline = material.IsKeywordEnabled("_VAT");
            Assert.That(originalKeywordBaseline, Is.EqualTo(vatKeyword.isValid ? true : beforeEnable),
                "Declared VAT must enable; undeclared EnableKeyword must preserve the real prior query.");
            if (vatKeyword.isValid) Assert.That(material.IsKeywordEnabled(vatKeyword), Is.True, "Declared native-compatible VAT local API must enable.");
            var root = Root(material); Assert.That(Ready(root), Is.True);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Assert.That(Select(root, true), Is.True); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(material.GetFloat(Flipbook), Is.EqualTo(1)); Assert.That(material.GetFloat(VAT), Is.Zero);
                Assert.That(material.IsKeywordEnabled("_VAT"), Is.False);
                if (vatKeyword.isValid) Assert.That(material.IsKeywordEnabled(vatKeyword), Is.False, "Declared VAT must close with original mutual exclusion.");
                Assert.That(material.GetFloat("_NB_CustomDataFlag2Lo16"), Is.EqualTo(32769));
                Undo.PerformUndo();
                Assert.That(material.GetFloat(Flipbook), Is.Zero); Assert.That(material.GetFloat(VAT), Is.EqualTo(1));
                Assert.That(material.GetFloat("_NB_CustomDataFlag2Lo16"), Is.EqualTo(32769), "Undo must preserve unrelated split flags.");
                Assert.That(material.IsKeywordEnabled("_VAT"), Is.EqualTo(originalKeywordBaseline), "Undo restores the actual pre-edit keyword baseline.");
                if (vatKeyword.isValid) Assert.That(material.IsKeywordEnabled(vatKeyword), Is.EqualTo(originalKeywordBaseline), "Declared local VAT Undo must retain original native-compatible state.");
                Undo.PerformRedo();
                Assert.That(material.GetFloat(VAT), Is.Zero); Assert.That(material.GetFloat(Flipbook), Is.EqualTo(1));
                Assert.That(material.GetFloat("_NB_CustomDataFlag2Lo16"), Is.EqualTo(32769));
                Assert.That(material.IsKeywordEnabled("_VAT"), Is.False);
                if (vatKeyword.isValid) Assert.That(material.IsKeywordEnabled(vatKeyword), Is.False);
            }
            finally { Undo.RevertAllDownToGroup(group); }
        }
        [TestCase(TestName = "G4SharedFlipbook_MixedFirstValueUpdatesAll")]
        public void MixedFirstValueSelection()
        { var a = Material(); var b = Material(); a.SetFloat(Flipbook, 1); b.SetFloat(VAT, 1); var root = Root(a, b); Assert.That(Ready(root), Is.True); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); try { Assert.That(Select(root, true), Is.True); Assert.That(b.GetFloat(Flipbook), Is.EqualTo(1)); Assert.That(a.GetFloat(VAT) + b.GetFloat(VAT), Is.Zero); } finally { Undo.RevertAllDownToGroup(group); } }
        [TestCase(TestName = "G4SharedFlipbook_SameValueNoop")]
        public void SameValueNoop() { var material = Material(); var root = Root(material); Assert.That(Ready(root), Is.True); string before = Snapshot(material); Assert.That(Select(root, false), Is.False); Assert.That(Snapshot(material), Is.EqualTo(before)); }
        [TestCase(TestName = "G4SharedFlipbook_UnknownMarkerRejects")]
        public void UnknownMarkerRejects() { var material = Material(); material.SetFloat(Marker, 3); string before = Snapshot(material); Assert.That(Ready(Root(material)), Is.False); Assert.That(Snapshot(material), Is.EqualTo(before)); }
        [TestCase(TestName = "G4SharedFlipbook_NonFiniteBlendVectorRejects")]
        public void CorruptHelperSchemaRejects() { var material = Material(); material.SetVector("_BaseMap_AnimationSheetBlend_ST", new Vector4(float.NaN, 1, 0, 0)); Assert.That(Ready(Root(material)), Is.False); }
        [TestCase(TestName = "G4SharedFlipbook_GeneralGraphSyncStillProtected")]
        public void GeneralSyncRemainsProtected()
        { var material = Material(); material.SetFloat(VAT, 1); material.SetFloat(Flipbook, 1); var root = Root(material); Assert.That(Ready(root), Is.True); var sync = root.GetType().GetProperty("SyncService", All).GetValue(root); string before = Snapshot(material); sync.GetType().GetMethod("SyncMaterialState", All, null, System.Type.EmptyTypes, null).Invoke(sync, null); Assert.That(Snapshot(material), Is.EqualTo(before)); }
        [TestCase(TestName = "G4SharedFlipbook_SaveReimportActualMaterial")]
        public void MaterialReopens()
        {
            var material = Material(); var root = Root(material); Assert.That(Ready(root), Is.True); Assert.That(Select(root, true), Is.True); material.SetFloat("_NB_CustomDataFlag3Lo16", 32769); material.hideFlags = HideFlags.None;
            Guard(); assetFolder = "Assets/NBFXContextFlipbookTest-" + Guid.NewGuid().ToString("N"); Assert.That(AssetDatabase.CreateFolder("Assets", assetFolder.Substring("Assets/".Length)), Is.Not.Empty); string path = assetFolder + "/Animation.mat";
            AssetDatabase.CreateAsset(material, path); owned.Remove(material); AssetDatabase.SaveAssets(); Guard(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); var loaded = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.That(loaded.GetFloat(Flipbook), Is.EqualTo(1)); Assert.That(loaded.GetFloat(VAT), Is.Zero); Assert.That(loaded.GetFloat("_NB_CustomDataFlag3Lo16"), Is.EqualTo(32769)); Assert.That(State(Root(loaded), "FlipbookEnabled"), Is.EqualTo(1));
        }
        [TestCase(TestName = "G4SharedFlipbook_HelperLifecycleOwnsBit15AndST")]
        public void ToggleDoesNotTakeOverExistingHelperLifecycle()
        {
            var material = Material(); var go = new GameObject("Existing AnimationSheetHelper / shared UI"); owned.Add(go); go.SetActive(false); var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            var helperType = Type("AnimationSheetHelper"); var helper = go.AddComponent(helperType); helperType.GetField("xSize").SetValue(helper, 2); helperType.GetField("ySize").SetValue(helper, 2); helperType.GetField("manualPlay").SetValue(helper, true); go.SetActive(true);
            uint BeforeWord() => (uint)Mathf.RoundToInt(material.GetFloat("_NB_Flags1Lo16")) | ((uint)Mathf.RoundToInt(material.GetFloat("_NB_Flags1Hi16")) << 16);
            Assert.That((BeforeWord() & 32768u) != 0, Is.True); var st = material.GetVector("_BaseMap_ST"); var next = material.GetVector("_BaseMap_AnimationSheetBlend_ST"); var root = Root(material); Assert.That(Ready(root), Is.True); Assert.That(Select(root, true), Is.True);
            Assert.That((BeforeWord() & 32768u) != 0, Is.True); Assert.That(material.GetVector("_BaseMap_ST"), Is.EqualTo(st)); Assert.That(material.GetVector("_BaseMap_AnimationSheetBlend_ST"), Is.EqualTo(next));
            go.SetActive(false); Assert.That((BeforeWord() & 32768u) == 0, Is.True); Assert.That(material.GetFloat(Flipbook), Is.EqualTo(1));
        }
    }
}
