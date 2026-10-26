"""마을 v5c 평면 — v5b 판정(건물이 흩뿌린 점 · 칸 못 채움 · 규칙의 깨끗함 · 곶 길 어긋남 · 지름길 0.29) 고침.
사용: python -X utf8 tools/map/make_town_v5c.py   (QUICK=1 이면 길·재미 검사만, 건물·그림 생략)
v5b 와 같은 땅·뼈대(make_town_v5b 를 불러온다)에서 바꾼 것:
  건물 = 길에 면한 연속 필지(옆 틈 0~0.5 m 심장부, 깊이 다양, 길 휘면 따라 휜 사다리꼴, ㄱ·ㄷ 날개, 마당 뒤채) + 성긴 농가 — make_town_v5.buildings_rows
  칸 채움 = 동쪽 해안 앞길·골목 격자, 서쪽 층층길, 골짜기 줄기 가로 골목, 언덕 위 길·농가
  불규칙 = 길 폭이 구간마다 다름(widthProfile), 막다른 골목·꺾인 골목, 광장 2곳
  곶 길 = 지형에 맞춰 다시 그림(능선 낮춤, 바다 위 점 제거), 등대 지름길 = 곶 길 중간에 붙는 해안 돌길
"""
import math, os, random, sys
from collections import defaultdict
sys.path.insert(0, os.path.dirname(__file__))
import make_town_v5b as B
import make_town_v5 as V
import planlib as L

S = L.smooth
QUICK = 'quick' in sys.argv
V.TAG, V.TITLE = 'v5c', '마을 v5c (연속 벽면·해안 띠·줄기)'
V.PREV = [os.path.join(V.OUT, 'town-v5b.png')]
V.DY_LINK = 2.5
rnd = random.Random(1631)
V.rnd.seed(1631)
zc, xv1, xv2, contour, thin, ramp, near_pt, ramp_end = B.zc, B.xv1, B.xv2, B.contour, B.thin, B.ramp, B.near_pt, B.ramp_end
R, J = V.R, V.J

# ── 땅: 곶 등줄기를 낮춰(20→9 m) 길이 지형을 따라가게 ──
_h0 = B.h


def h(x, z, base=False):
    y = _h0(x, z, base)
    if base or y <= 1.6:
        return y
    d = z - zc(x)
    y -= 11 * math.exp(-((x + 292) / 30) ** 2) * math.exp(-((z + 325) / 45) ** 2)
    return max(y, 1.5 + 0.03 * min(d, 200))


B.h = h; V.h = h


def sc(*a, **k):
    try:
        return contour(*a, **k)
    except IndexError:
        return []


# ── 바꿔 그릴 길을 지운다 ──
DROP = {'V_stem', 'V_cape', 'SC_shore', 'L_w', 'SC_ridge', 'V_field', 'A_prom', 'L_e0', 'L_e1'}
V.ROADS[:] = [r for r in V.ROADS if r['id'] not in DROP and not r['id'].startswith(('L_a', 'L_b', 'A_h')) and r['id'] not in ('V_quay', 'L_mid', 'V_harbor')]
RD = {r['id']: r for r in V.ROADS}
main = [tuple(p) for p in RD['main']['xy']]
# 항구 심장 다시: 부두 d≈8(1.9 m 선) · 가운뎃길 d≈43(5.6 m 선) — 사이가 35 m 라 양쪽에서 짓는 두 줄이 들어간다. 큰길(d≈120)까지 80 m 깊이
quay = sc(1.9, -150, 85, step=4)
mid = sc(5.6, -140, 75, step=4)
R('V_quay', 'village', 5.0, 0.2, quay, '부두 길 — 만을 따라 휜 1.9 m 선(도모노우라 해안 길 폭 5 m)', xs='shared')
R('L_mid', 'village', 3.4, 0.45, mid, '가운뎃길 — 옛 상점가 돌포장(도모노우라)', xs='lane')
V.SAMP_PRE = {'V_quay': quay, 'L_mid': mid, 'main': main}
for k, x in enumerate([-128, -72, -10, 52]):
    B.cross(x, 'V_quay', 'L_mid', 'a', k)
for k, x in enumerate([-110, -62, -10, 38, 76]):
    B.cross(x, 'L_mid', 'main', 'b', k)
R('A_h0', 'alley', 2.0, 0.3, thin(sc(3.7, -100, -40, step=4)), '사람 골목')
R('A_h1', 'alley', 2.0, 0.3, thin(sc(7.5, -20, 40, step=4)), '사람 골목')
R('V_harbor', 'village', 4.5, 0.2, [quay[-1], min(main, key=lambda p: math.dist(p, (quay[-1][0] + 40, quay[-1][1] + 20)))],
  '동쪽 항구 길 — 부두 끝에서 큰길로(어시장 트럭)', start=J('V_quay'), end=J('main'))
upper = [tuple(p) for p in RD['V_upper']['xy']]
east2 = [tuple(p) for p in RD['V_rocco']['xy']][-len(B.east2):]
tier_pts = B.tier_pts

def lane_to(a, hi, g=0.11):
    """a 에서 hi 길 위 점 중 경사 g 이하로 닿는 가장 가까운 점(양쪽 방향). 없으면 None."""
    c = [p for p in hi if math.dist(p, a) > 12 and math.dist(p, a) * g >= abs(h(*p) - h(*a))]
    return min(c, key=lambda p: math.dist(p, a)) if c else None


def dev(a, b):
    """a→b 직선 아래 지형이 직선 높이에서 벗어난 최대값(성토·절토 예상)."""
    ya, yb = h(*a), h(*b)
    return max(abs(h(a[0] + (b[0] - a[0]) * t / 10, a[1] + (b[1] - a[1]) * t / 10) - (ya + (yb - ya) * t / 10)) for t in range(1, 10))


def pick_lane(src, x, hi):
    best = None
    for off in (0, 10, -10, 20, -20, 32, -32, 45, -45):
        a = near_pt(src, x + off); b = lane_to(a, hi)
        if b is None: continue
        dv = dev(a, b)
        if best is None or dv < best[0]: best = (dv, a, b)
    return (best[1], best[2]) if best and best[0] < 3.5 else (None, None)


def add_lane(rid, a, b, w, why, start, end, rough=0.45):
    if b is None: return None
    return R(rid, 'village', w, rough, [a, ((a[0] + b[0]) / 2 + rnd.uniform(-6, 6), (a[1] + b[1]) / 2 + rnd.uniform(-3, 3)), b], why, start=start, end=end, xs='lane')


# 골짜기 줄기 길(아말피) — 큰 골 바닥을 좌우로 휘며(경사 18 % 안쪽으로 길이를 늘려) 올라감
stem = []
for dd in range(110, 371, 12):
    x = xv1(dd) + 8 * math.sin(dd / 19); stem.append((x, zc(x) + dd))
R('V_stem', 'shortcut', 3.4, 0.45, [min(main, key=lambda p: math.dist(p, stem[0]))] + stem, "골짜기 줄기 길 — 큰 골 바닥을 좌우로 휘며 올라감(아말피 골목 줄기). 좁은 돌길, Z 길보다 짧고 거침", start=J('main'), xs='lane')

# ── 곶 길(등대): 지형 높이에 맞춰 — 터널 입구에서 곶 머리 지그재그, 바다 위 점 없음(coast 에서 d≥4) ──
cape = [(-266, -290), (-290, -292), (-316, -296)]
cape += V.hairpin(cape[-1], (-316, -318), (-1, 0)) + [(-316, -318), (-292, -322), (-274, -324)]
cape += V.hairpin(cape[-1], (-274, -340), (1, 0)) + [(-274, -340), (-285, -346), (-296, -352), (-293, -363)]
R('V_cape', 'village', 4.5, 0.25, cape, '등대 곶 길 — 터널 입구에서 곶 머리를 헤어핀 둘로 넘어(지형 따라) 끝의 등대로', start=J('main'), end={'type': '등대', 'label': '곶 끝 흰 등대'})
shore = [p for p in sc(3.0, -150, -262, step=4) if p[1] > -300] + [(-262, -312), (-270, -319), (-274, -324)]
R('SC_shore', 'shortcut', 3.0, 0.4, [quay[0]] + shore, '해안 돌길 — 부두 서쪽 끝에서 곶 발치를 따라 곶 길 첫 헤어핀 앞으로(큰길의 헤어핀 둘을 건너뜀)', start=J('V_quay'), end=J('V_cape'))
sw = near_pt(shore, -200); mw = ramp_end(sw, main, 1, 0.11)
R('L_w', 'village', 3.2, 0.4, [mw, ((sw[0] + mw[0]) / 2 + 4, (sw[1] + mw[1]) / 2), sw], '해안 돌길로 내려가는 갈림', start=J('main'), end=J('SC_shore'), xs='lane')

# ── 서쪽 층층길(칸 서쪽 채움): 곶 목 위 비탈, 기운 등고선 3층 + 헤어핀 ──
WT = [(14, 28, -232, -378), (34, 46, -378, -250), (52, 60, -250, -372)]
wz = []; wt_pts = []
for k, (ya, yb, xa, xb) in enumerate(WT):
    pts = sc(ramp(ya, yb, xa, xb), xa, xb, step=3)
    if not pts: continue
    if wz:
        A = wz[-1]; hd = (A[0] - wz[-2][0], A[1] - wz[-2][1]); m = math.hypot(*hd) or 1
        wz += V.hairpin(A, pts[0], (hd[0] / m, hd[1] / m))
    wz += pts; wt_pts.append(pts)
st = min(main, key=lambda p: math.dist(p, wz[0]))
R('V_west', 'village', 3.8, 0.3, [st] + wz, '서쪽 층층길 — 아말피 Via Capo di Croce: 곶 목 위 비탈을 기운 등고선 3층 + 헤어핀', start=J('main'))

# ── 동쪽 해안 띠(카몰리): 물가 앞길 + 큰길·윗길 사이 골목 격자 ──
front = sc(ramp(2.6, 3.4, 100, 380), 100, 380, step=4)
a0 = min(main, key=lambda p: math.dist(p, front[0]))
R('V_front', 'village', 4.0, 0.2, [a0] + front, '해안 앞길 — 카몰리 Lungomare: 물가를 따라 칸 끝까지(고기잡이 배 쪽 길)', start=J('main'), xs='lane')
for k, x in enumerate([135, 185, 235, 280, 330, 372]):
    a, b = pick_lane(front, x, main)
    add_lane(f'L_f{k}', a, b, 3.0, '해안 앞길 → 큰길 짧은 골목(카몰리 salita)', J('V_front'), J('main'))
for k, x in enumerate([150, 215, 265, 320, 360]):
    a, b = pick_lane(main, x, upper)
    add_lane(f'L_e{k}', a, b, 3.2, '띠 비탈 골목(카몰리 salita)', J('main'), J('V_upper'))

# ── 능선 흙길: 꼭대기 밭로 곧장 — 굽이 두 번 + 중간 둔턱, 출발을 큰길 동쪽 끝에서 ──
ridge = []
for dd in range(30, 451, 22):
    x = 60 + 0.1 * dd + 34 * math.sin(dd / 30); ridge.append((x, zc(x) + dd))
ra = min(main, key=lambda p: math.dist(p, ridge[0]))
R('SC_ridge', 'shortcut', 3.2, 0.7, [ra] + ridge, '능선 흙길 — 두 골 사이 능선을 구불구불 올라 꼭대기 밭으로(급경사·마루 둔턱·굽이)', start=J('main'),
  features=[{'type': 'jump', 'near': list(ridge[4]), 'step': 0.5, 'name': '능선 마루 둔턱'}])
fld = ridge[-1]; top = tier_pts[3][-1]
t3e = near_pt(tier_pts[2], 95)
R('V_field', 'village', 4.0, 0.3, [t3e, ((t3e[0] + fld[0]) / 2, (t3e[1] + fld[1]) / 2 - 5), fld], '밭 진입로 — Z 길 3층 동쪽 끝에서 능선 밭으로', start=J('V_pasitea'))

# ── 언덕 위 농가 길(성긴 언덕 채움): 동쪽 위 · 서쪽 위 ──
hill_e = sc(ramp(54, 66, 185, 380), 185, 380, step=4)
if hill_e:
    ee = east2[-1]
    R('V_hill_e', 'village', 3.4, 0.5, [ee] + hill_e, '동쪽 언덕 농가 길 — 동쪽 언덕 길 위 높은 선(포도밭·농가)', start=J('V_rocco'), xs='lane')
hill_w = sc(ramp(66, 74, -372, -200), -372, -200, step=4)
if hill_w:
    wt = near_pt(wt_pts[-1], -372)
    R('V_hill_w', 'village', 3.4, 0.5, [wt] + hill_w, '서쪽 언덕 농가 길 — 서쪽 층층길 맨 위에서 높은 선(농가)', start=J('V_west'), xs='lane')

# ── 계단 · 사람 골목 ──
k = 100
for lo, hi, xs in [(main, wt_pts[0], range(-232, -370, -28)), (wt_pts[0], wt_pts[1], range(-370, -250, 30))] if len(wt_pts) > 1 else []:
    for x in xs:
        a = near_pt(lo, x + rnd.uniform(-8, 8)); b = near_pt(hi, a[0])
        R(f'S_{k}', 'stairs', 1.8, 0.0, B.stairs_from(a[0], a[1], h(*b)), '계단 — 경사 직하(서쪽 비탈)', terrainFollows=False); k += 1
for x in (110, 175, 245, 310, 365):
    a = near_pt(front, x)
    R(f'A_f{x}', 'alley', 1.8, 0.2, [a, (a[0] + rnd.uniform(-4, 4), a[1] + 6), (a[0] + rnd.uniform(-5, 5), a[1] + 13)], '물가 사람 골목(선창으로 내려가는)')
if 'A_valley' not in RD: pass

# ── 막다른 길 · 꺾인 골목(시간이 쌓인 불규칙) ──
def road_grid():
    g = defaultdict(list)
    for r in V.ROADS:
        for p in L.sample({'knots': [[x, zz, 0.0] for x, zz in r['xy']]}, 3.0):
            g[(int(p[0] // 10), int(p[1] // 10))].append((p[0], p[1], r['id']))
    return g


def clear(g, x, zz, rad, skip, p0):
    for dx in (-1, 0, 1):
        for dz in (-1, 0, 1):
            for a, b, rid in g.get((int(x // 10) + dx, int(zz // 10) + dz), ()):
                if math.dist((a, b), (x, zz)) < rad and not (rid == skip and math.dist((a, b), p0) < 10):
                    return False
    return True


SPUR_PARENTS = ['V_stem', 'V_pasitea', 'V_upper', 'V_front', 'L_mid', 'V_west', 'V_quay']
spurs = []


def make_spurs(n_want, tries=400):
    g = road_grid(); made = 0
    for _ in range(tries):
        if made >= n_want: break
        pid = rnd.choice(SPUR_PARENTS); par = next((r for r in V.ROADS if r['id'] == pid), None)
        if not par: continue
        pts = L.sample({'knots': [[x, zz, 0.0] for x, zz in par['xy']]}, 4.0)
        i = rnd.randint(3, len(pts) - 4); p0 = pts[i]
        tx, tz = pts[i + 2][0] - pts[i - 2][0], pts[i + 2][1] - pts[i - 2][1]; m = math.hypot(tx, tz) or 1
        side = rnd.choice((-1, 1)); nx, nz = -tz / m * side, tx / m * side
        ln = rnd.uniform(34, 70); bend = math.radians(rnd.choice((-1, 1)) * rnd.uniform(30, 60))
        p1 = (p0[0] + nx * ln * 0.55, p0[1] + nz * ln * 0.55)
        n2 = (nx * math.cos(bend) - nz * math.sin(bend), nx * math.sin(bend) + nz * math.cos(bend))
        p2 = (p1[0] + n2[0] * ln * 0.45, p1[1] + n2[1] * ln * 0.45)
        path = [p0[:2], p1, p2]; ok = True; prev = None
        for a, b in zip(path, path[1:]):
            for t in range(1, 9):
                x = a[0] + (b[0] - a[0]) * t / 8; zz = a[1] + (b[1] - a[1]) * t / 8
                if h(x, zz) < 3.5 or not (-V.HALF + 6 < x < V.HALF - 6 and -V.HALF + 6 < zz < V.HALF - 6) or not clear(g, x, zz, 11, pid, p0[:2]): ok = False; break
                if prev is not None and abs(h(x, zz) - prev) > 0.14 * math.dist(a, b) / 8: ok = False; break
                prev = h(x, zz)
            if not ok: break
        if not ok: continue
        R(f'L_dead{made}', 'village', 3.0, 0.45, path, '막다른 골목(꺾여서 끝남) — 옛 집들이 쌓이며 남은 길', start=J(pid), end={'type': 'dead', 'label': '막다른 길'}, xs='lane')
        for t in range(0, 9):    # 새 골목도 격자에 반영
            for a, b in zip(path, path[1:]):
                g[(int((a[0] + (b[0] - a[0]) * t / 8) // 10), int((a[1] + (b[1] - a[1]) * t / 8) // 10))].append((a[0] + (b[0] - a[0]) * t / 8, a[1] + (b[1] - a[1]) * t / 8, f'L_dead{made}'))
        made += 1
    return made


# ── 장소 ──
cape_end = cape[-1]
V.STOPS[2]['pos'] = [round(cape_end[0], 1), round(cape_end[1], 1)]
V.STOPS[2]['why'] = '서쪽 곶 끝 — 곶 길(헤어핀 둘) vs 해안 돌길(첫 헤어핀 앞에서 합류)'
V.STOPS[3]['pos'] = [round(fld[0], 1), round(fld[1], 1)]
V.FIELDS[:] = [{'id': 'F_hill', 'rect': [round(fld[0]), round(fld[1] + 18), 46, 30], 'bands': 4, 'why': '능선 꼭대기 — 아말피 계단밭'}]
mart = near_pt(main, 200)
V.MART.update({'pos': [round(mart[0], 1), round(mart[1], 1)], 'why': '큰길이 동쪽 해안 띠로 들어서는 어귀 — 큰길가 상점(카몰리). 능선 흙길 들머리에서 멀다'})


# ── 구역 ──
def zone(x, zz):
    d = zz - zc(x)
    if d < 110 and -160 < x < 90: return 'dense'
    if abs(x - xv1(d)) < 36 and d < 330: return 'dense'
    if x > 90 and d < 85: return 'dense'
    if d < 460 and -240 < x < 400: return 'medium'
    if d < 360 and x <= -240: return 'medium'
    return 'sparse'


V.zone = zone


def districts():
    return [{'id': 'D_harbor', 'density': 'dense', 'name': '항구 심장(도모노우라)', 'rule': 'd<110, -160<x<90'},
            {'id': 'D_stem', 'density': 'dense', 'name': '골짜기 줄기(아말피)', 'rule': '|x-xv1(d)|<36, d<330'},
            {'id': 'D_strip', 'density': 'dense', 'name': '동쪽 해안 띠(카몰리)', 'rule': 'x>90, d<85'},
            {'id': 'D_west', 'density': 'medium', 'name': '서쪽 층층 마을', 'rule': 'x<-240, d<360'},
            {'id': 'D_slope', 'density': 'medium', 'name': '비탈(포지타노)', 'rule': 'd<460'}]


V.DISTRICTS = districts
V.FARMS = 110
V.INFILL.update({'dense': 0.5, 'medium': 0.2, 'sparse': 0.0})
V.ROWP['dense'].update(g0=0.8, wing=0.5, back=0.75)
V.ROWP['medium'].update(g0=0.5, g=(0.5, 2.5), wing=0.3, back=0.4)
# 계단이 블록을 너무 잘게 쪼갬(블록 장변 40 m, 실제 59~84 m) — 계단 절반만 남김
V.ROADS[:] = [r for r in V.ROADS if not (r['id'].startswith('S_') and rnd.random() < 0.5)]

_fin = V.finalize


def finalize():
    _fin()
    for r in V.ROADS:
        if r['kind'] in ('main', 'village', 'shortcut') and r['id'] not in ('V_quay',):
            V.WPROF[r['id']] = V.make_wprof(r, len(V.SAMP[r['id']]), rnd)


V.finalize = finalize
make_spurs(16)
V.PLAZAS[:] = []
_pa = near_pt(mid, -25); _pb = near_pt(quay, -25)
V.PLAZAS.append({'pos': [round((_pa[0] + _pb[0]) / 2, 1), round((_pa[1] + _pb[1]) / 2, 1)], 'r': 12, 'name': '항구 광장(부두와 가운뎃길 사이)'})
_st = [r for r in V.ROADS if r['id'] == 'V_stem'][0]['xy']
_sp = _st[len(_st) // 2]
V.PLAZAS.append({'pos': [round(_sp[0] + 14, 1), round(_sp[1], 1)], 'r': 10, 'name': '골짜기 성당 앞 광장'})
if not QUICK:
    V.buildings = V.buildings_rows

if __name__ == '__main__':
    if QUICK:
        V.finalize()
        for r in V.ROADS:
            if r.get('cutFillMax', 0) > 3: print('cutfill', r['id'], r['cutFillMax'])
        for row in V.fun_check():
            print(row['pair'], row.get('ratio'), row.get('ratioRough'), {t: row.get(t, {}) and (row[t]['m'], row[t]['hairpins']) for t in ('big', 'short') if row.get(t)})
    else:
        V.main()
