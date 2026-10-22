# -*- coding: utf-8 -*-
"""앞단 A (OSM) + 공통 고도맵 — 아말피 항구 조각 + 포지타노 비탈 조각을 480x400m 땅 하나로 섞는다 (D-107).

스펙 §6.1·§6.3. 실제 미터 그대로(확대·축소 없음). 원점 = 땅 남서 모서리, +z = 북.
- 아말피 조각: 땅 z 0~100m. 항구 중심 경도에서 좌우 240m, 남쪽 끝 = 해안선 - SEA_MARGIN.
- 포지타노 조각: 땅 z 100~400m. 해변 중심 경도에서 좌우 240m, 해안선 + POS_SKIP 부터 300m.
- 두 조각 경계 z 80~120m 에서 고도를 섞는다. 도로는 각 조각 범위 안만 쓰고, 경계는 뒷단 A* 가 잇는다.

출력 (tools/town-gen/out/):
  osm_network.json   — §4.1 선형 도로망 (source=osm)
  heights_480x400.json — 공통 고도맵: 4m 간격 격자, row 0 = 남쪽. 두 앞단(OSM·roadgen)이 같이 쓴다.
사용: py -X utf8 tools/town-gen/osm_to_network.py
"""
import glob
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out")

BOUNDS_X, BOUNDS_Z = 480.0, 400.0
SEAM_Z = 100.0          # 아말피 | 포지타노 경계
BLEND = 20.0            # 경계 ± 이만큼 고도 섞기
SEA_MARGIN = 16.0       # 아말피 해안선 아래로 바다를 이만큼 땅에 포함
POS_SKIP = 40.0         # 포지타노는 해변에서 이만큼 북쪽부터 (바다 두 번 생기지 않게)
HSTEP = 4.0             # 고도맵 간격 m

AMALFI_LON = 14.6027    # 아말피 항구(두오모 앞 광장·방파제) 중심 경도
POSITANO_LON = 14.4853  # 포지타노 Spiaggia Grande 중심 경도

# 변형: 기본("") = 1차 실험(TownGen_OSM 등 씬 4개 입력). "c" = 곡선 큰길 마을(spline-town §2.2 C):
#   포지타노 조각을 동쪽 60m · 해안선 북쪽 130m 부터로 옮겨 SS163(primary)이 땅 안에 들어오게. 출력 파일에 _c 접미사.
import sys as _sys
VARIANT = _sys.argv[_sys.argv.index("--variant") + 1] if "--variant" in _sys.argv else ""
POS_X_OFFSET = 60.0 if VARIANT == "c" else 0.0
if VARIANT == "c":
    POS_SKIP = 130.0
    # BLEND 60 시도(2026-10-20): 깎기 최대 11.1→9.7m 로 줄었으나 넓어진 비탈을 8% 로 오르려 잇기 길이 1.3→3.9km 로 폭증 → 되돌림(BUG-008 진단 참고)
SUFFIX = "_" + VARIANT if VARIANT else ""

M_PER_DEG_LAT = 110574.0


def m_per_deg_lon(lat):
    return 111320.0 * math.cos(math.radians(lat))


class DemCrop:
    def __init__(self, name):
        d = json.load(open(os.path.join(HERE, "dem", f"crop_{name}.json"), encoding="utf-8"))
        self.lat0, self.lon0, self.dlat, self.dlon = d["lat0"], d["lon0"], d["dlat"], d["dlon"]
        self.w, self.h, self.v = d["w"], d["h"], d["heights_m"]

    def at(self, lat, lon):
        fy = (self.lat0 - lat) / self.dlat   # 행 0 = 북
        fx = (lon - self.lon0) / self.dlon
        fy = min(max(fy, 0), self.h - 1); fx = min(max(fx, 0), self.w - 1)
        x0, y0 = int(fx), int(fy); x1, y1 = min(x0 + 1, self.w - 1), min(y0 + 1, self.h - 1)
        tx, ty = fx - x0, fy - y0
        g = lambda x, y: self.v[y * self.w + x]
        a = g(x0, y0) * (1 - tx) + g(x1, y0) * tx
        b = g(x0, y1) * (1 - tx) + g(x1, y1) * tx
        return a * (1 - ty) + b * ty

    def coast_lat(self, lon, thresh=1.0):
        """중심 경도에서 남쪽으로 내려가며 고도가 thresh 미만이 되는 첫 위도 = 해안선."""
        lat = self.lat0
        while lat > self.lat0 - self.dlat * (self.h - 1):
            if self.at(lat, lon) < thresh:
                return lat
            lat -= self.dlat / 4
        return lat


class Piece:
    """조각 하나: 땅 로컬 (x, z) <-> (lat, lon). z_lo~z_hi 가 이 조각의 땅 범위."""

    def __init__(self, crop, center_lon, south_lat, z_lo, z_hi):
        self.crop, self.center_lon, self.south_lat, self.z_lo, self.z_hi = crop, center_lon, south_lat, z_lo, z_hi
        self.mlon = m_per_deg_lon(south_lat)

    def to_local(self, lat, lon):
        return (lon - self.center_lon) * self.mlon + BOUNDS_X / 2, (lat - self.south_lat) * M_PER_DEG_LAT + self.z_lo

    def to_geo(self, x, z):
        return self.south_lat + (z - self.z_lo) / M_PER_DEG_LAT, self.center_lon + (x - BOUNDS_X / 2) / self.mlon

    def height(self, x, z):
        return self.crop.at(*self.to_geo(x, z))


def osm_class(tags):
    h = tags.get("highway", "")
    base = h[:-5] if h.endswith("_link") else h
    if base in ("motorway", "trunk", "primary", "secondary", "tertiary"):
        return "major"
    if base in ("residential", "unclassified", "living_street", "service", "road"):
        return "minor"
    if base == "pedestrian":
        return "alley"
    if base == "footway":
        return "footway"
    if base == "steps":
        return "steps"
    return "path"   # path, track, cycleway, bridleway, elevator, corridor ...


def latest(pattern):
    files = sorted(glob.glob(os.path.join(HERE, "osm", pattern)))
    files = [f for f in files if not f.endswith(".meta.json")]
    if not files:
        raise SystemExit("캐시 없음: " + pattern + " — fetch_sources.py 먼저")
    return files[-1]


def clip_ways(piece, osm_path, seg_id, segments, stats):
    doc = json.load(open(osm_path, encoding="utf-8"))
    for el in doc["elements"]:
        if el.get("type") != "way" or "geometry" not in el:
            continue
        cls = osm_class(el.get("tags", {}))
        stats[cls] = stats.get(cls, 0) + 1
        run = []
        for node in el["geometry"]:
            x, z = piece.to_local(node["lat"], node["lon"])
            inside = 0 <= x <= BOUNDS_X and piece.z_lo <= z <= piece.z_hi
            if inside:
                run.append([round(x, 2), round(z, 2)])
            elif run:
                if len(run) >= 2:
                    seg_id += 1; segments.append({"id": seg_id, "class": cls, "points_m": run, "osm_way": el["id"], "highway": el.get("tags", {}).get("highway", "")})
                run = []
        if len(run) >= 2:
            seg_id += 1; segments.append({"id": seg_id, "class": cls, "points_m": run, "osm_way": el["id"], "highway": el.get("tags", {}).get("highway", "")})
    return seg_id


def main():
    os.makedirs(OUT, exist_ok=True)
    amalfi_dem, positano_dem = DemCrop("amalfi_harbor"), DemCrop("positano_slope")
    a_coast = amalfi_dem.coast_lat(AMALFI_LON)
    p_coast = positano_dem.coast_lat(POSITANO_LON)
    amalfi = Piece(amalfi_dem, AMALFI_LON, a_coast - SEA_MARGIN / M_PER_DEG_LAT, 0.0, SEAM_Z)
    positano = Piece(positano_dem, POSITANO_LON + POS_X_OFFSET / m_per_deg_lon(p_coast), p_coast + POS_SKIP / M_PER_DEG_LAT, SEAM_Z, BOUNDS_Z)

    # 공통 고도맵
    nx, nz = int(BOUNDS_X / HSTEP) + 1, int(BOUNDS_Z / HSTEP) + 1
    heights = []
    for j in range(nz):
        z = j * HSTEP
        for i in range(nx):
            x = i * HSTEP
            ha, hp = amalfi.height(x, min(z, SEAM_Z + BLEND)), positano.height(x, max(z, SEAM_Z - BLEND))
            t = min(max((z - (SEAM_Z - BLEND)) / (2 * BLEND), 0.0), 1.0)
            t = t * t * (3 - 2 * t)
            heights.append(round(ha * (1 - t) + hp * t, 2))
    hdoc = {
        "bounds_m": [BOUNDS_X, BOUNDS_Z], "step_m": HSTEP, "nx": nx, "nz": nz, "rows_south_to_north": True,
        "pieces": {
            "amalfi": {"center_lon": AMALFI_LON, "south_lat": amalfi.south_lat, "coast_lat": a_coast, "z": [0, SEAM_Z]},
            "positano": {"center_lon": POSITANO_LON, "south_lat": positano.south_lat, "coast_lat": p_coast, "z": [SEAM_Z, BOUNDS_Z]},
        },
        "blend_z": [SEAM_Z - BLEND, SEAM_Z + BLEND],
        "attribution": "produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved",
        "heights_m": heights,
    }
    hdoc["variant"] = VARIANT
    json.dump(hdoc, open(os.path.join(OUT, f"heights_480x400{SUFFIX}.json"), "w", encoding="utf-8"))

    # 도로망
    segments, stats = [], {}
    sid = clip_ways(amalfi, latest("amalfi_harbor_*.json"), 0, segments, stats)
    clip_ways(positano, latest("positano_slope_*.json"), sid, segments, stats)
    net = {"source": "osm", "seed": 0, "bounds_m": [BOUNDS_X, BOUNDS_Z], "segments": segments, "landmarks_hint": [],
           "credit": "© OpenStreetMap contributors (ODbL)"}
    json.dump(net, open(os.path.join(OUT, f"osm_network{SUFFIX}.json"), "w", encoding="utf-8"), ensure_ascii=False)

    kept = {}
    for s in segments:
        kept[s["class"]] = kept.get(s["class"], 0) + 1
    hs = heights
    print(f"[heights] {nx}x{nz} (4m) min {min(hs):.1f} max {max(hs):.1f} m · 아말피 해안 {a_coast:.5f} · 포지타노 해안 {p_coast:.5f}")
    print(f"[osm] 원본 way 등급 {stats}")
    print(f"[osm] 땅 안 세그먼트 {len(segments)}: {kept}")


if __name__ == "__main__":
    main()
