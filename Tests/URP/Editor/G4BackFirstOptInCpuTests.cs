using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    public sealed class G4BackFirstOptInCpuTests
    {
        const string Root = "Packages/com.xuanxuan.nb.fx/Tests/URP/Graphs/NBBackFirst";
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static Type Type(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static object Call(string type, string method, params object[] values)
        {
            var m = Type(type).GetMethod(method, Flags); Assert.That(m, Is.Not.Null, type + "." + method);
            try { return m.Invoke(null, values); }
            catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); throw; }
        }
        [OneTimeSetUp] public void Warm()
        {
            Assert.That(Application.dataPath.Replace('\\', '/'), Is.EqualTo("D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002/Assets").IgnoreCase);
            for (int i = 0; i < SceneManager.sceneCount; ++i)
                Assert.That((SceneManager.GetSceneAt(i).name + "/" + SceneManager.GetSceneAt(i).path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            foreach (string label in new[] { "Legacy", "Modern" })
                AssetDatabase.ImportAsset(Root + label + ".shadergraph", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        Material New(bool modern)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + (modern ? "Modern" : "Legacy") + ".shadergraph");
            Assert.That(shader && shader.isSupported, Is.True); var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m.SetFloat("_NB_GraphGUIStateVersion", 2); m.SetFloat("_Surface", 1); m.SetFloat("_AlphaClip", 0); return m;
        }
        static string State(Material m) => EditorJsonUtility.ToJson(m);
        static bool Migrate(Material m, bool oldMain) => (bool)Call("NBShaderEditor.NBShaderSyncService", "TryMigrateGraphColorPassState", m, oldMain);
        static bool Apply(Material m, bool allow)
        {
            object tier = Enum.ToObject(Type("NBShader.NBShaderFeatureTier"), 3);
            return (bool)Call("NBShaderEditor.NBShaderSyncService", "TryApplyGraphBackFirstPassIntent", m, tier, null, allow ? new[] { "pass.backFirst" } : new string[0]);
        }
        static int Route(Material m)
        {
            object[] args = { m, -1 }; Assert.That((bool)Call("NBShader.NBShaderPassFeatureCatalog", "TryGetGraphColorRouting", args), Is.True); return (int)args[1];
        }
        [Test] public void G4BackFirstOptInCPU_legacy_raw_main_state()
        { var m = New(false); try { Assert.That(Route(m), Is.Zero); m.SetShaderPassEnabled("SRPDefaultUnlit", false); Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"), Is.False); m.SetShaderPassEnabled("SRPDefaultUnlit", true); Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"), Is.True); Assert.That(m.FindPass("NB Back First"), Is.EqualTo(-1)); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_modern_compiled_marker_not_material_override()
        { var m = New(true); try { Assert.That(Route(m), Is.EqualTo(1)); m.SetFloat("_NB_GraphPassRoutingVersion", 0); Assert.That(Route(m), Is.EqualTo(1)); } finally { Object.DestroyImmediate(m); } }
        [TestCase(true, TestName = "G4BackFirstOptInCPU_migrate_main_enabled")]
        [TestCase(false, TestName = "G4BackFirstOptInCPU_migrate_main_disabled")]
        public void MigratePreservesCapturedMain(bool oldMain)
        { var m = New(true); try { Assert.That(Migrate(m, oldMain), Is.True); Assert.That(m.GetShaderPassEnabled("UniversalForward"), Is.EqualTo(oldMain)); Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"), Is.False); Assert.That(m.GetFloat("_NB_BackFirstEffective"), Is.Zero); Assert.That(m.GetFloat("_NB_GraphPassMigrationComplete"), Is.EqualTo(1)); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_migration_idempotent_serialized_noop()
        { var m = New(true); try { Assert.That(Migrate(m, true), Is.True); string before = State(m); Assert.That(Migrate(m, false), Is.False); Assert.That(State(m), Is.EqualTo(before)); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_invalid_migration_marker_rejected()
        { var m = New(true); try { m.SetFloat("_NB_GraphPassMigrationComplete", float.NaN); string before = State(m); Assert.That(Migrate(m, true), Is.False); Assert.That(State(m), Is.EqualTo(before)); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_tier_denied_preserves_main()
        { var m = New(true); try { Migrate(m, false); m.SetFloat("_BackFirstPassToggle", 1); Apply(m, false); Assert.That(m.GetShaderPassEnabled("UniversalForward"), Is.False); Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"), Is.False); Assert.That(m.GetFloat("_NB_BackFirstEffective"), Is.Zero); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_tier_allowed_enables_back_effective()
        { var m = New(true); try { Migrate(m, true); m.SetFloat("_BackFirstPassToggle", 1); Assert.That(Apply(m, true), Is.True); Assert.That(m.GetShaderPassEnabled("UniversalForward"), Is.True); Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"), Is.True); Assert.That(m.GetFloat("_NB_BackFirstEffective"), Is.EqualTo(1)); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_opaque_surface_back_off()
        { var m = New(true); try { Migrate(m, true); m.SetFloat("_BackFirstPassToggle", 1); m.SetFloat("_Surface", 0); Apply(m, true); Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"), Is.False); Assert.That(m.GetFloat("_NB_BackFirstEffective"), Is.Zero); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_UI_mesh_source_back_off()
        { var m = New(true); try { Migrate(m, true); m.SetFloat("_BackFirstPassToggle", 1); m.SetFloat("_MeshSourceMode", 2); Apply(m, true); Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"), Is.False); Assert.That(m.GetFloat("_NB_BackFirstEffective"), Is.Zero); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_legacy_narrow_apply_readonly_reject()
        { var m = New(false); try { string before = State(m); Assert.That(Apply(m, true), Is.False); Assert.That(State(m), Is.EqualTo(before)); } finally { Object.DestroyImmediate(m); } }
        [Test] public void G4BackFirstOptInCPU_unmigrated_modern_back_effective_zero()
        { var m = New(true); try { m.SetFloat("_BackFirstPassToggle", 1); string before = State(m); Assert.That(Apply(m, true), Is.False); Assert.That(State(m), Is.EqualTo(before)); Assert.That(m.GetFloat("_NB_BackFirstEffective"), Is.Zero); } finally { Object.DestroyImmediate(m); } }
    }
}
