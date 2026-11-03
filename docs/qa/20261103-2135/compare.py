# 과제 06: DayLoopCheck before(05 after.json) ↔ after 비교. python compare.py <after.json>
# 단계 줄: 시각 ±1초 · 시각을 뺀 글이 같은지. 감독 로그: 숫자를 지운 줄을 순서대로.
import json, re, sys
base = json.load(open("docs/qa/20261103-2102/after.json", encoding="utf-8"))
new = json.load(open(sys.argv[1], encoding="utf-8"))
def split(s):
    m = re.match(r"\[([\d.]+)\]\s*(.*)", s)
    return (float(m.group(1)), m.group(2)) if m else (None, s)
bad = 0
bs, ns = base["steps"], new["steps"]
print(f"steps {len(bs)} vs {len(ns)}")
for i in range(max(len(bs), len(ns))):
    a = split(bs[i]) if i < len(bs) else (None, "-")
    b = split(ns[i]) if i < len(ns) else (None, "-")
    dt = (b[0] - a[0]) if a[0] is not None and b[0] is not None else None
    same = a[1] == b[1] and dt is not None and abs(dt) <= 1.0
    if not same: bad += 1
    print(f"{'OK ' if same else 'XX '} {a[0]}→{b[0]} {'' if a[1]==b[1] else '| ' + a[1] + ' || ' + b[1]}")
strip = lambda s: re.sub(r"-?\d+(\.\d+)?", "#", s)
bl = [strip(x) for x in base.get("log", [])]
nl = [strip(x) for x in new.get("log", [])]
print(f"log {len(bl)} vs {len(nl)}")
for i in range(max(len(bl), len(nl))):
    a = bl[i] if i < len(bl) else "-"; b = nl[i] if i < len(nl) else "-"
    if a != b: bad += 1; print(f"XX log {i}: {a}  ||  {b}")
print("RESULT", "SAME" if bad == 0 else f"DIFF {bad}")
