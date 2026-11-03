"""artlive 20261031-0142: 나무 후보 비교판 sheet.png + 단계 쌍 그림.
사용: python -X utf8 docs/qa/tmp/tree_sheet.py <NN>   (NN = 캡처 단계, 쌍은 직전 단계와)"""
import pathlib, sys
from PIL import Image, ImageDraw, ImageFont
R = pathlib.Path('D:/Github/F1Game'); EV = R / 'docs/qa/evidence/iter-trees'; RV = R / 'art/_review/2026-10-31-trees'
nn = int(sys.argv[1]) if len(sys.argv) > 1 else 2; S = f's{nn:02d}'
CW, CH, LW, HH = 480, 270, 150, 34
SP = [('곰솔', 1), ('팽나무', 3), ('동백 관목', 5), ('사이프러스', 7)]
PH = {1: '잎 소나무 바늘 · 껍질 옹이 소나무', 2: '잎 유럽곰솔 · 껍질 소나무', 3: '잎 팽나무 · 껍질 팽나무(chinese)', 4: '잎 느티나무속 · 껍질 팽나무(japanese)',
      5: '잎 동백(어두운 광택) · 껍질 벚나무', 6: '잎 올리브 · 껍질 플라타너스', 7: '잎 사이프러스 A · 껍질 삼나무', 8: '잎 사이프러스 B · 껍질 삼나무(chinese)'}
def font(sz):
    for f in ('C:/Windows/Fonts/malgunbd.ttf', 'C:/Windows/Fonts/malgun.ttf'):
        try: return ImageFont.truetype(f, sz)
        except OSError: pass
    return ImageFont.load_default()
F, Fs, Fb = font(20), font(15), font(40)
def cell(p): return Image.open(p).convert('RGB').resize((CW, CH), Image.LANCZOS)
W = LW + 4 * CW; Ht = HH + 4 * (CH + HH) + HH + CH
sh = Image.new('RGB', (W, Ht), (28, 28, 30)); d = ImageDraw.Draw(sh)
for j, t in enumerate(['A 눈높이 1.6m', 'A 카트 2.8m', 'B 눈높이 1.6m', 'B 카트 2.8m']):
    d.text((LW + j * CW + 8, 7), t, fill=(200, 200, 200), font=F)
for i, (nm, a) in enumerate(SP):
    y = HH + i * (CH + HH)
    d.text((10, y + CH // 2 - 30), nm, fill=(235, 235, 235), font=F)
    for k, n in enumerate((a, a + 1)):
        for v, view in enumerate(('eye', 'kart')):
            x = LW + (k * 2 + v) * CW; sh.paste(cell(EV / f'{S}_T{n}_{view}.png'), (x, y))
        x = LW + k * 2 * CW
        d.rectangle((x + 6, y + 6, x + 92, y + 54), fill=(20, 20, 20)); d.text((x + 14, y + 6), f'T{n}', fill=(255, 220, 90), font=Fb)
        d.text((x + 8, y + CH + 6), PH[n], fill=(190, 190, 190), font=Fs)
y = HH + 4 * (CH + HH); d.text((LW + 8, y + 7), '전체 — A 벌(T1·T3·T5·T7)  /  B 벌(T2·T4·T6·T8)  · 집·밭과 같은 빛', fill=(200, 200, 200), font=F)
for k, s in enumerate('AB'):
    sh.paste(Image.open(EV / f'{S}_set{s}_all.png').convert('RGB').resize((2 * CW, CH), Image.LANCZOS) .crop((0, 0, 2 * CW, CH)), (LW + k * 2 * CW, y + HH))
sh.save(RV / 'sheet.png'); print('sheet', sh.size)
# 쌍: 같은 캠, 직전 단계 | 이번 단계 (s01 은 직전이 없어 A벌 | B벌)
def pair(l, r, out, tl, tr):
    a, b = Image.open(l).convert('RGB'), Image.open(r).convert('RGB'); P = Image.new('RGB', (a.width + b.width + 8, a.height + 36), (28, 28, 30))
    P.paste(a, (0, 36)); P.paste(b, (a.width + 8, 36)); q = ImageDraw.Draw(P); q.text((8, 6), tl, fill=(230, 230, 230), font=F); q.text((a.width + 16, 6), tr, fill=(230, 230, 230), font=F); P.save(out)
pair(EV / 's01_setA_all.png', EV / 's01_setB_all.png', EV / 's01_pair.png', 's01 A벌', 's01 B벌')
if nn >= 2:
    pair(EV / f's{nn - 1:02d}_T5_eye.png', EV / f'{S}_T5_eye.png', EV / f'{S}_pair.png', f's{nn - 1:02d} T5 눈높이', f'{S} T5 눈높이')
# artlive 20261031-0243 손질 전/후: python -X utf8 docs/qa/tmp/tree_sheet.py <NN> <전NN> → fix_pair.png (팽나무 T3·T4 · 동백 T5·T6, 같은 캠)
if len(sys.argv) > 2:
    B = f's{int(sys.argv[2]):02d}'; ROWS = [('T3_eye', '팽나무 T3 눈높이'), ('T4_kart', '팽나무 T4 카트'), ('T5_eye', '동백 T5 눈높이'), ('T6_eye', '동백 T6 눈높이'), ('T6_kart', '동백 T6 카트')]
    PW, PHh = 640, 360; FP = Image.new('RGB', (LW + 2 * PW + 8, HH + len(ROWS) * (PHh + 8)), (28, 28, 30)); q = ImageDraw.Draw(FP)
    q.text((LW + 8, 7), f'전 ({B}, 0142 비교판)', fill=(230, 230, 230), font=F); q.text((LW + PW + 16, 7), f'후 ({S}, 손질)', fill=(230, 230, 230), font=F)
    for i, (c, t) in enumerate(ROWS):
        y = HH + i * (PHh + 8); q.text((10, y + PHh // 2 - 12), t, fill=(235, 235, 235), font=Fs)
        FP.paste(Image.open(EV / f'{B}_{c}.png').convert('RGB').resize((PW, PHh), Image.LANCZOS), (LW, y))
        FP.paste(Image.open(EV / f'{S}_{c}.png').convert('RGB').resize((PW, PHh), Image.LANCZOS), (LW + PW + 8, y))
    FP.save(RV / 'fix_pair.png'); print('fix_pair', FP.size)
print('pair ok')
