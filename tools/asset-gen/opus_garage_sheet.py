"""정비소·고카트 렌더 후처리: Look 6 하늘(위 #3b8fc4 → 지평 #a7cdd6, 도트 적운 2~3) 깔기 + sheet.png + index.html.

    python -X utf8 tools/asset-gen/opus_garage_sheet.py
"""
import json, os
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'art', '_review', '2026-10-27-garage-kart-opus')
TOP, HOR, CL, CS = (59, 143, 196), (167, 205, 214), (238, 240, 230), (195, 214, 218)
LABEL = {'g1_front': '정비소 정면', 'g2_34': '정비소 3/4', 'g3_inside': '정비소 안', 'k1_34': '카트 3/4',
         'k2_side': '카트 옆', 'k3_rear': '카트 뒤(짐상자)', 'x1_game': '1× 게임 컷(카트까지 약 7m)'}
CLOUDS = [(0.18, 0.16, 0.20), (0.62, 0.10, 0.14), (0.84, 0.24, 0.10)]  # x, y(화면비), 폭


def sky(w, h, horizon):
    im = Image.new('RGB', (w, h)); d = ImageDraw.Draw(im); step = 6  # 6px 띠 = 도트 하늘(그라데이션 대신 계단)
    for y in range(0, h, step):
        t = min(1.0, max(0.0, (horizon - y) / max(horizon, 1)))
        t = 1 - (1 - t) ** 3
        d.rectangle((0, y, w, y + step), fill=tuple(int(HOR[i] + (TOP[i] - HOR[i]) * t) for i in range(3)))
    for cx, cy, cw in CLOUDS:
        x0, y0, W = int(cx * w), int(cy * horizon), int(cw * w); u = 6
        for k, (ox, oy, rw) in enumerate(((0, 0, 1.0), (.15, -.5, .5), (.45, -.8, .35), (.3, -.3, .6))):
            xa, ya = x0 + int(ox * W), y0 + int(oy * W * .25)
            d.rectangle((xa // u * u, ya // u * u, (xa + int(rw * W)) // u * u, (y0 + int(W * .12)) // u * u), fill=CL)
        d.rectangle((x0 // u * u, (y0 + int(W * .08)) // u * u, (x0 + W) // u * u, (y0 + int(W * .12)) // u * u), fill=CS)
    return im


def main():
    meta = json.load(open(os.path.join(OUT, 'render_meta.json')))
    names = list(meta['views'])
    for n in names:
        fg = Image.open(os.path.join(OUT, '_raw', n + '.png')).convert('RGBA')
        a = fg.split()[3]; hz = next((y for y in range(fg.height) if max(a.crop((0, y, fg.width, y + 1)).getdata()) > 0
                                      and min(a.crop((0, y, fg.width, y + 1)).getdata()) > 0), fg.height // 2)
        bg = sky(fg.width, fg.height, hz).convert('RGBA'); bg.alpha_composite(fg); bg.convert('RGB').save(os.path.join(OUT, n + '.png'))
    W, H = 960, 540; cols = 2; rows = (len(names) + 1 + 1) // 2
    sh = Image.new('RGB', (W * cols, (H + 28) * rows + 40), (40, 42, 44)); d = ImageDraw.Draw(sh)
    d.text((10, 10), f"아트2(Opus) 정비소·고카트 v1 — 도트 기반 로우폴리(D-163) · 정비소 {meta['garage_tris']} tris · 카트 {meta['kart_tris']} tris · 16px/m Point",
           fill=(230, 230, 220))
    tiles = names + ['x1_game_4x']
    for i, n in enumerate(tiles):
        x, y = (i % cols) * W, 40 + (i // cols) * (H + 28)
        if n == 'x1_game_4x':
            g = Image.open(os.path.join(OUT, 'x1_game.png')); cw, ch = W // 4, H // 4
            cx, cy = int(W * .5), int(H * .84)  # 카트 자리
            im = g.crop((cx - cw // 2, cy - ch // 2, cx + cw // 2, cy + ch // 2)).resize((W, H), Image.NEAREST)
            lab = '1× 게임 컷 가운데 4× (텍셀 확인)'
        else:
            im = Image.open(os.path.join(OUT, n + '.png')); lab = LABEL[n]
        sh.paste(im, (x, y + 24)); d.text((x + 8, y + 6), lab, fill=(230, 230, 220))
    sh.save(os.path.join(OUT, 'sheet.png'))
    imgs = ''.join(f'<figure><img src="{n}.png"><figcaption>{LABEL[n]}</figcaption></figure>' for n in names)
    open(os.path.join(OUT, 'index.html'), 'w', encoding='utf8').write(f"""<!doctype html><meta charset=utf-8>
<title>정비소·고카트 v1 (Opus)</title><style>body{{background:#2a2c2e;color:#ddd;font-family:sans-serif}}
img{{image-rendering:pixelated;width:960px}} .x4 img{{width:3840px}} .sil img{{filter:brightness(0)}} figure{{display:inline-block;margin:8px}}</style>
<p>도현 승인 필요 — 1× → 흑실루엣 → 4× 순서로. <button onclick="document.body.className=''">1×</button>
<button onclick="document.body.className='sil'">흑실루엣</button><button onclick="document.body.className='x4'">4×</button>
<a href="sheet.png">sheet.png</a> · 정비소 {meta['garage_tris']} tris · 카트 {meta['kart_tris']} tris</p>{imgs}""")
    print('ok', len(names))


if __name__ == '__main__':
    main()
