# 2300: 길가 소품 묶음 해시 비교(base vs last) — 바뀐 묶음만 찍는다
import json, sys
a = json.load(open(sys.argv[1], encoding="utf8"))["groups"]
b = json.load(open(sys.argv[2], encoding="utf8"))["groups"]
same = [k for k in a if b.get(k) == a[k]]
print(f"base {len(a)} · now {len(b)} · same {len(same)}")
for k in sorted(set(a) | set(b)):
    if a.get(k) != b.get(k):
        print(f"  {'-' if k not in b else '+' if k not in a else '~'} {k}  {a.get(k)} -> {b.get(k)}")
