import json, os, shutil, sys
from PIL import Image, ImageDraw, ImageFont, ImageOps
ROOT = os.path.dirname(os.path.abspath(__file__))
F = 'C:/Windows/Fonts/malgun.ttf'
BOARDS = json.load(open(os.path.join(ROOT, '_picks.json'), encoding='utf-8'))
meta = json.load(open(os.path.join(ROOT, '_cand', 'meta.json'), encoding='utf-8'))

def board(name, title, cols, tiles, W=1600):
    gap, pad, head, cap = 8, 16, 64, 46
    tw = (W - 2 * pad - (cols - 1) * gap) // cols
    th = int(tw * 2 / 3) if cols == 3 else int(tw * 0.6)
    rows = (len(tiles) + cols - 1) // cols
    H = head + rows * (th + cap + gap) + pad
    img = Image.new('RGB', (W, H), (24, 24, 26))
    d = ImageDraw.Draw(img)
    d.text((pad, 12), title, font=ImageFont.truetype(F, 30), fill=(235, 230, 220))
    fn = ImageFont.truetype(F, 34); fc = ImageFont.truetype(F, 22)
    tdir = os.path.join(ROOT, name); os.makedirs(tdir, exist_ok=True)
    for i, t in enumerate(tiles):
        src = os.path.join(ROOT, '_cand', t['key'] + '.jpg')
        shutil.copy(src, os.path.join(tdir, f"{i+1:02d}.jpg"))
        im = Image.open(src).convert('RGB')
        im = ImageOps.fit(im, (tw, th), Image.LANCZOS, centering=tuple(t.get('c', (0.5, 0.5))))
        x = pad + (i % cols) * (tw + gap); y = head + (i // cols) * (th + cap + gap)
        img.paste(im, (x, y))
        d.rectangle([x, y, x + 52, y + 50], fill=(20, 20, 20))
        d.text((x + 12, y + 2), str(i + 1), font=fn, fill=(255, 210, 90))
        d.text((x + 2, y + th + 8), f"{i+1}. {t['cap']}", font=fc, fill=(230, 226, 216))
    out = os.path.join(ROOT, name + '.png')
    img.save(out, optimize=True); print(out, img.size)

for b in BOARDS:
    board(b['name'], b['title'], b['cols'], b['tiles'])

# SOURCES.md
L = ['# 분위기 보드 — 코리코식 기억 속 항구 (2026-10-25, D-137)', '',
     '실제 사진만(Wikimedia Commons, 1280px 썸네일). 구도·밀도·빛 기준용 — **게임 질감·빌드·스토어에 넣지 않는다**. 라이선스는 각 원본 페이지 기준.', '']
for b in BOARDS:
    L += [f"## {b['name']}.png — {b['title']}", '', '| # | 무엇을 가져오나 | 장소(검색어) | 원본 | 작가 | 라이선스 |', '|---|---|---|---|---|---|']
    for i, t in enumerate(b['tiles']):
        m = meta[t['key']]
        L.append(f"| {i+1} | {t['cap']} | {m['query']} | {m['page']} | {m['artist'].replace('|','/')} | {m['license']} |")
    L.append('')
L += ['## 두 층을 섞는 규칙 5줄', '']
L += [r for r in json.load(open(os.path.join(ROOT, '_rules.json'), encoding='utf-8'))]
L += ['', '타일 원본 사본: `board_form/`, `board_life/`, `board_light/` (번호 = 보드 번호). 후보 전체·메타: `_cand/`(로컬, 정리 대상).']
open(os.path.join(ROOT, 'SOURCES.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')
print('SOURCES.md ok')
