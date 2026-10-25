"""bayloop 평면도 공용 함수 — 지형 높이, 길 곡선 뽑기, 거리. 표준 라이브러리만 쓴다(numpy 없음).

좌표: x = 동(+), z = 북(+), y = 높이(해면 0). 단위 m.
"""
import json, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PLAN = os.path.join(ROOT, "docs", "map", "bayloop-plan.json")


def load(path=PLAN):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


# ── 기하 ──
def seg_dist(px, pz, ax, az, bx, bz):
    dx, dz = bx - ax, bz - az
    L2 = dx * dx + dz * dz
    t = 0.0 if L2 == 0 else max(0.0, min(1.0, ((px - ax) * dx + (pz - az) * dz) / L2))
    qx, qz = ax + dx * t, az + dz * t
    return math.hypot(px - qx, pz - qz), t


def in_poly(x, z, poly):
    inside = False
    n = len(poly)
    for i in range(n):
        x1, z1 = poly[i]
        x2, z2 = poly[(i + 1) % n]
        if (z1 > z) != (z2 > z) and x < (x2 - x1) * (z - z1) / (z2 - z1) + x1:
            inside = not inside
    return inside


def poly_edge_dist(x, z, poly):
    return min(seg_dist(x, z, *poly[i], *poly[(i + 1) % len(poly)])[0] for i in range(len(poly)))


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


# ── 지형 ──
class Terrain:
    """제어점 역거리 가중(IDW) + 해안선 마스크. 해안선 안쪽 cliffW m 안에서 높이가 0 으로 떨어진다(절벽)."""

    def __init__(self, plan):
        t = plan["terrain"]
        self.pts = [tuple(p) for p in t["controlPoints"]]
        for r in plan["roads"]:          # 길 제어점도 지형 제어점 — 길이 지형 위에 얹힌다
            if r.get("terrainFollows", True):
                self.pts += [(k[0], k[1], k[2]) for k in r["knots"]]
        self.soft = t.get("softening", 6.0) ** 2
        self.land = plan["sea"]["landPolygon"]
        self.cliffW = t.get("cliffWidth", 10.0)
        self.beach = t.get("beaches", [])   # [x, z, r, width]
        self.seaY = t.get("seaFloor", -6.0)

    def raw(self, x, z):
        sw = sh = 0.0
        for px, pz, ph in self.pts:
            d2 = (x - px) ** 2 + (z - pz) ** 2
            if d2 < 0.01:
                return ph
            w = 1.0 / (d2 + self.soft) ** 1.5
            sw += w
            sh += w * ph
        return sh / sw

    def h(self, x, z):
        if not in_poly(x, z, self.land):
            return self.seaY
        d = poly_edge_dist(x, z, self.land)
        cw = self.cliffW
        for bx, bz, br, bw in self.beach:
            if math.hypot(x - bx, z - bz) < br:
                cw = bw
        return self.raw(x, z) * smooth(d / cw) + 0.3


# ── 길 곡선 (중심점 Catmull-Rom, xyz 함께) ──
def _cr(p0, p1, p2, p3, n):
    def tj(ti, a, b):
        return ti + max(1e-6, math.dist(a[:2], b[:2])) ** 0.5
    t0 = 0.0
    t1 = tj(t0, p0, p1); t2 = tj(t1, p1, p2); t3 = tj(t2, p2, p3)
    out = []
    for i in range(n):
        t = t1 + (t2 - t1) * i / n
        def lerp(a, b, ta, tb):
            return [((tb - t) * a[k] + (t - ta) * b[k]) / (tb - ta) for k in range(3)]
        A1 = lerp(p0, p1, t0, t1); A2 = lerp(p1, p2, t1, t2); A3 = lerp(p2, p3, t2, t3)
        B1 = lerp(A1, A2, t0, t2); B2 = lerp(A2, A3, t1, t3)
        out.append(lerp(B1, B2, t1, t2))
    return out


def sample(road, step=1.0):
    """길 중심선을 약 step m 간격 점 [x, z, y] 로. closed 면 고리."""
    k = [list(p) for p in road["knots"]]
    closed = road.get("closed", False)
    if closed:
        ext = [k[-1]] + k + [k[0], k[1]]
    else:
        ext = [[2 * k[0][i] - k[1][i] for i in range(3)]] + k + [[2 * k[-1][i] - k[-2][i] for i in range(3)]]
    segs = len(k) if closed else len(k) - 1
    pts = []
    for s in range(segs):
        p0, p1, p2, p3 = ext[s], ext[s + 1], ext[s + 2], ext[s + 3]
        n = max(2, int(math.dist(p1[:2], p2[:2]) / step * 2))
        pts += _cr(p0, p1, p2, p3, n)
    pts.append(list(k[0] if closed else k[-1]))
    # 다시 고르게
    out = [pts[0]]
    acc = 0.0
    for a, b in zip(pts, pts[1:]):
        acc += math.dist(a[:2], b[:2])
        if acc >= step:
            out.append(b)
            acc = 0.0
    if not closed and out[-1] is not pts[-1]:
        out.append(pts[-1])
    return out


def knot_index_s(road, pts):
    """각 제어점이 뽑은 점 목록에서 몇 번째 근처인지(구간 경계용)."""
    res, j = [], 0
    for kp in road["knots"]:
        best, bi = 1e9, j
        for i in range(j, min(len(pts), j + 400)):
            d = math.dist(pts[i][:2], kp[:2])
            if d < best:
                best, bi = d, i
        res.append(bi)
        j = bi
    return res


def tangent(pts, i):
    a = pts[max(0, i - 1)]
    b = pts[min(len(pts) - 1, i + 1)]
    dx, dz = b[0] - a[0], b[1] - a[1]
    L = math.hypot(dx, dz) or 1.0
    return dx / L, dz / L


def length(pts):
    return sum(math.dist(a[:2], b[:2]) for a, b in zip(pts, pts[1:]))


def radius3(a, b, c):
    ab, bc, ca = math.dist(a[:2], b[:2]), math.dist(b[:2], c[:2]), math.dist(c[:2], a[:2])
    cross = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]))
    return 1e9 if cross < 1e-9 else ab * bc * ca / (2 * cross)


def nearest(pts, x, z, lo=0):
    best, bi = 1e9, lo
    for i in range(lo, len(pts)):
        d = (pts[i][0] - x) ** 2 + (pts[i][1] - z) ** 2
        if d < best:
            best, bi = d, i
    return bi, math.sqrt(best)


def road_dist(pts, x, z):
    return min(seg_dist(x, z, a[0], a[1], b[0], b[1])[0] for a, b in zip(pts, pts[1:]))


def rect_corners(p):
    """필지 꼭짓점 4개. rot = 앞면 바깥(길 쪽) 반대, 즉 안쪽 방향 각도(도, x축 기준 반시계)."""
    a = math.radians(p["rot"])
    ix, iz = math.cos(a), math.sin(a)       # 안쪽(깊이) 방향
    tx, tz = -iz, ix                        # 앞면 방향
    hw, hd = p["w"] / 2, p["d"] / 2
    return [(p["x"] + sx * tx * hw + sd * ix * hd, p["z"] + sx * tz * hw + sd * iz * hd) for sx, sd in ((-1, -1), (1, -1), (1, 1), (-1, 1))]


# ── 길망 그래프 (v2, D-135) ──
CAR = ("main", "village", "shortcut")
PAR = 55 / 3.6               # m/s
A_LAT = 8.0 ** 2 / 11.0      # 합성 주행이 반경 11m 헤어핀을 8m/s 로 도는 횡가속 ≈ 5.8 m/s²
ACC, BRK = 3.0, 6.0


def grade_max(pts, win=10):
    best, at = 0.0, pts[0]
    for i in range(len(pts)):
        acc, j = 0.0, i
        while j + 1 < len(pts) and acc < win:
            acc += math.dist(pts[j][:2], pts[j + 1][:2]); j += 1
        if acc >= win * 0.9:
            g = abs(pts[j][2] - pts[i][2]) / acc * 100
            if g > best:
                best, at = g, pts[i]
    return best, at


def curve_speed(pts, closed=False, h=4):
    n = len(pts)
    out = []
    for i in range(n):
        if not closed and (i < h or i >= n - h):
            r = 1e9
        else:
            r = radius3(pts[(i - h) % n], pts[i], pts[(i + h) % n])
        out.append(min(PAR, math.sqrt(A_LAT * r)))
    return out


def drive(pts, closed=False, v0=0.0, v1=0.0):
    """길 점 목록을 감속 모형으로 달린 시간(초). 굽이 속도 √(A·R), 가속 3·감속 6, 처음·끝 속도 v0·v1."""
    n = len(pts)
    if n < 2:
        return 0.0, PAR
    vc = curve_speed(pts, closed)
    ds = [max(1e-3, math.dist(pts[i][:2], pts[i + 1][:2])) for i in range(n - 1)]
    v = vc[:]
    v[0] = min(v[0], v0)
    if not closed:
        v[-1] = min(v[-1], v1)
    for i in range(n - 1):
        v[i + 1] = min(v[i + 1], math.sqrt(v[i] ** 2 + 2 * ACC * ds[i]))
    for i in range(n - 2, -1, -1):
        v[i] = min(v[i], math.sqrt(v[i + 1] ** 2 + 2 * BRK * ds[i]))
    t = sum(ds[i] / max(0.5, (v[i] + v[i + 1]) / 2) for i in range(n - 1))
    return t, min(vc)


def _seg_x(a, b, c, d):
    """선분 ab 와 cd 가 엇갈리면 (t, u), 아니면 None."""
    r = (b[0] - a[0], b[1] - a[1]); s = (d[0] - c[0], d[1] - c[1])
    den = r[0] * s[1] - r[1] * s[0]
    if abs(den) < 1e-9:
        return None
    t = ((c[0] - a[0]) * s[1] - (c[1] - a[1]) * s[0]) / den
    u = ((c[0] - a[0]) * r[1] - (c[1] - a[1]) * r[0]) / den
    if 0 <= t <= 1 and 0 <= u <= 1:
        return t, u
    return None


def crossings(plan, samp, kinds=None):
    """서로 다른 길 중심선이 엇갈리는 점. [(rid_a, i_a, rid_b, i_b, x, z, dy)] — 이어붙인 끝점끼리는 뺀다."""
    roads = [r for r in plan["roads"] if kinds is None or r["kind"] in kinds]
    cell = 6.0
    grid = {}
    for r in roads:
        pts = samp[r["id"]]
        for i in range(len(pts) - 1):
            k = (int(pts[i][0] // cell), int(pts[i][1] // cell))
            grid.setdefault(k, []).append((r["id"], i))
    out, seen = [], []
    for (cx, cz), lst in grid.items():
        cand = []
        for dx in (-1, 0, 1):
            for dz in (-1, 0, 1):
                cand += grid.get((cx + dx, cz + dz), [])
        for ra, ia in lst:
            pa = samp[ra]
            for rb, ib in cand:
                if rb <= ra:
                    continue
                pb = samp[rb]
                hit = _seg_x(pa[ia], pa[ia + 1], pb[ib], pb[ib + 1])
                if not hit:
                    continue
                # 끝점이 상대 길 위에 있는 이음(T)은 엇갈림이 아니다
                if (ia <= 2 or ia >= len(pa) - 4) or (ib <= 2 or ib >= len(pb) - 4):
                    continue
                x = pa[ia][0] + (pa[ia + 1][0] - pa[ia][0]) * hit[0]
                z = pa[ia][1] + (pa[ia + 1][1] - pa[ia][1]) * hit[0]
                if any(s[0] == ra and s[1] == rb and math.hypot(s[2] - x, s[3] - z) < 4 for s in seen):
                    continue
                seen.append((ra, rb, x, z))
                ya = pa[ia][2] + (pa[ia + 1][2] - pa[ia][2]) * hit[0]
                yb = pb[ib][2] + (pb[ib + 1][2] - pb[ib][2]) * hit[1]
                out.append((ra, ia, rb, ib, x, z, ya - yb))
    return out


class Net:
    """차길 그래프. 마디 = 길 점(1m), 이음 = 길을 따라 + 이어붙은 곳·엇갈리는 곳.
    gentle=True 면 지름길과 거칠기 > 0.3 인 길을 뺀 '순한 길망'."""

    def __init__(self, plan, samp, kinds=CAR, gentle=False):
        self.roads = [r for r in plan["roads"] if r["kind"] in kinds
                      and not (gentle and (r["kind"] == "shortcut" or r.get("roughness", 0) > 0.3))]
        ids = {r["id"] for r in self.roads}
        self.pts, self.vc, self.where, self.adj = [], [], [], []
        base = {}
        for r in self.roads:
            p = samp[r["id"]]
            base[r["id"]] = len(self.pts)
            vc = curve_speed(p, r.get("closed", False))
            for i, q in enumerate(p):
                self.pts.append(q); self.vc.append(vc[i]); self.where.append((r["id"], i)); self.adj.append([])
            n = len(p)
            for i in range(n if r.get("closed") else n - 1):
                self._edge(base[r["id"]] + i, base[r["id"]] + (i + 1) % n, r.get("roughness", 0))
        for r in self.roads:
            if r.get("closed"):
                continue
            p = samp[r["id"]]
            for key, k in (("start", 0), ("end", len(p) - 1)):
                j = r.get(key, {}).get("join")
                if j in ids:
                    jj, _ = nearest(samp[j], p[k][0], p[k][1])
                    self._edge(base[r["id"]] + k, base[j] + jj, 0.0)
        for ra, ia, rb, ib, x, z, dy in crossings(plan, samp, kinds):
            if ra in ids and rb in ids:
                self._edge(base[ra] + ia, base[rb] + ib, 0.0)

    def _edge(self, a, b, rough):
        d = max(0.01, math.dist(self.pts[a][:2], self.pts[b][:2]))
        self.adj[a].append((b, d, rough)); self.adj[b].append((a, d, rough))

    def node(self, x, z):
        best, bi = 1e18, None
        for i, q in enumerate(self.pts):
            d = (q[0] - x) ** 2 + (q[1] - z) ** 2
            if d < best:
                best, bi = d, i
        return bi, math.sqrt(best)

    def path(self, a, b):
        import heapq
        dist = {a: 0.0}; prev = {}
        pq = [(0.0, a)]
        while pq:
            d, u = heapq.heappop(pq)
            if u == b:
                break
            if d > dist.get(u, 1e18):
                continue
            for v, ln, _ in self.adj[u]:
                nd = d + ln / max(0.5, min(self.vc[u], self.vc[v]))
                if nd < dist.get(v, 1e18):
                    dist[v] = nd; prev[v] = u
                    heapq.heappush(pq, (nd, v))
        if b not in dist:
            return None
        seq = [b]
        while seq[-1] != a:
            seq.append(prev[seq[-1]])
        return seq[::-1]

    def evaluate(self, seq):
        pts = [self.pts[i] for i in seq]
        t, vmin = drive(pts)
        ln = rsum = rmax = 0.0
        roads = []
        for a, b in zip(seq, seq[1:]):
            e = next(e for e in self.adj[a] if e[0] == b)
            ln += e[1]; rsum += e[1] * e[2]; rmax = max(rmax, e[2])
            rid = self.where[b][0]
            if not roads or roads[-1] != rid:
                roads.append(rid)
        g, _ = grade_max(pts)
        return {"time": t, "length": ln, "grade": g, "rough": rsum / max(ln, 1e-6), "roughMax": rmax,
                "vmin": vmin, "roads": roads, "pts": pts}


def junctions(plan, samp, merge=7.0):
    """교차로 목록(이어붙은 곳 + 엇갈리는 곳). 가지 수는 차길(carArms)·사람길 포함(arms) 따로."""
    roads = {r["id"]: r for r in plan["roads"]}
    raw = []
    for r in plan["roads"]:
        if r.get("closed"):
            continue
        p = samp[r["id"]]
        for key, k in (("start", 0), ("end", len(p) - 1)):
            j = r.get(key, {}).get("join")
            if j in roads:
                jj, _ = nearest(samp[j], p[k][0], p[k][1])
                raw.append((p[k][0], p[k][1], r["id"], k)); raw.append((p[k][0], p[k][1], j, jj))
    for ra, ia, rb, ib, x, z, dy in crossings(plan, samp):
        raw.append((x, z, ra, ia)); raw.append((x, z, rb, ib))
    groups = []
    for x, z, rid, i in raw:
        for g in groups:
            if math.hypot(g["x"] - x, g["z"] - z) < merge:
                g["m"].append((rid, i)); break
        else:
            groups.append({"x": x, "z": z, "m": [(rid, i)]})
    out = []
    for g in groups:
        arms = {}
        for rid, i in g["m"]:
            r = roads[rid]; n = len(samp[rid])
            inner = r.get("closed") or (5 <= i <= n - 6)
            arms[rid] = max(arms.get(rid, 0), 2 if inner else 1)
        car = sum(v for k, v in arms.items() if roads[k]["kind"] in CAR)
        allv = sum(arms.values())
        if allv >= 3:
            out.append({"pos": [round(g["x"], 1), round(g["z"], 1)], "carArms": car, "arms": allv, "roads": sorted(arms)})
    return out


# ── v3 리듬 모형 (D-136, docs/research/road_rhythm.py 의 resample·profile·runs 를 옮김) ──
# 게임 차 값: 최고속 85 km/h · 가속 10.7 · 제동 8 m/s² (ArcadeCarController.cs:12-14,154). 굽이 속도 √(A_LAT·R).
CAP85, ACC85, BRK85 = 85 / 3.6, 10.7, 8.0
RSTEP, RH = 4.0, 3                  # 4 m 간격, 반경은 ±12 m 세 점
STRAIGHT_R = 150.0                  # 직선 = R ≥ 150 m
OPEN_BLD, SEA_NEAR = 12.0, 40.0     # 탁 트임 = 필지 가장자리 ≥12 m ∧ 해안선 ≤40 m


def resample(pts, step=RSTEP, closed=False):
    """호 길이 기준 step m 마다 [x, z, y] 를 다시 찍는다."""
    p = [list(q) + [0.0] * (3 - len(q)) for q in pts]
    if closed:
        p = p + [p[0]]
    cum = [0.0]
    for a, b in zip(p, p[1:]):
        cum.append(cum[-1] + math.dist(a[:2], b[:2]))
    out, j, s = [], 0, 0.0
    while s <= cum[-1] - (step * 0.5 if closed else -1e-9):
        while j < len(cum) - 2 and cum[j + 1] < s:
            j += 1
        seg = cum[j + 1] - cum[j]
        f = 0.0 if seg < 1e-9 else (s - cum[j]) / seg
        out.append([p[j][k] + (p[j + 1][k] - p[j][k]) * f for k in range(3)])
        s += step
    return out


def rhythm(pts, closed=False, cap=CAP85, acc=ACC85, brk=BRK85, v0=None, v1=None):
    """4 m 재표본 점의 (반경 R, 속도 v, 걸린 시간 t). v0/v1 을 주면 처음·끝 속도를 묶는다(정차 = 0)."""
    n = len(pts)
    R = [1e9 if (not closed and (i < RH or i >= n - RH)) else radius3(pts[(i - RH) % n], pts[i], pts[(i + RH) % n]) for i in range(n)]
    v = [min(cap, math.sqrt(A_LAT * r)) for r in R]
    if v0 is not None:
        v[0] = min(v[0], v0)
    if v1 is not None:
        v[-1] = min(v[-1], v1)
    m = n if closed else n - 1
    for _ in range(2 if closed else 1):
        for i in range(m):
            v[(i + 1) % n] = min(v[(i + 1) % n], math.sqrt(v[i] ** 2 + 2 * acc * RSTEP))
        for i in range(m - 1, -1, -1):
            v[i] = min(v[i], math.sqrt(v[(i + 1) % n] ** 2 + 2 * brk * RSTEP))
    t = sum(RSTEP / max(0.5, (v[i] + v[(i + 1) % n]) / 2) for i in range(m))
    return R, v, t


def runs(flags, step=RSTEP, closed=False):
    """참인 칸이 이어진 덩어리 [(시작 번호, 길이 m)]. closed 면 끝과 처음을 잇는다."""
    out, cur, st = [], 0, 0
    for i, f in enumerate(flags):
        if f:
            if not cur:
                st = i
            cur += 1
        elif cur:
            out.append([st, cur]); cur = 0
    if cur:
        out.append([st, cur])
    if closed and len(out) > 1 and out[0][0] == 0 and flags[-1]:
        last = out.pop(); out[0] = [last[0], last[1] + out[0][1]]
    return [(a, c * step) for a, c in out]


def parcel_gap(p, parcels):
    """길 점에서 가장 가까운 필지 가장자리(중심 거리 − ½·긴 변) — plan_drive_diag.py 와 같은 정의."""
    return min((math.hypot(p[0] - q["x"], p[1] - q["z"]) - 0.5 * max(q["w"], q["d"]) for q in parcels), default=1e9)


def downhill_straight(pts, min_slope=0.03):
    """길을 어느 방향으로든 달릴 때 '내려가는 직선'(R≥150 ∧ 경사 ≥3%) 최장 m 과 방향(+1 정/−1 역). 높이는 길 자체 y."""
    q = resample(pts)
    n = len(q)
    best = (0.0, 1)
    for sgn in (1, -1):
        fl = []
        for i in range(n):
            if i < RH or i >= n - RH or i + 1 >= n:
                fl.append(False); continue
            sl = sgn * (q[i][2] - q[i + 1][2]) / RSTEP
            fl.append(radius3(q[i - RH], q[i], q[i + RH]) >= STRAIGHT_R and sl >= min_slope)
        m = max((ln for _, ln in runs(fl)), default=0.0)
        if m > best[0]:
            best = (m, sgn)
    return best


def s_at(pts, x, z):
    """길 위 (x, z) 에 가장 가까운 점까지의 호 길이 m."""
    i, _ = nearest(pts, x, z)
    return round(length(pts[:i + 1]), 1)


def at_s(pts, s):
    acc = 0.0
    for a, b in zip(pts, pts[1:]):
        d = math.dist(a[:2], b[:2])
        if acc + d >= s:
            f = (s - acc) / max(d, 1e-9)
            return [a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f]
        acc += d
    return pts[-1][:2]


def cycles(plan, samp, kinds):
    """길로 둘러싸인 덩어리 수 = 이음 − 마디 + 연결 성분 (평면 그래프의 안쪽 면 수)."""
    net = Net(plan, samp, kinds=kinds)
    V = len(net.pts)
    E = sum(len(v) for v in net.adj) // 2
    seen, C = set(), 0
    for s in range(V):
        if s in seen:
            continue
        C += 1
        st = [s]; seen.add(s)
        while st:
            u = st.pop()
            for v, _, _ in net.adj[u]:
                if v not in seen:
                    seen.add(v); st.append(v)
    return E - V + C


# ── v4 (D-138) 공용 ──
def road_widths(road, pts):
    """점마다 길 폭. widthProfile [{from, to, width}] (본선 제어점 번호) 이 있으면 구간 폭, 경계 ±12m 에서 녹인다."""
    w0 = road["width"]
    prof = road.get("widthProfile")
    if not prof:
        return [w0] * len(pts)
    kidx = knot_index_s(road, pts) + [len(pts) - 1]
    w = [w0] * len(pts)
    for seg in prof:
        a, b = kidx[seg["from"]], kidx[min(seg["to"], len(kidx) - 1)]
        for i in range(a, b + 1):
            w[i] = seg["width"]
    n, H = len(w), 12
    closed = road.get("closed", False)
    out = []
    for i in range(n):
        acc = c = 0
        for k in range(-H, H + 1):
            j = (i + k) % n if closed else min(n - 1, max(0, i + k))
            acc += w[j]; c += 1
        out.append(acc / c)
    return out


def half_at(widths, pts, x, z):
    i, _ = nearest(pts, x, z)
    return widths[i] / 2


def poly_area(poly):
    return abs(sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly)))) / 2


def is_out(road):
    """끝 모를 길(v4 outRoads) — 경계 밖으로 안개 속까지 이어진다."""
    return bool(road.get("out"))


def road_xsecs(road, pts):
    """점마다 단면(crossSection, D-139). 본선은 widthProfile 구간마다 다르다."""
    prof = road.get("widthProfile")
    x0 = road.get("crossSection")
    if not prof:
        return [x0] * len(pts)
    kidx = knot_index_s(road, pts) + [len(pts) - 1]
    out = [x0 or prof[0].get("crossSection")] * len(pts)
    for seg in prof:
        a, b = kidx[seg["from"]], kidx[min(seg["to"], len(kidx) - 1)]
        for i in range(a, b + 1):
            out[i] = seg.get("crossSection", out[i])
    return out


def xs_side(xs, side):
    """side +1 = 진행 방향 왼쪽(법선 (-tz, tx)), -1 = 오른쪽"""
    return xs["left"] if side > 0 else xs["right"]


def xs_reach(xs, side):
    """차도 가장자리 → 인도 바깥까지(경계 + 인도·길가띠)."""
    if not xs:
        return 0.0
    return xs_side(xs, side)["walk"]
