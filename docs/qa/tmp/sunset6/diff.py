# 사용: diff.py a.png b.png ... (쌍) → 평균 밝기 차, 바뀐 픽셀 %(아무 채널 > 8), 물 영역(화면 아래 y>=수평선)만
import sys
from PIL import Image
import numpy as np
D = 'docs/qa/evidence/sunset6/raw/'
args = sys.argv[1:]
for i in range(0, len(args), 2):
    a = np.asarray(Image.open(D + args[i] + '.png').convert('RGB')).astype(int)
    b = np.asarray(Image.open(D + args[i + 1] + '.png').convert('RGB')).astype(int)
    d = np.abs(a - b)
    ch = (d.max(axis=2) > 8)
    lum = np.abs((a * [54, 183, 19]).sum(2) - (b * [54, 183, 19]).sum(2)) / 256
    print('%s vs %s: changed>8 %.1f%%  meanLumDiff %.2f  p95LumDiff %.1f' % (args[i], args[i + 1], ch.mean() * 100, lum.mean(), np.percentile(lum, 95)))
