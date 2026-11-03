"""Play 앉은 상태: 15 s 연속 1 s 간격 16장(snap.cs, q00..q15) + 상태 JSON(+ 갈매기 울음) → evidence/sunset9/strip15.jsonl
시작 때 다음 갈매기 무리를 3 s 뒤로 당긴다(15 s 안에 날기·울음이 한 번 들어오게 — 실제 간격은 22~38 s)."""
import subprocess, sys, time, pathlib, json
here = pathlib.Path(__file__).parent
out = here.parents[2] / "qa/evidence/sunset9/strip15.jsonl"
pull = ('var db = SunsetLook.Find().backdrop.GetComponent<DuskBackdrop>(); var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;'
        ' typeof(DuskBackdrop).GetField("flightNext", bf).SetValue(db, Time.time + 3f); return "pulled t=" + Time.time;')
print(subprocess.run(["bash", "scripts/ucli-http.sh", "exec", pull], cwd=here.parents[3], capture_output=True, text=True, encoding="utf-8").stdout.strip())
rows = []; t0 = time.time()
for i in range(16):
    while time.time() - t0 < i * 1.0: time.sleep(0.02)
    r = subprocess.run([sys.executable, str(here / "run.py"), "snap.cs", f"TAG=q{i:02d}"], capture_output=True, text=True, encoding="utf-8")
    g = subprocess.run([sys.executable, str(here / "run.py"), "gullsync.cs"], capture_output=True, text=True, encoding="utf-8")
    try:
        j = json.loads(r.stdout.strip()); j["sync"] = json.loads(g.stdout.strip()); j["wall"] = round(time.time() - t0, 2)
    except Exception as e:
        j = {"tag": f"q{i:02d}", "err": (r.stdout + g.stdout)[:200]}
    rows.append(json.dumps(j, ensure_ascii=False)); print(rows[-1][:230], flush=True)
out.write_text("\n".join(rows) + "\n", encoding="utf-8")
