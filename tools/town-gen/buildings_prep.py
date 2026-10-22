# -*- coding: utf-8 -*-
"""구조 조각 10(D-114) — OSM 실제 건물 윤곽을 Unity 가 읽을 로컬 좌표 JSON 으로 + 분포 측정.

입력: osm/structure_a_buildings_*.json (Overpass way/relation[building], ODbL)
출력: out/structure_a/buildings.json — 건물마다 바깥 고리(로컬 m, 반시계, 닫는 점 없음)·면적·층수(태그 있으면)·구역
      out/structure_a/buildings_stats.json — 개수·면적 분포·구역별 개수·층수 태그 비율
좌표: structure_prep.py 와 같음 (x 동쪽, z 북쪽, 원점 = 상자 남서 모서리)
구역(측정용 근사, 직선거리): 중심 = 아말피 항구점 150m 안 · 가장자리 = 150~350m · 콘카 = 콘카 씨앗 100m 안 · 해안 = 나머지
  (중심 반경 150m·가장자리 350m 는 조각 5 밀도 지도의 도로 거리 기준을 직선거리로 근사한 것)
relation(multipolygon) 은 outer 토막을 끝점으로 이어 고리를 만든다. inner(안뜰 구멍)는 버린다 — 바닥 압출 상자에선 안 보임.

사용: py -X utf8 tools/town-gen/buildings_prep.py
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
HARBOR = ((14.6025 - W) * MLON, (40.6335 - S) * M_PER_DEG_LAT)      # TownGenStructure.harborGeo 와 같은 점
CONCA = ((14.5725004 - W) * MLON, (40.6176092 - S) * M_PER_DEG_LAT)  # TownGenStructure.concaGeo


def local(nd):
    return ((nd["lon"] - W) * MLON, (nd["lat"] - S) * M_PER_DEG_LAT)


def area(ring):
    return 0.5 * sum(ring[i][0] * ring[(i + 1) % len(ring)][1] - ring[(i + 1) % len(ring)][0] * ring[i][1] for i in range(len(ring)))


def clean(ring):
    if len(ring) >= 2 and math.dist(ring[0], ring[-1]) < 1e-6:
        ring = ring[:-1]
    out = []
    for p in ring:
        if not out or math.dist(out[-1], p) > 0.05:
            out.append(p)
    if len(out) >= 2 and math.dist(out[0], out[-1]) <= 0.05:
        out.pop()
    if len(out) < 3:
        return None
    if area(out) < 0:
        out.reverse()
    return [[round(x, 2), round(z, 2)] for x, z in out]


def join_outers(members):
    """outer 토막(점 목록)을 끝점이 같은 것끼리 이어 닫힌 고리들로."""
    segs = [[local(g) for g in m["geometry"]] for m in members if m.get("type") == "way" and m.get("role") in ("outer", "") and m.get("geometry")]
    rings = []
    while segs:
        cur = segs.pop(0)
        changed = True
        while math.dist(cur[0], cur[-1]) > 0.05 and changed:
            changed = False
            for k, sg in enumerate(segs):
                if math.dist(cur[-1], sg[0]) <= 0.05:
                    cur += sg[1:]
                elif math.dist(cur[-1], sg[-1]) <= 0.05:
                    cur += sg[::-1][1:]
                elif math.dist(cur[0], sg[-1]) <= 0.05:
                    cur = sg[:-1] + cur
                elif math.dist(cur[0], sg[0]) <= 0.05:
                    cur = sg[::-1][:-1] + cur
                else:
                    continue
                segs.pop(k); changed = True
                break
        if math.dist(cur[0], cur[-1]) <= 0.05:
            rings.append(cur)
    return rings


def levels(tags):
    for k in ("building:levels", "levels"):
        v = tags.get(k)
        if v:
            try:
                return float(str(v).replace(",", ".").split(";")[0])
            except ValueError:
                pass
    return None


def height(tags):
    v = tags.get("height")
    if v:
        try:
            return float(str(v).replace("m", "").replace(",", ".").strip())
        except ValueError:
            pass
    return None


def zone(c):
    if math.dist(c, CONCA) <= 100:
        return "conca"
    d = math.dist(c, HARBOR)
    return "centre" if d <= 150 else "edge" if d <= 350 else "coast"


def main():
    src = sorted(f for f in glob.glob(os.path.join(HERE, "osm", f"{NAME}_buildings_2*.json")) if not f.endswith(".meta.json"))[-1]
    doc = json.load(open(src, encoding="utf-8"))
    Wm, Hm = (E - W) * MLON, (N - S) * M_PER_DEG_LAT
    out, skipped = [], {"open_ring": 0, "degenerate": 0, "outside_box": 0, "building_no": 0}
    for el in doc["elements"]:
        tags = el.get("tags", {})
        if tags.get("building") == "no":
            skipped["building_no"] += 1; continue
        if el["type"] == "way":
            rings = [[local(g) for g in el.get("geometry", [])]] if el.get("geometry") else []
            if rings and math.dist(rings[0][0], rings[0][-1]) > 0.05:
                skipped["open_ring"] += 1; continue
        elif el["type"] == "relation":
            rings = join_outers(el.get("members", []))
            if not rings:
                skipped["open_ring"] += 1; continue
        else:
            continue
        for k, rg in enumerate(rings):
            r = clean(rg)
            if r is None:
                skipped["degenerate"] += 1; continue
            cx, cz = sum(p[0] for p in r) / len(r), sum(p[1] for p in r) / len(r)
            if not (0 <= cx <= Wm and 0 <= cz <= Hm):
                skipped["outside_box"] += 1; continue
            out.append({"id": len(out) + 1, "osm": f"{el['type']}/{el['id']}" + (f"#{k}" if len(rings) > 1 else ""), "building": tags.get("building", "yes"),
                        "levels": levels(tags), "height": height(tags), "name": tags.get("name", ""), "area_m2": round(area(r), 1),
                        "centroid": [round(cx, 2), round(cz, 2)], "zone": zone((cx, cz)), "ring": r})
    os.makedirs(OUT, exist_ok=True)
    json.dump({"box_swne": [S, W, N, E], "source": os.path.basename(src), "license": "© OpenStreetMap contributors, ODbL",
               "frame": "local m, x east, z north, origin = box SW corner; rings CCW, not closed",
               "zones": {"centre": "≤150m from harbour point", "edge": "150~350m", "conca": "≤100m from Conca seed", "coast": "rest"},
               "buildings": out}, open(os.path.join(OUT, "buildings.json"), "w", encoding="utf-8"), ensure_ascii=False)

    areas = sorted(b["area_m2"] for b in out)
    def pct(p): return areas[min(len(areas) - 1, int(p * len(areas)))] if areas else 0
    bins = [0, 20, 50, 100, 200, 400, 800, 1e9]
    hist = {f"{bins[i]:.0f}-{bins[i + 1]:.0f}" if bins[i + 1] < 1e9 else f"{bins[i]:.0f}+": sum(1 for a in areas if bins[i] <= a < bins[i + 1]) for i in range(len(bins) - 1)}
    zones = {}
    for z in ("centre", "edge", "coast", "conca"):
        zb = [b for b in out if b["zone"] == z]
        za = sorted(b["area_m2"] for b in zb)
        zones[z] = {"count": len(zb), "area_sum_m2": round(sum(za)), "area_median_m2": za[len(za) // 2] if za else 0,
                    "with_levels": sum(1 for b in zb if b["levels"] is not None)}
    types = {}
    for b in out:
        types[b["building"]] = types.get(b["building"], 0) + 1
    stats = {"source": os.path.basename(src), "count": len(out), "skipped": skipped,
             "from_relations": sum(1 for b in out if b["osm"].startswith("relation")),
             "area_m2": {"min": areas[0] if areas else 0, "p10": pct(.1), "median": pct(.5), "p90": pct(.9), "max": areas[-1] if areas else 0, "sum": round(sum(areas))},
             "area_hist_m2": hist, "zones": zones, "with_levels": sum(1 for b in out if b["levels"] is not None),
             "with_height": sum(1 for b in out if b["height"] is not None),
             "levels_values": sorted({b["levels"] for b in out if b["levels"] is not None}),
             "building_types_top": dict(sorted(types.items(), key=lambda kv: -kv[1])[:12])}
    json.dump(stats, open(os.path.join(OUT, "buildings_stats.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(json.dumps(stats, ensure_ascii=False, indent=1))


if __name__ == "__main__":
    main()
