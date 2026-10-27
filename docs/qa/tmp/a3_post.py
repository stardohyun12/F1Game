# 과제 1124 후처리: toggles.png(항목별 '그 항목만 끈 판 | 전부 켬'), prev_vs_a3.png(위 B, 아래 소개; 왼 A2f 오른 A3), 측정
#  측정 = 320×180 bilinear: 밝기 L 평균 · 채도 HSV S 평균 · p10. 아래 빈 칸 = 화면 아래 절반을 16×16 칸으로 나눠 L 표준편차 < 6 인 칸 비율(가설 정의)
#  효과 크기 = 그 항목을 끈 판과 전부 켬 판의 320×180 RGB 평균 절대차(0~255)
from PIL import Image, ImageDraw, ImageStat, ImageChops
E = 'docs/qa/evidence/'; A3 = E + 'scene-a3/'; T = A3 + 'toggles/'
def small(p): return Image.open(p).convert('RGB').resize((320, 180), Image.BILINEAR)
def meas(p):
    im = small(p); L = sorted(im.convert('L').get_flattened_data()); S = im.convert('HSV').getchannel('S')
    lo = im.convert('L').crop((0, 90, 320, 180)); flat = tot = 0
    for y in range(0, 90, 16):
        for x in range(0, 320, 16):
            tot += 1; flat += ImageStat.Stat(lo.crop((x, y, x + 16, min(90, y + 16)))).stddev[0] < 6
    return round(sum(L) / len(L)), round(ImageStat.Stat(S).mean[0]), L[len(L) // 10], round(100 * flat / tot)
for nm, p in [('A2f B', E + 'scene-a2f/b_game.png'), ('A3 B', A3 + 'b_game.png'), ('A2f intro', E + 'scene-a2f/intro.png'), ('A3 intro', A3 + 'intro.png'), ('A3 D', A3 + 'd.png'),
              ('all_off B', T + 'all_off.png'), ('①_1', 'art/reference/dotlp-2026-10-27/a-short-hike_1.jpg'), ('①_2', 'art/reference/dotlp-2026-10-27/a-short-hike_2.jpg'),
              ('⑥_1', 'art/reference/dotlp-2026-10-27/easy-delivery-co_1.jpg'), ('⑥_2', 'art/reference/dotlp-2026-10-27/easy-delivery-co_2.jpg')]:
    print(nm, '밝기/채도/p10/아래빈칸%', meas(p))
on = small(T + 'all_on.png'); eff = []
for k in ['R1', 'R2', 'R3', 'M1', 'M2', 'M3']:
    d = ImageStat.Stat(ImageChops.difference(small(T + f'no_{k}.png'), on)).mean; eff.append((round(sum(d) / 3, 2), k))
print('효과(끈 판 vs 켬, 평균 절대차):', sorted(eff, reverse=True))
print('전부 끔 vs 전부 켬:', round(sum(ImageStat.Stat(ImageChops.difference(small(T + 'all_off.png'), on)).mean) / 3, 2))
# 시트: 줄마다 [끈 판 | 켬] 640×360 두 장
rows = [('all_off', '전부 끔(=A2f)')] + [(f'no_{k}', k) for _, k in sorted(eff, reverse=True)]
W, H = 640, 360; sheet = Image.new('RGB', (W * 2 + 10, (H + 24) * len(rows)), (30, 30, 30)); dr = ImageDraw.Draw(sheet)
onb = Image.open(T + 'all_on.png').convert('RGB').resize((W, H), Image.NEAREST)
for i, (f, lab) in enumerate(rows):
    y = i * (H + 24); sheet.paste(Image.open(T + f + '.png').convert('RGB').resize((W, H), Image.NEAREST), (0, y + 24)); sheet.paste(onb, (W + 10, y + 24))
    dr.text((6, y + 6), (lab + ' OFF' if f != 'all_off' else lab) + '   |   ON (all)', fill=(255, 255, 255))
sheet.save(A3 + 'toggles.png')
pv = Image.new('RGB', (W * 2 + 10, H * 2 + 10), (30, 30, 30))
for r, f in enumerate(['b_game', 'intro']):
    pv.paste(Image.open(E + f'scene-a2f/{f}.png').convert('RGB').resize((W, H), Image.NEAREST), (0, r * (H + 10)))
    pv.paste(Image.open(A3 + f + '.png').convert('RGB').resize((W, H), Image.NEAREST), (W + 10, r * (H + 10)))
pv.save(A3 + 'prev_vs_a3.png'); print('saved')
