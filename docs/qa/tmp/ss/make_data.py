# 과제 1428: 길가 배치 대역 — 3구간의 카메라 자리·영상 경로·소품 자리를 Unity 가 읽을 텍스트로 뽑는다.
import json, math, numpy as np
P = json.load(open('docs/map/streetscape-plan.json', encoding='utf-8'))
T = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
E = json.load(open('docs/map/env-buildings.json', encoding='utf-8'))
roads = {r['id']: r for r in T['roads']}
els = P['elements']; byid = {e['id']: e for e in els}

def poly(rid):
    k = np.array(roads[rid]['knots'], float)  # [x,z,y]
    k = k[np.r_[True, np.linalg.norm(np.diff(k[:, :2], axis=0), axis=1) > 1e-7]]
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(k[:, :2], axis=0), axis=1))]
    return k, d

def at(rid, s, rev=False):
    k, d = poly(rid)
    s = float(np.clip(s, 0, d[-1]))
    x = np.interp(s, d, k[:, 0]); z = np.interp(s, d, k[:, 1]); y = np.interp(s, d, k[:, 2])
    a = np.array([np.interp(max(0, s - 4), d, k[:, 0]), np.interp(max(0, s - 4), d, k[:, 1])])
    b = np.array([np.interp(min(d[-1], s + 4), d, k[:, 0]), np.interp(min(d[-1], s + 4), d, k[:, 1])])
    t = b - a
    if rev: t = -t
    return x, y, z, math.degrees(math.atan2(t[0], t[1]))

def nearest_station(rid, p):
    k, d = poly(rid)
    ss = np.linspace(0, d[-1], int(d[-1]) * 2 + 1)
    xs = np.interp(ss, d, k[:, 0]); zs = np.interp(ss, d, k[:, 1])
    return float(ss[np.argmin((xs - p[0]) ** 2 + (zs - p[1]) ** 2)])

lm = {b['id']: b['world'] for b in E['landmarks']}
ridge_st = [nearest_station('camo_309378634_165', lm[n]) for n in ('house_home', 'garage', 'depot', 'postoffice')]
print('ridge stations', ridge_st)

# (이름, 길, 사진 시작 station, 방향(+1/-1), 소품 범위 station [a,b], 추가 구역)
r0, r1 = min(ridge_st), max(ridge_st)
segs = [
    ('tomo', 'tomo_1296983883_62', 140.0, 440.0, None),
    ('posi', 'posi_1567571332_429', 45.0, 199.0, None),
    ('ridge', 'camo_309378634_165', max(0, r0 - 40), r1 + 40, 'ridge'),
]
out = []
for name, rid, a, b, zone in segs:
    k, d = poly(rid); b = min(b, d[-1])
    mid = (a + b) / 2
    shots = [mid - 62.5 + 25 * i for i in range(6)]
    shots = [min(max(s, a), b) for s in shots]
    out.append(f'SEG {name} {rid}')
    for i, s in enumerate(shots):
        x, y, z, yaw = at(rid, s); out.append(f'SHOT {i+1} {x:.3f} {y:.3f} {z:.3f} {yaw:.2f}')
    # 영상: 사진 구간 시작에서 10초 동안, 그 구간(125m) + 여유 25m
    vs = np.arange(shots[0], min(d[-1], shots[0] + 150) + 0.01, 0.5)
    for s in vs:
        x, y, z, yaw = at(rid, s); out.append(f'VP {x:.3f} {y:.3f} {z:.3f} {yaw:.2f}')
    # 소품: 이 구간 길 중심선(a..b) 에서 xz 35m 안
    ss = np.arange(a, b + 0.01, 2.0)
    cx = np.interp(ss, d, k[:, 0]); cz = np.interp(ss, d, k[:, 1])
    def near(px, pz): return np.min((cx - px) ** 2 + (cz - pz) ** 2) <= 35 ** 2
    picked = []
    for e in els:
        if e['kind'] == 'wire_span': continue
        if (zone and e['zone'] == zone) or near(e['pos'][0], e['pos'][2]): picked.append(e)
    ids = {e['id'] for e in picked}
    from collections import Counter
    print(name, rid, round(a), round(b), 'shots', [round(s) for s in shots], Counter(e['kind'] for e in picked))
    for e in picked:
        x, y, z = e['pos']; extra = e.get('pot_count', 0)
        line = f"E {e['id']} {e['kind']} {x:.3f} {y:.3f} {z:.3f} {e['yaw']:.2f} {e['side']} {extra}"
        if e['kind'] == 'curb_segment':
            line += ' ' + ' '.join(f'{p[0]:.3f},{p[1]:.3f}' for p in e['path_xz'])
        out.append(line)
    for e in els:
        if e['kind'] == 'wire_span' and all(i in ids for i in e['endpoints']):
            p1, p2 = byid[e['endpoints'][0]]['pos'], byid[e['endpoints'][1]]['pos']
            out.append(f"W {e['id']} {p1[0]:.3f} {p1[1]:.3f} {p1[2]:.3f} {p2[0]:.3f} {p2[1]:.3f} {p2[2]:.3f}")
            print('  wire', e['id'], round(e['length_m'], 1))
open('docs/qa/tmp/ss/data.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
print('lines', len(out))
