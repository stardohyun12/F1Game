#!/usr/bin/env python
"""새 도현 코멘트 알림 — 마지막으로 본 시각 이후 들어온 코멘트·[다시] 글을 보여 준다.
  python -X utf8 scripts/fb_new.py          # 새 것만 출력 + 본 시각 갱신
  python -X utf8 scripts/fb_new.py --peek   # 갱신 없이 보기
PD 는 매 보고 전에 돌린다(10/30 도현: "스튜디오에 아까 문제 입력했는데 지금까지 안 보고 있었다").
"""
import glob, json, os, sys
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SEEN = os.path.join(ROOT, ".claude/team/fb_seen.txt")
last = open(SEEN, encoding="utf-8").read().strip() if os.path.exists(SEEN) else "2026-10-30T00:00"
newest = last
out = []
for f in glob.glob(os.path.join(ROOT, "tools/viewer/feedback/*.jsonl")):
    for l in open(f, encoding="utf-8"):
        if not l.strip():
            continue
        d = json.loads(l)
        if (d.get("comment") or "").startswith("[배치]"):
            continue
        posts = [("comment", d.get("time", ""), d.get("comment") or "")]
        posts += [("다시", t.get("time", ""), t.get("text", "")) for t in d.get("thread", []) if t.get("who") == "director"]
        for kind, t, text in posts:
            if t[:19] > last[:19] and text and not (kind == "다시" and text == d.get("comment")):
                out.append((t, os.path.basename(f), d["id"][-4:], d.get("status") or "open", kind, text))
            newest = max(newest, t[:19])
for t, f, i, st, kind, text in sorted(out):
    print(f"{t[5:16]} {f.replace('.jsonl','')} {i} [{st}] {kind}: {text[:120]}")
if not out:
    print(f"새 코멘트 없음 (since {last[5:16]})")
if "--peek" not in sys.argv:
    open(SEEN, "w", encoding="utf-8").write(newest)
