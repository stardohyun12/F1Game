# 사용: python docs/qa/tmp/sunset4/cap.py PRE YAW PITCH v1,v3,v4  → cap.cs 자리표를 채워 에디터로 보냄
import subprocess, sys
pre, yaw, pitch, vers = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
src = open('docs/qa/tmp/sunset4/cap.cs', encoding='utf-8').read()
src = src.replace('__PRE__', pre).replace('__YAW__', yaw).replace('__PITCH__', pitch)
src = src.replace('__VERS__', ', '.join('"%s"' % v for v in vers.split(',')))
r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', src], capture_output=True, text=True, encoding='utf-8')
print(r.stdout[-2000:], r.stderr[-2000:])
