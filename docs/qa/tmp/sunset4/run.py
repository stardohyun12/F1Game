# 사용: python docs/qa/tmp/sunset4/run.py <file.cs> [KEY=VAL ...] → 자리표(__KEY__)를 채워 에디터 exec
import subprocess, sys
src = open(sys.argv[1], encoding='utf-8').read()
for kv in sys.argv[2:]:
    k, v = kv.split('=', 1)
    src = src.replace('__%s__' % k, v)
r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', src], capture_output=True, text=True, encoding='utf-8')
print(r.stdout[-3000:], r.stderr[-1500:])
