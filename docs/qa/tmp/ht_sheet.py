"""artlive 20261031-0205 집 유형 비교판: 마을 눈높이·카트(A|B) + 단독 8장(H1A~H4B) → art/_review/2026-10-31-house-types/sheet.png. 사용: python -X utf8 docs/qa/tmp/ht_sheet.py <단계번호> [직전번호]"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
R = Path('D:/Github/F1Game'); EV = R / 'docs/qa/evidence/iter-house-types'; RV = R / 'art/_review/2026-10-31-house-types'
nn = sys.argv[1].zfill(2); pv = sys.argv[2].zfill(2) if len(sys.argv) > 2 else None
try: F = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 30); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
except OSError: F = FS = ImageFont.load_default()
NAME = {'H1': '단독 2층 박공', 'H2': '붙은 연립 3칸', 'H3': '좁은 3층', 'H4': '가게주택'}
def im(f): return Image.open(EV / f).convert('RGB')
def lab(d, x, y, t, s=''):
    a = d.textlength(t, font=F); b = d.textlength(s, font=FS); d.rectangle([x, y, x + 24 + a + b, y + 40], fill=(20, 20, 20)); d.text((x + 8, y + 2), t, font=F, fill=(255, 236, 160)); d.text((x + 14 + a, y + 10), s, font=FS, fill=(235, 235, 235))
W = 1280 * 2 + 30; H = 60 + 2 * (720 + 20) + 2 * (360 + 20) + 20
S = Image.new('RGB', (W, H), (238, 234, 224)); d = ImageDraw.Draw(S)
d.text((12, 12), '배경 집 유형 후보 — A = 주인공 집 재료(살구벽·초록덧창·기와) / B = 다른 마을 톤(황토·크림벽, 청회·회색덧창, 다른 기와·붉은 골강판)', font=FS, fill=(30, 30, 30))
y = 60
for row in ('village_eye', 'village_kart'):
    for k, v in enumerate('AB'):
        x = 10 + k * 1290; S.paste(im(f's{nn}_set{v}_{row}.png'), (x, y)); lab(d, x + 8, y + 8, '세트 ' + v, ' 마을 ' + ('눈높이 1.6m' if 'eye' in row else '카트 2.8m'))
    y += 740
for v in 'AB':
    for i in range(4):
        h = f'H{i + 1}'; x = 10 + i * 645; S.paste(im(f's{nn}_{h}{v}_solo.png').resize((640, 360), Image.NEAREST), (x, y)); lab(d, x + 6, y + 6, h + v, ' ' + NAME[h])
    y += 380
S.save(RV / 'sheet.png'); print('sheet', S.size)
if pv:
    a, b = im(f's{pv}_setA_village_eye.png'), im(f's{nn}_setA_village_eye.png'); P = Image.new('RGB', (2570, 720), (238, 234, 224)); P.paste(a, (0, 0)); P.paste(b, (1290, 0))
    d2 = ImageDraw.Draw(P); lab(d2, 8, 8, 's' + pv); lab(d2, 1298, 8, 's' + nn); P.save(EV / f's{nn}_pair.png'); print('pair')
