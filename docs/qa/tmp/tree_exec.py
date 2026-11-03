"""artlive 20261031-0243: F1Studio 에디터에 .cs 하나 실행(bash 스크립트가 권한으로 막힐 때). 사용: python -X utf8 docs/qa/tmp/tree_exec.py <file.cs>"""
import json, pathlib, sys, urllib.request
def port():
    for p in (pathlib.Path.home() / '.unity-cli/instances').glob('*.json'):
        d = json.loads(p.read_text(encoding='utf-8'))
        if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == 'd:/github/f1game/f1studio' and d.get('state') != 'stopped': return d['port']
pt = port()
if not pt: print('FAIL: F1Studio 에디터 없음'); sys.exit(1)
code = pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig')
req = urllib.request.Request(f'http://127.0.0.1:{pt}/command', data=json.dumps({'command': 'exec', 'params': {'code': code}}).encode(), headers={'Content-Type': 'application/json'})
print(json.dumps(json.loads(urllib.request.urlopen(req, timeout=300).read()), ensure_ascii=False))
