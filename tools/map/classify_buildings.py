"""ENV E1: classify the existing V6b footprints, without opening Unity/Blender.

Run: py -X utf8 tools/map/classify_buildings.py
Check without rewriting: py -X utf8 tools/map/classify_buildings.py --check
Requires Python 3.10+, numpy, Shapely >=2, Pillow, PyYAML (already installed).
Coordinates and distances are horizontal Unity x,z metres; area is m².

Reference (principles, not copied code): Acid3croco/berat-racer at
ff79911c14930a3d099f61dec2254d638bb061b2, tools/facades.py (MIT).
Its buffered-boundary majority rule is replaced by symmetric, opposing edge
pairs and partial contact lengths; tolerance is 0.3m as requested, not 0.5m.
Classification thresholds/fallbacks below are explicit E1 assumptions, not
verified real-world building uses or approval for making game assets.
"""
from __future__ import annotations

import argparse
from collections import Counter
from hashlib import sha256
import json
import math
from pathlib import Path
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont, __version__ as pillow_version
import shapely
from shapely.geometry import LineString, Point, Polygon, box
from shapely.ops import unary_union
from shapely.strtree import STRtree
import yaml

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'docs/map/town-v6b.json'
BUILDER = ROOT / 'F1/Assets/Editor/BuildBayLoop.Gray.cs'
ROUTE = ROOT / 'docs/design/tutorial/first-delivery.yaml'
PLACEMENTS = ROOT / 'docs/design/tutorial/placements.yaml'
OUT = ROOT / 'docs/map/env-buildings.json'
PNG = ROOT / 'docs/map/env-buildings.png'
TYPES = {
    'town_house': ('마을집', '#BD9258'),
    'shop_house': ('상점주택', '#CE5976'),
    'terrace_house': ('비탈 층층집', '#499BB3'),
    'apartment': ('연립·아파트', '#8B6CB1'),
    'harbor_shed': ('항구 창고', '#D96C3C'),
    'annex': ('헛간·부속채', '#ADB3AB'),
    'detached': ('단독주택', '#57914A'),
}
EPS = 1e-8
CONTACT_GAP = .3
MIN_CONTACT = 1e-6
MAX_WALL_ANGLE = 15


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def ring_of(building):
    """Match Buildings(): remove closure, then reverse clockwise rings."""
    ring = np.asarray(building['poly'], dtype=np.float64)
    if len(ring) > 1 and np.linalg.norm(ring[0] - ring[-1]) < 1e-5:
        ring = ring[:-1]
    if len(ring) < 3:
        return ring
    signed = np.sum(ring[:, 0] * np.roll(ring[:, 1], -1)
                    - ring[:, 1] * np.roll(ring[:, 0], -1))
    return ring[::-1].copy() if signed < 0 else ring


def builder_center(ring):
    # Unity Vector2/F() use single precision and sequential accumulation.
    total = np.zeros(2, dtype=np.float32)
    for point in ring:
        total += point.astype(np.float32)
    return total / np.float32(len(ring))


def selected_buildings(plan):
    result = []
    for source_index, b in enumerate(plan['buildings']):
        # Center must be evaluated in source order, before winding reversal.
        raw = np.asarray(b['poly'], dtype=np.float64)
        if len(raw) > 1 and np.linalg.norm(raw[0] - raw[-1]) < 1e-5:
            raw = raw[:-1]
        if len(raw) < 3 or max(abs(builder_center(raw))) > 390:
            continue
        ring = ring_of(b)
        poly = Polygon(ring)
        if not poly.is_valid or poly.area <= 0:
            raise ValueError(f'Invalid source footprint: {source_index}')
        way = int(b.get('osm_way', len(result)))
        result.append(dict(id=f"{b['piece']}_{way}", osm_way=way,
                           source_index=source_index, zone=b['piece'],
                           floors=2 + ((way * 2654435761 & 0x7fffffff) % 3),
                           y=float(b['y']), center=builder_center(raw).tolist(),
                           ring=ring, poly=poly))
    return result


def contact(a, b):
    """Opposing CCW edges, near-parallel, with positive flat-cap overlap.

    Return lengths along each edge separately, so a 2m wall touching part of
    a 10m wall is recorded on BOTH, without declaring the whole 10m blind.
    Corner-only contact and crossing walls must not become party walls.
    """
    av, bv = np.diff(np.asarray(a.coords), axis=0)[0], np.diff(np.asarray(b.coords), axis=0)[0]
    if min(a.length, b.length) <= MIN_CONTACT:
        return None
    if np.dot(av, bv) / (a.length * b.length) > -math.cos(math.radians(MAX_WALL_ANGLE)):
        return None
    gap = a.distance(b)
    if gap > CONTACT_GAP + EPS:
        return None
    al = a.intersection(b.buffer(CONTACT_GAP + EPS, cap_style='flat')).length
    bl = b.intersection(a.buffer(CONTACT_GAP + EPS, cap_style='flat')).length
    if min(al, bl) <= MIN_CONTACT + EPS:
        return None
    return al, bl, gap


def shared_walls(buildings):
    edges, owners = [], []
    for bi, b in enumerate(buildings):
        ring = b['ring']
        for ei in range(len(ring)):
            edges.append(LineString([ring[ei], ring[(ei + 1) % len(ring)]]))
            owners.append((bi, ei))
    tree = STRtree(edges)
    out = [[] for _ in buildings]
    for i, a in enumerate(edges):
        bi, ei = owners[i]
        for j in sorted(tree.query(a, predicate='dwithin', distance=CONTACT_GAP + EPS)):
            bj, ej = owners[j]
            if j <= i or bi == bj:
                continue
            hit = contact(a, edges[j])
            if hit is None:
                continue
            al, bl, gap = hit
            for own, edge, other, other_edge, length, other_length, full in (
                (bi, ei, bj, ej, al, bl, a.length),
                (bj, ej, bi, ei, bl, al, edges[j].length),
            ):
                out[own].append(dict(edge=edge, neighbor_id=buildings[other]['id'],
                                     neighbor_edge=other_edge, contact_m=round(length, 6),
                                     neighbor_contact_m=round(other_length, 6),
                                     fraction=round(length / full, 6), gap_m=round(gap, 6)))
    for walls in out:
        walls.sort(key=lambda w: (w['edge'], w['neighbor_id'], w['neighbor_edge']))
    return out


def rectangle_info(poly):
    """Conservative near-rectangle test, not just vertex_count == 4."""
    rect = poly.minimum_rotated_rectangle
    fill = poly.area / rect.area
    simple = poly.simplify(.1, preserve_topology=True)
    points = np.asarray(simple.exterior.coords)[:-1]
    angles_ok = False
    if len(points) == 4 and simple.equals(simple.convex_hull):
        vectors = np.roll(points, -1, axis=0) - points
        lengths = np.linalg.norm(vectors, axis=1)
        cosines = np.sum(vectors * np.roll(vectors, -1, axis=0), axis=1) / (lengths * np.roll(lengths, -1))
        angles_ok = bool(np.all(np.abs(cosines) <= math.sin(math.radians(5))))
    deviation = poly.boundary.hausdorff_distance(rect.boundary)
    return bool(angles_ok and fill >= .97 and deviation <= .2), fill, deviation


def nearest_front(ring, walls, roads, surfaces):
    shared = {w['edge'] for w in walls}
    free = [i for i in range(len(ring)) if i not in shared]
    candidates = free or list(range(len(ring)))
    choices = []
    for ei in candidates:
        edge = LineString([ring[ei], ring[(ei + 1) % len(ring)]])
        if edge.length <= EPS:
            continue
        distances = shapely.distance(edge, surfaces)
        minimum = float(np.min(distances))
        # Exact edge distance first; midpoint resolves shared corner ties.
        for ri in np.flatnonzero(distances <= minimum + EPS):
            mid_distance = edge.interpolate(.5, normalized=True).distance(surfaces[ri])
            choices.append((round(minimum, 8), mid_distance, -edge.length, ei, int(ri)))
    distance, _, _, ei, ri = min(choices)
    return ei, ri, distance, not bool(free)


def classify(area, zone, y, floors, sparse, main_front, setback):
    """First match wins. Residual cases stay visible as fallbacks."""
    if area < 40:
        return 'annex', 'area_lt_40', False
    if (y < 4 and area >= 150) or (zone == 'tomo' and area >= 300):
        return 'harbor_shed', 'low_large_or_tomo_300', False
    if y > 40 and 60 <= area <= 200 and sparse:
        return 'detached', 'high_sparse_60_200', False
    if zone in ('tomo', 'camo') and area >= 150 and floors >= 4:
        return 'apartment', 'coast_harbor_large_4f', False
    if zone == 'tomo' and 40 <= area <= 200 and main_front and setback <= 6:
        return 'shop_house', 'tomo_main_front_6m', False
    if zone == 'posi':
        return 'terrace_house', 'remaining_posi', False
    if 40 <= area < 150 and floors in (2, 3) and setback <= 12:
        return 'town_house', 'medium_2_3f_roadside_12m', False
    # The draft is neither exclusive nor exhaustive; do not invent new types
    # or silently change current floors to make a building fit it.
    if area >= 150:
        return 'apartment', 'fallback_large_residential_under_4f', True
    return 'town_house', 'fallback_medium_4f_or_not_roadside', True


def load_context():
    plan = json.loads(SOURCE.read_text(encoding='utf-8'))
    tutorial = yaml.safe_load(ROUTE.read_text(encoding='utf-8'))
    route = next(q['route_world'] for q in tutorial['quests'] if q['id'] == 'first_delivery')
    places = yaml.safe_load(PLACEMENTS.read_text(encoding='utf-8'))
    landmarks = [dict(id=name, label=label, marker=chr(65 + i), **places['places'][name])
                 for i, (name, label) in enumerate((('house_home', '우리 집'), ('garage', '정비소'),
                                                   ('depot', '보관소'), ('postoffice', '우체국')))]
    return plan, route, landmarks, places['saved']


def build():
    plan, route, landmarks, saved = load_context()
    buildings = selected_buildings(plan)
    walls = shared_walls(buildings)
    polygons = [b['poly'] for b in buildings]
    tree = STRtree(polygons)
    roads = [r for r in plan['roads'] if r['kind'] in ('main', 'village') and len(r['knots']) >= 2]
    road_lines = [LineString([k[:2] for k in r['knots']]) for r in roads]
    surfaces = np.array([line.buffer(r['width'] / 2, cap_style='flat', join_style='mitre')
                         for r, line in zip(roads, road_lines)], dtype=object)
    main_indices = [i for i, r in enumerate(roads) if r['kind'] == 'main']
    route_line = LineString(route)
    main_lines = unary_union([road_lines[i] for i in main_indices])
    records = []
    for bi, b in enumerate(buildings):
        poly, ring, wall = b['poly'], b['ring'], walls[bi]
        neighbours = [int(i) for i in tree.query(poly, predicate='dwithin', distance=25) if i != bi]
        gaps = [poly.distance(polygons[i]) for i in neighbours]
        sparse = not wall and len(neighbours) <= 2 and min(gaps, default=26) >= 8
        front, ri, setback, all_shared = nearest_front(ring, wall, roads, surfaces)
        road = roads[ri]
        kind, reason, fallback = classify(poly.area, b['zone'], b['y'], b['floors'], sparse,
                                          road['kind'] == 'main' or road['width'] >= 6, setback)
        simple, fill, deviation = rectangle_info(poly)
        dr, dm = poly.distance(route_line), poly.distance(main_lines)
        recommended = ('flat' if kind in ('terrace_house', 'apartment') or not simple else
                       'shed' if kind == 'annex' else 'hip' if kind == 'detached' else 'gable')
        records.append(dict(
            id=b['id'], zone=b['zone'], type=kind, floors=b['floors'], area=round(poly.area, 6),
            front_edge=front, shared_walls=wall, simple_rect=simple,
            roof=dict(gable_possible=simple, recommended=recommended),
            dist_to_route=round(min(dr, dm), 6),
            osm_way=b['osm_way'], source_index=b['source_index'], y=b['y'],
            center=[round(v, 6) for v in b['center']], poly=ring.tolist(),
            classification_rule=reason, classification_fallback=fallback,
            front_road_id=road['id'], front_road_kind=road['kind'],
            front_setback_m=round(setback, 6), front_all_edges_shared=all_shared,
            dist_to_delivery_loop_m=round(dr, 6), dist_to_main_road_m=round(dm, 6),
            neighbors_within_25m=len(neighbours), sparse=sparse,
            rectangle_fill=round(fill, 6), rectangle_deviation_m=round(deviation, 6)))
    counts = {k: sum(b['type'] == k for b in records) for k in TYPES}
    pairs = sum(len(b['shared_walls']) for b in records) // 2
    overlap_pairs = []
    for i, p in enumerate(polygons):
        for j in tree.query(p, predicate='intersects'):
            if j > i and (area := p.intersection(polygons[j]).area) > .01:
                overlap_pairs.append(dict(a=records[i]['id'], b=records[j]['id'], area_m2=round(area, 6)))
    meta = dict(
        schema_version=1, units=dict(position='Unity x,z metres', area='m2', distance='horizontal metres'),
        sources={str(p.relative_to(ROOT)).replace('\\', '/'): digest(p)
                 for p in (SOURCE, BUILDER, ROUTE, PLACEMENTS)},
        dependencies=dict(python=sys.version.split()[0], numpy=np.__version__, shapely=shapely.__version__,
                          pillow=pillow_version, pyyaml=yaml.__version__),
        builder_filter='source ring vertex mean (float32), abs(x),abs(z)<=390; no removed-key filtering',
        floors='existing builder: 2 + ((osm_way * 2654435761 & 0x7fffffff) % 3); not proposed asset floors',
        edge_index='zero-based in output poly; closure removed, counter-clockwise like Buildings()',
        shared_walls='opposing edges within 15 degrees; gap<=0.3m; positive flat-cap buffered overlap>1e-6m on BOTH; partial lengths retained',
        shared_walls_scope='only the selected 1149 builder buildings, horizontal contact; not height-tested',
        front_edge='nearest free edge to main/village road surface; edge distance then midpoint distance; all-shared fallback flagged',
        route_distance='min(footprint to first_delivery.route_world, footprint to kind=main centerlines); 2D, no pathfinding',
        main_front='front road kind=main OR width>=6m; facade setback<=6m; plaza polygons absent',
        sparse='no shared edge; <=2 other footprints within 25m; none closer than 8m',
        simple_rect='simplify 0.1m => convex 4 corners, angles 85..95deg; MRR fill>=0.97, boundary deviation<=0.2m',
        roof='gable_possible is a geometry candidate only; no roof mesh built/validated; terrace/apartment prefer flat',
        type_priority=['annex', 'harbor_shed', 'detached', 'apartment_strict', 'shop_house',
                       'terrace_house', 'town_house_strict', 'apartment_fallback', 'town_house_fallback'],
        landmark_source_saved=saved,
        warnings=['Classification thresholds and residual fallbacks are E1 assumptions, not approved building uses.',
                  'Existing floors remain 2..4, including annexes/sheds. Model-design floors are a later decision.',
                  'Tutorial route is a saved draft and predates current landmark placements; neither is rewritten.',
                  'Source removed.osm_ways remains included to match the current builder selection, not the edited live scene.'])
    summary = dict(total=len(records), source_total=len(plan['buildings']), excluded=len(plan['buildings']) - len(records),
                   by_type=counts, by_zone=dict(Counter(b['zone'] for b in records)),
                   by_zone_type={z: {t: sum(b['zone'] == z and b['type'] == t for b in records) for t in TYPES}
                                 for z in ('tomo', 'posi', 'camo')},
                   by_floors=dict(sorted(Counter(b['floors'] for b in records).items())),
                   by_rule=dict(sorted(Counter(b['classification_rule'] for b in records).items())),
                   fallback_total=sum(b['classification_fallback'] for b in records), shared_edge_pairs=pairs,
                   shared_buildings=sum(bool(b['shared_walls']) for b in records),
                   simple_rect=sum(b['simple_rect'] for b in records),
                   source_overlap_pairs=overlap_pairs,
                   lod_distance_bands={label: sum(lo <= b['dist_to_route'] < hi for b in records)
                                       for label, lo, hi in [('0_60m', 0, 60), ('60_200m', 60, 200), ('200m_plus', 200, math.inf)]})
    return dict(meta=meta, summary=summary, landmarks=landmarks, buildings=records), plan, route


def validate(data, plan):
    records = data['buildings']
    expected = selected_buildings(plan)
    assert len(records) == len(expected) == 1149, 'Builder selection count changed'
    assert [b['id'] for b in records] == [b['id'] for b in expected], 'Builder coverage/order mismatch'
    ids = {b['id']: b for b in records}
    assert len(ids) == len(records)
    assert sum(data['summary']['by_type'].values()) == 1149
    count = Counter(b['type'] for b in records)
    assert dict(count) == {k: v for k, v in data['summary']['by_type'].items() if v}
    directed = set()
    for b, src in zip(records, expected):
        assert b['type'] in TYPES and b['floors'] == src['floors']
        assert abs(b['area'] - src['poly'].area) <= 1e-6
        assert 0 <= b['front_edge'] < len(b['poly'])
        assert math.isfinite(b['dist_to_route']) and b['dist_to_route'] >= 0
        assert b['roof']['gable_possible'] == b['simple_rect']
        for w in b['shared_walls']:
            neighbor = ids[w['neighbor_id']]
            assert b['id'] != neighbor['id'] and 0 <= w['edge'] < len(b['poly'])
            matches = [r for r in neighbor['shared_walls']
                       if r['neighbor_id'] == b['id'] and r['edge'] == w['neighbor_edge'] and r['neighbor_edge'] == w['edge']]
            assert len(matches) == 1, 'Asymmetric wall'
            assert matches[0]['contact_m'] == w['neighbor_contact_m']
            assert matches[0]['gap_m'] == w['gap_m'] <= CONTACT_GAP + 1e-6
            assert w['contact_m'] > MIN_CONTACT and 0 < w['fraction'] <= 1.000001
            directed.add((b['id'], w['edge'], neighbor['id'], w['neighbor_edge']))
    assert len(directed) == sum(len(b['shared_walls']) for b in records)
    assert len(directed) == 2 * data['summary']['shared_edge_pairs']
    return dict(builder_coverage=1149, type_sum=1149, unique_ids=1149,
                symmetric_edge_pairs=len(directed) // 2, invalid_fields=0)


def geometry_checks():
    """Boundary fixtures address actual risk: short contacts and corner ties."""
    a = LineString([(10, 0), (10, 10)])
    for gap in (0, .2, .3):
        b = LineString([(10 + gap, 10), (10 + gap, 0)])
        assert contact(a, b) is not None and contact(b, a) is not None
    assert contact(a, LineString([(10.31, 10), (10.31, 0)])) is None
    assert contact(a, LineString([(10, 20), (10, 10)])) is None
    assert contact(a, LineString([(10, 10), (20, 10)])) is None
    assert contact(a, LineString([(10, 0), (10, 10)])) is None
    assert contact(a, LineString([(10, 2), (10, 0)]))[:2] == (2., 2.)
    assert contact(a, LineString([(10, .2), (10, 0)]))[:2] == (.2, .2)
    assert rectangle_info(box(0, 0, 10, 6))[0]
    assert not rectangle_info(Polygon([(0, 0), (10, 0), (10, 4), (4, 4), (4, 10), (0, 10)]))[0]
    assert not rectangle_info(Polygon([(0, 0), (10, 0), (8, 6), (2, 6)]))[0]
    # Draft precedence and threshold boundaries (areas are unrounded).
    assert classify(39.99, 'posi', 50, 4, True, True, 0)[0] == 'annex'
    assert classify(150, 'tomo', 3.9, 4, True, True, 0)[0] == 'harbor_shed'
    assert classify(100, 'posi', 41, 3, True, False, 9)[0] == 'detached'
    assert classify(150, 'camo', 10, 4, False, False, 9)[0] == 'apartment'
    assert classify(40, 'tomo', 10, 3, False, True, 6)[0] == 'shop_house'
    assert classify(100, 'posi', 10, 3, False, True, 0)[0] == 'terrace_house'
    assert classify(100, 'tomo', 10, 3, False, False, 12)[0] == 'town_house'
    return 'PASS (contact boundaries/corners/partial symmetry, roof shapes, type precedence)'


def render(data, plan, route):
    """Diagnostic categorical map, not an art/concept image."""
    image = Image.new('RGB', (2400, 1900), '#F6F5F0')
    draw = ImageDraw.Draw(image)
    font_path = Path('C:/Windows/Fonts/malgun.ttf')
    if not font_path.exists():
        raise FileNotFoundError('Korean map labels require C:/Windows/Fonts/malgun.ttf')
    fonts = {s: ImageFont.truetype(str(font_path), s) for s in (20, 23, 25, 28, 34, 44)}
    def text(xy, s, size=23, color='#303B42', **kwargs):
        draw.text(xy, s, font=fonts[size], fill=color, **kwargs)
    text((55, 24), 'ENV E1  |  배경 건물 1,149채 분류', 44)
    text((57, 87), 'town-v6b · 빌더 중심 ±390m · 유형은 규칙 기반 가설 · 게임 에셋 변경 없음', 25)
    all_pts = np.concatenate([np.asarray(b['poly']) for b in data['buildings']])
    extent = max(400, math.ceil(float(abs(all_pts).max()) / 50) * 50)
    x0, y0, size = 80, 170, 1640
    scale = size / (2 * extent)
    def p(q):
        return (x0 + (q[0] + extent) * scale, y0 + (extent - q[1]) * scale)
    draw.rectangle((x0, y0, x0 + size, y0 + size), fill='#E9EDE5', outline='#7E8A89', width=2)
    for v in range(-extent, extent + 1, 100):
        xx, zz = p((v, v))
        draw.line([(xx, y0), (xx, y0 + size)], fill='#D4DCD4')
        draw.line([(x0, zz), (x0 + size, zz)], fill='#D4DCD4')
        text((xx, y0 + size + 8), str(v), 20, anchor='mt')
        text((x0 - 12, zz), str(v), 20, anchor='rm')
    map_background = image.copy()
    for coast in plan['sea']['coast']:
        if len(coast) > 1:
            draw.line([p(q[:2]) for q in coast], fill='#74AAB6', width=4)
    for road in plan['roads']:
        pts = [p(k[:2]) for k in road['knots']]
        if len(pts) < 2:
            continue
        car = road['kind'] in ('main', 'village')
        draw.line(pts, fill='#FFFFFF' if car else '#CDD2CA',
                  width=max(1, round(road['width'] * scale)) if car else 1, joint='curve')
    for b in data['buildings']:
        draw.polygon([p(q) for q in b['poly']], fill=TYPES[b['type']][1], outline='#63716C')
    main_line = [p(q) for q in route]
    draw.line(main_line, fill='#174B77', width=3, joint='curve')
    draw.rectangle((*p((-390, 390)), *p((390, -390))), outline='#4C5C62', width=2)
    # The full source road/coast network extends far beyond selected buildings.
    # Clip the drawing, not the source, so it cannot cover title/legend/axes.
    crop_box = (x0, y0, x0 + size + 1, y0 + size + 1)
    map_background.paste(image.crop(crop_box), (x0, y0))
    image = map_background
    draw = ImageDraw.Draw(image)
    text((x0 + 20, y0 + 15), '+Z ↑', 28)
    text((x0 + size - 90, y0 + size - 45), '+X →', 25)
    bar_start = p((-extent + 30, -extent + 38))
    draw.line([bar_start, (bar_start[0] + 100 * scale, bar_start[1])], fill='#283C42', width=7)
    text((bar_start[0], bar_start[1] - 35), '100 m', 23)
    for landmark in data['landmarks']:
        xx, yy = p(landmark['world'])
        draw.ellipse((xx - 13, yy - 13, xx + 13, yy + 13), fill='#202D37', outline='white', width=2)
        text((xx, yy - 1), landmark['marker'], 20, 'white', anchor='mm')
    lx = 1780
    text((lx, 170), '유형별 실제 집계', 34)
    for i, (kind, (label, color)) in enumerate(TYPES.items()):
        y = 235 + i * 76
        draw.rectangle((lx, y + 3, lx + 31, y + 34), fill=color)
        text((lx + 45, y), f'{label}   {data["summary"]["by_type"][kind]}채', 28)
        text((lx + 45, y + 36), kind, 20, '#657279')
    text((lx, 790), 'tomo 613 / posi 400 / camo 136', 23)
    draw.line([(lx, 850), (lx + 45, 850)], fill='#174B77', width=4)
    text((lx + 60, 831), '저장된 배달 고리 (초안)', 23)
    draw.line([(lx, 895), (lx + 45, 895)], fill='#74AAB6', width=4)
    text((lx + 60, 876), '원본 해안선', 23)
    text((lx, 935), '핵심 4채: placements.yaml 기준', 25)
    text((lx, 977), data['meta']['landmark_source_saved'], 20)
    # Enlarged locator for close-by depot/postoffice markers.
    landmarks = data['landmarks']
    xs, zs = zip(*(b['world'] for b in landmarks))
    lowx, highx, lowz, highz = min(xs) - 15, max(xs) + 15, min(zs) - 25, max(zs) + 25
    inset = (lx, 1030, 2350, 1380)
    draw.rectangle(inset, fill='#E9EDE5', outline='#83928A', width=2)
    factor = min((inset[2] - inset[0] - 20) / (highx - lowx), (inset[3] - inset[1] - 20) / (highz - lowz))
    def ip(q):
        return (inset[0] + 10 + (q[0] - lowx) * factor, inset[3] - 10 - (q[1] - lowz) * factor)
    clip = box(lowx, lowz, highx, highz)
    for b in data['buildings']:
        shape = Polygon(b['poly']).intersection(clip)
        for part in getattr(shape, 'geoms', [shape]):
            if part.geom_type == 'Polygon' and not part.is_empty:
                draw.polygon([ip(q) for q in part.exterior.coords], fill=TYPES[b['type']][1])
    for lm in landmarks:
        xx, yy = ip(lm['world'])
        draw.ellipse((xx - 14, yy - 14, xx + 14, yy + 14), fill='#202D37')
        text((xx, yy), lm['marker'], 23, 'white', anchor='mm')
    for i, lm in enumerate(landmarks):
        text((lx, 1410 + i * 46), f"{lm['marker']}  {lm['label']}  ({lm['world'][0]:g}, {lm['world'][1]:g})", 23)
    text((lx, 1615), '주: 고리는 현재 핵심 건물 위치보다 이전 초안.', 20)
    text((lx, 1650), '건물 4채는 위치 표식이며 1,149채 합계와 별개.', 20)
    text((lx, 1685), f"초안 조건 밖 보완 분류: {data['summary']['fallback_total']}채", 20)
    text((lx, 1720), '맞벽·층수·판정 근거: env-buildings.json', 20)
    text((80, 1855), '평면 거리(m) / 면적(m²) · 진단용 유형색이며 게임 팔레트 아님 · 자료: OSM 기반 town-v6b + 기획 배치 파일', 23)
    image.save(PNG)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='recompute and compare JSON without writing outputs')
    args = parser.parse_args()
    fixture_result = geometry_checks()
    data, plan, route = build()
    data['validation'] = validate(data, plan)
    data['validation']['geometry_fixtures'] = fixture_result
    serialized = json.dumps(data, ensure_ascii=False, indent=2, allow_nan=False) + '\n'
    if args.check:
        assert OUT.read_text(encoding='utf-8') == serialized, 'Saved JSON differs from deterministic rebuild'
        with Image.open(PNG) as img:
            img.verify()
        print('CHECK PASS: saved JSON equals recomputation; PNG decodes')
    else:
        OUT.write_text(serialized, encoding='utf-8', newline='\n')
        render(data, plan, route)
    print(json.dumps(dict(summary=data['summary'], validation=data['validation']), ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
