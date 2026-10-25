"""bayloop 평면도 데이터를 만든다 → docs/map/bayloop-plan.json   (v1·v2·v3 는 docs/map/v1~v3/)

v4 (D-138 "코스가 아니라 은근히 자유로운 동네"): 전체 구성(만·언덕·구간)은 v3 그대로 두고 '허락'을 더한다 —
  openGround(길 아닌 차가 갈 수 있는 땅: 광장·부두·해변·밭·공터·풀밭·집 앞 마당), 본선을 이름 있는 큰길로 나눠 폭을 구간마다 다르게,
  끝 모를 길 3갈래(outRoads, 경계 밖 안개 속으로), 배달지 후보 주소 16곳(addressPool), 집 앞면 방향 섞기(facing).

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
    if "crossSection" not in r:
        r["crossSection"] = XS_OUT if r.get("out") else XS_BY_ID.get(rid, XS_BY_KIND.get(kind))
    SAMP[rid] = L.sample(r)
    return r


J = lambda rid: {"join": rid}

ROADS = []
# v4: '본선' 하나가 아니라 이름 있는 큰길 여섯 — 폭이 구간마다 다르다(4.6~7m). 기하는 고리 하나 그대로(빌더·검사·캡처 호환).
# v4 (D-139) 길 단면: 차도 | 경계 | 인도·길가띠 | 앞 공간 | 건물.  edge = curb(경계석 h m) / white_line / gutter / none
#   walk = 인도·길가띠 폭, front = 건물 앞 공간 깊이 범위 [min, max], furniture = 인도에 놓는 것, railing = 바다·벼랑 쪽(낙차 >2m)이면 난간
def XS(edge, walk, front, furniture, h=0.0, railing=True, left=None):
    side = {"edge": edge, "walk": walk, "front": list(front)}
    if edge == "curb":
        side["curbH"] = h or 0.10
    xs = {"left": dict(side, **(left or {})), "right": dict(side), "furniture": furniture, "railing": "drop>2m" if railing else None}
    return xs
XS_BIG = XS("curb", 2.5, (1.0, 3.5), ["StreetLamp", "Bench"], h=0.10)              # 해안도로·큰길: 연석 인도 2~3m + 가로등·벤치·난간
XS_SHOP = XS("curb", 2.0, (1.0, 2.5), ["StreetLamp", "Planter"], h=0.08)           # 상점가: 낮은 연석 인도 + 가게 앞 진열
XS_LANE = XS("white_line", 1.2, (0.8, 3.5), ["Pole", "Planter"])                    # 동네 차길: 흰 선 길가띠 1.2m + 전봇대·화분
XS_OLD = XS("gutter", 0.8, (0.5, 2.5), ["Pole", "Planter"])                         # 아주 좁은 옛 골목 차길: 배수로 + 길가띠 0.8m(예외 하한)
XS_SHARED = XS("none", 0.0, (1.5, 4.0), ["CafeTable", "Stall"], railing=True)      # 광장·부두: 차·사람 함께 쓰는 돌포장
XS_DIRT = XS("none", 1.0, (1.5, 4.0), [], railing=False)                            # 흙길 지름길: 풀 길가 1m
XS_WALK = XS("none", 0.0, (0.3, 1.5), ["Planter"], railing=False)                   # 골목·계단: 사람만(길 자체가 사람 공간)
XS_OUT = XS("none", 0.8, (0, 0), ["StreetLamp"], railing=True)                      # 끝 모를 길: 길가 0.8m + 난간
XS_BY_ID = {"V_pier": XS_SHARED, "V_kyoto_back": XS_OLD, "SC_oldroad": XS_DIRT, "SC_ridge": XS_DIRT}
XS_BY_KIND = {"village": XS_LANE, "shortcut": XS_DIRT, "alley": XS_WALK, "stairs": XS_WALK}

MAIN_NAMES = [
    {"name": "항구길", "from": B[0], "to": 4, "width": 7.0, "crossSection": XS_BIG, "why": "MART 앞 평지 — 짐차·손수레가 같이 쓰는 넓은 길"},
    {"name": "상점가 오르막", "from": 4, "to": B[1], "width": 4.4, "crossSection": XS_SHOP, "why": "처마 사이 좁은 비탈(교토)"},
    {"name": "꼭대기 길", "from": B[1], "to": B[2], "width": 5.5, "crossSection": XS_LANE, "why": "슬라이스 실물 폭"},
    {"name": "비탈길", "from": B[2], "to": B[3], "width": 4.4, "crossSection": XS_LANE, "why": "포지타노 층층이 — 집 사이로 좁게"},
    {"name": "곶길", "from": B[3], "to": B[3] + 8, "width": 6.0, "crossSection": XS_BIG, "why": "곶 큰 굽이 — 전망 쉬는 자리"},
    {"name": "해안도로", "from": B[3] + 8, "to": B[4], "width": 6.8, "crossSection": XS_BIG, "why": "바다 오른쪽 직선 — 버스도 다니는 넓은 길"},
]
ROADS.append({"id": "main", "kind": "main", "width": 5.5, "closed": True, "roughness": 0.1, "why": "고리 큰길(뼈대) — 이름은 구간마다 다르다(widthProfile)",
              "knots": MAIN, "sectionKnots": {"1": [B[0], B[1]], "2": [B[1], B[2]], "3": [B[2], B[3]], "4": [B[3], B[4]]},
              "widthProfile": MAIN_NAMES})
SAMP["main"] = L.sample(ROADS[0])
WMAIN = L.road_widths(ROADS[0], SAMP["main"])
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
    R("V_tunnel", "village", 5.2, 0.3, "1", "동쪽 언덕을 뚫던 옛 터널 — 막힘(동쪽 경계 가림)",
      [M(112, -112), [132, -114], [152, -116]], J("main"), {"type": "터널", "label": "공사 중 터널"}),
    R("A_shrine", "alley", 2.5, 0, "1", "상점가 뒷골목 — 남쪽 능선 작은 전망 쉼터",
      [M(40, -141), [46, -152, 7.5], [58, -163, 11]], J("main"), {"type": "전망대", "label": "쉼터"}),
    # ── ② 꼭대기
    R("A_field", "alley", 3.0, 0, "2", "밭 사이 흙길(사람·경운기) — v2 지름길 SC_field(3초) 를 골목으로 내림",
      [snap("V_detour", 38, 147), M(40, 171.5), [46, 194], M(54, 215)], J("V_detour"), J("main")),
    R("A_plateau", "alley", 3.0, 0, "2", "꼭대기 마을 골목 — 우회길과 본선 사이 블록 안쪽 집 앞",
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
    R("V_x1", "village", 5.2, 0.25, "4", "곶 마을 가로길 — 옛길과 곶길을 등고선 따라 잇는다",
      [snap("SC_oldroad", -83, 150), [-110, 150], [-140, 150], M(-163, 150)], J("SC_oldroad"), J("main")),
    R("V_x2", "village", 5.2, 0.25, "4", "서쪽 면 가로길",
      [snap("SC_oldroad", -75, 60), [-100, 60], [-125, 60], M(-143.5, 60)], J("SC_oldroad"), J("main")),
    R("V_x3", "village", 5.2, 0.25, "4", "서쪽 면 가로길",
      [snap("SC_oldroad", -64.7, -12), [-90, -12], M(-125.9, -12)], J("SC_oldroad"), J("main")),
]
ROADS += [
    R("A_west1", "alley", 3.0, 0, "4", "블록 안쪽 골목 — 가로길 둘을 남북으로 이음",
      [snap("V_x1", -100, 150), [-97, 105], snap("V_x2", -92, 60)], J("V_x1"), J("V_x2")),
    R("A_west2", "alley", 3.0, 0, "4", "블록 안쪽 골목",
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

# ── v4 끝 모를 길 3갈래 (경계 밖 300~600m, 끝은 이유가 있다) ──
ROADS += [
    R("O_mountain", "village", 4.2, 0.35, "2", "옆 마을 가는 산길 — 농가 위에서 동쪽 언덕을 깎아 넘고 고개 너머 안개 속으로",
      [snap("V_detour", 141, 150), [160, 160], [186, 171], [215, 179, 50.0], [255, 184], [300, 179], [350, 166], [400, 152], [455, 128, 46.0]],
      J("V_detour"), {"type": "고개", "label": "옆 마을 3km — 고개 너머 휘어 내려감"}, out=True),
    R("O_breakwater", "village", 4.0, 0.25, "4", "방파제 등대길 — 부두에서 바다로 곧게 뻗어 안개 속 흰 등대까지",
      [M(-95.5, -111.2), [-110, -121], [-128, -132], [-180, -150], [-250, -165], [-330, -178], [-410, -188, 2.6]],
      J("main"), {"type": "등대", "label": "방파제 끝 흰 등대"}, overWater=True, out=True),
    R("O_coast", "village", 4.5, 0.25, "1", "남쪽 해안도로 — 항구길에서 남쪽 곶 기슭 벼랑을 따라 서쪽 끝 다리로",
      [M(24, -141), [12, -160, 7.0], [-5, -176, 9.5], [-40, -186, 10.0], [-90, -199, 10.0], [-135, -209, 10.0], [-168, -222, 10.0]],
      J("main"), None, out=True),
]
ROADS[-1]["end"] = {"join": "O_bridge"}
ROADS += [
    R("O_bridge", "village", 4.5, 0.15, "1", "공사 중 다리 — 바다 건너 옆 섬으로 놓다 만 다리, 안개 속에서 상판이 끊긴다",
      [snap("O_coast", -168, -222), [-200, -238], [-260, -258], [-340, -280], [-420, -296, 10.0]],
      J("O_coast"), {"type": "공사 바리케이드", "label": "공사 중 다리 — 끊긴 상판"}, overWater=True, out=True),
]
OUT_ROADS = [
    {"name": "옆 마을 산길", "roads": ["O_mountain"], "end": "고개 너머 휘어 내려감(안개)", "why": "언덕 너머에 다른 마을이 있다 — 지도 동쪽 끝이 벽이 아니라 고개"},
    {"name": "방파제 등대길", "roads": ["O_breakwater"], "end": "등대(방파제 끝)", "why": "곶은 15m 벼랑이 바다 17m 안이라 곶길 대신 항구 방파제로(가설) — 바다 쪽 끝이 수평선"},
    {"name": "남쪽 해안도로·공사 중 다리", "roads": ["O_coast", "O_bridge"], "end": "공사 중 다리(끊긴 상판)", "why": "남쪽 곶 너머로 길이 이어진다 — 다리가 다 놓이면 옆 섬"},
]

# ── v4 열린 땅: 길이 아니어도 차가 들어갈 수 있는 땅(D-138 '허락'). 빌더가 길과 같은 높이로 녹인다(턱 0).
#    flatten = 주변 길 높이에 맞춘 평면으로 고른다(광장·부두·공터), 아니면 지형 그대로(해변·밭·풀밭)
OPEN_GROUND = [
    {"id": "G_plaza", "kind": "plaza", "name": "항구 광장", "roughness": 0.2, "flatten": True,
     "poly": [[-62, -133], [-48, -126], [-30, -121], [-12, -121], [-2, -128], [-4, -136], [-30, -137], [-50, -139]], "why": "MART 앞 — 차·손수레·사람이 같이 쓰는 돌포장"},
    {"id": "G_quay", "kind": "quay", "name": "부두", "roughness": 0.15, "flatten": True,
     "poly": [[-118, -95], [-113, -112], [-97, -131], [-76, -146], [-54, -151], [-50, -145], [-64, -141], [-80, -134], [-93, -123], [-101, -110], [-107, -95]], "why": "만 안쪽 콘크리트 부두 — 해안도로 바다 쪽을 통째로 열어 둔다"},
    {"id": "G_beach", "kind": "beach", "name": "모래사장", "roughness": 0.6, "flatten": False,
     "poly": [[-50, -146], [-36, -143], [-24, -144], [-14, -150], [-18, -157], [-30, -158], [-48, -156]], "why": "만 바닥 작은 해변 — 모래는 거칠다"},
    {"id": "G_field_hill", "kind": "field", "name": "꼭대기 밭", "roughness": 0.5, "flatten": False,
     "poly": [[56, 182], [92, 184], [96, 200], [80, 206], [56, 208]], "why": "밭 사이로 질러갈 수 있다(흙)"},
    {"id": "G_field_kyoto", "kind": "field", "name": "능선 기슭 밭", "roughness": 0.5, "flatten": False,
     "poly": [[76, -166], [110, -166], [112, -140], [78, -140]], "why": "상점가 뒤 남쪽 밭"},
    {"id": "G_field_west", "kind": "field", "name": "서쪽 면 밭", "roughness": 0.5, "flatten": False,
     "poly": [[-64, 14], [-40, 14], [-40, 48], [-64, 48]], "why": "옛길 위 계단식 밭"},
    {"id": "G_meadow_top", "kind": "meadow", "name": "꼭대기 풀밭", "roughness": 0.45, "flatten": False,
     "poly": [[84, 152], [104, 147], [124, 140], [134, 146], [132, 162], [118, 172], [100, 177], [86, 170]], "why": "꼭대기 평지 동쪽 — 우회길과 헤어핀 사이 풀밭"},
    {"id": "G_meadow_farm", "kind": "meadow", "name": "농가 동쪽 풀밭", "roughness": 0.45, "flatten": False,
     "poly": [[136, 124], [156, 128], [160, 146], [150, 160], [144, 150], [140, 135]], "why": "농가 마당에서 산길로 이어지는 풀밭"},
    {"id": "G_meadow_cape", "kind": "meadow", "name": "곶 풀밭", "roughness": 0.45, "flatten": False,
     "poly": [[-140, 170], [-118, 168], [-110, 190], [-120, 210], [-140, 205]], "why": "곶 큰 굽이 안쪽 — 길을 버리고 질러갈 수 있다"},
    {"id": "G_meadow_east", "kind": "meadow", "name": "동쪽 언덕 풀밭", "roughness": 0.45, "flatten": False,
     "poly": [[70, 40], [120, 30], [150, 60], [150, 110], [110, 125], [75, 110]], "why": "상점가 오르막과 꼭대기 사이 언덕 — 길을 버리고 비탈 풀밭을 질러 오른다"},
    {"id": "G_lot_shop", "kind": "lot", "name": "상점가 뒤 공터", "roughness": 0.3, "flatten": True,
     "poly": [[62, -126], [88, -126], [96, -112], [86, -106], [62, -108]], "why": "상점가 뒤 주차장·공터"},
    {"id": "G_lot_steps", "kind": "plaza", "name": "계단 아래 광장", "roughness": 0.2, "flatten": True,
     "poly": [[28, -108], [50, -108], [50, -96], [44, -93], [28, -94]], "why": "돌계단 골목 아래 — 차는 여기까지, 위로는 걸어서"},
    {"id": "G_lot_view", "kind": "lot", "name": "곶 전망 공터", "roughness": 0.3, "flatten": True,
     "poly": [[-166, 204], [-152, 202], [-150, 214], [-162, 218]], "why": "곶 끝 전망 주차"},
]

# ── v4 (D-140) 구역 강약: 빽빽한 곳이 있으면 성긴 곳도 있다. 앞에서부터 처음 걸리는 구역(마지막 D_village = 나머지 전부).
#    road = 이 구역 동네 차길 폭 범위(큰길 6~8m·흙 지름길·끝 모를 길은 예외), gap = 이웃 집 사이 틈 m, front = 앞 공간(마당) m,
#    floors = 층, types = 건물 type 가중치(building-types.md 의 type 이름 — 프리팹 <type>_<변형> 이 늘면 빌더가 저절로 섞는다)
DISTRICTS = [
    {"id": "D_harbor", "density": "dense", "name": "항구 앞·상점가 옛 중심", "road": [3.5, 4.5], "gap": [0.0, 0.3], "front": [0.3, 1.2], "floors": [2, 3],
     "types": {"shop_house": 5, "harbor_warehouse": 2, "town_house": 2, "terrace_house": 1},
     "poly": [[-80, -185], [60, -185], [122, -132], [128, -88], [122, -50], [100, -16], [56, -10], [8, -10], [-26, -60], [-40, -84], [-80, -112]],
     "why": "어항·상점가 — 처마가 붙은 좁은 길, 골목·돌계단 상점가는 여기에만"},
    {"id": "D_slope", "density": "dense", "name": "비탈 마을(포지타노)", "road": [3.5, 4.5], "gap": [0.0, 0.5], "front": [0.3, 1.5], "floors": [2, 4],
     "types": {"terrace_house": 5, "town_house": 2, "shop_house": 1},
     "poly": [[-92, 158], [-70, 124], [-30, 124], [-24, 160], [42, 196], [52, 232], [0, 252], [-62, 252], [-96, 236]],
     "why": "벼랑에 층층이 붙은 집 — 계단 골목"},
    {"id": "D_hill", "density": "sparse", "name": "꼭대기 밭·언덕", "road": [4.0, 6.0], "gap": [8.0, 16.0], "front": [3.0, 7.0], "floors": [1, 2],
     "types": {"farmhouse": 3, "detached_house": 3, "cliff_villa": 2},
     "poly": [[20, 30], [160, 20], [160, 262], [52, 262], [52, 232], [42, 196], [20, 150]],
     "why": "띄엄띄엄 농가 — 큰 마당·나무·빈터·밭"},
    {"id": "D_cape", "density": "sparse", "name": "곶·해안 빌라", "road": [4.0, 6.0], "gap": [8.0, 16.0], "front": [3.0, 7.0], "floors": [1, 2],
     "types": {"cliff_villa": 4, "detached_house": 2, "farmhouse": 1},
     "poly": [[-184, 90], [-120, 90], [-110, 160], [-96, 236], [-92, 262], [-184, 262]],
     "why": "곶 위 빌라 몇 채 — 바다 쪽이 비어 있다"},
    {"id": "D_south", "density": "sparse", "name": "남쪽 곶 기슭", "road": [4.0, 6.0], "gap": [8.0, 16.0], "front": [3.0, 7.0], "floors": [1, 2],
     "types": {"cliff_villa": 2, "detached_house": 2, "farmhouse": 1},
     "poly": [[-184, -212], [160, -212], [160, -170], [120, -170], [60, -185], [-80, -185], [-184, -160]],
     "why": "해안도로 벼랑 — 집은 드문드문"},
    {"id": "D_village", "density": "medium", "name": "마을 차길", "road": [5.0, 6.0], "gap": [1.5, 4.0], "front": [1.0, 3.0], "floors": [2, 3],
     "types": {"town_house": 4, "detached_house": 2, "terrace_house": 1, "shop_house": 1, "cliff_villa": 1},
     "poly": [[-184, -212], [160, -212], [160, 262], [-184, 262]],
     "why": "나머지 — 집 사이 틈·담장·작은 마당"},
]
DENSITY_SHARE = {"dense": (0.20, 0.30), "medium": (0.33, 0.47), "sparse": (0.28, 0.42)}   # 육지 면적비 목표(~25/40/35%)


def district_at(x, z):
    return next(d for d in DISTRICTS if L.in_poly(x, z, d["poly"]) or d["id"] == "D_village")


# ── v4 배달지 후보: 판마다 이 중 4~5 곳을 뽑는다(가설). 주소 + 가까운 랜드마크. park = 차를 대는 곳(계단 위 집처럼 차가 못 가면)
ADDRESS_POOL = [
    {"id": "a_beach", "name": "모래사장 집", "pos": [-38, -149], "landmark": "MART 앞 해변"},
    {"id": "a_pier_end", "name": "짐 부두 끝", "pos": [-43, -168], "landmark": "크레인"},
    {"id": "a_breakwater", "name": "방파제 입구 창고", "pos": [-106, -122], "landmark": "어시장 지붕"},
    {"id": "a_tea", "name": "찻집", "pos": [2, -99], "landmark": "상점가 뒷길"},
    {"id": "a_steps_top", "name": "계단 위 집", "pos": [43, -72], "park": [38, -103], "landmark": "니넨자카 돌계단"},
    {"id": "a_shrine", "name": "쉼터 옆 집", "pos": [56, -160], "landmark": "능선 쉼터"},
    {"id": "a_garage", "name": "터널 앞 정비소", "pos": [140, -112], "landmark": "공사 중 터널"},
    {"id": "a_lot", "name": "공터 뒤 집", "pos": [75, -115], "landmark": "상점가 뒤 주차장"},
    {"id": "a_farm", "name": "농가", "pos": [128, 136], "landmark": "농가 마당"},
    {"id": "a_meadow", "name": "풀밭 오두막", "pos": [110, 160], "landmark": "꼭대기 풀밭"},
    {"id": "a_door", "name": "꼭대기 문", "pos": [46, 216], "landmark": "노란 우체통"},
    {"id": "a_cape_lane", "name": "곶 가로길 집", "pos": [-104, 150], "landmark": "곶 마을 가로길"},
    {"id": "a_cafe", "name": "절벽 카페", "pos": [-97, 224], "landmark": "물탱크"},
    {"id": "a_west_farm", "name": "서쪽 밭 농가", "pos": [-46, 32], "landmark": "서쪽 면 계단밭"},
    {"id": "a_mountain", "name": "산길 입구 집", "pos": [150, 152], "landmark": "옆 마을 이정표"},
    {"id": "a_cliff", "name": "해안도로 벼랑 집", "pos": [-60, -192], "landmark": "남쪽 해안도로"},
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

KEEP = [(st["pos"][0], st["pos"][1], 7) for st in STOPS] + [(MART["pos"][0], MART["pos"][1], 9)] + [(a["pos"][0], a["pos"][1], 3) for a in ADDRESS_POOL]
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
        "meta": {"name": "bayloop", "version": 4, "decision": "D-134, D-135, D-136, D-138, D-139, D-140", "districts": "구역 강약(D-140): 앞에서부터 처음 걸리는 폴리곤. dense 붙은 집·좁은 길 / medium 틈 1.5~4m·작은 마당 / sparse 8m+ 띄엄띄엄·큰 마당·나무. 필지에 district·density 기록", "sectionCharacters": SECTION_CHAR,
                 "crossSection": "길 단면(D-139): 차도 | edge(curb 경계석 8~12cm·white_line·gutter·none) | walk 인도·길가띠 m | front 건물 앞 공간 m [min,max] | 건물. "
                                 "left = 진행 방향 왼쪽. 'section' 은 지도 구간 번호(v1부터)라 단면은 crossSection 키로 둔다. 필지 후퇴 = 차도 가장자리 + walk + front",
                 "openGround": "길 아닌 차가 갈 수 있는 땅(D-138). flatten=True 는 주변 길 높이 평면, 아니면 지형 그대로. 경계 턱 0",
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
        "roads": ROADS, "districts": DISTRICTS, "openGround": OPEN_GROUND, "outRoads": OUT_ROADS, "addressPool": ADDRESS_POOL, "stops": STOPS, "mart": MART, "fields": FIELDS, "boundary": BOUNDARY, "fog": FOG, "farMasses": FAR,
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

    # v4: 점마다 폭·단면. 건물은 차도 + 인도·길가띠 밖에만(D-139), 열린 땅 위엔 안 짓는다(D-138)
    WID = {r["id"]: L.road_widths(r, samp[r["id"]]) for r in ROADS}
    XSS = {r["id"]: L.road_xsecs(r, samp[r["id"]]) for r in ROADS}

    def reach(rid, i):
        """길 중심 → 사람 층 바깥(차도 반 + 양쪽 중 넓은 인도)"""
        xs = XSS[rid][i]
        return WID[rid][i] / 2 + (max(xs["left"]["walk"], xs["right"]["walk"]) if xs else 0)

    def blocked(x, z, margin=0.6, own=None, own_margin=None):
        for r in ROADS:
            i, dd = L.nearest(samp[r["id"]], x, z)
            if dd < reach(r["id"], i) + (own_margin if r["id"] == own and own_margin is not None else margin):
                return True
        for kx, kz, kr in KEEP:
            if math.hypot(x - kx, z - kz) < kr:
                return True
        if any(L.in_poly(x, z, g["poly"]) for g in OPEN_GROUND):
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
                j = min(i1 - 1, i + math.ceil(w))
                ta, tb = L.tangent(pts, i), L.tangent(pts, j)
                if ta[0] * tb[0] + ta[1] * tb[1] < math.cos(math.radians(14)):
                    i += 3; continue
                m = (i + j) // 2
                tx, tz = L.tangent(pts, m)
                nx, nz = -tz * side, tx * side          # 길에서 집 쪽
                # v4 (D-139) 후퇴 = 차도 가장자리 + 인도·길가띠 + 앞 공간
                xs = L.xs_side(XSS[rid][m], side)
                # v4 (D-140) 구역이 앞 공간·틈·층을 정한다: 밀집 = 거의 붙음, 성김 = 큰 마당
                dist = district_at(pts[m][0] + nx * (WID[rid][m] / 2 + 3), pts[m][1] + nz * (WID[rid][m] / 2 + 3))
                if road["kind"] in ("alley", "stairs") and dist["density"] != "dense":
                    i += 3; continue                     # 골목·계단 상점가는 밀집 구역에만
                if dist["density"] == "sparse" and rnd.random() < 0.35:
                    i += int(rnd.uniform(6, 14)); continue   # 성김: 빈터·밭으로 남기는 자리
                lo, hi = xs["front"]
                dlo, dhi = dist["front"]
                front = rnd.uniform(max(lo, dlo) if dist["density"] == "sparse" else lo, min(hi, dhi) if dist["density"] == "dense" else max(hi, dhi) if dist["density"] == "sparse" else hi)
                if dist["density"] == "sparse":
                    w = rnd.uniform(max(w, 7), max(w, 7) + 2.5)
                off = WID[rid][m] / 2 + xs["walk"] + front
                fx, fz = pts[m][0] + nx * off, pts[m][1] + nz * off
                p = {"x": round(fx + nx * d / 2, 2), "z": round(fz + nz * d / 2, 2), "w": round(w, 2), "d": round(d, 2),
                     "rot": round(math.degrees(math.atan2(nz, nx)), 1)}
                c = L.rect_corners(p)
                inner = c[2:] + [((c[2][0] + c[3][0]) / 2, (c[2][1] + c[3][1]) / 2), (p["x"], p["z"])]
                # 앞 모서리는 자기 길의 사람 층까지는 허용(앞 공간 0 이면 인도에 바로 붙음), 다른 길에는 0.3m 떨어짐
                if any(blocked(x, z) for x, z in inner) or any(blocked(x, z, margin=0.3, own=rid, own_margin=-0.05 - front) for x, z in c[:2]) or overlaps(c):
                    i += 2; continue
                hs = [terr.h(x, z) for x, z in c]
                floors = rnd.randint(*dist["floors"]) + (1 if (sec or road["section"]) == "3" and min(hs) < pts[m][2] - 2 else 0)
                floors = min(floors, 4)
                p.update({"id": f"P{len(parcels):03d}", "section": sec or road["section"], "district": dist["id"], "density": dist["density"], "front": {"road": rid, "side": side},
                          "frontMid": [round(fx, 2), round(fz, 2)], "edge": xs["edge"], "walk": xs["walk"], "frontSpace": round(front, 2),
                          "floors": floors, "baseY": round(min(hs), 1),
                          "plinth": round(max(hs) - min(hs), 1)})
                parcels.append(p)
                cnt += 1
                g = rnd.uniform(*dist["gap"])
                if dist["density"] == "medium" and cnt % gap == 0:
                    g += 4                                # 가끔 더 넓은 틈(담장 안 마당)
                p["gapAfter"] = round(g, 1)
                i = j + int(round(g))
    # v3 R3: ③·④ 본선이 해안선 40m 안을 지나는 곳은 '탁 트인 바다 길' — 그 점에서 필지 가장자리 12.5m 안의 집을 뺀다(산 쪽 집은 옛길·가로길에 붙어 남는다)
    seg = [p for i, p in enumerate(mp) if i >= kidx[B[2]] and i % 2 == 0 and L.poly_edge_dist(p[0], p[1], LAND) <= L.SEA_NEAR + 4]
    parcels = [q for q in parcels if min((math.hypot(p[0] - q["x"], p[1] - q["z"]) - 0.5 * max(q["w"], q["d"]) for p in seg), default=99) >= 12.5]
    # v4 (D-138) 앞면 방향 섞기: 뒤가 열린 땅·다른 길이면 그쪽을 보게(뒤), 앞뒤 길이가 비슷한 집은 일부 옆으로
    frnd = random.Random(1384)
    for k, q in enumerate(parcels):
        q["id"] = f"P{k:03d}"
        a = math.radians(q["rot"]); ix, iz = math.cos(a), math.sin(a)
        bx, bz = q["x"] + ix * (q["d"] / 2 + 3), q["z"] + iz * (q["d"] / 2 + 3)
        back_open = any(L.in_poly(bx, bz, g["poly"]) for g in OPEN_GROUND) or any(
            r["id"] != q["front"]["road"] and L.road_dist(samp[r["id"]], bx, bz) < r["width"] / 2 + 2.5 for r in ROADS)
        if back_open and frnd.random() < 0.6:
            q["facing"] = "back"
        elif abs(q["w"] - q["d"]) <= 2.5 and frnd.random() < 0.2:
            q["facing"] = "side"
        else:
            q["facing"] = "front"
    plan["parcels"] = parcels

    # v4 (D-140) 나무: 성김 구역 빈터·마당에 듬성듬성, 보통 구역 마당에 가끔. 밀집엔 없음
    trees, trnd = [], random.Random(1402)
    for gx in range(-184, 161, 7):
        for gz in range(-212, 263, 7):
            x, z = gx + trnd.uniform(-3, 3), gz + trnd.uniform(-3, 3)
            dd = district_at(x, z)["density"]
            if trnd.random() > {"sparse": 0.30, "medium": 0.07}.get(dd, 0) or blocked(x, z, margin=1.5):
                continue
            if any(math.hypot(x - q["x"], z - q["z"]) < max(q["w"], q["d"]) * 0.75 + 1.0 for q in parcels):
                continue
            trees.append([round(x, 1), round(z, 1), "Tree" if trnd.random() < 0.7 else "Bush"])
    plan["trees"] = {"note": "D-140 성김 구역 빈터·마당 나무(가설: 7m 격자 30%, 보통 7%, 밀집 0)", "points": trees}

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
        # v4: 그 자리 차도 폭 + 인도 바깥쪽 1/3 지점(인도 안). 큰길은 가로등, 동네 길은 전봇대
        side = 1 if (best[1], best[2]) == (-tz, tx) else -1
        xs = L.xs_side(XSS["main"][i], side)
        off = WMAIN[i] / 2 + max(0.5, xs["walk"] * 0.75)
        x, z = mp[i][0] + best[1] * off, mp[i][1] + best[2] * off
        if any(L.road_dist(samp[r["id"]], x, z) < r["width"] / 2 + 0.3 for r in ROADS if r["id"] != "main"):
            continue
        kind = next((f for f in XSS["main"][i]["furniture"] if f in ("StreetLamp", "Pole")), "Pole")
        poles.append([round(x, 1), round(z, 1), kind])
        acc = 0.0
    plan["poles"] = {"road": "main", "spacing": 26, "side": "산 쪽 인도 안(바깥 1/4). 점 [x, z, StreetLamp|Pole]", "points": poles}
    plan["junctions"] = L.junctions(plan, samp)

    os.makedirs(os.path.dirname(L.PLAN), exist_ok=True)
    with open(L.PLAN, "w", encoding="utf-8") as f:
        json.dump(plan, f, ensure_ascii=False, indent=1)
    print(f"wrote {L.PLAN}: roads {len(ROADS)}, parcels {len(parcels)}, poles {len(poles)}, junctions {len(plan['junctions'])}, main samples {len(mp)}")


if __name__ == "__main__":
    main()
