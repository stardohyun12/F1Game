"""Studio-only file snippets; never falls back to F1."""
import json, pathlib, sys, urllib.request
project='D:/Github/F1Game/F1Studio'
instances=[json.loads(p.read_text(encoding='utf-8')) for p in (pathlib.Path.home()/'.unity-cli/instances').glob('*.json')]
d=next(d for d in instances if d.get('projectPath','').replace('\\','/').rstrip('/').lower()==project.lower() and d.get('state')!='stopped')
code=pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig')
req=urllib.request.Request(f'http://127.0.0.1:{d["port"]}/command',data=json.dumps({'command':'exec','params':{'code':code}}).encode(),headers={'Content-Type':'application/json'})
result=json.loads(urllib.request.urlopen(req,timeout=120).read())
print(json.dumps(result,ensure_ascii=False))
if len(sys.argv)>2: pathlib.Path(sys.argv[2]).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
sys.exit(0 if result.get('success') else 1)
