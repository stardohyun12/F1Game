# D-245 검증: 새 Play → 빈 카메라 판정 → 기능 2 봇 1회 완주 → 콘솔 에러 → 완주 중 판정 3번 → stop.
import glob, json, os, sys, time, urllib.request
sys.stdout.reconfigure(encoding="utf-8")
D = "docs/qa/tmp/nodisp/"
lim = float(sys.argv[1]) if len(sys.argv) > 1 else 420
PROJ = "d:/github/f1game/f1"


def port():
    for f in glob.glob(os.path.expanduser("~/.unity-cli/instances/*.json")):
        try: d = json.load(open(f, encoding="utf-8"))
        except Exception: continue
        if d.get("projectPath", "").replace("\\", "/").rstrip("/").lower() == PROJ and d.get("state") != "stopped": return d["port"]


def ex(code):
    last = None
    for _ in range(20):
        p = port()
        if p is None: time.sleep(1); continue
        try:
            body = json.dumps({"command": "exec", "params": {"code": code}}).encode()
            req = urllib.request.Request("http://127.0.0.1:%d/command" % p, data=body, headers={"Content-Type": "application/json"})
            d = json.loads(urllib.request.urlopen(req, timeout=280).read())
            if not d.get("success"): return "FAIL: " + str(d.get("message"))
            out = d.get("data"); return out if isinstance(out, str) else json.dumps(out, ensure_ascii=False)
        except Exception as e:
            time.sleep(1); last = e
    return "FAIL: request " + str(last)


def exf(path): return ex(open(path, encoding="utf-8").read())


print(ex('UnityEditor.EditorApplication.EnterPlaymode(); return "play";'))
for _ in range(60):
    time.sleep(1)
    if ex("return UnityEditor.EditorApplication.isPlaying ? \"yes\" : \"no\";") == "yes": break
r = ""
for _ in range(30):
    time.sleep(2)
    r = exf(D + "start.cs")
    if not r.startswith("FAIL") and "NOT PLAYING" not in r: break
print(r, flush=True)
t0 = time.time(); last = ""; checks = 0; ck_at = 0
while time.time() - t0 < lim:
    s = exf("docs/qa/tmp/feature2/bot_status.cs")
    key = s.split(" frame=")[0]
    if key != last: print(int(time.time() - t0), "s", s, flush=True); last = key
    if checks < 3 and time.time() - ck_at > 40:
        print("ref", checks, exf(D + "ref.cs"), flush=True); checks += 1; ck_at = time.time()
    if key.startswith("FAIL") or key.startswith("status=FAIL") or key == "status=arrived" or "delivered=True" in s or "no director" in s:
        break
    time.sleep(2)
time.sleep(2)
print("ref end", exf(D + "ref.cs"))
print("console:", exf("docs/qa/tmp/uiv1b/console.cs"))
print(exf("docs/qa/tmp/feature2/bot_status.cs"))
print(ex('UnityEditor.EditorApplication.ExitPlaymode(); return "stop";'))
