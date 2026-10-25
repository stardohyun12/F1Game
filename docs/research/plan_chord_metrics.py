# -*- coding: utf-8 -*-
"""평면도 v2 의 '가로지름(현)'·등고선 방향·허브를 잰다 (PD 20261025-1418). 읽기 전용.
사용: python -X utf8 docs/research/plan_chord_metrics.py [plan.json]
 - 현(chord) = 양 끝이 본선(main)에 직접 붙는 차길. 본선 위 붙는 자리(호 길이 s)와 현 길이를 잰다.
 - 질러가기 비 = 현 길이 / 본선을 따라 도는 짧은 쪽 호 길이. 작을수록 많이 질러간다.
 - 등고선 방향 = osm_street_metrics.py 의 Positano DEM 측정과 같은 정의(길-경사선 사이 각 ≥60° / ≤30°).
   주의: planlib.Terrain 은 길 매듭도 지형 제어점에 더한다 → 길이 경사선 쪽으로 '측정되는' 순환. 그래서 지형 제어점만으로도 잰다.
"""
import math, os, sys
import statistics as S
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "tools", "map"))
import planlib as L   # noqa: E402


def contour_table(plan, roads, samp, terr):
    def grad(x, z, d=3.0):
        return ((terr.h(x + d, z) - terr.h(x - d, z)) / (2 * d), (terr.h(x, z + d) - terr.h(x, z - d)) / (2 * d))
    print("\n| 길 종류 | 측정 m | 등고선 방향(≥60°) % | 경사선 방향(≤30°) % | 지형 평균 경사 % | 평지(<2%) 제외 m |\n|---|---|---|---|---|---|")
    groups = {"전체 차길": [r for r in plan["roads"] if r["kind"] in L.CAR],
              "main": [roads["main"]],
              "main 외 차길": [r for r in plan["roads"] if r["kind"] in L.CAR and r["id"] != "main"],
              "stairs": [r for r in plan["roads"] if r["kind"] == "stairs"]}
    for name, rs in groups.items():
        tot = cont = fall = 0.0; slopes = []; flat = 0.0
        for r in rs:
            pts = samp[r["id"]]
            for a, b in zip(pts, pts[1:]):
                seg = math.hypot(b[0] - a[0], b[1] - a[1])
                if seg < 1e-6: continue
                mx, mz = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
                gx, gz = grad(mx, mz); g = math.hypot(gx, gz)
                if g < 0.02:
                    flat += seg; continue
                c = abs((b[0] - a[0]) * gx + (b[1] - a[1]) * gz) / (seg * g)
                ang = math.degrees(math.acos(min(1.0, c)))
                tot += seg; slopes.append(g * 100 * seg)
                if ang >= 60: cont += seg
                elif ang <= 30: fall += seg
        if tot:
            print(f"| {name} | {tot:.0f} | {100*cont/tot:.0f} | {100*fall/tot:.0f} | {sum(slopes)/tot:.1f} | {flat:.0f} |")


def main():
    plan = L.load(sys.argv[1] if len(sys.argv) > 1 else L.PLAN)
    roads = {r["id"]: r for r in plan["roads"]}
    samp = {rid: L.sample(r) for rid, r in roads.items()}
    mp = samp["main"]
    cum = [0.0]
    for a, b in zip(mp, mp[1:]):
        cum.append(cum[-1] + L.length([a, b]))
    Lm = cum[-1]

    def arc_of(p):
        i, d = L.nearest(mp, p[0], p[1])
        return cum[i], d

    def short_arc(s1, s2):
        d = abs(s1 - s2)
        return min(d, Lm - d)

    print(f"본선 길이 {Lm:.0f} m")
    print("\n| 길 | 종류 | 길이 m | 본선 위 s1 | s2 | 본선 따라 짧은 호 m | 질러가기 비(현/호) | 절약 m |\n|---|---|---|---|---|---|---|---|")
    att = []
    chords = 0
    for r in plan["roads"]:
        if r["id"] == "main" or r["kind"] not in L.CAR:
            continue
        s, e = r.get("start", {}), r.get("end", {})
        pts = samp[r["id"]]
        ln = L.length(pts)
        if s.get("join") == "main":
            att.append(arc_of(pts[0])[0])
        if e.get("join") == "main":
            att.append(arc_of(pts[-1])[0])
        if s.get("join") == "main" and e.get("join") == "main":
            s1, s2 = arc_of(pts[0])[0], arc_of(pts[-1])[0]
            arc = short_arc(s1, s2)
            chords += 1
            print(f"| {r['id']} | {r['kind']} | {ln:.0f} | {s1:.0f} | {s2:.0f} | {arc:.0f} | {ln/arc:.2f} | {arc-ln:+.0f} |")
    att = sorted(att)
    gaps = [b - a for a, b in zip(att, att[1:])] + [att[0] + Lm - att[-1]]
    print(f"\n본선에 직접 붙는 가지 끝 {len(att)}개 · 직접 현 {chords}개")
    print("본선 위 붙는 자리 간격(m): " + ", ".join(f"{g:.0f}" for g in gaps))
    print(f"간격 중앙값 {S.median(gaps):.0f} m · 최대 {max(gaps):.0f} m · 최소 {min(gaps):.0f} m")
    non = sorted(L.length(samp[r['id']]) for r in plan['roads'] if r['id'] != 'main' and r['kind'] in L.CAR)
    print("본선 외 차길 길이(m, 오름차순): " + ", ".join(f"{x:.0f}" for x in non))
    print(f"≤ 40 m 짜리 {sum(1 for x in non if x <= 40)}개 / {len(non)}개 (연결 토막 의심)")

    for label, only_ctrl in (("지형 제어점만(순환 없음)", True), ("길 매듭 포함(순환, 참고)", False)):
        terr = L.Terrain(plan)
        if only_ctrl:
            terr.pts = [tuple(p) for p in plan["terrain"]["controlPoints"]]
        print(f"\n### 등고선 방향 측정 — {label}")
        contour_table(plan, roads, samp, terr)

    js = plan.get("junctions") or L.junctions(plan, samp)
    mx, mz = plan["mart"]["pos"]
    near = sorted(((math.hypot(j["pos"][0] - mx, j["pos"][1] - mz), j) for j in js), key=lambda t: t[0])[:3]
    print("\nMART 가까운 교차점 3곳(거리 m, 차길 갈래, 길):", [(round(d), j["carArms"], j["roads"]) for d, j in near])
    within = [j for j in js if math.hypot(j["pos"][0] - mx, j["pos"][1] - mz) <= 60]
    print(f"MART 60 m 안 교차점 {len(within)}곳 · 그중 차길 갈래 합 {sum(j['carArms'] for j in within)}")


if __name__ == "__main__":
    main()
