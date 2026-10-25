"""bayloop-plan.json → 평면도 PNG 2장.
  docs/map/bayloop-plan-v2.png           전체 (북쪽 위, 2px/m)
  docs/map/bayloop-plan-v2-sections.png  ①~④ 확대 4칸 + 배달 쌍 대안 경로 2칸
사용: python -X utf8 tools/map/render_plan.py [plan.json]
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import planlib as L
from PIL import Image, ImageDraw, ImageFont

FONT = "C:/Windows/Fonts/malgun.ttf"
FONTB = "C:/Windows/Fonts/malgunbd.ttf"
OUT = os.path.join(L.ROOT, "docs", "map")
EXT = (-205, -235, 200, 290)   # x0, z0, x1, z1
G = 2.0                        # 지형 격자 m

ROAD_STYLE = {"village": ((150, 90, 20), (245, 170, 70)), "shortcut": ((80, 30, 110), (170, 95, 225)), "main": ((150, 40, 35), (225, 80, 60))}
SEC_FILL = {"1": (236, 200, 165), "2": (226, 220, 210), "3": (250, 236, 205), "4": (215, 222, 228)}
# v4: 구역(D-140) · 열린 땅(D-138) · 필지는 구역 밀도색
DIST_FILL = {"dense": (200, 60, 50, 46), "medium": (0, 0, 0, 0), "sparse": (60, 160, 60, 40)}
DIST_LINE = {"dense": (170, 40, 30), "medium": (120, 120, 120), "sparse": (40, 120, 40)}
PARCEL_FILL = {"dense": (196, 110, 90), "medium": (232, 196, 150), "sparse": (250, 238, 205)}
OG_FILL = {"plaza": (205, 195, 180, 210), "quay": (185, 185, 190, 210), "lot": (200, 200, 195, 190), "beach": (240, 220, 160, 210),
           "field": (175, 200, 110, 170), "meadow": (150, 205, 120, 150)}
SEC_COL = {"1": (190, 90, 40), "2": (120, 90, 160), "3": (210, 140, 30), "4": (40, 110, 160)}


def font(sz, bold=False):
    return ImageFont.truetype(FONTB if bold else FONT, sz)


class Grid:
    def __init__(self, terr):
        x0, z0, x1, z1 = EXT
        self.nx = int((x1 - x0) / G) + 1
        self.nz = int((z1 - z0) / G) + 1
        self.v = [[terr.h(x0 + i * G, z0 + j * G) for i in range(self.nx)] for j in range(self.nz)]

    def h(self, x, z):
        fx = (x - EXT[0]) / G; fz = (z - EXT[1]) / G
        i = max(0, min(self.nx - 2, int(fx))); j = max(0, min(self.nz - 2, int(fz)))
        tx = min(1, max(0, fx - i)); tz = min(1, max(0, fz - j))
        a = self.v[j][i] * (1 - tx) + self.v[j][i + 1] * tx
        b = self.v[j + 1][i] * (1 - tx) + self.v[j + 1][i + 1] * tx
        return a * (1 - tz) + b * tz


def land_color(h):
    stops = [(0, (214, 222, 180)), (15, (205, 214, 165)), (30, (222, 208, 160)), (45, (210, 186, 146)), (65, (186, 162, 132))]
    for (h0, c0), (h1, c1) in zip(stops, stops[1:]):
        if h <= h1:
            t = max(0.0, (h - h0) / (h1 - h0))
            return tuple(int(c0[k] + (c1[k] - c0[k]) * t) for k in range(3))
    return stops[-1][1]


def render(plan, grid, samp, ext, s, title=None, legend=True, labels=True, sec_labels=True, routes=(), rhythm=None):
    x0, z0, x1, z1 = ext
    M = 36
    W = int((x1 - x0) * s) + 2 * M + (300 if legend else 0)
    H = int((z1 - z0) * s) + 2 * M + (40 if title else 0)
    top = M + (40 if title else 0)
    img = Image.new("RGB", (W, H), (250, 250, 247))
    px = img.load()
    mw, mh = int((x1 - x0) * s), int((z1 - z0) * s)
    # 지형: 높이색 + 음영 + 5m 등고선(25m 굵게)
    hs = [[grid.h(x0 + (i + .5) / s, z1 - (j + .5) / s) for i in range(mw + 1)] for j in range(mh + 1)]
    for j in range(mh):
        row, nrow = hs[j], hs[j + 1]
        for i in range(mw):
            h = row[i]
            if h < 0:
                c = (150, 190, 218)
            else:
                c = land_color(h)
                gx = (row[i + 1] - h) * s; gz = (nrow[i] - h) * s
                sh = max(-0.25, min(0.25, (-gx * 0.7 + gz * 0.7) * 0.08))   # 북서쪽 빛
                c = tuple(max(0, min(255, int(v * (1 + sh)))) for v in c)
                a, b, d = math.floor(h / 5), math.floor(row[i + 1] / 5), math.floor(nrow[i] / 5)
                if a != b or a != d:
                    k = 0.62 if (max(a, b, d) % 5 == 0) else 0.84
                    c = tuple(int(v * k) for v in c)
            if h >= 0 and (row[i + 1] < 0 or nrow[i] < 0):
                c = (70, 100, 130)
            px[M + i, top + j] = c

    def P(x, z):
        return (M + (x - x0) * s, top + (z1 - z) * s)

    # v4 (D-140) 구역 강약 반투명 + (D-138) 열린 땅
    ov = Image.new("RGBA", img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(ov)
    for d in reversed(plan.get("districts", [])[:-1]):
        od.polygon([P(*q) for q in d["poly"]], fill=DIST_FILL[d["density"]])
    for g in plan.get("openGround", []):
        od.polygon([P(*q) for q in g["poly"]], fill=OG_FILL.get(g["kind"], (200, 200, 200, 150)), outline=(90, 90, 90, 200))
    img = Image.alpha_composite(img.convert("RGBA"), ov).convert("RGB")
    dr = ImageDraw.Draw(img)
    for d in plan.get("districts", [])[:-1]:
        pts_ = [P(*q) for q in d["poly"]]
        dr.line(pts_ + pts_[:1], fill=DIST_LINE[d["density"]], width=2)

    def inside(x, z, pad=0):
        return x0 - pad <= x <= x1 + pad and z0 - pad <= z <= z1 + pad

    def text(xy, t, sz, col=(30, 30, 30), bold=False, anchor="mm", stroke=3):
        dr.text(xy, t, font=font(sz, bold), fill=col, anchor=anchor, stroke_width=stroke, stroke_fill=(255, 255, 255))

    terr_samp = grid
    # 필지
    for p in plan["parcels"]:
        if not inside(p["x"], p["z"], 10):
            continue
        c = PARCEL_FILL.get(p.get("density"), SEC_FILL.get(p["section"], (230, 230, 230)))
        c = tuple(int(v * (1 - 0.07 * (p["floors"] - 1))) for v in c)
        dr.polygon([P(*q) for q in L.rect_corners(p)], fill=c, outline=(90, 75, 60))
    # 나무(D-140 성김 구역 빈터·마당)
    for t in plan.get("trees", {}).get("points", []):
        if inside(t[0], t[1]):
            c = P(t[0], t[1]); rr = max(2, s * (1.6 if t[2] == "Tree" else 1.0))
            dr.ellipse([c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr], fill=(60, 130, 60), outline=(30, 80, 30))
    # 밭
    for f in plan["fields"]:
        x, z, w, d = f["rect"]
        dr.rectangle([P(x, z + d), P(x + w, z)], fill=(160, 195, 110), outline=(80, 110, 50), width=2)
        for b in range(1, f["bands"]):
            zz = z + d * b / f["bands"]
            dr.line([P(x, zz), P(x + w, zz)], fill=(90, 120, 55), width=2)
    # 길 — 깎은 면/메운 면 표시 먼저
    car = [r for r in plan["roads"] if r["kind"] in ROAD_STYLE]
    for r in car:
        pts = samp[r["id"]]; hw = r["width"] / 2
        for i in range(0, len(pts), 2):
            if not inside(pts[i][0], pts[i][1], 2):
                continue
            tx, tz = L.tangent(pts, i)
            for side in (1, -1):
                nx, nz = -tz * side, tx * side
                hh = terr_samp.h(pts[i][0] + nx * (hw + 4), pts[i][1] + nz * (hw + 4)) - pts[i][2]
                ax, az = pts[i][0] + nx * (hw + 0.8), pts[i][1] + nz * (hw + 0.8)
                if hh > 1.8:      # 산 쪽 깎은 면 = 갈색 빗금
                    dr.line([P(ax, az), P(ax + nx * 3.2, az + nz * 3.2)], fill=(140, 85, 45), width=max(1, int(s * 0.7)))
                elif hh < -2.2:   # 바다/골 쪽 메운 면 = 난간(남색 선)
                    if i + 2 < len(pts):
                        bx, bz = pts[i + 2][0] + nx * (hw + 1.2), pts[i + 2][1] + nz * (hw + 1.2)
                        dr.line([P(ax + nx * .4, az + nz * .4), P(bx, bz)], fill=(40, 60, 120), width=max(2, int(s * 0.6)))
    # v4 (D-139) 사람의 층: 인도·길가띠 띠(연석 = 회색, 흰 선·배수로 = 옅은 회색)
    for r in car:
        pts = samp[r["id"]]; wl = L.road_widths(r, pts); xl = L.road_xsecs(r, pts)
        for i in range(0, len(pts) - 1, 1):
            xs = xl[i]
            if not xs or not inside(pts[i][0], pts[i][1], 4):
                continue
            for side in (1, -1):
                sd = L.xs_side(xs, side)
                if sd["walk"] <= 0:
                    continue
                tx, tz = L.tangent(pts, i); nx, nz = -tz * side, tx * side
                o = wl[i] / 2 + sd["walk"] / 2
                col = (165, 165, 160) if sd["edge"] == "curb" else (210, 208, 200)
                dr.line([P(pts[i][0] + nx * o, pts[i][1] + nz * o), P(pts[i + 1][0] + nx * o, pts[i + 1][1] + nz * o)], fill=col, width=max(1, int(sd["walk"] * s)))
    for r in sorted(car, key=lambda r: list(ROAD_STYLE).index(r["kind"])):
        pts = samp[r["id"]]; out, fill = ROAD_STYLE[r["kind"]]
        wl = L.road_widths(r, pts)
        keep = [i for i, p in enumerate(pts) if inside(p[0], p[1], pad=4)]   # 지도 틀 밖(끝 모를 길 원경)은 그리지 않는다
        pts = pts[:keep[-1] + 1] if keep else []
        step = 6
        for i in range(0, len(pts) - 1, step):
            w = max(3, int(wl[i] * s))
            xy = [P(p[0], p[1]) for p in pts[i:i + step + 1]]
            dr.line(xy, fill=out, width=w + 4, joint="curve")
        for i in range(0, len(pts) - 1, step):
            w = max(3, int(wl[i] * s))
            xy = [P(p[0], p[1]) for p in pts[i:i + step + 1]]
            dr.line(xy, fill=fill, width=w, joint="curve")
    for r in plan["roads"]:
        if r["kind"] == "alley":
            pts = samp[r["id"]]
            for a, b in zip(pts[::3], pts[2::3]):
                dr.line([P(a[0], a[1]), P(b[0], b[1])], fill=(110, 70, 40), width=max(2, int(r["width"] * s * .6)))
        if r["kind"] == "stairs":
            pts = samp[r["id"]]
            dr.line([P(p[0], p[1]) for p in pts], fill=(60, 60, 60), width=max(2, int(s * 0.5)))
            for i in range(0, len(pts), 2):
                tx, tz = L.tangent(pts, i); nx, nz = -tz, tx
                dr.line([P(pts[i][0] - nx * 1.2, pts[i][1] - nz * 1.2), P(pts[i][0] + nx * 1.2, pts[i][1] + nz * 1.2)], fill=(30, 30, 30), width=max(1, int(s * .4)))
            for q in (pts[0], pts[-1]):   # v4: 볼라드 대신 계단 입구 화분(초록 네모)
                c = P(q[0], q[1]); rr = max(2, s * .8)
                dr.rectangle([c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr], fill=(70, 150, 70), outline=(0, 0, 0))
    # 전봇대·가로등 (점 [x, z, kind])
    pl = plan["poles"]["points"]
    for a, b in zip(pl, pl[1:]):
        if math.dist(a[:2], b[:2]) < 40 and a[2:] == ["Pole"] and b[2:] == ["Pole"]:
            dr.line([P(*a[:2]), P(*b[:2])], fill=(70, 70, 70), width=1)
    for q in pl:
        c = P(*q[:2]); rr = max(2, s * .8)
        dr.ellipse([c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr], fill=(250, 200, 40) if q[2:] == ["StreetLamp"] else (20, 20, 20), outline=(0, 0, 0))
    # 구간 경계 눈금
    mp = samp["main"]; main = next(r for r in plan["roads"] if r["id"] == "main")
    kidx = L.knot_index_s(main, mp) + [len(mp) - 1]
    for sid, (a, b) in main["sectionKnots"].items():
        q = mp[kidx[a]]
        tx, tz = L.tangent(mp, kidx[a]); nx, nz = -tz, tx
        dr.line([P(q[0] - nx * 7, q[1] - nz * 7), P(q[0] + nx * 7, q[1] + nz * 7)], fill=SEC_COL[sid], width=max(3, int(s * 1.2)))
    # 경계·슬라이스 범위
    bp = plan["boundary"]["play"]
    for a, b in zip(bp, bp[1:] + bp[:1]):
        n = int(math.dist(a, b) / 6)
        for k in range(0, n, 2):
            u0, u1 = k / n, (k + 1) / n
            dr.line([P(a[0] + (b[0] - a[0]) * u0, a[1] + (b[1] - a[1]) * u0), P(a[0] + (b[0] - a[0]) * u1, a[1] + (b[1] - a[1]) * u1)], fill=(150, 60, 160), width=2)
    sx0, sz0, sx1, sz1 = -144, -32, 144, 240
    for a, b in [((sx0, sz0), (sx1, sz0)), ((sx1, sz0), (sx1, sz1)), ((sx1, sz1), (sx0, sz1)), ((sx0, sz1), (sx0, sz0))]:
        n = int(math.dist(a, b) / 4)
        for k in range(0, n, 3):
            u0, u1 = k / n, (k + 1) / n
            dr.line([P(a[0] + (b[0] - a[0]) * u0, a[1] + (b[1] - a[1]) * u0), P(a[0] + (b[0] - a[0]) * u1, a[1] + (b[1] - a[1]) * u1)], fill=(110, 90, 160), width=1)
    # 길 끝 기호
    for r in plan["roads"]:
        e = r.get("end", {})
        if not e or "type" not in e or not e["type"]:
            continue
        q = samp[r["id"]][-1]
        if not inside(q[0], q[1]):
            continue
        c = P(q[0], q[1]); k = max(7, s * 3)
        t = e["type"]
        if t == "주차장":
            dr.rectangle([c[0] - k, c[1] - k, c[0] + k, c[1] + k], fill=(40, 90, 200), outline=(0, 0, 0))
            dr.text(c, "P", font=font(int(k * 1.5), True), fill="white", anchor="mm")
        elif t == "전망대":
            dr.polygon([(c[0], c[1] - k * 1.2), (c[0] - k, c[1] + k * .8), (c[0] + k, c[1] + k * .8)], fill=(250, 210, 40), outline=(0, 0, 0))
        elif t == "터널":
            dr.pieslice([c[0] - k, c[1] - k, c[0] + k, c[1] + k], 90, 270, fill=(0, 0, 0))
            dr.line([(c[0] + 2, c[1] - k), (c[0] + 2, c[1] + k)], fill=(240, 180, 0), width=3)
        elif t == "부두":
            dr.rectangle([c[0] - k * .5, c[1] - k, c[0] + k * .5, c[1] + k], fill=(130, 90, 50), outline=(0, 0, 0))
        else:
            dr.rectangle([c[0] - k, c[1] - k * .4, c[0] + k, c[1] + k * .4], fill=(255, 140, 0), outline=(0, 0, 0))
        if labels:
            text((c[0], c[1] + k + 10), f"{e.get('label', t)}", 13 if s < 3 else 15, (40, 40, 40))
    # 교차로 점(차길 3갈래 이상 = 검정, 사람길이 끼면 회색)
    for j in plan.get("junctions", []):
        if not inside(*j["pos"]):
            continue
        c = P(*j["pos"]); rr = max(3, s * 1.6) if j["carArms"] >= 3 else max(2, s * 1.0)
        dr.ellipse([c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr], fill=(15, 15, 15) if j["carArms"] >= 3 else (120, 120, 120), outline=(255, 255, 255), width=2)
    # 쌍 경로 강조(빠른·거친 = 자홍, 느린·순한 = 초록)
    for pts, col, off in routes:
        xy = [P(p[0] + off, p[1] + off) for p in pts[::2]]
        dr.line(xy, fill=(255, 255, 255), width=max(6, int(s * 4)) + 4, joint="curve")
        dr.line(xy, fill=col, width=max(6, int(s * 4)), joint="curve")
    # v3 리듬 겹침: 본선을 속도(파랑)·기술(빨강)·중간(회색)으로, 탁 트인 바다 길은 하늘색 띠, 직선은 검은 꺾쇠
    if rhythm:
        q, n = rhythm["q"], len(rhythm["q"])
        for i in range(n):
            if rhythm["open"][i] and rhythm["open"][(i + 1) % n]:
                dr.line([P(*q[i][:2]), P(*q[(i + 1) % n][:2])], fill=(120, 215, 245), width=int(s * 13))
        for i in range(n):
            col = (25, 90, 230) if rhythm["fast"][i] else (225, 35, 35) if rhythm["slow"][i] else (150, 150, 150)
            dr.line([P(*q[i][:2]), P(*q[(i + 1) % n][:2])], fill=col, width=int(s * 5))
        for a, ln in rhythm["straight"]:
            if ln < 100:
                continue
            for e in (a, (a + int(ln / 4)) % n):
                c = P(*q[e][:2]); t = q[(e + 1) % n]; tx, tz = t[0] - q[e][0], t[1] - q[e][1]; d = math.hypot(tx, tz) or 1
                nx, nz = -tz / d * s * 9, tx / d * s * 9
                dr.line([(c[0] - nx, c[1] + nz), (c[0] + nx, c[1] - nz)], fill=(0, 0, 0), width=4)
            mid = q[(a + int(ln / 8)) % n]
            text(P(mid[0], mid[1]), f"직선 {ln:.0f}m", 15, (0, 0, 0), True, stroke=3)
    # 지름길 맛·표지 기호 (roads[].features): 점프 = 주황 삼각, 좁은 틈 = 두 줄, 표지 = 보라 마름모
    for r in plan["roads"]:
        for f_ in r.get("features", []):
            p = L.at_s(samp[r["id"]], f_["s"])
            if not inside(*p):
                continue
            c = P(*p); k = max(7, s * 3.2)
            if f_["type"] == "jump":
                dr.polygon([(c[0], c[1] - k * 1.3), (c[0] - k, c[1] + k * .7), (c[0] + k, c[1] + k * .7)], fill=(255, 140, 0), outline=(0, 0, 0), width=2)
            elif f_["type"] == "gap":
                dr.line([(c[0] - k, c[1] - k * .5), (c[0] + k, c[1] - k * .5)], fill=(0, 0, 0), width=4)
                dr.line([(c[0] - k, c[1] + k * .5), (c[0] + k, c[1] + k * .5)], fill=(0, 0, 0), width=4)
            else:
                dr.polygon([(c[0], c[1] - k), (c[0] - k, c[1]), (c[0], c[1] + k), (c[0] + k, c[1])], fill=(150, 60, 200), outline=(255, 255, 255), width=2)
            if labels and (rhythm or f_["type"] != "landmark"):
                text((c[0], c[1] - k - 10), f_.get("name", f_["type"]), 12 if s < 3 else 14, (60, 20, 90), True, stroke=3)
    if labels:
        for sl in plan.get("seaLabels", []):
            if inside(*sl["pos"]):
                text(P(*sl["pos"]), sl["name"], 20 if s < 3 else 24, (30, 70, 140), True, stroke=3)
    # v4 끝 모를 길: 지도 틀을 나가는 곳에 화살표 + 이름·끝
    for o in plan.get("outRoads", []):
        pts = [p for rid in o["roads"] for p in samp[rid]]
        k_ = next((i for i, p in enumerate(pts) if not inside(p[0], p[1], pad=-12)), len(pts) - 1)
        a, b = pts[max(0, k_ - 6)], pts[k_]
        ca, cb = P(a[0], a[1]), P(b[0], b[1])
        ux, uy = cb[0] - ca[0], cb[1] - ca[1]; dl = math.hypot(ux, uy) or 1; ux, uy = ux / dl, uy / dl
        k = max(12, s * 6)
        dr.polygon([(cb[0] + ux * k, cb[1] + uy * k), (cb[0] - uy * k * .6, cb[1] + ux * k * .6), (cb[0] + uy * k * .6, cb[1] - ux * k * .6)], fill=(230, 60, 140), outline=(0, 0, 0))
        if labels:
            text((cb[0] - ux * 90, cb[1] - uy * 90 - 14), f"{o['name']} → {o['end']}", 12 if s < 3 else 14, (150, 20, 90), True)
    # v4 배달지 후보(주소)
    for a in plan.get("addressPool", []):
        if not inside(*a["pos"]):
            continue
        c = P(*a["pos"]); rr = max(3, s * 1.5)
        dr.ellipse([c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr], fill=(40, 120, 230), outline=(255, 255, 255), width=2)
        if labels and s >= 2.5:
            text((c[0], c[1] + rr + 8), a["name"], 11, (20, 60, 140))
    # MART · 배달지
    m = plan["mart"]; c = P(*m["pos"]); k = max(10, s * 4)
    dr.rectangle([c[0] - k * 1.6, c[1] - k * .8, c[0] + k * 1.6, c[1] + k * .8], fill=(20, 70, 160), outline=(0, 0, 0), width=2)
    dr.text(c, "MART", font=font(int(k * .9), True), fill="white", anchor="mm")
    for st in plan["stops"]:
        if not inside(*st["pos"]):
            continue
        c = P(*st["pos"]); k = max(10, s * 3.5)
        dr.ellipse([c[0] - k, c[1] - k, c[0] + k, c[1] + k], fill=(255, 225, 40) if not st.get("optional") else (255, 255, 255), outline=(0, 0, 0), width=2)
        dr.text(c, str(st["n"]), font=font(int(k * 1.2), True), fill=(0, 0, 0), anchor="mm")
        if labels:
            text((c[0] + k + 4, c[1]), st["name"], 14 if s < 3 else 16, (0, 0, 0), True, anchor="lm")
    # 지도 틀 밖으로 삐져나온 길·글자를 지운다
    bg = (250, 250, 247)
    for r in ([0, 0, W, top], [0, top + mh, W, H], [0, 0, M, H], [M + mw, 0, M + mw + M // 2 if legend else W, H]):
        dr.rectangle(r, fill=bg)
    dr.rectangle([M, top, M + mw, top + mh], outline=(120, 120, 120), width=1)
    if labels and sec_labels:
        for sec in plan["sections"]:
            if inside(*sec["label"], pad=-25):
                text(P(*sec["label"]), sec["name"], 22 if s < 3 else 26, SEC_COL[sec["id"]], True, stroke=4)
    # 축척·북쪽
    bx, by = M + 14, top + mh - 24
    dr.rectangle([bx - 6, by - 22, bx + 100 * s + 30, by + 14], fill=(255, 255, 255))
    dr.line([(bx, by), (bx + 100 * s, by)], fill=(0, 0, 0), width=4)
    for k2 in (0, 50, 100):
        dr.line([(bx + k2 * s, by - 6), (bx + k2 * s, by + 6)], fill=(0, 0, 0), width=2)
    dr.text((bx + 50 * s, by - 13), "100 m", font=font(14, True), fill=(0, 0, 0), anchor="mm")
    nx_, ny_ = M + mw - 30, top + 34
    dr.polygon([(nx_, ny_ - 22), (nx_ - 11, ny_ + 10), (nx_, ny_ + 3), (nx_ + 11, ny_ + 10)], fill=(0, 0, 0))
    text((nx_, ny_ + 24), "북", 16, (0, 0, 0), True)
    dr.rectangle([M, top, M + mw, top + mh], outline=(60, 60, 60), width=1)
    if title:
        dr.text((M, 14), title, font=font(20, True), fill=(20, 20, 20), anchor="lt")
    if legend:
        lx, ly = M + mw + 24, top
        items = [("dense", None, "밀집 구역 (붙은 집·좁은 길 3.5~4.5m)"), ("sparse", None, "성김 구역 (띄엄띄엄·큰 마당·나무)"), ("tree", None, "나무"),
                 ("og", None, "열린 땅 (광장·부두·공터·해변·밭·풀밭)"), ("walk", None, "인도(연석)·길가띠 = 사람의 층"),
                 ("road", "main", "큰길 고리 (4.4~7m, 이름 6개)"), ("road", "village", "마을 차길 (보통 구역 5~6m)"), ("road", "shortcut", "지름길 (차 가능, 거침·급경사)"), ("junc", None, "교차로 (회색 = 사람길과 만남)"), ("alley", None, "골목 (사람만)"),
                 ("stairs", None, "계단 (입구 화분, 볼라드 없음)"), ("outr", None, "끝 모를 길 (지도 밖 300~600m)"), ("addr", None, "배달지 후보 (주소 16)"),
                 ("cut", None, "산 쪽 깎은 면"), ("fill", None, "메운 면 → 난간"),
                 ("pole", None, "전봇대(검정)·가로등(노랑)"), ("field", None, "계단식 밭"), ("parcel", None, "필지 (구역 색, 진할수록 층 많음)"),
                 ("stop", None, "배달지 (흰색 = 선택)"), ("P", None, "주차장"), ("view", None, "전망대"), ("tunnel", None, "터널 입구 (막힘)"),
                 ("pier", None, "부두"), ("fjump", None, "점프 둔턱 (지름길 맛)"), ("fgap", None, "좁은 틈 (지름길 맛)"), ("fmark", None, "표지 (직선 끝·갈림 앞)"), ("sec", None, "구간 경계 눈금"), ("bound", None, "플레이 경계 (밖은 원경)"), ("slice", None, "Hilltop 슬라이스 실물 범위")]
        if rhythm:
            items = [("rfast", None, "속도 구간 (68 km/h 이상 = 최고속 80%)"), ("rmid", None, "중간"), ("rslow", None, "기술 구간 (43 km/h 이하)"),
                     ("ropen", None, "탁 트인 바다 길 (건물 12m+ · 해안 40m 안)"), ("rstraight", None, "직선 100m+ (반경 150m+) 양 끝"),
                     ("road", "shortcut", "지름길"), ("road", "village", "마을 차길"), ("fjump", None, "점프 둔턱"), ("fgap", None, "좁은 틈"),
                     ("fmark", None, "표지 (직선 끝·갈림 앞)"), ("stop", None, "배달지")]
        dr.text((lx, ly), "범례", font=font(18, True), fill=(0, 0, 0))
        y = ly + 34
        for kind, sub, label in items:
            cx, cy = lx + 16, y + 9
            if kind == "road":
                o, f_ = ROAD_STYLE[sub]; dr.line([(cx - 14, cy), (cx + 14, cy)], fill=o, width=10 if sub == "main" else 8); dr.line([(cx - 14, cy), (cx + 14, cy)], fill=f_, width=6 if sub == "main" else 4)
            elif kind in ("dense", "sparse"):
                dr.rectangle([cx - 14, cy - 8, cx + 14, cy + 8], fill=PARCEL_FILL[kind], outline=DIST_LINE[kind], width=2)
            elif kind == "tree":
                dr.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=(60, 130, 60), outline=(30, 80, 30))
            elif kind == "og":
                dr.rectangle([cx - 14, cy - 8, cx, cy + 8], fill=OG_FILL["plaza"][:3]); dr.rectangle([cx, cy - 8, cx + 14, cy + 8], fill=OG_FILL["meadow"][:3])
            elif kind == "walk":
                dr.line([(cx - 14, cy - 5), (cx + 14, cy - 5)], fill=(165, 165, 160), width=4); dr.line([(cx - 14, cy + 1), (cx + 14, cy + 1)], fill=(225, 80, 60), width=6)
            elif kind == "outr":
                dr.polygon([(cx + 12, cy), (cx - 2, cy - 8), (cx - 2, cy + 8)], fill=(230, 60, 140), outline=(0, 0, 0))
            elif kind == "addr":
                dr.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], fill=(40, 120, 230), outline=(255, 255, 255), width=2)
            elif kind == "junc":
                dr.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], fill=(15, 15, 15), outline=(255, 255, 255), width=2)
            elif kind == "alley":
                dr.line([(cx - 14, cy), (cx - 4, cy)], fill=(110, 70, 40), width=3); dr.line([(cx + 2, cy), (cx + 12, cy)], fill=(110, 70, 40), width=3)
            elif kind == "stairs":
                dr.line([(cx - 14, cy), (cx + 14, cy)], fill=(60, 60, 60), width=2)
                for t_ in range(-12, 13, 5): dr.line([(cx + t_, cy - 4), (cx + t_, cy + 4)], fill=(0, 0, 0))
            elif kind == "cut":
                for t_ in range(-12, 13, 4): dr.line([(cx + t_, cy - 5), (cx + t_, cy + 5)], fill=(140, 85, 45), width=2)
            elif kind == "fill":
                dr.line([(cx - 14, cy), (cx + 14, cy)], fill=(40, 60, 120), width=3)
            elif kind == "pole":
                dr.line([(cx - 14, cy), (cx + 14, cy)], fill=(70, 70, 70)); dr.ellipse([cx - 14, cy - 3, cx - 8, cy + 3], fill=(0, 0, 0)); dr.ellipse([cx + 8, cy - 3, cx + 14, cy + 3], fill=(0, 0, 0))
            elif kind == "field":
                dr.rectangle([cx - 14, cy - 8, cx + 14, cy + 8], fill=(160, 195, 110), outline=(80, 110, 50))
            elif kind == "parcel":
                dr.rectangle([cx - 14, cy - 7, cx - 2, cy + 7], fill=SEC_FILL["3"], outline=(90, 75, 60)); dr.rectangle([cx + 2, cy - 7, cx + 14, cy + 7], fill=tuple(int(v * .8) for v in SEC_FILL["3"]), outline=(90, 75, 60))
            elif kind == "stop":
                dr.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=(255, 225, 40), outline=(0, 0, 0), width=2); dr.text((cx, cy), "1", font=font(12, True), fill=(0, 0, 0), anchor="mm")
            elif kind == "P":
                dr.rectangle([cx - 8, cy - 8, cx + 8, cy + 8], fill=(40, 90, 200)); dr.text((cx, cy), "P", font=font(12, True), fill="white", anchor="mm")
            elif kind == "view":
                dr.polygon([(cx, cy - 9), (cx - 8, cy + 7), (cx + 8, cy + 7)], fill=(250, 210, 40), outline=(0, 0, 0))
            elif kind == "tunnel":
                dr.pieslice([cx - 8, cy - 8, cx + 8, cy + 8], 90, 270, fill=(0, 0, 0)); dr.line([(cx + 2, cy - 8), (cx + 2, cy + 8)], fill=(240, 180, 0), width=3)
            elif kind == "pier":
                dr.rectangle([cx - 4, cy - 8, cx + 4, cy + 8], fill=(130, 90, 50), outline=(0, 0, 0))
            elif kind == "fjump":
                dr.polygon([(cx, cy - 9), (cx - 8, cy + 6), (cx + 8, cy + 6)], fill=(255, 140, 0), outline=(0, 0, 0), width=2)
            elif kind == "fgap":
                dr.line([(cx - 10, cy - 4), (cx + 10, cy - 4)], fill=(0, 0, 0), width=4); dr.line([(cx - 10, cy + 4), (cx + 10, cy + 4)], fill=(0, 0, 0), width=4)
            elif kind == "fmark":
                dr.polygon([(cx, cy - 8), (cx - 8, cy), (cx, cy + 8), (cx + 8, cy)], fill=(150, 60, 200), outline=(0, 0, 0))
            elif kind in ("rfast", "rslow", "rmid"):
                dr.line([(cx - 14, cy), (cx + 14, cy)], fill={"rfast": (25, 90, 230), "rslow": (225, 35, 35), "rmid": (150, 150, 150)}[kind], width=8)
            elif kind == "ropen":
                dr.line([(cx - 14, cy), (cx + 14, cy)], fill=(120, 215, 245), width=18)
            elif kind == "rstraight":
                dr.line([(cx - 12, cy - 8), (cx - 12, cy + 8)], fill=(0, 0, 0), width=4); dr.line([(cx + 12, cy - 8), (cx + 12, cy + 8)], fill=(0, 0, 0), width=4); dr.line([(cx - 12, cy), (cx + 12, cy)], fill=(25, 90, 230), width=6)
            elif kind == "sec":
                for t_, sid in zip((-10, -3, 4, 11), "1234"): dr.line([(cx + t_, cy - 8), (cx + t_, cy + 8)], fill=SEC_COL[sid], width=4)
            elif kind == "bound":
                for t_ in (-14, -2, 10): dr.line([(cx + t_, cy), (cx + t_ + 6, cy)], fill=(150, 60, 160), width=2)
            elif kind == "slice":
                for t_ in (-14, -4, 6): dr.line([(cx + t_, cy), (cx + t_ + 4, cy)], fill=(110, 90, 160), width=1)
            dr.text((lx + 40, cy), label, font=font(14), fill=(20, 20, 20), anchor="lm")
            y += 27
        y += 10
        notes = ["등고선 5m (25m 굵게), 바다 = 파랑", "지형 = 제어점 보간 (② 는 빌드 때", "   HilltopHeight+30 으로 교체)",
                 "경계 가림: 남 = 곶 능선, 동 = 언덕·터널,", "   서 = 만 건너 곶, 북 = 바다+안개", f"안개 {plan['fog']['start']}~{plan['fog']['end']}m, 먼 섬 약 640m 서쪽",
                 "슬라이스 → 지도: (x, y+30, z), 회전 0"]
        if rhythm:
            notes = ["모형: 최고속 85 km/h · 가속 10.7 · 제동 8", "   굽이 속도 = √(5.82 × 반경), 4m 간격", "   (tools/map/planlib.py rhythm)", rhythm.get("note", "")]
        for n_ in notes:
            dr.text((lx, y), n_, font=font(13), fill=(60, 60, 60)); y += 20
    return img


def main():
    plan = L.load(sys.argv[1] if len(sys.argv) > 1 else L.PLAN)
    terr = L.Terrain(plan)
    grid = Grid(terr)
    samp = {r["id"]: L.sample(r) for r in plan["roads"]}
    full = render(plan, grid, samp, EXT, 2.0, title="bayloop v4 — 구역 강약(D-140)·사람의 층(D-139)·열린 땅·끝 모를 길(D-138) · 평면도, 북쪽 위")
    full.save(os.path.join(OUT, "bayloop-plan-v4.png"))
    # 리듬 그림: 본선 4m 마다 85 km/h 모형 속도
    q = L.resample(samp["main"], closed=True)
    Rm, v, t85 = L.rhythm(q, closed=True)
    cap = L.CAP85
    land = plan["sea"]["landPolygon"]
    rh = {"q": q, "fast": [x >= 0.8 * cap for x in v], "slow": [x <= 0.5 * cap for x in v],
          "open": [L.parcel_gap(p, plan["parcels"]) >= L.OPEN_BLD and L.poly_edge_dist(p[0], p[1], land) <= L.SEA_NEAR for p in q],
          "straight": [r for r in L.runs([x >= L.STRAIGHT_R for x in Rm], closed=True) if r[1] >= 40]}
    rh["note"] = f"한 바퀴 {t85:.0f}초 · 속도 구간 시간 {100 * sum(4 / max(.5, x) for x, f in zip(v, rh['fast']) if f) / t85:.0f}%"
    rimg = render(plan, grid, samp, EXT, 2.0, title="bayloop v4 리듬 — 파랑 = 밟는 곳, 빨강 = 브레이크 밟는 곳, 하늘색 = 바다가 트인 길", rhythm=rh)
    rimg.save(os.path.join(OUT, "rhythm-v4.png"))
    # 구간 확대: 본선 구간 점 + 여백 (4칸) + 배달 쌍 대안 경로 (2칸)
    mp = samp["main"]; main_r = next(r for r in plan["roads"] if r["id"] == "main")
    kidx = L.knot_index_s(main_r, mp) + [len(mp) - 1]

    def box(pts, pad):
        xs = [p[0] for p in pts]; zs = [p[1] for p in pts]
        cx, cz = (min(xs) + max(xs)) / 2, (min(zs) + max(zs)) / 2
        half = max(max(xs) - min(xs), max(zs) - min(zs)) / 2 + pad
        ext = (max(EXT[0], cx - half), max(EXT[1], cz - half), min(EXT[2], cx + half), min(EXT[3], cz + half))
        return ext, 560 / max(ext[2] - ext[0], ext[3] - ext[1])

    panels = []
    for sec in plan["sections"]:
        a, b = main_r["sectionKnots"][sec["id"]]
        ext, s = box(mp[kidx[a]:kidx[b] + 1], 25)
        panels.append(render(plan, grid, samp, ext, s, title=sec["name"], legend=False, sec_labels=False))
    fast, gentle = L.Net(plan, samp), L.Net(plan, samp, gentle=True)
    pos = {"MART": plan["mart"]["pos"], **{str(st["n"]): st["pos"] for st in plan["stops"]}}
    for na, nb in (("1", "2"), ("4", "MART")):
        rs = []
        for net, col, off in ((gentle, (40, 160, 70), 0), (fast, (220, 30, 160), 0)):
            ev = net.evaluate(net.path(net.node(*pos[na])[0], net.node(*pos[nb])[0]))
            rs.append((ev, col, off))
        ext, s = box(rs[0][0]["pts"] + rs[1][0]["pts"], 20)
        g, f = rs[0][0], rs[1][0]
        title = f"{na}→{nb}: 자홍 빠른·거친 {f['length']:.0f}m {f['time']:.0f}초 / 초록 순한 {g['length']:.0f}m {g['time']:.0f}초"
        panels.append(render(plan, grid, samp, ext, s, title=title, legend=False, sec_labels=False, labels=False,
                             routes=[(e["pts"], c, o) for e, c, o in rs]))
    pw = max(p.width for p in panels); ph = max(p.height for p in panels)
    sheet = Image.new("RGB", (pw * 2, ph * 3), (250, 250, 247))
    for k, p in enumerate(panels):
        sheet.paste(p, ((k % 2) * pw, (k // 2) * ph))
    sheet.save(os.path.join(OUT, "bayloop-plan-v4-sections.png"))
    print("wrote", os.path.join(OUT, "bayloop-plan-v4.png"), full.size, "/ sections", sheet.size)


if __name__ == "__main__":
    main()
