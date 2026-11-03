# fp_<a>.tsv vs fp_<b>.tsv 차이 — python -X utf8 fpdiff.py before rebuild0
import sys, os, collections
D = os.path.dirname(os.path.abspath(__file__))
def load(t):
    m = {}
    for l in open(os.path.join(D, f"fp_{t}.tsv"), encoding="utf-8"):
        k, *rest = l.rstrip("\n").split("\t")
        m.setdefault(k, []).append("\t".join(rest))
    return m
a, b = load(sys.argv[1]), load(sys.argv[2])
only_a = [k for k in a if k not in b]; only_b = [k for k in b if k not in a]
diff = [k for k in a if k in b and sorted(a[k]) != sorted(b[k])]
def grp(ks):
    c = collections.Counter("/".join(k.split("/")[:3]) for k in ks); return ", ".join(f"{g}×{n}" for g, n in c.most_common(12))
print(f"A {sum(map(len,a.values()))} objs, B {sum(map(len,b.values()))} objs | only A {len(only_a)} | only B {len(only_b)} | changed {len(diff)}")
print("only A:", grp(only_a)); print("only B:", grp(only_b)); print("changed:", grp(diff))
for k in (only_a[:8]): print("  -A", k, a[k][0][:90])
for k in (only_b[:8]): print("  +B", k, b[k][0][:90])
for k in diff[:15]: print("  ~", k, "\n     A", a[k][0][:140], "\n     B", b[k][0][:140])
