# diag_low.txt 요약: 블록별 낮은 칸 깊이 분포 + 10 m 칸 묶음(어디 몰렸나)
import os, collections
D = os.path.dirname(os.path.abspath(__file__))
rows = [l.split() for l in open(os.path.join(D, "diag_low.txt"), encoding="utf8") if l.strip()]
by = collections.defaultdict(list)
for r in rows: by[r[0]].append((float(r[1]), float(r[2]), float(r[3]), float(r[4])))
for b, cs in by.items():
    dd = sorted(c[2] for c in cs)
    print(f"block {b}: low {len(cs)}  dd p10 {dd[len(dd)//10]:.2f} med {dd[len(dd)//2]:.2f} p90 {dd[len(dd)*9//10]:.2f}  <-0.7: {sum(1 for v in dd if v < -.7)}")
    g = collections.Counter((int(c[0] // 20) * 20, int(c[1] // 20) * 20) for c in cs)
    print("  20m cells:", g.most_common(12))
