import json
p = 'tools/viewer/feedback/unity-Map_TownV6.jsonl'
L = open(p, encoding='utf8').read().splitlines()
fix = {
    'unity-Map_TownV6-20261029-034450-9a2a': ("A7 셰이더 그늘색 전역값(_A7Shade 등)이 이 씬엔 안 들어가 그늘 면이 검정이었음 → A7ShadeGlobals 붙여 Scene_A7_Light 와 같은 청회색 그늘로", "docs/qa/evidence/fb0344/light_after.png"),
    'unity-Map_TownV6-20261029-034633-25cd': ("검게 진 건 그림자가 아니라 그늘 면이 (0,0,0) 이던 것 — 같은 원인, A7ShadeGlobals 로 그늘색 채움(차고는 03:56 배치로 옮겨져 새 자리에서 찍음)", "docs/qa/evidence/fb0344/shadow_after.png"),
    'unity-Map_TownV6-20261029-034733-18ae': ("길 ridge_up_a 추가: (250,-112)→(287,-63) 108 m, 폭 4.5 m, 최대 경사 11%, 묻힘 0", "docs/qa/evidence/fb0344/road_a.png"),
    'unity-Map_TownV6-20261029-034811-2d53': ("길 ridge_up_b 추가: (238,-71)→(236,-53) 41 m, 폭 4.5 m, 최대 경사 11%, 묻힘 0", "docs/qa/evidence/fb0344/road_b.png"),
    'unity-Map_TownV6-20261029-034941-0e88': ("길 ridge_up_c 추가: (248,-61)→(291,-37) 88 m, 폭 4.5 m, 최대 경사 11%, 묻힘 0", "docs/qa/evidence/fb0344/road_c.png"),
}
n = 0
for i, l in enumerate(L):
    if not l.strip():
        continue
    d = json.loads(l)
    if d.get('id') in fix:
        d['status'] = 'fixed'
        d['reply'], d['after_shot'] = fix[d['id']]
        L[i] = json.dumps(d, ensure_ascii=False)
        n += 1
open(p, 'w', encoding='utf8', newline='\n').write('\n'.join(L) + '\n')
print(n)

# 길 캡처 스크립트 생성(각 길 중간 위 25 m, 남쪽 20 m 에서 내려다봄)
src = open('docs/qa/tmp/fb0404_cap.cs', encoding='utf8').read()
roads = {'a': ((250, 14.8, -112), (287, 25.4, -63)), 'b': ((238, 22.4, -71), (236, 26.7, -53)), 'c': ((248, 24.8, -61), (291, 31.5, -37))}
shots = []
for k, (s, e) in roads.items():
    m = tuple((s[j] + e[j]) / 2 for j in range(3))
    span = max(abs(s[0] - e[0]), abs(s[2] - e[2]))
    cam = (m[0] + span * .5, m[1] + span * .6 + 15, m[2] - span * .6 - 10)
    shots.append(f'  ("docs/qa/evidence/fb0344/road_{k}.png", new Vector3({cam[0]}f,{cam[1]}f,{cam[2]}f), new Vector3({m[0]}f,{m[1]}f,{m[2]}f))')
open('docs/qa/tmp/fb0404_cap_roads.cs', 'w', encoding='utf8').write(src.replace('  ("__SHOTS__", Vector3.zero, Vector3.zero),', ',\n'.join(shots) + ','))
