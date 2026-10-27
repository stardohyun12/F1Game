# 과제 1726: 검정·회색이어야 할 물체 텍스처 → A6d 전용 사본 (원본·공유 재질 안 건드림)
# 규칙(텍셀 단위): 채도폭(max-min) < 45 이고
#   휘도 < 140 → 숯색 금속 R=G=B=44, 명도 결 ±3% (원래 휘도 순서를 그대로 옮김 — 결만 남고 색상 흔들림 0)
#   휘도 ≥ 140 → 같은 휘도의 무채색 (밝은 회색 부분은 밝기 유지, 색만 뺌)
# 그 밖(뚜렷한 유색: 램프 유리·빨강 플라스틱 등)은 그대로.
from PIL import Image
import os
SRC = 'D:/Github/F1Game/F1/'; DST = 'D:/Github/F1Game/F1/Assets/Art/SceneA6d/Tex/'
os.makedirs(DST, exist_ok=True)
NAMES = ['streetlight_01', 'wire', 'tires_01', 'tires_02', 'bicycle_01', 'bench_01', 'old_car_01', 'transformer_01']
FULL = {'streetlight_01', 'tires_01', 'tires_02', 'wire'}   # 통째로 검정이어야 하는 것
paths = {}
for line in open('D:/Github/F1Game/docs/qa/tmp/a6d_texpaths.txt', encoding='utf8'):
    p = line.rstrip('\n').split('\t')
    if len(p) == 3: paths[p[0]] = p[2]
for n in NAMES:
    src = paths['SA4_' + n]; im = Image.open(SRC + src).convert('RGB'); px = im.load(); W, H = im.size
    darkL = []
    for y in range(H):
        for x in range(W):
            r, g, b = px[x, y]; L = 0.299 * r + 0.587 * g + 0.114 * b
            if max(r, g, b) - min(r, g, b) < 45 and L < 140: darkL.append(L)
    lo, hi = (min(darkL), max(darkL)) if darkL else (0, 1); nd = nl = 0
    for y in range(H):
        for x in range(W):
            r, g, b = px[x, y]; L = 0.299 * r + 0.587 * g + 0.114 * b
            if n in FULL: L = min(L, 139)   # 가로등: 1차 캡처에서 남긴 노랑 텍셀(a99e6c 등)이 기둥 모서리·점으로 보임 → 전부 숯색
            elif max(r, g, b) - min(r, g, b) >= 45: continue
            if L < 140:
                t = max(-1, min(1, (L - lo) / max(1e-3, hi - lo) * 2 - 1)); v = round(44 * (1 + 0.03 * t)); nd += 1
            else:
                v = round(L); nl += 1
            px[x, y] = (v, v, v)
    im.save(DST + n + '_mono.png')
    print(f'{n:15s} {W}x{H} src {src} -> charcoal {nd} ({100*nd/(W*H):.0f}%) neutral-light {nl} ({100*nl/(W*H):.0f}%) kept {W*H-nd-nl}')
