import subprocess, sys, time
t0=time.time(); lim=float(sys.argv[1]); prev=None
while time.time()-t0<lim:
    r=subprocess.run(['bash','scripts/ucli-http.sh','exec','return OnFootRouteBot.Status;'],capture_output=True,text=True,encoding='utf-8')
    s=(r.stdout or r.stderr).strip()
    if s!=prev: print(round(time.time()-t0), s, flush=True); prev=s
    if s.startswith('done') or 'FAIL' in s: break
    time.sleep(8)
else: print('timeout', prev)
