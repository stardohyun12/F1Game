# artlive 1846: 코멘트별 [도현 뷰어 화면 | Unity 전 | Unity 후] 3칸 → docs/qa/evidence/feedback-1028/<id>_pair.png
import sys
from PIL import Image, ImageDraw
D = 'docs/qa/evidence/feedback-1028/'
for i in sys.argv[1:]:
    ims = [Image.open(p).convert('RGB').resize((640, 360), Image.NEAREST) for p in ('tools/viewer/feedback/shots/%s.png' % i, D + i + '_before.png', D + i + '_after.png')]
    c = Image.new('RGB', (640 * 3 + 16, 380), (20, 20, 20)); dr = ImageDraw.Draw(c)
    for k, (im, t) in enumerate(zip(ims, ['viewer shot', 'Unity before', 'Unity after'])):
        c.paste(im, (k * 648, 20)); dr.text((k * 648 + 4, 4), t, fill=(255, 255, 255))
    c.save(D + i + '_pair.png')
print('ok')
