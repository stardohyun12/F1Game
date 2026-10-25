# 사용: python bl_waitkey.py <SessionState 키> [timeout초] — 값에 DONE 이 들어올 때까지 3초마다 에디터에 묻고, 값을 출력
import subprocess, sys, time
key = sys.argv[1]; limit = float(sys.argv[2]) if len(sys.argv) > 2 else 600
code = 'return UnityEditor.SessionState.GetString("%s","");' % key
t0 = time.time(); out = ""
while time.time() - t0 < limit:
    out = subprocess.run(["bash", "scripts/ucli-http.sh", "exec", code], capture_output=True, text=True, encoding="utf-8").stdout
    if "DONE" in out: break
    time.sleep(3)
print(out.strip() or "TIMEOUT")
