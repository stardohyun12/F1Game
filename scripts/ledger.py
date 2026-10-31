#!/usr/bin/env python
"""비용 장부(.claude/team/ledger.jsonl) — 과제마다 team.sh 가 한 줄 씀.
사용: python -X utf8 scripts/ledger.py            # 요약(역할별·결과별 비용, 낭비율)
      python -X utf8 scripts/ledger.py mark <stamp> <pass|redo|drop> [메모]
낭비 = redo(다시 해야 함) + drop(버림). 결과 빈 칸 = 아직 PD 판정 안 함."""
import json, sys, collections, os
P = os.path.join(os.path.dirname(__file__), '..', '.claude', 'team', 'ledger.jsonl')

def load():
    if not os.path.exists(P): return []
    return [json.loads(l) for l in open(P, encoding='utf-8') if l.strip()]

def save(rows):
    with open(P, 'w', encoding='utf-8') as f:
        for r in rows: f.write(json.dumps(r, ensure_ascii=False) + '\n')

if len(sys.argv) > 1 and sys.argv[1] == 'mark':
    rows = load(); n = 0
    for r in rows:
        if r['stamp'] == sys.argv[2]:
            r['result'] = sys.argv[3]; r['note'] = ' '.join(sys.argv[4:]); n += 1
    save(rows); print('marked', n); sys.exit()

rows = load(); by = collections.defaultdict(float); res = collections.defaultdict(float)
for r in rows:
    c = r.get('cost') or 0; by[r['role']] += c; res[r.get('result') or '미판정'] += c
tot = sum(by.values()) or 1
print(f"과제 {len(rows)}건 ${tot:.2f}")
for k, v in sorted(by.items(), key=lambda x: -x[1]): print(f"  {k}: ${v:.2f}")
for k, v in res.items(): print(f"  [{k}] ${v:.2f} ({v/tot:.0%})")
print(f"낭비율(redo+drop) {(res['redo']+res['drop'])/tot:.0%}")
