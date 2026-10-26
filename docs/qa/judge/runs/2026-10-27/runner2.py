import json, subprocess, sys, os, random
from concurrent.futures import ThreadPoolExecutor
S = "C:/Users/rtx40/AppData/Local/hermes/cache/scratch/judge/run/"
A = json.load(open("D:/Github/F1Game/docs/qa/judge/anchors.json", encoding="utf-8"))
Q = {a["id"]: a["quote"] for a in A["positive"] + A["negative"]}
pos = [a["id"] for a in A["positive"]]; neg = [a["id"] for a in A["negative"]]
# near-duplicate groups: never show a sibling of a test item as calibration
GROUPS = [{"X16", "X26"}, {"X62", "X22"}, {"X29", "X86"}, {"X42", "X59", "X32", "X35", "X72"},
          {"X63", "X80", "X15", "X36"}]
def grp(i):
    for g in GROUPS:
        if i in g: return g
    return {i}
MODEL = sys.argv[1] if len(sys.argv) > 1 else "sonnet"
K = 3
P2B = open(S + "p2b_pair_anchored.txt", encoding="utf-8").read()
pairs = json.load(open(S + "pairs.json"))

def calib(test_ids, seed):
    ban = set().union(*[grp(t) for t in test_ids])
    rnd = random.Random(seed)
    cp = [i for i in pos if i not in ban]; cn = [i for i in neg if i not in ban]
    # at most one per group so the examples are diverse
    def pick(lst):
        rnd.shuffle(lst); out = []; used = []
        for i in lst:
            if any(i in g for g in used): continue
            out.append(i); used.append(grp(i))
            if len(out) == K: break
        return out
    return pick(cp), pick(cn)

def fmt(ids):
    return "\n".join(f"- img/{i}.png — 디렉터: \"{Q[i]}\"" for i in ids)

jobs = []; meta = {}
for k, (p, n) in enumerate(pairs):
    cp, cn = calib([p, n], k)
    meta[f"{p}_{n}"] = (cp, cn)
    base = P2B.replace("{POS}", fmt(cp)).replace("{NEG}", fmt(cn))
    jobs.append((f"p2b_{p}_{n}", base.replace("{L}", p).replace("{R}", n)))
    jobs.append((f"p2b_{n}_{p}", base.replace("{L}", n).replace("{R}", p)))
json.dump(meta, open(S + "p2b_calib.json", "w"), indent=0)

def run(job):
    name, prompt = job
    out = S + f"out/{MODEL}_{name}.txt"
    if os.path.exists(out) and os.path.getsize(out) > 20:
        return name, "skip"
    try:
        r = subprocess.run(["claude", "-p", prompt, "--model", MODEL, "--setting-sources", "local",
                            "--no-session-persistence", "--disable-slash-commands",
                            "--allowedTools", "Read", "--max-turns", "16"],
                           cwd=S, capture_output=True, text=True, encoding="utf-8", timeout=500)
        open(out, "w", encoding="utf-8").write(r.stdout + ("\n[stderr]" + r.stderr if r.returncode else ""))
        return name, r.returncode
    except Exception as e:
        return name, repr(e)
if __name__ == "__main__":
    with ThreadPoolExecutor(int(os.environ.get("PAR", "8"))) as ex:
        for name, st in ex.map(run, jobs):
            print(name, st, flush=True)
    print("DONE", len(jobs))
