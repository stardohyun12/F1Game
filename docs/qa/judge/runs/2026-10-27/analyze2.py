import json, re, os, sys
S = "C:/Users/rtx40/AppData/Local/hermes/cache/scratch/judge/run/"
MODEL = sys.argv[1] if len(sys.argv) > 1 else "sonnet"
A = json.load(open("D:/Github/F1Game/docs/qa/judge/anchors.json", encoding="utf-8"))
lab = {a["id"]: 1 for a in A["positive"]} | {a["id"]: 0 for a in A["negative"]}
pairs = json.load(open(S + "pairs.json"))
def read(n):
    p = S + f"out/{MODEL}_{n}.txt"
    return open(p, encoding="utf-8").read() if os.path.exists(p) else ""
out = {}
def score(tag):
    sc = {}
    for i in lab:
        m = re.findall(r"SCORE:\s*([\d.]+)", read(f"{tag}_{i}"))
        if m: sc[i] = float(m[-1])
    pp = [sc[i] for i in sc if lab[i]]; nn = [sc[i] for i in sc if not lab[i]]
    auc = sum((p > n) + .5 * (p == n) for p in pp for n in nn) / max(1, len(pp) * len(nn))
    best = max(((sum((sc[i] >= t) == bool(lab[i]) for i in sc) / len(sc)), t) for t in [3,4,5,5.5,6,6.5,7,8]) if sc else None
    acc7 = sum((sc[i] >= 7) == bool(lab[i]) for i in sc) / max(1, len(sc))
    return dict(n=len(sc), auc=round(auc, 3), acc_at7=round(acc7, 3), best_thr=best,
                mean_pos=round(sum(pp)/max(1,len(pp)), 2), mean_neg=round(sum(nn)/max(1,len(nn)), 2), scores=sc)
def verdict(tag):
    vd = {}
    for i in lab:
        m = re.findall(r"VERDICT:\s*(통과|거부)", read(f"{tag}_{i}"))
        if m: vd[i] = int(m[-1] == "통과")
    acc = sum(vd[i] == lab[i] for i in vd) / max(1, len(vd))
    tp = sum(vd[i] for i in vd if lab[i]); P = sum(1 for i in vd if lab[i])
    tn = sum(1 - vd[i] for i in vd if not lab[i]); N = sum(1 for i in vd if not lab[i])
    return dict(n=len(vd), acc=round(acc, 3), pos_pass=f"{tp}/{P}", neg_reject=f"{tn}/{N}",
                balanced=round((tp/max(1,P) + tn/max(1,N))/2, 3), verdicts=vd)
def pair(tag):
    single = []; both = both_ok = 0; nL = posL = 0; fails = []
    for p, n in pairs:
        r1 = re.findall(r"PICK:\s*([LR])", read(f"{tag}_{p}_{n}")); r2 = re.findall(r"PICK:\s*([LR])", read(f"{tag}_{n}_{p}"))
        o1 = (r1[-1] == "L") if r1 else None; o2 = (r2[-1] == "R") if r2 else None
        for r in (r1, r2):
            if r: nL += 1; posL += r[-1] == "L"
        for o in (o1, o2):
            if o is not None: single.append(o)
        if o1 is not None and o2 is not None:
            both += 1; both_ok += o1 and o2
        if not (o1 and o2): fails.append((p, n, o1, o2))
    return dict(single=f"{sum(single)}/{len(single)}", single_acc=round(sum(single)/max(1,len(single)), 3),
                both_orders=f"{both_ok}/{both}", both_acc=round(both_ok/max(1,both), 3), L_picks=f"{posL}/{nL}", fails=fails)
out["p1_score"] = score("p1"); out["p1b_score_anchored"] = score("p1b")
out["p3_defect"] = verdict("p3"); out["p3b_defect_anchored"] = verdict("p3b")
out["p2_pair"] = pair("p2"); out["p2b_pair_anchored"] = pair("p2b")
json.dump(out, open(S + f"summary_{MODEL}.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
for k, v in out.items():
    print(k, {kk: vv for kk, vv in v.items() if kk not in ("scores", "verdicts")})
