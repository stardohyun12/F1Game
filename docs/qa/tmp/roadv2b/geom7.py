# 1b: (리컴파일 대기 →) RoadNetV2Build → fb_geom --radius 40 7건 요약.
#   python -X utf8 docs/qa/tmp/roadv2b/geom7.py <태그> [--build] [--refresh]
import json, os, sys, time
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "..", "scripts"))
import fb_geom
PROJ = "D:/Github/F1Game/F1"
tag = sys.argv[1] if len(sys.argv) > 1 else "x"


def ex(code, tries=80):
    for _ in range(tries):
        try:
            return fb_geom.ucli_exec(PROJ, code)
        except RuntimeError as e:
            if not any(k in str(e) for k in ("10061", "Expecting value", "connector")):
                raise
            time.sleep(3)
    raise RuntimeError("에디터 응답 없음")


if "--refresh" in sys.argv:
    try:
        ex('AssetDatabase.Refresh(); return "ok";', 3)
    except Exception:
        pass
    time.sleep(5)
    for _ in range(80):
        if "idle" in ex('return (EditorApplication.isCompiling || EditorApplication.isUpdating) ? "busy" : "idle";'):
            break
        time.sleep(3)
if "--build" in sys.argv:
    b = ex("return BuildBayLoop.RoadNetV2Build(150,-130,330,-20,15);")
    open(os.path.join(HERE, f"build_{tag}.txt"), "w", encoding="utf-8").write(b)
    for ln in b.splitlines():
        if any(k in ln for k in ("검사", "그래프", "맞춤", "겹침 위치", "재질", "토막", "rror")) and "진단" not in ln:
            print(ln[:230])
IDS = ["230810-268e", "230823-7da7", "230848-453e", "230939-4143", "231007-66ad", "231130-94a3", "032740-45c3"]
out = {}
for i in IDS:
    ok, items, res = fb_geom.run(i, 40.0)
    bad = [x for x in items if x["problem"]]
    out[i] = {"ok": ok, "fail": len(bad), "n": len(items), "bad": [f'{x["name"][:40]} | {x["problem"]} | {x["value"][:110]}' for x in bad]}
    print(f'{i}: {"PASS" if ok else "FAIL"} {len(bad)}/{len(items)}')
    for b in out[i]["bad"]:
        print("   ", b)
json.dump(out, open(os.path.join(HERE, f"geom_{tag}.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
