using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;

// Diagnostic only: copy into isolated clone Assets/Editor, never the product package.
public static class NBFXT08AlphaStaircasePlayerBuild
{
    const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
    const string ScenePath = "Assets/NBFXT08AlphaStaircasePlayer.unity";
    const string OutputRoot = "/tmp/nbfx-vfx-alpha-staircase-20260930";
    const string PlayerPath = OutputRoot + "/player/AlphaStaircase.app";
    static readonly string[] Paths = { "Assets/NBGraphVFXMeshAlpha100.vfx",
        "Assets/NBGraphVFXMeshAlpha095.vfx", "Assets/NBGraphVFXMeshAlpha050.vfx" };

    public static void Run()
    {
        Directory.CreateDirectory(OutputRoot);
        foreach (var path in Paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var alpha100 = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Paths[0]);
        var alpha095 = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Paths[1]);
        var alpha050 = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Paths[2]);
        var graph = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        var shader100 = GetGeneratedShader(Paths[0]);
        var shader095 = GetGeneratedShader(Paths[1]);
        var shader050 = GetGeneratedShader(Paths[2]);
        if (!alpha100 || !alpha095 || !alpha050 || !graph || !shader100 || !shader095 || !shader050)
            throw new Exception("Alpha staircase import assets absent");
        using (var report = new StreamWriter(OutputRoot + "/editor-material-inspection.txt"))
        {
            Inspect(Paths[0], "alpha100", report);
            Inspect(Paths[1], "alpha095", report);
            Inspect(Paths[2], "alpha050", report);
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var host = new GameObject("Alpha Staircase Probe");
        var probe = host.AddComponent<NBFXT08AlphaStaircasePlayerProbe>();
        probe.alpha100Asset = alpha100;
        probe.alpha095Asset = alpha095;
        probe.alpha050Asset = alpha050;
        probe.graphShader = graph;
        probe.alpha100Shader = shader100;
        probe.alpha095Shader = shader095;
        probe.alpha050Shader = shader050;
        EditorSceneManager.SaveScene(scene, ScenePath);
        Directory.CreateDirectory(Path.GetDirectoryName(PlayerPath));
        var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, locationPathName = PlayerPath,
            target = BuildTarget.StandaloneOSX, options = BuildOptions.None
        });
        Debug.Log("NBFX_T08_ALPHA_STAIRCASE_BUILD result=" + build.summary.result +
            " errors=" + build.summary.totalErrors + " warnings=" + build.summary.totalWarnings);
        if (build.summary.result != BuildResult.Succeeded)
            throw new Exception("Alpha staircase Player build failed");
    }
    static Shader GetGeneratedShader(string path)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Shader shader) return shader;
        return null;
    }
    static void DumpMaterial(Material material, string label, TextWriter report)
    {
        report.WriteLine(label + " shader=" + material.shader.name + " queue=" + material.renderQueue +
            " _Surface=" + GetFloat(material, "_Surface") + " _AlphaClip=" + GetFloat(material, "_AlphaClip") +
            " _SrcBlend=" + GetFloat(material, "_SrcBlend") + " _DstBlend=" + GetFloat(material, "_DstBlend") +
            " opaquePass=" + material.FindPass("NBCameraOpaqueDistortPass") +
            " deferredPass=" + material.FindPass("NBDeferredDistortPass") +
            " keywords=" + string.Join(",", material.shaderKeywords));
    }
    static string GetFloat(Material material, string property)
    {
        return material.HasProperty(property) ? material.GetFloat(property).ToString("R") : "MISSING";
    }
    static void Inspect(string path, string label, TextWriter report)
    {
        report.WriteLine("case=" + label + " path=" + path);
        bool materialFound = false;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Material material)
            {
                materialFound = true;
                DumpMaterial(material, "imported-material", report);
            }
        }
        if (!materialFound) report.WriteLine("imported-material=NOT_EXPOSED_AS_ASSET (no runtime material inference)");
        var shader = GetGeneratedShader(path);
        var defaultMaterial = new Material(shader);
        try { DumpMaterial(defaultMaterial, "fresh-default-material", report); }
        finally { UnityEngine.Object.DestroyImmediate(defaultMaterial); }
        // Save generated sources for exact pass/render state and alpha-keyword inspection.
        var resourceType = FindResourceType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var resource = resourceType.GetMethod("GetResourceAtPath", flags).Invoke(null, new object[] { path });
        // The VFX output's own hidden material is more informative than a new
        // Material(shader). Reflection is test-only and failure stays visible in
        // the report rather than aborting the independent Player screenshot test.
        try
        {
            var graphProperty = resourceType.GetProperty("graph", flags);
            var graph = graphProperty != null ? graphProperty.GetValue(resource, null) : null;
            var childrenProperty = graph != null ? graph.GetType().GetProperty("children", flags) : null;
            var children = childrenProperty != null ? childrenProperty.GetValue(graph, null) as IEnumerable : null;
            bool outputMaterialFound = false;
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child == null || !child.GetType().Name.Contains("ComposedParticleOutput")) continue;
                    var findMaterial = child.GetType().GetMethod("FindMaterial", flags);
                    var material = findMaterial != null ? findMaterial.Invoke(child, null) as Material : null;
                    if (!material) continue;
                    DumpMaterial(material, "vfx-output-material", report);
                    outputMaterialFound = true;
                }
            }
            if (!outputMaterialFound) report.WriteLine("vfx-output-material=NOT_AVAILABLE");
        }
        catch (Exception error)
        {
            report.WriteLine("vfx-output-material=REFLECTION_ERROR " + error.GetType().Name + " " + error.Message);
        }
        int count = (int)resourceType.GetMethod("GetShaderSourceCount", flags).Invoke(resource, null);
        var getter = resourceType.GetMethod("GetShaderSource", flags);
        report.WriteLine("generatedShaderSourceCount=" + count);
        for (int i = 0; i < count; i++)
        {
            var source = (string)getter.Invoke(resource, new object[] { i });
            var savePath = OutputRoot + "/generated-" + label + "-" + i + ".shader";
            File.WriteAllText(savePath, source);
            report.WriteLine("source[" + i + "]=" + savePath +
                " hasAlphaTestKeyword=" + source.Contains("_ALPHATEST_ON") +
                " hasTransparentKeyword=" + source.Contains("_SURFACE_TYPE_TRANSPARENT") +
                " hasDynamicBlend=" + source.Contains("Blend [_SrcBlend] [_DstBlend]") +
                " hasVFXAlphaClip=" + source.Contains("VFXClipFragmentColor"));
        }
    }
    static Type FindResourceType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("UnityEditor.VFX.VisualEffectResource");
            if (type != null) return type;
        }
        throw new Exception("VisualEffectResource reflection type unavailable");
    }
}
