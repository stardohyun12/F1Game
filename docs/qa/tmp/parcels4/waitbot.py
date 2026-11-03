# 0236(parcels3 사본, done 에서만 멈춤): TownPathBot.Status 가 끝 상태가 될 때까지 20초마다 묻는다 (셸 until 루프 대신)
import json, glob, os, sys, time, urllib.request
proj = "d:/github/f1game/f1"
port = None
for f in glob.glob(os.path.expanduser("~/.unity-cli/instances/*.json")):
    try: d = json.load(open(f, encoding="utf-8"))
    except Exception: continue
    if d.get("projectPath", "").replace("\\", "/").rstrip("/").lower() == proj and d.get("state") != "stopped": port = d["port"]; break
u = f"http://127.0.0.1:{port}/command"
def ask():
    body = {"command": "exec", "params": {"code": "return TownPathBot.Status;"}}
    req = urllib.request.Request(u, data=json.dumps(body).encode(), headers={"Content-Type": "application/json"})
    return str(json.loads(urllib.request.urlopen(req, timeout=60).read()).get("data"))
t0 = time.time(); s = ""
while time.time() - t0 < float(sys.argv[1] if len(sys.argv) > 1 else 840):
    try: s = ask()
    except Exception as e: s = "ERR " + str(e)
    low = s.lower()
    if low.startswith("done") or low.startswith("err"): break
    time.sleep(20)
print(round(time.time() - t0), "s:", s)
