import os
from runner2 import S, run
from concurrent.futures import ThreadPoolExecutor
P2C = open(S+"p2c_pair_picks.txt",encoding="utf-8").read()
P1 = open(S+"p1_score.txt",encoding="utf-8").read()
jobs=[]
for a,b in [("X01","X32"),("X35","X72")]:   # doh: first > second
    for rep in (1,2):
        jobs.append((f"rel{rep}_p2c_{a}_{b}", P2C.replace("{L}",a).replace("{R}",b)))
        jobs.append((f"rel{rep}_p2c_{b}_{a}", P2C.replace("{L}",b).replace("{R}",a)))
jobs.append(("p1_X01", P1.replace("{ID}","X01")))
with ThreadPoolExecutor(9) as ex:
    for r in ex.map(run, jobs): print(r)
