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
    // Real MaterialEditor/MaterialProperty/shared Item backend transactions.
    // No visible inspector click or new lighting GPU coverage is claimed here.
    public sealed class G4GraphSharedLightModeGUITests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string Mode = "_FxLightMode", Foldout = "_LightBigBlockItemFoldOut", Marker = "_NB_GraphGUIStateVersion";
        readonly List<Object> owned = new List<Object>();
        string assetFolder;
        Type Type(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        [OneTimeSetUp] public void Preflight()
        {
            GuardIsolation(); AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        static void GuardIsolation()
        {
            Assert.That(Path.GetFullPath(Application.dataPath), Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            { var scene = SceneManager.GetSceneAt(i); Assert.That((scene.name + "/" + scene.path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0)); }
        }
        Material Material()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath); Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material); material.SetFloat(Marker, 2); return material;
        }
        Material Probe(string omitted, bool integerMode = false)
        {
            string source = "Shader \"Hidden/NBFX/SharedLightModeSchema\" { Properties { ";
            foreach (string name in new[] { "_NB_DistortionMode", "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16", Mode, Foldout, "_SixWayColorAbsorptionToggle", Marker })
                if (name != omitted) source += name + "(\"" + name + "\"," + (integerMode && name == Mode ? "Integer" : "Float") + ")=" + (name == Marker ? "2" : "0") + " ";
            source += "} SubShader { Pass { } } }";
            var shader = ShaderUtil.CreateShaderAsset(source, false); Assert.That(shader, Is.Not.Null); shader.hideFlags = HideFlags.HideAndDontSave; owned.Add(shader);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material); return material;
        }
        object Root(params Material[] materials)
        {
            object root = Activator.CreateInstance(Type("NBShaderEditor.NBShaderRootItem")); var type = root.GetType();
            var editor = (MaterialEditor)Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)); owned.Add(editor);
            type.GetField("MatEditor", All).SetValue(root, editor); type.GetField("Mats", All).SetValue(root, materials.ToList()); type.GetField("Shader", All).SetValue(root, materials[0].shader);
            type.GetMethod("InitFlags", All).Invoke(root, new object[] { materials.ToList() });
            var dictionary = (IDictionary)type.GetField("PropertyInfoDic", All).GetValue(root);
            // MaterialEditor retains all actual targets. Its property API receives one
            // same-shader group; production guards still inspect every root.Mats host.
            Assert.That(editor.targets.Length, Is.EqualTo(materials.Length));
            var propertyTargets = materials.Where(m => m.shader == materials[0].shader).Cast<Object>().ToArray();
            foreach (var property in MaterialEditor.GetMaterialProperties(propertyTargets))
            {
                var info = Activator.CreateInstance(Type("NBShaderEditor.ShaderPropertyInfo")); var it = info.GetType();
                it.GetField("Property").SetValue(info, property); it.GetField("Name").SetValue(info, property.name); it.GetField("Index").SetValue(info, materials[0].shader.FindPropertyIndex(property.name)); dictionary.Add(property.name, info);
            }
            return root;
        }
        bool Ready(object root) => (bool)root.GetType().GetMethod("InitializeGraphLightModeInputs", All).Invoke(root, null);
        object Block(object root) => root.GetType().GetField("_graphLightModeBlock", All).GetValue(root);
        object Popup(object root) => ((IList)Block(root).GetType().GetField("ChildrenItemList", All).GetValue(Block(root)))[0];
        bool Select(object root, int value)
        {
            object popup = Popup(root); bool changed = (bool)popup.GetType().GetMethod("CommitSelectedMode", All).Invoke(popup, new object[] { value });
            if (changed) popup.GetType().GetMethod("OnEndChange", All).Invoke(popup, null); return changed;
        }
        void Sync(Material material) => Type("NBShaderEditor.NBShaderGraphGUI").GetMethod("SyncSixWayKeywords", All).Invoke(null, new object[] { material });
        string Snapshot(Material material) => EditorJsonUtility.ToJson(material) + "\n" + string.Join("|", material.shaderKeywords.OrderBy(k => k));
        [TearDown] public void Cleanup()
        {
            foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear();
            if (assetFolder != null)
            {
                GuardIsolation(); string expected = Path.GetFullPath(Path.Combine(Application.dataPath, assetFolder.Substring("Assets/".Length)));
                Assert.That(expected.StartsWith(Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
                AssetDatabase.DeleteAsset(assetFolder); assetFolder = null;
            }
        }
        [TestCase(TestName = "G4SharedLightModeGUI_ActualFactoryOnlyMode_ReadOnlyInit")]
        public void ExistingItemFactoryOnly()
        {
            var material = Material(); material.SetFloat("_NB_Flags0Lo16", -3.75f); string before = Snapshot(material); object root = Root(material);
            Assert.That(Ready(root), Is.True); Assert.That(Block(root).GetType().FullName, Is.EqualTo("NBShaderEditor.BigBlockItem")); Assert.That(Popup(root).GetType().FullName, Is.EqualTo("NBShaderEditor.FxLightModePopupItem"));
            Assert.That(((IList)Block(root).GetType().GetField("ChildrenItemList", All).GetValue(Block(root))).Count, Is.EqualTo(1));
            var names = (IEnumerable<string>)root.GetType().GetMethod("GetSharedGraphPropertyNames", All).Invoke(root, null); Assert.That(names, Is.EquivalentTo(new[] { Mode, Foldout })); Assert.That(Snapshot(material), Is.EqualTo(before));
        }
        [TestCase(Mode, TestName = "G4SharedLightModeGUI_MissingMode")]
        [TestCase(Foldout, TestName = "G4SharedLightModeGUI_MissingFoldout")]
        [TestCase("_SixWayColorAbsorptionToggle", TestName = "G4SharedLightModeGUI_MissingAbsorption")]
        [TestCase(Marker, TestName = "G4SharedLightModeGUI_MissingMarker")]
        public void MissingSchemaRejects(string omitted)
        { var material = Probe(omitted); string before = Snapshot(material); Assert.That(Ready(Root(material)), Is.False); Assert.That(Snapshot(material), Is.EqualTo(before)); }
        [TestCase(TestName = "G4SharedLightModeGUI_IntegerModeRejects")]
        public void WrongTypeRejects() { var material = Probe(null, true); Assert.That(Ready(Root(material)), Is.False); }
        [TestCase(TestName = "G4SharedLightModeGUI_UnknownMarkerRejects")]
        public void UnknownMarkerRejects() { var material = Material(); material.SetFloat(Marker, 3); string before = Snapshot(material); Assert.That(Ready(Root(material)), Is.False); Assert.That(Snapshot(material), Is.EqualTo(before)); }
        [TestCase(float.NaN, TestName = "G4SharedLightModeGUI_NaNModeRejects")]
        [TestCase(-1f, TestName = "G4SharedLightModeGUI_NegativeModeRejects")]
        [TestCase(2.25f, TestName = "G4SharedLightModeGUI_FractionalModeRejects")]
        public void InvalidEnumRejects(float value) { var material = Material(); material.SetFloat(Mode, value); Assert.That(Ready(Root(material)), Is.False); }
        [TestCase(TestName = "G4SharedLightModeGUI_MixedNativeGraphRejects")]
        public void MixedHostRejects()
        { var graph = Material(); var nativeShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader"); Assert.That(nativeShader && nativeShader.isSupported, Is.True); var native = new Material(nativeShader); owned.Add(native); string before = Snapshot(graph); Assert.That(Ready(Root(graph, native)), Is.False); Assert.That(Snapshot(graph), Is.EqualTo(before)); }
        [TestCase(TestName = "G4SharedLightModeGUI_DifferentGraphShadersRejects")]
        public void DifferentShadersReject()
        { var a = Material(); var b = Probe(null); Assert.That(Ready(Root(a, b)), Is.False); }
        [TestCase(TestName = "G4SharedLightModeGUI_MixedFirstValue_MultiUndoRedo")]
        public void MixedValueFirstSelectionIsARealTransaction()
        {
            var a = Material(); var b = Material(); a.SetFloat(Mode, 4); b.SetFloat(Mode, 2); Sync(a); Sync(b); var root = Root(a, b); Assert.That(Ready(root), Is.True);
            a.SetFloat("_NB_CustomDataFlag3Lo16", 65536.25f); float packed = a.GetFloat("_NB_CustomDataFlag3Lo16");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Assert.That(Select(root, 4), Is.True, "Selection equal to first mixed value must still update the other material."); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(a.GetFloat(Mode), Is.EqualTo(4)); Assert.That(b.GetFloat(Mode), Is.EqualTo(4)); Assert.That(a.IsKeywordEnabled("EVALUATE_SH_VERTEX") && b.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.True);
                Assert.That(a.GetFloat("_NB_CustomDataFlag3Lo16"), Is.EqualTo(packed)); Undo.PerformUndo(); Assert.That(a.GetFloat(Mode), Is.EqualTo(4)); Assert.That(b.GetFloat(Mode), Is.EqualTo(2)); Assert.That(a.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.True); Assert.That(b.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.False);
                Undo.PerformRedo(); Assert.That(b.GetFloat(Mode), Is.EqualTo(4)); Assert.That(b.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.True);
            }
            finally { Undo.RevertAllDownToGroup(group); }
        }
        [TestCase(TestName = "G4SharedLightModeGUI_ResetUsesDefaultAndSameSync")]
        public void ResetIsSameSharedItem()
        {
            var material = Material(); material.SetFloat(Mode, 4); Sync(material); var root = Root(material); Assert.That(Ready(root), Is.True); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try { Popup(root).GetType().GetMethod("ExecuteReset", All).Invoke(Popup(root), new object[] { false }); Undo.FlushUndoRecordObjects(); Assert.That(material.GetFloat(Mode), Is.Zero); Assert.That(material.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.False); Undo.PerformUndo(); Assert.That(material.GetFloat(Mode), Is.EqualTo(4)); Assert.That(material.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.True); }
            finally { Undo.RevertAllDownToGroup(group); }
        }
        [TestCase(TestName = "G4SharedLightModeGUI_SaveAndExactReimport")]
        public void RealAssetSaveAndReimport()
        {
            var material = Material(); object root = Root(material); Assert.That(Ready(root), Is.True); Assert.That(Select(root, 4), Is.True);
            material.SetFloat(Foldout, 1); material.SetFloat("_NB_CustomDataFlag3Lo16", 32769); material.hideFlags = HideFlags.None;
            assetFolder = "Assets/NBFXSharedLightModeTest-" + Guid.NewGuid().ToString("N"); GuardIsolation(); Assert.That(AssetDatabase.CreateFolder("Assets", assetFolder.Substring("Assets/".Length)), Is.Not.Empty);
            string path = assetFolder + "/Selected.mat"; AssetDatabase.CreateAsset(material, path); owned.Remove(material); AssetDatabase.SaveAssets(); GuardIsolation(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.That(loaded, Is.Not.Null); Assert.That(loaded.shader, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Shader>(GraphPath))); Assert.That(loaded.GetFloat(Mode), Is.EqualTo(4)); Assert.That(loaded.GetFloat(Foldout), Is.EqualTo(1)); Assert.That(loaded.GetFloat("_NB_CustomDataFlag3Lo16"), Is.EqualTo(32769)); Assert.That(loaded.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.True);
        }
        [TestCase(TestName = "G4SharedLightModeGUI_SameValueNoop")]
        public void SameSelectionIsNoop() { var material = Material(); var root = Root(material); Assert.That(Ready(root), Is.True); string before = Snapshot(material); Assert.That(Select(root, 0), Is.False); Assert.That(Snapshot(material), Is.EqualTo(before)); }
    }
}
