# -*- coding: utf-8 -*-
"""평면도의 '달리는 맛' 진단 3가지 (PD 20261025-1509). 읽기 전용.
사용: python -X utf8 docs/research/plan_drive_diag.py [plan.json]
 1) 탁 트인 구간 — 본선 점마다 (건물 없음: 가장 가까운 필지 가장자리 ≥ OPEN_BLD m) · (바다 보임: 해안선 ≤ SEA_NEAR m) 의 이어진 길이
 2) 지름길의 내리막 — 지름길을 양쪽 방향으로 달릴 때 '내려가는 직선'(반경 ≥150 m · 내리막 ≥ 3 %) 최장 m, 고저차
 3) 배달 쌍의 빠른 길 / 순한 길이 가진 직선 최장 m (반경 ≥150 m)
정의는 임의(임계값) — 값 자체보다 v2 와 실제 도로를 같은 잣대로 본다는 데 의미가 있다.
"""
import math, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "tools", "map"))
sys.path.insert(0, HERE)
import planlib as L          # noqa: E402
import road_rhythm as RR     # noqa: E402

OPEN_BLD = 12.0   # 길 점에서 가장 가까운 필지 가장자리가 이보다 멀면 '건물이 안 막는다'
SEA_NEAR = 40.0   # 해안선과 이 안이면 '바다가 보인다'
DOWN = float(sys.argv[2]) if len(sys.argv) > 2 else 0.03   # 사용: plan_drive_diag.py [plan.json|-] [내리막 하한, 기본 0.03]


def parcel_gap(p, parcels):
    best = 1e9
    for q in parcels:
        d = math.hypot(p[0] - q["x"], p[1] - q["z"]) - 0.5 * max(q["w"], q["d"])
        if d < best: best = d
    return best


def longest(flags, step):
    best = cur = 0
    runs = []
    for f in flags:
        if f: cur += 1
        else:
            if cur: runs.append(cur * step)
            cur = 0
    if cur: runs.append(cur * step)
    return (max(runs) if runs else 0.0), runs


def straight_runs(pts, step=4.0):
    q = RR.resample([(a[0], a[1]) for a in pts], step)
    n = len(q)
    flags = []
    for i in range(n):
        if i < RR.H or i >= n - RR.H: flags.append(False)
        else: flags.append(RR.radius3(q[i - RR.H], q[i], q[i + RR.H]) >= RR.STRAIGHT_R)
    return longest(flags, step)


def main():
    plan = L.load(sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] != "-" else L.PLAN)
    roads = {r["id"]: r for r in plan["roads"]}
    samp = {rid: L.sample(r) for rid, r in roads.items()}
    parcels = plan["parcels"]
    land = plan["sea"]["landPolygon"]
    terr = L.Terrain(plan)

    # 1) 탁 트인 구간 (본선, 4 m 간격)
    mp = RR.resample([(a[0], a[1]) for a in samp["main"]], 4.0)
    gap = [parcel_gap(p, parcels) for p in mp]
    coast = [L.poly_edge_dist(p[0], p[1], land) for p in mp]
    nb = [g >= OPEN_BLD for g in gap]
    ns = [c <= SEA_NEAR for c in coast]
    both = [a and b for a, b in zip(nb, ns)]
    tot = 4.0 * (len(mp) - 1)
    print(f"## 1) 본선 {tot:.0f} m — 건물이 안 막는 길이(필지 가장자리 ≥{OPEN_BLD:.0f} m) / 해안선 ≤{SEA_NEAR:.0f} m / 둘 다")
    for nm, fl in (("건물 없음", nb), ("해안 가까움", ns), ("둘 다(탁 트인 바다 길)", both)):
        mx, rs = longest(fl, 4.0)
        print(f"- {nm}: 비율 {100*sum(fl)/len(fl):.0f}% · 최장 {mx:.0f} m · 100 m 이상 {sum(1 for r in rs if r >= 100)}개 · 구간 수 {len(rs)}")
    # 해안이 가까운 구간의 위치와, 그 안의 필지 수
    i = 0
    while i < len(ns):
        if ns[i]:
            j = i
            while j + 1 < len(ns) and ns[j + 1]: j += 1
            ln = (j - i + 1) * 4
            if ln >= 40:
                nbld = sum(1 for q in parcels if any(math.hypot(q["x"] - mp[k][0], q["z"] - mp[k][1]) < 25 for k in range(i, j + 1, 2)))
                print(f"  · 해안 ≤{SEA_NEAR:.0f} m 구간: s={i*4} m 부터 {ln} m · ({mp[i][0]:.0f},{mp[i][1]:.0f}) → ({mp[j][0]:.0f},{mp[j][1]:.0f}) · 25 m 안 필지 {nbld}채 · 이 구간 필지 가장자리 거리 중앙값 {sorted(gap[i:j+1])[(j-i+1)//2]:.1f} m")
            i = j + 1
        else:
            i += 1
    print(f"- 본선에서 가장 가까운 필지 가장자리 거리: 중앙값 {sorted(gap)[len(gap)//2]:.1f} m · 최소 {min(gap):.1f} m · 최대 {max(gap):.1f} m")
    print(f"- 본선 해안선 거리: 중앙값 {sorted(coast)[len(coast)//2]:.0f} m · 최소 {min(coast):.0f} m · 최대 {max(coast):.0f} m")

    # 2) 지름길 내리막 직선
    print("\n## 2) 지름길 — 길이 · 고저차 · 달리는 시간(최고속 85 km/h 모형) · 내려가는 직선")
    print("| 지름길 | 길이 m | 고저차 m (끝-처음) | 평균 경사 % | 직선 최장 m | 내려가는 직선 최장 m (어느 방향) | 모형 시간 s |\n|---|---|---|---|---|---|---|")
    for r in plan["roads"]:
        if r["kind"] != "shortcut": continue
        pts = samp[r["id"]]
        ln = L.length(pts)
        ys = [terr.h(p[0], p[1]) for p in pts]
        dy = ys[-1] - ys[0]
        st, _ = straight_runs(pts)
        # 4 m 간격 점에서 반경 + 경사 방향
        q = RR.resample([(a[0], a[1]) for a in pts], 4.0)
        qy = [terr.h(a[0], a[1]) for a in q]
        n = len(q)
        best = {}
        for sgn, nm in ((1, "정방향"), (-1, "역방향")):
            fl = []
            for i in range(n):
                if i < RR.H or i >= n - RR.H or i + 1 >= n:
                    fl.append(False); continue
                R = RR.radius3(q[i - RR.H], q[i], q[i + RR.H])
                sl = sgn * (qy[i] - qy[i + 1]) / 4.0     # 달리는 방향으로 내려가면 +
                fl.append(R >= RR.STRAIGHT_R and sl >= DOWN)
            best[nm] = longest(fl, 4.0)[0]
        bnm = max(best, key=best.get)
        cap = 85 / 3.6
        _, v = RR.profile(q, cap)
        t = sum(4.0 / max(0.5, (v[i] + v[i + 1]) / 2) for i in range(len(v) - 1))
        print(f"| {r['id']} | {ln:.0f} | {dy:+.1f} | {100*abs(dy)/ln:.1f} | {st:.0f} | {best[bnm]:.0f} ({bnm}) | {t:.0f} |")

    # 3) 배달 쌍 경로의 직선
    print("\n## 3) 배달 쌍 — 빠른 길 / 순한 길의 길이 · 직선 최장 · 최고속 85 km/h 모형 시간")
    fast, gentle = L.Net(plan, samp), L.Net(plan, samp, gentle=True)
    seq = [("MART", plan["mart"]["pos"])] + [(str(s["n"]), s["pos"]) for s in sorted(plan["stops"], key=lambda s: s["n"]) if not s.get("optional")]
    seq.append(seq[0])
    print("| 쌍 | 빠른 길 m | 직선 최장 m | 모형 s | 순한 길 m | 직선 최장 m | 모형 s |\n|---|---|---|---|---|---|---|")
    for (na, pa), (nb_, pb) in zip(seq, seq[1:]):
        row = []
        for net in (fast, gentle):
            a, _ = net.node(*pa); b, _ = net.node(*pb)
            pth = net.path(a, b)
            ev = net.evaluate(pth)
            pts = ev["pts"]
            st, _ = straight_runs(pts)
            q = RR.resample([(x[0], x[1]) for x in pts], 4.0)
            _, v = RR.profile(q, 85 / 3.6)
            t = sum(4.0 / max(0.5, (v[i] + v[i + 1]) / 2) for i in range(len(v) - 1))
            row += [f"{ev['length']:.0f}", f"{st:.0f}", f"{t:.0f}"]
        print(f"| {na}→{nb_} | " + " | ".join(row) + " |")

    # 4) 본선 구간(1~4) 안의 직선 위치
    print("\n## 4) 본선 직선(반경≥150 m) 위치 — 호 길이 s 기준")
    q = mp
    n = len(q)
    flags = []
    for i in range(n):
        if i < RR.H or i >= n - RR.H: flags.append(False)
        else: flags.append(RR.radius3(q[i - RR.H], q[i], q[i + RR.H]) >= RR.STRAIGHT_R)
    i = 0
    out = []
    while i < n:
        if flags[i]:
            j = i
            while j + 1 < n and flags[j + 1]: j += 1
            out.append((i * 4, (j - i + 1) * 4, q[i], q[j]))
            i = j + 1
        else:
            i += 1
    for s, ln, a, b in out:
        if ln >= 40:
            print(f"- s={s:.0f} m 부터 {ln:.0f} m · ({a[0]:.0f},{a[1]:.0f}) → ({b[0]:.0f},{b[1]:.0f})")


if __name__ == "__main__":
    main()
