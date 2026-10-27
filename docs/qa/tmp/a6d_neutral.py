# 과제 1726: 검정·회색이어야 할 텍셀이 _Sat 로 색 노이즈가 되는 재질 찾기
# 회색 텍셀 = 원본 채도폭(max-min) < 24. 셰이더 _Sat 적용 뒤(lum + (c-lum)*sat) 채도폭과 색상 수를 본다
from PIL import Image
import colorsys
F = 'D:/Github/F1Game/F1/'
for line in open('D:/Github/F1Game/docs/qa/tmp/a6d_texpaths.txt', encoding='utf8'):
    p = line.rstrip('\n').split('\t')
    if len(p) < 3 or p[2] == '-': continue
    nm, sat, path = p[0], float(p[1]), p[2]
    try: im = Image.open(F + path).convert('RGB')
    except Exception as e: print(nm, 'ERR', e); continue
    if im.width * im.height > 600000: im = im.resize((im.width // 4, im.height // 4), Image.NEAREST)
    px = list(im.getdata()); n = len(px); grey = []
    for r, g, b in px:
        if max(r, g, b) - min(r, g, b) < 24: grey.append((r, g, b))
    if not grey: print(f'{nm:34s} sat {sat:.2f} grey 0%'); continue
    boosted = []; hues = set()
    for r, g, b in grey:
        L = 0.299 * r + 0.587 * g + 0.114 * b; c = [max(0, min(255, L + (x - L) * sat)) for x in (r, g, b)]
        ch = max(c) - min(c); boosted.append(ch)
        if ch > 12: hues.add(int(colorsys.rgb_to_hsv(*[x / 255 for x in c])[0] * 12))
    boosted.sort(); gl = sorted(0.299 * r + 0.587 * g + 0.114 * b for r, g, b in grey)
    print(f'{nm:34s} sat {sat:.2f} grey {100*len(grey)/n:5.1f}%  greyLum med {gl[len(gl)//2]:.0f}  chroma after sat med {boosted[len(boosted)//2]:.0f} p90 {boosted[len(boosted)*9//10]:.0f}  hue bins {len(hues)}')
