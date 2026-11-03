# artlive 2056: Play 켜고 → field_patch_cap.cs 가 OK 낼 때까지 5초 간격 재시도 → Play 끄기
import subprocess, sys, time
T = 'D:/Github/F1Game/docs/qa/tmp/'
ux = lambda f: subprocess.run([sys.executable, '-X', 'utf8', T + 'fp2056/ux.py', T + f], capture_output=True, text=True, encoding='utf-8').stdout.strip()
print(ux('fp2056/play.cs'))
for i in range(24):
    time.sleep(5); r = ux('field_patch_cap.cs'); print(r[:400])
    if r.startswith('True OK'): break
time.sleep(1); print(ux('fp2056/stop.cs'))
