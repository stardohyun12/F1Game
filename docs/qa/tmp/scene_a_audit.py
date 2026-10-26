# 과제 0429 — 장면 A 4판 측정(audit_compare.measure 그대로) + 시트 2장 + 표
# 실행: Python313 docs/qa/tmp/scene_a_audit.py [태그]
import sys, os, json, importlib.util
from PIL import Image, ImageDraw
ROOT = 'D:/Github/F1Game/'
try:
    import numpy  # noqa: F401
    spec = importlib.util.spec_from_file_location('ac', ROOT + 'docs/research/audit_compare.py')
except ImportError:  # numpy 없는 python → 같은 식의 순수 파이썬판(기준 ①⑥⑫·v5 값 소수 4자리 일치 확인)
    spec = importlib.util.spec_from_file_location('ac', ROOT + 'docs/qa/tmp/audit_lite.py')
ac = importlib.util.module_from_spec(spec); spec.loader.exec_module(ac)
tag = sys.argv[1] if len(sys.argv) > 1 else ''
E = 'docs/qa/evidence/scene-a/'
REF = [('① A Short Hike', 'art/reference/dotlp-2026-10-27/a-short-hike_1.jpg'), ('⑥ Easy Delivery Co.', 'art/reference/dotlp-2026-10-27/easy-delivery-co_1.jpg'), ('⑫ Crow Country', 'art/reference/dotlp-2026-10-27/crow-country_1.jpg')]
NAMES = ['V0 v5 그대로', 'V1 포토배싱+EDC', 'V2 팔레트+계단', 'V3 V1+면 변화']
OURS = [(f'{NAMES[v]} · {c}', f'{E}V{v}_{c}{tag}.png') for v in range(4) for c in ('game', 'intro')]
GATE = [('face_uniform_frac', '<=', .50, '단색 면'), ('face_std_median', '>=', 1.5, '면 안 변화'), ('empty_cell_frac_bottom', '<=', .20, '아래 빈 칸'), ('val_p5_p95', '>=', .55, '밝기 폭')]
def ok(m, k, op, t): return m[k] <= t if op == '<=' else m[k] >= t
res = {}
for n, p in REF + OURS:
    m, _ = ac.measure(n, p); res[n] = m
lines = ['| 컷 | 단색 면 ≤50% | 면 안 변화 ≥1.5 | 아래 빈 칸 ≤20% | 밝기 폭 ≥0.55 | 통과 |', '|---|---|---|---|---|---|']
for n, _ in REF + OURS:
    m = res[n]; g = [ok(m, k, op, t) for k, op, t, _ in GATE]
    lines.append(f"| {n} | {m['face_uniform_frac']:.0%} | {m['face_std_median']:.2f} | {m['empty_cell_frac_bottom']:.0%} | {m['val_p5_p95']:.2f} | {sum(g)}/4 |")
open(ROOT + E + f'table{tag}.md', 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
json.dump({n: {k: res[n][k] for k, *_ in GATE} for n in res}, open(ROOT + E + f'measure{tag}.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print('\n'.join(lines))
# 시트: 위 = 기준 ①⑥⑫, 아래 = V0~V3 게임 컷. 칸 640×360, 위에 이름·게이트
TW, TH, LH, P = 640, 360, 58, 8
def label(d, x, y, n, m, ref=False):
    g = [ok(m, k, op, t) for k, op, t, _ in GATE]; allok = all(g)
    d.rectangle([x, y, x + TW, y + LH - 4], fill=(30, 30, 30) if ref else ((30, 110, 50) if allok else (150, 40, 30)))
    d.text((x + 6, y + 3), n + ('' if ref else ('  — 게이트 통과' if allok else f'  — 게이트 {sum(g)}/4')), fill=(255, 255, 255), font=ac.font(17, True))
    d.text((x + 6, y + 29), '  '.join(f"{lab} {m[k]:.0%}" if k != 'face_std_median' and k != 'val_p5_p95' else f"{lab} {m[k]:.2f}" for k, _, _, lab in GATE), fill=(235, 235, 235), font=ac.font(14))
for kind in ('game', 'intro'):
    W = 4 * TW + 5 * P; Hh = 2 * (TH + LH + P) + P + 34
    sh = Image.new('RGB', (W, Hh), (255, 255, 255)); d = ImageDraw.Draw(sh)
    d.text((P, 6), f'장면 A — 위: 기준 ①⑥⑫ / 아래: V0~V3 {"게임 컷" if kind == "game" else "소개 컷"} (Unity 720 렌더 → 1920×1080 Nearest, 칸은 640×360 BOX 축소)', fill=(20, 20, 20), font=ac.font(18, True))
    for i, (n, p) in enumerate(REF):
        x = P + i * (TW + P); y = 34 + P; label(d, x, y, n, res[n], True)
        sh.paste(Image.open(ROOT + p).convert('RGB').resize((TW, TH), Image.BOX), (x, y + LH))
    for v in range(4):
        n = f'{NAMES[v]} · {kind}'; x = P + v * (TW + P); y = 34 + 2 * P + TH + LH; label(d, x, y, n, res[n])
        sh.paste(Image.open(ROOT + f'{E}V{v}_{kind}{tag}.png').convert('RGB').resize((TW, TH), Image.BOX), (x, y + LH))
    sh.save(ROOT + E + ('sheet' if kind == 'game' else 'sheet_intro') + f'{tag}.png')
# 100% 크롭 시트: 같은 상대 위치(가운데 아래쪽 = 카트·정비소 입구) 640×360 원본 픽셀
cw, ch = 640, 360
W = 4 * cw + 5 * P; Hh = 2 * (ch + 30 + P) + P + 34
cs = Image.new('RGB', (W, Hh), (255, 255, 255)); d = ImageDraw.Draw(cs)
d.text((P, 6), '100% 크롭 (확대·축소 없음) — 위: 기준(원본 해상도 그대로) / 아래: V0~V3 게임 컷 1920×1080 중 가운데 아래', fill=(20, 20, 20), font=ac.font(18, True))
for i, (n, p) in enumerate(REF):
    im = Image.open(ROOT + p).convert('RGB'); X, Y = im.size; x0, y0 = (X - cw) // 2, int(Y * .55) - ch // 2
    x = P + i * (cw + P); d.text((x, 34 + P), f'{n} ({X}×{Y})', fill=(20, 20, 20), font=ac.font(15, True)); cs.paste(im.crop((x0, y0, x0 + cw, y0 + ch)), (x, 34 + P + 26))
for v in range(4):
    im = Image.open(ROOT + f'{E}V{v}_game{tag}.png').convert('RGB'); x0, y0 = (1920 - cw) // 2, int(1080 * .55) - ch // 2
    x = P + v * (cw + P); y = 34 + 2 * P + ch + 26; d.text((x, y), NAMES[v], fill=(20, 20, 20), font=ac.font(15, True)); cs.paste(im.crop((x0, y0, x0 + cw, y0 + ch)), (x, y + 26))
cs.save(ROOT + E + f'sheet_crop100{tag}.png')
