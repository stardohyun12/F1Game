"""주행 기준 검사 (과제 0506) — json 하나를 빌더 눈으로 잰다.

사용: python -X utf8 tools/map/drive_check.py v6e v6f      → docs/map/drive-check-v6e-v6f.md
  ① 길↔건물: 차길 띠(중심선 ± 폭/2 + 어깨 1 m)가 건물 다각형과 닿는 건물 수 · 닿는 길 길이(1 m 표본)
  ② 갈림목 옆면 턱: 차길 A 의 가로 단면(폭 전체, 0.5 m 간격)이 다른 차길 B 의 노면 안에 들어간 점에서 |A 높이 − B 높이|.
     (빌더 띠는 가로로 수평 — 두 띠가 같은 자리를 다른 높이로 지나가면 낮은 띠 위로 높은 띠의 옆면이 턱이 된다.)
     json 에 junctions[].poly 가 있으면 그 판 안의 점은 판 높이로 잰다(빌더가 판을 깐다).
  ③ 최대 경사(2 m 이상 떨어진 두 표본): 한도 12 %, 헤어핀(R < 20 m) 안쪽 15 %, 돌길(요철 ≥ 0.4 · sc_) 15 %.
  ④ 1 m 경사 변화(road_check.profile 그대로).
"""
import json, math, os, sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(__file__))
import planlib as L
import road_check as RC

OUT = RC.OUT
SHOULDER = 1.0
G_CAR, G_PIN, G_STONE, PIN_R = 0.12, 0.15, 0.15, 20.0
LEDGE = 0.02


def is_stone(r):
    return r['roughness'] >= RC.STONE or r['id'].startswith('sc_')


def caps(r):
    """표본마다 경사 한도: 돌길 15 %, 헤어핀(1 m 표본 ±12 m 세 점 반경 < 20 m) 15 %, 그 밖 12 %."""
    q = r['pts']; n = len(q)
    out = []
    for i in range(n):
        if is_stone(r): out.append(G_STONE); continue
        a, b = max(0, i - 12), min(n - 1, i + 12)
        pin = b - a >= 12 and L.radius3(q[a][:2], q[i][:2], q[b][:2]) < PIN_R
        out.append(G_PIN if pin else G_CAR)
    return out


def grades(rs):
    """[(경사, 한도, 길, x, z)] — 2 m 이상 떨어진 두 표본(앞 표본 기준)."""
    res = []
    for r in rs:
        q = r['pts']; cp = caps(r); j = 0
        for i in range(len(q)):
            j = max(j, i)
            while j < len(q) - 1 and math.hypot(q[j][0] - q[i][0], q[j][1] - q[i][1]) < 2: j += 1
            d = math.hypot(q[j][0] - q[i][0], q[j][1] - q[i][1])
            if d >= 2: res.append((abs(q[j][2] - q[i][2]) / d, max(cp[i], cp[j]), r['id'], q[i][0], q[i][1]))
    return res


def places(items, gap=3.0):
    """(x, z, ...) 목록을 3 m 안에서 이어 1곳으로 — [(대표 항목, 개수)]."""
    out = []
    for it in sorted(items, key=lambda t: -t[0]):
        for o in out:
            if math.hypot(o[0][3] - it[3], o[0][4] - it[4]) < gap: o[1] += 1; break
        else: out.append([it, 1])
    return out


def bld_hits(rs, blds):
    """차길 띠(폭/2 + 1 m)에 닿는 건물 [(건물 번호, 길 id, x, z)] — 1 m 표본마다."""
    bb = [(min(p[0] for p in b), max(p[0] for p in b), min(p[1] for p in b), max(p[1] for p in b)) for b in blds]
    g = defaultdict(list)
    for i, (x0, x1, z0, z1) in enumerate(bb):
        for gx in range(int(x0 // 10), int(x1 // 10) + 1):
            for gz in range(int(z0 // 10), int(z1 // 10) + 1): g[(gx, gz)].append(i)
    hits = []
    for r in rs:
        hw = r['width'] / 2 + SHOULDER
        for q in r['pts']:
            seen = set()
            for gx in range(int((q[0] - hw) // 10), int((q[0] + hw) // 10) + 1):
                for gz in range(int((q[1] - hw) // 10), int((q[1] + hw) // 10) + 1):
                    for i in g.get((gx, gz), ()):
                        if i in seen: continue
                        seen.add(i); x0, x1, z0, z1 = bb[i]
                        if q[0] < x0 - hw or q[0] > x1 + hw or q[1] < z0 - hw or q[1] > z1 + hw: continue
                        if L.in_poly(q[0], q[1], blds[i]) or L.poly_edge_dist(q[0], q[1], blds[i]) < hw:
                            hits.append((i, r['id'], q[0], q[1]))
    return hits


def _on(rs_idx, r, p, bank=None):
    """점 p 에서 길 r 중심선까지 거리와 그 자리 노면 높이(선분 투영 보간 + 판 옆 가로 기울기 bank)."""
    q = r['pts']; best = (1e9, 0.0)
    j = min(range(len(q)), key=lambda k: (q[k][0] - p[0]) ** 2 + (q[k][1] - p[1]) ** 2)
    for k in (j - 1, j):
        if k < 0 or k + 1 >= len(q): continue
        d, t = L.seg_dist(p[0], p[1], q[k][0], q[k][1], q[k + 1][0], q[k + 1][1])
        if (k == 0 and t <= 0) or (k + 2 == len(q) and t >= 1): continue    # 길 끝 너머는 노면이 아니다(빌더 띠는 끝에서 잘린다)
        if d < best[0]:
            y = q[k][2] + (q[k + 1][2] - q[k][2]) * t
            if bank:
                bk = bank.get(k, 0.0) * (1 - t) + bank.get(k + 1, 0.0) * t
                if bk:
                    dx, dz = q[k + 1][0] - q[k][0], q[k + 1][1] - q[k][1]; l = math.hypot(dx, dz) or 1
                    y += bk * ((p[0] - q[k][0]) * -dz / l + (p[1] - q[k][1]) * dx / l)
            best = (d, y)
    return best


def banks(d, rs):
    """v6g 판 옆 가로 기울기: {길 id: {표본 번호: bank}} — 팔 끝에서 arm.bank, 바깥(outward)으로 bankFade m 동안 0 으로."""
    byid = {r['id']: r for r in rs}; out = defaultdict(dict)
    for j in d.get('junctions', []):
        fade = j.get('bankFade')
        if not fade: continue
        for a in j['arms']:
            r = byid.get(a['road'])
            if not r or not a.get('bank'): continue
            q = r['pts']; e = min(range(len(q)), key=lambda k: (q[k][0] - a['edge'][0]) ** 2 + (q[k][1] - a['edge'][1]) ** 2)
            s, k = 0.0, e
            while 0 <= k < len(q) and s < fade:
                out[r['id']][k] = a['bank'] * (1 - s / fade)
                k2 = k + a['outward']
                if 0 <= k2 < len(q): s += math.hypot(q[k2][0] - q[k][0], q[k2][1] - q[k][1])
                k = k2
            k = e - a['outward']                                  # 판 안 쪽 표본도 같은 기울기(띠는 판 안에서 안 그리지만 보간용)
            while 0 <= k < len(q) and L.in_poly(q[k][0], q[k][1], [(v[0], v[1]) for v in j['poly']]):
                out[r['id']][k] = a['bank']; k -= a['outward']
    return out


def ledges(rs, plates=(), bank=None):
    """옆면 턱 [(|높이차|, 0, 길A↔길B, x, z)]. plates = [(다각형, 높이)] — 그 안의 점은 판 위라 뺀다."""
    bank = bank or {}
    cell = defaultdict(set)
    for n, r in enumerate(rs):
        for q in r['pts']: cell[(int(q[0] // 8), int(q[1] // 8))].add(n)
    pb = [(p, y, min(a for a, _ in p), max(a for a, _ in p), min(b for _, b in p), max(b for _, b in p)) for p, y in plates]
    def plate_y(x, z):
        for p, y, x0, x1, z0, z1 in pb:
            if x0 <= x <= x1 and z0 <= z <= z1 and L.in_poly(x, z, p): return y
        return None
    out = []
    for n, r in enumerate(rs):
        q = r['pts']; hw = r['width'] / 2
        for i in range(len(q)):
            a, b = q[max(0, i - 1)], q[min(len(q) - 1, i + 1)]
            dx, dz = b[0] - a[0], b[1] - a[1]; l = math.hypot(dx, dz) or 1; nx, nz = -dz / l, dx / l
            near = set()
            for gx in (-1, 0, 1):
                for gz in (-1, 0, 1): near |= cell.get((int(q[i][0] // 8) + gx, int(q[i][1] // 8) + gz), set())
            near.discard(n)
            if not near: continue
            steps = max(1, int(hw / 0.5))
            for s in range(-steps, steps + 1):
                o = hw * s / steps; p = (q[i][0] + nx * o, q[i][1] + nz * o)
                ya = q[i][2] + bank.get(r['id'], {}).get(i, 0.0) * o; py = plate_y(*p) if pb else None
                if py is not None: continue                   # 판 위: 빌더가 판을 깐다
                for m in near:
                    d, yb = _on(None, rs[m], p, bank.get(rs[m]['id']))
                    if d < rs[m]['width'] / 2 - 0.05:
                        out.append((abs(ya - yb), 0, f"{r['id']}↔{rs[m]['id']}", p[0], p[1]))
    return out


def plate_edges(d, rs):
    """판 경계에서 각 팔 띠 높이 − 판 평면 높이: 중심과 양쪽 가장자리(띠 가로 기울기 = arm.bank, 검사기 자신의 법선으로 다시 잼)."""
    out = []
    byid = {r['id']: r for r in rs}
    for j in d.get('junctions', []):
        gx, gz = j.get('grad', (0.0, 0.0)); px, pz = j['pos']
        yat = lambda x, z: j['y'] + gx * (x - px) + gz * (z - pz)
        for a in j.get('arms', []):
            r = byid.get(a['road'])
            if not r: continue
            q = r['pts']; e = min(range(len(q)), key=lambda k: (q[k][0] - a['edge'][0]) ** 2 + (q[k][1] - a['edge'][1]) ** 2)   # 팔 끝 = 그 길의 표본 하나
            u, v = q[max(0, e - 1)], q[min(len(q) - 1, e + 1)]
            dx, dz = v[0] - u[0], v[1] - u[1]; l = math.hypot(dx, dz) or 1; nx, nz = -dz / l, dx / l
            bk = a.get('bank', 0.0) if j.get('bankFade') else 0.0
            for o in (-a['width'] / 2, 0.0, a['width'] / 2):
                x, z = q[e][0] + nx * o, q[e][1] + nz * o
                out.append((abs(q[e][2] + bk * o - yat(x, z)), 0, f"{j['id']}:{a['road']}", x, z))
    return out


def check(tag):
    d, rs = RC.load(tag)
    s = RC.summary(rs)
    blds = [b['poly'] for b in d['buildings']]
    bh = bld_hits(rs, blds)
    plates = [([(v[0], v[1]) for v in j['poly']], j['y']) for j in d.get('junctions', []) if j.get('poly')]
    tilt = max((math.hypot(*j['grad']) for j in d.get('junctions', []) if j.get('grad')), default=0.0)
    lg = ledges(rs, plates, banks(d, rs))
    lgp = places([x for x in lg if x[0] > LEDGE])
    pe = plate_edges(d, rs)
    gs = grades(rs)
    over = places([g for g in gs if g[0] > g[1] + 0.002])
    def gmax(f): return max((g[0] for g in gs if f(g)), default=0)
    stone_ids = {r['id'] for r in rs if is_stone(r)}
    return {'tag': tag, 'bldHit': len({h[0] for h in bh}), 'bldHitM': len({(h[1], round(h[2]), round(h[3])) for h in bh}),
            'bldTop': sorted(defaultdict(int, {}).items()),
            'ledgePlaces': len(lgp), 'ledgeMax': max((x[0] for x in lg), default=0), 'ledgeTop': [(round(o[0][0], 2), o[0][2], round(o[0][3]), round(o[0][4])) for o in lgp[:8]],
            'plateEdgeMax': max((x[0] for x in pe), default=None), 'plates': len(plates), 'plateTiltMax': round(tilt, 4),
            'fun': {f['pair']: f.get('ratioRough') for f in d.get('fun', [])},
            'gradeOver': len(over), 'gradeOverTop': [(round(o[0][0], 3), o[0][1], o[0][2], round(o[0][3]), round(o[0][4])) for o in over[:10]],
            'gradeMaxCar': gmax(lambda g: g[2] not in stone_ids), 'gradeMaxStone': gmax(lambda g: g[2] in stone_ids),
            'bump': s['count'], 'bumpMax': s['max'], 'overlaps': len(s['overlaps']), 'bh': bh}


def main():
    tags = sys.argv[1:] or ['v6e', 'v6f']
    res = [check(t) for t in tags]
    lines = [f"# 주행 기준 검사 — {' → '.join(tags)}", '',
             f'띠 = 폭/2 + 어깨 {SHOULDER:.0f} m · 턱 > {100 * LEDGE:.0f} cm · 경사 한도 {100 * G_CAR:.0f} %(헤어핀 안쪽·돌길 {100 * G_PIN:.0f} %) 2 m 구간 · 덜컹 = 1 m 경사 변화 > 3 %/m(돌길 6) · 3 m 안 이어진 것 = 1곳', '']
    for c in res:
        by = defaultdict(int)
        for h in c['bh']: by[h[1]] += 1
        print(c['tag'], {k: v for k, v in c.items() if k not in ('bh', 'bldTop')}, 'bldRoads', sorted(by.items(), key=lambda t: -t[1])[:8])
        lines += [f"## {c['tag']}", '',
                  f"- 길↔건물: 건물 {c['bldHit']}채 · 닿는 길 {c['bldHitM']} m · 많이 닿는 길 {sorted(by.items(), key=lambda t: -t[1])[:6]}",
                  f"- 갈림목 옆면 턱 > 2 cm: {c['ledgePlaces']}곳 · 최대 {100 * c['ledgeMax']:.0f} cm · 판 {c['plates']}개 · 판 경계 높이 어긋남 최대 {c['plateEdgeMax']} · 판 기울기 최대 {100 * c['plateTiltMax']:.1f} %",
                  f"- 배달 시간비(요철): {c['fun']}",
                  f"  - 상위: {c['ledgeTop']}",
                  f"- 경사 한도 넘는 곳: {c['gradeOver']} · 최대 차길 {100 * c['gradeMaxCar']:.1f} % / 돌길 {100 * c['gradeMaxStone']:.1f} %",
                  f"  - 상위: {c['gradeOverTop']}",
                  f"- 덜컹(1 m 경사 변화): {c['bump']} · 최대 %/m {({k: round(100 * v, 1) for k, v in c['bumpMax'].items()})} · 나란히 겹친 쌍 {c['overlaps']}", '']
    open(os.path.join(OUT, f"drive-check-{'-'.join(tags)}.md"), 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
    return res


if __name__ == '__main__':
    main()
