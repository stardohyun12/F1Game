#!/usr/bin/env python3
"""D-204 ① 고침 판정 게이트 — 도현이 지적한 화면(before) | 고친 뒤(after) 를 나란히 놓고 판정자 2명에게 묻는다.

  python scripts/fb_judge.py <id> [--after 경로]     (id 앞부분만 줘도 됨)
  after 기본 = tools/viewer/feedback/shots/<id>_after.png (scripts/fb_shot.sh 가 만든다)
판정자: Claude(`claude -p --model sonnet`, 설정 끔·Read 만) · Astra(`codex exec -m gpt-6-astra -i <png>`)
  — 호출 방식은 docs/qa/judge/judge_pair.py 와 같다. 이미지는 익명 이름으로 임시 폴더에 복사(경로·id 비공개).
결과: tools/viewer/feedback/judge/<id>.json. 둘 다 NO(문제 안 보임) 면 exit 0, 아니면 exit 1(판정 실패·무응답 포함).
"""
import argparse, datetime, glob, json, os, re, shutil, subprocess, sys, tempfile
from concurrent.futures import ThreadPoolExecutor
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FB = os.path.join(ROOT, "tools", "viewer", "feedback")

PROMPT = """너는 게임 화면 검수자다. {img} 를 본다(한 장에 두 화면이 나란히 있다).
왼쪽 = 게임 감독이 문제를 지적한 화면(빨간 선은 감독이 그린 표시일 수 있다). 오른쪽 = 같은 카메라로 고친 뒤 다시 찍은 화면.
감독이 처음 한 말: "{comment}"
{last}
질문: 감독이 말한 문제가 오른쪽 화면에도 아직 보이나?
화면 전체 인상이 아니라 감독이 말한 그 문제만 본다. 고친 흔적이 없거나, 문제 대상이 그냥 사라져서 확인이 안 되면 YES 로 본다.
답 형식(정확히 두 줄): 첫 줄 YES 또는 NO, 둘째 줄 이유 한 줄(한국어).
"""


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


def director_lines(d):
    """처음 한 말 + thread 의 마지막 도현 글(처음 말과 다를 때만)."""
    first = d.get("comment") or d.get("text") or ""
    last = None
    for p in d.get("thread") or []:
        if p.get("who") == "director" and p.get("text") and p.get("text") != first:
            last = p["text"]
    return first, last


def side_by_side(before, after, out):
    a = Image.open(before).convert("RGB"); b = Image.open(after).convert("RGB")
    if b.size != a.size: b = b.resize(a.size)
    im = Image.new("RGB", (a.width * 2 + 12, a.height), (255, 255, 255))
    im.paste(a, (0, 0)); im.paste(b, (a.width + 12, 0))
    im.save(out)


def parse(txt):
    for line in (txt or "").splitlines():
        s = line.strip().strip("*").strip()
        m = re.match(r"^(YES|NO)\b", s, re.I)
        if m:
            rest = [l.strip() for l in txt.splitlines()[txt.splitlines().index(line) + 1:] if l.strip()]
            return m.group(1).upper(), (rest[0] if rest else "")
    return None, (txt or "").strip()[-200:]


def ask_claude(tmp, prompt):
    r = subprocess.run(["claude", "-p", prompt.replace("{img}", "pair.png 파일(Read 로 연다)"), "--model", "sonnet",
                        "--setting-sources", "local", "--no-session-persistence", "--disable-slash-commands",
                        "--allowedTools", "Read", "--max-turns", "6"],
                       cwd=tmp, capture_output=True, text=True, encoding="utf-8", timeout=500)
    return r.stdout or r.stderr


def ask_astra(tmp, prompt):
    cx = shutil.which("codex.cmd") or shutil.which("codex") or "codex"
    cmd = [cx, "exec", "--skip-git-repo-check", "-m", "gpt-6-astra", "-s", "read-only", "-C", tmp.replace("\\", "/"),
           "-i", os.path.join(tmp, "pair.png"), "-"]
    r = subprocess.run(cmd, cwd=tmp, capture_output=True, text=True, encoding="utf-8", timeout=500,
                       input=prompt.replace("{img}", "첨부 이미지"))
    out = r.stdout or r.stderr
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("id"); ap.add_argument("--after")
    a = ap.parse_args()
    d = find(a.id); cid = d["id"]
    if not d.get("screenshot"):
        sys.exit("원본 스샷 없음 — 판정 불가: " + cid)
    before = os.path.join(FB, d["screenshot"])
    after = os.path.abspath(a.after) if a.after else os.path.join(FB, "shots", cid + "_after.png")
    if not os.path.exists(after):
        sys.exit("after 없음: " + after + "  (bash scripts/fb_shot.sh " + cid + ")")
    first, last = director_lines(d)
    prompt = PROMPT.replace("{comment}", first).replace("{last}", f'감독이 마지막에 덧붙인 말: "{last}"' if last else "")
    tmp = tempfile.mkdtemp(prefix="fbjudge_")
    try:
        side_by_side(before, after, os.path.join(tmp, "pair.png"))
        with ThreadPoolExecutor(2) as ex:
            fc, fa = ex.submit(ask_claude, tmp, prompt), ex.submit(ask_astra, tmp, prompt)
            raw = {"claude": fc.result(), "astra": fa.result()}
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    res = {}
    for k, t in raw.items():
        v, why = parse(t)
        res[k] = {"verdict": v, "reason": why}
    passed = all(r["verdict"] == "NO" for r in res.values())
    out = {"id": cid, "time": datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
           "before": d["screenshot"], "after": os.path.relpath(after, FB).replace("\\", "/"),
           "comment": first, "last_director": last, "judges": res, "pass": passed}
    os.makedirs(os.path.join(FB, "judge"), exist_ok=True)
    with open(os.path.join(FB, "judge", cid + ".json"), "w", encoding="utf-8") as fh:
        json.dump(out, fh, ensure_ascii=False, indent=1)
    for k, r in res.items():
        print(f"{k}: {r['verdict'] or '무응답'} — {r['reason']}")
    print("판정:", "통과(둘 다 NO)" if passed else "아직 보임/판정 실패")
    sys.exit(0 if passed else 1)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
