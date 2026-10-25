"""bayloop 평면도 규칙 검사 v2 (D-135 길망). 사용: python -X utf8 tools/map/check_plan.py [plan.json]
결과는 마크다운 표로 찍고, 하나라도 FAIL 이면 exit 1.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import planlib as L

MAX_GRADE = {"main": 12.0, "village": 12.0, "shortcut": 16.0}   # % — 지름길만 16 까지(따로 표시)
MIN_R = 9.0           # m, 헤어핀 실측 기준
LIMIT = 150.0
MAIN_LEN = (900, 1500)
RATIO = (0.10, 0.40)  # 순한 길이 빠른 길보다 10~40% 느려야 '고를 이유' 가 생긴다
MAX_DY = 1.5          # 엇갈리는 곳 높이 차(이보다 크면 교차가 아니라 입체)
END_OK = {"부두", "전망대", "공사 바리케이드", "터널", "주차장"}


def radius_min(pts, h=4, closed=False):
    best, at = 1e9, pts[0]
    n = len(pts)
    rng = range(n) if closed else range(h, n - h)
    for i in rng:
        r = L.radius3(pts[(i - h) % n], pts[i], pts[(i + h) % n])
        if r < best:
            best, at = r, pts[i]
    return best, at


def main():
    plan = L.load(sys.argv[1] if len(sys.argv) > 1 else L.PLAN)
    terr = L.Terrain(plan)
    roads = {r["id"]: r for r in plan["roads"]}
    samp = {rid: L.sample(r) for rid, r in roads.items()}
    car = [r for r in plan["roads"] if r["kind"] in L.CAR]
    rows, fails = [], 0

    def row(rule, item, val, ok, note=""):
        nonlocal fails
        fails += 0 if ok else 1
        rows.append(f"| {rule} | {item} | {val} | {'PASS' if ok else 'FAIL'} | {note} |")

    def warn(rule, item, val, ok, note=""):
        rows.append(f"| {rule} | {item} | {val} | {'PASS' if ok else 'WARN'} | {note} |")
    r85, rhythm_out = [], {}

    # 규칙 1 — 경사·반경·바다 위 길·노면
    for r in car:
        pts = samp[r["id"]]
        g, ga = L.grade_max(pts)
        rad, ra = radius_min(pts, closed=r.get("closed", False))
        wet = 0 if r.get("overWater") else sum(1 for p in pts if terr.h(p[0], p[1]) < 0.5)
        lim = MAX_GRADE[r["kind"]]
        mark = " ⚑지름길(16% 허용)" if r["kind"] == "shortcut" and g > 12 else ""
        row("1 경사", f"{r['id']} ({r['kind']}, 거칠기 {r.get('roughness')})", f"{g:.1f}%", g <= lim, f"상한 {lim:.0f}% @({ga[0]:.0f},{ga[1]:.0f}){mark}")
        if len(pts) > 9:
            row("1 반경", r["id"], f"{rad:.1f}m", rad >= MIN_R, f"@({ra[0]:.0f},{ra[1]:.0f})")
        if wet:
            row("1 바다 위", r["id"], f"{wet}점", False)
    nor = [r["id"] for r in car if "roughness" not in r]
    row("1 노면 거칠기", f"차길 {len(car)}개", f"없음 {len(nor)}", not nor, ", ".join(nor))
    xs = L.crossings(plan, samp)
    badx = [f"{a}×{b}@({x:.0f},{z:.0f}) {dy:+.1f}m" for a, _, b, _, x, z, dy in xs if abs(dy) > MAX_DY]
    row("1 엇갈림 높이차", f"엇갈림 {len(xs)}곳", f"{len(badx)}곳 > {MAX_DY}m", not badx, ", ".join(badx))
    cf = [abs(p[2] - terr.h(p[0], p[1])) for p in samp["main"][::4]]
    row("1 깎기/메우기", "main 중심선", f"최대 {max(cf):.1f}m · 평균 {sum(cf)/len(cf):.1f}m", True, "지형 IDW 기준 참고값")

    # 규칙 2 — 필지가 길에 닿는가
    ps = plan["parcels"]
    unreach, inroad, wetp, outside = [], [], [], []
    play = plan["boundary"]["play"]
    for p in ps:
        fr = roads[p["front"]["road"]]
        d = L.road_dist(samp[fr["id"]], *p["frontMid"]) - fr["width"] / 2
        if not (-0.3 <= d <= 3.5):
            unreach.append(p["id"])
        c = L.rect_corners(p)
        for x, z in c[2:] + [(p["x"], p["z"])]:
            if any(L.road_dist(samp[r["id"]], x, z) < r["width"] / 2 for r in plan["roads"]):
                inroad.append(p["id"]); break
        if min(terr.h(x, z) for x, z in c) < 0.5:
            wetp.append(p["id"])
        if not all(L.in_poly(x, z, play) for x, z in c):
            outside.append(p["id"])
    kinds = {}
    for p in ps:
        k = roads[p["front"]["road"]]["kind"]; kinds[k] = kinds.get(k, 0) + 1
    row("2 못 닿는 필지", f"필지 {len(ps)}채", f"{len(unreach)}채", not unreach, "앞면 길 종류: " + ", ".join(f"{k} {v}" for k, v in kinds.items()))
    row("2 길을 덮는 필지", "", f"{len(inroad)}채", not inroad, ", ".join(inroad[:8]))
    row("2 바다에 걸친 필지", "", f"{len(wetp)}채", not wetp)
    row("2 경계 밖 필지", "", f"{len(outside)}채", not outside)
    far_in = [f["name"] for f in plan["farMasses"] if "center" in f and "radius" in f and L.in_poly(*f["center"], play)]
    row("2 경계 안 원경", "", f"{len(far_in)}", not far_in)

    # 규칙 3 — 길 끝
    bad, ends = [], 0
    for r in plan["roads"]:
        if r.get("closed"):
            continue
        pts = samp[r["id"]]
        for key, p in (("start", pts[0]), ("end", pts[-1])):
            ends += 1
            e = r.get(key, {})
            if "join" in e:
                j = roads[e["join"]]
                if L.road_dist(samp[j["id"]], p[0], p[1]) > j["width"] / 2 + 5:
                    bad.append(f"{r['id']}.{key}(접속 안 됨)")
            elif e.get("type") in END_OK:
                if e["type"] != "부두" and terr.h(p[0], p[1]) < 1.0:
                    bad.append(f"{r['id']}.{key}(바다에서 끝남)")
            else:
                bad.append(f"{r['id']}.{key}(이유 없음)")
    deadc = sum(1 for r in plan["roads"] for k in ("start", "end") if not r.get("closed") and "type" in r.get(k, {}))
    row("3 이유 없는 길 끝", f"끝 {ends}개 (막다른 끝 {deadc})", f"{len(bad)}개", not bad, ", ".join(bad))
    outr = [r["id"] for r in plan["roads"] if not all(L.in_poly(p[0], p[1], play) for p in samp[r["id"]])]
    row("3 경계 밖 길", "", f"{len(outr)}", not outr, ", ".join(outr))

    # 길망 — 교차로·블록
    js = plan.get("junctions") or L.junctions(plan, samp)
    cT = sum(1 for j in js if j["carArms"] == 3); cX = sum(1 for j in js if j["carArms"] >= 4)
    aT = sum(1 for j in js if j["arms"] == 3); aX = sum(1 for j in js if j["arms"] >= 4)
    blk_car = L.cycles(plan, samp, L.CAR)
    blk_all = L.cycles(plan, samp, tuple({r["kind"] for r in plan["roads"]}))
    row("망 교차로", "차길만", f"T {cT} · 十 {cX}", cT + cX >= 8, f"사람길 포함 T {aT} · 十 {aX} (7m 안은 하나로)")
    row("망 블록", "길로 둘러싸인 덩어리", f"차길 {blk_car} · 사람길 포함 {blk_all}", blk_car >= 5, "이음−마디+성분")

    # 규칙 5 — 본선 길이
    mp = samp["main"]
    Lm = L.length(mp)
    t_loop, vmin = L.drive(mp, True)
    kidx = L.knot_index_s(roads["main"], mp) + [len(mp) - 1]
    sl = {k: L.length(mp[kidx[a]:kidx[b] + 1]) for k, (a, b) in roads["main"]["sectionKnots"].items()}
    row("5 본선 길이", "고리", f"{Lm:.0f}m", MAIN_LEN[0] <= Lm <= MAIN_LEN[1], " · ".join(f"{k} {v:.0f}m" for k, v in sl.items()) + f" · 감속 모형 한 바퀴 {t_loop:.0f}초")

    # 규칙 6 — 배달 쌍마다 빠른·거친 길 vs 느린·순한 길
    fast, gentle = L.Net(plan, samp), L.Net(plan, samp, gentle=True)
    seq = [("MART", plan["mart"]["pos"])] + [(str(s["n"]), s["pos"]) for s in sorted(plan["stops"], key=lambda s: s["n"]) if not s.get("optional")]
    seq.append(seq[0])
    prs, tot = [], {"f": 0.0, "g": 0.0}
    for (na, pa), (nb, pb) in zip(seq, seq[1:]):
        res = {}
        for key, net in (("f", fast), ("g", gentle)):
            a, _ = net.node(*pa); b, _ = net.node(*pb)
            pth = net.path(a, b)
            res[key] = net.evaluate(pth) if pth else None
        f, g = res["f"], res["g"]
        if not f or not g:
            row("6 경로 둘", f"{na}→{nb}", "경로 1개", False); continue
        same = f["roads"] == g["roads"]
        ratio = g["time"] / f["time"] - 1
        ok = not same and RATIO[0] <= ratio <= RATIO[1]
        timed = nb != "MART"
        if timed:
            tot["f"] += f["time"]; tot["g"] += g["time"]
        # v3: 55 km/h 모형 비율은 참고(R9 가 85 km/h 모형으로 대신 본다)
        warn("6 경로 둘(55km/h 참고)", f"{na}→{nb}", f"순한 길 +{ratio * 100:.0f}%", ok, "같은 길(대안 없음)" if same else "")
        f85 = L.rhythm(L.resample(f["pts"]))[2]; g85 = L.rhythm(L.resample(g["pts"]))[2]
        r85.append((f"{na}→{nb}", f85, g85))
        for key, name, e in (("f", "빠른·거친", f), ("g", "느린·순한", g)):
            prs.append(f"| {na}→{nb} | {name} | {e['length']:.0f}m | {e['time']:.0f}초 | {(f85 if key == 'f' else g85):.0f}초 | {e['grade']:.1f}% | {e['rough']:.2f} / {e['roughMax']:.2f} | {' → '.join(e['roads'])} |")
    warn("5 갈 때 시간(55km/h 참고)", "MART→4 빠른 길만", f"{tot['f']:.0f}초", tot["f"] <= LIMIT, f"순한 길만 {tot['g']:.0f}초")

    # ── v3 운전 리듬 R1~R12 (D-136, docs/research/2026-10-25-real-town-street-networks.md §5) — 최고속 85 km/h·가속 10.7·제동 8 ──
    cap = L.CAP85
    q = L.resample(mp, closed=True)
    Rm, v, t85 = L.rhythm(q, closed=True)
    n = len(q)
    dt = [L.RSTEP / max(0.5, (v[i] + v[(i + 1) % n]) / 2) for i in range(n)]
    fast_f = [x >= 0.8 * cap for x in v]; slow_f = [x <= 0.5 * cap for x in v]
    fast_t = 100 * sum(d for d, f_ in zip(dt, fast_f) if f_) / t85
    slow_t = 100 * sum(d for d, f_ in zip(dt, slow_f) if f_) / t85
    row("R1 속도 구간 시간", "main", f"{fast_t:.0f}%", fast_t >= 55, f"v≥0.8·cap · 한 바퀴 {t85:.0f}초")
    st_runs = [r for r in L.runs([x >= L.STRAIGHT_R for x in Rm], closed=True) if r[1] >= 40]
    st_max = max((ln for _, ln in st_runs), default=0)
    st100 = sum(1 for _, ln in st_runs if ln >= 100)
    row("R2 직선", "main (R≥150, 40m 미만 제외)", f"최장 {st_max:.0f}m · 100m+ {st100}개", st_max >= 220 and st100 >= 2,
        " · ".join(f"s{a*4}:{ln:.0f}" for a, ln in st_runs))
    land = plan["sea"]["landPolygon"]
    open_f = [L.parcel_gap(p, ps) >= L.OPEN_BLD and L.poly_edge_dist(p[0], p[1], land) <= L.SEA_NEAR for p in q]
    op_runs = L.runs(open_f, closed=True)
    st_set = set()
    for a, c in st_runs:
        if c >= 100:
            st_set |= {(a + k) % n for k in range(int(c / 4))}
    op_ok = [(a, ln) for a, ln in op_runs if ln >= 200 and sum(1 for k in range(int(ln / 4)) if (a + k) % n in st_set) * 4 >= 100]
    op_max = max((ln for _, ln in op_runs), default=0)
    row("R3 탁 트인 바다 길", "main (필지 ≥12m ∧ 해안 ≤40m)", f"최장 {op_max:.0f}m", bool(op_ok), "≥200m 이고 그 안 직선 ≥100m: " + (", ".join(f"s{a*4}:{ln:.0f}" for a, ln in op_ok) or "없음"))
    fr = L.runs(fast_f, closed=True)
    fr_max = max((ln for _, ln in fr), default=0)
    row("R4 이어진 속도 구간", "main", f"최장 {fr_max:.0f}m", fr_max >= 450, " · ".join(f"s{a*4}:{ln:.0f}" for a, ln in fr if ln >= 80))
    cum = [0.0]
    for d in dt:
        cum.append(cum[-1] + d)
    starts = [i for i in range(n) if fast_f[i] and not fast_f[i - 1]]
    per = sorted((cum[b] - cum[a]) for a, b in zip(starts, starts[1:])) + ([t85 - cum[starts[-1]] + cum[starts[0]]] if starts else [])
    per = sorted(per)
    med = per[len(per) // 2] if per else 0
    row("R5 리듬", "main", f"기술 {slow_t:.0f}% · 속도 {fast_t:.0f}% · 주기 중앙 {med:.0f}초", 10 <= slow_t <= 20 and fast_t <= 75 and 5 <= med <= 12, "기술 10~20% · 속도 ≤75% · 주기 5~12초")
    rhythm_out.update(q=q, v=v, R=Rm, fast=fast_f, slow=slow_f, open=open_f, straight=st_runs)

    sc = [r for r in plan["roads"] if r["kind"] == "shortcut"]
    for r in sc:
        pts = samp[r["id"]]
        tq = L.rhythm(L.resample(pts))[2]
        row("R7 지름길 시간", r["id"], f"{tq:.0f}초", 8 <= tq <= 20, f"길이 {L.length(pts):.0f}m · 8~20초")
        dh, sgn = L.downhill_straight(pts)
        feats = r.get("features", [])
        jump = [f_ for f_ in feats if f_["type"] == "jump" and f_.get("step", 0) >= 0.5]
        gap = [f_ for f_ in feats if f_["type"] == "gap" and f_.get("width", 9) <= 2.6 and f_.get("len", 0) >= 10]
        taste = (["내리막 직선 %.0fm(%s)" % (dh, "정" if sgn > 0 else "역")] if dh >= 80 else []) + [f"점프 s{f_['s']:.0f}" for f_ in jump] + [f"좁은 틈 s{f_['s']:.0f}" for f_ in gap]
        row("R8 지름길 맛", r["id"], ", ".join(taste) or "없음", bool(taste), f"내리막 직선 최장 {dh:.0f}m")
        rad, _ = radius_min(pts)
        cost = (["좁음 %.1fm" % r["width"]] if r["width"] <= 3.4 else []) + (["R<15 굽이"] if rad < 15 else []) + (["점프"] if jump else [])
        warn("R9 지름길 대가", r["id"], ", ".join(cost) or "없음", bool(cost), f"거칠기 {r.get('roughness')}")
    row("R7 지름길 수", "shortcut", f"{len(sc)}개", 2 <= len(sc) <= 3, "2~3개")
    for nm, f85, g85 in r85:
        warn("R9 쌍 시간비", nm, f"빠른/순한 {f85 / g85:.2f}", f85 / g85 <= 0.80, f"{f85:.0f}초 / {g85:.0f}초")
    far = [(s_["n"], L.road_dist(mp, *s_["pos"])) for s_ in plan["stops"] if not s_.get("optional")]
    nfar = sum(1 for _, d in far if d >= 40)
    row("R10 갈래 위 배달지", "필수 4곳", f"본선 40m+ {nfar}곳", nfar >= 2, " · ".join(f"{k}:{d:.0f}m" for k, d in far))
    cj = [j for j in js if j["carArms"] >= 3]
    sp = min((math.dist(a["pos"], b["pos"]) for i, a in enumerate(cj) for b in cj[i + 1:]), default=999)
    warn("R11 교차로", "차길", f"{len(cj)}곳 · 최소 간격 {sp:.0f}m", len(cj) <= 24, "≤24 · 간격 ≥110m 은 참고(마을 안은 짧다)")
    t5 = sum(f85 for nm, f85, _ in r85)
    row("R12 배달 쌍 합", "빠른 길 5쌍 (85km/h)", f"{t5:.0f}초 = {100 * t5 / LIMIT:.0f}%", 0.40 * LIMIT <= t5 <= 0.70 * LIMIT, "150초의 40~70%")

    print("| 규칙 | 대상 | 값 | 판정 | 메모 |\n|---|---|---|---|---|")
    print("\n".join(rows))
    print("\n| 쌍 | 경로 | 길이 | 55km/h 모형 | 85km/h 모형 | 최대 경사 | 거칠기 평균/최대 | 거치는 길 |\n|---|---|---|---|---|---|---|---|")
    print("\n".join(prs))
    print(f"\n교차로 {len(js)}곳 (차길 T {cT} · 十 {cX})")
    print(f"\nFAIL {fails}")
    sys.exit(1 if fails else 0)


if __name__ == "__main__":
    main()
