#!/usr/bin/env python3
"""도현 코멘트(tools/viewer/feedback/*.jsonl) — 팀원 쪽 도구. Unity 기획 창 · 웹 뷰어 코멘트 공통.

  python scripts/fb.py list [--file X] [--all]       할 일(open)만, 대화(thread) 포함. --all = fixed 까지
  python scripts/fb.py fixed <id> "<무엇을 고쳤나 한 줄>" [--shot 경로]   status open→fixed + 내 글(후 스샷)
  python scripts/fb.py reply <id> "<글>" [--shot 경로]                    status 그대로, 글만

status: open(할 일) → fixed(팀원이 고쳤음 — 도현 검토 대기) → done(도현이 [넘기기]).
도현이 [다시] 를 누르면 director 글이 붙고 open 으로 돌아온다. 팀원은 done 으로 바꾸지 않는다.
쓰는 사람 이름 = --who, 없으면 환경변수 PITCREW_ROLE, 없으면 "team".
쓰기는 같은 폴더 tmp 에 쓰고 os.replace (Unity PlanIO.Mutate 와 같은 방식, 같은 줄 형식).
"""
import argparse, datetime, glob, json, os, sys

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
            with open(tmp, "w", encoding="utf-8", newline="\n") as fh:
                fh.write("".join(dump(d) + "\n" for d in rows))
            os.replace(tmp, path)
            return path
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


def cmd_fixed(a):
    def edit(d):
        if status_of(d) == "done":
            sys.exit("이미 done(도현이 넘김) — 바꾸지 않음: " + a.id)
        push(d, a.who, a.text, a.shot)
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
        p.add_argument("--who", default=os.environ.get("PITCREW_ROLE") or "team"); p.set_defaults(fn=fn)
    a = ap.parse_args()
    a.fn(a)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
