# roadpaint 검증: rp_snap_<a>.tsv/.bin 과 <b> 비교 → 바뀐 메시 개수·위치, 지형 정점 변화(1 cm 초과) 개수·범위. 인자: a b cx cz
import sys, struct, math, json
a, b, cx, cz = sys.argv[1], sys.argv[2], float(sys.argv[3]), float(sys.argv[4])
d = "D:/Github/F1Game/docs/qa/tmp/"
def tsv(t):
    r = {}
    for ln in open(d + f"rp_snap_{t}.tsv", encoding="utf8"):
        p = ln.rstrip("\n").split("\t")
        if len(p) >= 6: r.setdefault(p[0], []).append((p[1], float(p[2]), float(p[3])))
    return r
A, B = tsv(a), tsv(b)
changed = [(k, v[0][1], v[0][2]) for k, v in B.items() if k not in A or sorted(x[0] for x in A[k]) != sorted(x[0] for x in v)]
gone = [k for k in A if k not in B]
def terr(t):
    raw = open(d + f"rp_terr_{t}.bin", "rb").read()
    return struct.unpack(f"<{len(raw)//4}f", raw)
TA, TB = terr(a), terr(b)
res = {"meshes_before": sum(len(v) for v in A.values()), "meshes_after": sum(len(v) for v in B.values()),
       "changed_or_new": len(changed), "gone": len(gone)}
if len(TA) == len(TB):
    mv = []
    for i in range(0, len(TA), 3):
        dy = TB[i+1] - TA[i+1]
        if abs(dy) > .01: mv.append((math.hypot(TA[i]-cx, TA[i+2]-cz), dy))
    res["terrain_moved_gt1cm"] = len(mv)
    if mv:
        ds = sorted(x[0] for x in mv)
        res["terrain_moved_dist_from_edit_m"] = {"min": round(ds[0],1), "p50": round(ds[len(ds)//2],1), "p95": round(ds[int(len(ds)*.95)],1), "max": round(ds[-1],1)}
        res["terrain_moved_within_30m"] = sum(1 for x in mv if x[0] < 30)
        res["terrain_max_dy"] = round(max(abs(x[1]) for x in mv), 2)
else:
    res["terrain_vertcount"] = [len(TA)//3, len(TB)//3]
    # 정점 수가 다르면 2 m 칸 평균 높이로 비교
    def grid(T):
        g = {}
        for i in range(0, len(T), 3):
            c = (int(T[i] // 2), int(T[i+2] // 2)); s = g.setdefault(c, [0.0, 0]); s[0] += T[i+1]; s[1] += 1
        return {c: s[0] / s[1] for c, s in g.items()}
    GA, GB = grid(TA), grid(TB)
    mv = [(math.hypot(c[0]*2+1-cx, c[1]*2+1-cz), GB[c]-GA[c]) for c in GA if c in GB and abs(GB[c]-GA[c]) > .05]
    res["terrain_cells_2m_moved_gt5cm"] = len(mv)
    if mv:
        ds = sorted(x[0] for x in mv)
        res["terrain_cells_dist_from_edit_m"] = {"min": round(ds[0],1), "p50": round(ds[len(ds)//2],1), "p95": round(ds[int(len(ds)*.95)],1), "max": round(ds[-1],1)}
        res["terrain_cells_far_gt60m"] = sum(1 for x in mv if x[0] > 60)
# 이름 무관(갈림목 V2j_N 은 번호가 밀림): 내용 해시가 전엔 없던 메시만
hA = {x[0] for v in A.values() for x in v}
newc = sorted((math.hypot(x-cx, z-cz), k) for k, v in B.items() for h, x, z in v if h not in hA)
res["content_new_by_hash"] = len(newc)
res["content_new_dist_from_edit_m"] = [round(c[0]) for c in newc]
res["content_new_far_gt60m"] = [k for dd, k in newc if dd > 60]
ch = sorted((math.hypot(x-cx, z-cz), k) for k, x, z in changed)
res["changed_dist_from_edit_m"] = [round(c[0]) for c in ch][:40]
res["changed_far_gt60m"] = [k for dd, k in ch if dd > 60][:15]
print(json.dumps(res, ensure_ascii=False, indent=1))
