# -*- coding: utf-8 -*-
"""구조 스펙(D-113) 조각 1 — 상자 A 원본 캐시를 Unity 가 읽을 로컬 좌표 JSON 으로.

입력: osm/structure_a_*.json (Overpass, ODbL) · dem/crop_structure_a.json (Copernicus GLO-30)
출력: out/structure_a/
  heights_8m.json  — 8m 격자 원래 고도(m, 배율 적용 전). 행 0 = 남쪽. Unity 가 배율(0.3)을 곱하고, 길 띠는 같은 값을 2m 로 겹선형 보간한다
                     (DEM 원본이 30m 라 2m 띠에 새 정보는 없음 — 띠는 다음 조각의 길 깎기/메우기 해상도용).
  network.json     — 차도(major·minor) + 보행로(alley=pedestrian)·계단(steps) 선분(class 로 구분), 로컬 미터 (x 동쪽, z 북쪽, 원점 = 상자 남서 모서리), highway·ref·name·osm_way
좌표: x = (lon − 서쪽 경도) × 111320·cos(남쪽 위도), z = (lat − 남쪽 위도) × 110574 (osm_to_network.py 와 같은 근사)

사용: py -X utf8 tools/town-gen/structure_prep.py
"""
import glob
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from fetch_sources import BOXES  # noqa: E402
from osm_to_network import DemCrop, osm_class, M_PER_DEG_LAT, m_per_deg_lon  # noqa: E402

NAME = "structure_a"
OUT = os.path.join(HERE, "out", NAME)
STEP = 8.0


def main():
    os.makedirs(OUT, exist_ok=True)
    s, w, n, e = BOXES[NAME]
    mlon = m_per_deg_lon(s)
    W, H = (e - w) * mlon, (n - s) * M_PER_DEG_LAT
    crop = DemCrop(NAME)
    nx, nz = int(W // STEP) + 1, int(H // STEP) + 1
    heights = []
    for j in range(nz):
        for i in range(nx):
            lat = s + (j * STEP) / M_PER_DEG_LAT
            lon = w + (i * STEP) / mlon
            heights.append(round(crop.at(lat, lon), 2))
    json.dump({"box_swne": [s, w, n, e], "width_m": W, "height_m": H, "step_m": STEP, "nx": nx, "nz": nz, "rows_south_to_north": True,
               "units": "m, raw DEM (height scale applied in Unity)",
               "attribution": "produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved",
               "heights_m": heights}, open(os.path.join(OUT, "heights_8m.json"), "w", encoding="utf-8"))

    osm = sorted(f for f in glob.glob(os.path.join(HERE, "osm", f"{NAME}_2*.json")) if not f.endswith(".meta.json"))[-1]
    doc = json.load(open(osm, encoding="utf-8"))
    segs, stats, length = [], {}, {}
    for el in doc["elements"]:
        if el.get("type") != "way" or "geometry" not in el:
            continue
        tags = el.get("tags", {})
        cls = osm_class(tags)
        if cls not in ("major", "minor", "alley", "steps"):   # 조각 5b(§4.2): 중심 골목(pedestrian)·공용 계단(steps) 자리도
            continue
        run = []

        def flush():
            if len(run) >= 2:
                segs.append({"id": len(segs) + 1, "class": cls, "highway": tags.get("highway", ""), "ref": tags.get("ref", ""), "name": tags.get("name", ""),
                             "osm_way": el["id"], "points_m": list(run)})
                L = sum(math.dist(run[k - 1], run[k]) for k in range(1, len(run)))
                length[tags.get("highway", "")] = length.get(tags.get("highway", ""), 0) + L
        for nd in el["geometry"]:
            x, z = (nd["lon"] - w) * mlon, (nd["lat"] - s) * M_PER_DEG_LAT
            if 0 <= x <= W and 0 <= z <= H:
                run.append([round(x, 2), round(z, 2)])
            else:
                flush(); run = []
        flush()
        stats[cls] = stats.get(cls, 0) + 1
    json.dump({"box_swne": [s, w, n, e], "width_m": W, "height_m": H, "source": os.path.basename(osm), "license": "© OpenStreetMap contributors, ODbL",
               "segments": segs}, open(os.path.join(OUT, "network.json"), "w", encoding="utf-8"), ensure_ascii=False)
    print(f"[structure_a] 땅 {W:.0f} x {H:.0f} m · 8m 격자 {nx} x {nz} = {nx * nz} 점 · 고도 {min(heights):.1f}~{max(heights):.1f} m(배율 전)")
    print(f"[structure_a] 차도 선분 {len(segs)} · 길이(m) " + ", ".join(f"{k} {v:.0f}" for k, v in sorted(length.items(), key=lambda kv: -kv[1])))


if __name__ == "__main__":
    main()
