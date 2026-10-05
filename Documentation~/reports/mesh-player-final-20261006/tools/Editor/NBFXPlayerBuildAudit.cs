using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;

namespace NBFX.PlayerValidation.Editor
{
    // Read-only shader callback. Counts describe this callback point, not a promise
    // of final binary variants, SVC retention, or AssetBundle coverage.
    public sealed class NBFXPlayerBuildAudit:IPreprocessShaders
    {
        [Serializable] sealed class Row{public string shader,assetPath,pass,stage,passType,compilerPlatform;public int observedVariants,ovzEnabled,ovzDisabled;}
        [Serializable] sealed class Artifact{public string path,sha256;public long bytes;}
        [Serializable] sealed class Artifacts{public string scope;public Artifact[] files;}
        [Serializable] sealed class BuildData
        {
            public string scope,output,result,error;public double totalSeconds;public ulong reportedBytes;
            public Row[] shaderCallbacks;public string[] processors,buildFiles,messages;public long totalDiskBytes;public bool generatedAssetsUnchanged;
        }
        static string output;static readonly List<Row> rows=new List<Row>();static BuildData data;static Artifact[] artifacts;
        public int callbackOrder=>int.MaxValue;
        public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> variants)
        {
            if(string.IsNullOrEmpty(output)||!shader)return;
            string path=AssetDatabase.GetAssetPath(shader);
            if(!path.EndsWith("/NBShaders2/Shader/NBShader.shader",StringComparison.Ordinal)&&!path.EndsWith("/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph",StringComparison.Ordinal))return;
            foreach(var group in variants.GroupBy(v=>v.shaderCompilerPlatform))
            {
                int on=group.Count(v=>v.shaderKeywordSet.GetShaderKeywords().Any(k=>k.name=="_OVERRIDE_Z"));
                rows.Add(new Row{shader=shader.name,assetPath=path,pass=snippet.passName,stage=snippet.shaderType.ToString(),passType=snippet.passType.ToString(),compilerPlatform=group.Key.ToString(),observedVariants=group.Count(),ovzEnabled=on,ovzDisabled=group.Count()-on});
            }
        }
        public static void Begin(string path)
        {
            output=path;rows.Clear();data=new BuildData{scope="One combined Native+Graph Development Windows64 build. Variant counts observed at callbackOrder=int.MaxValue, not guaranteed final binary/SVC/AssetBundle inventory. Size includes both shaders, development symbols, validation and performance harness.",output=path};
            data.processors=TypeCache.GetTypesDerivedFrom<IPreprocessShaders>().Where(t=>!t.IsAbstract&&!t.ContainsGenericParameters).Select(t=>t.AssemblyQualifiedName).ToArray();
        }
        public static void Record(BuildReport report)
        {data.result=report.summary.result.ToString();data.totalSeconds=report.summary.totalTime.TotalSeconds;data.reportedBytes=report.summary.totalSize;data.messages=report.steps.SelectMany(s=>s.messages.Where(m=>m.type==LogType.Error||m.type==LogType.Warning||m.type==LogType.Exception).Select(m=>s.name+": "+m.type+": "+m.content)).ToArray();}
        public static void RecordArtifacts(string staging)
        {
            string root=Path.GetDirectoryName(Application.dataPath);artifacts=Directory.GetFiles(Path.Combine(root,staging),"*",SearchOption.AllDirectories).Select(p=>new Artifact{path=p,sha256=NBFXPlayerBuildPreparation.SHA(p),bytes=new FileInfo(p).Length}).ToArray();
            File.WriteAllText(Path.Combine(output,"generated-artifacts.json"),JsonUtility.ToJson(new Artifacts{scope="Exact serialized scene/material/texture inputs after staging and before BuildPlayer. Explicit retained on/off materials are scene references, no SVC/AlwaysIncluded mutation.",files=artifacts},true));
        }
        public static void End(string error)
        {
            if(string.IsNullOrEmpty(output))return;
            data.error=error;data.shaderCallbacks=rows.ToArray();data.generatedAssetsUnchanged=artifacts!=null&&artifacts.All(f=>File.Exists(f.path)&&NBFXPlayerBuildPreparation.SHA(f.path)==f.sha256);data.buildFiles=Directory.Exists(output)?Directory.GetFiles(output,"*",SearchOption.AllDirectories):Array.Empty<string>();data.totalDiskBytes=data.buildFiles.Sum(p=>new FileInfo(p).Length);
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"build-observation.json"),JsonUtility.ToJson(data,true));output=null;
        }
    }
}
