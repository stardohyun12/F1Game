# 과제 1046 — 320×180 으로 줄여 밝기(L 평균)·채도(HSV S 평균)·p10(L) 측정. 사용: python a2d_measure.py img...
import sys
from PIL import Image
for p in sys.argv[1:]:
    im = Image.open(p).convert('RGB').resize((320, 180), Image.BILINEAR)
    L = sorted(im.convert('L').getdata()); S = list(im.convert('HSV').getchannel('S').getdata())
    print(f'{p}: 밝기 {sum(L) / len(L):.0f} · 채도 {sum(S) / len(S):.0f} · p10 {L[len(L) // 10]}')
