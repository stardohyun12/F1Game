"""정비소·고카트 v2 후처리 (D-164): 1280×720 렌더 뒤에 밝은 오후 하늘(위 #9fc2cf → 지평 = 안개 #c3d5d2, 매끈한 그라데이션 +
흐린 구름) 깔기 → 1920×1080 BILINEAR 확대 → sheet.png(왼쪽 EDC 2장 / 오른쪽 우리 렌더) + index.html.

    python -X utf8 tools/asset-gen/opus_garage_sheet_v2.py
"""
import json, os
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-garage-kart-opus-v2')
EDC = os.path.join(ROOT, 'art', 'reference', 'edc-2026-10-26')
TOP, HOR, CL = (159, 194, 207), (195, 213, 210), (232, 236, 228)
LABEL = {'g1_front': '정비소 정면', 'g2_34': '정비소 3/4', 'g3_inside': '정비소 안', 'k1_34': '카트 3/4',
         'k2_side': '카트 옆', 'k3_rear': '카트 뒤(짐상자 낮게)', 'x1_game': '게임 컷 — 카트 뒤 약 7m'}
CLOUDS = [(0.16, 0.30, 0.30), (0.58, 0.18, 0.22), (0.86, 0.38, 0.16)]  # x, y(지평선 비), 폭(화면비)
FONT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 14)  # 한글 라벨(기본 폰트는 한글이 깨진다)


def sky(w, h, horizon):
    sky_im = Image.new('RGB', (w, h)); g = ImageDraw.Draw(sky_im)
    for y in range(h):
        t = min(1.0, max(0.0, (horizon - y) / max(horizon, 1))) ** 0.8
        g.line((0, y, w, y), fill=tuple(round(HOR[i] + (TOP[i] - HOR[i]) * t) for i in range(3)))
    cl = Image.new('L', (w, h), 0); d = ImageDraw.Draw(cl)
    for cx, cy, cw in CLOUDS:
        x0, y0, W = cx * w, cy * horizon, cw * w
        for ox, oy, rw, rh in ((0, 0, .5, .09), (.18, -.05, .3, .1), (-.15, .02, .3, .06), (.3, .02, .25, .05)):
            xa, ya = x0 + ox * W, y0 + oy * W
            d.ellipse((xa - rw * W, ya - rh * W, xa + rw * W, ya + rh * W), fill=110)
    cl = cl.filter(ImageFilter.GaussianBlur(w * 0.012))
    return Image.composite(Image.new('RGB', (w, h), CL), sky_im, cl)


def specks(im):
    """1px 반점 비율: 8이웃 모두와 밝기 차 > 24 인 픽셀(EDC 분석과 같은 정의)."""
    a = im.convert('L'); m = None
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if not (dy or dx): continue
            k = ImageChops.difference(a, ImageChops.offset(a, dx, dy)).point(lambda v: 255 if v > 24 else 0)
            m = k if m is None else ImageChops.darker(m, k)
    hist = m.crop((1, 1, a.width - 1, a.height - 1)).histogram()
    return 100.0 * hist[255] / sum(hist)


def fit(im, W, H):
    s = max(W / im.width, H / im.height); im = im.resize((round(im.width * s), round(im.height * s)), Image.BILINEAR)
    x, y = (im.width - W) // 2, (im.height - H) // 2
    return im.crop((x, y, x + W, y + H))


def main():
    meta = json.load(open(os.path.join(OUT, 'render_meta.json')))
    names = list(meta['views']); sp = {}
    for n in names:
        fg = Image.open(os.path.join(OUT, '_raw', n + '.png')).convert('RGBA')
        al = fg.split()[3]
        hz = next((y for y in range(fg.height) if al.crop((0, y, fg.width, y + 1)).getextrema()[0] > 0), fg.height // 2)
        bg = sky(fg.width, fg.height, hz).convert('RGBA'); bg.alpha_composite(fg)
        lo = bg.convert('RGB'); lo.save(os.path.join(OUT, '_raw', n + '_720.png'))
        sp[n] = round(specks(lo), 3)
        lo.resize((1920, 1080), Image.BILINEAR).save(os.path.join(OUT, n + '.png'))
    W, H = 960, 540; TW, TH = 640, 360
    rest = [n for n in names if n not in ('x1_game', 'g2_34')]
    sh = Image.new('RGB', (W * 2, 40 + 2 * (H + 28) + ((len(rest) + 2) // 3) * (TH + 28)), (40, 42, 44)); d = ImageDraw.Draw(sh); d.font = FONT
    d.text((10, 8), f"아트2(Opus) 정비소·고카트 v2 — D-164: 1280×720 렌더 → 1080 부드럽게 확대 · 손칠 텍스처(Bilinear) · 평평한 빛 + 청록 그늘 · "
           f"정비소 {meta['garage_tris']} / 카트 {meta['kart_tris']} tris", fill=(230, 230, 220))
    d.text((10, 22), "왼쪽 = EDC 실제 화면(참고), 오른쪽 = 우리 렌더. 질문: 같은 게임 계열로 보이나? 밝은 지브리 색인가?", fill=(200, 200, 190))
    pairs = [('edc_02.jpg', 'x1_game'), ('edc_04.jpg', 'g2_34')]
    for r, (e, n) in enumerate(pairs):
        y = 40 + r * (H + 28)
        sh.paste(fit(Image.open(os.path.join(EDC, e)).convert('RGB'), W, H), (0, y + 24))
        sh.paste(fit(Image.open(os.path.join(OUT, n + '.png')), W, H), (W, y + 24))
        d.text((8, y + 6), f'EDC {e}', fill=(230, 230, 220)); d.text((W + 8, y + 6), LABEL[n], fill=(230, 230, 220))
    y0 = 40 + 2 * (H + 28)
    for i, n in enumerate(rest):
        x, y = (i % 3) * TW, y0 + (i // 3) * (TH + 28)
        sh.paste(fit(Image.open(os.path.join(OUT, n + '.png')), TW, TH), (x, y + 24)); d.text((x + 8, y + 6), LABEL[n], fill=(230, 230, 220))
    sh.save(os.path.join(OUT, 'sheet.png'))
    edc_sp = {e: round(specks(fit(Image.open(os.path.join(EDC, e)).convert('RGB'), 1280, 720)), 3) for e, _ in pairs}
    json.dump({'specks_pct_720': sp, 'edc_specks_pct': edc_sp}, open(os.path.join(OUT, 'measure.json'), 'w'), indent=1)
    figs = ''.join(f'<figure><img src="{n}.png"><figcaption>{LABEL[n]} · 반점 {sp[n]}%</figcaption></figure>' for n in names)
    edc = ''.join(f'<figure><img src="../../reference/edc-2026-10-26/{e}"><figcaption>EDC {e}</figcaption></figure>' for e, _ in pairs)
    open(os.path.join(OUT, 'index.html'), 'w', encoding='utf8').write(f"""<!doctype html><meta charset=utf-8>
<title>정비소·고카트 v2 (Opus, D-164)</title><style>body{{background:#2a2c2e;color:#ddd;font-family:sans-serif}}
img{{width:960px}} .x4 img{{width:3840px;image-rendering:pixelated}} .sil img{{filter:brightness(0)}} figure{{display:inline-block;margin:8px;vertical-align:top}}</style>
<p>도현 승인 필요 — 1× → 흑실루엣 → 4× 순서로. <button onclick="document.body.className=''">1×</button>
<button onclick="document.body.className='sil'">흑실루엣</button><button onclick="document.body.className='x4'">4×</button>
<a href="sheet.png">sheet.png</a> · 정비소 {meta['garage_tris']} tris · 카트 {meta['kart_tris']} tris · 렌더 1280×720 → 1920×1080 bilinear</p>
<h3>EDC (참고)</h3>{edc}<h3>우리 v2</h3>{figs}""")
    print('ok', sp, edc_sp)


if __name__ == '__main__':
    main()
