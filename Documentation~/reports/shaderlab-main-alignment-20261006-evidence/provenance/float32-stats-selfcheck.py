from pathlib import Path
import json,math,struct
from compare_player import float32,delta_statistic_float32,same_float32_statistic,delta
HERE=Path(__file__).resolve().parent
rows=[]
def check(name,value):assert value,name;rows.append({'name':name,'passed':True})
check('Csharp-subtract-rounds-before-max',delta_statistic_float32([float32(.8)],[float32(.15)])==float32(.6499999761581421))
check('Python-double-is-distinct-statistic',delta([float32(.8)],[float32(.15)])!=delta_statistic_float32([float32(.8)],[float32(.15)]))
value=delta_statistic_float32([float32(.8)],[float32(.15)])
check('declared-Single-JSON-roundtrip-bit-exact',same_float32_statistic(value,json.loads(json.dumps(value))))
next_value=struct.unpack('<f',struct.pack('<I',struct.unpack('<I',struct.pack('<f',value))[0]+1))[0]
check('one-ULP-wrong-statistic-rejected',not same_float32_statistic(value,next_value))
check('cross-endpoint-single-small-difference-still-nonzero',delta([0.25],[0.2500000000001])>0)
(HERE/'selfcheck-summary.json').write_text(json.dumps({'scope':'offline typed-statistic selfcheck only','checks':rows,'passed':len(rows)},indent=2)+'\n',encoding='utf-8')
print(json.dumps(rows,indent=2))
