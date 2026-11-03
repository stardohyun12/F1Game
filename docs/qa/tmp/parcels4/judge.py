"""필지 판 컷 6장을 Astra(codex gpt-6-astra)에게 의도 없이 판정시킨다. ux-reviewer-prompt 형식.
  python -X utf8 docs/qa/tmp/parcels4/judge.py <컷폴더> <접미사 예: _3pan> <출력.txt>
이미지는 익명 이름(1.png..6.png)으로 임시 폴더에 복사."""
import os, shutil, subprocess, sys, tempfile

CUTS = ["dohyun", "8b98", "main_kart", "garage", "top_home", "slope_eye"]
PROMPT = """너는 이 게임 화면을 처음 보는 사람이다. 무엇을 의도했는지 일부러 안 알려준다. 좋게 봐줄 필요 없다 — 결함을 찾는 게 임무다.
첨부 이미지 6장(1~6번)을 각각 본다. 각 장마다 한국어로 짧게:
1. 첫인상 한 줄 — 무엇으로 보이나
2. 가장 깨지거나 어색한 점 3개 — 화면 속 위치와 함께
3. 네 질문에 예/아니오 + 한 줄: (a) 해안가 비탈 마을처럼 보이나 (b) 집이 띄엄띄엄 흩어져 보이나 (c) 아파트·창고 단지 같나 (d) 집이 땅에 어색하게 앉았나(뜸·박힘·기단)
4. 10점 만점 몇 점
마지막에 6장 공통으로 가장 큰 결함 3개를 순위대로."""

src, suf, out = sys.argv[1], sys.argv[2], sys.argv[3]
tmp = tempfile.mkdtemp(prefix="pjudge_")
imgs = []
for i, c in enumerate(CUTS, 1):
    p = os.path.join(tmp, f"{i}.png")
    shutil.copy(os.path.join(src, c + suf + ".png"), p)
    imgs.append(p)
cx = shutil.which("codex.cmd") or shutil.which("codex") or "codex"
cmd = [cx, "exec", "--skip-git-repo-check", "-m", "gpt-6-astra", "-s", "read-only", "-C", tmp.replace("\\", "/")]
for p in imgs:
    cmd += ["-i", p]
cmd += ["-"]
r = subprocess.run(cmd, cwd=tmp, capture_output=True, text=True, encoding="utf-8", timeout=900, input=PROMPT)
with open(out, "w", encoding="utf-8") as fh:
    fh.write((r.stdout or "") + "\n---stderr-tail---\n" + (r.stderr or "")[-1500:])
shutil.rmtree(tmp, ignore_errors=True)
print("done", r.returncode)
