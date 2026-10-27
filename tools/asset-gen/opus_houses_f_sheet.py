"""집 f판 시트 + 요소 크롭 표 (STYLE.md 규칙 7, 과제 20261027-1504). PIL 만.
    python tools/asset-gen/opus_houses_f_sheet.py      (opus_houses_preview.py 를 e·f 둘 다 먼저)
위: e판 vs f판 같은 카메라 B 길가 컷(1280×720 → Nearest 2배).
아래: 요소 크롭 — 집 e·f(Blender 미리보기 street_B) · 게임 컷 b_game 의 나무·길·집(e판, Unity). 게임 컷은 1920×1080 이라 1280×720 기준으로 ÷1.5 해서 같은 화면 배율로 놓음.
표: 화면 텍셀 크기(px, 1280×720 기준) · 색 수(4bit 양자화 90% 덮는 수) · 명암 단계 수(밝기 16칸 중 ≥5% 인 칸).
⚠ 나무·길은 게임 컷(Unity, 후처리 있음)에서, 집 f 는 Blender 미리보기(조명 없음 unlit)에서 — 같은 장면 렌더가 아님. f 를 게임 안에 넣은 컷은 QA 몫.
"""
import json
from collections import Counter
from PIL import Image, ImageDraw, ImageFont
E, Fd, G = 'art/_review/2026-10-27-houses-e', 'art/_review/2026-10-27-houses-f', 'docs/qa/evidence/scene-a4e/b_game.png'
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 30); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
L = lambda p: .299 * p[0] + .587 * p[1] + .114 * p[2]


def texel_px(im):
    """4bit 양자화 뒤 가로 같은 색 연속 길이(2~16px)의 중앙값 — 확대된 텍셀 한 칸의 화면 폭. 1px 연속(필터 번짐·그라데이션)은 뺌."""
    im = im.point(lambda v: v >> 4 << 4); w, h = im.size; px = im.load(); runs = Counter()
    for y in range(0, h, 2):
        n = 1
        for x in range(1, w):
            if px[x, y] == px[x - 1, y]: n += 1
            else:
                if 2 <= n <= 16: runs[n] += 1
                n = 1
    arr = sorted(runs.elements()); return arr[len(arr) // 2] if arr else 0  # 최빈값은 전부 2 라 구분 못 함(실측) → 중앙값


def stats(im):
    px = list(im.get_flattened_data()); tot = len(px)
    q = Counter(((r >> 4), (g >> 4), (b >> 4)) for r, g, b in px); acc = n90 = 0
    for _, c in q.most_common():
        acc += c; n90 += 1
        if acc >= .9 * tot: break
    hb = Counter(min(15, int(L(p) // 16)) for p in px)
    return dict(texel_px=texel_px(im), colors90=n90, light_steps=sum(1 for c in hb.values() if c >= .05 * tot))


g = Image.open(G).convert('RGB'); g720 = lambda b: g.crop(b).resize((round((b[2] - b[0]) / 1.5), round((b[3] - b[1]) / 1.5)), Image.NEAREST)
CROPS = [('집 f (미리보기)', Image.open(f'{Fd}/street_B.png').convert('RGB').crop((200, 0, 480, 470))),
         ('집 e (미리보기)', Image.open(f'{E}/street_B.png').convert('RGB').crop((200, 0, 480, 470))),
         ('집 e (게임 컷)', g720((240, 90, 700, 560))),
         ('나무 (게임 컷)', g720((1450, 150, 1910, 690))),
         ('길 (게임 컷)', g720((720, 640, 1120, 1080))),
         ('풀·땅 (게임 컷)', g720((0, 560, 500, 800)))]
rows = [(n, im, stats(im)) for n, im in CROPS]
N2 = lambda im: im.resize((im.width * 2, im.height * 2), Image.NEAREST)
st_e, st_f = N2(Image.open(f'{E}/street_B.png').convert('RGB')), N2(Image.open(f'{Fd}/street_B.png').convert('RGB'))
ch = max(im.height for _, im, _ in rows) * 2; cw = sum(im.width * 2 + 16 for _, im, _ in rows)
W = max(st_e.width, cw); sheet = Image.new('RGB', (W, 20 + (st_e.height + 56) * 2 + ch + 160), (236, 232, 222)); d = ImageDraw.Draw(sheet); y = 10
for lab, im in (('e판 — 길가 B (1280×720 → 2배)', st_e), ('f판 — 같은 카메라 (STYLE 규칙 1~6)', st_f)):
    d.text((10, y), lab, font=FT, fill=(40, 40, 40)); sheet.paste(im, (0, y + 46)); y += im.height + 56
x = 0
for n, im, s in rows:
    d.text((x + 4, y), n, font=FS, fill=(40, 40, 40)); sheet.paste(N2(im), (x, y + 34))
    d.text((x + 4, y + 40 + ch), f"텍셀 {s['texel_px']}px · 색 {s['colors90']} · 명암 {s['light_steps']}단", font=FS, fill=(40, 40, 40)); x += im.width * 2 + 16
sheet.save(f'{Fd}/sheet.png')
json.dump({n: s for n, _, s in rows}, open(f'{Fd}/element_stats.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
for n, _, s in rows: print(n, s)
open(f'{Fd}/index.html', 'w', encoding='utf-8').write('''<!doctype html><meta charset=utf-8><title>집 f판</title>
<style>body{margin:0;background:#ece8de;font:14px sans-serif}img{image-rendering:pixelated;display:block}#b{position:sticky;top:0;background:#fff;padding:6px}</style>
<div id=b>배율 <button onclick=z(.5)>1×</button><button onclick=z(1)>2×</button><button onclick=z(2)>4×</button>
<button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
<button onclick="document.body.style.background=document.body.style.background=='rgb(32, 32, 32)'?'#ece8de':'#202020'">배경</button></div>
<img id=i src=sheet.png><script>const i=document.getElementById('i');function z(k){i.style.width=(i.naturalWidth*k/2)+'px'}</script>''')
print('sheet', sheet.size)
