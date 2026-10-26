"""형태 언어 시트(D-166 B): 기준 ①⑥⑫⑦ 크롭 + 측정 선, 정비소 v4 흑실루엣 전/후.
좌표는 opus_grid_crop.py 50px 격자에서 읽은 원본(1920×1080) px. 눈대중 오차 ±10px.
    python -X utf8 tools/asset-gen/opus_shape_sheet.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
REF = os.path.join(ROOT, 'art', 'reference', 'dotlp-2026-10-27')
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-shape-language')
RAW = os.path.join(OUT, '_raw')
F = lambda s: ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', s)
PW, PH = 960, 540
Y, C, M, W = (255, 225, 40), (40, 230, 255), (255, 80, 200), (255, 255, 255)

# (파일, 크롭, 제목, [(x0,y0,x1,y1,색,글)], [(x0,y0,x1,y1,색,글) 상자])
REFS = [
    ('a-short-hike_1.jpg', (60, 140, 1180, 770), '① A Short Hike — 사람이 벽만큼 크다',
     [(474, 468, 474, 576, Y, '벽 108'), (373, 556, 373, 650, C, '문 94 = 벽 .87'),
      (444, 505, 444, 580, C, '창 75 = 벽 .7'), (648, 430, 648, 560, M, '사람 130 = 벽 1.2'),
      (66, 522, 96, 522, Y, '처마 30 = 벽 .28'), (492, 444, 492, 468, Y, '지붕끝 24 = 벽 .22'),
      (1160, 150, 1160, 505, W, '나무 355 = 벽 3.3 · 사람 2.7')], []),
    ('easy-delivery-co_2.jpg', (60, 0, 1660, 900), '⑥ Easy Delivery Co. — 기운 전신주·큰 나무',
     [(952, 400, 952, 458, Y, '벽 58'), (796, 372, 796, 400, Y, '지붕 28 = 벽 .5'),
      (962, 140, 962, 372, W, '나무 230 = 벽 4'), (130, 130, 205, 470, M, '전신주 12°'),
      (1545, 245, 1505, 465, M, '10°'), (752, 860, 1078, 860, C, '트럭 폭 326')],
     [(1146, 438, 1250, 500, C, '표지판 다리 2')]),
    ('crow-country_1.jpg', (0, 0, 1920, 1080), '⑫ Crow Country — 소품이 실제보다 크다',
     [(1095, 515, 1095, 985, M, '사람 470'), (870, 515, 870, 640, M, '머리 125 = .27'),
      (225, 545, 225, 720, C, '콘 175 = 사람 .37 (실제 .28)'), (522, 500, 522, 560, C, '캔 60 = .13 (실제 .07)'),
      (300, 350, 300, 555, Y, '자루 205 = .44')],
     [(445, 90, 655, 250, Y, '간판 폭 = 사람 .45')]),
    ('jalopy_1.jpg', (900, 560, 1700, 1010), '⑦ Jalopy — 외길, 차가 길을 채운다',
     [(1475, 898, 1218, 772, C, '차 길이 285'), (1410, 700, 1290, 950, Y, '길 폭 ~280 (눈대중)'),
      (1380, 690, 1440, 690, W, '덤불')], []),
]


def ref_panel(fn, crop, title, lines, boxes):
    im = Image.open(os.path.join(REF, fn)).convert('RGB').crop(crop)
    sx, sy = PW / im.width, PH / im.height
    im = im.resize((PW, PH), Image.LANCZOS); d = ImageDraw.Draw(im)
    T = lambda x, y: ((x - crop[0]) * sx, (y - crop[1]) * sy)
    for x0, y0, x1, y1, col, txt in lines:
        a, b = T(x0, y0), T(x1, y1)
        d.line([a, b], fill=(0, 0, 0), width=6); d.line([a, b], fill=col, width=3)
        for p in (a, b): d.ellipse([p[0] - 4, p[1] - 4, p[0] + 4, p[1] + 4], fill=col, outline=(0, 0, 0))
        tx, ty = max(a[0], b[0]) + 8, (a[1] + b[1]) / 2 - 10
        if tx > PW - 230: tx = min(a[0], b[0]) - 8 - d.textlength(txt, font=F(17))
        if abs(b[0] - a[0]) > abs(b[1] - a[1]) and abs(b[1] - a[1]) > 5:  # 비스듬히 누운 선: 끝점(b) 위에
            tx, ty = b[0] - d.textlength(txt, font=F(17)) / 2, b[1] - 30
        d.text((tx, ty), txt, font=F(17), fill=col, stroke_width=3, stroke_fill=(0, 0, 0))
    for x0, y0, x1, y1, col, txt in boxes:
        a, b = T(x0, y0), T(x1, y1)
        d.rectangle([a, b], outline=(0, 0, 0), width=5); d.rectangle([a, b], outline=col, width=2)
        d.text((a[0], b[1] + 4), txt, font=F(17), fill=col, stroke_width=3, stroke_fill=(0, 0, 0))
    d.rectangle([0, 0, PW, 34], fill=(20, 20, 20)); d.text((10, 4), title, font=F(20), fill=W)
    return im


def sil(mode, view):
    im = Image.open(os.path.join(RAW, f'{mode}_{view}.png')).convert('RGBA')
    cov = sum(1 for a in im.getchannel('A').getdata() if a > 0) / (im.width * im.height)
    bg = Image.new('RGBA', im.size, (233, 228, 214, 255)); bg.alpha_composite(im)
    im = bg.convert('RGB').resize((1920, 1080), Image.NEAREST).resize((PW, PH), Image.BOX)  # 게임 조건(1.5배 Nearest) 뒤 절반
    return im, cov


def main():
    tris = {m: json.load(open(os.path.join(RAW, f'{m}_tris.json'))) for m in ('before', 'after')}
    views = [('g1_front', '정면'), ('g2_34', '3/4'), ('x1_game', '게임 컷(카트 뒤 7m)')]
    head = 90
    S = Image.new('RGB', (PW * 2, head + PH * 2 + 50 + PH * 3), (24, 24, 24)); d = ImageDraw.Draw(S)
    d.text((16, 10), '형태 언어 (D-166 B) — 기준 측정 + 정비소 v4 흑실루엣 전/후', font=F(32), fill=W)
    d.text((16, 52), '선 = 원본 1920×1080 위 50px 격자에서 읽은 px(±10). 비율이 규칙이다 — docs/design/SHAPE.md', font=F(18), fill=(200, 200, 200))
    for i, r in enumerate(REFS):
        S.paste(ref_panel(*r), ((i % 2) * PW, head + (i // 2) * PH))
    y0 = head + PH * 2
    d.text((16, y0 + 10), f'왼쪽 = v4 그대로(정비소 {tris["before"]["garage"]} · 주변 {tris["before"]["env"]} tris)   '
           f'오른쪽 = SHAPE 규칙 적용(정비소 {tris["after"]["garage"]} · 주변 {tris["after"]["env"]} tris)', font=F(20), fill=W)
    cov = {}
    for j, (v, name) in enumerate(views):
        for k, m in enumerate(('before', 'after')):
            im, c = sil(m, v); cov[f'{m}_{v}'] = round(c, 3)
            dd = ImageDraw.Draw(im)
            dd.text((10, 8), f'{"전" if m == "before" else "후"} · {name} · 화면 덮음 {c * 100:.0f}%', font=F(20),
                    fill=(20, 20, 20), stroke_width=3, stroke_fill=(233, 228, 214))
            S.paste(im, (k * PW, y0 + 50 + j * PH))
    S.save(os.path.join(OUT, 'sheet.png'))
    json.dump(cov, open(os.path.join(OUT, 'coverage.json'), 'w'), indent=1)
    print(cov)


main()
