using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    // Installed Graph generation and imported-shader SRP compatibility only.
    // These are not rendered-image, runtime, stripping, or performance tests.
    public sealed class G4GraphOverrideDepthSourceTests
    {
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        const string GraphPath = Package + "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string ForwardPath = "NBShaders2/ShaderGraph/Passes/NBGraphForwardPass.hlsl";
        const string AdapterPath = "NBShaders2/ShaderGraph/NBGraphOverrideDepth.hlsl";
        const string SharedPath = "NBShaders2/Shader/HLSL/NBShaderOverrideDepthV1.hlsl";
        const BindingFlags StaticMethods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Serializable] sealed class FileHash { public string path, sha256; }
        [Serializable] sealed class PassRow
        {
            public string name, lightMode;
            public string[] overridePragmas;
            [NonSerialized] public string source;
        }
        [Serializable] sealed class SourceReport
        {
            public string scope, unity, api, resolvedPackage, generatedSHA256;
            public FileHash[] inputFiles;
            public PassRow[] passes;
        }
        [Serializable] sealed class CompatibilityRow
        {
            public string role, assetPath, shaderName, reason, error;
            public int subShader = 0, code = -1;
            public bool known, supported;
        }
        [Serializable] sealed class CompatibilityReport
        {
            public string scope, unity, api, codeSignature, reasonSignature, apiError;
            public bool apiKnown;
            public CompatibilityRow[] shaders;
        }

        static string Evidence()
        {
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXOverrideZSource");
            string folder = Path.Combine(root, "overridez-source");
            Directory.CreateDirectory(folder);
            return folder;
        }

        static string SHA(byte[] bytes)
        {
            using (var algorithm = SHA256.Create())
                return string.Concat(algorithm.ComputeHash(bytes).Select(b => b.ToString("x2")));
        }

        static void Save(string name, object report)
        {
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.Combine(Evidence(), name), json, new UTF8Encoding(false));
            Debug.Log("NBFX_OVERRIDEZ_SOURCE " + json);
        }

        static int MatchingBrace(string text, int open)
        {
            int depth = 0;
            bool quoted = false, lineComment = false, blockComment = false;
            for (int i = open; i < text.Length; ++i)
            {
                char c = text[i], next = i + 1 < text.Length ? text[i + 1] : '\0';
                if (lineComment) { if (c == '\n') lineComment = false; continue; }
                if (blockComment) { if (c == '*' && next == '/') { blockComment = false; ++i; } continue; }
                if (quoted) { if (c == '\\') ++i; else if (c == '"') quoted = false; continue; }
                if (c == '"') { quoted = true; continue; }
                if (c == '/' && next == '/') { lineComment = true; ++i; continue; }
                if (c == '/' && next == '*') { blockComment = true; ++i; continue; }
                if (c == '{') ++depth;
                if (c == '}' && --depth == 0) return i;
            }
            Assert.Fail("Generated ShaderLab has an unclosed Pass block.");
            return -1;
        }

        static PassRow[] ReadPasses(string shader)
        {
            var rows = new List<PassRow>();
            foreach (Match match in Regex.Matches(shader, @"(?m)^[ \t]*Pass\s*\{"))
            {
                int open = shader.IndexOf('{', match.Index);
                string body = shader.Substring(open + 1, MatchingBrace(shader, open) - open - 1);
                var name = Regex.Match(body, "\\bName\\s+\"([^\"]+)\"");
                Assert.That(name.Success, Is.True, "Generated Pass has no Name.");
                var lightMode = Regex.Match(body, "\"LightMode\"\\s*=\\s*\"([^\"]+)\"");
                rows.Add(new PassRow {
                    name = name.Groups[1].Value,
                    lightMode = lightMode.Success ? lightMode.Groups[1].Value : "<None>",
                    overridePragmas = Regex.Matches(body, @"(?m)^\s*#\s*pragma[^\r\n]*\b_OVERRIDE_Z\b[^\r\n]*")
                        .Cast<Match>().Select(m => m.Value.Trim()).ToArray(),
                    source = body
                });
            }
            return rows.ToArray();
        }

        [Test]
        public void G4OverrideZSource_generated_pass_scope()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(GraphPath);
            Assert.That(packageInfo, Is.Not.Null, "Installed NB_FX package is unknown.");
            Assert.That(packageInfo.resolvedPath, Is.Not.Null.And.Not.Empty);
            var graphAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.ShaderGraph.Editor");
            var importer = graphAssembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter", true);
            // Same SG17.3 test-only API already exercised by G4GraphChromaticTests.
            var generate = importer.GetMethods(StaticMethods).Single(m => m.Name == "GetShaderText" &&
                m.ReturnType == typeof(string) && m.GetParameters().Length == 4 && m.GetParameters()[3].IsOut);
            object[] arguments = { GraphPath, null, null, null };
            string generated = (string)generate.Invoke(null, arguments);
            Assert.That(arguments[3], Is.Not.Null, "Importer returned no real GraphData.");
            Assert.That(generated, Is.Not.Null.And.Not.Empty);
            File.WriteAllText(Path.Combine(Evidence(), "generated-installed-overridez.shader"), generated, new UTF8Encoding(false));
            var passes = ReadPasses(generated);
            var paths = new[] { "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph", ForwardPath, AdapterPath, SharedPath };
            var hashes = paths.Select(p => new FileHash {
                path = p, sha256 = SHA(File.ReadAllBytes(Path.Combine(packageInfo.resolvedPath, p)))
            }).ToArray();
            Save("generated-source.json", new SourceReport {
                scope = "Installed Mesh Graph generated Pass/pragma/source checks only; no image/runtime/Player/performance claim.",
                unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(),
                resolvedPackage = packageInfo.resolvedPath, generatedSHA256 = SHA(new UTF8Encoding(false).GetBytes(generated)),
                inputFiles = hashes, passes = passes
            });
            var forward = passes.Where(p => p.name == "Universal Forward").ToArray();
            Assert.That(forward.Length, Is.EqualTo(1), "Expected one normal Mesh Forward.");
            Assert.That(forward[0].overridePragmas.Length, Is.EqualTo(1));
            Assert.That(Regex.IsMatch(forward[0].overridePragmas[0], @"^#\s*pragma\s+shader_feature_local_fragment\b"), Is.True);
            Assert.That(forward[0].source, Does.Contain(Package + ForwardPath));
            Assert.That(forward[0].source, Does.Contain("NBOverrideDeviceDepth"));
            Assert.That(forward[0].source, Does.Contain(Package + AdapterPath));
            foreach (var pass in passes.Where(p => p.name != "Universal Forward"))
                Assert.That(pass.overridePragmas, Is.Empty, pass.name + " unexpectedly declares an OverrideZ axis.");
            foreach (string name in new[] { "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass" })
                Assert.That(passes.Count(p => p.name == name), Is.EqualTo(1), "Missing/duplicate excluded Pass " + name);

            string wrapper = File.ReadAllText(Path.Combine(packageInfo.resolvedPath, ForwardPath));
            Assert.That(Regex.IsMatch(wrapper, @"#if\s+defined\s*\(\s*_OVERRIDE_Z\s*\)\s*,\s*out\s+float\s+outDepth\s*:\s*SV_Depth\s*#endif"), Is.True);
            Assert.That(Regex.IsMatch(wrapper, @"#if\s+defined\s*\(\s*_OVERRIDE_Z\s*\)\s*outDepth\s*=\s*surface\.NBOverrideDeviceDepth\s*;\s*#endif"), Is.True);
            string adapter = File.ReadAllText(Path.Combine(packageInfo.resolvedPath, AdapterPath));
            Assert.That(adapter, Does.Contain("#include \"" + Package + SharedPath + "\""));
            Assert.That(Regex.IsMatch(adapter, @"DeviceDepth\s*=\s*0\s*;\s*#if\s+defined\s*\(\s*_OVERRIDE_Z\s*\)\s*DeviceDepth\s*=\s*NBFX_OverrideZDeviceDepthV1"), Is.True);
            Assert.That(adapter, Does.Not.Contain("PixelPosition.z"));
            Assert.That(Regex.IsMatch(adapter, @"\bif\s*\(\s*OverrideZToggle"), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(packageInfo.resolvedPath, SharedPath)), Does.Contain("float NBFX_OverrideZDeviceDepthV1"));
        }

        [Test]
        public void G4OverrideZSource_srp_batcher_codes()
        {
            var report = new CompatibilityReport {
                scope = "Native imported ShaderUtil subShader0 SRP Batcher compatibility. Code0=compatible as confirmed from this Editor's ShaderInspector IL; unknown is failure. No runtime/performance claim.",
                unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(),
                shaders = new[] {
                    new CompatibilityRow { role = "frozen", assetPath = Package + "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader" },
                    new CompatibilityRow { role = "current", assetPath = Package + "NBShaders2/Shader/NBShader.shader" },
                    new CompatibilityRow { role = "graph", assetPath = GraphPath }
                }
            };
            MethodInfo code = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", StaticMethods, null, new[] { typeof(Shader), typeof(int) }, null);
            MethodInfo reason = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", StaticMethods, null, new[] { typeof(Shader), typeof(int), typeof(int) }, null);
            report.apiKnown = code != null && code.ReturnType == typeof(int) && reason != null && reason.ReturnType == typeof(string);
            report.codeSignature = code == null ? "missing" : code.ToString();
            report.reasonSignature = reason == null ? "missing" : reason.ToString();
            if (!report.apiKnown) report.apiError = "Required current-version ShaderUtil API signature is missing/unknown.";
            foreach (var row in report.shaders)
            {
                try
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(row.assetPath);
                    if (shader == null) throw new InvalidOperationException("Imported shader is missing.");
                    row.shaderName = shader.name;
                    row.supported = shader.isSupported;
                    if (!report.apiKnown) throw new InvalidOperationException(report.apiError);
                    row.code = (int)code.Invoke(null, new object[] { shader, 0 });
                    if (row.code != 0) row.reason = (string)reason.Invoke(null, new object[] { shader, 0, row.code });
                    if (row.code < 0 || (row.code != 0 && string.IsNullOrEmpty(row.reason)))
                        throw new InvalidOperationException("Native compatibility status/reason is unknown.");
                    row.known = true;
                }
                catch (Exception ex) { row.error = ex.ToString(); }
            }
            Save("srp-batcher-subshader0.json", report);
            Assert.That(report.apiKnown, Is.True, report.apiError);
            foreach (var row in report.shaders)
            {
                Assert.That(row.known, Is.True, row.role + ": " + row.error);
                Assert.That(row.supported, Is.True, row.role + " imported shader is unsupported.");
            }
            var frozen = report.shaders[0];
            var current = report.shaders[1];
            var graph = report.shaders[2];
            // Preserve real baseline failures. They cannot make an incompatible
            // Graph pass. Current may preserve the same known issue or improve.
            if (frozen.code == 0) Assert.That(current.code, Is.Zero, current.reason);
            else Assert.That(current.code == 0 || (current.code == frozen.code && current.reason == frozen.reason), Is.True,
                "Current introduced a different SRP incompatibility. Frozen: " + frozen.reason + "; current: " + current.reason);
            Assert.That(graph.code, Is.Zero, "Graph is SRP Batcher incompatible: " + graph.reason);
        }
    }
}
