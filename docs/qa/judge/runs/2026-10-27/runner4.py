import json, os, sys
from runner2 import S, run, MODEL
from concurrent.futures import ThreadPoolExecutor
# usage: python runner4.py <model> <variant: p2|p2c>
V = sys.argv[2] if len(sys.argv) > 2 else "p2c"
P = open(S + ("p2_pair.txt" if V == "p2" else "p2c_pair_picks.txt"), encoding="utf-8").read()
pairs = json.load(open(S + "pairs.json"))
jobs = []
for p, n in pairs:
    jobs.append((f"{V}_{p}_{n}", P.replace("{L}", p).replace("{R}", n)))
    jobs.append((f"{V}_{n}_{p}", P.replace("{L}", n).replace("{R}", p)))
with ThreadPoolExecutor(int(os.environ.get("PAR", "6"))) as ex:
    for name, st in ex.map(run, jobs):
        print(name, st, flush=True)
print("DONE", len(jobs))
