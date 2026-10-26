import json, subprocess, sys, os, random, itertools
from concurrent.futures import ThreadPoolExecutor
S = "C:/Users/rtx40/AppData/Local/hermes/cache/scratch/judge/run/"
os.makedirs(S + "out", exist_ok=True)
A = json.load(open("D:/Github/F1Game/docs/qa/judge/anchors.json", encoding="utf-8"))
pos = [a["id"] for a in A["positive"]]; neg = [a["id"] for a in A["negative"]]
P1 = open(S + "p1_score.txt", encoding="utf-8").read()
P2 = open(S + "p2_pair.txt", encoding="utf-8").read()
P3 = open(S + "p3_defect.txt", encoding="utf-8").read()
MODEL = sys.argv[1] if len(sys.argv) > 1 else "sonnet"
ONLY = sys.argv[2] if len(sys.argv) > 2 else "all"

jobs = []
for i in pos + neg:
    jobs.append((f"p1_{i}", P1.replace("{ID}", i)))
    jobs.append((f"p3_{i}", P3.replace("{ID}", i)))
# pairs: each pos with 4 negs (round robin), both orders
random.seed(7)
nq = itertools.cycle(random.sample(neg, len(neg)))
pairs = []
for p in pos:
    for _ in range(4):
        pairs.append((p, next(nq)))
json.dump(pairs, open(S + "pairs.json", "w"))
for p, n in pairs:
    jobs.append((f"p2_{p}_{n}", P2.replace("{L}", p).replace("{R}", n)))
    jobs.append((f"p2_{n}_{p}", P2.replace("{L}", n).replace("{R}", p)))
if ONLY != "all":
    jobs = [j for j in jobs if j[0].startswith(ONLY)]

def run(job):
    name, prompt = job
    out = S + f"out/{MODEL}_{name}.txt"
    if os.path.exists(out) and os.path.getsize(out) > 20:
        return name, "skip"
    try:
        r = subprocess.run(["claude", "-p", prompt, "--model", MODEL, "--setting-sources", "local",
                            "--no-session-persistence", "--disable-slash-commands",
                            "--allowedTools", "Read", "--max-turns", "10"],
                           cwd=S, capture_output=True, text=True, encoding="utf-8", timeout=400, shell=False)
        open(out, "w", encoding="utf-8").write(r.stdout + ("\n[stderr]" + r.stderr if r.returncode else ""))
        return name, r.returncode
    except Exception as e:
        return name, repr(e)

with ThreadPoolExecutor(int(os.environ.get("PAR", "8"))) as ex:
    for name, st in ex.map(run, jobs):
        print(name, st, flush=True)
print("DONE", len(jobs))
