"""artlive 20261031-1926: 짙은 숲 후보 비교판 sheet.png (F1~F4 × 멀리 무리 · 카트 무리 · 단독) + 단계 쌍 그림.
사용: python -X utf8 docs/qa/tmp/forest_sheet.py <NN>   (쌍 = 직전 단계와 같은 캠 F1 멀리)"""
import pathlib, sys
from PIL import Image, ImageDraw, ImageFont
R = pathlib.Path('D:/Github/F1Game'); EV = R / 'docs/qa/evidence/iter-forest'; RV = R / 'art/_review/2026-10-31-forest'; RV.mkdir(parents=True, exist_ok=True)
nn = int(sys.argv[1]) if len(sys.argv) > 1 else 1; S = f's{nn:02d}'
CW, CH, LW, HH = 560, 315, 210, 34
ROWS = [('F1', '침엽 A', '삼나무·편백형\n잎 사이프러스 A\n껍질 삼나무'), ('F2', '침엽 B', '삼나무·편백형\n잎 유럽곰솔\n껍질 삼나무(chinese)'),
        ('F3', '활엽 A', '후박·구실잣밤형\n잎 동백(광택)\n껍질 팽나무(chinese)'), ('F4', '활엽 B', '후박·구실잣밤형\n잎 느티나무속\n껍질 팽나무(japanese)')]
def font(sz):
    for f in ('C:/Windows/Fonts/malgunbd.ttf', 'C:/Windows/Fonts/malgun.ttf'):
        try: return ImageFont.truetype(f, sz)
        except OSError: pass
    return ImageFont.load_default()
F, Fs, Fb = font(20), font(15), font(40)
W = LW + 3 * CW; Ht = HH + 4 * (CH + 6)
sh = Image.new('RGB', (W, Ht), (28, 28, 30)); d = ImageDraw.Draw(sh)
for j, t in enumerate(['무리 · 멀리 (~50m, 집 너머 비탈)', '무리 · 카트 높이 2.8m', '단독 · 눈높이 1.6m']):
    d.text((LW + j * CW + 8, 7), t, fill=(200, 200, 200), font=F)
for i, (fn, nm, ph) in enumerate(ROWS):
    y = HH + i * (CH + 6)
    d.text((10, y + 8), fn, fill=(255, 220, 90), font=Fb); d.text((10, y + 60), nm, fill=(235, 235, 235), font=F); d.multiline_text((10, y + 92), ph, fill=(180, 180, 180), font=Fs, spacing=4)
    for v, view in enumerate(('far', 'kart', 'single')):
        sh.paste(Image.open(EV / f'{S}_{fn}_{view}.png').convert('RGB').resize((CW, CH), Image.LANCZOS), (LW + v * CW, y))
sh.save(RV / 'sheet.png'); print('sheet', sh.size)
def pair(l, r, out, tl, tr):
    a, b = Image.open(l).convert('RGB'), Image.open(r).convert('RGB'); P = Image.new('RGB', (a.width + b.width + 8, a.height + 36), (28, 28, 30))
    P.paste(a, (0, 36)); P.paste(b, (a.width + 8, 36)); q = ImageDraw.Draw(P); q.text((8, 6), tl, fill=(230, 230, 230), font=F); q.text((a.width + 16, 6), tr, fill=(230, 230, 230), font=F); P.save(out)
if nn >= 2:
    for c in sys.argv[2:] or ['F1_far']:
        pair(EV / f's{nn - 1:02d}_{c}.png', EV / f'{S}_{c}.png', EV / f'{S}_pair{"" if c == "F1_far" else "_" + c}.png', f's{nn - 1:02d} {c}', f'{S} {c}')
print('ok')
