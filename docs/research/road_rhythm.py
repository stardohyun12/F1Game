# -*- coding: utf-8 -*-
"""'속도 구간 / 기술 구간' 리듬을 같은 모형으로 잰다 (PD 20261025-1509). 읽기 전용.
사용: python -X utf8 docs/research/road_rhythm.py [plan|osm|all] [cap_kmh ...]
 입력 두 가지, 같은 모형:
   plan = docs/map/bayloop-plan.json 의 길(중심선, planlib.sample)
   osm  = 실제 도로(OSM way 를 이어 만든 한 줄). 아말피 SS163, 빅서 CA-1, 일본 이로하자카 등 (PLACES_ROAD)
 모형(= tools/map/planlib.py drive()): 굽이 속도 v_c = min(cap, sqrt(A_LAT * R)), A_LAT = 8^2/11 ≈ 5.8 m/s²,
   가속 3 m/s², 감속 6 m/s². R 은 4 m 간격 점의 ±12 m 현(3점 외접원).
 구간 정의(임의 — 값이 아니라 같은 잣대로 비교하는 데 의미가 있다):
   속도 구간 = v ≥ 0.8·cap, 기술 구간 = v ≤ 0.5·cap, 사이는 중간.
   직선 = 반경 ≥ 150 m 가 이어지는 구간.
"""
import json, math, os, sys, tempfile, time, urllib.parse, urllib.request
import statistics as S
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "tools", "map"))
A_LAT = 8.0 ** 2 / 11.0
ACC, BRK = 3.0, 6.0
STEP = 4.0
H = 3            # 점 3개 = ±12 m
STRAIGHT_R = 150.0

MIRRORS = ["https://overpass-api.de/api/interpreter", "https://overpass.kumi.systems/api/interpreter"]
UA = "PitCrew-research/1.0 (road rhythm measurement)"
# (이름, 남서북동 상자, Overpass way 필터)
PLACES_ROAD = {
    "SS163_Amalfitana(포지타노~아말피)": ((40.600, 14.470, 40.650, 14.620), '["ref"~"SS ?163"]'),
    "CA-1_BigSur(PCH)": ((36.230, -121.950, 36.400, -121.800), '["ref"="CA 1"]'),
    "이로하자카(닛코)": ((36.720, 139.480, 36.760, 139.540), '["ref"~"120"]'),
}


def fetch(name, bb, flt):
    safe = "".join(c if c.isalnum() else "_" for c in name)
    path = os.path.join(tempfile.gettempdir(), f"osm_road_{safe}.json")
    if os.path.exists(path):
        return json.load(open(path, encoding="utf-8"))
    q = f'[out:json][timeout:90];way["highway"]{flt}({bb[0]},{bb[1]},{bb[2]},{bb[3]});(._;>;);out body;'
    last = None
    for m in MIRRORS:
        try:
            req = urllib.request.Request(m, data=urllib.parse.urlencode({"data": q}).encode(), headers={"User-Agent": UA})
            d = json.load(urllib.request.urlopen(req, timeout=170))
            json.dump(d, open(path, "w", encoding="utf-8")); time.sleep(2)
            return d
        except Exception as e:  # noqa: BLE001
            last = e
    raise RuntimeError(f"{name}: {last}")


def to_xy(lon, lat, lat0):
    return ((lon) * 111320 * math.cos(math.radians(lat0)), lat * 110540)


def longest_chain(d, lat0):
    """OSM way 들을 이어 가장 긴 한 줄(두 끝 사이 최장 경로) 점 목록(m)을 만든다. 갈래가 있으면 가장 긴 쪽."""
    nodes = {e["id"]: to_xy(e["lon"], e["lat"], lat0) for e in d["elements"] if e["type"] == "node"}
    adj = {}
    for w in d["elements"]:
        if w["type"] != "way": continue
        ids = [n for n in w["nodes"] if n in nodes]
        for a, b in zip(ids, ids[1:]):
            L = math.dist(nodes[a], nodes[b])
            adj.setdefault(a, []).append((b, L)); adj.setdefault(b, []).append((a, L))

    def far(src):
        dist, prev, st = {src: 0.0}, {src: None}, [src]
        while st:
            u = st.pop()
            for v, L in adj[u]:
                if v not in dist:
                    dist[v] = dist[u] + L; prev[v] = u; st.append(v)
        e = max(dist, key=dist.get)
        return e, dist, prev
    start = next(iter(adj))
    a, _, _ = far(start)
    b, dist, prev = far(a)
    path, u = [], b
    while u is not None:
        path.append(nodes[u]); u = prev[u]
    return path, dist[b]


def resample(pts, step=STEP):
    """호 길이 기준으로 step m 마다 점을 다시 찍는다."""
    cum = [0.0]
    for a, b in zip(pts, pts[1:]):
        cum.append(cum[-1] + math.dist(a, b))
    total = cum[-1]
    out, j, s = [], 0, 0.0
    while s <= total + 1e-9:
        while j < len(cum) - 2 and cum[j + 1] < s:
            j += 1
        seg = cum[j + 1] - cum[j]
        f = 0.0 if seg < 1e-9 else (s - cum[j]) / seg
        out.append((pts[j][0] + (pts[j + 1][0] - pts[j][0]) * f, pts[j][1] + (pts[j + 1][1] - pts[j][1]) * f))
        s += step
    return out


def radius3(a, b, c):
    ab, bc, ca = math.dist(a, b), math.dist(b, c), math.dist(c, a)
    cr = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
    if abs(cr) < 1e-9: return 1e9
    return ab * bc * ca / (2 * abs(cr))


def profile(pts, cap_mps, closed=False):
    n = len(pts)
    R = []
    for i in range(n):
        if not closed and (i < H or i >= n - H): R.append(1e9)
        else: R.append(radius3(pts[(i - H) % n], pts[i], pts[(i + H) % n]))
    v = [min(cap_mps, math.sqrt(A_LAT * r)) for r in R]
    ds = [STEP] * (n - 1)
    for rep in range(2 if closed else 1):
        for i in range(n - 1):
            v[i + 1] = min(v[i + 1], math.sqrt(v[i] ** 2 + 2 * ACC * ds[i]))
        for i in range(n - 2, -1, -1):
            v[i] = min(v[i], math.sqrt(v[i + 1] ** 2 + 2 * BRK * ds[i]))
        if closed:
            v[0] = min(v[0], v[-1]); v[-1] = v[0]
    return R, v


def runs(flags, step=STEP):
    out, cur = [], 0
    for f in flags:
        if f: cur += 1
        elif cur: out.append(cur * step); cur = 0
    if cur: out.append(cur * step)
    return out


def analyze(name, pts, caps_kmh, closed=False):
    pts = resample(pts) if len(pts) > 2 else pts
    total = STEP * (len(pts) - 1)
    print(f"\n### {name} — 길이 {total:.0f} m (점 {len(pts)}개, {STEP:.0f} m 간격)")
    R = None
    rows = []
    for cap_kmh in caps_kmh:
        cap = cap_kmh / 3.6
        R, v = profile(pts, cap, closed)
        t = sum(STEP / max(0.5, (v[i] + v[i + 1]) / 2) for i in range(len(v) - 1))
        fast = [x >= 0.8 * cap for x in v]
        slow = [x <= 0.5 * cap for x in v]
        fr = runs(fast); sr = runs(slow)
        # 교대: 속도→기술→속도 한 주기의 시간(초) = 속도 구간 시작 사이 시간
        starts = [i for i in range(1, len(fast)) if fast[i] and not fast[i - 1]]
        cum_t = [0.0]
        for i in range(len(v) - 1): cum_t.append(cum_t[-1] + STEP / max(0.5, (v[i] + v[i + 1]) / 2))
        periods = [cum_t[b] - cum_t[a] for a, b in zip(starts, starts[1:])]
        fast_t = sum(STEP / max(0.5, v[i]) for i in range(len(v) - 1) if fast[i])
        slow_t = sum(STEP / max(0.5, v[i]) for i in range(len(v) - 1) if slow[i])
        rows.append((cap_kmh, t, 100 * sum(fast) / len(fast), 100 * sum(slow) / len(slow), 100 * fast_t / t, 100 * slow_t / t,
                     max(fr) if fr else 0, len(fr), S.median(periods) if periods else float("nan"), len(sr), sum(v) / len(v) * 3.6))
    straight = runs([r >= STRAIGHT_R for r in R])
    hair = sum(1 for i in range(1, len(R) - 1) if R[i] < 15 and R[i] <= min(R[i - 1], R[i + 1]))
    print(f"직선(반경≥{STRAIGHT_R:.0f} m 이어진 구간): 최장 {max(straight) if straight else 0:.0f} m · 100 m 이상 {sum(1 for s in straight if s >= 100)}개 · 200 m 이상 {sum(1 for s in straight if s >= 200)}개 · 길이 합 {100*sum(straight)/total:.0f}%")
    print(f"반경<15 m 굽이(국소 최소) {hair}개 → km당 {hair/(total/1000):.1f} · 반경<30 m 비율 {100*sum(1 for r in R if r<30)/len(R):.0f}% · 반경 30~150 m(고속 굽이) 비율 {100*sum(1 for r in R if 30<=r<150)/len(R):.0f}%")
    print("\n| 최고속 km/h | 한 바퀴 시간 s | 속도 구간 길이 % | 기술 구간 길이 % | 속도 구간 시간 % | 기술 구간 시간 % | 최장 속도 구간 m | 속도 구간 수 | 속도→속도 주기 중앙값 s | 기술 구간 수 | 평균 속도 km/h |\n|---|---|---|---|---|---|---|---|---|---|---|")
    for r in rows:
        print(f"| {r[0]:.0f} | {r[1]:.0f} | {r[2]:.0f} | {r[3]:.0f} | {r[4]:.0f} | {r[5]:.0f} | {r[6]:.0f} | {r[7]} | {r[8]:.0f} | {r[9]} | {r[10]:.0f} |")


def main():
    global ACC, BRK
    mode = sys.argv[1] if len(sys.argv) > 1 else "all"
    rest = sys.argv[2:]
    for a in [a for a in rest if "=" in a]:        # 가속·감속 바꿔 보기: ACC=10.7 BRK=8 (게임 차: ArcadeCarController.cs:13-14,154)
        k, v = a.split("=")
        if k.upper() == "ACC": ACC = float(v)
        if k.upper() == "BRK": BRK = float(v)
    caps = [float(x) for x in rest if "=" not in x] or [55.0, 85.0]
    if mode in ("plan", "all"):
        import planlib as L
        plan = L.load()
        for rid in ("main",):
            r = next(x for x in plan["roads"] if x["id"] == rid)
            pts = [(p[0], p[1]) for p in L.sample(r)]
            analyze(f"평면도 v2 {rid}", pts, caps, closed=bool(r.get("closed")))
        for r in plan["roads"]:
            if r["kind"] == "shortcut":
                pts = [(p[0], p[1]) for p in L.sample(r)]
                analyze(f"평면도 v2 {r['id']} (shortcut)", pts, caps)
    if mode in ("osm", "all"):
        for name, (bb, flt) in PLACES_ROAD.items():
            try:
                d = fetch(name, bb, flt)
                lat0 = (bb[0] + bb[2]) / 2
                pts, ln = longest_chain(d, lat0)
                analyze(f"{name} OSM", pts, caps)
            except Exception as e:  # noqa: BLE001
                print(f"\n### {name}: 실패 {e}")


if __name__ == "__main__":
    main()
