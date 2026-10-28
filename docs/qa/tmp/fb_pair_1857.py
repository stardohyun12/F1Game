# artlive 1857: [도현 뷰어 화면 | Unity 전(1846 뒤) | Unity 후(1857)] 3칸 → <id>_pair.png (이미 있으면 <id>_pair1857.png — 지우지 않음)
# 시계: clock_0310 | clock_1045 2칸 → clock_pair.png
import sys, os
from PIL import Image, ImageDraw
D = 'docs/qa/evidence/feedback-1028/'
def sheet(paths, titles, out):
    ims = [Image.open(p).convert('RGB').resize((640, 360), Image.NEAREST) for p in paths]
    c = Image.new('RGB', (648 * len(ims) - 8, 380), (20, 20, 20)); dr = ImageDraw.Draw(c)
    for k, (im, t) in enumerate(zip(ims, titles)):
        c.paste(im, (k * 648, 20)); dr.text((k * 648 + 4, 4), t, fill=(255, 255, 255))
    c.save(out); print(out)
for i in sys.argv[1:]:
    out = D + i + ('_pair1857.png' if os.path.exists(D + i + '_pair.png') else '_pair.png')
    sheet(['tools/viewer/feedback/shots/%s.png' % i, D + i + '_after.png', D + i + '_after1857.png'], ['viewer shot', 'Unity before (1846)', 'Unity after (1857)'], out)
sheet([D + 'clock_0310_after1857.png', D + 'clock_1045_after1857.png'], ['WallClock.Show 3:10', 'WallClock.Show 10:45'], D + 'clock_pair.png')
