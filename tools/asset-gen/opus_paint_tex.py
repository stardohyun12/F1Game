"""정비소·고카트 v2 부드러운 손칠 텍스처 (D-164: EDC 방식 + 밝게). PIL 만 — 사진·AI 없음.
큰 색면 + 0.3~1.5m 저주파 얼룩(paint_atlas.noise) + 녹·기름·덧댄 판자는 큰 덩어리만. Bilinear 로 쓴다 — 텍셀 네모가 안 보이게.
반복 금지: 바닥은 12m 한 장(uv fit), 벽·지붕은 8m 한 장. 판(포스터·보드)은 v1 도트 그림을 8배 키워 흐리게(그린 그림, 도트 아님).

    python -X utf8 tools/asset-gen/opus_paint_tex.py
"""
import os, random, sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops
sys.path.insert(0, os.path.dirname(__file__))
import paint_atlas as pa

ROOT = pa.ROOT
SRC_V1 = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex')
OUT = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex_v2')
TILE = {}  # 텍스처 한 장이 덮는 미터(world uv)


def hx(h):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def base(size, col, seed, amp=9.0, parts=((4, 1.0, 1.0), (9, .5, 1.0)), warm=.06):
    return pa.tint(hx(col), pa.mix_noise(size, list(parts), seed), amp, warm)


def blob(im, x, y, rx, ry, col, a, blur, wrap=True):
    """부드러운 큰 덩어리(녹·기름·칠 자국). wrap 이면 이음매 없이."""
    m = Image.new('L', im.size, 0); d = ImageDraw.Draw(m); W, H = im.size
    offs = (-W, 0, W) if wrap else (0,)
    offy = (-H, 0, H) if wrap else (0,)
    for ox in offs:
        for oy in offy: d.ellipse((x + ox - rx, y + oy - ry, x + ox + rx, y + oy + ry), fill=int(255 * a))
    m = m.filter(ImageFilter.GaussianBlur(blur))
    im.paste(Image.new('RGB', im.size, hx(col)), (0, 0), m)


def band(im, y0, y1, col, a, blur, vertical=False):
    m = Image.new('L', im.size, 0); d = ImageDraw.Draw(m); W, H = im.size
    d.rectangle((y0, 0, y1, H) if vertical else (0, y0, W, y1), fill=int(255 * a))
    im.paste(Image.new('RGB', im.size, hx(col)), (0, 0), m.filter(ImageFilter.GaussianBlur(blur)))


def masked(im, mask_l, col, a, blur=6):
    m = mask_l.point(lambda v: int(min(255, v * a))).filter(ImageFilter.GaussianBlur(blur))
    im.paste(Image.new('RGB', im.size, hx(col)), (0, 0), m)


def thresh(n, t, soft=24):
    return n.point(lambda v: max(0, min(255, int((v - t) * 255 / soft))))


S = 512


def tin_wall(seed, col='#86a092'):
    """함석 8m: 바랜 청록 큰 색면, 1m 판 이음 흐린 줄, 아래 녹 띠는 저주파 경계, 녹물 몇 줄기."""
    TILE['tin_wall'] = 8.0; px = S / 8
    # v2b: 판정 「청록/녹 얼룩이 크게 뜬다」 → 바탕 흔들림·녹 대비를 낮춰 큰 색면이 먼저 읽히게
    im = base(S, col, seed, amp=6)
    for k in range(8): band(im, int(k * px), int(k * px) + 2, '#5f7a70', .35, 1.5, vertical=True)
    n = pa.noise(S, 5, seed + 3)
    grad = Image.linear_gradient('L').resize((S, S))  # 위 0 → 아래 255
    rust = ImageChops.multiply(thresh(ImageChops.add(n, grad, 1.0, -150), 90, 60), grad)
    masked(im, rust, '#a3683f', .45, 5)
    rnd = random.Random(seed)
    for _ in range(5):
        x = rnd.uniform(0, S); y = rnd.uniform(S * .1, S * .5)
        blob(im, x, y + 60, 5, 70, '#9a6a48', .3, 6)
    blob(im, S * .3, S * .35, 40, 28, '#a3683f', .3, 10); blob(im, S * .8, S * .2, 30, 20, '#9a6a48', .25, 9)
    return im


def tin_roof(seed):
    TILE['tin_roof'] = 8.0; px = S / 10
    im = base(S, '#b26e5a', seed, amp=11)
    for k in range(10): band(im, int(k * px), int(k * px) + 3, '#8e5446', .4, 2, vertical=True)
    n = pa.noise(S, 4, seed + 9)
    masked(im, thresh(n, 150, 40), '#8a5236', .8, 8)
    blob(im, S * .6, S * .7, 70, 40, '#9a5d48', .5, 14)
    return im


def planks(seed, col, paint, fresh, name):
    """판자 8m: 판 25cm 마다 흐린 틈, 판마다 톤 다름(±), 옛 칠 큰 자국 2~3, 덧댄 새 판 2장(큰 덩어리)."""
    TILE[name] = 8.0; px = S / 8; bh = px * .25
    im = base(S, col, seed, amp=8, parts=((4, 1.0, 1.0), (14, .4, 4.0)))
    rnd = random.Random(seed)
    for i in range(int(S / bh)):
        y = i * bh
        band(im, int(y), int(y + bh), '#000000' if rnd.random() < .5 else '#ffffff', rnd.uniform(.02, .07), 1)
        band(im, int(y + bh - 2), int(y + bh), '#3f3024', .55, 1.2)
    for (x, y, rx, ry) in ((S * .2, S * .3, 90, 26), (S * .7, S * .62, 120, 34), (S * .45, S * .9, 70, 20)):
        blob(im, x, y, rx, ry, paint, .55, 12)
    for (x0, row) in ((S * .1, 9), (S * .6, 21)):
        y = row * bh
        m = Image.new('L', im.size, 0); ImageDraw.Draw(m).rectangle((x0, y + 1, x0 + px * 1.4, y + bh - 2), fill=210)
        im.paste(Image.new('RGB', im.size, hx(fresh)), (0, 0), m.filter(ImageFilter.GaussianBlur(1.5)))
    return im


def floor(seed):
    """바닥 12m 한 장(반복 없음): 콘크리트 큰 얼룩 + 기름 자국은 손으로 정한 자리·크기(0.4~1.5m)."""
    TILE['floor'] = 12.0; px = S / 12
    im = base(S, '#bdb4a3', seed, amp=9)
    for (x, y, rx, ry, a) in ((3.0, 7.5, 1.4, .9, .55), (8.6, 4.8, .9, .6, .5), (6.0, 2.2, .5, .35, .45),
                              (1.6, 10.6, .6, .5, .4), (9.8, 9.6, 1.1, .7, .45), (4.4, 5.6, .35, .3, .5)):
        blob(im, x * px, S - y * px, rx * px, ry * px, '#6f6a60', a, 7, wrap=False)
        blob(im, x * px + 4, S - y * px + 2, rx * px * .5, ry * px * .5, '#5a5650', a * .7, 5, wrap=False)
    for x in (4.0, 8.0): band(im, int(x * px), int(x * px) + 1, '#958d7e', .5, 1, vertical=True)
    for y in (6.0,): band(im, int(S - y * px), int(S - y * px) + 1, '#958d7e', .5, 1)
    for x in (5.2, 6.4):  # 바퀴 자국 두 줄(입구→안)
        band(im, int(x * px), int(x * px) + 6, '#a39a8a', .35, 4, vertical=True)
    return im


def ground(seed):
    """늦가을 풀밭 16m: 올리브 + 큰 황토 얼룩 + 짙은 녹색 그늘 얼룩(자연은 진하게)."""
    TILE['ground'] = 16.0
    # v2b: 자연은 진하게(지브리) — 올리브 → 짙은 풀색, 늦가을 황토는 옅게만
    im = base(S, '#5e7c3c', seed, amp=10, parts=((3, 1.0, 1.0), (7, .6, 1.0)))
    n = pa.noise(S, 4, seed + 1); masked(im, thresh(n, 140, 50), '#93924c', .35, 10)
    n2 = pa.noise(S, 5, seed + 2); masked(im, thresh(n2, 160, 40), '#566a42', .6, 10)
    return im


def dirt(seed):
    TILE['dirt'] = 12.0
    im = base(S, '#c4b391', seed, amp=10)
    n = pa.noise(S, 5, seed + 4); masked(im, thresh(n, 150, 50), '#a99878', .6, 10)
    for x in (.42, .58): band(im, int(S * x), int(S * x) + 14, '#b2a07e', .5, 6, vertical=True)
    return im


def simple(name, col, seed, amp=8, size=256, tile=1.0, extra=None):
    TILE[name] = tile
    im = base(size, col, seed, amp=amp)
    if extra: extra(im)
    return im


def tire_x(im):
    W, H = im.size; band(im, 0, H // 8, '#5a5b5e', .6, 3)
    for k in range(16): band(im, int(k * W / 16), int(k * W / 16) + 3, '#2a2b2e', .5, 2, vertical=True)


def drum_x(band_col, rust_at):
    def f(im):
        W, H = im.size
        for y in (.08, .32, .68, .92): band(im, int(H * y) - 4, int(H * y) + 4, band_col, .7, 2)
        blob(im, W * .12, H * .5, 8, H * .4, '#ffffff', .18, 6)  # 하이라이트 세로 띠
        for (x, y, r) in rust_at: blob(im, W * x, H * y, r, r * .7, '#7a4e34', .7, 8)
    return f


def kart_x(im):
    W, H = im.size
    blob(im, W * .25, H * .3, 26, 18, '#9aa0a0', .85, 3)  # 칠 벗겨진 쇠
    blob(im, W * .7, H * .75, 18, 12, '#9aa0a0', .8, 3)
    m = Image.new('L', im.size, 0); d = ImageDraw.Draw(m)
    d.rectangle((W * .5, H * .22, W * .92, H * .34), fill=235); d.rectangle((W * .12, H * .6, W * .22, H * .95), fill=235)
    im.paste(Image.new('RGB', im.size, hx('#d0ccba')), (0, 0), m.filter(ImageFilter.GaussianBlur(1.5)))  # 테이프
    blob(im, W * .82, H * .55, 10, 8, '#6e4434', .7, 4)


def crate_x(im):
    W, H = im.size
    for y in (.3, .62, .97): band(im, int(H * y) - 3, int(H * y) + 2, '#6b4f36', .65, 1.5)
    for x in (.03, .97): band(im, int(W * x) - 6, int(W * x) + 6, '#a8834f', .6, 2, vertical=True)
    blob(im, W * .5, H * .46, 22, 14, '#4f6a6a', .55, 3)


def from_pixel(name, scale=8, blur=2.5):
    """v1 의 판 그림(포스터·보드·스티커·창·공구판)을 그린 그림으로: 8배 키우고 흐리게 — 도트가 아니라 가장자리 부드러운 색면."""
    im = Image.open(os.path.join(SRC_V1, name + '.png')).convert('RGB')
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST).filter(ImageFilter.GaussianBlur(blur))


def leaf(seed):
    """늦가을 나무: 짙은 초록 바탕 + 황토·녹슨 주황 큰 덩어리(자연은 진하게)."""
    TILE['leaf'] = 4.0
    im = base(S, '#3c6236', seed, amp=10, parts=((3, 1.0, 1.0), (6, .7, 1.0)))  # v2b: 짙은 초록, 단풍은 몇 점만
    n = pa.noise(S, 4, seed + 7); masked(im, thresh(n, 150, 40), '#8f8a44', .35, 12)
    n2 = pa.noise(S, 5, seed + 8); masked(im, thresh(n2, 175, 30), '#b0703e', .45, 10)
    n3 = pa.noise(S, 4, seed + 9); masked(im, thresh(n3, 165, 40), '#2f4a3e', .6, 12)
    return im


def main():
    os.makedirs(OUT, exist_ok=True)
    T = {
        'tin_wall': tin_wall(11), 'tin_roof': tin_roof(12),
        'planks': planks(13, '#b08a5e', '#8fa5ab', '#d0b282', 'planks'),
        'planks_dark': planks(14, '#8c6c4a', '#7d9590', '#bf9e70', 'planks_dark'),
        'floor': floor(15), 'ground': ground(16), 'dirt': dirt(17), 'leaf': leaf(18),
        'tire': simple('tire', '#3e3f42', 19, amp=5, extra=tire_x),
        'drum_red': simple('drum_red', '#a85646', 20, extra=drum_x('#7a3b30', ((.6, .8, 30), (.85, .3, 18)))),
        'drum_blue': simple('drum_blue', '#5f7f96', 21, extra=drum_x('#435e72', ((.4, .85, 26), (.7, .5, 14)))),
        'drum_top': simple('drum_top', '#8d8a82', 22, extra=lambda im: blob(im, 160, 100, 40, 30, '#5a5650', .6, 10)),
        'wood': simple('wood', '#8a6a48', 23, amp=10, tile=2.0), 'metal': simple('metal', '#6c7476', 24, amp=7, tile=2.0),
        'kart_body': simple('kart_body', '#ad5b4c', 25, amp=8, tile=1.0, extra=kart_x),
        'crate': simple('crate', '#c9a36e', 26, amp=8, extra=crate_x),
        'strap': simple('strap', '#7a6a46', 27, amp=6), 'seat': simple('seat', '#44566a', 28, amp=8,
                                                                         extra=lambda im: blob(im, 170, 70, 30, 14, '#d0ccba', .9, 2)),
        'lamp': simple('lamp', '#5a7a76', 29, amp=5, extra=lambda im: band(im, 190, 256, '#f2dc9a', 1, 2)),
        'sea': simple('sea', '#3f8095', 30, amp=6, size=512, tile=40.0), 'stone': simple('stone', '#cfc4ac', 31, amp=10, tile=4.0),
    }
    for n in ('poster_a', 'poster_b', 'record', 'slots', 'pegboard', 'sticker', 'window', 'parts'):
        T[n] = from_pixel(n)
    for n, im in T.items():
        im.save(os.path.join(OUT, n + '.png'))
    import json
    json.dump(TILE, open(os.path.join(OUT, 'tile_m.json'), 'w'), indent=1)
    for n in ('tin_wall', 'planks', 'floor', 'ground', 'kart_body'):
        print(f'{n:10s} speck {pa.speck_pct(T[n]):.3f}%  lumStd {pa.lum_std(T[n]):.1f}')


if __name__ == '__main__':
    main()
