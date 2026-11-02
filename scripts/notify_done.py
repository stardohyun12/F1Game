"""팀원 과제가 끝나면(커밋/보류) 도현 스레드로 바로 알림 — "끝났는데 말 안 함" 방지 (D-247).
사용: python -X utf8 scripts/notify_done.py <line> <NAME> <ok|hold> [commit]
- 과제 첫 줄 제목, 팀원 최종 메시지(.out 끝) 요약, 이번 커밋의 비교 그림 1장(compare/board/sheet 우선)을 보냄.
- 대기열이 비면 "다음 과제 없음 — PD 판단 필요"도 붙임."""
import sys, subprocess, pathlib, re, glob
line, name, status = sys.argv[1:4]
commit = sys.argv[4] if len(sys.argv) > 4 else ""
root = pathlib.Path("D:/Github/F1Game"); q = root / ".claude/team/queue" / line
task = q / "done" / f"{name}.task"; out = q / "done" / f"{name}.out"
title = ""
if task.exists():
    m = re.search(r"\]\[?([^\[\]]{4,80})\]", task.read_text(encoding="utf-8", errors="ignore").splitlines()[0] + "]")
    first = task.read_text(encoding="utf-8", errors="ignore").splitlines()[0]
    gs = [g for g in re.findall(r"\[([^\]]+)\]", first) if g not in ("EDITOR", "OPUS", "NOLIMIT", "research", "pd")]
    title = (gs[0] if gs else name)[:80]
summ = ""
if out.exists():
    t = out.read_text(encoding="utf-8", errors="ignore")
    t = t.split("ok=True", 1)[-1] if "ok=True" in t else t
    lines = [l.strip() for l in t.splitlines() if l.strip() and not l.startswith("[")]
    summ = "\n".join(lines[:8])[:900]
img = ""
if commit:
    files = subprocess.run(["git", "-C", str(root), "show", "--name-only", "--pretty=", commit], capture_output=True, text=True, encoding="utf-8").stdout.split()
    pngs = [f for f in files if f.endswith(".png") and f.startswith("docs/qa/evidence")]
    pref = [f for f in pngs if re.search(r"compare|board|sheet|cmp|pair", f)]
    pick = (pref or pngs)
    if pick: img = str(root / pick[0])
nxt = sorted(glob.glob(str(q / "*.task")))
tail = f"\n다음: {pathlib.Path(nxt[0]).stem} 자동 시작" if nxt else "\n대기열 비었음 — PD 가 결과 확인하고 이어서 정리해 보낼게요."
head = f"✅ 끝남 {commit} — {title}" if status == "ok" else f"⚠️ 보류(커밋 안 함) — {title}"
msg = f"{head}\n\n{summ}{tail}"
subprocess.run([sys.executable, "-X", "utf8", str(root / "scripts/tg_notify.py"), msg] + ([img] if img else []))
# D-256 도현이 한 번에 확인: 끝난 과제를 묶음 문서에 쌓음(제목·커밋·요약·그림·Play 순서는 보고서에)
# D-264 BATCH 한 장: 표 한 줄(그림 1장 + 첫 요약 한 줄 + 판정 칸)
rv = root / "docs/review/BATCH.md"
rel = (pathlib.Path(img).relative_to(root).as_posix() if img else "")
one = (summ.splitlines() or [""])[0][:120].replace("|", "/")
with open(rv, "a", encoding="utf-8") as f:
    pic = ("![](../../" + rel + ")") if rel else "(그림 없음)"
    mark = "⬜" if status == "ok" else "⚠️ 보류"
    f.write(f"| + | {title} ({commit}) | {pic} | {one} | {mark} |" + chr(10))
