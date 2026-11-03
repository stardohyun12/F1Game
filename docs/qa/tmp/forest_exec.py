"""artlive 20261031-2009: F1Studio 에디터에 .cs 하나를 exec (Play 아님). 사용: python -X utf8 docs/qa/tmp/forest_exec.py forest_grove.cs"""
import json, pathlib, sys, urllib.request
T = pathlib.Path('D:/Github/F1Game/docs/qa/tmp')
def port():
    for p in (pathlib.Path.home() / '.unity-cli/instances').glob('*.json'):
        d = json.loads(p.read_text(encoding='utf-8'))
        if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == 'd:/github/f1game/f1studio' and d.get('state') != 'stopped': return d['port']
req = urllib.request.Request(f'http://127.0.0.1:{port()}/command', data=json.dumps({'command': 'exec', 'params': {'code': (T / sys.argv[1]).read_text(encoding='utf-8-sig')}}).encode(), headers={'Content-Type': 'application/json'})
print(json.dumps(json.loads(urllib.request.urlopen(req, timeout=280).read()), ensure_ascii=False)[:1500])
