using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;

// Isolated diagnostic only: do not include in Packages/NB_FX.
public static class NBFXT08VFXCDExplicitTexturesPlayerBuild
{
    const string Root = "/tmp/nbfx-vfx-cd-explicit-textures-20260930";
    const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
    const string ScenePath = "Assets/NBFXT08VFXCDExplicitTexturesPlayer.unity";
    const string PlayerPath = Root + "/player/VFXCDExplicitTexturesPlayer.app";
    const string GrayPath = "Assets/NBDissolveGray133Linear.png";
    const string WhitePath = "Assets/NBDissolveMaskWhiteLinear.png";
    const string GrayGuid = "955ea0926dde4e128aec69a8ce164b47";
    const string WhiteGuid = "cef3ff1fb43149c781878d9235ea29fd";
    static readonly string[] Paths = { "Assets/NBGraphVFXCDExplicitOff.vfx",
        "Assets/NBGraphVFXCDExplicitSingle.vfx", "Assets/NBGraphVFXCDExplicitProcessMask.vfx" };
    public static void Run()
    {
        Directory.CreateDirectory(Root);
        AssetDatabase.ImportAsset(GrayPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(WhitePath, ImportAssetOptions.ForceUpdate);
        foreach (var path in Paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var off = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Paths[0]);
        var single = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Paths[1]);
        var processMask = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Paths[2]);
        var graph = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        var offShader = FindShader(Paths[0]);
        var singleShader = FindShader(Paths[1]);
        var processMaskShader = FindShader(Paths[2]);
        if (!off || !single || !processMask || !graph || !offShader || !singleShader || !processMaskShader)
            throw new Exception("VFX C/D assets absent after import");
        using (var report = new StreamWriter(Root + "/editor-inspection.txt"))
        {
            ValidateTexture(GrayPath, GrayGuid, 133f / 255f, report);
            ValidateTexture(WhitePath, WhiteGuid, 1f, report);
            Inspect(Paths[0], "off", report);
            Inspect(Paths[1], "single", report);
            Inspect(Paths[2], "process-mask", report);
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var host = new GameObject("VFX C/D Probe");
        var probe = host.AddComponent<NBFXT08VFXCDExplicitTexturesPlayerProbe>();
        probe.offAsset = off;
        probe.singleAsset = single;
        probe.processMaskAsset = processMask;
        probe.graphShader = graph;
        probe.offShader = offShader;
        probe.singleShader = singleShader;
        probe.processMaskShader = processMaskShader;
        EditorSceneManager.SaveScene(scene, ScenePath);
        Directory.CreateDirectory(Path.GetDirectoryName(PlayerPath));
        var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, locationPathName = PlayerPath,
            target = BuildTarget.StandaloneOSX, options = BuildOptions.None
        });
        Debug.Log("NBFX_T08_VFX_CD_EXPLICIT_BUILD result=" + build.summary.result +
            " errors=" + build.summary.totalErrors + " warnings=" + build.summary.totalWarnings);
        if (build.summary.result != BuildResult.Succeeded)
            throw new Exception("VFX C/D Player build failed");
    }
    static void ValidateTexture(string path, string guid, float expected, TextWriter report)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!importer || !texture || AssetDatabase.AssetPathToGUID(path) != guid ||
            importer.sRGBTexture || importer.mipmapEnabled || !importer.isReadable ||
            importer.textureCompression != TextureImporterCompression.Uncompressed ||
            texture.width != 1 || texture.height != 1)
            throw new Exception("Explicit texture import contract failed: " + path);
        var color = texture.GetPixel(0, 0);
        if (Mathf.Abs(color.r - expected) > .003f ||
            Mathf.Abs(color.g - expected) > .003f ||
            Mathf.Abs(color.b - expected) > .003f ||
            Mathf.Abs(color.a - 1f) > .003f)
            throw new Exception("Explicit texture pixel mismatch: " + path + " color=" + color);
        report.WriteLine("texture=" + path + " guid=" + guid + " pixel=" + color +
            " linear=" + !importer.sRGBTexture + " mipmap=" + importer.mipmapEnabled +
            " compression=" + importer.textureCompression);
    }
    static Shader FindShader(string path)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Shader shader) return shader;
        return null;
    }
    static void DumpMaterial(Material material, string label, TextWriter report)
    {
        var dissolve = material.GetTexture("_DissolveMap");
        var dissolveMask = material.GetTexture("_DissolveMaskMap");
        report.WriteLine(label + " shader=" + material.shader.name +
            " _Surface=" + material.GetFloat("_Surface") +
            " _AlphaClip=" + material.GetFloat("_AlphaClip") +
            " _SrcBlend=" + material.GetFloat("_SrcBlend") +
            " _DstBlend=" + material.GetFloat("_DstBlend") +
            " dissolveMap=" + (dissolve ? dissolve.name : "NULL") +
            " dissolveMaskMap=" + (dissolveMask ? dissolveMask.name : "NULL") +
            " keywords=" + string.Join(",", material.shaderKeywords));
    }
    static void Inspect(string path, string label, TextWriter report)
    {
        var resourceType = FindResourceType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var resource = resourceType.GetMethod("GetResourceAtPath", flags).Invoke(null, new object[] { path });
        // Hidden-output material reflection is diagnostic, not a build prerequisite.
        try
        {
            var graphProperty = resourceType.GetProperty("graph", flags);
            var graph = graphProperty != null ? graphProperty.GetValue(resource, null) : null;
            var childrenProperty = graph != null ? graph.GetType().GetProperty("children", flags) : null;
            var children = childrenProperty != null ? childrenProperty.GetValue(graph, null) as IEnumerable : null;
            bool materialFound = false;
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child == null || !child.GetType().Name.Contains("ComposedParticleOutput")) continue;
                    var findMaterial = child.GetType().GetMethod("FindMaterial", flags);
                    var material = findMaterial != null ? findMaterial.Invoke(child, null) as Material : null;
                    if (!material) continue;
                    DumpMaterial(material, label + " VFX-output-material", report);
                    materialFound = true;
                }
            }
            if (!materialFound) report.WriteLine(label + " VFX-output-material=NOT_AVAILABLE");
        }
        catch (Exception exception) { report.WriteLine(label + " VFX-output-material=INSPECTION_FAILED " + exception); }
        int sourceCount = (int)resourceType.GetMethod("GetShaderSourceCount", flags).Invoke(resource, null);
        var getter = resourceType.GetMethod("GetShaderSource", flags);
        for (int i = 0; i < sourceCount; i++)
        {
            var source = (string)getter.Invoke(resource, new object[] { i });
            var savePath = Root + "/generated-" + label + "-" + i + ".shader";
            File.WriteAllText(savePath, source);
            report.WriteLine(label + " source[" + i + "]=" + savePath +
                " toggle1=" + source.Contains("output._Dissolve_Toggle = (float)1") +
                " dissolveMask1=" + source.Contains("output._DissolveMask_Toggle = (float)1") +
                " graphDissolveDefaultGrey=" + source.Contains("_DissolveMap(\"DissolveMap\", 2D) = \"grey\"") +
                " graphDissolveMaskDefaultWhite=" + source.Contains("_DissolveMaskMap(\"DissolveMaskMap\", 2D) = \"white\""));
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
