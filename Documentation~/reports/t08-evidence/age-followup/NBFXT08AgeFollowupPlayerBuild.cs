using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;

// Test-only isolated clone build entrypoint.
public static class NBFXT08AgeFollowupPlayerBuild
{
    public static void Run()
    {
        const string constantPath = "Assets/NBGraphVFXMeshConstant1.vfx";
        const string ageAlphaPath = "Assets/NBGraphVFXMeshAgeBound.vfx";
        const string ageRedPath = "Assets/NBGraphVFXMeshAgeRed.vfx";
        const string graphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string scenePath = "Assets/NBFXT08AgeFollowupPlayer.unity";
        const string outputPath = "/tmp/nbfx-vfx-age-followup-20260930/player/AgeFollowup.app";
        foreach (var path in new[] { constantPath, ageAlphaPath, ageRedPath })
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var constant = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(constantPath);
        var ageAlpha = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(ageAlphaPath);
        var ageRed = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(ageRedPath);
        var graph = AssetDatabase.LoadAssetAtPath<Shader>(graphPath);
        var constantShader = GetGeneratedShader(constantPath);
        var ageAlphaShader = GetGeneratedShader(ageAlphaPath);
        var ageRedShader = GetGeneratedShader(ageRedPath);
        if (!constant || !ageAlpha || !ageRed || !graph ||
            !constantShader || !ageAlphaShader || !ageRedShader)
            throw new Exception("Age followup assets absent");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var host = new GameObject("Age Followup Probe");
        var probe = host.AddComponent<NBFXT08AgeFollowupPlayerProbe>();
        probe.constantAsset = constant;
        probe.ageAlphaAsset = ageAlpha;
        probe.ageRedAsset = ageRed;
        probe.graphShader = graph;
        probe.constantShader = constantShader;
        probe.ageAlphaShader = ageAlphaShader;
        probe.ageRedShader = ageRedShader;
        EditorSceneManager.SaveScene(scene, scenePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scenePath }, locationPathName = outputPath,
            target = BuildTarget.StandaloneOSX, options = BuildOptions.None
        });
        Debug.Log("NBFX_T08_AGE_FOLLOWUP_BUILD result=" + result.summary.result +
            " errors=" + result.summary.totalErrors + " warnings=" + result.summary.totalWarnings);
        if (result.summary.result != BuildResult.Succeeded)
            throw new Exception("Age followup Player build failed");
    }
    static Shader GetGeneratedShader(string path)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Shader shader) return shader;
        return null;
    }
}
