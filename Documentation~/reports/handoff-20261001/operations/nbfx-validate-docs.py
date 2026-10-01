from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlparse,unquote
import hashlib
p=Path('Packages/NB_FX/Documentation~');errors=[]
class P(HTMLParser):
 def __init__(self):super().__init__();self.links=[];self.ids=set()
 def handle_starttag(self,t,a):
  a=dict(a)
  if a.get('id'):self.ids.add(a['id'])
  for key in ('href','src'):
   if a.get(key):self.links.append(a[key])
parsed={}
for f in p.rglob('*.html'):
 v=P();v.feed(f.read_text());parsed[f.resolve()]=v
for f,v in parsed.items():
 for l in v.links:
  u=urlparse(l)
  if u.scheme or u.netloc:continue
  target=(f.parent/unquote(u.path)).resolve()if u.path else f
  if not target.exists():errors.append((str(f),l,'missing'))
  elif u.fragment and target in parsed and unquote(u.fragment)not in parsed[target].ids:errors.append((str(f),l,'anchor'))
for folder in ['render-state-20261001','texture-noise-20261001']:
 r=p/'reports/t07-evidence'/folder;count=0
 for line in (r/'sha256.txt').read_text().splitlines():
  h,f=line.split('  ',1);count+=1
  if hashlib.sha256((r/f).read_bytes()).hexdigest()!=h:errors.append((folder,f,'hash'))
 print('hash',folder,count)
print('html',len(parsed),'errors',errors[:20]);raise SystemExit(bool(errors))
