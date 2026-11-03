# D-235 낚시 파문: cs 파일을 에디터로 보내 결과 출력. 사용: python docs/qa/tmp/fishing/run.py a.cs [b.cs ...] (KEY=VAL 은 __KEY__ 치환)
import subprocess, sys, time
subs = {}
for a in sys.argv[1:]:
    if '=' in a and not a.endswith('.cs'):
        k, v = a.split('=', 1); subs['__' + k + '__'] = v; continue
    if a.startswith('sleep'):
        time.sleep(float(a[5:])); continue
    if a in ('play', 'stop'):
        r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'editor', a] + (['--wait'] if a == 'play' else []), capture_output=True, text=True, encoding='utf-8')
        print(a, r.stdout.strip()[-300:], r.stderr.strip()[-300:], flush=True); continue
    src = open('docs/qa/tmp/fishing/' + a, encoding='utf-8').read()
    for k, v in subs.items(): src = src.replace(k, v)
    r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', src], capture_output=True, text=True, encoding='utf-8')
    print(a, '->', r.stdout.strip()[-1500:], r.stderr.strip()[-800:], flush=True)
