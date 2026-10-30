#!/usr/bin/env python3
"""도현 코멘트(tools/viewer/feedback/*.jsonl) — 팀원 쪽 도구. Unity 기획 창 · 웹 뷰어 코멘트 공통.

  python scripts/fb.py list [--file X] [--all]       할 일(open)만, 대화(thread) 포함. --all = fixed 까지
  python scripts/fb.py fixed <id> "<무엇을 고쳤나 한 줄>" [--after 경로]  판정 게이트(D-204) 통과해야 open→fixed
      --after 없으면 bash scripts/fb_shot.sh <id>(코멘트 때 카메라로 재촬영) → python scripts/fb_judge.py <id>
      판정자 둘(Claude·Astra) 다 NO(문제 안 보임)일 때만 fixed + 판정 요약. 아니면 거부(exit 1), thread 에 "판정: 아직 보임 — 이유"
      코멘트에 공간 단어(묻·뜸·떠·땅·길·겹·박혀·공중·붙)가 있으면 그 전에 scripts/fb_geom.py(계산) — FAIL 이면 거부, 이유 = fix 칸
  python scripts/fb.py reply <id> "<글>" [--shot 경로]                    status 그대로, 글만

status: open(할 일) → fixed(팀원이 고쳤음 — 도현 검토 대기) → done(도현이 [넘기기]).
도현이 [다시] 를 누르면 director 글이 붙고 open 으로 돌아온다. 팀원은 done 으로 바꾸지 않는다.
쓰는 사람 이름 = --who, 없으면 환경변수 PITCREW_ROLE, 없으면 "team".
쓰기는 같은 폴더 tmp 에 쓰고 os.replace (Unity PlanIO.Mutate 와 같은 방식, 같은 줄 형식).
"""
import argparse, datetime, glob, json, os, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FB = os.path.join(ROOT, "tools", "viewer", "feedback")


def now():
    return datetime.datetime.now().astimezone().isoformat(timespec="seconds")


def files(only=None):
    out = sorted(glob.glob(os.path.join(FB, "*.jsonl")))
    if only:
        out = [f for f in out if os.path.basename(f) in (only, only + ".jsonl") or os.path.abspath(f) == os.path.abspath(only)]
        if not out:
            sys.exit("파일 없음: " + only)
    return out


def read(path):
    rows = []
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            if line.strip():
                rows.append(json.loads(line))
    return rows


def thread_of(d):
    """옛 줄(thread 없음) → comment 를 첫 글로, reply/after_shot(옛 맵 형식)을 둘째 글로."""
    if isinstance(d.get("thread"), list):
        return d["thread"]
    t = [{"who": "director", "time": d.get("time"), "text": d.get("comment") or d.get("text"), "screenshot": d.get("screenshot")}]
    if d.get("reply"):
        t.append({"who": d.get("reply_by") or "team", "time": d.get("fixed_time") or d.get("time"), "text": d["reply"], "screenshot": d.get("after_shot")})
    return t


def dump(d):
    return json.dumps(d, ensure_ascii=False, separators=(",", ":"))


def mutate(cid, edit):
    for path in files():
        rows = read(path)
        hit = False
        for d in rows:
            if d.get("id") == cid:
                edit(d)
                hit = True
        if hit:
            tmp = path + ".tmp"
            data = "".join(dump(d) + "\n" for d in rows)
            for k in range(20):   # Unity 기획 창이 2초마다 읽어서 Windows 에서 OSError 22/13 가 가끔 남(10/30) → 재시도
                try:
                    with open(tmp, "w", encoding="utf-8", newline="\n") as fh:
                        fh.write(data)
                    os.replace(tmp, path)
                    return path
                except OSError:
                    import time; time.sleep(0.5)
            sys.exit("쓰기 실패(파일 잠김): " + path)
    sys.exit("id 없음: " + cid)


def push(d, who, text, shot):
    t = thread_of(d)
    p = {"who": who, "time": now(), "text": text}
    if shot:
        p["screenshot"] = shot.replace("\\", "/")
    t.append(p)
    d["thread"] = t


def status_of(d):
    s = d.get("status") or "open"
    return s if s in ("open", "fixed", "done") else "open"


def cmd_list(a):
    n = 0
    for path in files(a.file):
        for d in read(path):
            if d.get("kind") == "placement":
                continue  # [배치] 자동 기록 = 처리 불필요
            st = status_of(d)
            if st == "done" or (st == "fixed" and not a.all):
                continue
            n += 1
            print("─" * 60)
            print(f"[{st}] {d.get('id')}   ({os.path.basename(path)}, {d.get('building') or ''}{' / ' + d['node'] if d.get('node') else ''})")
            if d.get("point"):
                print(f"  point {d['point']}   space {d.get('space', 'gltf')}")
            for p in thread_of(d):
                who = "도현" if p.get("who") == "director" else p.get("who")
                print(f"  {who} {(p.get('time') or '')[5:16].replace('T', ' ')}: {p.get('text')}")
                if p.get("screenshot"):
                    print(f"      스샷 {p['screenshot']}")
    print("─" * 60)
    print(f"{n}건")


def find(cid):
    for path in files():
        for d in read(path):
            if d.get("id") == cid:
                return d
    sys.exit("id 없음: " + cid)


def judge_gate(a):
    """D-204: after 스샷(없으면 fb_shot.sh 로 그때 카메라 재촬영) → fb_judge.py. (통과?, 요약, after 경로)"""
    after = a.after or a.shot
    if not after:
        r = subprocess.run(["bash", os.path.join(ROOT, "scripts", "fb_shot.sh"), a.id], cwd=ROOT,
                           capture_output=True, text=True, encoding="utf-8")
        print(r.stdout.strip())
        if r.returncode != 0:
            sys.exit("fb_shot 실패 — fixed 안 함 (--after <후 캡처> 로 직접 줄 수 있다): " + (r.stdout + r.stderr).strip()[-300:])
        after = os.path.join(FB, "shots", a.id + "_after.png")
    r = subprocess.run([sys.executable, os.path.join(ROOT, "scripts", "fb_judge.py"), a.id, "--after", after], cwd=ROOT,
                       capture_output=True, text=True, encoding="utf-8")
    print(r.stdout.strip())
    jf = os.path.join(FB, "judge", a.id + ".json")
    if not os.path.exists(jf):
        sys.exit("fb_judge 실패 — fixed 안 함: " + (r.stdout + r.stderr).strip()[-300:])
    j = json.load(open(jf, encoding="utf-8"))
    summary = " / ".join(f"{k} {v['verdict'] or '무응답'}: {v['reason']}" for k, v in j["judges"].items())
    return r.returncode == 0 and j.get("pass"), summary, after


SPACE_WORDS = ("묻", "뜸", "떠", "파묻", "땅", "길", "겹", "박혀", "공중", "붙")


def geom_gate(a, d):
    """하네스 ②: 코멘트에 공간 단어가 있으면 fb_geom(계산) 먼저 — VLM 판정자는 묻힘·뜸·겹침에 약하다. FAIL 이면 거부."""
    text = " ".join(p.get("text") or "" for p in thread_of(d) if p.get("who") == "director")
    if not any(w in text for w in SPACE_WORDS) or not d.get("point"):
        return
    sys.path.insert(0, os.path.join(ROOT, "scripts"))
    import fb_geom
    try:
        ok, items, out = fb_geom.run(d["id"])
    except RuntimeError as e:
        if "지형 콜라이더 없음" in str(e):   # 스튜디오 씬(지형 없음) — 땅 검사 대상 아님, 건너뜀(10/30)
            print("fb_geom 건너뜀(지형 없는 씬)")
            return
        sys.exit("fb_geom 조사 실패 — fixed 안 함: " + str(e))
    if ok:
        print("fb_geom PASS →", os.path.relpath(out, ROOT).replace("\\", "/"))
        return
    why = " | ".join(i["fix"] for i in items if i["problem"])
    mutate(a.id, lambda d: push(d, a.who, "판정(계산): 아직 공간 결함 — " + why, None))
    print("거부: fb_geom FAIL — status 그대로(open). 고칠 것:\n  " + why.replace(" | ", "\n  "))
    sys.exit(1)


def cmd_fixed(a):
    d = find(a.id)
    if status_of(d) == "done":
        sys.exit("이미 done(도현이 넘김) — 바꾸지 않음: " + a.id)
    geom_gate(a, d)
    ok, summary, after = judge_gate(a)
    rel = os.path.relpath(os.path.abspath(after), ROOT).replace("\\", "/")
    if not ok:
        mutate(a.id, lambda d: push(d, a.who, "판정: 아직 보임 — " + summary, rel))
        print("거부: 판정 통과 못 함 — status 그대로(open). 이유:\n  " + summary)
        sys.exit(1)

    def edit(d):
        push(d, a.who, a.text + "  [판정 통과 — " + summary + "]", rel)
        d["status"] = "fixed"
    print("fixed →", mutate(a.id, edit))


def cmd_reply(a):
    print("reply →", mutate(a.id, lambda d: push(d, a.who, a.text, a.shot)))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("list"); p.add_argument("--file"); p.add_argument("--all", action="store_true"); p.set_defaults(fn=cmd_list)
    for name, fn in (("fixed", cmd_fixed), ("reply", cmd_reply)):
        p = sub.add_parser(name); p.add_argument("id"); p.add_argument("text"); p.add_argument("--shot")
        if name == "fixed": p.add_argument("--after")
        p.add_argument("--who", default=os.environ.get("PITCREW_ROLE") or "team"); p.set_defaults(fn=fn)
    a = ap.parse_args()
    a.fn(a)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
