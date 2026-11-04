"""DayLoopCheck 두 JSON 의 단계 줄을 시각([초]) 빼고 비교. 사용: dlc_cmp.py <전.json> <후.json>"""
import json, re, sys
def steps(p):
    j = json.load(open(p, encoding="utf-8"))
    s = j.get("steps", j) if isinstance(j, dict) else j
    return [re.sub(r"^\[[0-9.]+\]\s*", "", x) for x in s]
a, b = steps(sys.argv[1]), steps(sys.argv[2])
diff = [(i, x, y) for i, (x, y) in enumerate(zip(a, b)) if x != y]
print(f"전 {len(a)}단계 · 후 {len(b)}단계 · 다른 줄 {len(diff)} → {'SAME' if len(a) == len(b) and not diff else 'DIFF'}")
for i, x, y in diff[:8]: print(f"  #{i}\n   전 {x}\n   후 {y}")
# 방 단계 = 침대 앞부터 끝까지(침대 → 일기 → 아침). 이름 + 상태·단계·안내·지나온만 본다(포장 물리 숫자는 흔들림)
def room(s):
    i = next((k for k, x in enumerate(s) if x.startswith("침대")), len(s))
    return [x.split("|")[0].split()[0] + " |" + x.split("|", 1)[1] if "|" in x else x for x in s[i:]]
ra, rb = room(a), room(b)
print(f"방 단계: 전 {len(ra)} · 후 {len(rb)} → {'SAME' if ra == rb else 'DIFF'}")
for x, y in zip(ra, rb): print(("  = " if x == y else "  ≠ ") + y[:150])
