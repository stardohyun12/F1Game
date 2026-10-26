import json, os, sys
sys.argv = sys.argv[:2]
from runner2 import S, pos, neg, calib, fmt, run, MODEL
from concurrent.futures import ThreadPoolExecutor
P1 = open(S + "p1_score.txt", encoding="utf-8").read()
P3 = open(S + "p3_defect.txt", encoding="utf-8").read()
BLOCK = """
이 게임 디렉터의 눈 — 과거 판정 예시(먼저 다 열어 본다):
디렉터가 받아들인 것:
{POS}
디렉터가 거부한 것:
{NEG}
→ 이 예시들에서 디렉터가 무엇을 받아들이고 무엇을 싫어하는지 먼저 두 줄로 정리하고, 그 눈으로 판정한다.
  (형식 차이 — 게임 캡처인지 에셋 시트인지 — 는 판단 근거가 아니다.)
"""
jobs = []; meta = {}
for k, i in enumerate(pos + neg):
    cp, cn = calib([i], 100 + k); meta[i] = (cp, cn)
    blk = BLOCK.replace("{POS}", fmt(cp)).replace("{NEG}", fmt(cn))
    for tag, P in (("p1b", P1), ("p3b", P3)):
        body = P.replace("{ID}", i)
        cut = body.index("판정 대상")
        jobs.append((f"{tag}_{i}", body[:cut] + blk + "\n" + body[cut:]))
json.dump(meta, open(S + "p13b_calib.json", "w"), indent=0)
with ThreadPoolExecutor(int(os.environ.get("PAR", "6"))) as ex:
    for name, st in ex.map(run, jobs):
        print(name, st, flush=True)
print("DONE", len(jobs))
