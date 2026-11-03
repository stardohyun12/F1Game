# UI 1판 방향 표시 크기 비교(D-243): Play → 기능 2 봇 → 주행 중 3번, 시간 정지 상태에서 1판 크기 / 새 크기를 같은 프레임으로 캡처 → 완주 대기 → 콘솔 → stop.
import glob, json, os, sys, time, urllib.request
sys.stdout.reconfigure(encoding="utf-8")
D = "docs/qa/tmp/uiv1b/"
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
t0 = time.time(); last = ""; cart_at = None; shots = 0; shot_at = 0
SH = [14, 26, 38]
while time.time() - t0 < lim:
    s = exf("docs/qa/tmp/feature2/bot_status.cs")
    key = s.split(" frame=")[0]
    if key != last: print(int(time.time() - t0), "s", s, flush=True); last = key
    if "state=InCart" in s and cart_at is None: cart_at = time.time()
    if cart_at is not None and shots < len(SH) and time.time() - cart_at > SH[shots] and time.time() - shot_at > 3:
        code = open(D + "cmp.cs", encoding="utf-8").read().replace("TAG", "drive%d" % shots)
        print("cmp", shots, ex(code), flush=True); shots += 1; shot_at = time.time()
    if key.startswith("FAIL") or key.startswith("status=FAIL") or key == "status=arrived" or "delivered=True" in s or "no director" in s:
        break
    time.sleep(2)
time.sleep(2)
print("console:", exf(D + "console.cs"))
print(exf("docs/qa/tmp/feature2/bot_status.cs"))
print(ex('UnityEditor.EditorApplication.ExitPlaymode(); return "stop";'))
