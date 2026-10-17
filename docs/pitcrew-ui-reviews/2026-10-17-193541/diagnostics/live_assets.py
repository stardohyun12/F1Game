from pathlib import Path
import json,urllib.request,re
OUT=Path(__file__).resolve().parents[1]
APP=OUT.parents[2]/'tools/studio-dashboard'
def scrub(s):return re.sub(r'(?i)([A-Z]:[\\/]+Users[\\/]+)[^\\/\s"\']+',r'\1USER',s)
def write(name,obj):(OUT/name).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
rows=json.loads((OUT/'evidence/http-live.json').read_text(encoding='utf-8'))
for row in rows:
    url=row['path']
    if not (url=='/' or url.endswith(('.js','.css'))):
        row.pop('disk_sha256',None);row.pop('disk_match',None)
        continue
    name='index.html' if url=='/' else url[1:]
    with urllib.request.urlopen('http://127.0.0.1:7878'+url,timeout=8) as r:b=r.read()
    disk=(APP/'web'/name).read_bytes()
    row['normalized_newline_match']=disk.replace(b'\r\n',b'\n')==b.replace(b'\r\n',b'\n')
    d=OUT/'evidence/live_web'/name;d.parent.mkdir(exist_ok=True);d.write_text(scrub(b.decode('utf-8')),encoding='utf-8')
write('evidence/http-live.json',rows)
