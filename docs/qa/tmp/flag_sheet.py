"""깃발 크기 전/후 시트 + 대비 측정. 사용: python -I flag_sheet.py <증거폴더>
- 컷마다 깃발(Signal) 픽셀 bbox, 받침 상자(Ink) bbox, 상자/깃발 바깥 4px 고리의 밝기 중앙값, WCAG 대비(고리 vs Ink, Signal vs Ink)를 측정해 contrast.json 에 쓴다.
- pair_<지점>_<노온|노을>.png: 왼쪽 원래 / 오른쪽 새 전체 화면(2배) + 깃발 주변 6배 확대.
"""
import json, os, sys
from PIL import Image
import numpy as np

d = sys.argv[1]
INK = np.array([0x23, 0x30, 0x3B]); SIG = np.array([0xD9, 0x3A, 0x2B]); PAP = np.array([0xF3, 0xE9, 0xD2])

def lum(c):
    c = np.asarray(c, float) / 255.0
    c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return float(0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2])

def cr(a, b):
    la, lb = lum(a), lum(b)
    hi, lo = max(la, lb), min(la, lb)
    return round((hi + 0.05) / (lo + 0.05), 2)

def bbox(mask):
    ys, xs = np.where(mask)
    if len(xs) == 0: return None
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())

logs = {}
for l in open(os.path.join(d, 'dest_log.jsonl'), encoding='utf-8'):
    j = json.loads(l)
    if 'shot' in j: logs[j['shot']] = j

out = {}
for shot, j in logs.items():
    f = os.path.join(d, shot + '.png')
    if not os.path.exists(f) or j['mode'] not in ('edge', 'on'): continue
    im = np.array(Image.open(f).convert('RGB')).astype(int)
    H, W = im.shape[:2]
    px, py = j['px']; py_img = H - 1 - py  # 로그는 왼쪽 아래 기준
    x0, x1 = max(0, int(px) - 30), min(W, int(px) + 40)
    y0, y1 = max(0, int(py_img) - 50), min(H, int(py_img) + 30)
    win = im[y0:y1, x0:x1]
    # 화면은 톤맵을 거친다 → 코드 상수 대신 컷에서 신호색(붉은 점)·잉크(그 둘레 어두운 점)를 직접 뽑는다.
    red = (win[..., 0] > 150) & (win[..., 1] < 100) & (win[..., 2] < 90) & (win[..., 0] - win[..., 1] > 90)
    if red.sum() < 20: out[shot] = {'flag': None}; continue
    SIGm = np.median(win[red], axis=0)
    sig = np.all(np.abs(win - SIGm) < 14, axis=2)
    sb = bbox(sig)
    sub = win[max(0, sb[1] - 1):sb[3] + 2, max(0, sb[0] - 1):sb[2] + 2].reshape(-1, 3)
    dark = sub[[lum(c) < 0.045 for c in sub]]
    INKm = np.median(dark, axis=0) if len(dark) > 5 else np.array(INK)
    ink = np.all(np.abs(win - INKm) < 5, axis=2)
    sbb = (sb[0] + x0, sb[1] + y0, sb[2] + x0, sb[3] + y0)
    # 받침(Ink) bbox = 깃발 bbox 를 품는 Ink 덩어리(깃발 주변 14px 안)
    pad = 14
    ib = bbox(ink[max(0, sb[1] - pad):sb[3] + pad, max(0, sb[0] - pad):sb[2] + pad])
    ox, oy = max(0, sb[0] - pad), max(0, sb[1] - pad)
    ibb = (ib[0] + ox + x0, ib[1] + oy + y0, ib[2] + ox + x0, ib[3] + oy + y0) if ib else None
    box = ibb if (j['mode'] == 'edge' and ibb) else sbb
    r = 4
    bx0, by0, bx1, by1 = box[0] - r, box[1] - r, box[2] + r, box[3] + r
    ring = []
    for y in range(max(0, by0), min(H, by1 + 1)):
        for x in range(max(0, bx0), min(W, bx1 + 1)):
            if x < bx0 + 2 or x > bx1 - 2 or y < by0 + 2 or y > by1 - 2: ring.append(im[y, x])
    ring = np.array(ring)
    med = np.median(ring, axis=0)
    out[shot] = {
        'flagBBox': sbb, 'flagWH': [sbb[2] - sbb[0] + 1, sbb[3] - sbb[1] + 1], 'inkBBox': ibb,
        'ringMedianRGB': [int(v) for v in med],
        'signalRGB': [int(v) for v in SIGm], 'inkRGB': [int(v) for v in INKm],
        'contrast_ring_vs_ink': cr(med, INKm),
        'contrast_signal_vs_ink': cr(SIGm, INKm),
        'contrast_signal_vs_ring': cr(SIGm, med),
        'ring_dark_share': round(float(np.mean([lum(p) < 0.08 for p in ring])), 2),
    }
json.dump(out, open(os.path.join(d, 'contrast.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

# 비교 시트
def crop(shot):
    j = logs[shot]; im = Image.open(os.path.join(d, shot + '.png')).convert('RGB')
    W, H = im.size; px, py = j['px']; py = H - 1 - py
    c = im.crop((max(0, int(px) - 30), max(0, int(py) - 45), min(W, int(px) + 45), min(H, int(py) + 25)))
    return im, c.resize((c.width * 6, c.height * 6), Image.NEAREST)

pairs = sorted({s.rsplit('_', 1)[0] for s in logs if s.endswith('_old')})
for p in pairs:
    if p + '_new' not in logs: continue
    io, co = crop(p + '_old'); inw, cn = crop(p + '_new')
    sheet = Image.new('RGB', (1280 + 20, 720 + 20 + max(co.height, cn.height)), (20, 20, 20))
    sheet.paste(io.resize((640 * 1, 360 * 1)).resize((640, 360), Image.NEAREST).resize((640, 360)), (0, 0))
    sheet.paste(io.resize((640, 360)), (0, 0)); sheet.paste(inw.resize((640, 360)), (660, 0))
    sheet.paste(co, (0, 380)); sheet.paste(cn, (660, 380))
    sheet = sheet.crop((0, 0, 1300, 380 + max(co.height, cn.height)))
    sheet.save(os.path.join(d, 'pair_' + p + '.png'))
print(json.dumps(out, ensure_ascii=False, indent=1))
