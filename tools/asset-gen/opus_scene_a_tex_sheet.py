"""장면 A 텍스처 일람 시트 (아트2, 2026-10-27 과제 0412) → art/_review/2026-10-27-scene-a-tex/sheet.png + index.html
위: V1 포토배싱 텍스처(정수배 Nearest 확대, 이름·크기·색 수·1px 반점%) / 가운데: V2 팔레트(4×) / 아래: V1·V2 Blender 미리보기(참고용).
    python -X utf8 tools/asset-gen/opus_scene_a_tex_sheet.py
"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
B = sys.argv[1:2] == ['b']  # 과제 0430: V1b·V2b → 2026-10-27-scene-a-tex-b
SFX = 'b' if B else ''
REV = os.path.join(ROOT, 'art', '_review', '2026-10-27-scene-a-tex' + ('-b' if B else ''))
TEX = os.path.join(ROOT, 'art', 'source', 'scene-a-v1' + SFX, 'tex')
PAL = os.path.join(ROOT, 'art', 'palette')
PNG = 'autumn_b' if B else 'autumn'


def font(n):
    for f in ('C:/Windows/Fonts/malgun.ttf', 'arial.ttf'):
        try: return ImageFont.truetype(f, n)
        except OSError: pass
    return ImageFont.load_default()


def main():
    rep = json.load(open(os.path.join(TEX, 'noise_report.json'), encoding='utf-8'))
    W, CELL, F, FS = 1680, 280, font(15), font(22)
    cols = W // CELL; rows = (len(rep) + cols - 1) // cols
    pal = Image.open(os.path.join(PAL, PNG + '.png'))
    ncol = len(json.load(open(os.path.join(PAL, 'palette' + ('_b' if B else '') + '.json')))['colors']); pal4 = pal.resize((pal.width * 4, pal.height * 4), Image.NEAREST)
    prev = [Image.open(os.path.join(REV, f'{v}{SFX}_game.png')).convert('RGB').resize((820, 461), Image.LANCZOS) for v in ('v1', 'v2')]
    H = 40 + rows * (CELL + 40) + 40 + 461 + 60
    sh = Image.new('RGB', (W, max(H, 40 + rows * (CELL + 40) + pal4.height + 60)), (34, 34, 36)); d = ImageDraw.Draw(sh)
    d.text((10, 8), f'V1{SFX} 포토배싱 텍스처 (CC0 ambientCG → 평탄화·잔디테일 제거·LANCZOS 축소·8~14색 디더 없음·명암 칠함) — 정수배 Nearest 확대', fill=(240, 236, 210), font=FS)
    for i, (nm, r) in enumerate(rep.items()):
        im = Image.open(os.path.join(TEX, nm + '.png')); s = max(1, (CELL - 20) // max(im.size))
        x, y = (i % cols) * CELL + 10, 44 + (i // cols) * (CELL + 40)
        sh.paste(im.resize((im.width * s, im.height * s), Image.NEAREST), (x, y))
        ok = (120, 220, 120) if r['ok'] else (240, 110, 90)
        d.text((x, y + CELL - 16), f"{nm}  {r['size'][0]}×{r['size'][1]} ×{s}", fill=(230, 230, 230), font=F)
        rust = f" · 녹 {r['rust_pct']}%" if 'rust_pct' in r else ''
        d.text((x, y + CELL + 2), f"{r['colors']}색 · 반점 {r['speck']}%{rust} · {r['src']}", fill=ok, font=F)
    y0 = 44 + rows * (CELL + 40) + 10
    d.text((10, y0), f'V2{SFX} 가을 팔레트 {ncol}색 + 재질별 계단 그라데이션 칸 (4×)', fill=(240, 236, 210), font=FS)
    sh.paste(pal4, (10, y0 + 34))
    px = 10 + pal4.width + 20
    for j, (v, im) in enumerate(zip((f'V1{SFX} 포토배싱', f'V2{SFX} 팔레트'), prev)):
        yy = y0 + 34 + j * (461 + 36) if px + 820 > W - 10 else y0 + 34
        xx = px if px + 820 <= W - 10 else 10
        if j == 1 and px + 820 * 2 + 20 > W: yy = y0 + 34 + 461 + 36
        sh.paste(im, (xx, yy)); d.text((xx, yy - 24 if yy > y0 + 34 else yy + 465), f'{v} — Blender 미리보기(참고용, 판정은 Unity)', fill=(240, 236, 210), font=F)
    sh = sh.crop((0, 0, W, max(y0 + 34 + pal4.height, y0 + 34 + 2 * (461 + 36)) + 20))
    sh.save(os.path.join(REV, 'sheet.png'))
    html = f"""<!doctype html><meta charset="utf-8"><title>장면 A 텍스처 V1/V2</title>
<style>body{{background:#222;color:#eee;font-family:sans-serif;margin:16px}}img{{image-rendering:pixelated;max-width:100%}}
.bar button{{margin-right:6px}} .sil img{{filter:brightness(0)}} .x4 img.p{{width:400%;max-width:none}}</style>
<h3>장면 A 텍스처 시도 — V1{SFX} 포토배싱 / V2{SFX} 팔레트 (도현 승인 필요 · 판정은 Unity 화면)</h3>
<div class="bar"><button onclick="document.body.className=''">1×</button><button onclick="document.body.className='sil'">흑실루엣</button><button onclick="document.body.className='x4'">4×</button></div>
<p>일람</p><img class="p" src="sheet.png">
<p>V1 미리보기</p><img class="p" src="v1{SFX}_game.png"><p>V2 미리보기</p><img class="p" src="v2{SFX}_game.png">
<p>V1 텍스처</p>{''.join(f'<img class="p" src="../../source/scene-a-v1{SFX}/tex/{n}.png" title="{n}" style="margin:4px;height:128px">' for n in rep)}
<p>팔레트</p><img class="p" src="../../palette/{PNG}_x4.png">"""
    open(os.path.join(REV, 'index.html'), 'w', encoding='utf-8').write(html)
    print('sheet', sh.size)


if __name__ == '__main__':
    main()
