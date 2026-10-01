"""Prepare existing GUI tests for the documented schema1->2 seed migration.

Output only to ignored preview space. No product/test installation or Unity run.
All original raw/state/Undo comparisons stay; only the newly seeded UI fields
and marker are allowed during the migration, with independent exact seed checks.
"""
from pathlib import Path
import json

work = Path(__file__).resolve().parent
package = work.parents[1] / 'Packages' / 'NB_FX'
source = package / 'Tests/URP/Editor/G4GraphGuiMainTextureTests.cs'
text = source.read_text(encoding='utf-8')

helper = '''        static string[] GraphSeedChangedNames(Material material)
        {
            var syncType = FindType("NBShaderEditor.NBShaderSyncService");
            var schema = syncType.GetMethod("GraphFlagIntentSchemaAvailable", BindingFlags.Static | BindingFlags.NonPublic);
            if (schema == null || !(bool)schema.Invoke(null, new object[] { material })) return new[] { Version };
            var result = new List<string> { Version };
            foreach (string table in new[] { "ToggleFlagBindings", "ModeFlagBindings" })
                foreach (object binding in (Array)syncType.GetField(table, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
                    result.Add((string)Field(binding, "propertyName"));
            Assert.That(result.Count, Is.EqualTo(30), "Same authority: 29 new UI mirrors plus marker");
            return result.ToArray();
        }

        static float GraphSeedVersion(Material material) => GraphSeedChangedNames(material).Length == 30 ? 2f : 1f;

        static void AssertGraphSeedMirrors(Material material)
        {
            if (GraphSeedVersion(material) < 2f) return;
            var syncType = FindType("NBShaderEditor.NBShaderSyncService");
            foreach (string table in new[] { "ToggleFlagBindings", "ModeFlagBindings" })
                foreach (object binding in (Array)syncType.GetField(table, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
                {
                    string name = (string)Field(binding, "propertyName");
                    int bits = (int)Field(binding, "flagBits"), index = (int)Field(binding, "flagIndex");
                    string prefix = index == 0 ? "_NB_Flags0" : "_NB_Flags1";
                    uint low = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix + "Lo16"), 0, 65535));
                    uint high = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(prefix + "Hi16"), 0, 65535));
                    bool on = (((high << 16) | low) & unchecked((uint)bits)) != 0;
                    int enabled = table == "ToggleFlagBindings" ? 1 : (int)Field(binding, "enabledMode");
                    int disabled = table == "ToggleFlagBindings" ? 0 : enabled == 0 ? 1 : 0;
                    Assert.That(material.GetFloat(name), Is.EqualTo(on ? enabled : disabled), "Exact UI seed " + name);
                }
        }

'''
anchor = '        [TestCase(false)] [TestCase(true)]\n        public void GUI1A_VersionSeed_ChangesOnlyMarker_EightRawWordsAreNeverReencoded'
assert text.count(anchor) == 1
text = text.replace(anchor, helper + anchor)
text = text.replace('Assert.That(material.GetFloat(Version), Is.EqualTo(1f)); before.AssertSame(material, "Version-only seed", Version);',
                    'Assert.That(material.GetFloat(Version), Is.EqualTo(GraphSeedVersion(material))); AssertGraphSeedMirrors(material); before.AssertSame(material, "Version/UI-only seed", GraphSeedChangedNames(material));')
text = text.replace('aa.AssertSame(a, "Independent unseeded target", Version);',
                    'aa.AssertSame(a, "Independent unseeded target", GraphSeedChangedNames(a)); AssertGraphSeedMirrors(a);')
text = text.replace('Assert.That(a.GetFloat(Version), Is.EqualTo(1f)); Assert.That(b.GetFloat(Version), Is.EqualTo(4f));',
                    'Assert.That(a.GetFloat(Version), Is.EqualTo(GraphSeedVersion(a))); Assert.That(b.GetFloat(Version), Is.EqualTo(4f));')
text = text.replace('Assert.That(a.GetFloat(Version), Is.EqualTo(1f)); Assert.That(b.GetFloat(Version), Is.EqualTo(1f));',
                    'Assert.That(a.GetFloat(Version), Is.EqualTo(GraphSeedVersion(a))); Assert.That(b.GetFloat(Version), Is.EqualTo(GraphSeedVersion(b))); AssertGraphSeedMirrors(a); AssertGraphSeedMirrors(b);')
text = text.replace('aa.AssertSame(a, "Seed redo A", Version); bb.AssertSame(b, "Seed redo B", Version);',
                    'aa.AssertSame(a, "Seed redo A", GraphSeedChangedNames(a)); bb.AssertSame(b, "Seed redo B", GraphSeedChangedNames(b));')
# Keep historic IDs for source-to-source comparison; the expanded migration
# semantics are documented, and the original raw/future/mixed/Undo checks remain.
output = work / 'gui1b' / 'adapted-tests'
output.mkdir(parents=True, exist_ok=True)
(output / source.name).write_text(text, encoding='utf-8', newline='\n')
candidate = package / 'Documentation~/reports/handoff-20261001/candidates/gui1b/tests/G4GraphGuiFeatureIntentTests.cs'
new = candidate.read_text(encoding='utf-8')
guard = '''            string explicitClone = Environment.GetEnvironmentVariable("NBFX_ISOLATED_PROJECT_DIR");
            bool explicitlyOwnedClone = !string.IsNullOrEmpty(explicitClone) &&
                string.Equals(project, Path.GetFullPath(explicitClone).Replace('\\\\', '/'), StringComparison.Ordinal) &&
                string.Equals(Path.GetFileName(Path.GetDirectoryName(project)), ".utmp", StringComparison.Ordinal);
'''
old = '            Assert.That(project.StartsWith("/tmp/", StringComparison.Ordinal) || project.StartsWith("/private/tmp/", StringComparison.Ordinal), Is.True,'
assert new.count(old) == 1
new = new.replace(old, guard + old.replace('), Is.True,', ') || explicitlyOwnedClone, Is.True,'))
(output / candidate.name).write_text(new, encoding='utf-8', newline='\n')
print(json.dumps({'scope':'GUI1B test previews only', 'rawUndoAuthorityAssertionsPreserved':True,
                  'newUiMirrorsExplicitlyVerified':True, 'portableOwnedCloneGuard':True,
                  'installed':False,'ranUnity':False}))
