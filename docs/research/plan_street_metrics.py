# -*- coding: utf-8 -*-
"""평면도(docs/map/bayloop-plan.json)를 osm_street_metrics.py 와 같은 잣대로 잰다 (PD 20261025-1418).
사용: python -X utf8 docs/research/plan_street_metrics.py [plan.json]
읽기 전용. tools/map/planlib.py 를 가져다 쓴다. 정의:
 - 차길 = main/village/shortcut. 교차점 = planlib.junctions(merge=7 m) 중 차길 가지 수(carArms) ≥ 3.
 - 막다른 끝 = 닫히지 않은 차길의 start/end 중 join 이 없는 것(이유 붙은 막다른 길 포함).
 - 가닥 = 차길을 교차점·길 끝에서 자른 토막. 길이 ≥ 7 m 만.
 - 외곽 면적 = 차길 점들의 볼록껍질 (OSM 쪽과 같은 정의).
"""
import json, math, os, sys
import statistics as S
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "tools", "map"))
import planlib as L   # noqa: E402


def hull_area(pts):
    pts = sorted(set((round(x, 2), round(z, 2)) for x, z in pts))
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
    a = sorted(a); k = (len(a)-1)*q; f = int(k); c = min(f+1, len(a)-1)
    return a[f] + (a[c]-a[f])*(k-f)


def main():
    plan = L.load(sys.argv[1] if len(sys.argv) > 1 else L.PLAN)
    samp = {r["id"]: L.sample(r) for r in plan["roads"]}
    roads = {r["id"]: r for r in plan["roads"]}
    car = [r for r in plan["roads"] if r["kind"] in L.CAR]
    js = plan.get("junctions") or L.junctions(plan, samp)
    cj = [j for j in js if j["carArms"] >= 3]
    tot = sum(L.length(samp[r["id"]]) for r in car)
    area = hull_area([(p[0], p[1]) for r in car for p in samp[r["id"]]])/1e6
    # 가닥: 각 차길을 교차점(7 m 안)에서 자른다
    edges = []
    for r in car:
        pts = samp[r["id"]]; n = len(pts)
        cuts = {0, n-1}
        for j in cj:
            if r["id"] not in j["roads"]: continue
            i, d = L.nearest(pts, j["pos"][0], j["pos"][1])
            if d < 7.0 + r["width"]/2: cuts.add(i)
        cs = sorted(cuts)
        for a, b in zip(cs, cs[1:]):
            ln = L.length(pts[a:b+1])
            if ln >= 7: edges.append(ln)
    ends = [(r["id"], k) for r in car if not r.get("closed") for k in ("start", "end") if "join" not in r.get(k, {})]
    # 닫힌 길(main) 은 끝이 없다. 막다른 끝 비율 = 막다른 끝 / (막다른 끝 + 교차점)
    dead = len(ends)
    stairs = [r for r in plan["roads"] if r["kind"] == "stairs"]
    hp = 0
    for r in car:
        pts = samp[r["id"]]; best = []
        h = 4
        for i in range(h, len(pts)-h):
            if L.radius3(pts[i-h], pts[i], pts[i+h]) < 12.0: best.append(i)
        # 연속 구간은 하나로
        hp += sum(1 for k, i in enumerate(best) if k == 0 or i - best[k-1] > 8)
    # OSM 쪽과 같은 헤어핀 정의(5 m 간격, 40 m 안에서 방향이 누적 140° 이상 꺾임)
    sys.path.insert(0, HERE)
    import osm_street_metrics as O   # noqa: E402
    hp2, gaps = 0, []
    for r in car:
        pos = O.hairpins([(p[0], p[1]) for p in samp[r["id"]]])
        hp2 += len(pos); gaps += [b-a for a, b in zip(pos, pos[1:])]
    cyc_car = L.cycles(plan, samp, L.CAR)
    cyc_all = L.cycles(plan, samp, tuple({r["kind"] for r in plan["roads"]}))
    print(f"차길 {tot/1000:.2f} km · 외곽 {area:.2f} km² · 차길 {tot/1000/area:.1f} km/km²")
    print(f"교차점(차길 가지≥3) {len(cj)} · /km² {len(cj)/area:.0f} · 간격 {tot/len(cj):.0f} m · 4갈래↑ {sum(1 for j in cj if j['carArms']>=4)}")
    print(f"막다른 끝 {dead} → 막다른 끝 비율 {100*dead/(dead+len(cj)):.0f}% · 끝 목록 " + ", ".join(f"{a}.{b}" for a, b in ends))
    print(f"가닥 {len(edges)} · 길이 중앙값 {S.median(edges):.0f} m (p25 {pct(edges,.25):.0f} – p75 {pct(edges,.75):.0f}, p90 {pct(edges,.9):.0f})")
    print(f"블록 차길 {cyc_car} (평균 {100*area/max(1,cyc_car):.1f} ha) · 사람길 포함 {cyc_all}")
    print(f"계단 {len(stairs)}개 · 길이 {sum(L.length(samp[r['id']]) for r in stairs):.0f} m · 사람길(alley+stairs) 총 {sum(L.length(samp[r['id']]) for r in plan['roads'] if r['kind'] in ('alley','stairs')):.0f} m")
    print(f"굽이 반경<12 m 구간 {hp} · 차길 km당 {hp/(tot/1000):.1f}")
    print(f"헤어핀(OSM 쪽과 같은 정의: 40 m 안 누적 ≥140°) {hp2} · 차길 km당 {hp2/(tot/1000):.1f} · 연속 간격 중앙값 {S.median(gaps) if gaps else float('nan'):.0f} m (표본 {len(gaps)})")
    mainlen = sum(L.length(samp[r['id']]) for r in car if r['id'] == 'main')
    print(f"본선(main) 비중 {100*mainlen/tot:.0f}% · 본선 외 차길 {tot-mainlen:.0f} m")
    kinds = {}
    for r in plan["roads"]:
        kinds.setdefault(r["kind"], []).append(round(L.length(samp[r["id"]])))
    print("길 종류별 길이(m):", json.dumps(kinds, ensure_ascii=False))
    # 가게↔가게(MART·정거장) 잇는 길 개수
    print(f"배달점 {len(plan['stops'])}개 (MART 제외)")


if __name__ == "__main__":
    main()
