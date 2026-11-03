# artlive 2056: F1Studio 에디터에 C# 한 덩어리 보내기(studio_open 없이 — Play 중에도). 사용: python ux.py <file.cs>
import sys, json, glob, os, urllib.request
proj = 'd:/github/f1game/f1studio'; port = None
for f in glob.glob(os.path.expanduser('~/.unity-cli/instances/*.json')):
    d = json.load(open(f, encoding='utf-8'))
    if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == proj: port = d['port']
code = open(sys.argv[1], encoding='utf-8').read()
body = json.dumps({'command': 'exec', 'params': {'code': code}}).encode()
d = json.loads(urllib.request.urlopen(urllib.request.Request(f'http://127.0.0.1:{port}/command', data=body, headers={'Content-Type': 'application/json'}), timeout=280).read())
print(d.get('success'), d.get('data') if d.get('success') else d.get('message'))
