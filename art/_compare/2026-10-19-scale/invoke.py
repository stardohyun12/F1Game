import json, pathlib, urllib.request, sys
instances = pathlib.Path.home().joinpath('.unity-cli/instances').glob('*.json')
d = next(d for p in instances if (d := json.loads(p.read_text(encoding='utf-8')))['projectPath'].replace('\\', '/').rstrip('/').lower() == 'd:/github/f1game/f1' and d['state'] != 'stopped')
body = {'command':'exec','params':{'code':pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig')}}
req = urllib.request.Request(f'http://127.0.0.1:{d["port"]}/command',data=json.dumps(body).encode(),headers={'Content-Type':'application/json'})
result = json.load(urllib.request.urlopen(req,timeout=90))
print(json.dumps(result,ensure_ascii=False,indent=2))
sys.exit(0 if result.get('success') else 1)
