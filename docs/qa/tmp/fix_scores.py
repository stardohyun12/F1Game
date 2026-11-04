import json, re, io
# scores.json 의 장소 이름이 인코딩으로 깨져 있어서(에디터가 CP949 로 씀) 좌표로 world.yaml 장소 이름을 다시 붙인다.
raw = open("docs/qa/evidence/debug-view/scores.json", "rb").read().decode("utf-8", errors="replace")
rows = json.loads(raw)
yaml_text = open("world/world.yaml", encoding="utf-8").read()
places = []
for m in re.finditer(r"- \{name: (.+?),\s+role:.*?feet: \[([-\d.]+), ([-\d.]+), ([-\d.]+)\]", yaml_text):
    places.append((m.group(1).strip(), float(m.group(2)), float(m.group(4))))
dps = {"DP_MART": (225.1, -78.3), "DP_depot": (270.0, -34.6), "DP_hill_field": (180.8, 6.9),
       "DP_lighthouse": (-378.0, -361.1), "DP_tea": (93.8, -288.0), "DP_top_house": (-295.4, 357.9)}
for r in rows:
    if r["name"].startswith("place:"):
        best = min(places, key=lambda p: (p[1] - r["x"]) ** 2 + (p[2] - r["z"]) ** 2)
        r["name"] = "place:" + best[0]
json.dump(rows, open("docs/qa/evidence/debug-view/scores.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(len(rows), "rows")
for r in rows[:10]:
    ok = r["building"] >= 0.10 and r["building"] <= 0.40 and r["sea"] >= 0.10 and r["sky"] >= 0.20 and not r["blocked"]
    print(f'{r["rank"]:>2} {r["name"]:<22} score {r["score"]:6.1f}  ({r["x"]:.1f},{r["y"]:.1f},{r["z"]:.1f}) yaw {r["yaw"]:.0f}  건물 {r["building"]*100:4.1f}%  바다 {r["sea"]*100:4.1f}%  하늘 {r["sky"]*100:4.1f}%  가림 {"예" if r["blocked"] else "없음"}  {"통과" if ok else "-"}')
