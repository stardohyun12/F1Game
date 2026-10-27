# 과제 0248: tv6_route.json 경로(x,z) → 2m 간격 점 → tv6_loop_pts.txt ("x z" 한 줄씩) + 지점별 인덱스
import json, os, math, sys
GO = len(sys.argv) > 1
D = os.path.dirname(__file__)
r = json.load(open(os.path.join(D, "tv6_route_go.json" if GO else "tv6_route.json"), encoding="utf8"))
P = [(p[0], p[1]) for p in r["path"]]
out = [P[0]]
for a, b in zip(P, P[1:]):
    L = math.dist(a, b); n = max(1, int(L / 2))
    for i in range(1, n + 1): out.append((a[0] + (b[0] - a[0]) * i / n, a[1] + (b[1] - a[1]) * i / n))
# 같은 자리 되밟기(왕복 꼭지) 제거: 바로 이전 점과 0.3m 안이면 버림
o2 = [out[0]]
for p in out[1:]:
    if math.dist(p, o2[-1]) > .3: o2.append(p)
open(os.path.join(D, "tv6_go_pts.txt" if GO else "tv6_loop_pts.txt"), "w").write("\n".join(f"{x:.2f} {z:.2f}" for x, z in o2))
print(len(o2), "pts")
