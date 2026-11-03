# 기능 2 봇이 끝날 때까지(Status 가 done/FAIL) 15초마다 확인 — D-252 작업용
import subprocess, time, sys
code = 'return OnFootRouteBot.Status;'
for i in range(60):
    r = subprocess.run(["bash", "scripts/ucli-http.sh", "exec", code], capture_output=True, text=True, encoding="utf-8", cwd="D:/Github/F1Game").stdout.strip()
    if r.startswith("done") or r.startswith("FAIL"):
        print(r); sys.exit(0)
    time.sleep(15)
print("timeout", r)
