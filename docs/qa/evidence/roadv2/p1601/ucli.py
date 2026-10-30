"""본 F1 에디터 HTTP: refresh(컴파일 대기·오류 확인) / exec 파일 / build(빌드 로그 저장).
usage: ucli.py refresh | ucli.py exec <file.cs> [out] | ucli.py build <out.log>"""
import glob, json, os, sys, time, urllib.request
PROJ = 'd:/github/f1game/f1'
def port():
    for f in glob.glob(os.path.expanduser('~/.unity-cli/instances/*.json')):
        try: d = json.load(open(f, encoding='utf-8'))
        except Exception: continue
        if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == PROJ and d.get('state') != 'stopped': return d['port']
def post(code, timeout=600):
    for _ in range(120):
        p = port()
        try:
            req = urllib.request.Request(f'http://127.0.0.1:{p}/command', data=json.dumps({'command': 'exec', 'params': {'code': code}}).encode(), headers={'Content-Type': 'application/json'})
            d = json.loads(urllib.request.urlopen(req, timeout=timeout).read())
            if d.get('success'): return d.get('data')
            msg = str(d.get('message'))
            if 'compil' in msg.lower() or 'reload' in msg.lower(): time.sleep(2); continue
            return 'FAIL: ' + msg
        except Exception as e:
            time.sleep(2)
    return 'FAIL: timeout'
cmd = sys.argv[1]
if cmd == 'refresh':
    print(post('UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate); return "r";'))
    time.sleep(4)
    for _ in range(150):
        r = post('return UnityEditor.EditorApplication.isCompiling + "|" + UnityEditor.EditorApplication.isUpdating + "|" + UnityEditor.EditorUtility.scriptCompilationFailed;')
        if r and r.startswith('False|False'): print('ready', r); break
        time.sleep(2)
elif cmd == 'exec':
    out = post(open(sys.argv[2], encoding='utf-8').read())
    if len(sys.argv) > 3: open(sys.argv[3], 'w', encoding='utf-8').write(str(out)); print('saved', len(str(out)))
    else: print(out)
elif cmd == 'build':
    t = time.time(); out = str(post('return BuildBayLoop.RoadNetV2BuildAll();')).replace('\\n', '\n')
    open(sys.argv[2], 'w', encoding='utf-8').write(out)
    print('%.0fs' % (time.time() - t))
    for l in out.split('\n'):
        if any(k in l for k in ['검사:', '그래프:', '입구 겹침', '접힘 위치', '겹침 위치', '다리라', 'FAIL', 'error', 'Exception', '큰길 띠 밖']): print(l[:300])
