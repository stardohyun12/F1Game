"""정비소·고카트 v5 후처리 — v4 시트(opus_garage_sheet_v4.py)의 하늘·크롭을 그대로 쓰고 경로·컷만 바꾼다.
판정 단위 = 장면: 위 2줄 = 기준 ①⑥⑫(1×·4×), 아래 2줄 = 우리 게임 컷·소개 컷·카트 3/4(1×·4×).

    python -X utf8 tools/asset-gen/opus_garage_sheet_v5.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import opus_garage_sheet_v4 as v4

v4.OUT = os.path.join(v4.ROOT, 'art', '_review', '2026-10-27-garage-kart-opus-v5')
v4.LABEL = {'x1_game': '게임 컷 — 카트 뒤 5.6m·높이 2.8m·세로 FOV 64', 'x2_intro': '소개 컷 — 조금 물러남(FOV 50)',
            'k1_34': '카트 3/4 (FOV 40)', 'k2_side': '카트 옆 (FOV 40)'}
v4.OURS = [('게임 컷', 'x1_game', (960, 560)), ('소개 컷', 'x2_intro', (1000, 560)), ('카트 3/4', 'k1_34', (960, 540))]
_text = v4.ImageDraw.ImageDraw.text


def text(self, xy, s, *a, **kw):  # v4 시트 문구를 v5 로

    s = s.replace('v4', 'v5').replace('정비소·고카트 v5 —', '정비소 앞 장면 v5 —')
    return _text(self, xy, s, *a, **kw)


v4.ImageDraw.ImageDraw.text = text
# 판정 1차: 하늘이 한낮 파랑 → 옅은 안개 쪽(늦가을 오후). 지평선 = 재질 안개색 #bfd8d8 근처, 조금 따뜻하게
v4.TOP, v4.HOR, v4.CS = (158, 196, 208), (208, 218, 208), (214, 220, 212)
SHADE = (0.70, 0.81, 0.90)  # 그림자 = 청록 쪽 곱(sRGB). v4 재질의 그늘색 비율과 같은 방향


def shaded(n):
    """그림자 패스: sh(그림자 on) ÷ ns(off). 해를 받는 면 중 가려진 곳만 SHADE 를 곱한다(해 반대 면은 재질이 이미 그늘)."""
    raw = os.path.join(v4.OUT, '_raw', n + '.png'); dst = os.path.join(v4.OUT, '_raw', n + '_shaded.png')
    im = _open(raw).convert('RGBA'); sh = _open(raw[:-4] + '_sh.png').convert('L'); ns = _open(raw[:-4] + '_ns.png').convert('L')
    px, a, b = im.load(), sh.load(), ns.load(); cnt = 0
    for y in range(im.height):
        for x in range(im.width):
            if b[x, y] > 20 and a[x, y] < .55 * b[x, y]:
                r, g, bb, al = px[x, y]; px[x, y] = (round(r * SHADE[0]), round(g * SHADE[1]), round(bb * SHADE[2]), al); cnt += 1
    im.save(dst); return dst, cnt / (im.width * im.height)


_open = v4.Image.open
MAP, COV = {}, {}
for n in ('x1_game', 'x2_intro', 'k1_34', 'k2_side'):
    MAP[os.path.normcase(os.path.join(v4.OUT, '_raw', n + '.png'))], COV[n] = shaded(n)
v4.Image.open = lambda fp, *a, **kw: _open(MAP.get(os.path.normcase(str(fp)), fp), *a, **kw)
print('shadow px share', {k: round(v, 3) for k, v in COV.items()})
v4.main()
html = os.path.join(v4.OUT, 'index.html')
s = open(html, encoding='utf8').read().replace('v4', 'v5').replace(' · 텍셀 실측 texel_probe.json', '')
open(html, 'w', encoding='utf8').write(s)
