#!/usr/bin/env python3
"""D-204 하네스 ② 공간 결함 계산형 센서 — VLM 판정자가 약한 묻힘·뜸·겹침을 숫자로 판정한다.

  python -X utf8 scripts/fb_geom.py <코멘트id(앞부분 가능)> [--radius 25] [--dry-offset 이름=dy]

코멘트 jsonl 의 point(unity-world) 반경 안을 열린 에디터(ucli exec, 읽기만)로 조사:
  - 건물 루트(*Buildings 묶음의 자식, TownV6 밑 프리팹 루트): 발자국 네 모서리+중앙 → 지형까지(아래로 레이캐스트)
      바닥이 지형보다 0.10m 넘게 아래 = 묻힘, 위 = 뜸
  - 길(ROAD_*): 메시 경계 변을 1m 간격 → 길 높이-지형 높이. |차|>0.15m 비율 >10% 또는 지형이 길을 덮은 점(차<-0.02m) >0 이면 실패
  - 겹침: 건물끼리 월드 AABB 0.2m 넘게, 길 정점이 건물 발자국 안
결과 tools/viewer/feedback/geom/<id>.json {pass, items[{name, problem, value, fix}]}. exit 0=PASS 1=FAIL 2=조사 실패.
--dry-offset: 그 루트만 메모리에서 y+dy 한 셈으로 계산(씬은 안 바뀜) — 센서가 FAIL 을 잡는지 확인용.
"""
import argparse, glob, json, os, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FB = os.path.join(ROOT, "tools", "viewer", "feedback")
TOL_B, TOL_R, BAD_RATIO = 0.10, 0.15, 0.10


def find(q):
    hits = []
    for f in sorted(glob.glob(os.path.join(FB, "*.jsonl"))):
        for line in open(f, encoding="utf-8"):
            if line.strip():
                d = json.loads(line)
                if d.get("id") == q or q in (d.get("id") or ""):
                    hits.append(d)
    if len(hits) != 1:
        sys.exit(f"id 가 {len(hits)}건 맞음: {q}")
    return hits[0]


def probe(d, radius, dry):
    p = d.get("point") or d.get("pos")
    if not p or (d.get("space") or "unity-world") != "unity-world":
        raise RuntimeError("world 좌표(point, space=unity-world) 없음")
    name, dy = (dry.split("=", 1) + ["0"])[:2] if dry else ("", "0")
    cs = open(os.path.join(ROOT, "scripts", "fb_geom.cs.txt"), encoding="utf-8").read()
    for k, v in {"__PX__": p[0], "__PY__": p[1], "__PZ__": p[2], "__RAD__": radius, "__SCENE__": d.get("scene") or "",
                 "__DRYNAME__": name, "__DRYDY__": float(dy)}.items():
        cs = cs.replace(k, str(v))
    proj = "D:/Github/F1Game/F1Studio" if d.get("scene") == "Studio_Building" else "D:/Github/F1Game/F1"
    out = ucli_exec(proj, cs)
    if out.startswith("FAIL") or "\nT\t" not in "\n" + out:
        raise RuntimeError(out.strip()[-400:])
    return [l.split("\t") for l in out.splitlines() if "\t" in l]


def ucli_exec(proj, code):
    """ucli-http.sh exec 와 같은 HTTP 호출을 직접. python→bash→ucli-http.sh 는 이 머신에서 0xC0000005 로 죽는다(2026-10-29 실측)."""
    import urllib.request
    port = None
    for f in glob.glob(os.path.expanduser("~/.unity-cli/instances/*.json")):
        try:
            j = json.load(open(f, encoding="utf-8"))
        except Exception:
            continue
        if j.get("projectPath", "").replace("\\", "/").rstrip("/").lower() == proj.lower() and j.get("state") != "stopped":
            port = j["port"]; break
    if not port:
        raise RuntimeError(f"Editor connector 없음({proj}) — 에디터가 꺼져 있다")
    body = json.dumps({"command": "exec", "params": {"code": code}}).encode()
    req = urllib.request.Request(f"http://127.0.0.1:{port}/command", data=body, headers={"Content-Type": "application/json"})
    try:
        d = json.loads(urllib.request.urlopen(req, timeout=280).read())
    except Exception as e:
        raise RuntimeError(f"ucli 요청 실패: {e}")
    if not d.get("success"):
        raise RuntimeError("ucli: " + str(d.get("message"))[-400:])
    out = d.get("data")
    return out if isinstance(out, str) else json.dumps(out, ensure_ascii=False)


def judge(rows):
    items, B = [], {}
    for r in rows:
        if r[0] == "B":
            B.setdefault(r[1], []).append(r)
    for name, cs in B.items():
        root_y = float(cs[0][7])
        pairs = [(c[2], float(c[5]), float(c[6])) for c in cs if c[6] != "nan"]
        if not pairs:
            items.append({"name": name, "problem": "지형 없음", "value": "모서리 아래 지형 레이캐스트 0", "fix": f"{name} 이 지형 밖 — 위치 확인"})
            continue
        floor = pairs[0][1]
        diffs = [(lab, round(fy - gy, 3)) for lab, fy, gy in pairs]
        gmin, gmax = min(g for _, _, g in pairs), max(g for _, _, g in pairs)
        val = ", ".join(f"{lab} {v:+.2f}" for lab, v in diffs)
        worst_low, worst_high = min(v for _, v in diffs), max(v for _, v in diffs)
        ok = True
        if worst_low < -TOL_B:
            ok = False
            items.append({"name": name, "problem": "묻힘", "value": f"바닥 {floor:.2f}, 지형 {gmin:.2f}~{gmax:.2f} (바닥-지형: {val})",
                          "fix": f"{name} 루트 y 를 {root_y + gmax - floor:.2f} 로 / 또는 발밑 테라스를 {floor:.2f} 로 평평하게 (지금 {gmin:.2f}~{gmax:.2f})"})
        if worst_high > TOL_B:
            ok = False
            items.append({"name": name, "problem": "뜸", "value": f"바닥 {floor:.2f}, 지형 {gmin:.2f}~{gmax:.2f} (바닥-지형: {val})",
                          "fix": f"{name} 발밑 테라스를 {floor:.2f} 로 메우기 / 또는 루트 y 를 {root_y - (floor - gmin):.2f} 로(높은 쪽 {gmax - gmin:.2f}m 묻힘 주의)"})
        if ok:
            items.append({"name": name, "problem": None, "value": f"바닥-지형 {worst_low:+.2f}~{worst_high:+.2f}", "fix": None})
    for r in rows:
        if r[0] == "R":
            name, n, bad, cov, worst = r[1], int(r[2]), int(r[3]), int(r[4]), float(r[5])
            ratio = bad / n
            val = f"가장자리 {n}점, |차|>{TOL_R} {bad}점({ratio:.0%}), 지형에 덮임 {cov}점, 최악 {worst:+.2f}m @({float(r[6]):.1f},{float(r[8]):.1f})"
            if cov > 0 or ratio > BAD_RATIO:
                fix = (f"{name} @({float(r[6]):.1f},{float(r[8]):.1f}) 근처 지형을 길밑 -0.15 로 내리기(빌더 길 깎기)" if cov > 0
                       else f"{name} 가장자리 어깨 높이를 지형에 맞추기(최악 {worst:+.2f}m @({float(r[6]):.1f},{float(r[8]):.1f}))")
                items.append({"name": name, "problem": "길-지형 높이차", "value": val, "fix": fix})
            else:
                items.append({"name": name, "problem": None, "value": val, "fix": None})
        elif r[0] == "O":
            items.append({"name": f"{r[1]} × {r[2]}", "problem": "건물 겹침", "value": f"{float(r[3]):.2f} × {float(r[4]):.2f} m",
                          "fix": f"{r[2]} 를 {min(float(r[3]), float(r[4])):.2f}m 이상 떼기"})
        elif r[0] == "RB":
            items.append({"name": f"{r[1]} × {r[2]}", "problem": "길-건물 겹침", "value": f"길 정점 {r[3]}개가 발자국 안",
                          "fix": f"{r[2]} 를 길 밖으로 옮기거나 {r[1]} 를 우회"})
    return items


def run(cid, radius=25.0, dry=None):
    """fb.py 에서도 부른다. (pass, items, json 경로) — 조사 실패면 RuntimeError."""
    d = find(cid)
    items = judge(probe(d, radius, dry))
    res = {"id": d["id"], "point": d.get("point"), "radius": radius, "dry_offset": dry,
           "pass": all(i["problem"] is None for i in items), "items": items}
    os.makedirs(os.path.join(FB, "geom"), exist_ok=True)
    out = os.path.join(FB, "geom", d["id"] + (".dry" if dry else "") + ".json")
    with open(out, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(res, fh, ensure_ascii=False, indent=1)
    return res["pass"], items, out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("id"); ap.add_argument("--radius", type=float, default=25.0); ap.add_argument("--dry-offset")
    a = ap.parse_args()
    try:
        ok, items, out = run(a.id, a.radius, a.dry_offset)
    except RuntimeError as e:
        print("geom 조사 실패:", e); sys.exit(2)
    for i in items:
        print(("  ok  " if i["problem"] is None else "  FAIL ") + f"{i['name']}: {i['problem'] or ''} {i['value']}" + (f"\n        고치기: {i['fix']}" if i["fix"] else ""))
    print(("PASS" if ok else "FAIL") + " → " + os.path.relpath(out, ROOT).replace("\\", "/"))
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
