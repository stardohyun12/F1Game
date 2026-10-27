import subprocess, time, sys
end = time.time() + float(sys.argv[1] if len(sys.argv) > 1 else 450)
while time.time() < end:
    r = subprocess.run(["bash", "scripts/ucli-http.sh", "exec", 'return UnityEditor.SessionState.GetString("op.qa","");'], capture_output=True, text=True, encoding="utf-8").stdout.strip()
    if r:
        print(r[:3500]); sys.exit(0)
    time.sleep(10)
print("still running")
