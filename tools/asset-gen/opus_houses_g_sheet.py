"""집 g판 시트 (과제 20261027-1516) — f판 시트 스크립트(opus_houses_f_sheet.py)를 f→g 로 바꿔 실행 + 집마다 근접 컷 색 수·명암 단 표.
    python tools/asset-gen/opus_houses_g_sheet.py      (opus_houses_preview.py 를 f·g 둘 다 먼저)
집마다: close_<이름>.png(480×600, 조명 없음)에서 배경(네 귀퉁이 색) 뺀 픽셀 중 ≥0.5% 인 색 수, 밝기 16칸 중 ≥5% 인 칸 수.
"""
import os, json
from collections import Counter
from PIL import Image, ImageDraw, ImageFont
H = os.path.dirname(os.path.abspath(__file__))
s = open(os.path.join(H, 'opus_houses_f_sheet.py'), encoding='utf-8').read()
for a, b in [("E, Fd, G = 'art/_review/2026-10-27-houses-e', 'art/_review/2026-10-27-houses-f',", "E, Fd, G = 'art/_review/2026-10-27-houses-f', 'art/_review/2026-10-27-houses-g',"),
             ("('집 f (미리보기)'", "('집 g (미리보기)'"), ("('집 e (미리보기)'", "('집 f (미리보기)'"),
             ("'e판 — 길가 B (1280×720 → 2배)'", "'f판 — 길가 B (1280×720 → 2배)'"), ("'f판 — 같은 카메라 (STYLE 규칙 1~6)'", "'g판 — 같은 카메라 (명암 4단·얼룩 덩어리·집당 ≤12색)'"),
             ("<title>집 f판</title>", "<title>집 g판</title>")]:
    assert a in s, a; s = s.replace(a, b)
exec(compile(s, 'opus_houses_f_sheet.py', 'exec'))

NAMES = ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']


def house_stats(p):
    im = Image.open(p).convert('RGB'); w, h = im.size; bg = {im.getpixel(c) for c in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1))}
    px = [c for c in im.get_flattened_data() if c not in bg]; tot = len(px); cnt = Counter(px)
    hb = Counter(min(15, int(L(c) // 16)) for c in px)
    return dict(colors=sum(1 for v in cnt.values() if v >= .005 * tot), light_steps=sum(1 for v in hb.values() if v >= .05 * tot))


per = {n: {k: house_stats(f'{d}/close_{n}.png') for k, d in (('f', E), ('g', Fd))} for n in NAMES}
st = json.load(open(f'{Fd}/element_stats.json', encoding='utf-8')); st['per_house_close'] = per
json.dump(st, open(f'{Fd}/element_stats.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
# 근접 f|g 쌍을 시트 아래에 붙임
sh = Image.open(f'{Fd}/sheet.png'); cw, chh = 240, 300
row = Image.new('RGB', (sh.width, chh + 90), (236, 232, 222)); d = ImageDraw.Draw(row); x = 0
for n in NAMES:
    for k, dd in (('f', E), ('g', Fd)):
        row.paste(Image.open(f'{dd}/close_{n}.png').convert('RGB').resize((cw, chh), Image.NEAREST), (x, 40))
        p = per[n][k]; d.text((x + 4, 4), f"{n[6:]} {k}", font=FS, fill=(40, 40, 40)); d.text((x + 4, chh + 46), f"색 {p['colors']} · 명암 {p['light_steps']}단", font=FS, fill=(40, 40, 40)); x += cw + 4
    x += 12
out = Image.new('RGB', (sh.width, sh.height + row.height), (236, 232, 222)); out.paste(sh, (0, 0)); out.paste(row, (0, sh.height)); out.save(f'{Fd}/sheet.png')
for n in NAMES: print(n, per[n])
