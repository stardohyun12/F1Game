# CamJitterProbe.Done 이 될 때까지 기다렸다가 Summary 출력. usage: cam_wait.py [timeout]
import subprocess, sys, time
to = float(sys.argv[1]) if len(sys.argv) > 1 else 60
t0 = time.time()
while time.time() - t0 < to:
    r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', 'return CamJitterProbe.Done ? CamJitterProbe.Summary : "";'], capture_output=True, text=True, encoding='utf-8')
    s = r.stdout.strip()
    if s: print(s); sys.exit(0)
    time.sleep(2)
print('TIMEOUT'); sys.exit(1)
