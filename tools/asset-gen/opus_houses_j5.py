"""집 j5 — j4 와 같은 과정, 덧창 색만 살림 + 모서리 띠 얼룩 (아트2, 과제 20261027-2015). Blender:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j5.py
j4 스크립트를 읽어 몇 줄만 바꿔 돌린다(나머지 과정·숫자 그대로):
 1) 팔레트 40 + 6 = 46색: 바랜 파랑 3단(C1 라벤더 루버에서 색상, 밝음·중간·그늘) · 바랜 초록 3단(C2 에서). 새 6색은 덧창(shutter·plank)에서만 쓴다.
    덧창 텍셀 = 사진 밝기 순위로 두 단(앞쪽 벽 방향 = 밝음/중간, 그늘 방향 = 중간/그늘). 그 밖의 면은 j4 와 같은 40색 양자화.
 2) D4: j4 가는 띠 조건 '폭 < 6텍셀' 이 narrow_b 모서리 띠(UV 6.0×95, 0.1m 폭 창 옆 띠)를 놓침 → '폭 ≤ 8텍셀' 로.
출력: art/export/scene-a4-houses-j5/ (+ palette46.gpl) · 로그 art/models/opus_houses_j5_log.txt
"""
import os
_P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_j4.py')
_s = open(_P, encoding='utf-8').read()
SHUT = {'C1': [(112, 120, 166), (92, 100, 142), (72, 80, 116)],   # 바랜 파랑: 밝음 L122 · 중간 L101 · 그늘 L81 (채도 54/50/44)
        'C2': [(100, 140, 98), (80, 118, 80), (62, 94, 66)]}       # 바랜 초록: 밝음 L124 · 중간 L102 · 그늘 L82 (채도 42/38/32)
_HELP = '''
SHUT = %r
PALX = np.concatenate([PAL, np.array([c for v in SHUT.values() for c in v], float)])
def shq(pname, col, nv):
    """덧창 전용: 사진 밝기 순위 → 같은 색상 두 단"""
    tri = np.array(SHUT['C1' if 'C1' in pname else 'C2'], float); kmax = max(kdir.values()) if kdir else 1
    pair = tri[:2] if kof(nv) >= .9 * kmax else tri[1:]
    if col is None: return pair[:1]
    L = col @ LUM; return np.where((L > np.percentile(L, 45))[:, None], pair[0], pair[1])
''' % SHUT
REP = [
    ("OUT = R + 'art/export/scene-a4-houses-j4/'", "OUT = R + 'art/export/scene-a4-houses-j5/'"),
    ("art/models/opus_houses_j4_log.txt", "art/models/opus_houses_j5_log.txt"),
    ("_cache = {}\n", _HELP + "_cache = {}\n"),
    ("free = isbg & (cls == 0); painted = np.zeros((H, W), bool); cnt = {}", "free = isbg & (cls == 0); painted = np.zeros((H, W), bool); shut = np.zeros((H, W), bool); cnt = {}"),
    ("col = quant(mean_col * kof(nv))[0]; uv = cell(col)",
     "col = shq(pname, None, nv)[0] if role in ('shutter', 'plank') else quant(mean_col * kof(nv))[0]; uv = cell(col)"),
    ("px[ys, xs] = quant(col * ao[:, None]); painted[ys, xs] = True; c += 1",
     "sh_ = role in ('shutter', 'plank'); px[ys, xs] = shq(pname, col * ao[:, None], nv) if sh_ else quant(col * ao[:, None]); painted[ys, xs] = True; shut[ys, xs] |= sh_; c += 1"),
    ("if nb: px[y, x] = quant(np.median(nb, 0))[0]",
     "if nb and shut[y, x]: k_ = [tuple(v) for v in nb]; px[y, x] = max(set(k_), key=k_.count)\n        elif nb: px[y, x] = quant(np.median(nb, 0))[0]"),
    ("if e.min() < 6 and e.max() >= 2.5 * max(e.min(), 1):", "if e.min() <= 8 and e.max() >= 2.5 * max(e.min(), 1):"),
    ("np.isin(np.unique(key[painted]), PAL @ np.array((65536., 256., 1.)))", "np.isin(np.unique(key[painted]), PALX @ np.array((65536., 256., 1.)))"),
    ("rooms=rooms,", "rooms=rooms, shutter_texels=int(shut.sum()), shutter_colors=int(len(np.unique(key[shut]))),"),
    ("print('J4', n,", "print('J5', n,"),
    ("im = bpy.data.images.new(n + '_j4'", "im = bpy.data.images.new(n + '_j5'"),
]
for a, b in REP:
    assert a in _s, a; _s = _s.replace(a, b, 1)
exec(compile(_s, _P, 'exec'))
# 46색 팔레트·측정
with open(OUT + 'palette46.gpl', 'w', encoding='utf-8') as f_:
    f_.write(open(SRC + 'palette40.gpl', encoding='utf-8').read().rstrip('\n') + '\n')
    for k_, v_ in SHUT.items():
        for nm, c_ in zip(('light', 'mid', 'shade'), v_): f_.write('%3d %3d %3d\tshutter_%s_%s\n' % (*c_, 'blue' if k_ == 'C1' else 'green', nm))
json.dump(dict(task='20261027-2015', source='scene-a4-houses-j3 (+ j4 과정)', palette='palette46.gpl = 40 + 덧창 6', shutter_palette=SHUT, files=meas),
          open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE')
