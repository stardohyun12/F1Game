# audit_compare.measure 의 게이트 4항목을 numpy 없이 같은 식으로 옮긴 것(과제 0429 — Python313 실행이 권한에 막힘).
# 검산: 기준 ①⑥⑫ 값이 docs/research/2026-10-27-audit-compare.json 과 같은지 main() 에서 비교한다.
import math
from PIL import Image, ImageFont
ROOT = 'D:/Github/F1Game/'
W, H, EDGE_T, GX, GY = 480, 270, 24.0, 16, 9


def font(sz, bold=False):
    for f in (('C:/Windows/Fonts/malgunbd.ttf' if bold else 'C:/Windows/Fonts/malgun.ttf'),):
        try: return ImageFont.truetype(f, sz)
        except OSError: pass
    return ImageFont.load_default()


def pct(xs, q):  # numpy linear percentile
    s = sorted(xs); k = (len(s) - 1) * q / 100; f = math.floor(k); c = min(f + 1, len(s) - 1)
    return s[f] + (s[c] - s[f]) * (k - f)


def refl(i, n):  # numpy pad 'reflect'
    while i < 0 or i >= n:
        i = -i if i < 0 else 2 * (n - 1) - i
    return i


def measure(name, path):
    im = Image.open(ROOT + path).convert('RGB')
    if im.size != (1920, 1080): im = im.resize((1920, 1080), Image.BOX)
    im = im.resize((W, H), Image.BOX)
    px = list(im.getdata())
    V = [v / 255.0 for v in im.convert('HSV').getdata(band=2)]
    L = [0.299 * r + 0.587 * g + 0.114 * b for r, g, b in px]
    # sobel (edge pad)
    def Lp(x, y): return L[min(max(y, 0), H - 1) * W + min(max(x, 0), W - 1)]
    edge = [False] * (W * H)
    for y in range(H):
        for x in range(W):
            gx = (Lp(x + 1, y - 1) + 2 * Lp(x + 1, y) + Lp(x + 1, y + 1)) - (Lp(x - 1, y - 1) + 2 * Lp(x - 1, y) + Lp(x - 1, y + 1))
            gy = (Lp(x - 1, y + 1) + 2 * Lp(x, y + 1) + Lp(x + 1, y + 1)) - (Lp(x - 1, y - 1) + 2 * Lp(x, y - 1) + Lp(x + 1, y - 1))
            edge[y * W + x] = math.hypot(gx, gy) / 4.0 > EDGE_T
    # dilate r=2 (np.roll = 감김)
    dil = [False] * (W * H)
    for y in range(H):
        for x in range(W):
            if edge[y * W + x]:
                for dy in range(-2, 3):
                    for dx in range(-2, 3): dil[((y + dy) % H) * W + (x + dx) % W] = True
    # 5×5 국소 표준편차(reflect), 채널 평균
    lstd = [0.0] * (W * H)
    for c in range(3):
        ch = [p[c] for p in px]
        for y in range(H):
            rows = [refl(y + d, H) * W for d in range(-2, 3)]
            for x in range(W):
                cols = [refl(x + d, W) for d in range(-2, 3)]
                s = s2 = 0.0
                for ry in rows:
                    for cx in cols:
                        v = ch[ry + cx]; s += v; s2 += v * v
                m = s / 25; lstd[y * W + x] += math.sqrt(max(s2 / 25 - m * m, 0)) / 3
    fl = [lstd[i] for i in range(W * H) if not dil[i]]
    # 화면 칸
    cw, chh = W // GX, H // GY; empty = []
    for j in range(GY):
        for i in range(GX):
            cnt = {}; e = 0
            for y in range(j * chh, (j + 1) * chh):
                for x in range(i * cw, (i + 1) * cw):
                    r, g, b = px[y * W + x]; k = (r // 32) * 64 + (g // 32) * 8 + b // 32; cnt[k] = cnt.get(k, 0) + 1; e += edge[y * W + x]
            colors = sum(1 for n in cnt.values() if n >= 3)
            empty.append(e / (cw * chh) < 0.02 and colors <= 4)
    bottom = empty[(GY // 2) * GX:]
    return {'name': name, 'path': path,
            'face_uniform_frac': sum(1 for v in fl if v < 1.5) / len(fl) if fl else None,
            'face_std_median': pct(fl, 50) if fl else None,
            'empty_cell_frac_bottom': sum(bottom) / len(bottom),
            'val_p5_p95': pct(V, 95) - pct(V, 5)}, im


if __name__ == '__main__':
    import json
    ref = {m['name']: m for m in json.load(open(ROOT + 'docs/research/2026-10-27-audit-compare.json', encoding='utf-8'))['main']}
    for n, m0 in ref.items():
        m, _ = measure(n, m0['path'])
        print(n, ' '.join(f"{k}: lite={m[k]:.4f} np={m0[k]:.4f}" for k in ('face_uniform_frac', 'face_std_median', 'empty_cell_frac_bottom', 'val_p5_p95')))
