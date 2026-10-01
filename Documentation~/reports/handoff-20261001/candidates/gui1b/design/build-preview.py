#!/usr/bin/env python3
"""GUI1B seed-only candidate. Always reads current product; outputs /tmp only.
No existing product/Test/Unity/Git write. Does NOT pretend to implement Tier.
"""
from pathlib import Path
import argparse,copy,difflib,hashlib,json,re,uuid
ap=argparse.ArgumentParser();ap.add_argument('--source-package',default='/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX');ap.add_argument('--output',required=True);args=ap.parse_args()
root=Path(args.source_package).resolve();out=Path(args.output).resolve();tmp=Path('/tmp').resolve()
assert out != tmp and tmp in out.parents, 'Candidate destination must be a child of /tmp'
assert root != out and root not in out.parents, 'Never overwrite product source'
csrel='NBShaders2/Editor/ShaderGUIItems/NBShaderSyncService.cs';grel='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
sourceCS=(root/csrel).read_bytes();sourceGraph=(root/grel).read_bytes()
cs=sourceCS.decode('utf-8');gtext=sourceGraph.decode('utf-8');oldcs=cs
# The bindings are the existing single authority. Never copy flag constants into a second list.
toggles=re.findall(r'new FlagToggleBinding\("([^"\n]+)",\s*NBShaderFlags\.([A-Z_0-9]+),\s*(\d+)\)',cs)
modes=re.findall(r'new FlagModeBinding\("([^"\n]+)",\s*NBShaderFlags\.([A-Z_0-9]+),\s*(\d+),\s*(\d+)\)',cs)
assert len(toggles)==22 and len(modes)==7, 'Binding schema changed; audit migration version before generating'
assert all(int(x[3]) in (0,1) for x in modes), 'Binary disabled value requires explicit audit'

helper='''        // GUI1B candidate: serialized UI mirrors of the EXISTING flag protocol.
        // No Graph Tier/effective projection, no periodic mirror->flags authority switch.
        internal static bool GraphFlagIntentSchemaAvailable(Material material)
        {
            if (!NBShaderGUIContext.IsGraphMaterial(material)) return false;
            for (int i = 0; i < ToggleFlagBindings.Length; ++i)
                if (!NBShaderRootItem.HasFloatProperty(material, ToggleFlagBindings[i].propertyName)) return false;
            for (int i = 0; i < ModeFlagBindings.Length; ++i)
                if (!NBShaderRootItem.HasFloatProperty(material, ModeFlagBindings[i].propertyName)) return false;
            return true;
        }

        private static void SeedGraphFlagIntents(Material material)
        {
            var flags = new NBShaderFlags(material); // Shared Material halfword read hooks; NEVER write words here.
            for (int i = 0; i < ToggleFlagBindings.Length; ++i)
            {
                var binding = ToggleFlagBindings[i];
                material.SetFloat(binding.propertyName,
                    flags.CheckFlagBits(binding.flagBits, index: binding.flagIndex) ? 1f : 0f);
            }
            for (int i = 0; i < ModeFlagBindings.Length; ++i)
            {
                var binding = ModeFlagBindings[i];
                int disabledMode = binding.enabledMode == 0 ? 1 : 0;
                material.SetFloat(binding.propertyName,
                    flags.CheckFlagBits(binding.flagBits, index: binding.flagIndex)
                        ? binding.enabledMode : disabledMode);
            }
        }

'''
method='''        internal void PrepareGraphGUIState()
        {
            if (_rootItem.Mats == null || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return;
            var uninitialized = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats)
            {
                if (!NBShaderGUIContext.IsGraphMaterial(material) ||
                    !NBShaderRootItem.HasFloatProperty(material, GraphGUIStateVersionProperty) ||
                    !NBShaderRootItem.HasFloatProperty(material, "_MainTexBigBlockItemFoldOut") ||
                    !NBShaderRootItem.HasFloatProperty(material, "_BaseMapFoldOut")) continue;
                float targetVersion = GraphFlagIntentSchemaAvailable(material) ? 2f : 1f;
                if (material.GetFloat(GraphGUIStateVersionProperty) < targetVersion) uninitialized.Add(material);
            }
            if (uninitialized.Count == 0) return;
            Undo.RecordObjects(uninitialized.ToArray(), "Initialize NB Graph GUI state");
            foreach (UnityEngine.Object target in uninitialized)
            {
                var material = (Material)target;
                bool seedFlagIntents = GraphFlagIntentSchemaAvailable(material);
                if (seedFlagIntents) SeedGraphFlagIntents(material);
                // Commit version LAST. Existing Graph functional properties, raw
                // halfwords (including noncanonical finite values), URP surface,
                // keywords, passes, queue, textures and prior foldouts are untouched.
                material.SetFloat(GraphGUIStateVersionProperty, seedFlagIntents ? 2f : 1f);
                EditorUtility.SetDirty(material);
            }
        }'''
anchor='        internal void PrepareGraphGUIState()';start=cs.index(anchor);brace=cs.index('{',start);depth=1;end=brace+1
while depth:
 if cs[end]=='{':depth+=1
 elif cs[end]=='}':depth-=1
 end+=1
cs=cs[:start]+helper+method+cs[end:]
# Mechanical guarantee: legacy helpers/sync/tables are untouched.
assert oldcs[:start] in cs and oldcs[end:] in cs

# Multi-object Graph stream: append unconnected genuine Hidden Float editor inputs.
dec=json.JSONDecoder();objs=[];pos=0
while pos<len(gtext):
 while pos<len(gtext) and gtext[pos].isspace():pos+=1
 if pos==len(gtext):break
 obj,pos=dec.raw_decode(gtext,pos);objs.append(obj)
oldobjs=copy.deepcopy(objs);by={o.get('m_ObjectId'):o for o in objs}
props=[by[r['m_Id']] for r in objs[0]['m_Properties']]
def pname(o):return o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName')
template=next(o for o in props if pname(o)=='_NB_Flags0Lo16');assert template['m_Type']=='UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty'
new=[];manifest=[]
for row,kind in [(r,'toggle') for r in toggles]+[(r,'mode') for r in modes]:
 name,flag,word=row[:3];default=0 if kind=='toggle' or int(row[3])==1 else 1
 existing=next((o for o in props if pname(o)==name),None)
 if existing:
  assert existing['m_Type']==template['m_Type'] and existing.get('m_FloatType')==0, 'Existing incompatible property '+name
  # Preserve existing property object/default/visibility; do not claim version2
  # migration safe if another owner introduced and used this field first.
  status='already-present-requires-human-migration-audit'
 else:
  o=copy.deepcopy(template)
  # Stable IDs for identical/dynamic reruns; no duplicate property objects.
  o['m_ObjectId']=uuid.uuid5(uuid.NAMESPACE_URL, 'com.xuanxuan.nb.fx/gui1b/object/'+name).hex
  o['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(uuid.NAMESPACE_URL, 'com.xuanxuan.nb.fx/gui1b/guid/'+name))}
  assert o['m_ObjectId'] not in by, 'Deterministic intent object ID collision '+name
  o['m_Name']='NB GUI flag intent '+name;o['m_DefaultReferenceName']=name;o['m_OverrideReferenceName']=name;o['m_Hidden']=True;o['m_Value']=float(default);o['m_FloatType']=0
  objs[0]['m_Properties'].append({'m_Id':o['m_ObjectId']});objs.append(o);props.append(o);new.append(name);status='appended-hidden-real-float'
 manifest.append({'name':name,'kind':kind,'originalFlag':flag,'word':int(word),'enabledMode':int(row[3]) if kind=='mode' else None,'disabledMode':default,'status':status})
# Pre-existing UI mirrors could already contain user intent. A version1->2
# seed cannot overwrite them without a migration audit, so abort by default.
assert all(m['status']=='appended-hidden-real-float' for m in manifest), 'Existing intent Float collision: audit material migration before generating seed candidate'
# Preserve every old object byte-semantically, except top-level property reference list.
for old,newobj in zip(oldobjs[1:],objs[1:]):assert old==newobj
rootold=copy.deepcopy(oldobjs[0]);rootnew=copy.deepcopy(objs[0]);rootold.pop('m_Properties');rootnew.pop('m_Properties');assert rootold==rootnew
assert (root/csrel).read_bytes()==sourceCS and (root/grel).read_bytes()==sourceGraph, 'Product changed during generation; rerun latest snapshot'
out.mkdir(parents=True,exist_ok=True)
def sha(b):return hashlib.sha256(b).hexdigest()
records=[]
for rel,source,newtext in [(csrel,oldcs,cs),(grel,gtext,'\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in objs)+'\n')]:
 p=out/rel;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(newtext)
 dp=out/'diffs'/(rel.replace('/','__')+'.diff');dp.parent.mkdir(exist_ok=True);dp.write_text(''.join(difflib.unified_diff(source.splitlines(True),newtext.splitlines(True),fromfile=str(root/rel),tofile=str(p))))
 records.append({'path':rel,'originalSha256':sha(source.encode('utf-8')),'candidateSha256':sha(p.read_bytes()),'diff':str(dp)})
summary={'scope':'GUI1B seed-only PREVIEW; no Tier/render/complete-GUI claim','sourcePackage':str(root),'sourceRecords':records,'bindings':manifest,'graphObjectsBefore':len(oldobjs),'graphObjectsAfter':len(objs),'graphPropertiesBefore':len(oldobjs[0]['m_Properties']),'graphPropertiesAfter':len(objs[0]['m_Properties']),'addedHiddenFloatInputs':new,'oldNonRootObjectsUnchanged':True,'rootOtherFieldsUnchanged':True,'SupportVFXTargetsPortsUnchanged':True,'noSourceProductWrite':True,'noForwardMirrorSyncEnabled':True}
(out/'source-manifest.json').write_text(json.dumps(summary,indent=2,ensure_ascii=False)+'\n');print(json.dumps(summary,indent=2,ensure_ascii=False))
