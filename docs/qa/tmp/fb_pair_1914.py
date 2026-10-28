# artlive 1914: [도현 뷰어 화면 | Unity 전(1846 뒤) | Unity 후(<tag>)] 3칸 → <id>_pair_<tag>.png (지우지 않음)
# python -X utf8 docs/qa/tmp/fb_pair_1914.py <tag> <id>... | 시계: clock 을 id 로 주면 clock_0310 | clock_1045 2칸
import sys
from PIL import Image, ImageDraw
D = 'docs/qa/evidence/feedback-1028/'
def sheet(paths, titles, out):
    ims = [Image.open(p).convert('RGB').resize((640, 360), Image.NEAREST) for p in paths]
    c = Image.new('RGB', (648 * len(ims) - 8, 380), (20, 20, 20)); dr = ImageDraw.Draw(c)
    for k, (im, t) in enumerate(zip(ims, titles)):
        c.paste(im, (k * 648, 20)); dr.text((k * 648 + 4, 4), t, fill=(255, 255, 255))
    c.save(out); print(out)
tag = sys.argv[1]
for i in sys.argv[2:]:
    if i == 'clock':
        sheet([D + 'clock_0310_%s.png' % tag, D + 'clock_1045_%s.png' % tag], ['WallClock.Show 3:10', 'WallClock.Show 10:45'], D + 'clock_pair_%s.png' % tag)
        continue
    sheet(['tools/viewer/feedback/shots/%s.png' % i, D + i + '_after.png', D + i + '_%s.png' % tag],
          ['viewer shot', 'Unity before (1846)', 'Unity after (%s)' % tag], D + i + '_pair_%s.png' % tag)
