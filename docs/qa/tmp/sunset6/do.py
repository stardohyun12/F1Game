# 사용: python docs/qa/tmp/sunset6/do.py <step> ...
#  wait                       컴파일·임포트 끝날 때까지
#  import <asset path>        강제 임포트 후 wait
#  cs <file.cs> K=V ...       자리표 채워 exec
#  sleep <s>
# 여러 단계를 ';' 로 이어 한 번에: do.py import Assets/x.cs ';' cs a.cs TAG=x
import subprocess, sys, time


def ex(src):
    r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', src], capture_output=True, text=True, encoding='utf-8')
    return (r.stdout.strip() + ' ' + r.stderr.strip()).strip()


def wait():
    for _ in range(60):
        r = ex('return (UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating) ? "busy" : "idle";')
        if r.endswith('idle'):
            return 'idle'
        time.sleep(3)
    return 'timeout'


steps, cur = [], []
for a in sys.argv[1:]:
    if a == ';':
        steps.append(cur); cur = []
    else:
        cur.append(a)
steps.append(cur)
for st in steps:
    if not st:
        continue
    op = st[0]
    if op == 'wait':
        time.sleep(4); print('wait', wait())
    elif op == 'import':
        print(ex('UnityEditor.AssetDatabase.ImportAsset("%s", UnityEditor.ImportAssetOptions.ForceUpdate); return "imp";' % st[1]))
        time.sleep(4); print('wait', wait())
    elif op == 'sleep':
        time.sleep(float(st[1]))
    elif op == 'cs':
        src = open(st[1], encoding='utf-8').read()
        for kv in st[2:]:
            k, v = kv.split('=', 1)
            src = src.replace('__%s__' % k, v)
        print(st[1].split('/')[-1], st[2:3], '->', ex(src)[-1500:], flush=True)
    elif op == 'raw':
        print(ex(st[1])[-2000:])
