#!/usr/bin/env python3
"""Build self-contained, deterministic VFX Output base-color fixtures under /tmp."""
from __future__ import annotations
import gzip, hashlib, pathlib, re, shutil, struct, uuid, zlib

ROOT = pathlib.Path(__file__).resolve().parent
OUT = ROOT / 'Assets'
ARCH = pathlib.Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX/Documentation~/reports/t08-evidence/vfx-dissolve-numeric-one-particle')
OUTPUT_ID = 8926484042661614853
BASEMAP_ID = 8926484042661614854
CHANNEL_ID = 8926484042661614888
TIMELINE_ID = 8926484042661616001
RGBA = (64, 128, 160, 192)
CASES = (('A0',3,0), ('A1',3,1), ('A2',3,2), ('R0',0,0))

def guid(name: str) -> str:
    return uuid.uuid5(uuid.NAMESPACE_URL,'nbfx-vfx-basecolor-probe-20260930/'+name).hex

def only(s: str, old: str, new: str, label: str) -> str:
    count=s.count(old)
    if count!=1: raise ValueError(f'{label}: expected one {old!r}, got {count}')
    return s.replace(old,new,1)

def object_match(s: str, fid: int) -> re.Match[str]:
    m=re.search(rf'(?ms)^--- !u!114 &{fid}\n.*?(?=^--- !u!|\Z)',s)
    if not m: raise ValueError(f'missing object {fid}')
    return m

def change_object(s: str, fid: int, old: str, new: str) -> str:
    m=object_match(s,fid)
    b=only(m.group(),old,new,f'object {fid}')
    return s[:m.start()]+b+s[m.end():]

def change_value(s: str, fid: int, old: str, new: str) -> str:
    return change_object(s,fid,'m_SerializableObject: '+old+'\n','m_SerializableObject: '+new+'\n')

def meta(src: pathlib.Path, filename: str) -> str:
    s=src.read_text()
    m=re.search(r'(?m)^guid: ([a-f0-9]{32})$',s)
    if not m: raise ValueError(f'missing GUID in {src}')
    return only(s,m.group(1),guid(filename),str(src))

def chunk(kind: bytes, payload: bytes) -> bytes:
    return struct.pack('!I',len(payload))+kind+payload+struct.pack('!I',zlib.crc32(kind+payload)&0xffffffff)

def png_rgba_1x1() -> bytes:
    return (b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!IIBBBBB',1,1,8,6,0,0,0))+
            chunk(b'IDAT',zlib.compress(b'\x00'+bytes(RGBA)))+chunk(b'IEND',b''))

def main() -> None:
    OUT.mkdir(exist_ok=True)
    source=gzip.decompress((ARCH/'NBFXNumericOff.vfx.gz').read_bytes()).decode()
    if '  capacity: 1\n' not in source or '  cullMode: 0\n' not in source:
        raise ValueError('numeric source is not one-particle Cull Off fixture')
    if 'name: _BaseColorIntensityForTimeline' in source or str(TIMELINE_ID) in source:
        raise ValueError('timeline slot already exists; redo fixture model, do not duplicate')
    # Copy OFF-state auxiliary textures. Only BaseMap changes to a deliberately
    # non-white, non-gray RGBA texture, imported as linear/uncompressed/point.
    for old,new in (('NBFXNumericGray133Linear.png','NBFXBCGray.png'),
                    ('NBFXNumericWhiteLinear.png','NBFXBCWhite.png')):
        src_meta=(ARCH/(old+'.meta')).read_text()
        old_guid=re.search(r'(?m)^guid: ([a-f0-9]{32})$',src_meta).group(1)
        shutil.copyfile(ARCH/old,OUT/new)
        (OUT/(new+'.meta')).write_text(meta(ARCH/(old+'.meta'),new))
        source=source.replace(old_guid,guid(new))
    rgba_name='NBFXBC_RGBA_64_128_160_192.png'
    (OUT/rgba_name).write_bytes(png_rgba_1x1())
    (OUT/(rgba_name+'.meta')).write_text(meta(ARCH/'NBFXNumericWhiteLinear.png.meta',rgba_name))
    white_guid=guid('NBFXBCWhite.png')
    rgba_guid=guid(rgba_name)
    source=change_object(source,BASEMAP_ID,white_guid,rgba_guid)
    # New ShaderGraph property was added after the archived VFX source. Clone a
    # known scalar slot, give it a unique fileID/name, and attach to this one
    # Output. Unity reimport must prove it survived and reached generated HLSL.
    template=object_match(source,CHANNEL_ID).group()
    if 'name: _NB_ColorChannelLo16' not in template:
        raise ValueError('packed channel template mismatch')
    timeline=template.replace(str(CHANNEL_ID),str(TIMELINE_ID)).replace(
        'name: _NB_ColorChannelLo16','name: _BaseColorIntensityForTimeline')
    source=change_object(source,OUTPUT_ID,'  m_OutputSlots: []\n',
        f'  - {{fileID: {TIMELINE_ID}}}\n  m_OutputSlots: []\n')
    source+='\n'+timeline
    results=[]
    for name,channel,intensity in CASES:
        text=change_value(source,CHANNEL_ID,'0',str(channel))
        text=change_value(text,TIMELINE_ID,'0',str(intensity))
        filename=f'NBFXBC{name}.vfx'
        (OUT/filename).write_text(text)
        (OUT/(filename+'.meta')).write_text(meta(ARCH/'NBFXNumericOff.vfx.meta',filename))
        results.append((name,channel,intensity,text))
    def normalize(s: str,ch: int,val: int)->str:
        s=change_value(s,CHANNEL_ID,str(ch),'X')
        return change_value(s,TIMELINE_ID,str(val),'Y')
    if len({hashlib.sha256(normalize(s,ch,v).encode()).hexdigest()
            for _,ch,v,s in results})!=1:
        raise ValueError('VFX variants differ outside channel/timeline scalar slots')
    for path in sorted(OUT.iterdir()):
        if path.is_file(): print(hashlib.sha256(path.read_bytes()).hexdigest(),path)

if __name__=='__main__': main()
