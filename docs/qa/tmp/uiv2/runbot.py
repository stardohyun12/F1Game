# UI 2판: Play → 시간대(aft|dusk) 세팅 + 기능 2 봇 → 주행 중 Road/Compass 캡처 → 완주 대기 → 콘솔 → stop.
# ucli-http.sh 를 파이썬 subprocess 로 부르면 exec 가 "L15 ; 필요" 컴파일 실패를 돌려줬다(셸에서 직접은 됨, 원인 미확인)
# → 같은 HTTP 요청을 여기서 직접 보낸다.
import glob, json, os, sys, time, urllib.request
sys.stdout.reconfigure(encoding="utf-8")
D = "docs/qa/tmp/uiv2/"
n = sys.argv[1]
lim = float(sys.argv[2]) if len(sys.argv) > 2 else 420
PROJ = "d:/github/f1game/f1"


def port():
    for f in glob.glob(os.path.expanduser("~/.unity-cli/instances/*.json")):
        try: d = json.load(open(f, encoding="utf-8"))
        except Exception: continue
        if d.get("projectPath", "").replace("\\", "/").rstrip("/").lower() == PROJ and d.get("state") != "stopped": return d["port"]


def ex(code):
    for _ in range(20):
        p = port()
        if p is None: time.sleep(1); continue
        try:
            body = json.dumps({"command": "exec", "params": {"code": code}}).encode()
            req = urllib.request.Request(f"http://127.0.0.1:{p}/command", data=body, headers={"Content-Type": "application/json"})
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
for _ in range(30):
    time.sleep(2)
    r = exf(D + f"start_{n}.cs")
    if not r.startswith("FAIL") and "NOT PLAYING" not in r: break
print(r, flush=True)
t0 = time.time(); last = ""; cart_at = None; shots = 0
while time.time() - t0 < lim:
    s = exf("docs/qa/tmp/feature2/bot_status.cs")
    key = s.split(" frame=")[0]
    if key != last: print(int(time.time() - t0), "s", s, flush=True); last = key
    if "state=InCart" in s and cart_at is None: cart_at = time.time()
    if cart_at is not None and shots == 0 and time.time() - cart_at > 16:
        print("road:", exf(D + f"road_{n}.cs"), flush=True); shots = 1; shot_at = time.time()
    if shots == 1 and time.time() - shot_at > 5.5:
        print("compass:", exf(D + f"compass_{n}.cs"), flush=True); shots = 2; shot_at = time.time()
    if shots == 2 and time.time() - shot_at > 5.5:
        print(exf(D + "reset.cs"), flush=True); shots = 3
    if key.startswith("FAIL") or key.startswith("status=FAIL") or key == "status=arrived" or "delivered=True" in s or "no director" in s:
        break
    time.sleep(2)
time.sleep(2)
print("console:", exf(D + "console.cs"))
print(exf("docs/qa/tmp/feature2/bot_status.cs"))
print(ex('UnityEditor.EditorApplication.ExitPlaymode(); return "stop";'))
