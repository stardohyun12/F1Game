"""TownPathBot.Status 가 done 이 될 때까지 기다린다. 사용: python -X utf8 docs/qa/tmp/botwait.py <최대초>"""
import subprocess, sys, time
t0 = time.time(); lim = float(sys.argv[1]) if len(sys.argv) > 1 else 900
while time.time() - t0 < lim:
    r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', 'return TownPathBot.Status;'], capture_output=True, text=True, encoding='utf-8')
    s = (r.stdout or r.stderr).strip()
    if s.startswith('done'): print(s, round(time.time() - t0)); break
    time.sleep(10)
else:
    print('timeout', s)
