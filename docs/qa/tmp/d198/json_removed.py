# town-v6b.json 에 D-198 제거 목록을 넣음(씬에서 끈 길·지운 OSM 건물). 다른 키는 그대로.
import json, os
H = os.path.dirname(os.path.abspath(__file__)); R = os.path.abspath(os.path.join(H, "../../../.."))
fp = os.path.join(R, "docs/map/town-v6b.json"); raw = open(fp, encoding="utf8").read(); j = json.loads(raw)
rid = j["roads"][47]["id"]; assert "726989353" in str(rid), rid
ways = sorted({w for v in json.load(open(os.path.join(H, "osm_hits.json"))).values() for w in v})
j["removed"] = {"roads": [rid], "osm_ways": ways, "why": "D-198 도현 배치(placements.yaml) — 정비소가 마을길 위, 보관소·우체국이 OSM 건물 위. 씬(Map_TownV6)에서 끈 것과 같은 목록. 빌더(BuildBayLoop.Gray)는 아직 이 키를 안 읽음"}
# 서식 diff 를 안 만들려고 끝의 } 앞에 키 하나만 텍스트로 덧붙임
assert "removed" not in json.loads(raw)
body = raw.rstrip(); assert body.endswith("}")
new = body[:-1].rstrip() + ",\n\"removed\": " + json.dumps(j["removed"], ensure_ascii=False) + "\n}\n"
json.loads(new); open(fp, "w", encoding="utf8", newline="").write(new)
print(rid, ways, "bytes", len(raw), "->", len(new))
