# 1935: fb_geom 의 probe/judge 를 피드백 파일 없이 두 블록 중심에 돌린다. 사용: python -X utf8 docs/qa/tmp/sink1935/geom.py <tag>
import json, os, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../.."))
sys.path.insert(0, os.path.join(ROOT, "scripts"))
import fb_geom
tag = sys.argv[1] if len(sys.argv) > 1 else "now"
pts = {"A": [115, 8, 23], "B": [112, 8, -47]}
res = {}
for k, p in pts.items():
    items = fb_geom.judge(fb_geom.probe({"point": p, "scene": "Map_TownV6", "space": "unity-world"}, 35.0, None))
    res[k] = {"pass": all(i["problem"] is None for i in items), "items": items}
    bad = [i for i in items if i["problem"]]
    print(f"{k}: {'PASS' if not bad else 'FAIL'} items {len(items)} bad {len(bad)}")
    for i in bad:
        print("   ", i["name"], i["problem"], i["value"])
with open(os.path.join(os.path.dirname(__file__), f"geom_{tag}.json"), "w", encoding="utf-8") as fh:
    json.dump(res, fh, ensure_ascii=False, indent=1)
