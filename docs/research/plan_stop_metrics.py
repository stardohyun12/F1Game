# -*- coding: utf-8 -*-
"""배달점(정류장)이 본선 위에 있는가, 가지 위에 있는가 (PD 20261025-1418). 읽기 전용.
사용: python -X utf8 docs/research/plan_stop_metrics.py [plan.json]
각 점에서 가장 가까운 차길 2개와 거리, 그리고 check_plan 규칙 6 과 같은 빠른 길/순한 길 경로가 거치는 길 목록.
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "tools", "map"))
import planlib as L   # noqa: E402


def main():
    plan = L.load(sys.argv[1] if len(sys.argv) > 1 else L.PLAN)
    samp = {r["id"]: L.sample(r) for r in plan["roads"]}
    car = [r for r in plan["roads"] if r["kind"] in L.CAR]
    pts = [("MART", plan["mart"]["pos"])] + [(f'{s["n"]} {s["name"]}', s["pos"]) for s in sorted(plan["stops"], key=lambda s: s["n"])]
    print("| 점 | 가까운 차길 1 (거리 m) | 2 (거리 m) |\n|---|---|---|")
    for n, p in pts:
        ds = sorted((L.road_dist(samp[r["id"]], p[0], p[1]), r["id"], r["kind"]) for r in car)[:2]
        print(f"| {n} | {ds[0][1]} {ds[0][2]} ({ds[0][0]:.0f}) | {ds[1][1]} {ds[1][2]} ({ds[1][0]:.0f}) |")
    fast, gentle = L.Net(plan, samp), L.Net(plan, samp, gentle=True)
    seq = [("MART", plan["mart"]["pos"])] + [(str(s["n"]), s["pos"]) for s in sorted(plan["stops"], key=lambda s: s["n"]) if not s.get("optional")]
    seq.append(seq[0])
    print("\n| 쌍 | 빠른 길이 거치는 길 | 길이 m | 순한 길이 거치는 길 | 길이 m | 두 길이 겹치지 않는 길이 m (순한 길 기준) |\n|---|---|---|---|---|---|")
    for (na, pa), (nb, pb) in zip(seq, seq[1:]):
        out = []
        for net in (fast, gentle):
            a, _ = net.node(*pa); b, _ = net.node(*pb)
            pth = net.path(a, b)
            out.append(net.evaluate(pth) if pth else None)
        f, g = out
        if not f or not g:
            print(f"| {na}→{nb} | 경로 없음 |"); continue
        shared = [r for r in g["roads"] if r in f["roads"]]
        uniq = sum(L.length(samp[r]) for r in g["roads"] if r not in f["roads"])
        print(f"| {na}→{nb} | {' → '.join(f['roads'])} | {f['length']:.0f} | {' → '.join(g['roads'])} | {g['length']:.0f} | {uniq:.0f} (길 단위 근사) |")


if __name__ == "__main__":
    main()
