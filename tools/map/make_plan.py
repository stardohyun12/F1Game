"""bayloop 평면도 v2 데이터를 만든다 → docs/map/bayloop-plan.json   (v1 은 docs/map/v1/)

v2 (D-135): 고리 본선은 뼈대로 두고 길망을 더한다 — 마을 차길·지름길(차 가능, 거침)·골목(사람만).
배달지 사이마다 '빠르지만 거친 길' 과 '느리지만 순한 길' 이 둘 다 있게.
손으로 정한 것: 해안선 · 지형 제어점 · 길 제어점(평면) · 배달지 · 길 끝 · 계단 · 밭 · 경계.
코드로 채우는 것: 붙는 길에서 읽은 높이(사이는 길이 비례) · 필지 · 전봇대 · 교차로 목록.
수치는 전부 가설 — 근거는 docs/team/reports/programmer-20261025-1359.md.
"""
import json, math, os, random, sys
sys.path.insert(0, os.path.dirname(__file__))
import planlib as L

SLICE_DY = 30.0   # BuildHilltopSlice 좌표 → 지도 좌표: (x, y + 30, z), 회전 0. 슬라이스 해면 −30 = 지도 해면 0.

def S(p):  # 슬라이스 (x, y, z) → 지도 [x, z, y]
    return [p[0], p[2], round(p[1] + SLICE_DY, 2)]

SLICE_MAIN = [(0, 1, 0), (6, 5.5, 50), (-6, 10.5, 95), (-2, 13.6, 125), (1, 14.05, 150), (12, 12.6, 167), (34, 12.3, 171), (65, 12.0, 173),
              (95, 11.6, 180), (110, 11.0, 183), (121, 10.0, 194), (110, 8.8, 205), (85, 7.4, 211), (48, 5.8, 216)]
SLICE_DETOUR = [(-2, 13.6, 125), (10, 14.1, 140), (38, 14.3, 147), (72, 14.3, 147), (104, 14.1, 141), (130, 13.6, 132)]


def _cum(k, start=None):
    d = [0.0 if start is None else math.dist(start, k[0][:2])]
    for a, b in zip(k, k[1:]):
        d.append(d[-1] + math.dist(a[:2], b[:2]))
    return d


def even(k, y0, y1, start=None, tail=None):
    """높이를 길이에 비례해 고르게(한 굽이에 몰리지 않게). start/tail = 앞뒤에 더 셀 점."""
    d = _cum(k, start)
    tot = d[-1] + (math.dist(k[-1][:2], tail) if tail else 0.0)
    for p, s in zip(k, d):
        p[2] = round(y0 + (y1 - y0) * s / tot, 2)


def arc(c, r, a0, a1, y0, y1, n):
    return [[round(c[0] + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), 2), round(c[1] + r * math.sin(math.radians(a0 + (a1 - a0) * i / n)), 2), round(y0 + (y1 - y0) * i / n, 2)] for i in range(n + 1)]


# ① 교토식 오르막: MART(만 바닥) → 동쪽 골짜기 → 북으로 꺾어 슬라이스 시작점. 평균 9.7%
# v3: 항구 평지 직선(MART → 동쪽 72m, 출발 가속) 뒤에 상점가 오르막. 직선은 ④ 끝 굽이(만 안쪽)와 한 줄로 이어 110m+
K1 = [[-22, -141, 3.0], [4, -141, 4.0], [30, -141, 5.0], [56, -141, 6.0], [76, -141, 7.0], [98, -135, 0], [114, -118, 0], [118, -94, 0],
      [108, -72, 0], [92, -58, 0], [64, -46, 0], [34, -34, 0], [12, -24, 0], [4, -14, 0]]
even(K1[4:], 7.0, 31.0, tail=(0, 0))
K2 = [S(p) for p in SLICE_MAIN]
# 꼬챙이 헤어핀(제어점 3개)은 곡선 보간에서 꼭짓점이 반경 5m 로 조여진다 → 같은 원 위 점을 보탠다(슬라이스 실물은 반경 11.0m)
K2[9:12] = arc((110, 194), 11, -90, 90, 41.0, 38.8, 8)
# ③ 포지타노식 내리막: 문 → 북쪽 비탈 서쪽 → 위 다리(남) → 헤어핀 H1 → 아래 다리(북) → 서쪽으로 굽어 곶길
#    v1 의 H2 헤어핀은 없앴다 — ④ 가 남쪽이 아니라 서쪽 곶으로 나가기 때문(반경 15m 굽이로 바꿈)
#    문을 나와 바다 쪽으로 한 번 부풀었다가(전망) 위 다리로 — 그 안쪽 대각선이 SC_terrace 지름길
# v3: 문을 나서면 바다 쪽으로 곧게 내려가는 직선(바다가 열린다) → 끝에서 왼쪽으로 꺾어 비탈 마을로
K3 = [[26, 223.3, 0], [4, 230.6, 0], [-18, 238, 0], [-31, 236, 0], [-38, 224, 0], [-38, 206, 0], [-37, 185, 0], [-37, 162, 0], [-36, 148, 0],
      *arc((-47, 138), 11, 0, -180, 0, 0, 8),
      [-58, 152, 0], [-60, 172, 0],
      *arc((-82, 200), 22, 0, 90, 0, 0, 6)]   # v3: 반경 15 → 22 — 곶 큰 굽이로 속도를 이어 준다
even(K3, 35.8, 15.5, start=(48, 216))
# ④ 해안 절벽길: 북쪽 곶을 서쪽으로 크게 돌아(바다 오른쪽) 만 안쪽 기슭을 따라 MART
#    v3 (D-136): 곶은 반경 70m 큰 굽이 하나(최고속 유지) → 서쪽 해안 232m 직선(필지 없음, 바다 오른쪽) → 반경 70m 로 만을 돌아 항구 직선
K4 = [*arc((-94.26, 153.94), 70, 92, 194, 0, 0, 7),
      [-150.95, 92.01, 0], [-139.74, 47.03, 0], [-128.54, 2.04, 0], [-117.33, -42.95, 0],
      *arc((-38.2, -71), 70, 194, 270, 0, 0, 5)]
even(K4, 15.5, 3.0, start=tuple(K3[-1][:2]), tail=(-22, -141))
MAIN = K1 + K2 + K3 + K4
B = [0, len(K1), len(K1) + len(K2) - 1, len(K1) + len(K2) + len(K3) - 1, len(MAIN)]   # 구간 경계 제어점 번호

# 만: 북쪽 곶이 서쪽으로 x −180 까지 나가고, 남쪽 곶과 함께 항구(MART)를 안쪽에 감싼다
LAND = [[200, 275], [100, 263], [20, 262], [-50, 260], [-100, 252], [-140, 240], [-170, 222], [-184, 196], [-186, 165],
        [-177, 138], [-169, 100], [-157, 50], [-145, 0], [-132, -50], [-124, -88], [-118, -112], [-98, -136], [-75, -152],
        [-50, -158], [-22, -160], [-50, -176], [-95, -188], [-140, -198], [-172, -212], [-182, -232], [-170, -240], [200, -240]]

CTRL = [  # 지형 제어점 [x, z, 높이] — 길 제어점(terrainFollows)은 자동으로 더해진다
    # 꼭대기 평지·동쪽 언덕(슬라이스 EastHill, 헤어핀 뒤 막이)
    [50, 150, 44.5], [90, 152, 44.5], [30, 128, 44], [120, 140, 45], [60, 125, 44], [165, 150, 62], [175, 60, 58], [170, 230, 58],
    [185, -40, 56], [155, -110, 42], [190, 280, 35], [200, -230, 50],
    # ①과 슬라이스 오르막 사이 등성이, 굽이 안쪽 언덕
    [40, 0, 34], [60, -20, 31], [105, -40, 34], [115, -100, 28], [52, -88, 23], [110, 40, 46], [60, 60, 42], [130, -60, 36],
    # 남쪽 곶 능선(남쪽 경계 가림)
    [20, -200, 32], [80, -195, 38], [140, -175, 45], [-60, -195, 20], [-125, -205, 16], [0, -175, 16], [40, -165, 20], [100, -150, 24],
    # 서쪽 면(능선 → 옛길 → 곶길로 내려가는 비탈)
    [-18, 105, 38], [-45, 60, 22], [-55, 5, 17], [-30, -40, 20], [-62, 100, 17], [-45, -40, 11], [-15, -95, 13], [-40, -72, 10],
    [-15, 170, 35], [-15, 200, 33], [-48, 150, 23], [-72, 150, 17], [-48, 200, 22], [-74, 180, 16],
    # 북쪽 곶
    [-140, 185, 16], [-125, 200, 17], [-160, 205, 12], [-130, 168, 14],
    # 북쪽 비탈(문 아래 → 바다)
    [0, 240, 14], [60, 240, 16], [120, 235, 22], [-40, 248, 8], [80, 228, 25],
    # 만 바닥 낮은 땅
    [-28, -148, 2.2], [-10, -150, 4],
]

SAMP = {}


def snap(rid, x, z):
    """이미 만든 길 위 가장 가까운 점 [x, z, y] — 이어붙는 곳의 높이가 저절로 맞는다."""
    i, _ = L.nearest(SAMP[rid], x, z)
    p = SAMP[rid][i]
    return [round(p[0], 2), round(p[1], 2), round(p[2], 2)]


def fill(knots):
    """[x, z] 는 앞뒤 높이가 정해진 점 사이를 길이 비례로 채운다."""
    k = [list(p) for p in knots]
    d = _cum(k)
    fixed = [i for i, p in enumerate(k) if len(p) == 3]
    for i, p in enumerate(k):
        if len(p) == 3:
            continue
        a = max(f for f in fixed if f < i); after = [f for f in fixed if f > i]
        if not after:   # 막다른 끝: 붙은 점 높이 그대로(평지 마당·전망대)
            p.append(k[a][2]); continue
        b = after[0]
        p.append(round(k[a][2] + (k[b][2] - k[a][2]) * (d[i] - d[a]) / (d[b] - d[a]), 2))
    return k


def R(rid, kind, width, rough, section, why, knots, start=None, end=None, **kw):
    r = {"id": rid, "kind": kind, "width": width, "section": section, "why": why, "knots": fill(knots)}
    if kind in L.CAR:
        r["roughness"] = rough
    if start:
        r["start"] = start
    if end:
        r["end"] = end
    if kind in ("alley", "stairs"):
        r["terrainFollows"] = False
    r.update(kw)
    SAMP[rid] = L.sample(r)
    return r


J = lambda rid: {"join": rid}

ROADS = []
ROADS.append({"id": "main", "kind": "main", "width": 5.5, "closed": True, "roughness": 0.1, "why": "고리 본선(뼈대) — 갈 때 ①②③, 올 때 ④. 포장",
              "knots": MAIN, "sectionKnots": {"1": [B[0], B[1]], "2": [B[1], B[2]], "3": [B[2], B[3]], "4": [B[3], B[4]]}})
SAMP["main"] = L.sample(ROADS[0])
M = lambda x, z: snap("main", x, z)

ROADS += [
    # ── ② 슬라이스 우회길: v1 은 농가 마당에서 끝 → v2 는 헤어핀 입구까지 이어 꼭대기 평지에 블록을 만든다
    R("V_detour", "village", 4.8, 0.25, "2", "슬라이스 우회 — 꼭대기 평지 남쪽을 돌아 헤어핀 입구로(헤어핀 지름길 안 쓰는 길)",
      [S(p) for p in SLICE_DETOUR] + [[142, 141], [140, 158], [126, 173], M(104, 182)], J("main"), J("main")),
]
ROADS += [
    # ── ④ 옛길(곶 목 지름길) — 뒷길이 여기에 붙으므로 먼저 만든다
    R("SC_oldroad", "shortcut", 3.4, 0.55, "4", "옛 우마찻길 — 곶 목에서 마을 안쪽으로 곧게 내려가 항구로(패이고 좁음). 바다 쪽 본선(곶·해안 직선)과 갈래가 다른 풍경",
      [M(-100, 223), [-92, 196], [-84, 160], [-81, 128], [-78, 90], [-74, 50], [-68, 10], [-62, -30], [-58, -68], [-55, -100], M(-60, -137)],
      J("main"), J("main"),
      features=[{"type": "gap", "s": 212, "width": 2.6, "len": 14, "name": "돌담 사이 좁은 목"},
                {"type": "landmark", "s": 6, "name": "옛 우마찻길 돌 이정표"}]),
    # ── ① 서쪽 등성이 지름길(v3 새로): 항구 직선 가운데서 갈라져 만이 내려다보이는 등성이를 곧게 올라 슬라이스 길로. 상점가(본선)는 마을 안
    R("SC_ridge", "shortcut", 3.2, 0.7, "1", "서쪽 등성이 흙길 — 항구에서 만을 내려다보며 곧게 오름(16% 가까움·자갈). 상점가 오르막을 통째로 건너뜀",
      [M(14, -141), [-6, -128], [-24, -114], [-31, -96], [-30, -60], [-29, -24], [-20, 2], M(1, 24)],
      J("main"), J("main"),
      features=[{"type": "jump", "s": 176, "step": 0.6, "name": "등성이 마루 둔턱(짐이 뜬다)"},
                {"type": "landmark", "s": 120, "name": "등성이 풍차"}]),
]
ROADS += [
    # ── ① 교토 비탈
    R("V_kyoto_back", "village", 4.2, 0.2, "1", "상점가 뒷길 — 본선과 나란히, 등고선을 따라 서쪽으로 내려가 등성이 지름길에 닿음(돌포장)",
      [M(116, -96), [90, -99], [62, -102], [40, -100], [15, -101], [-10, -99], snap("SC_ridge", -30, -98)], J("main"), J("SC_ridge")),
]
ROADS += [
    R("A_kyoto_steps", "stairs", 2.0, 0, "1", "니넨자카식 돌계단 골목 — 뒷길에서 상점가 윗굽이로(블록 안쪽 집들 앞)",
      [snap("V_kyoto_back", 38, -100), [42, -70], M(46, -41)], J("V_kyoto_back"), J("main")),
    R("V_pier", "village", 4.5, 0.2, "1", "MART 앞 광장 → 짐 싣는 부두",
      [[-22, -140, 3], [-31, -152, 2.6], [-44, -170, 2.0]], J("main"), {"type": "부두", "label": "짐 부두"}, overWater=True),
    R("V_tunnel", "village", 4.5, 0.3, "1", "동쪽 언덕을 뚫던 옛 터널 — 막힘(동쪽 경계 가림)",
      [M(112, -112), [132, -114], [152, -116]], J("main"), {"type": "터널", "label": "공사 중 터널"}),
    R("A_shrine", "alley", 2.5, 0, "1", "상점가 뒷골목 — 남쪽 능선 작은 전망 쉼터",
      [M(40, -141), [46, -152, 7.5], [58, -163, 11]], J("main"), {"type": "전망대", "label": "쉼터"}),
    # ── ② 꼭대기
    R("A_field", "alley", 2.5, 0, "2", "밭 사이 흙길(사람·경운기) — v2 지름길 SC_field(3초) 를 골목으로 내림",
      [snap("V_detour", 38, 147), M(40, 171.5), [46, 194], M(54, 215)], J("V_detour"), J("main")),
    R("A_plateau", "alley", 2.5, 0, "2", "꼭대기 마을 골목 — 우회길과 본선 사이 블록 안쪽 집 앞",
      [snap("V_detour", 78, 147), M(78, 176)], J("V_detour"), J("main")),
    R("V_farmyard", "village", 4.0, 0.3, "2", "농가 마당(차 돌리는 곳)",
      [snap("V_detour", 130, 132), [133, 121]], J("V_detour"), {"type": "주차장", "label": "농가 마당"}),
    # ── ③ 포지타노
    R("A_terrace", "alley", 2.5, 0, "3", "계단식 밭 둑길(사람만) — v2 지름길 SC_terrace(4초) 를 골목으로 내림",
      [M(30, 222), [12, 203], [-20, 186], M(-37, 177)], J("main"), J("main")),
    R("A_cliffwalk", "alley", 2.2, 0, "3", "북쪽 절벽 위 산책길(사람만) — v2 지름길 SC_cliff(7초) 를 골목으로 내림",
      [M(-38, 200), [-47, 214], [-60, 234], M(-84, 223)], J("main"), J("main")),
    # ── ④ 곶
    R("V_view", "village", 4.5, 0.2, "4", "곶 끝 전망 주차 — 등대 옆, 올 때 쉬어 가는 자리",
      [M(-150, 200), [-160, 211]], J("main"), {"type": "전망대", "label": "곶 전망대"}),
]
ROADS += [
    R("V_x1", "village", 4.0, 0.25, "4", "곶 마을 가로길 — 옛길과 곶길을 등고선 따라 잇는다",
      [snap("SC_oldroad", -83, 150), [-110, 150], [-140, 150], M(-163, 150)], J("SC_oldroad"), J("main")),
    R("V_x2", "village", 4.0, 0.25, "4", "서쪽 면 가로길",
      [snap("SC_oldroad", -75, 60), [-100, 60], [-125, 60], M(-143.5, 60)], J("SC_oldroad"), J("main")),
    R("V_x3", "village", 4.0, 0.25, "4", "서쪽 면 가로길",
      [snap("SC_oldroad", -64.7, -12), [-90, -12], M(-125.9, -12)], J("SC_oldroad"), J("main")),
]
ROADS += [
    R("A_west1", "alley", 2.5, 0, "4", "블록 안쪽 골목 — 가로길 둘을 남북으로 이음",
      [snap("V_x1", -100, 150), [-97, 105], snap("V_x2", -92, 60)], J("V_x1"), J("V_x2")),
    R("A_west2", "alley", 2.5, 0, "4", "블록 안쪽 골목",
      [snap("V_x2", -92, 60), [-88, 25], snap("V_x3", -83, -12)], J("V_x2"), J("V_x3")),
    # ── 계단(사람만, 양 끝 볼라드)
    R("S_hairpin", "stairs", 1.6, 0, "2", "헤어핀 두 다리 사이를 걷는 사람이 질러 깎음(슬라이스 실물)",
      [[97, 186.5, 41.4], [96.5, 203.5, 38.2]], J("main"), J("main")),
    R("S_pos1", "stairs", 1.6, 0, "3", "③ 위 다리 → 아래 다리, 마을 사람 지름길",
      [M(-40, 166), [-48, 167], M(-56.5, 168)], J("main"), J("main")),
    R("S_pos2", "stairs", 1.6, 0, "3", "③ 아래 다리 → 옛길·곶 가로길(십자)",
      [M(-62.5, 150), [-72, 150], snap("SC_oldroad", -83, 150)], J("main"), J("SC_oldroad")),
    R("S_beach", "stairs", 1.8, 0, "3", "절벽 위 길 → 바닷가 작은 부두(포지타노 계단)",
      [snap("A_cliffwalk", -58, 231), [-60, 245], [-61, 255.5, 2.0]], J("A_cliffwalk"), {"type": "부두", "label": "바닷가 부두"}),
]

STOPS = [
    {"n": 1, "id": "kyoto_tea", "name": "찻집", "section": "1", "pos": [2, -99], "timed": True, "why": "v3 R10: 상점가 뒷길 안쪽(본선 40m+) — 항구 비탈길·등성이 지름길·뒷길 셋 중 고른다"},
    {"n": 2, "id": "hilltop_door", "name": "꼭대기 문", "section": "2", "pos": [46, 216], "timed": True, "why": "D-131 받는 사람 — 꼭대기에서 먼저 보인다(슬라이스 HilltopDoor)"},
    {"n": 3, "id": "cape_lane", "name": "곶 마을 가로길 집", "section": "4", "pos": [-104, 150], "timed": True, "why": "v3 R10: 곶 마을 가로길 위(본선 40m+) — 비탈 마을 아래 다리에서 계단/옛길로 들어가거나 곶을 돌아 가로길로"},
    {"n": 4, "id": "cape_cafe", "name": "절벽 카페", "section": "4", "pos": [-97, 224], "timed": True, "why": "곶 목 — 절벽 위 지름길과 헤어핀 길이 만나는 곳, 마지막 배달"},
    {"n": 5, "id": "farm", "name": "농가(선택)", "section": "2", "pos": [128, 136], "timed": True, "optional": True, "why": "우회길 위 — 갈림길 판단을 만드는 선택 배달"},
]
MART = {"id": "mart", "name": "MART", "pos": [-22, -140], "why": "만 바닥 항구 — 출발·복귀"}

FIELDS = [
    {"id": "F_hilltop", "rect": [62, 186, 22, 14], "bands": 3, "why": "슬라이스 실물 — 모서리 길과 아래 길 사이 비탈"},
    {"id": "F_westface", "rect": [-58, 20, 14, 24], "bands": 4, "why": "옛길 위 서쪽 면 비탈의 계단식 밭"},
    {"id": "F_kyoto", "rect": [82, -160, 24, 18], "bands": 3, "why": "상점가 뒤 남쪽 능선 기슭"},
]

BOUNDARY = {"play": [[-184, -212], [160, -212], [160, 262], [-184, 262]],
            "hiddenBy": [{"side": "남", "by": "남쪽 곶 능선 20~45m"}, {"side": "동", "by": "동쪽 언덕 55~62m + 공사 중 터널"},
                         {"side": "서·북", "by": "바다 + 안개(만 입구 너머 먼 섬만 실루엣)"}]}
FOG = {"start": 70, "end": 600, "note": "현 Proto 값(programmer-20261023-1754). 원경은 이 밖"}
FAR = [{"name": "먼 섬", "center": [-640, -60], "radius": 90, "height": 60},
       {"name": "먼 산 고리", "center": [0, 180], "ringRadius": 520, "height": 90, "note": "슬라이스 RingC 380m 산 고리를 지도 중심으로 키움"}]

KEEP = [(st["pos"][0], st["pos"][1], 7) for st in STOPS] + [(MART["pos"][0], MART["pos"][1], 9)]
for f in FIELDS:
    x, z, w, d = f["rect"]; KEEP.append((x + w / 2, z + d / 2, max(w, d) * 0.62))

# 필지 규칙: (길 id, 본선 구간 또는 None, 쪽들, 폭, 깊이, 후퇴, 층, 틈 주기)
PARCEL_RULES = [
    ("main", "1", (1, -1), (5, 7), (8, 10), (0, 0.5), (2, 2), 5),       # 교토 상점가 — 좁은 앞면·깊은 집·후퇴 0
    ("main", "2", (1, -1), (6, 8), (6, 8), (0.5, 2.5), (2, 3), 4),
    ("main", "3", (1, -1), (6, 9), (6, 8), (0.5, 2.5), (2, 4), 4),      # 포지타노 — 층층이
    ("main", "4", (1,), (7, 10), (7, 9), (1, 3), (2, 2), 3),
    ("V_kyoto_back", None, (1, -1), (5, 7), (7, 9), (0, 0.5), (2, 2), 5),
    ("SC_ridge", None, (1,), (7, 9), (7, 9), (1, 2), (1, 1), 2),
    ("A_kyoto_steps", None, (1, -1), (5, 6), (6, 8), (0, 0.3), (2, 2), 5),
    ("V_pier", None, (1, -1), (8, 12), (8, 10), (1, 2), (1, 2), 9),
    ("V_detour", None, (1, -1), (8, 11), (8, 10), (2, 3), (1, 2), 3),
    ("A_plateau", None, (1, -1), (6, 8), (6, 8), (0.3, 1), (1, 2), 4),
    ("A_field", None, (1, -1), (8, 11), (8, 10), (2, 3), (1, 2), 2),
    ("V_tunnel", None, (1, -1), (6, 8), (7, 9), (0.5, 1.5), (2, 2), 4),
    ("A_shrine", None, (1, -1), (5, 6), (6, 7), (0, 0.3), (2, 2), 6),
    ("A_terrace", None, (1, -1), (6, 8), (6, 8), (0.5, 1.5), (2, 3), 4),
    ("A_cliffwalk", None, (-1,), (6, 9), (6, 8), (1, 2), (2, 3), 3),
    ("SC_oldroad", None, (1, -1), (7, 10), (7, 9), (1, 2.5), (1, 2), 4),
    ("V_x1", None, (1, -1), (6, 9), (6, 8), (0.5, 1.5), (2, 2), 5),
    ("V_x2", None, (1, -1), (6, 9), (6, 8), (0.5, 1.5), (2, 2), 5),
    ("V_x3", None, (1, -1), (6, 9), (6, 8), (0.5, 1.5), (2, 2), 5),
    ("A_west1", None, (1, -1), (6, 8), (6, 8), (0.3, 1), (2, 2), 5),
    ("A_west2", None, (1, -1), (6, 8), (6, 8), (0.3, 1), (2, 2), 5),
]


# v3 (D-136) 구간마다 성격 하나 — s 는 본선 호 길이(MART=0), main() 에서 채운다
SECTION_CHAR = [
    {"id": "0", "name": "항구 평지", "from": [-48, -140], "to": [56, -141], "feel": "출발 가속 — 짐 싣고 곧게 밟는다(평지 직선)"},
    {"id": "1", "name": "① 상점가 오르막", "from": [56, -141], "to": [0, 0], "feel": "좁고 정확하게 — 처마 사이 오르막 굽이"},
    {"id": "2", "name": "② 꼭대기 → 해안 내리막", "from": [0, 0], "to": [-18, 238], "feel": "헤어핀을 지나 문에서 바다 쪽으로 밟는다 — 바다가 열린다"},
    {"id": "3", "name": "③ 비탈 마을", "from": [-18, 238], "to": [-82, 222], "feel": "헤어핀·계단 골목 — 기술 구간, 마을 사람 지름길(계단)"},
    {"id": "4", "name": "④ 절벽길 귀환", "from": [-82, 222], "to": [-48, -140], "feel": "곶 큰 굽이 → 해안 232m 직선 → 만 굽이: 순항, 바다 오른쪽"},
]
# 본선 표지(직선 끝·갈림 앞): 이름, 위치 — s 는 main() 에서 계산
MAIN_MARKS = [("크레인(항구 직선 시작)", -40, -141), ("등성이 갈림 앞 종탑", 4, -141), ("문 앞 노란 우체통(내리막 직선 시작)", 44, 218),
              ("옛길 갈림 앞 물탱크", -88, 223), ("등대(곶 끝·해안 직선 시작)", -162, 137), ("어시장 지붕(해안 직선 끝)", -106, -88)]


def main():
    rnd = random.Random(1359)
    plan = {
        "meta": {"name": "bayloop", "version": 3, "decision": "D-134, D-135, D-136", "sectionCharacters": SECTION_CHAR,
                 "rhythmModel": "최고속 85 km/h · 가속 10.7 · 제동 8 m/s² · 굽이 √(5.82·R) (tools/map/planlib.py rhythm)","units": "m", "axes": "x=동, z=북, y=높이(해면 0). 점은 [x, z, y]",
                 "roadKinds": {"main": "고리 본선 5.5m 포장", "village": "마을 차길 4~4.8m", "shortcut": "지름길 3.2~3.8m — 차 가능, 거침(경사 ≤16%·비포장)",
                               "alley": "골목(사람만)", "stairs": "계단(사람만, 볼라드)"},
                 "roughness": "노면·요철 0~1 (0.1 포장 · 0.2~0.3 돌포장 · 0.55~0.75 비포장/자갈). 화물 흔들림 가중치로 쓸 값 — 가설",
                 "slicePlacement": {"scene": "Slice_Hilltop (BuildHilltopSlice.cs)", "offset": [0, SLICE_DY, 0], "rotationY": 0,
                                    "joins": {"in": [0, 0, 31.0], "out": [48, 216, 35.8], "detourEnd": [130, 132, 43.6]},
                                    "replace": ["남쪽 막이(z<-10 에서 +15m)를 지우고 ① 골짜기로", "서쪽 계단식 단(TerraceX)과 z<128 서쪽 막힘을 지우고 지도 서쪽 면으로",
                                                "바둑판 마을(Town) 대신 ③ 길 필지", "본선 끝(문)에서 끊지 말고 ③ 으로 이어감", "먼 산 고리 반경 380 → 지도 FAR",
                                                "우회길 끝(농가 마당)을 헤어핀 입구 (103,181) 까지 이어 고리로", "밭 사이 흙길 SC_field: 우회길 (38,147) → 본선 (40,171) 십자 → 문 동쪽 (54,215)"]},
                 "generatedBy": "tools/map/make_plan.py"},
        "terrain": {"method": "idw", "power": 3, "softening": 6.0, "cliffWidth": 10.0, "seaFloor": -6.0,
                    "beaches": [[-30, -152, 30, 26], [-60, 252, 14, 18]], "controlPoints": CTRL,
                    "note": "길 제어점(terrainFollows)도 지형 제어점에 더한다. ② 범위는 빌드 때 HilltopHeight(+30)로 바꿔 끼운다"},
        "sea": {"level": 0, "landPolygon": LAND},
        "roads": ROADS, "stops": STOPS, "mart": MART, "fields": FIELDS, "boundary": BOUNDARY, "fog": FOG, "farMasses": FAR,
        "sections": [{"id": "1", "name": "① 상점가 오르막", "label": [100, -20]}, {"id": "2", "name": "② 꼭대기 → 바다 내리막", "label": [70, 95]},
                     {"id": "3", "name": "③ 비탈 마을 (헤어핀)", "label": [40, 248]}, {"id": "4", "name": "④ 절벽길 귀환 (순항)", "label": [-168, 60]}],
        "seaLabels": [{"name": "만 (항구)", "pos": [-150, -95]}],
    }
    terr = L.Terrain(plan)
    samp = SAMP
    mp = samp["main"]
    kidx = L.knot_index_s(ROADS[0], mp)
    kidx.append(len(mp) - 1)
    for c in SECTION_CHAR:
        c["s"] = [L.s_at(mp, *c["from"]), L.s_at(mp, *c["to"])]
    ROADS[0]["features"] = [{"type": "landmark", "s": L.s_at(mp, x, z), "name": nm} for nm, x, z in MAIN_MARKS]

    def blocked(x, z, margin=0.6):
        for r in ROADS:
            if L.road_dist(samp[r["id"]], x, z) < r["width"] / 2 + margin:
                return True
        for kx, kz, kr in KEEP:
            if math.hypot(x - kx, z - kz) < kr:
                return True
        return terr.h(x, z) < 1.0 or not L.in_poly(x, z, BOUNDARY["play"])

    parcels = []

    def overlaps(c):
        cx = sum(p[0] for p in c) / 4; cz = sum(p[1] for p in c) / 4
        for q in parcels:
            if math.hypot(cx - q["x"], cz - q["z"]) > 16:
                continue
            qc = L.rect_corners(q)
            if any(L.in_poly(x, z, qc) for x, z in c + [(cx, cz)]) or any(L.in_poly(x, z, c) for x, z in qc + [(q["x"], q["z"])]):
                return True
        return False

    for rid, sec, sides, wr, dr, sr, fr, gap in PARCEL_RULES:
        pts = samp[rid]
        road = next(r for r in ROADS if r["id"] == rid)
        i0, i1 = (kidx[B[int(sec) - 1]], kidx[B[int(sec)]]) if sec else (0, len(pts) - 1)
        for side in sides:
            i, cnt = i0 + 4, 0
            while i < i1 - 4:
                w = rnd.uniform(*wr); d = rnd.uniform(*dr); sb = rnd.uniform(*sr)
                j = min(i1 - 1, i + int(w))
                ta, tb = L.tangent(pts, i), L.tangent(pts, j)
                if ta[0] * tb[0] + ta[1] * tb[1] < math.cos(math.radians(14)):
                    i += 3; continue
                m = (i + j) // 2
                tx, tz = L.tangent(pts, m)
                nx, nz = -tz * side, tx * side          # 길에서 집 쪽
                off = road["width"] / 2 + sb
                fx, fz = pts[m][0] + nx * off, pts[m][1] + nz * off
                p = {"x": round(fx + nx * d / 2, 2), "z": round(fz + nz * d / 2, 2), "w": round(w, 2), "d": round(d, 2),
                     "rot": round(math.degrees(math.atan2(nz, nx)), 1)}
                c = L.rect_corners(p)
                inner = c[2:] + [((c[2][0] + c[3][0]) / 2, (c[2][1] + c[3][1]) / 2), (p["x"], p["z"])]
                if any(blocked(x, z) for x, z in inner) or any(blocked(x, z, margin=-0.05) for x, z in c[:2]) or overlaps(c):
                    i += 2; continue
                hs = [terr.h(x, z) for x, z in c]
                floors = rnd.randint(*fr) + (1 if (sec or road["section"]) == "3" and min(hs) < pts[m][2] - 2 else 0)
                p.update({"id": f"P{len(parcels):03d}", "section": sec or road["section"], "front": {"road": rid, "side": side},
                          "frontMid": [round(fx, 2), round(fz, 2)], "floors": floors, "baseY": round(min(hs), 1),
                          "plinth": round(max(hs) - min(hs), 1)})
                parcels.append(p)
                cnt += 1
                i = j + int(rnd.uniform(0.8, 2.5)) + (4 if cnt % gap == 0 else 0)
    # v3 R3: ③·④ 본선이 해안선 40m 안을 지나는 곳은 '탁 트인 바다 길' — 그 점에서 필지 가장자리 12.5m 안의 집을 뺀다(산 쪽 집은 옛길·가로길에 붙어 남는다)
    seg = [p for i, p in enumerate(mp) if i >= kidx[B[2]] and i % 2 == 0 and L.poly_edge_dist(p[0], p[1], LAND) <= L.SEA_NEAR + 4]
    parcels = [q for q in parcels if min((math.hypot(p[0] - q["x"], p[1] - q["z"]) - 0.5 * max(q["w"], q["d"]) for p in seg), default=99) >= 12.5]
    for k, q in enumerate(parcels):
        q["id"] = f"P{k:03d}"
    plan["parcels"] = parcels

    # 전봇대: 본선 26m 간격, 산 쪽(지형이 높은 쪽) 어깨 밖 1.2m
    poles = []
    acc = 0.0
    for i in range(1, len(mp) - 1):
        acc += math.dist(mp[i][:2], mp[i - 1][:2])
        if acc < 26:
            continue
        tx, tz = L.tangent(mp, i)
        best = None
        for side in (1, -1):
            nx, nz = -tz * side, tx * side
            hh = terr.h(mp[i][0] + nx * 8, mp[i][1] + nz * 8)
            if best is None or hh > best[0]:
                best = (hh, nx, nz)
        off = 5.5 / 2 + 1.2
        x, z = mp[i][0] + best[1] * off, mp[i][1] + best[2] * off
        if any(L.road_dist(samp[r["id"]], x, z) < r["width"] / 2 + 0.3 for r in ROADS):
            continue
        poles.append([round(x, 1), round(z, 1)])
        acc = 0.0
    plan["poles"] = {"road": "main", "spacing": 26, "side": "산 쪽 어깨 밖 1.2m", "points": poles}
    plan["junctions"] = L.junctions(plan, samp)

    os.makedirs(os.path.dirname(L.PLAN), exist_ok=True)
    with open(L.PLAN, "w", encoding="utf-8") as f:
        json.dump(plan, f, ensure_ascii=False, indent=1)
    print(f"wrote {L.PLAN}: roads {len(ROADS)}, parcels {len(parcels)}, poles {len(poles)}, junctions {len(plan['junctions'])}, main samples {len(mp)}")


if __name__ == "__main__":
    main()
