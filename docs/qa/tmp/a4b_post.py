# 과제 1302 후처리: 측정(320×180 bilinear — L 평균·p5·p95, HSV 채도·p90, r−b) + a4_vs_a4b.png(왼 A4, 오 A4b; 위 B, 아래 소개)
from PIL import Image
E = 'docs/qa/evidence/'
def small(p): return Image.open(p).convert('RGB').resize((320, 180), Image.BILINEAR)
def meas(p):
    im = small(p); L = sorted(im.convert('L').getdata()); S = sorted(im.convert('HSV').getchannel('S').getdata()); n = len(L)
    rb = sum(r - b for r, g, b in im.getdata()) / n
    return 'L %d p5 %d p95 %d | S %.2f p90 %.2f | r-b %+.0f' % (sum(L) / n, L[n // 20], L[n * 95 // 100], sum(S) / n / 255, S[n * 9 // 10] / 255, rb)
for nm, p in [('A4 B', E + 'scene-a4/b_game.png'), ('A4b B', E + 'scene-a4b/b_game.png'), ('A4 intro', E + 'scene-a4/intro.png'), ('A4b intro', E + 'scene-a4b/intro.png'),
              ('A4b garage_in', E + 'scene-a4b/garage_interior.png'), ('A4b house', E + 'scene-a4b/house_close.png')]:
    print(nm.ljust(14), meas(p))
W, H = 640, 360; pv = Image.new('RGB', (W * 2 + 10, H * 2 + 10), (30, 30, 30))
for r, f in enumerate(['b_game', 'intro']):
    pv.paste(Image.open(E + f'scene-a4/{f}.png').convert('RGB').resize((W, H), Image.NEAREST), (0, r * (H + 10)))
    pv.paste(Image.open(E + f'scene-a4b/{f}.png').convert('RGB').resize((W, H), Image.NEAREST), (W + 10, r * (H + 10)))
pv.save(E + 'scene-a4b/a4_vs_a4b.png'); print('saved')
