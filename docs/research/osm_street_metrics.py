# -*- coding: utf-8 -*-
"""OSM 길망 수치 측정 (PD 20261025-1418). 사용: python -X utf8 docs/research/osm_street_metrics.py [장소...]
Overpass 에서 highway 길을 받아(임시 폴더에 캐시) 그래프로 만들고 마을 길망 수치를 잰다.
지도 데이터: (c) OpenStreetMap contributors, ODbL. 표고는 Positano 한 곳만 리포의 Copernicus 30 m 잘라낸 파일 사용.
측정 정의는 아래 주석 참고. 읽기 전용 — 리포 파일을 건드리지 않는다.
"""
import json, math, os, sys, tempfile, time, urllib.parse, urllib.request
import statistics as S

# (남, 서, 북, 동)
PLACES = {
    "Positano":      (40.627, 14.480, 40.633, 14.490),      # 리포 DEM 조각과 같은 상자
    "Amalfi":        (40.628, 14.595, 40.640, 14.612),
    "Vernazza":      (44.1325, 9.6790, 44.1385, 9.6880),
    "Manarola":      (44.1055, 9.7265, 44.1105, 9.7335),
    "Riomaggiore":   (44.0965, 9.7340, 44.1020, 9.7440),
    "Camogli":       (44.3440, 9.1480, 44.3500, 9.1570),
    "Assisi":        (43.0670, 12.6140, 43.0740, 12.6260),
    "Kyoto_Higashiyama": (34.9935, 135.7740, 35.0040, 135.7870),
    "Gamcheon":      (35.0925, 129.0050, 35.1010, 129.0150),
    "Dongpirang":    (34.8420, 128.4260, 34.8490, 128.4350),
    "Barcelona_Eixample(격자 대조)": (41.3880, 2.1550, 41.3950, 2.1650),
}
DEM = {"Positano": "tools/town-gen/dem/crop_positano_slope.json"}
CAR = {"motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "living_street", "service", "track",
       "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link"}
FOOT = {"footway", "path", "pedestrian", "cycleway", "bridleway"}
SKIP_SERVICE = {"parking_aisle", "driveway", "drive-through", "emergency_access"}
MERGE = 7.0      # m — 이 안의 교차점은 하나로 (tools/map/planlib.py junctions(merge=7.0) 와 같게)
MARGIN = 15.0    # m — 상자 가장자리에서 잘린 끝은 막다른 길로 세지 않는다
MIRRORS = ["https://overpass-api.de/api/interpreter", "https://overpass.kumi.systems/api/interpreter"]
UA = "PitCrew-research/1.0 (street network measurement)"


def fetch(name, bb):
    safe = "".join(c if c.isalnum() else "_" for c in name)
    path = os.path.join(tempfile.gettempdir(), f"osm_street_{safe}.json")
    if os.path.exists(path):
        return json.load(open(path, encoding="utf-8"))
    q = f'[out:json][timeout:90];way["highway"]({bb[0]},{bb[1]},{bb[2]},{bb[3]});(._;>;);out body;'
    last = None
    for m in MIRRORS:
        try:
            req = urllib.request.Request(m, data=urllib.parse.urlencode({"data": q}).encode(), headers={"User-Agent": UA})
            d = json.load(urllib.request.urlopen(req, timeout=170))
            json.dump(d, open(path, "w", encoding="utf-8"))
            time.sleep(2)
            return d
        except Exception as e:  # noqa: BLE001
            last = e
    raise RuntimeError(f"{name}: {last}")


def classify(t):
    h = t.get("highway")
    if h in CAR:
        if h == "service" and t.get("service") in SKIP_SERVICE:
            return None
        return "track" if h == "track" else "car"
    if h == "steps":
        return "steps"
    if h in FOOT:
        if t.get("footway") in ("sidewalk", "crossing"):
            return None
        return "foot"
    return None


def hull_area(pts):
    pts = sorted(set(pts))
    if len(pts) < 3:
        return 0.0
    def cr(o, a, b): return (a[0]-o[0])*(b[1]-o[1]) - (a[1]-o[1])*(b[0]-o[0])
    lo, up = [], []
    for p in pts:
        while len(lo) >= 2 and cr(lo[-2], lo[-1], p) <= 0: lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(up) >= 2 and cr(up[-2], up[-1], p) <= 0: up.pop()
        up.append(p)
    h = lo[:-1] + up[:-1]
    return abs(sum(h[i][0]*h[(i+1) % len(h)][1] - h[(i+1) % len(h)][0]*h[i][1] for i in range(len(h)))) / 2


def pct(a, q):
    if not a: return float("nan")
    a = sorted(a); k = (len(a)-1)*q; f = int(k); c = min(f+1, len(a)-1)
    return a[f] + (a[c]-a[f])*(k-f)


def hairpins(poly, step=5.0, win=8, thr=140.0):
    """폴리라인을 step m 로 다시 찍고, win*step(=40 m) 안에서 방향이 누적 thr 도 이상 꺾이는 곳(헤어핀)을 센다. 위치(누적 거리) 목록 반환."""
    pts, acc, out = [poly[0]], 0.0, []
    for a, b in zip(poly, poly[1:]):
        d = math.dist(a, b)
        if d == 0: continue
        n = max(1, int(d // step))
        for i in range(1, n+1):
            pts.append((a[0]+(b[0]-a[0])*i/n, a[1]+(b[1]-a[1])*i/n))
    if len(pts) < win+2: return out
    hd = [math.atan2(pts[i+1][1]-pts[i][1], pts[i+1][0]-pts[i][0]) for i in range(len(pts)-1)]
    dh = []
    for i in range(len(hd)-1):
        d = hd[i+1]-hd[i]
        d = (d+math.pi) % (2*math.pi) - math.pi
        dh.append(math.degrees(d))
    last = -1e9
    for i in range(len(dh)-win+1):
        t = sum(dh[i:i+win])
        if abs(t) >= thr and i*step - last >= 40.0:
            out.append(i*step + win*step/2); last = i*step
    return out


class Dem:
    def __init__(self, path, lat0, lon0):
        d = json.load(open(path, encoding="utf-8"))
        self.d, self.lat0, self.lon0 = d, lat0, lon0
        self.cl = math.cos(math.radians(lat0))

    def h(self, x, y):
        d = self.d
        lat = self.lat0 + y/110574.0; lon = self.lon0 + x/(111320.0*self.cl)
        c = (lon-d["lon0"])/d["dlon"]; r = (d["lat0"]-lat)/d["dlat"]
        if not (0 <= c <= d["w"]-1 and 0 <= r <= d["h"]-1): return None
        c0, r0 = int(c), int(r); c1, r1 = min(c0+1, d["w"]-1), min(r0+1, d["h"]-1); fc, fr = c-c0, r-r0
        g = lambda rr, cc: d["heights_m"][rr*d["w"]+cc]
        return (g(r0, c0)*(1-fc)+g(r0, c1)*fc)*(1-fr) + (g(r1, c0)*(1-fc)+g(r1, c1)*fc)*fr

    def grad(self, x, y, e=30.0):
        a, b, c, d = self.h(x+e, y), self.h(x-e, y), self.h(x, y+e), self.h(x, y-e)
        if None in (a, b, c, d): return None
        return (a-b)/(2*e), (c-d)/(2*e)


def cyc(deg, adj):
    """블록 수 = 이음 − 마디 + 연결 성분 (tools/map/planlib.py cycles 와 같은 정의). 상자 밖으로 뻗은 가닥은 영향 없음."""
    V = len(deg); E = sum(len(v) for v in adj.values())//2
    seen, C = set(), 0
    for s in adj:
        if s in seen: continue
        C += 1; st = [s]; seen.add(s)
        while st:
            u = st.pop()
            for v, _, _ in adj[u]:
                if v not in seen: seen.add(v); st.append(v)
    return E - V + C


def analyze(name, bb):
    data = fetch(name, bb)
    lat0 = (bb[0]+bb[2])/2; lon0 = (bb[1]+bb[3])/2; cl = math.cos(math.radians(lat0))
    proj = lambda la, lo: ((lo-lon0)*111320.0*cl, (la-lat0)*110574.0)
    nodes = {e["id"]: proj(e["lat"], e["lon"]) for e in data["elements"] if e["type"] == "node"}
    x0, y0 = proj(bb[0], bb[1]); x1, y1 = proj(bb[2], bb[3])
    inner = lambda p: x0+MARGIN <= p[0] <= x1-MARGIN and y0+MARGIN <= p[1] <= y1-MARGIN
    ways = []
    for e in data["elements"]:
        if e["type"] == "way":
            k = classify(e.get("tags", {}))
            if k and all(n in nodes for n in e["nodes"]):
                ways.append((k, e["nodes"], e.get("tags", {})))
    res = {"place": name, "bbox_km2": (x1-x0)*(y1-y0)/1e6}
    allp = [nodes[n] for k, ns, t in ways for n in ns if inner(nodes[n])]
    res["hull_km2"] = hull_area(allp)/1e6

    def graph(kinds):
        deg, adj = {}, {}
        for k, ns, t in ways:
            if k not in kinds: continue
            for a, b in zip(ns, ns[1:]):
                if a == b: continue
                deg[a] = deg.get(a, 0)+1; deg[b] = deg.get(b, 0)+1
                adj.setdefault(a, []).append((b, id(ns), k)); adj.setdefault(b, []).append((a, id(ns), k))
        return deg, adj

    def metrics(kinds, tag):
        deg, adj = graph(kinds)
        if not deg: return
        L = lambda ns: sum(math.dist(nodes[a], nodes[b]) for a, b in zip(ns, ns[1:]))
        tot = sum(L(ns) for k, ns, t in ways if k in kinds)
        key = {n for n, d in deg.items() if d != 2}
        # 교차점(차수 ≥3) 7 m 병합
        inter = [n for n in key if deg[n] >= 3 and inner(nodes[n])]
        parent = {n: n for n in inter}
        def f(a):
            while parent[a] != a: parent[a] = parent[parent[a]]; a = parent[a]
            return a
        for i, a in enumerate(inter):
            for b in inter[i+1:]:
                if math.dist(nodes[a], nodes[b]) < MERGE: parent[f(a)] = f(b)
        ncl = len({f(n) for n in inter})
        dead = [n for n in key if deg[n] == 1 and inner(nodes[n])]
        n4 = len({f(n) for n in inter if deg[n] >= 4})
        # 끝점 사이 가닥(edge) 길이·우회도
        seen, edges = set(), []
        for s in key:
            for (nb, wid, k) in adj.get(s, []):
                if (s, nb, wid) in seen: continue
                path, prev, cur = [s], s, nb
                seen.add((s, nb, wid)); path.append(cur)
                while cur not in key:
                    nxt = [(m, w2) for (m, w2, kk) in adj[cur] if m != prev or len(adj[cur]) == 1]
                    if not nxt: break
                    m, w2 = nxt[0]
                    seen.add((cur, m, w2)); prev, cur = cur, m; path.append(cur)
                if cur in key:
                    seen.add((cur, path[-2], wid))
                    ln = sum(math.dist(nodes[a], nodes[b]) for a, b in zip(path, path[1:]))
                    st_d = math.dist(nodes[path[0]], nodes[path[-1]])
                    if inner(nodes[path[0]]) and inner(nodes[path[-1]]) and ln >= MERGE and st_d >= 3.0:   # 제자리로 도는 고리는 우회도에서 뺀다
                        edges.append((ln, ln/st_d))
        V = len({n for n in key if inner(nodes[n])}); E = len(edges)
        res[tag] = {
            "길이_km": tot/1000, "교차점": ncl, "교차점_4갈래이상": n4, "막다른끝": len(dead),
            "막다른끝_비율%": 100*len(dead)/max(1, len(dead)+ncl),
            "가닥수": E,
            "가닥길이_중앙값": S.median([e[0] for e in edges]) if edges else float("nan"),
            "가닥길이_p25": pct([e[0] for e in edges], .25), "가닥길이_p75": pct([e[0] for e in edges], .75),
            "가닥길이_p90": pct([e[0] for e in edges], .90),
            "우회도_평균": S.median([e[1] for e in edges]) if edges else float("nan"),   # 중앙값(이름은 옛 것)
            "블록수": cyc(deg, adj), "블록_평균_ha": 100*res["hull_km2"]/max(1, cyc(deg, adj)),
            "교차점_간격_m(길이÷교차점)": tot/max(1, ncl),
        }
    metrics({"car", "track"}, "차길")
    metrics({"car", "track", "foot", "steps"}, "전체(사람길 포함)")
    # 길 종류별 길이·계단
    kl = {}
    for k, ns, t in ways:
        kl[k] = kl.get(k, 0)+sum(math.dist(nodes[a], nodes[b]) for a, b in zip(ns, ns[1:]))
    res["길이_m"] = {k: round(v) for k, v in kl.items()}
    deg_car, _ = graph({"car", "track"})
    st = [ns for k, ns, t in ways if k == "steps"]
    res["계단_갯수"] = len(st)
    res["계단_차길과_양끝_접속"] = sum(1 for ns in st if ns[0] in deg_car and ns[-1] in deg_car)
    # 헤어핀 (이름 같은 차길을 한 줄로 이은 뒤 센다, 못 이으면 way 하나씩)
    car_ways = [(ns, t) for k, ns, t in ways if k in ("car",) and len(ns) > 1]
    tot_car = sum(math.dist(nodes[a], nodes[b]) for ns, t in car_ways for a, b in zip(ns, ns[1:]))
    hp, gaps = 0, []
    groups = {}
    for ns, t in car_ways:
        groups.setdefault(t.get("name") or t.get("ref") or id(ns), []).append(ns)
    for g, lst in groups.items():
        ends = {}
        for i, ns in enumerate(lst):
            for e in (ns[0], ns[-1]): ends.setdefault(e, []).append(i)
        used, chains = set(), []
        for i, ns in enumerate(lst):
            if i in used: continue
            chain = list(ns); used.add(i); grew = True
            while grew:
                grew = False
                for j, m in enumerate(lst):
                    if j in used: continue
                    if m[0] == chain[-1]: chain += m[1:]; used.add(j); grew = True
                    elif m[-1] == chain[-1]: chain += m[::-1][1:]; used.add(j); grew = True
                    elif m[-1] == chain[0]: chain = m[:-1]+chain; used.add(j); grew = True
                    elif m[0] == chain[0]: chain = m[::-1][:-1]+chain; used.add(j); grew = True
            chains.append(chain)
        for ch in chains:
            pos = hairpins([nodes[n] for n in ch])
            hp += len(pos); gaps += [b-a for a, b in zip(pos, pos[1:])]
    res["헤어핀"] = {"개수": hp, "차길km당": hp/max(1e-6, tot_car/1000), "연속_간격_중앙값_m": S.median(gaps) if gaps else float("nan"), "간격_표본": len(gaps)}
    # 표고(있을 때만): 길 방향 vs 경사 방향
    if name in DEM:
        dem = Dem(DEM[name].replace("/", os.sep) if os.path.exists(DEM[name]) else DEM[name], lat0, lon0)
        out = {}
        for kind in ("car", "steps", "foot"):
            along, across, sl = 0.0, 0.0, []
            for k, ns, t in ways:
                if k != kind: continue
                for a, b in zip(ns, ns[1:]):
                    pa, pb = nodes[a], nodes[b]; d = math.dist(pa, pb)
                    if d < 1: continue
                    m = ((pa[0]+pb[0])/2, (pa[1]+pb[1])/2); g = dem.grad(*m)
                    if g is None: continue
                    gm = math.hypot(*g)
                    sl.append((100*gm, d))
                    if gm < 0.03: continue    # 3 % 미만은 평지로 보고 방향 판정 제외
                    c = abs(((pb[0]-pa[0])*g[0]+(pb[1]-pa[1])*g[1])/(d*gm))   # 1 = 경사 방향(fall line), 0 = 등고선 방향
                    phi = math.degrees(math.acos(min(1, c)))
                    if phi <= 30: along += d
                    elif phi >= 60: across += d
            tl = sum(d for _, d in sl)
            if tl:
                out[kind] = {"측정길이_m": round(tl), "경사방향(≤30°)%": round(100*along/tl), "등고선방향(≥60°)%": round(100*across/tl),
                             "평균경사%": round(sum(s*d for s, d in sl)/tl, 1)}
        res["표고_30m_DEM"] = out
    return res


def fmt(v):
    return "nan" if isinstance(v, float) and math.isnan(v) else (f"{v:.2f}" if isinstance(v, float) else str(v))


if __name__ == "__main__":
    names = sys.argv[1:] or list(PLACES)
    allr = []
    for n in names:
        try:
            r = analyze(n, PLACES[n]); allr.append(r)
        except Exception as e:  # noqa: BLE001
            print("FAIL", n, e)
    print("| 장소 | 외곽 km² | 차길 km | 차길 km/km² | 교차점(차길) | 교차점 /km² | 교차점 간격 m | 막다른 끝 % | 가닥 길이 중앙값 m (p25–p75) | 4갈래↑/교차점 | 우회도(중앙값) | 블록수 | 블록 평균 ha | 계단 갯수 | 헤어핀 /차길km |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for r in allr:
        c = r.get("차길")
        if not c: print(f"| {r['place']} | 차길 없음 |"); continue
        print(f"| {r['place']} | {r['hull_km2']:.2f} | {c['길이_km']:.2f} | {c['길이_km']/max(1e-6, r['hull_km2']):.1f} | {c['교차점']} | {c['교차점']/max(1e-6, r['hull_km2']):.0f} | {c['교차점_간격_m(길이÷교차점)']:.0f} | {c['막다른끝_비율%']:.0f} | "
              f"{c['가닥길이_중앙값']:.0f} ({c['가닥길이_p25']:.0f}–{c['가닥길이_p75']:.0f}) | {c['교차점_4갈래이상']}/{c['교차점']} | {c['우회도_평균']:.2f} | {c['블록수']} | {c['블록_평균_ha']:.1f} | {r['계단_갯수']} | {r['헤어핀']['차길km당']:.1f} |")
    print()
    print("| 장소 | [전체] 길이 km | 교차점 | 교차점 간격 m | 막다른 끝 % | 가닥 길이 중앙값 m | 사람길(foot+steps) 길이 m | 차길 길이 m | 계단이 차길↔차길 | 헤어핀 간격 중앙값 m (표본) |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for r in allr:
        a = r.get("전체(사람길 포함)")
        if not a: continue
        L = r["길이_m"]
        print(f"| {r['place']} | {a['길이_km']:.2f} | {a['교차점']} | {a['교차점_간격_m(길이÷교차점)']:.0f} | {a['막다른끝_비율%']:.0f} | {a['가닥길이_중앙값']:.0f} | {L.get('foot', 0)+L.get('steps', 0)} | {L.get('car', 0)+L.get('track', 0)} | {r['계단_차길과_양끝_접속']} | "
              f"{fmt(r['헤어핀']['연속_간격_중앙값_m'])} ({r['헤어핀']['간격_표본']}) |")
    for r in allr:
        if "표고_30m_DEM" in r:
            print("\n표고(30 m DEM)", r["place"], json.dumps(r["표고_30m_DEM"], ensure_ascii=False))
