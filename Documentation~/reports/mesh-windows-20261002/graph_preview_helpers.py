from pathlib import Path
import copy,json,uuid
class GraphPreview:
    def __init__(self,path,namespace):
        self.path=Path(path);s=self.path.read_text(encoding='utf-8');d=json.JSONDecoder();pos=0;self.objects=[]
        while pos<len(s):
            while pos<len(s) and s[pos].isspace():pos+=1
            if pos==len(s):break
            o,pos=d.raw_decode(s,pos);self.objects.append(o)
        self.before=copy.deepcopy(self.objects);self.g=self.objects[0];self.by={o['m_ObjectId']:o for o in self.objects};self.namespace=uuid.UUID(namespace)
    def uid(self,s):return uuid.uuid5(self.namespace,s).hex
    def add(self,o):
        assert o['m_ObjectId'] not in self.by;self.objects.append(o);self.by[o['m_ObjectId']]=o
    def slot(self,node,name):return next(self.by[r['m_Id']] for r in node['m_Slots'] if self.by[r['m_Id']]['m_DisplayName']==name)
    def source_for_property(self,ref):
        prop=next((o for o in self.objects if o.get('m_OverrideReferenceName')==ref),None)
        if prop is None:return None
        node=next(o for o in self.objects if o.get('m_Property',{}).get('m_Id')==prop['m_ObjectId'])
        output=self.by[node['m_Slots'][0]['m_Id']]
        return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':output['m_Id']}
    def property(self,ref,label,value,texture=False):
        src=self.source_for_property(ref)
        if src is not None:return src
        template=next(o for o in self.objects if o.get('m_OverrideReferenceName')==('_posTexture' if texture else '_B_autoPlayback'))
        prop=copy.deepcopy(template);prop['m_ObjectId']=self.uid(ref);prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(self.namespace,ref+':guid'))}
        for k in ['m_Name','m_RefNameGeneratedByDisplayName']:prop[k]=label
        for k in ['m_DefaultReferenceName','m_OverrideReferenceName']:prop[k]=ref
        prop['m_Hidden']=False
        if not texture:prop['m_Value']=float(value);prop['m_FloatType']=0
        node_template=next(o for o in self.objects if o.get('m_Property',{}).get('m_Id')==template['m_ObjectId'])
        node=copy.deepcopy(node_template);node['m_ObjectId']=self.uid(ref+':node');node['m_Property']={'m_Id':prop['m_ObjectId']}
        output=copy.deepcopy(self.by[node_template['m_Slots'][0]['m_Id']]);output['m_ObjectId']=self.uid(ref+':out');output['m_DisplayName']=output['m_ShaderOutputName']=label
        if not texture:output['m_Value']=output['m_DefaultValue']=float(value)
        node['m_Slots']=[{'m_Id':output['m_ObjectId']}]
        for o in [prop,node,output]:self.add(o)
        self.g['m_Properties'].append({'m_Id':prop['m_ObjectId']});self.g['m_Nodes'].append({'m_Id':node['m_ObjectId']})
        return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':output['m_Id']}
    def uv(self,channel):
        node=next((o for o in self.objects if o.get('m_OutputChannel')==channel),None)
        if node is None:
            node=copy.deepcopy(next(o for o in self.objects if o.get('m_OutputChannel')==1));node['m_ObjectId']=self.uid('UV'+str(channel));node['m_OutputChannel']=channel
            output=copy.deepcopy(self.by[node['m_Slots'][0]['m_Id']]);output['m_ObjectId']=self.uid('UV'+str(channel)+':out');node['m_Slots']=[{'m_Id':output['m_ObjectId']}]
            self.add(node);self.add(output);self.g['m_Nodes'].append({'m_Id':node['m_ObjectId']})
        output=self.by[node['m_Slots'][0]['m_Id']]
        return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':output['m_Id']}
    def input(self,node,label,src,template):
        assert src is not None,'Missing property/UV source: '+label
        sid=max(self.by[r['m_Id']]['m_Id'] for r in node['m_Slots'])+1
        slot=copy.deepcopy(self.slot(node,template));slot['m_ObjectId']=self.uid(node['m_FunctionName']+':'+label);slot['m_Id']=sid;slot['m_DisplayName']=slot['m_ShaderOutputName']=label
        self.add(slot);node['m_Slots'].append({'m_Id':slot['m_ObjectId']});self.g['m_Edges'].append({'m_OutputSlot':src,'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':sid}})
    def serialize(self,changed_nodes):
        for e in self.g['m_Edges']:
            assert e['m_OutputSlot'] is not None
            for key in ['m_OutputSlot','m_InputSlot']:
                endpoint=e[key];node=self.by[endpoint['m_Node']['m_Id']]
                assert any(self.by[s['m_Id']]['m_Id']==endpoint['m_SlotId'] for s in node['m_Slots'])
        allowed={self.g['m_ObjectId'],*changed_nodes}
        for o in self.before:
            if o['m_ObjectId'] not in allowed:assert o==self.by[o['m_ObjectId']],o.get('m_Name')
        return '\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in self.objects)+'\n'
