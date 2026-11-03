"""가장자리 표식 받침 상자 대비: 종이색 테 vs 배경, (이전) 잉크 상자 vs 배경. flag_sheet.py 가 만든 contrast.json 을 읽는다.
사용: python -I edgebox_contrast.py <증거폴더>"""
import json, sys
import numpy as np
from PIL import Image

d = sys.argv[1].rstrip('/') + '/'
C = json.load(open(d + 'contrast.json', encoding='utf-8'))


def lum(c):
    c = np.asarray(c, float) / 255
    c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


def cr(a, b):
    la, lb = lum(a), lum(b)
    return round((max(la, lb) + .05) / (min(la, lb) + .05), 2)


res = {}
for k, v in C.items():
    if not k.endswith('_new') or v.get('flag', 1) is None or not v.get('inkBBox'): continue
    if not any(t in k for t in ('p1_', 'p2_', 'p6_')): continue   # edge 모드 컷만
    im = np.array(Image.open(d + k + '.png').convert('RGB')).astype(int)
    x0, y0, x1, y1 = v['inkBBox']
    # 종이 테 = 잉크 bbox 바로 바깥 1px 줄(위·아래), 배경 = 그 바깥 4~5px 줄
    rows = [im[y0 - 1, x0 + 3:x1 - 2], im[min(359, y1 + 1), x0 + 3:x1 - 2]]
    paper = np.median(np.vstack(rows), axis=0)
    ring = []
    for off in (4, 5):
        ring += list(im[max(0, y0 - off), x0:x1 + 1]); ring += list(im[min(359, y1 + off), x0:x1 + 1])
    bg = np.median(np.array(ring), axis=0)
    res[k] = {'paperRGB': [int(x) for x in paper], 'bgRGB': [int(x) for x in bg],
              'paper_vs_bg': cr(paper, bg), 'ink_vs_bg(잉크 상자만 있을 때)': v['contrast_ring_vs_ink']}
    print(k, res[k])
json.dump(res, open(d + 'edgebox_contrast.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
