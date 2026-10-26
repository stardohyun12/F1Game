# 아트 감사(2026-10-27) — v5 게임 컷 vs 기준 ①⑥⑫ 같은 크기 측정 + 비교 그림
# 실행: C:/Users/rtx40/AppData/Local/Programs/Python/Python313/python.exe docs/research/audit_compare.py
# 출력: docs/research/2026-10-27-audit-compare.png, docs/research/2026-10-27-audit-compare.json
import json, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
R = lambda p: os.path.join(ROOT, p)
MAIN = [
    ('우리 v5 x1_game', 'art/_review/2026-10-27-garage-kart-opus-v5/x1_game.png'),
    ('① A Short Hike', 'art/reference/dotlp-2026-10-27/a-short-hike_1.jpg'),
    ('⑥ Easy Delivery Co.', 'art/reference/dotlp-2026-10-27/easy-delivery-co_1.jpg'),
    ('⑫ Crow Country', 'art/reference/dotlp-2026-10-27/crow-country_1.jpg'),
]
TREND = [
    ('우리 v1 x1_game', 'art/_review/2026-10-27-garage-kart-opus/x1_game.png'),
    ('우리 v2 x1_game', 'art/_review/2026-10-27-garage-kart-opus-v2/x1_game.png'),
    ('우리 v4 x1_game', 'art/_review/2026-10-27-garage-kart-opus-v4/x1_game.png'),
]
W, H = 480, 270          # 측정 해상도 = 기준 게임 내부 해상도 근처(화면 블록 4~5px @1080)
EDGE_T = 24.0            # Sobel 크기 문턱(0~255 밝기)
GX, GY = 16, 9           # 화면 칸(30×30px @480)


def load(p):
    im = Image.open(R(p)).convert('RGB')
    if im.size != (1920, 1080):
        im = im.resize((1920, 1080), Image.BOX)
    return im


def box_std(a, k):
    """k×k 창 국소 표준편차(가장자리 반사 패딩)."""
    r = k // 2
    p = np.pad(a, r, mode='reflect').astype(np.float64)
    def win_sum(x):
        c = np.cumsum(np.cumsum(x, 0), 1)
        c = np.pad(c, ((1, 0), (1, 0)))
        return c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]
    n = k * k
    m = win_sum(p) / n
    m2 = win_sum(p * p) / n
    return np.sqrt(np.maximum(m2 - m * m, 0))


def sobel(L):
    p = np.pad(L, 1, mode='edge')
    gx = (p[:-2, 2:] + 2 * p[1:-1, 2:] + p[2:, 2:]) - (p[:-2, :-2] + 2 * p[1:-1, :-2] + p[2:, :-2])
    gy = (p[2:, :-2] + 2 * p[2:, 1:-1] + p[2:, 2:]) - (p[:-2, :-2] + 2 * p[:-2, 1:-1] + p[:-2, 2:])
    return np.hypot(gx, gy) / 4.0


def dilate(m, r):
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def block_px(full):
    """1080 화면에서 같은 색이 가로로 이어지는 길이 중앙값(화면 블록 크기 추정, JPEG 오차 허용 8)."""
    a = np.asarray(full.convert('L'), dtype=np.int16)
    runs = []
    for y in range(100, 1000, 37):
        row = a[y]
        d = np.abs(np.diff(row)) > 8
        idx = np.flatnonzero(d)
        if len(idx) > 2:
            runs.extend(np.diff(idx).tolist())
    runs = np.array(runs)
    runs = runs[runs <= 40]
    return float(np.median(runs)) if len(runs) else None


def measure(name, path):
    full = load(path)
    im = full.resize((W, H), Image.BOX)
    rgb = np.asarray(im, dtype=np.float64)
    hsv = np.asarray(im.convert('HSV'), dtype=np.float64) / 255.0
    V = hsv[..., 2]
    S = (rgb.max(2) - rgb.min(2)) / 255.0   # 채도 = 크로마(max-min). HSV S 는 어두운 화소(⑫)에서 부풀어 안 씀
    L = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
    mag = sobel(L)
    edge = mag > EDGE_T
    flat = ~dilate(edge, 2)
    lstd = np.mean([box_std(rgb[..., c], 5) for c in range(3)], axis=0)
    # 화면 칸 디테일: 칸마다 색 수(채널 8단계 양자화, 3px 이상) + 엣지 비율
    q = (rgb // 32).astype(np.int32)
    code = q[..., 0] * 64 + q[..., 1] * 8 + q[..., 2]
    cw, ch = W // GX, H // GY
    colors = np.zeros((GY, GX)); ecell = np.zeros((GY, GX))
    for j in range(GY):
        for i in range(GX):
            c = code[j * ch:(j + 1) * ch, i * cw:(i + 1) * cw].ravel()
            u, n = np.unique(c, return_counts=True)
            colors[j, i] = int((n >= 3).sum())
            ecell[j, i] = edge[j * ch:(j + 1) * ch, i * cw:(i + 1) * cw].mean()
    empty = (ecell < 0.02) & (colors <= 4)
    m = {
        'name': name, 'path': path,
        'sat_mean': float(S.mean()), 'sat_p90': float(np.percentile(S, 90)),
        'sat_hi_frac': float((S > 0.6).mean()),
        'val_mean': float(V.mean()), 'val_p5_p95': float(np.percentile(V, 95) - np.percentile(V, 5)),
        'flat_frac': float(flat.mean()),
        'face_std_median': float(np.median(lstd[flat])) if flat.any() else None,
        'face_std_p75': float(np.percentile(lstd[flat], 75)) if flat.any() else None,
        'face_uniform_frac': float((lstd[flat] < 1.5).mean()) if flat.any() else None,
        'edge_density': float(edge.mean()),
        'cell_colors_median': float(np.median(colors)),
        'cell_colors_mean': float(colors.mean()),
        'empty_cell_frac': float(empty.mean()),
        'empty_cell_frac_bottom': float(empty[GY // 2:].mean()),
        'dark_frac': float((V < 0.25).mean()),
        'hist_s': np.histogram(S, bins=20, range=(0, 1))[0].tolist(),
        'hist_v': np.histogram(V, bins=20, range=(0, 1))[0].tolist(),
    }
    vis = {'im': im, 'lstd': lstd, 'edge': edge, 'colors': colors, 'empty': empty}
    return m, vis


def font(sz, bold=False):
    f = 'C:/Windows/Fonts/malgunbd.ttf' if bold else 'C:/Windows/Fonts/malgun.ttf'
    return ImageFont.truetype(f, sz)


def hist_img(h, color, w=480, hh=110, ymax=None):
    img = Image.new('RGB', (w, hh), (250, 250, 248))
    d = ImageDraw.Draw(img)
    h = np.array(h, dtype=np.float64); h = h / h.sum()
    ymax = ymax or h.max()
    bw = w / len(h)
    for i, v in enumerate(h):
        y = hh - 14 - (hh - 20) * min(v / ymax, 1)
        d.rectangle([i * bw + 1, y, (i + 1) * bw - 1, hh - 14], fill=color)
    d.line([0, hh - 14, w, hh - 14], fill=(120, 120, 120))
    d.text((2, hh - 13), '0', fill=(90, 90, 90), font=font(10)); d.text((w - 12, hh - 13), '1', fill=(90, 90, 90), font=font(10))
    return img


def heat(a, vmax, w=480, h=270, empty=None):
    a = np.clip(a / vmax, 0, 1)
    r = (255 * np.clip(a * 1.6, 0, 1)).astype(np.uint8)
    g = (255 * np.clip(a * 1.6 - 0.6, 0, 1)).astype(np.uint8)
    b = (255 * np.clip(0.35 - a, 0, 1) * 2).astype(np.uint8)
    img = Image.fromarray(np.dstack([r, g, b])).resize((w, h), Image.NEAREST)
    if empty is not None:
        d = ImageDraw.Draw(img)
        gy, gx = empty.shape
        cw, ch = w / gx, h / gy
        for j in range(gy):
            for i in range(gx):
                if empty[j, i]:
                    d.line([i * cw, j * ch, (i + 1) * cw, (j + 1) * ch], fill=(255, 255, 255), width=1)
                    d.line([(i + 1) * cw, j * ch, i * cw, (j + 1) * ch], fill=(255, 255, 255), width=1)
    return img


def main():
    res, vis = [], []
    for n, p in MAIN:
        m, v = measure(n, p); res.append(m); vis.append(v)
    trend = [measure(n, p)[0] for n, p in TREND if os.path.exists(R(p))]

    COLW, PAD, LBLW = 480, 10, 190
    rows = [('화면\n(480×270 측정본)', 270), ('채도(크로마)\n히스토그램 0→1', 110), ('명도\n히스토그램 0→1', 110),
            ('면 안 색 변화\n5×5 국소 표준편차\n(파랑=단색 면,\n 0→20)', 270), ('엣지\nSobel>24', 270),
            ('화면 칸 색 수\n16×9칸, 0→40\nX = 빈 칸\n(엣지<2%·색≤4)', 270)]
    TBL = 340
    Wt = LBLW + len(MAIN) * (COLW + PAD) + PAD
    Ht = 60 + sum(h + PAD for _, h in rows) + 40 + TBL
    canvas = Image.new('RGB', (Wt, Ht), (255, 255, 255))
    d = ImageDraw.Draw(canvas)
    d.text((PAD, 12), '아트 감사 비교 — 우리 v5 게임 컷 vs 기준 ①⑥⑫ (모두 1920×1080 → 480×270 BOX 축소 후 같은 조건 측정)', fill=(20, 20, 20), font=font(22, True))
    smax = max(max(np.array(m['hist_s']) / sum(m['hist_s'])) for m in res)
    vmax = max(max(np.array(m['hist_v']) / sum(m['hist_v'])) for m in res)
    y = 60
    for ri, (lab, hgt) in enumerate(rows):
        d.multiline_text((PAD, y + 4), lab, fill=(40, 40, 40), font=font(15, True), spacing=3)
        for ci, (m, v) in enumerate(zip(res, vis)):
            x = LBLW + ci * (COLW + PAD)
            if ri == 0:
                tile = v['im']
            elif ri == 1:
                tile = hist_img(m['hist_s'], (220, 120, 60), ymax=smax)
            elif ri == 2:
                tile = hist_img(m['hist_v'], (80, 110, 170), ymax=vmax)
            elif ri == 3:
                tile = heat(v['lstd'], 20)
            elif ri == 4:
                e = (v['edge'] * 255).astype(np.uint8)
                tile = Image.fromarray(255 - e).convert('RGB')
            else:
                tile = heat(v['colors'], 40, empty=v['empty'])
            canvas.paste(tile, (x, y))
            if ri == 0:
                d.rectangle([x, y, x + 300, y + 24], fill=(0, 0, 0))
                d.text((x + 6, y + 2), m['name'], fill=(255, 255, 255), font=font(15, True))
        y += hgt + PAD
    # 표
    y += 10
    cols = ['항목'] + [m['name'] for m in res]
    keys = [('채도(크로마) 평균', 'sat_mean', '{:.2f}'), ('크로마>0.6 면적', 'sat_hi_frac', '{:.0%}'),
            ('명도 평균', 'val_mean', '{:.2f}'), ('명도 폭 p5–p95', 'val_p5_p95', '{:.2f}'),
            ('면 안 변화 중앙값', 'face_std_median', '{:.1f}'), ('면 안 거의 단색(<1.5) 비율', 'face_uniform_frac', '{:.0%}'),
            ('엣지 밀도', 'edge_density', '{:.1%}'), ('칸당 색 수 중앙값', 'cell_colors_median', '{:.0f}'),
            ('빈 칸 비율', 'empty_cell_frac', '{:.0%}'), ('아래 절반 빈 칸 비율', 'empty_cell_frac_bottom', '{:.0%}'),
            ('어두운 면적(명도<0.25)', 'dark_frac', '{:.0%}')]
    cw0 = 260; cw = (Wt - 2 * PAD - cw0) // len(res)
    rh = 26
    for ci, c in enumerate(cols):
        x = PAD + (0 if ci == 0 else cw0 + (ci - 1) * cw)
        d.rectangle([x, y, x + (cw0 if ci == 0 else cw) - 2, y + rh - 2], fill=(40, 40, 40))
        d.text((x + 6, y + 3), c, fill=(255, 255, 255), font=font(14, True))
    for ki, (lab, k, fmt) in enumerate(keys):
        yy = y + (ki + 1) * rh
        bg = (245, 245, 242) if ki % 2 == 0 else (255, 255, 255)
        d.rectangle([PAD, yy, Wt - PAD, yy + rh - 2], fill=bg)
        d.text((PAD + 6, yy + 3), lab, fill=(20, 20, 20), font=font(14))
        for ci, m in enumerate(res):
            x = PAD + cw0 + ci * cw
            val = m[k]
            txt = fmt.format(val) if val is not None else '-'
            d.text((x + 6, yy + 3), txt, fill=(20, 20, 20) if ci else (170, 40, 30), font=font(14, ci == 0))
    canvas.save(R('docs/research/2026-10-27-audit-compare.png'))
    for m in res + trend:
        m.pop('hist_s'); m.pop('hist_v')
    json.dump({'main': res, 'trend': trend, 'params': {'W': W, 'H': H, 'EDGE_T': EDGE_T, 'grid': [GX, GY]}},
              open(R('docs/research/2026-10-27-audit-compare.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    for m in res + trend:
        print(m['name'], {k: (round(v, 3) if isinstance(v, float) else v) for k, v in m.items() if k not in ('name', 'path')})


if __name__ == '__main__':
    main()
