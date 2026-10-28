"""도현 눈 예측 판정기 — A/B 쌍대비교(p2c), 양 순서.

사용:
  python docs/qa/judge/judge_pair.py NEW.png OLD.png            # 두 컷 비교 (양 순서)
  python docs/qa/judge/judge_pair.py NEW.png --gate             # 게이트: 부정 앵커 3장과 차례로 비교
  python docs/qa/judge/judge_pair.py --qualify [--model sonnet] # 판정자 자격 시험(앵커 28쌍 + 상대 2쌍)
판정자는 `claude -p`(Read 만 허용, 사용자/프로젝트 설정 끔 → 도현 반응·의도 비공개).
이미지는 익명 이름(C1/C2, X..)으로 임시 폴더에 복사한다. 경로·파일명이 판정자에게 안 간다.
"""
import json, os, re, shutil, subprocess, sys, tempfile
from concurrent.futures import ThreadPoolExecutor
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
PROMPT = open(os.path.join(HERE, "prompts", "p2c_pair_picks.txt"), encoding="utf-8").read()
ANCH = json.load(open(os.path.join(HERE, "anchors.json"), encoding="utf-8"))
GATE_NEG = ["X35", "X32", "X65"]  # v5 정비소 장면 · Opus v2 시트 · Look4 거리 — 판정자가 가장 자주 속은 부정 앵커


def prep(tmp, items):
    """items: {anon_id: (path, crop)} → tmp/img/<id>.png"""
    os.makedirs(os.path.join(tmp, "img"), exist_ok=True)
    for t, name in zip(ANCH["targets"], ["REF1", "REF6", "REF12"]):
        im = Image.open(os.path.join(REPO, t["path"])).convert("RGB"); im.thumbnail((1280, 1280))
        im.save(os.path.join(tmp, "img", name + ".png"))
    for k, (p, crop) in items.items():
        im = Image.open(p if os.path.isabs(p) else os.path.join(REPO, p)).convert("RGB")
        if crop: im = im.crop(tuple(crop))
        im.thumbnail((1600, 1600)); im.save(os.path.join(tmp, "img", k + ".png"))


def ask(tmp, L, R, model):
    if model.startswith("gpt") or model.startswith("codex"):
        # 두 번째 판정 가족(Astra = Codex CLI). --model 뒤 값이 gpt*·codex* 면 codex exec 로.
        cx_model = "gpt-6-astra" if model == "codex" else model
        cx = shutil.which("codex.cmd") or shutil.which("codex") or "codex"
        # Codex 는 Read 로 이미지를 못 보니 -i 로 첨부(순서: REF들 → L → R). 프롬프트에 첨부 순서를 알림.
        imgs = [os.path.join(tmp, "img", n + ".png") for n in [t for t in ("REF1", "REF6", "REF12") if os.path.exists(os.path.join(tmp, "img", t + ".png"))] + [L, R]]
        names = [os.path.basename(p)[:-4] for p in imgs]
        note = "\n\n[첨부 이미지 순서: " + ", ".join(f"{i+1}={n}" for i, n in enumerate(names)) + " — Read 대신 첨부 이미지를 본다]"
        cmd = [cx, "exec", "--skip-git-repo-check", "-m", cx_model, "-s", "read-only", "-C", tmp.replace("\\", "/")]
        for p in imgs: cmd += ["-i", p]
        cmd += ["--", PROMPT.replace("{L}", L).replace("{R}", R) + note]
        r = subprocess.run(cmd, cwd=tmp, capture_output=True, text=True, encoding="utf-8", timeout=500, stdin=subprocess.DEVNULL)
        m = re.findall(r"PICK:\s*([LR])", r.stdout)
        return (m[-1] if m else None), r.stdout
    r = subprocess.run(["claude", "-p", PROMPT.replace("{L}", L).replace("{R}", R), "--model", model,
                        "--setting-sources", "local", "--no-session-persistence", "--disable-slash-commands",
                        "--allowedTools", "Read", "--max-turns", "12"],
                       cwd=tmp, capture_output=True, text=True, encoding="utf-8", timeout=500)
    m = re.findall(r"PICK:\s*([LR])", r.stdout)
    return (m[-1] if m else None), r.stdout


def duel(tmp, a, b, model):
    """a 가 양 순서 모두 이기면 'a', 모두 지면 'b', 갈리면 'tie'."""
    with ThreadPoolExecutor(2) as ex:
        (p1, t1), (p2, t2) = ex.map(lambda lr: ask(tmp, lr[0], lr[1], model), [(a, b), (b, a)])
    w1 = a if p1 == "L" else b; w2 = b if p2 == "L" else a
    return (w1 if w1 == w2 else "tie"), [t1, t2]


def anchor_items():
    out = {}
    for a in ANCH["positive"] + ANCH["negative"] + ANCH.get("relative", []):
        out[a["id"]] = (a["path"], a.get("crop"))
    return out


def main():
    args = sys.argv[1:]
    model = "sonnet"
    if "--model" in args:
        i = args.index("--model"); model = args[i + 1]; del args[i:i + 2]
    global PROMPT
    if "--refs156" in args:   # 과제 1046: 색 판정용 — ⑫ Crow Country(어두운 공포) 자리에 ⑤ Wheel World. 자격 시험은 ①⑥⑫ 로만 했다
        args.remove("--refs156")
        PROMPT = open(os.path.join(HERE, "prompts", "p2c_pair_picks_156.txt"), encoding="utf-8").read()
        ANCH["targets"][2] = dict(ANCH["targets"][2], path="art/reference/dotlp-2026-10-27/wheel-world_1.jpg")
    if "--refs16" in args:    # 과제 1058: 기준 ①·⑥ 두 장만(⑫ 뺌). 자격 시험은 ①⑥⑫ 로만 했다
        args.remove("--refs16")
        PROMPT = open(os.path.join(HERE, "prompts", "p2c_pair_picks_16.txt"), encoding="utf-8").read()
        del ANCH["targets"][2:]
    tmp = tempfile.mkdtemp(prefix="judge_")
    try:
        if args and args[0] == "--qualify":
            items = anchor_items(); prep(tmp, items)
            pairs = [tuple(p) for p in ANCH["qualify_pairs"]]  # (긍정·더 나은 쪽, 부정·더 나쁜 쪽)
            with ThreadPoolExecutor(4) as ex:
                res = list(ex.map(lambda pq: duel(tmp, pq[0], pq[1], model)[0], pairs))
            ok = sum(r == p for r, (p, n) in zip(res, pairs))
            print(f"[{model}] 양 순서 모두 맞힘 {ok}/{len(pairs)} = {ok/len(pairs):.0%}  (자격: ≥70%)")
            for r, (p, n) in zip(res, pairs):
                if r != p: print("  틀림/갈림:", p, "vs", n, "→", r)
            return
        new = args[0]
        items = {"C1": (os.path.abspath(new), None)}
        if "--gate" in args:
            ai = anchor_items()
            for g in GATE_NEG: items[g] = ai[g]
            prep(tmp, items)
            wins = losses = 0
            for g in GATE_NEG:
                w, _ = duel(tmp, "C1", g, model); wins += w == "C1"; losses += w == g
                print(f"새 컷 vs 부정 앵커 {g}: {'승' if w == 'C1' else ('갈림' if w == 'tie' else '패')}")
            print("게이트:", "통과 → 도현에게" if wins == len(GATE_NEG) else
                  ("돌려보냄" if losses else "갈림 → 도현 30초 판정"))
        else:
            items["C2"] = (os.path.abspath(args[1]), None); prep(tmp, items)
            w, texts = duel(tmp, "C1", "C2", model)
            print({"C1": f"{args[0]} 가 양 순서 모두 이김", "C2": f"{args[1]} 가 양 순서 모두 이김",
                   "tie": "순서에 따라 갈림 = 판정 불가(도현에게)"}[w])
            for t in texts: print("----\n" + t.strip()[-600:])
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


if __name__ == "__main__":
    main()
