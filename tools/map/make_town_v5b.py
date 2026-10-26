"""마을 v5b 평면 — v5 판정(부채꼴·동심원, 작고 성김, 큰길 짧음) 고침. 생성·건물·재미 검사·그림은 make_town_v5 를 그대로 쓰고 땅과 길만 바꾼다.
사용: python -X utf8 tools/map/make_town_v5b.py → docs/map/town-v5b.json · town-v5b.png · v5b-vs-real.png
v5 와 다른 점:
  땅 = 비대칭. 해안이 서남→동북으로 비스듬(카몰리), 만은 서쪽으로 치우침, 큰 곶은 서쪽 하나, 골짜기 둘(서쪽 큰 골은 비스듬히 북서로, 동쪽 작은 골), 그 사이 능선.
  길 = 손으로 반원을 그리지 않고 '등고선을 따라간다' — 높이 Y(x) 를 주면 그 높이 선을 찾아 길로 삼는다. 그래서 골에서는 안으로 파고들고 능선에서는 밖으로 불룩, 층마다 길이·끝 위치가 다르다.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import make_town_v5 as V
import planlib as L

S = L.smooth
V.TAG, V.TITLE = 'v5b', '마을 v5b (비대칭 지형·등고선 길)'
V.PREV = [os.path.join(V.OUT, 'town-v5.png')]
V.DY_LINK = 2.5                    # 등고선 길끼리 가운데서 만나는 교차는 높이 맞춤을 안 하므로 조금 느슨하게
V.rnd.seed(1562)


# ── 층 1·2: 땅 ──
def zc(x):
    """해안: 서남→동북 비스듬(카몰리) + 서쪽으로 치우친 만(x≈-40, 90 m) + 서쪽 큰 곶(x≈-300, 남으로 120 m) + 동쪽 작은 갑(x≈230)."""
    return (-230 + 0.15 * x + 90 * math.exp(-((x + 40) / 80) ** 2) - 95 * math.exp(-((x + 290) / 45) ** 2)
            - 25 * math.exp(-((x - 230) / 30) ** 2))


def xv1(d):   # 서쪽 큰 골 — 만 뒤에서 북서로 비스듬히(아말피 Valle dei Mulini)
    return -40 - 0.35 * max(0.0, d - 40)


def xv2(d):   # 동쪽 작은 골
    return 150 + 0.15 * d


def h(x, z, base=False):
    d = z - zc(x)
    if d < 0:
        return -6.0
    fw = 15 + 32 * math.exp(-((x + 40) / 110) ** 2)            # 해안 평지 폭: 만 안쪽 47 m, 나머지 15 m(카몰리 띠)
    top = 70 + 0.03 * (x + 380)                                  # 동쪽이 조금 더 높고 가파름
    y = 1 + 5 * min(d, fw) / fw + (top - 6) * S((d - fw) / 520)
    if base:
        return max(y, 1.5 + 0.03 * min(d, 200))
    y -= 20 * math.exp(-((x - xv1(d)) / 30) ** 2) * S((d - 30) / 40)                                   # 큰 골
    y -= 14 * math.exp(-((x - xv2(d)) / 18) ** 2) * S((d - 20) / 30) * (1 - S((d - 280) / 60))         # 작은 골
    y += 10 * math.exp(-((x - 60 - 0.1 * d) / 40) ** 2) * S(d / 80)                                     # 두 골 사이 능선
    y += 20 * math.exp(-((x + 292) / 30) ** 2) * math.exp(-((z + 325) / 45) ** 2)                       # 곶 등줄기
    y += 4 * math.sin(x / 47 + 1.3) * S(d / 150)                                                         # 잔주름
    return max(y, 1.5 + 0.03 * min(d, 200))


def contour(Y, x0, x1, step=3.0, d0=0.0, base=False):
    """높이 Y(x)(숫자 또는 함수) 선을 x0→x1 로 따라간다. 해안에서 위로 처음 Y 를 넘는 곳."""
    out = []; n = max(2, int(abs(x1 - x0) / step))
    for k in range(n + 1):
        x = x0 + (x1 - x0) * k / n; y = Y(x) if callable(Y) else Y
        prev = None; dd = d0
        while dd < 760:
            hv = h(x, zc(x) + dd, base)
            if hv >= y:
                if prev is not None:
                    dd -= 2 * (hv - y) / max(1e-6, hv - prev)
                out.append((x, zc(x) + dd)); break
            prev = hv; dd += 2
    return thin(out)


def thin(pts, gap=10.0):
    o = [pts[0]]
    for p in pts[1:-1]:
        if math.dist(p, o[-1]) >= gap: o.append(p)
    o.append(pts[-1]); return o


def ramp(Ya, Yb, xa, xb):
    return lambda x: Ya + (Yb - Ya) * (x - xa) / (xb - xa)


def grad_dir(x, z, e=3.0):
    gx = (h(x + e, z) - h(x - e, z)); gz = (h(x, z + e) - h(x, z - e)); m = math.hypot(gx, gz) or 1
    return gx / m, gz / m


V.zc, V.h = zc, h
R, J = V.R, V.J
V.ROADS.clear()

# ── 층 4(먼저 뼈대): 해안 큰길 — 높이 10 m 선(아말피 SS163). 서쪽은 곶 목을 터널로 뚫고 칸 밖으로, 동쪽은 해안 띠를 따라 칸 밖으로 ──
mainE = contour(lambda x: 10 + 6 * S((x - 150) / 200), -235, 400, step=4, base=True)
mainW = [(-400, -318), (-330, -304), (-268, -288)]
main = mainW + [p for p in mainE if math.dist(p, mainW[-1]) > 12]
R('main', 'main', 6.0, 0.1, main, '해안 큰길 — 높이 10 m 선을 따라 만 뒤를 돌고(골 입구에서 안으로 굽음), 동쪽 해안 띠를 따라 칸 밖으로; 서쪽은 곶 목 터널로 칸 밖',
  start={'type': '터널', 'label': '서쪽 곶 터널(옆 마을, 칸 밖)'}, end={'type': 'out', 'label': '동쪽 해안도로(시내, 칸 밖)'},
  features=[{'type': 'tunnel', 'from': [-400, -318], 'to': [-275, -290], 'name': '곶 목 터널 125 m'}], yFix=[(-420, -266, 10.0)])

# ── 층 3: 항구 심장(도모노우라) — 만 안쪽. 부두(2 m 선)·가운뎃길(5.2 m 선)·큰길 사이를 세로 골목이 엇갈려 끊음. 블록 50~70 m ──
quay = contour(2.2, -150, 85, step=4)
mid = contour(5.2, -140, 75, step=4)
R('V_quay', 'village', 5.0, 0.2, quay, '부두 길 — 만을 따라 휜 2 m 높이 선(도모노우라 해안 길 폭 5 m)', xs='shared')
R('L_mid', 'village', 3.4, 0.45, mid, '가운뎃길 — 옛 상점가 돌포장(도모노우라)', xs='lane')


def cross(x, lo, hi, tag, k, jit=6):
    """lo 길 → hi 길, x 근처에서 경사 방향으로(골목은 해안에 직각)."""
    a = min(V.SAMP_PRE[lo], key=lambda p: abs(p[0] - x)); b = min(V.SAMP_PRE[hi], key=lambda p: abs(p[0] - x - V.rnd.uniform(-jit, jit)))
    m = ((a[0] + b[0]) / 2 + V.rnd.uniform(-4, 4), (a[1] + b[1]) / 2)
    return R(f'L_{tag}{k}', 'village', 3.2, 0.45, [a, m, b], '해안에 직각인 옛 골목 — 위·아래 칸이 엇갈린 T자(도모노우라), 간격 50~70 m', start=J(lo), end=J(hi), xs='lane')


V.SAMP_PRE = {'V_quay': quay, 'L_mid': mid, 'main': main}
for k, x in enumerate([-128, -72, -10, 52]):
    cross(x, 'V_quay', 'L_mid', 'a', k)
for k, x in enumerate([-104, -40, 20, 68]):
    cross(x, 'L_mid', 'main', 'b', k)
R('A_h0', 'alley', 2.0, 0.3, thin(contour(3.6, -100, -40, step=4)), '사람 골목(블록 3~4개당 하나 — 도모노우라 2 m 골목)')
R('A_h1', 'alley', 2.0, 0.3, thin(contour(7.5, -20, 40, step=4)), '사람 골목')
R('V_harbor', 'village', 4.5, 0.2, [quay[-1], min(main, key=lambda p: math.dist(p, (quay[-1][0] + 40, quay[-1][1] + 20)))],
  '동쪽 항구 길 — 부두 끝에서 큰길로(어시장 트럭)', start=J('V_quay'), end=J('main'))

# ── 층 5: 비탈 — 기운 등고선(층마다 7~9 % 오름) + 끝에서 헤어핀. 층마다 범위가 달라 Z 가 비대칭 ──
TIERS = [  # (시작 높이, 끝 높이, 시작 x, 끝 x)
    (11, 26, -205, 40),       # 1층: 만 뒤 서쪽에서 출발해 큰 골을 깊이 감고 능선 앞까지
    (32, 44, 40, -175),       # 2층: 큰 골 안쪽으로 크게 파고듦
    (50, 61, -175, 105),      # 3층: 능선 넘어 작은 골 가장자리까지
    (66, 71, 105, -30),       # 4층(꼭대기): 짧게
]
z_road = []; tier_pts = []
for k, (ya, yb, xa, xb) in enumerate(TIERS):
    pts = contour(ramp(ya, yb, xa, xb), xa, xb, step=3)
    if z_road:
        A = z_road[-1]; hd = (A[0] - z_road[-2][0], A[1] - z_road[-2][1]); m = math.hypot(*hd)
        z_road += V.hairpin(A, pts[0], (hd[0] / m, hd[1] / m))
    z_road += pts; tier_pts.append(pts)
st = min(main, key=lambda p: math.dist(p, z_road[0]))
R('V_pasitea', 'village', 4.0, 0.25, [st] + z_road, '비탈 Z 길 — 포지타노 Viale Pasitea: 기운 등고선 4층 + 헤어핀 3번. 골에서 안으로 파고들어 층마다 모양이 다름', start=J('main'))

# 골짜기 줄기 길(아말피 Via Lorenzo d'Amalfi) — 큰 골 바닥을 따라 곧장 오름. 좁은 돌길이라 '마을 안 지름길'
stem = []
for dd in range(110, 381, 15):
    x = xv1(dd); stem.append((x, zc(x) + dd))
st0 = min(main, key=lambda p: math.dist(p, stem[0]))
R('V_stem', 'shortcut', 3.4, 0.45, [st0] + stem, "골짜기 줄기 길 — 큰 골 바닥을 따라 곧장(아말피 골목 줄기). 좁은 돌길(경사 18 %까지), Z 길보다 짧고 거침", start=J('main'), xs='lane')

def ramp_end(a, hi, dirx, g=0.15):
    """a 에서 hi 길 위 점 중, 경사 g 이하가 되는 가장 가까운 점(dirx 쪽으로)."""
    c = [p for p in hi if (p[0] - a[0]) * dirx >= 0 and math.dist(p, a) * g >= h(*p) - h(*a)]
    return min(c, key=lambda p: math.dist(p, a)) if c else max(hi, key=lambda p: (p[0] - a[0]) * dirx)


# 동쪽 해안 띠(카몰리) — 큰길 위 18~30 m 선의 윗길 + 잇는 비탈 둘
upper = contour(ramp(24, 30, 120, 380), 120, 380, step=4)
R('V_upper', 'village', 3.6, 0.3, upper, '동쪽 띠 윗길 — 카몰리 Via Figari: 해안 띠 위쪽을 나란히', xs='lane')
east2 = contour(ramp(36, 50, 370, 175), 370, 175, step=4)
hd = (upper[-1][0] - upper[-2][0], upper[-1][1] - upper[-2][1]); m = math.hypot(*hd)
R('V_rocco', 'village', 3.6, 0.3, [upper[-2], upper[-1]] + V.hairpin(upper[-1], east2[0], (hd[0] / m, hd[1] / m)) + east2,
  '동쪽 언덕 길 — 카몰리 Via San Rocco: 윗길 끝에서 헤어핀 하나로 꺾어 올라 작은 골에서 끝남', start=J('V_upper'), xs='lane')
for k, x in enumerate([190, 300]):
    a = min(main, key=lambda p: abs(p[0] - x)); b = ramp_end(a, upper, -1, 0.11)
    R(f'L_e{k}', 'village', 3.2, 0.45, [a, ((a[0] + b[0]) / 2 - 8, (a[1] + b[1]) / 2), b], '띠 비탈 골목(카몰리 salita)', start=J('main'), end=J('V_upper'), xs='lane')

# 차 지름길(돌길 비탈) — 층 사이를 비스듬히
def near_pt(pts, x):
    return min(pts, key=lambda p: abs(p[0] - x))


for k, (lo, hi, xa, dx) in enumerate([(main, tier_pts[0], 20, -1), (tier_pts[0], tier_pts[1], -120, 1), (tier_pts[1], tier_pts[2], -10, 1), (tier_pts[2], tier_pts[3], 20, 1)]):
    a = near_pt(lo, xa); b = ramp_end(a, hi, dx)
    R(f'SC_ramp{k}', 'shortcut', 3.0, 0.65, [a, ((a[0] + b[0]) / 2 + 6, (a[1] + b[1]) / 2), b], f'{k}층 → {k + 1}층 돌길 비탈(카몰리 salita)', start=J('main' if k == 0 else 'V_pasitea'), end=J('V_pasitea'))

# ── 바깥(아말피): 등대 곶 길 · 해안 돌길 · 능선 흙길 → 꼭대기 밭 ──
cape = [(-266, -290), (-295, -292), (-318, -296)]                    # 곶 머리(약 30 m)를 지그재그로 넘음 — 아말피 곶 길(Capo di Conca)
cape += V.hairpin(cape[-1], (-318, -318), (-1, 0)) + [(-318, -318), (-290, -320), (-268, -326)]
cape += V.hairpin(cape[-1], (-270, -346), (1, 0)) + [(-270, -346), (-292, -350), (-300, -356), (-293, -363)]
R('V_cape', 'village', 4.5, 0.25, cape, '등대 곶 길 — 터널 입구에서 곶 머리(약 30 m)를 헤어핀 둘로 넘어 끝의 등대로', start=J('main'), end={'type': '등대', 'label': '곶 끝 흰 등대'})
shore = contour(3.0, -150, -262, step=4)
shore = [p for p in shore if p[1] > -300] + [(-262, -318), (-268, -340), (-280, -356), cape[-2]]
R('SC_shore', 'shortcut', 3.0, 0.4, [quay[0]] + shore, '해안 돌길 — 부두 서쪽 끝에서 곶 동쪽 발치를 따라 등대로. v5 의 사람 해안길을 차 한 대 폭 돌길로(아말피 해안 marina 길)',
  start=J('V_quay'), end=J('V_cape'))
sw = near_pt(shore, -200); mw = ramp_end(sw, main, 1, 0.11)
R('L_w', 'village', 3.2, 0.4, [mw, ((sw[0] + mw[0]) / 2 + 4, (sw[1] + mw[1]) / 2), sw], '해안 돌길로 내려가는 갈림 — 큰길에서 곶 발치 해안 돌길로(옛 사람길을 차 한 대 폭으로)', start=J('main'), end=J('SC_shore'), xs='lane')
ridge = []
for dd in range(30, 451, 25):
    x = 60 + 0.1 * dd + 28 * math.sin(dd / 38); ridge.append((x, zc(x) + dd))
ra = min(main, key=lambda p: math.dist(p, ridge[0]))
R('SC_ridge', 'shortcut', 3.2, 0.7, [ra] + ridge, '능선 흙길 — 두 골 사이 능선을 따라 꼭대기 밭으로 곧장(급경사·마루 둔턱)', start=J('main'),
  features=[{'type': 'jump', 'near': list(ridge[4]), 'step': 0.5, 'name': '능선 마루 둔턱'}])

# ── 사람 길: 계단(경사 직하) · 층 사이 사람 길 · 해안 산책길 ──
def stairs_from(x0, z0, ytop, maxlen=140):
    pts = [(x0, z0)]; x, z = x0, z0
    for _ in range(int(maxlen / 2)):
        gx, gz = grad_dir(x, z); x, z = x + 2 * gx, z + 2 * gz; pts.append((x, z))
        if h(x, z) >= ytop: break
    return thin(pts, 8)


k = 0
for lo, hi, xs in [(main, tier_pts[0], range(-200, 40, 30)), (tier_pts[0], tier_pts[1], range(-165, 40, 32)), (tier_pts[1], tier_pts[2], range(-160, 40, 34)),
                   (tier_pts[2], tier_pts[3], range(-25, 105, 30)), (main, upper, range(130, 380, 28)), (upper, east2, range(180, 370, 30))]:
    for x in xs:
        a = near_pt(lo, x + V.rnd.uniform(-8, 8)); b = near_pt(hi, a[0])
        R(f'S_{k}', 'stairs', 1.8, 0.0, stairs_from(a[0], a[1], h(*b)), '계단 — 경사 직하(카몰리)', terrainFollows=False); k += 1
for k, (ya, yb, xa, xb) in enumerate([(19, 35, -190, 20), (41, 55, -160, 80), (60, 66, -150, 90), (30, 42, 360, 170), (46, 60, 360, 190)]):
    R(f'A_c{k}', 'alley', 1.8, 0.3, contour(ramp(ya, yb, xa, xb), xa, xb, step=4), '층 사이 사람 길(포지타노 mulattiera)')
R('A_prom', 'alley', 2.5, 0.1, contour(2.4, 95, 380, step=6), '동쪽 해안 산책길(카몰리 해변 길)')
R('A_valley', 'alley', 1.8, 0.3, [(xv2(dd) + 3, zc(xv2(dd)) + dd) for dd in range(30, 280, 25)], '작은 골 사람 길(계단+길)')

# ── 장소 ──
tea = near_pt(quay, 45); top = tier_pts[3][-1]; fld = ridge[-1]; lh = cape[-1]
gar = near_pt(main, -95); mart = near_pt(main, 120)
V.STOPS[:] = [
    {'n': 1, 'id': 'tea', 'name': '찻집', 'pos': [round(tea[0], 1), round(tea[1] + 4, 1)], 'cargo': 'Box', 'why': '부두 동쪽 바다 앞 — 넓은 길로는 항구 길로 돌고, 옛 골목으로는 곧장 내려온다'},
    {'n': 2, 'id': 'top_house', 'name': '꼭대기 집(할머니)', 'pos': [round(top[0], 1), round(top[1], 1)], 'cargo': 'Eggs', 'why': 'Z 길 4층 끝(큰 골 머리 위) — Z 길 vs 골짜기 줄기 돌길 vs 돌길 비탈'},
    {'n': 3, 'id': 'lighthouse', 'name': '등대', 'pos': [round(lh[0], 1), round(lh[1], 1)], 'cargo': 'Glass', 'why': '서쪽 곶 끝 — 곶 등줄기 길 vs 해안 돌길'},
    {'n': 4, 'id': 'hill_field', 'name': '언덕 밭', 'pos': [round(fld[0], 1), round(fld[1], 1)], 'cargo': 'Box', 'why': '능선 꼭대기 계단밭 — Z 길 3층에서 들어가기 vs 능선 흙길'},
]
V.GARAGE.update({'pos': [round(gar[0], 1), round(gar[1], 1)], 'why': '큰길이 만 뒤를 돌며 골 입구를 지나는 자리 — Z 길·줄기 길·옛 골목·곶 쪽이 다 여기서 갈린다'})
V.MART.update({'pos': [round(mart[0], 1), round(mart[1], 1)], 'why': '큰길이 옛 시가지를 지나 동쪽 해안 띠로 들어가는 어귀(카몰리 큰길가 상점)'})
V.FIELDS[:] = [{'id': 'F_hill', 'rect': [round(fld[0]), round(fld[1] + 18), 46, 30], 'bands': 4, 'why': '능선 꼭대기 — 아말피 계단밭'}]
# 언덕 밭은 Z 길에서도 들어가게: 3층 동쪽 끝 근처에서 밭으로 짧은 마을길
t3e = near_pt(tier_pts[2], 95)
R('V_field', 'village', 4.0, 0.3, [t3e, ((t3e[0] + fld[0]) / 2, (t3e[1] + fld[1]) / 2 - 5), fld], '밭 진입로 — Z 길 3층 동쪽 끝에서 능선 밭으로', start=J('V_pasitea'))


# ── 층 6: 구역 — 만 안쪽 심장 + 골짜기 줄기 + 동쪽 해안 띠는 빽빽, 비탈은 위로 성김 ──
def zone(x, z):
    d = z - zc(x)
    if d < 110 and -160 < x < 90: return 'dense'                        # 만 안쪽 심장
    if abs(x - xv1(d)) < 35 and d < 330: return 'dense'                  # 골짜기 따라 올라가는 줄기
    if x > 90 and d < 75: return 'dense'                                 # 카몰리 해안 띠
    if d < 460 and -240 < x < 400: return 'medium'
    return 'sparse'


V.zone = zone
V.ZP['dense'] = ((5, 9), (8, 13), (0.0, 0.2), (0.1, 0.4), 1.0)        # 도모노우라 덮음 0.37 쪽으로: 더 깊고 붙게


def districts():
    return [{'id': 'D_harbor', 'density': 'dense', 'name': '항구 심장(도모노우라)', 'rule': 'd<110, -160<x<90'},
            {'id': 'D_stem', 'density': 'dense', 'name': '골짜기 줄기(아말피)', 'rule': '|x-xv1(d)|<35, d<260'},
            {'id': 'D_strip', 'density': 'dense', 'name': '동쪽 해안 띠(카몰리)', 'rule': 'x>90, d<75'},
            {'id': 'D_slope', 'density': 'medium', 'name': '비탈(포지타노)', 'rule': 'd<330'}]


V.DISTRICTS = districts

if __name__ == '__main__':
    V.main()
