using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NBFX.PlayerValidation.Editor
{
    // One reviewed migration of the existing 94-file test stage; never recreates it.
    public static class NBFXPlayerStageFixturePatch
    {
        [Serializable] sealed class FileRow{public string path,sha256;}
        [Serializable] sealed class MaterialRow{public string path,fileSHA256,serializedBefore;}
        [Serializable] sealed class Ledger{public string project,staging,svcReceiptSHA256,origin;public FileRow[] files;public MaterialRow[] materials;}
        [Serializable] sealed class Change{public string path,beforeSHA256,afterSHA256,beforeJSON,afterJSON;}
        [Serializable] sealed class Receipt{public string status,error,originalLedgerSHA256;public Change[] changes;public string[] createdPaths;public bool allOtherOriginalFilesExact,allOldOtherMaterialsExact,sceneBytesExact;}
        static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static string SHA(string path)=>NBFXPlayerBuildPreparation.SHA(path);
        static string ScalarGuard(string json,string name)
        {
            string anchor="\"first\":\""+name+"\",\"second\":";
            int begin=json.IndexOf(anchor,StringComparison.Ordinal);
            Need(begin>=0&&json.LastIndexOf(anchor,StringComparison.Ordinal)==begin,"Expected one material key/value scalar "+name);
            int number=begin+anchor.Length,end=json.IndexOf('}',number);float value;
            Need(end>number&&float.TryParse(json.Substring(number,end-number),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value),"Expected exact numeric material scalar "+name);
            return json.Substring(0,number)+"\"<owned>\""+json.Substring(end);
        }
        static string BlendGuard(string json)=>ScalarGuard(ScalarGuard(json,"_SrcBlend"),"_DstBlend");
        static string ConfigGuard(string json)
        {const string p="\"nbPostReadbackMaterial\"\\s*:\\s*\\{[^{}]*\\}";Need(Regex.Matches(json,p).Count==1,"Expected one explicit retained material reference.");return Regex.Replace(json,p,"\"nbPostReadbackMaterial\":\"<owned>\"");}
        public static string Apply(string ledgerPath,string reviewedSHA,string output)
        {
            NBFXPlayerBuildPreparation.Guard();Need(!Directory.Exists(output),"New private receipt/backup folder required.");Need(SHA(ledgerPath)==reviewedSHA,"Reviewed current 94-file input ledger changed.");
            var l=JsonUtility.FromJson<Ledger>(File.ReadAllText(ledgerPath));string root=Path.GetDirectoryName(Application.dataPath).Replace('\\','/');Need(root==l.project&&l.files.Length==94&&l.materials.Length==33,"Exact existing stage/33 material baseline required.");
            var actual=Directory.GetFiles(Path.Combine(root,l.staging),"*",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/').Substring(root.Length+1)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();Need(actual.SequenceEqual(l.files.Select(f=>f.path).OrderBy(p=>p,StringComparer.Ordinal)),"Unknown staging file; no overwrite.");
            foreach(var row in l.files)Need(SHA(Path.Combine(root,row.path))==row.sha256,"Existing stage bytes changed: "+row.path);
            foreach(var row in l.materials){var m=AssetDatabase.LoadAssetAtPath<Material>(row.path);Need(m&&EditorJsonUtility.ToJson(m)==row.serializedBefore,"Material memory changed before patch: "+row.path);}
            string configPath=l.staging+"/PlayerConfig.asset",newMatPath=l.staging+"/NBPostReadback.mat";var config=AssetDatabase.LoadAssetAtPath<NBFXMeshPlayerConfig>(configPath);Need(config&&!config.nbPostReadbackMaterial&&!File.Exists(Path.Combine(root,newMatPath)),"Observer input already exists; do not overwrite.");
            string script=AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(config));string shaderPath=Path.GetDirectoryName(script).Replace('\\','/')+"/NBFXPlayerReadback.shader";var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);Need(shader&&shader.isSupported,"Explicit retained runtime shader copy missing.");
            Need(SHA(Path.Combine(root,shaderPath))==SHA(Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.xuanxuan.nb.fx/Tests/URP/Editor/G2MaskView.shader").resolvedPath,"Tests/URP/Editor/G2MaskView.shader")),"Readback shader copy must remain byte-identical.");
            string[] changed=new[]{"B-Forward-on.mat","B-Forward-control.mat","B-Forward-strength0.mat"}.Select(n=>l.staging+"/"+n).Concat(new[]{configPath}).ToArray();
            Directory.CreateDirectory(output);foreach(string path in changed)File.Copy(Path.Combine(root,path),Path.Combine(output,Path.GetFileName(path)+".before"),false);File.Copy(ledgerPath,Path.Combine(output,"ledger-before.json"),false);
            var changes=new List<Change>();var created=new List<string>();string error=null;
            try
            {
                foreach(string path in changed.Take(3)){
                    var m=AssetDatabase.LoadAssetAtPath<Material>(path);Need(m.GetFloat("_Blend")==0&&m.GetFloat("_TransparentMode")==1&&!m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")&&!m.IsKeywordEnabled("_ALPHAMODULATE_ON"),"Require explicit existing legal Native Alpha intent.");
                    Need(m.GetFloat("_SrcBlend")==1&&m.GetFloat("_DstBlend")==0,"Original Native Forward factors changed.");
                    var row=new Change{path=path,beforeSHA256=SHA(Path.Combine(root,path)),beforeJSON=EditorJsonUtility.ToJson(m)};
                    m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);row.afterJSON=EditorJsonUtility.ToJson(m);Need(BlendGuard(row.beforeJSON)==BlendGuard(row.afterJSON),"Native material changed beyond two blend factors.");
                    EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);Need(EditorJsonUtility.ToJson(m)==row.afterJSON,"Save changed additional Native material state.");row.afterSHA256=SHA(Path.Combine(root,path));changes.Add(row);
                }
                var configRow=new Change{path=configPath,beforeSHA256=SHA(Path.Combine(root,configPath)),beforeJSON=EditorJsonUtility.ToJson(config)};
                var retained=new Material(shader){name="NBFX original globals readback"};Need(retained.passCount==3,"Three original copy passes required.");AssetDatabase.CreateAsset(retained,newMatPath);created.Add(newMatPath);created.Add(newMatPath+".meta");AssetDatabase.SaveAssetIfDirty(retained);
                config.nbPostReadbackMaterial=retained;configRow.afterJSON=EditorJsonUtility.ToJson(config);Need(ConfigGuard(configRow.beforeJSON)==ConfigGuard(configRow.afterJSON),"Config changed beyond explicit readback reference.");EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);Need(EditorJsonUtility.ToJson(config)==configRow.afterJSON,"Save changed additional Config state.");configRow.afterSHA256=SHA(Path.Combine(root,configPath));changes.Add(configRow);
                foreach(var row in l.files.Where(f=>!changed.Contains(f.path)))Need(SHA(Path.Combine(root,row.path))==row.sha256,"Unexpected original file mutation: "+row.path);
                foreach(var row in l.materials.Where(f=>!changed.Contains(f.path)))Need(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<Material>(row.path))==row.serializedBefore,"Unrelated material memory changed: "+row.path);
                l.files=Directory.GetFiles(Path.Combine(root,l.staging),"*",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.Ordinal).Select(p=>new FileRow{path=p.Replace('\\','/').Substring(root.Length+1),sha256=SHA(p)}).ToArray();Need(l.files.Length==96,"Only one material and meta may be added.");
                l.materials=l.materials.Select(row=>new MaterialRow{path=row.path,fileSHA256=SHA(Path.Combine(root,row.path)),serializedBefore=EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<Material>(row.path))}).Concat(new[]{new MaterialRow{path=newMatPath,fileSHA256=SHA(Path.Combine(root,newMatPath)),serializedBefore=EditorJsonUtility.ToJson(retained)}}).ToArray();
                l.origin="Versioned test fixture inputs: exactly three Native Forward 1/0->legal Alpha5/10, one original readback material/ref; prior ledger/backups retained. Scenes and all other original material fields unchanged.";
                File.WriteAllText(Path.Combine(output,"adoption-after.json"),JsonUtility.ToJson(l,true));return Path.Combine(output,"adoption-after.json");
            }
            catch(Exception e){error=e.ToString();throw;}
            finally{File.WriteAllText(Path.Combine(output,"patch-receipt.json"),JsonUtility.ToJson(new Receipt{status=error==null?"APPLIED_AWAIT_ROOT_REVIEW":"FAILED_PARTIAL_RETAINED",error=error,originalLedgerSHA256=reviewedSHA,changes=changes.ToArray(),createdPaths=created.ToArray(),allOtherOriginalFilesExact=error==null,allOldOtherMaterialsExact=error==null,sceneBytesExact=error==null},true));}
        }
    }
}
