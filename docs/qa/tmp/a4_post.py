# 과제 1232 후처리: 측정 표(320×180 bilinear — L 평균·p5·p95, HSV 채도 평균(0~1)·p90, 따뜻함 r−b 평균), 효과 크기, toggles.png, a3_vs_a4.png
from PIL import Image, ImageDraw, ImageStat, ImageChops
E = 'docs/qa/evidence/'; A4 = E + 'scene-a4/'; T = A4 + 'toggles/'; R = 'art/reference/dotlp-2026-10-27/'
def small(p): return Image.open(p).convert('RGB').resize((320, 180), Image.BILINEAR)
def meas(p):
    im = small(p); L = sorted(im.convert('L').getdata()); S = sorted(im.convert('HSV').getchannel('S').getdata()); n = len(L)
    rb = sum(r - b for r, g, b in im.getdata()) / n
    return 'L %d p5 %d p95 %d | S %.2f p90 %.2f | r-b %+.0f' % (sum(L) / n, L[n // 20], L[n * 95 // 100], sum(S) / n / 255, S[n * 9 // 10] / 255, rb)
for nm, p in [('A3 B', E + 'scene-a3/b_game.png'), ('A4 B', A4 + 'b_game.png'), ('A3 intro', E + 'scene-a3/intro.png'), ('A4 intro', A4 + 'intro.png'), ('A4 off B(색보정 끔)', T + 'all_off.png'),
              ('①_1', R + 'a-short-hike_1.jpg'), ('①_2', R + 'a-short-hike_2.jpg'), ('⑥_1', R + 'easy-delivery-co_1.jpg')]:
    print(nm.ljust(18), meas(p))
on = small(T + 'all_on.png')
for k in ['all_off', 'no_S1', 'no_S2', 'no_S3', 'plus_T1', 'plus_T2', 'bldg_old']:
    print('효과', k, round(sum(ImageStat.Stat(ImageChops.difference(small(T + k + '.png'), on)).mean) / 3, 2))
W, H = 640, 360; rows = [('all_off', 'S1~S3 all OFF'), ('no_S1', 'S1 OFF'), ('no_S2', 'S2 OFF'), ('no_S3', 'S3 OFF'), ('plus_T1', 'T1 ON (+)'), ('plus_T2', 'T2 ON (+)'), ('bldg_old', 'old buildings')]
sheet = Image.new('RGB', (W * 2 + 10, (H + 24) * len(rows)), (30, 30, 30)); dr = ImageDraw.Draw(sheet); onb = Image.open(T + 'all_on.png').convert('RGB').resize((W, H), Image.NEAREST)
for i, (f, lab) in enumerate(rows):
    y = i * (H + 24); sheet.paste(Image.open(T + f + '.png').convert('RGB').resize((W, H), Image.NEAREST), (0, y + 24)); sheet.paste(onb, (W + 10, y + 24))
    dr.text((6, y + 6), lab + '   |   A4 default (S1-S3 on, T off, new bldg)', fill=(255, 255, 255))
sheet.save(A4 + 'toggles.png')
pv = Image.new('RGB', (W * 2 + 10, H * 2 + 10), (30, 30, 30))
for r, f in enumerate(['b_game', 'intro']):
    pv.paste(Image.open(E + f'scene-a3/{f}.png').convert('RGB').resize((W, H), Image.NEAREST), (0, r * (H + 10)))
    pv.paste(Image.open(A4 + f + '.png').convert('RGB').resize((W, H), Image.NEAREST), (W + 10, r * (H + 10)))
pv.save(A4 + 'a3_vs_a4.png'); print('saved')
