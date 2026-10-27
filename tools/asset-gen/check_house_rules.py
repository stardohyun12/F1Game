"""집 e판 규칙 판정표 — docs/research/2026-10-27-ai-look-external.md §6 R1~R11 (아트2, 과제 20261027-1406).
    python tools/asset-gen/check_house_rules.py [export 폴더 = art/export/scene-a4-houses-e]
읽는 것: <폴더>/parts.json (opus_houses_e.py 가 조립하면서 남긴 부품 목록 — 창·문 자리, 덧붙임, 막은 창, 보수 칠, 배관 끝, 덧창 상태, 변형 값).
주의: 메시를 다시 재지 않고 생성기가 실제로 놓은 좌표를 판정한다(bpy 불필요). R9·R10 은 변형 함수에 넣은 값.
"""
import json, os, sys
from collections import Counter
D = sys.argv[1] if len(sys.argv) > 1 else 'art/export/scene-a4-houses-e'
P = json.load(open(os.path.join(D, 'parts.json'), encoding='utf-8'))
rows = []; allshut = []
for name, h in P.items():
    win = [o for o in h['openings'] if o['kind'] == 'window']
    sizes = sorted({(round(o['u1'] - o['u0'], 2), round(o['z1'] - o['z0'], 2)) for o in win})
    kinds = []  # 면적 15% 넘게 다른 것만 다른 크기로
    for w, hh in sizes:
        if all(abs(w * hh - a) / max(a, 1e-6) > .15 for a in kinds): kinds.append(w * hh)
    off = [abs(o['z0'] - h['zb'] - 3 * o['story'] - h['sill']) for o in win]
    r2 = sum(.22 <= v <= .45 for v in off); r2bad = sum(v > .45 for v in off)
    adds = len(h.get('add', [])); need = 2 if h['stories'] >= 3 else 1
    r5 = len(h.get('blocked', [])) + sum(1 for o in win if o.get('own'))
    patches = h.get('patch', []); r8 = sum(p['why'] != 'free' for p in patches) / max(1, len(patches))
    pipes = h.get('pipe', []); r7 = all(abs(p['bottom'][2] - p['support_bottom']) < .1 for p in pipes)
    asym = sum(1 for k in ('blocked', 'add', 'pipe') if h.get(k)) + sum(1 for o in win if o.get('own') and o['face'] == 'front')
    st = [s for x in h.get('shutter', []) for s in x['states']]
    if not st and any(o.get('louvre') for o in h['openings']): st = [o['louvre'] for o in h['openings'] if o.get('louvre')]
    allshut.append(('plank' if h.get('shutter') else 'louvre' if st else 'none'))
    pots = Counter(p['model'] for p in h.get('pot', []))
    rows.append((name, [
        ('R1 창 크기 ≥3종', len(kinds), len(kinds) >= 3),
        ('R2 어긋난 창 1개(0.22~0.45m)·과한 것 0', f'{r2}/{r2bad}', r2 >= 1 and r2bad == 0),
        ('R3 대칭 깨는 요소 ≥2', asym, asym >= 2),
        (f'R4 덧붙임 ≥{need}', adds, adds >= need),
        ('R5 막은 창/새 창 ≥1', r5, r5 >= 1),
        ('R6 보수 칠 1~3', len(patches), 1 <= len(patches) <= 3),
        ('R7 배관 양 끝 붙음', len(pipes), bool(pipes) and r7),
        ('R8 흔적 ≥70% 사건 위치', f'{r8:.0%}', r8 >= .7),
        ('R9 용마루 처짐 1~3%', f"{h['deform']['sag']:.1%}", .01 <= h['deform']['sag'] <= .03),
        ('R10 평면 모서리 2~6°', f"{h['deform']['skew']}°", 2 <= h['deform']['skew'] <= 6),
        ('R11 덧창 상태 ≥2', len(set(st)), len(set(st)) >= 2 or not st),
        ('띠(0.6m) 있음', h['band'] > 0, None),
        ('생활 소품 수', len(h.get('life', [])), None)]))
print(f'판정: {D}/parts.json  (설계 좌표 기준)\n')
tot = Counter()
for name, rs in rows:
    ok = sum(1 for r in rs if r[2]); n = sum(1 for r in rs if r[2] is not None); tot['ok'] += ok; tot['n'] += n
    print(f'## {name}  {ok}/{n}')
    for k, v, p in rs: print(f"  {'·' if p is None else 'O' if p else 'X'} {k}: {v}")
bands = sum(1 for _, rs in rows if rs[11][1]); shut = Counter(allshut)
print(f"\n6채 공통: 아래 띠 {bands}/6 (≤2 → {'O' if bands <= 2 else 'X'}) · 덧창 종류 {dict(shut)} (같은 것 ≤2 → {'O' if max(shut.values()) <= 2 else 'X'})")
pm = Counter(m for h in P.values() for m in {p['model'] for p in h.get('pot', [])})
print(f"화분 모델별 쓰인 집 수 {dict(pm)} (≤2 → {'O' if max(pm.values()) <= 2 else 'X'})")
print(f"합계 {tot['ok']}/{tot['n']}")
