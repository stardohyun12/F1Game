# -*- coding: utf-8 -*-
"""구조 조각 14 — 아말피 중심 마을 길을 한 장 면으로 (길 buffer 합집합 − 건물).

Unity(TownGenRoadSheet.cs)가 빌드 중에 부른다. 입력은 그 빌드의 길·건물(조각 13 틈 폭이 이미 적용된 가운데 선·반폭).

  U     = ∪ 좁은 차로 buffer(점마다 반폭) ∪ 보행길 buffer ∪ 광장 고리 ∪ 계단 디딤
  U     = close(U, r)            — 교차로 안쪽 모서리 둥글리기(buffer +r → −r, 가설 r = 1.5m)
  sheet = U − 건물 합집합 − 큰길 단면(보도 끝까지) − 계단 디딤 − 바다 위 토막, 반경 원으로 자름
  lane  = sheet ∩ 좁은 차로 buffer (아스팔트) · walk = sheet − lane (포장)

삼각분할: 2m 지형 격자 삼각형(Unity 지형 메시와 같은 대각선)으로 잘라 조각마다 constrained Delaunay
  → 보행 면 꼭짓점이 지형 삼각형 안에 놓여 높이 = 지형 + 0.06 이 정확(뜨거나 묻히지 않음).

측정: 연결 성분 수(v13 그린 면 vs v14 면), 겹쳐 그린 면적·쌍(z-fighting 후보), 보행 교차점 목록(평탄화용).

사용: py -X utf8 tools/town-gen/road_sheet.py <in.json> <out.json>
"""
import json
import math
import sys
import time

import numpy as np
import shapely
from shapely.geometry import LineString, MultiPolygon, Point, Polygon, box
from shapely.strtree import STRtree

QS = 4  # 원 근사 사분원당 선분 수


def var_buffer(pts, half):
    """점마다 반폭이 다른 선 buffer = 이웃 두 원의 볼록 껍질 합."""
    parts = []
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        ha, hb = max(half[i], 0.05), max(half[i + 1], 0.05)
        parts.append(shapely.convex_hull(shapely.union(Point(a).buffer(ha, quad_segs=QS), Point(b).buffer(hb, quad_segs=QS))))
    if len(pts) == 1:
        parts.append(Point(pts[0]).buffer(max(half[0], 0.05), quad_segs=QS))
    return parts


def polys(g):
    if g.is_empty:
        return []
    if isinstance(g, Polygon):
        return [g]
    if isinstance(g, MultiPolygon):
        return list(g.geoms)
    return [p for p in getattr(g, "geoms", []) if isinstance(p, Polygon)]


def components(g, gap=0.05):
    return len(polys(shapely.union_all([g]).buffer(gap, quad_segs=2)))


def triangulate(part, step):
    """part 를 2m 격자 삼각형(지형 메시 대각선: (i,j)-(i,j+1)-(i+1,j+1), (i,j)-(i+1,j+1)-(i+1,j))으로 잘라 CDT."""
    out = []
    if part.is_empty:
        return out
    T = 16.0
    x0, z0, x1, z1 = part.bounds
    for tx in np.arange(math.floor(x0 / T) * T, x1, T):
        for tz in np.arange(math.floor(z0 / T) * T, z1, T):
            piece = shapely.intersection(part, box(tx, tz, tx + T, tz + T))
            if piece.is_empty or piece.area < 1e-6:
                continue
            px0, pz0, px1, pz1 = piece.bounds
            cells = []
            for i in range(int(math.floor(px0 / step)), int(math.ceil(px1 / step))):
                for j in range(int(math.floor(pz0 / step)), int(math.ceil(pz1 / step))):
                    a, b = i * step, j * step
                    c, d = a + step, b + step
                    cells.append(Polygon([(a, b), (a, d), (c, d)]))
                    cells.append(Polygon([(a, b), (c, d), (c, b)]))
            if not cells:
                continue
            shapely.prepare(piece)
            arr = np.array(cells, dtype=object)
            hit = shapely.intersects(piece, arr)
            cut = shapely.intersection(arr[hit], piece)
            for g in cut:
                for p in polys(g):
                    if p.area < 1e-5:
                        continue
                    tri = shapely.constrained_delaunay_triangles(p)
                    for t in getattr(tri, "geoms", []):
                        if t.area < 1e-7:
                            continue
                        c = list(t.exterior.coords)[:3]
                        out.append([round(c[0][0], 3), round(c[0][1], 3), round(c[1][0], 3), round(c[1][1], 3), round(c[2][0], 3), round(c[2][1], 3)])
    return out


def overlap_stats(items, extra=None):
    """items = [(way, poly)]. 다른 길 사이 겹침 쌍(면적 > 0.01㎡)과 겹쳐 그린 면적(Σ면적 − 합집합 면적)."""
    geoms = [p for _, p in items]
    tree = STRtree(geoms)
    pairs, pair_area = 0, 0.0
    by_kind = {}
    for i, (w, p) in enumerate(items):
        for j in tree.query(p):
            if j <= i or items[j][0] == w:
                continue
            a = shapely.intersection(p, geoms[j]).area
            if a > 0.01:
                pairs += 1
                pair_area += a
                k = "+".join(sorted([w.split(":")[0], items[j][0].split(":")[0]]))
                by_kind[k] = by_kind.get(k, 0) + 1
    tot = sum(g.area for g in geoms)
    uni = shapely.union_all(geoms).area if geoms else 0.0
    r = {"pairs_between_ways": pairs, "pair_overlap_m2": round(pair_area, 2), "drawn_m2": round(tot, 1), "union_m2": round(uni, 1),
         "double_drawn_m2": round(tot - uni, 2), "pairsByKind": dict(sorted(by_kind.items(), key=lambda kv: -kv[1])[:12])}
    if extra is not None:
        r["over_main_road_m2"] = round(shapely.intersection(shapely.union_all(geoms), extra).area, 2) if geoms else 0
    return r


def junctions(paths):
    """보행 선 교차점: 끝점마다 그 자리(0.75m 안)를 지나는 다른 선은 2, 끝나는 선은 1 로 센 차수 ≥ 3."""
    lines = [(LineString(w["pts"]), w) for w in paths if len(w["pts"]) >= 2 and w["highway"] != "steps"]
    if not lines:
        return []
    tree = STRtree([l for l, _ in lines])
    found = []
    for li, (l, w) in enumerate(lines):
        for end in (Point(w["pts"][0]), Point(w["pts"][-1])):
            deg, hmax = 1, max(w["half"][0], w["half"][-1])
            for j in tree.query(end.buffer(0.75)):
                if j == li:
                    continue
                o, ow = lines[j]
                if o.distance(end) > 0.75:
                    continue
                ends = min(Point(ow["pts"][0]).distance(end), Point(ow["pts"][-1]).distance(end))
                deg += 1 if ends <= 0.75 else 2
                hmax = max(hmax, max(ow["half"]))
            if deg >= 3 and all(math.hypot(end.x - f[0], end.y - f[1]) > 2.0 for f in found):
                found.append([round(end.x, 2), round(end.y, 2), round(min(hmax, 4.0) + 1.0, 2), deg])
    return found


def main(src, dst):
    t0 = time.time()
    d = json.load(open(src, encoding="utf-8"))
    cx, cz = d["center"]
    R, r_fillet, step = d["radius"], d["fillet"], d["terrainStep"]
    circle = Point(cx, cz).buffer(R, quad_segs=32)

    lane_parts, walk_parts = [], []
    for w in d["lanes"]:
        lane_parts += var_buffer(w["pts"], w["half"])
    sea_skipped = 0.0
    for w in d["paths"]:
        if w["highway"] == "steps":
            continue  # 계단은 디딤(treads)으로 넣는다
        pts, half, sea = w["pts"], w["half"], w["sea"]
        run = []
        for i in range(len(pts)):
            if sea[i] and (i == 0 or sea[i - 1]):
                if len(run) >= 1:
                    walk_parts += var_buffer([pts[k] for k in run], [half[k] for k in run])
                if i > 0:
                    sea_skipped += math.dist(pts[i - 1], pts[i])
                run = []
                continue
            run.append(i)
        if run:
            walk_parts += var_buffer([pts[k] for k in run], [half[k] for k in run])
    plazas = [Polygon(a["ring"]).buffer(0) for a in d["plazas"]]
    treads = shapely.union_all([Polygon(p["ring"]) for p in d["treads"]]) if d["treads"] else Polygon()
    bld = shapely.union_all([Polygon(r).buffer(0) for r in d["buildings"]])
    bld_cut = bld.buffer(d.get("buildingClear", 0.01), quad_segs=2)  # 좌표 mm 반올림이 벽 안으로 밀지 않게 1cm 띄움
    main_parts = []
    for m in d["mains"]:
        main_parts.append(LineString(m["pts"]).buffer(m["half"], quad_segs=QS, cap_style="flat"))
    for jr in d["junctionRings"]:
        main_parts.append(Polygon(jr).buffer(d["junctionGrow"], quad_segs=QS))
    main_foot = shapely.union_all(main_parts) if main_parts else Polygon()
    t1 = time.time()

    lane_u = shapely.union_all(lane_parts) if lane_parts else Polygon()
    U = shapely.union_all([lane_u, shapely.union_all(walk_parts), shapely.union_all(plazas), treads.buffer(0.02)])
    U_closed = U.buffer(r_fillet, quad_segs=QS).buffer(-r_fillet, quad_segs=QS)
    fillet_added = U_closed.difference(U).area
    cut = shapely.union_all([bld_cut, main_foot, treads])
    sheet = U_closed.difference(cut).intersection(circle)
    sheet = shapely.union_all([p for p in polys(sheet) if p.area >= 0.2])
    lane = sheet.intersection(lane_u)
    walk = sheet.difference(lane_u)
    t2 = time.time()

    lane_tris = triangulate(lane, step)
    walk_tris = triangulate(walk, step)
    t3 = time.time()

    # 측정 — 연결 성분: 그린 면(보행·광장·계단·좁은 차로)을 0.05m 붙여 몇 덩어리인가
    v13_items = [(s["way"], Polygon(s["poly"]).buffer(0)) for s in d["v13"]]
    v13_items = [(w, p) for w, p in v13_items if not p.is_empty and p.intersects(circle)]
    v13_geom = shapely.union_all([p for _, p in v13_items])
    v14_geom = shapely.union_all([sheet, treads.intersection(circle)])
    tri_area = sum(abs((t[2] - t[0]) * (t[5] - t[1]) - (t[4] - t[0]) * (t[3] - t[1])) / 2 for t in lane_tris + walk_tris)
    tread_items = [(p["way"], Polygon(p["ring"])) for p in d["treads"]]
    res = {
        "rule": "U = union(narrow-lane var-width buffers, walking-path var-width buffers (piece-13 gap widths), plaza rings, step treads); "
                "U = close(U, fillet) (buffer +r then -r: rounds inner junction corners); sheet = U - buildings - main road section (to sidewalk outer edge) - step treads, "
                "clipped to radius circle, pieces < 0.2 m2 dropped; lane = sheet ∩ lane buffers (asphalt), walk = rest (paving). Triangulated per 2m terrain triangle (same diagonal as terrain mesh) with constrained Delaunay.",
        "radius_m": R, "fillet_m": r_fillet,
        "area_m2": {"sheet": round(sheet.area, 1), "lane": round(lane.area, 1), "walk": round(walk.area, 1), "filletAdded": round(fillet_added, 1),
                    "cutByBuildings": round(U_closed.intersection(bld).area, 1), "cutByMainRoad": round(U_closed.intersection(main_foot).area, 1), "triangleSum": round(tri_area, 1)},
        "seaSkipped_m": round(sea_skipped, 1),
        "components": {"rule": "drawn town surfaces (walk ribbons, plaza cells, step treads, narrow-lane stone) within radius, grown 0.05m and counted as separate pieces; tunnels / main roads not counted as links",
                       "v13": components(v13_geom), "v14": components(v14_geom),
                       "v13_withMainRoads": components(shapely.union_all([v13_geom, main_foot.intersection(circle)])),
                       "v14_withMainRoads": components(shapely.union_all([v14_geom, main_foot.intersection(circle)]))},
        "overlap": {"rule": "pairs of drawn surface polygons from different ways overlapping > 0.01 m2 (z-fighting candidates); double_drawn = sum of areas - union area; over_main_road = drawn town surface on top of the big-road section",
                    "v13": overlap_stats(v13_items, main_foot),
                    "v14": {"sheet_vs_steps_m2": round(sheet.intersection(treads).area, 3), "sheet_vs_main_road_m2": round(sheet.intersection(main_foot).area, 3),
                            "sheet_vs_buildings_m2": round(sheet.intersection(bld).area, 3), "triangle_double_drawn_m2": round(tri_area - sheet.area, 2),
                            "steps_between_ways_unchanged_from_v13": overlap_stats(tread_items)["pairs_between_ways"]}},
        "pieces": {"sheet": len(polys(sheet)), "holes": sum(len(p.interiors) for p in polys(sheet))},
        "junctions": junctions(d["paths"]),
        "triangles": {"lane": len(lane_tris), "walk": len(walk_tris)},
        "seconds": {"load": round(t1 - t0, 2), "geometry": round(t2 - t1, 2), "triangulate": round(t3 - t2, 2), "total": round(time.time() - t0, 2)},
        "shapely": shapely.__version__,
        "lane_tris": lane_tris, "walk_tris": walk_tris,
    }
    json.dump(res, open(dst, "w", encoding="utf-8"), separators=(",", ":"))
    print(json.dumps({k: v for k, v in res.items() if not k.endswith("_tris")}, ensure_ascii=False))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
