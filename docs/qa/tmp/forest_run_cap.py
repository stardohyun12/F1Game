"""artlive 20261031-1926: F1Studio Play → forest_cap.cs → Stop. 사용: python -X utf8 docs/qa/tmp/forest_run_cap.py <단계번호>  (tree_run_cap.py 와 같음)"""
import json, pathlib, sys, time, urllib.request
T = pathlib.Path('D:/Github/F1Game/docs/qa/tmp')
def port():
    for p in (pathlib.Path.home() / '.unity-cli/instances').glob('*.json'):
        d = json.loads(p.read_text(encoding='utf-8'))
        if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == 'd:/github/f1game/f1studio' and d.get('state') != 'stopped': return d['port']
def ex(code, to=200):
    req = urllib.request.Request(f'http://127.0.0.1:{port()}/command', data=json.dumps({'command': 'exec', 'params': {'code': code}}).encode(), headers={'Content-Type': 'application/json'})
    return json.loads(urllib.request.urlopen(req, timeout=to).read())
nn = sys.argv[1] if len(sys.argv) > 1 else '1'
print(ex(f'UnityEditor.SessionState.SetString("fo.cap", "{nn}"); UnityEditor.EditorApplication.EnterPlaymode(); return "play";'))
for i in range(90):
    time.sleep(1)
    try:
        if ex('return UnityEditor.EditorApplication.isPlaying ? "yes" : "no";', 10).get('data') == 'yes': break
    except Exception: pass
time.sleep(4)
try: print(json.dumps(ex((T / (sys.argv[2] if len(sys.argv) > 2 else 'forest_cap.cs')).read_text(encoding='utf-8-sig')), ensure_ascii=False))
finally:
    print(ex('UnityEditor.EditorApplication.ExitPlaymode(); return "stop";'))
