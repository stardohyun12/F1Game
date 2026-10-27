# 과제 1726: 비교 시트 + 수치(가로등 색 흔들림, 그레인 세기)
from PIL import Image, ImageDraw, ImageChops
R = 'D:/Github/F1Game/docs/qa/evidence/'; d = R + 'scene-a6d/'
def row(files, labels, out, w=960, h=540):
    V = Image.new('RGB', (len(files) * (w + 10) - 10, h + 30), (30, 30, 30)); dr = ImageDraw.Draw(V)
    for i, (f, l) in enumerate(zip(files, labels)):
        V.paste(Image.open(f).convert('RGB').resize((w, h), Image.NEAREST), (i * (w + 10), 30)); dr.text((i * (w + 10) + 8, 8), l, fill=(255, 255, 255))
    V.save(out)
row([R + 'scene-a6c/b_game.png', d + 'b_game.png'], ['A6c (before)', 'A6d default: S2/S3 off, (a) surface grain on'], d + 'a6c_vs_a6d.png')
row([d + 'b_game.png', d + 'b_game_b_only.png', d + 'b_game_ab.png', d + 'b_game_off.png'], ['(a) only = default', '(b) only', '(a)+(b)', 'both off'], d + 'grain_4way.png', 720, 405)
row([d + 'light_close_off.png', d + 'light_close.png', d + 'kart_close.png'], ['streetlight, grain off', 'streetlight, (a) on', 'kart v6b, (a) on'], d + 'close_set.png', 640, 360)
# 그레인이 화면을 얼마나 흔드나: 'both off' 대비 픽셀 밝기 차의 평균·p95 (0~255)
off = Image.open(d + 'b_game_off.png').convert('L')
for f in ['b_game.png', 'b_game_b_only.png', 'b_game_ab.png']:
    df = sorted(ImageChops.difference(off, Image.open(d + f).convert('L')).getdata())
    print(f'{f:18s} vs off: mean |dL| {sum(df)/len(df):.2f}  p95 {df[len(df)*95//100]}  max {df[-1]}')
print('a6c vs a6d(off) mean |dL|', round(sum(ImageChops.difference(Image.open(R + 'scene-a6c/b_game.png').convert('L'), off).getdata()) / (1920 * 1080), 2))
# 채도(HSV S 평균) A6c vs A6d
for lab, f in [('A6c', R + 'scene-a6c/b_game.png'), ('A6d', d + 'b_game.png')]:
    s = list(Image.open(f).convert('HSV').getdata()); print(lab, 'mean sat', round(sum(p[1] for p in s) / len(s), 1), 'mean V', round(sum(p[2] for p in s) / len(s), 1))
