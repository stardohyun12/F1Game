# 과제 0640 — 봇이 끝날 때까지 기다림(셸 반복문 대신). 사용: python docs/qa/tmp/v6h_wait.py [최대초]
import subprocess, sys, time
lim = float(sys.argv[1]) if len(sys.argv) > 1 else 560
t0 = time.time(); s = ''
while time.time() - t0 < lim:
    r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', 'return TownPathBot.Status;'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    s = (r.stdout.strip().splitlines() or [''])[-1]
    if s.startswith('done'): break
    time.sleep(10)
print(s, 'waited %.0fs' % (time.time() - t0))
