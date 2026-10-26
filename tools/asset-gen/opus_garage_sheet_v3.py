"""정비소·고카트 v3 후처리 (D-165): 960×540 렌더 뒤에 하늘(960 에서 그림 → 같이 계단 확대) → 1920×1080 Nearest 2배
→ sheet.png: 위 = ①⑥⑫ 크롭 / 아래 = 우리 컷 크롭, 같은 1080 배율로 1× 줄과 4× 줄. + index.html(1×·흑실루엣·4×).

    python -X utf8 tools/asset-gen/opus_garage_sheet_v3.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-garage-kart-opus-v3')
REF = os.path.join(ROOT, 'art', 'reference', 'dotlp-2026-10-27')
TOP, HOR, CL, CS = (143, 192, 212), (191, 216, 216), (236, 238, 230), (212, 222, 222)
LABEL = {'g1_front': '정비소 정면', 'g2_34': '정비소 3/4', 'g3_inside': '정비소 안', 'k1_34': '카트 3/4',
         'k2_side': '카트 옆', 'k3_rear': '카트 뒤', 'x1_game': '게임 컷 — 카트 뒤 약 7m'}
CLOUDS = [(0.14, 0.34, 0.22), (0.55, 0.16, 0.17), (0.83, 0.42, 0.12)]
FONT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15)
# 크롭 중심(1080 좌표): 기준 = 차·물건 디테일 자리, 우리 = 카트·정비소 입구
REFS = [('① A Short Hike', 'a-short-hike_2', (1260, 330)), ('⑥ Easy Delivery Co.', 'easy-delivery-co_2', (900, 700)),
        ('⑫ Crow Country', 'crow-country_1', (1420, 900))]
OURS = [('게임 컷(카트)', 'x1_game', None), ('정비소 3/4', 'g2_34', None), ('정비소 안', 'g3_inside', (960, 600))]


def sky(w, h, horizon):
    im = Image.new('RGB', (w, h)); d = ImageDraw.Draw(im)
    for y in range(h):
        t = min(1.0, max(0.0, (horizon - y) / max(horizon, 1))) ** 0.8
        d.line((0, y, w, y), fill=tuple(round(HOR[i] + (TOP[i] - HOR[i]) * t) for i in range(3)))
    for cx, cy, cw in CLOUDS:  # 평평한 구름 덩어리(①): 밝은 위 + 그늘 아래 띠, 흐림 없음
        x0, y0, W = cx * w, cy * horizon, cw * w
        for ox, oy, rw, rh in ((0, 0, .5, .12), (.18, -.07, .3, .13), (-.2, .02, .28, .08), (.34, .03, .22, .06)):
            d.ellipse((x0 + (ox - rw) * W, y0 + (oy - rh) * W, x0 + (ox + rw) * W, y0 + (oy + rh) * W), fill=CL)
        d.rectangle((x0 - .45 * W, y0 + .07 * W, x0 + .55 * W, y0 + .12 * W), fill=HOR)
        d.rectangle((x0 - .4 * W, y0 + .05 * W, x0 + .5 * W, y0 + .07 * W), fill=CS)
    return im


def crop(im, c, w, h, k):
    cx, cy = c; cw, ch = w // k, h // k
    return im.crop((cx - cw // 2, cy - ch // 2, cx - cw // 2 + cw, cy - ch // 2 + ch)).resize((w, h), Image.NEAREST)


def main():
    meta = json.load(open(os.path.join(OUT, 'render_meta.json')))
    names = list(meta['views'])
    for n in names:
        fg = Image.open(os.path.join(OUT, '_raw', n + '.png')).convert('RGBA')
        al = fg.split()[3]
        hz = next((y for y in range(fg.height) if al.crop((0, y, fg.width, y + 1)).getextrema()[0] > 0), fg.height // 2)
        bg = sky(fg.width, fg.height, hz).convert('RGBA'); bg.alpha_composite(fg)
        bg.convert('RGB').resize((fg.width * 2, fg.height * 2), Image.NEAREST).save(os.path.join(OUT, n + '.png'))
    kc = json.load(open(os.path.join(OUT, 'kart_px.json'))) if os.path.exists(os.path.join(OUT, 'kart_px.json')) else {}
    TW, TH = 640, 360; rows = [('1× (1080 화면 그대로)', 1), ('4× 크롭 (같은 배율)', 4)]
    sh = Image.new('RGB', (TW * 3, 46 + 4 * (TH + 26)), (40, 42, 44)); d = ImageDraw.Draw(sh); d.font = FONT
    d.text((10, 6), f"아트2(Opus) 정비소·고카트 v3 — D-165: 960×540 렌더 → 1920×1080 Nearest 2배 · 텍스처 32px/m(카트 64) Point · "
           f"정비소 {meta['garage_tris']} / 카트 {meta['kart_tris']} tris", fill=(230, 230, 220))
    d.text((10, 24), "줄마다: 위 = 기준 ①⑥⑫, 아래 = 우리. 같은 1080 배율. 질문: 같은 픽셀 게임 계열인가 / 밝은 늦가을인가 / 장난감 같지 않나",
           fill=(200, 200, 190))
    y = 46
    for lab, k in rows:
        for i, (t, f, c) in enumerate(REFS):
            sh.paste(crop(Image.open(os.path.join(REF, f + '.jpg')).convert('RGB'), c, TW, TH, k), (i * TW, y + 22))
            d.text((i * TW + 6, y + 3), f'{t} · {lab}', fill=(230, 230, 220))
        y += TH + 26
        for i, (t, f, c) in enumerate(OURS):
            c = c or tuple(kc.get(f, (960, 700)))
            sh.paste(crop(Image.open(os.path.join(OUT, f + '.png')), c, TW, TH, k), (i * TW, y + 22))
            d.text((i * TW + 6, y + 3), f'우리 {t} · {lab}', fill=(255, 220, 160))
        y += TH + 26
    sh.save(os.path.join(OUT, 'sheet.png'))
    full = Image.new('RGB', (960 * 2, 46 + 4 * (540 + 26)), (40, 42, 44)); d = ImageDraw.Draw(full); d.font = FONT
    d.text((10, 14), '우리 v3 전체 컷(½ 축소 — 픽셀은 sheet.png·index.html 에서 1× 로)', fill=(230, 230, 220))
    for i, n in enumerate(names):
        x, yy = (i % 2) * 960, 46 + (i // 2) * (540 + 26)
        full.paste(Image.open(os.path.join(OUT, n + '.png')).resize((960, 540), Image.NEAREST), (x, yy + 22)); d.text((x + 6, yy + 3), LABEL[n], fill=(230, 230, 220))
    full.save(os.path.join(OUT, 'all_views.png'))
    figs = ''.join(f'<figure><img src="{n}.png"><figcaption>{LABEL[n]}</figcaption></figure>' for n in names)
    refs = ''.join(f'<figure><img src="../../reference/dotlp-2026-10-27/{f}.jpg"><figcaption>{t}</figcaption></figure>' for t, f, _ in REFS)
    open(os.path.join(OUT, 'index.html'), 'w', encoding='utf8').write(f"""<!doctype html><meta charset=utf-8>
<title>정비소·고카트 v3 (Opus, D-165)</title><style>body{{background:#2a2c2e;color:#ddd;font-family:sans-serif}}
img{{width:1920px;image-rendering:pixelated}} .x4 img{{width:7680px}} .sil img{{filter:brightness(0)}} figure{{margin:8px}}</style>
<p>도현 승인 필요 — 1× → 흑실루엣 → 4× 순서로(1× = 1920 원본 크기, 브라우저 배율 100%). <button onclick="document.body.className=''">1×</button>
<button onclick="document.body.className='sil'">흑실루엣</button><button onclick="document.body.className='x4'">4×</button>
<a href="sheet.png">sheet.png(기준과 나란히)</a> · <a href="all_views.png">전체 컷</a> · 정비소 {meta['garage_tris']} tris · 카트 {meta['kart_tris']} tris · 960×540 → 1920×1080 Nearest</p>
<h3>우리 v3</h3>{figs}<h3>기준 ①⑥⑫</h3>{refs}""")
    print('ok', names)


if __name__ == '__main__':
    main()
