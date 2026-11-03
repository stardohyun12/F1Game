"""artlive 20261031-0205 집 유형: F1Studio 에 cs 실행. 사용: python -X utf8 docs/qa/tmp/ht_ex.py <file.cs> [--play]  (--play = Play 들어가 실행 후 Stop)"""
import json, pathlib, sys, time, urllib.request
def port():
    for p in (pathlib.Path.home() / '.unity-cli/instances').glob('*.json'):
        try: d = json.loads(p.read_text(encoding='utf-8'))
        except (OSError, ValueError): continue   # 다른 에디터가 쓰는 중인 파일은 건너뜀
        if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == 'd:/github/f1game/f1studio' and d.get('state') != 'stopped': return d['port']
def ex(code, to=300):
    req = urllib.request.Request(f'http://127.0.0.1:{port()}/command', data=json.dumps({'command': 'exec', 'params': {'code': code}}).encode(), headers={'Content-Type': 'application/json'})
    return json.loads(urllib.request.urlopen(req, timeout=to).read())
code = pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig')
if '--play' not in sys.argv: print(json.dumps(ex(code), ensure_ascii=False)); sys.exit()
ex('UnityEditor.EditorApplication.EnterPlaymode(); return "play";')
for i in range(90):
    time.sleep(1)
    try:
        if ex('return UnityEditor.EditorApplication.isPlaying ? "yes" : "no";', 10).get('data') == 'yes': break
    except Exception: pass
time.sleep(4)
try: print(json.dumps(ex(code), ensure_ascii=False))
finally: print(ex('UnityEditor.EditorApplication.ExitPlaymode(); return "stop";'))
