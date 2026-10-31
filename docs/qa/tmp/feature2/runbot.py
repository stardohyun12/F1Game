import subprocess, time, sys
D = "docs/qa/tmp/feature2/"
def u(*a):
    r = subprocess.run(["bash", "scripts/ucli-http.sh", *a], capture_output=True, text=True, encoding="utf-8")
    return (r.stdout + r.stderr).strip()
print(u("editor", "play", "--wait"))
print(u("exec", "@" + D + "bot_start.cs"))
t0 = time.time(); last = ""
while time.time() - t0 < float(sys.argv[1]) if len(sys.argv) > 1 else 600:
    s = u("exec", "@" + D + "bot_status.cs")
    key = s.split(" frame=")[0]
    if key != last: print(int(time.time() - t0), "s", s, flush=True); last = key
    if key.startswith("status=FAIL") or key == "status=arrived" or "no director" in s: break
    time.sleep(5)
