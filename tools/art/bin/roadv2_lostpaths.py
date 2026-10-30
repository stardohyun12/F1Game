"""pg1501: 원본 노선(town-v6b.json) 대비 v2 결과에서 사라진 길 길이를 센다 + 구역 그림.

  python -X utf8 tools/art/bin/roadv2_lostpaths.py --tag after [--box x0,z0,x1,z1 --png out.png]

원본 길 중심선을 1 m 간격으로 찍고, 각 점이 같은 재질의 v2 면(띠·갈림목·걷는 길) 안에 있으면 '살아 있음'.
다른 재질 면에 덮였어도 큰길(main) 중심선에서 반폭+4 m 안이면 정상 갈림목 입구로 본다(차수 교차·합류).
그 밖은 '사라짐'(풀밭이거나, 진회색 갈림목에 칠해져 밝은 길이 없어짐). 길별로 사라진 연속 구간 ≥3 m 만 센다.
"""
import argparse, json, math, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from roadv2_codex import request, ROOT

KIND_MAT = {"main": "TV6_Main", "village": "TV6_Village"}


def dump(box, parent, active_only=False):
    cs = (ROOT / "docs/qa/tmp/pg1501_dump.cs.txt").read_text(encoding="utf-8").replace("__PARENT__", parent).replace("__ACTIVEONLY__", "true" if active_only else "false")
    for k, v in zip(("__X0__", "__Z0__", "__X1__", "__Z1__"), box):
        cs = cs.replace(k, str(v))
    return json.loads(request("exec", {"code": cs}))


class Grid:
    def __init__(self, objs, cell=4.0):
        self.cell, self.g = cell, {}
        for o in objs:
            for t in o["tris"]:
                xs, zs = t[0:6:2], t[1:6:2]
                for gx in range(math.floor(min(xs) / cell), math.floor(max(xs) / cell) + 1):
                    for gz in range(math.floor(min(zs) / cell), math.floor(max(zs) / cell) + 1):
                        self.g.setdefault((gx, gz), []).append((t, o["mat"], o["name"]))

    def hits(self, x, z):
        out = []
        for t, mat, name in self.g.get((math.floor(x / self.cell), math.floor(z / self.cell)), ()):
            ax, az, bx, bz, cx, cz = t[:6]
            d1 = (bx - ax) * (z - az) - (bz - az) * (x - ax)
            d2 = (cx - bx) * (z - bz) - (cz - bz) * (x - bx)
            d3 = (ax - cx) * (z - cz) - (az - cz) * (x - cx)
            if (d1 >= -1e-6 and d2 >= -1e-6 and d3 >= -1e-6) or (d1 <= 1e-6 and d2 <= 1e-6 and d3 <= 1e-6):
                out.append((mat, name))
        return out


def samples(knots):
    pts = []
    for a, b in zip(knots, knots[1:]):
        n = max(1, math.ceil(math.hypot(b[0] - a[0], b[1] - a[1])))
        pts += [(a[0] + (b[0] - a[0]) * s / n, a[1] + (b[1] - a[1]) * s / n) for s in range(n)]
    return pts + [(knots[-1][0], knots[-1][1])]


def segd(p, a, b):
    dx, dz = b[0] - a[0], b[1] - a[1]; L = dx * dx + dz * dz or 1e-9
    t = max(0, min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L))
    return math.hypot(p[0] - a[0] - t * dx, p[1] - a[1] - t * dz)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tag", required=True); ap.add_argument("--box"); ap.add_argument("--png")
    ap.add_argument("--min-run", type=float, default=3.0)
    ap.add_argument("--old", action="store_true", help="v2 전 옛 길(Roads, 꺼진 것 포함) — 재질 무시, 덮였으면 살아 있음")
    a = ap.parse_args()
    plan = json.load(open(ROOT / "docs/map/town-v6b.json", encoding="utf-8"))
    roads = plan["roads"]
    allk = [k for r in roads for k in r["knots"]]
    box = [float(v) for v in a.box.split(",")] if a.box else [min(k[0] for k in allk) - 5, min(k[1] for k in allk) - 5, max(k[0] for k in allk) + 5, max(k[1] for k in allk) + 5]
    # v2 는 구역(+여유)에 닿은 길만 다시 깔고 나머지 옛 길은 켜 둔다 → 켜진 옛 길도 화면에 보이는 길로 센다
    objs = dump(box, "Roads") if a.old else dump(box, "RoadsV2") + dump(box, "Roads", True)
    if a.old:
        for o in objs: o["mat"] = "ANY"
    grid = Grid(objs)
    # 걷는 길(alley/stairs) 재질 = 그 길 이름의 V2p 조각 재질
    pmat = {}
    for o in objs:
        if o["name"].startswith("ROAD_V2p_"):
            pmat[o["name"][9:].rsplit("_", 1)[0]] = o["mat"]
    # 차길 재질 = 씬 옛 길(ROAD_<id>) 재질(json kind 와 씬 kind 가 다름: shortcut=TV6_Stone 등)
    smat = {}
    for line in request("exec", {"code": (ROOT / "docs/qa/tmp/pg1501_kinds.cs.txt").read_text(encoding="utf-8")}).splitlines():
        f = line.split("	")
        if len(f) >= 5: smat[f[1]] = f[4]
    mains = [r for r in roads if r["kind"] == "main"]
    lost_roads, total_lost, total_len, rows = 0, 0.0, 0.0, []
    for r in roads:
        ks = r["knots"]
        if not any(box[0] <= k[0] <= box[2] and box[1] <= k[1] <= box[3] for k in ks):
            continue
        want = "ANY" if a.old else (smat.get(r["id"]) or pmat.get(r["id"]))
        pts = [p for p in samples(ks) if box[0] <= p[0] <= box[2] and box[1] <= p[1] <= box[3]]
        L = max(0, len(pts) - 1); total_len += L
        bad = []
        for p in pts:
            h = grid.hits(*p)
            if want and any(m == want for m, _ in h):
                bad.append(False); continue
            if r["id"] not in smat:   # 걷는 길은 차길 띠 안에서 일부러 지운다(차길이 덮음) → 덮였으면 정상
                bad.append(not h and bool(want)); continue
            near_main = any(segd(p, (m["knots"][i][0], m["knots"][i][1]), (m["knots"][i + 1][0], m["knots"][i + 1][1])) < m["width"] * .5 + 4
                            for m in mains if m is not r for i in range(len(m["knots"]) - 1))
            if r["kind"] == "main":
                near_main = False
            bad.append(not near_main if want else False)
        runs, i = [], 0
        while i < len(bad):
            if not bad[i]: i += 1; continue
            j = i
            while j < len(bad) and bad[j]: j += 1
            if j - i >= a.min_run: runs.append((pts[i], pts[j - 1], j - i))
            i = j
        lost = sum(x[2] for x in runs)
        if lost:
            lost_roads += 1; total_lost += lost
            rows.append({"id": r["id"], "kind": r["kind"], "len": L, "lost": lost, "runs": [[round(p[0], 1), round(p[1], 1), round(q[0], 1), round(q[1], 1), n] for p, q, n in runs]})
    rows.sort(key=lambda x: -x["lost"])
    res = {"tag": a.tag, "box": box, "roads_checked_len_m": round(total_len), "lost_roads": lost_roads, "lost_m": round(total_lost, 1), "rows": rows}
    out = ROOT / f"docs/qa/evidence/roadv2/pg1501/lost_{a.tag}.json"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"{a.tag}: 사라진 구간 있는 길 {lost_roads}개, 사라진 길이 {total_lost:.0f} m / 검사 {total_len:.0f} m → {out}")
    for x in rows[:25]:
        print(f"  {x['id'][:40]:40s} {x['kind']:8s} {x['lost']:4.0f}/{x['len']:4.0f} m {x['runs'][:3]}")
    if a.png:
        # 위에서 내려다본 그림(북=위). 면: 진회색=큰길 재질, 베이지=마을길, 노랑=걷는 길. 선: 빨강=원본 큰길, 파랑=원본 마을길, 초록=원본 걷는 길. 자홍=사라진 구간
        from PIL import Image, ImageDraw
        s = 8.0; W, H = int((box[2] - box[0]) * s), int((box[3] - box[1]) * s)
        P = lambda x, z: ((x - box[0]) * s, (box[3] - z) * s)
        im = Image.new("RGB", (W, H), (120, 150, 90)); d = ImageDraw.Draw(im)
        col = {"TV6_Main": (70, 70, 72), "TV6_Village": (205, 195, 175)}
        for o in objs:
            for t in o["tris"]:
                d.polygon([P(t[0], t[1]), P(t[2], t[3]), P(t[4], t[5])], fill=col.get(o["mat"], (225, 195, 110)))
        for r in roads:
            d.line([P(k[0], k[1]) for k in r["knots"]], fill={"main": (230, 40, 40), "village": (40, 80, 230)}.get(r["kind"], (40, 160, 40)), width=2)
        for x in rows:
            for p0x, p0z, p1x, p1z, n in x["runs"]:
                for q in (P(p0x, p0z), P(p1x, p1z)): d.ellipse([q[0] - 6, q[1] - 6, q[0] + 6, q[1] + 6], outline=(255, 0, 255), width=3)
        for gx in range(math.ceil(box[0] / 10) * 10, int(box[2]), 10):
            d.text((P(gx, 0)[0] + 2, 2), str(gx), fill=(0, 0, 0))
        for gz in range(math.ceil(box[1] / 10) * 10, int(box[3]), 10):
            d.text((2, P(0, gz)[1]), str(gz), fill=(0, 0, 0))
        d.text((W - 200, 4), a.tag, fill=(0, 0, 0))
        im.save(ROOT / a.png); print(a.png)


if __name__ == "__main__":
    main()
