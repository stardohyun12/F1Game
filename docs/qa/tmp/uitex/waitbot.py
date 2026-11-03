"""봇 끝(arrived/FAIL) 까지 15초마다 상태 확인 — 최대 9분."""
import subprocess, time
s = ""
for i in range(36):
    s = subprocess.run(["C:/Program Files/Git/bin/bash.exe", "scripts/ucli-http.sh", "exec", "@docs/qa/tmp/uitex/status.cs"],
                       capture_output=True, text=True, encoding="utf-8", errors="replace").stdout.strip()
    if any(k in s for k in ("arrived", "FAIL", "rror")):
        print(s); break
    time.sleep(15)
else:
    print("timeout:", s)
