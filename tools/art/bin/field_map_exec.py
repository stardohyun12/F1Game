"""Map review only. Writes allowed: tools/art/bin/field_map* and art/_review/2026-10-29-field_patch/map*.
Resolves the main project connector by projectPath, never by a guessed port.
"""
import json, pathlib, sys, urllib.request
project='D:/Github/F1Game/F1'
instances=[json.loads(p.read_text(encoding='utf-8')) for p in (pathlib.Path.home()/'.unity-cli/instances').glob('*.json')]
d=next(d for d in instances if d.get('projectPath','').replace('\\','/').rstrip('/').lower()==project.lower() and d.get('state')!='stopped')
guard='if (Application.dataPath.Replace("\\\\", "/") != "D:/Github/F1Game/F1/Assets" || Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Map_TownV6") throw new System.Exception("Main Map_TownV6 edit mode required");\n'
code=guard+pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig')
req=urllib.request.Request(f'http://127.0.0.1:{d["port"]}/command',data=json.dumps({'command':'exec','params':{'code':code}}).encode(),headers={'Content-Type':'application/json'})
result=json.loads(urllib.request.urlopen(req,timeout=120).read())
result['resolvedPort']=d['port'];result['projectPath']=project
print(json.dumps(result,ensure_ascii=False))
if len(sys.argv)>2:pathlib.Path(sys.argv[2]).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
sys.exit(0 if result.get('success') else 1)
