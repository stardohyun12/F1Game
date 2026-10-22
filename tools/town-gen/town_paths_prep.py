# -*- coding: utf-8 -*-
"""구조 조각 11 — 아말피 중심(항구점 반경 400m) OSM 길 전 종류를 Unity 가 읽을 로컬 좌표 JSON 으로.

조각 10 까지 network.json 은 major·minor·alley(pedestrian)·steps 만 담았다 → footway·path 가 통째로 빠졌고,
광장(area=yes pedestrian)은 둘레 선으로 들어가 둘레 건물을 "길에 걸침"으로 뺐다. tunnel/covered/layer 태그도 버렸다.

입력: osm/structure_a_<날짜>.json (Overpass way[highway], ODbL)
출력: out/structure_a/town_paths.json
  ways  — highway 가 있는 선(area=yes 제외), 반경 안 토막만. kind = car(SplineTown 차도가 그림) / path(조각 11 이 그림)
          under = tunnel 또는 covered 가 yes/building_passage → 건물 아래로 지나감(건물을 빼지 않는다)
          width_m = OSM width 태그(있으면, 1~12 로 자름) 아니면 종류별 가설 폭
  areas — highway=pedestrian 등 + area=yes 닫힌 고리 = 실제 광장 바닥(반시계, 닫는 점 없음)
  stats — 종류별 길이(반경 안), 태그별 길이
좌표: structure_prep.py 와 같음 (x 동쪽, z 북쪽, 원점 = 상자 남서 모서리)

사용: py -X utf8 tools/town-gen/town_paths_prep.py
"""
import glob
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from fetch_sources import BOXES  # noqa: E402
from osm_to_network import M_PER_DEG_LAT, m_per_deg_lon  # noqa: E402

NAME = "structure_a"
OUT = os.path.join(HERE, "out", NAME)
S, W, N, E = BOXES[NAME]
MLON = m_per_deg_lon(S)
HARBOR = ((14.6025 - W) * MLON, (40.6335 - S) * M_PER_DEG_LAT)   # TownGenStructure.harborGeo
RADIUS = 400.0

# 차도 = SplineTown 이 곡선 차도/골목으로 그리는 종류(조각 10 과 같은 처리). 나머지는 조각 11 이 OSM 선 그대로 그린다.
CAR = {"motorway", "trunk", "primary", "secondary", "tertiary", "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link", "residential", "unclassified", "road"}
# 종류별 전체 폭 가설(m). width 태그가 있으면 그걸 쓴다.
WIDTH = {"living_street": 4.0, "service": 4.0, "pedestrian": 3.5, "footway": 2.0, "steps": 2.0, "path": 1.5, "track": 3.0, "cycleway": 2.0, "corridor": 2.0}
UNDER = {"yes", "building_passage"}


def local(nd):
    return ((nd["lon"] - W) * MLON, (nd["lat"] - S) * M_PER_DEG_LAT)


def area(r):
    return 0.5 * sum(r[i][0] * r[(i + 1) % len(r)][1] - r[(i + 1) % len(r)][0] * r[i][1] for i in range(len(r)))


def width_of(tags, hw):
    w = tags.get("width", "").replace("m", "").replace(",", ".").strip()
    try:
        v = float(w)
        if 0.5 <= v <= 30:
            return min(max(v, 1.0), 12.0), "tag"
    except ValueError:
        pass
    return WIDTH.get(hw, 2.0), "guess"


def main():
    osm = sorted(f for f in glob.glob(os.path.join(HERE, "osm", f"{NAME}_2*.json")) if not f.endswith(".meta.json"))[-1]
    doc = json.load(open(osm, encoding="utf-8"))
    ways, areas, length, tagLen = [], [], {}, {}
    for el in doc["elements"]:
        if el.get("type") != "way" or "geometry" not in el:
            continue
        tags = el.get("tags", {})
        hw = tags.get("highway")
        if not hw:
            continue
        pts = [local(nd) for nd in el["geometry"]]
        if tags.get("area") == "yes":
            ring = pts[:-1] if len(pts) > 1 and math.dist(pts[0], pts[-1]) < 0.05 else pts
            if len(ring) >= 3 and any(math.dist(p, HARBOR) <= RADIUS for p in ring):
                if area(ring) < 0:
                    ring = ring[::-1]
                areas.append({"osm_way": el["id"], "highway": hw, "name": tags.get("name", ""), "area_m2": round(abs(area(ring)), 1), "ring": [[round(x, 2), round(z, 2)] for x, z in ring]})
            continue
        under = tags.get("tunnel") in UNDER or tags.get("covered") in UNDER
        layer = tags.get("layer", "")
        wd, wsrc = width_of(tags, hw)
        kind = "car" if hw in CAR else "path"
        # 반경 안 토막만(구간 중점 기준)
        run = []

        def flush():
            if len(run) >= 2:
                L = sum(math.dist(run[k - 1], run[k]) for k in range(1, len(run)))
                ways.append({"osm_way": el["id"], "highway": hw, "kind": kind, "name": tags.get("name", ""), "under": under, "tunnel": tags.get("tunnel", ""), "covered": tags.get("covered", ""),
                             "layer": layer, "width_m": wd, "width_src": wsrc, "length_m": round(L, 2), "points_m": [[round(x, 2), round(z, 2)] for x, z in run]})
                length[hw] = length.get(hw, 0) + L
                for k in ("tunnel", "covered", "layer"):
                    if tags.get(k):
                        key = f"{k}={tags[k]}"; tagLen[key] = tagLen.get(key, 0) + L
        for k, p in enumerate(pts):
            inside = math.dist(p, HARBOR) <= RADIUS
            if inside:
                if not run and k > 0:
                    run.append(pts[k - 1])   # 반경 경계를 넘는 구간도 잇는다
                run.append(p)
            elif run:
                run.append(p); flush(); run = []
        flush()
    out = {"source": os.path.basename(osm), "license": "© OpenStreetMap contributors, ODbL", "harbor": [round(HARBOR[0], 2), round(HARBOR[1], 2)], "radius_m": RADIUS,
           "width_guess_m": WIDTH, "car_highways": sorted(CAR), "ways": ways, "areas": areas,
           "stats": {"length_m_by_highway": {k: round(v, 1) for k, v in sorted(length.items(), key=lambda kv: -kv[1])},
                     "length_m_by_tag": {k: round(v, 1) for k, v in sorted(tagLen.items(), key=lambda kv: -kv[1])},
                     "areas": [{"name": a["name"], "area_m2": a["area_m2"]} for a in areas]}}
    os.makedirs(OUT, exist_ok=True)
    json.dump(out, open(os.path.join(OUT, "town_paths.json"), "w", encoding="utf-8"), ensure_ascii=False)
    print(f"[town_paths] 선 {len(ways)} (path {sum(1 for w in ways if w['kind'] == 'path')}) · 광장 면 {len(areas)} · 아래로 지나감 {sum(1 for w in ways if w['under'])}")
    print("[town_paths] 길이(m) " + ", ".join(f"{k} {v:.0f}" for k, v in sorted(length.items(), key=lambda kv: -kv[1])))


if __name__ == "__main__":
    main()
