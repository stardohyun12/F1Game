"""공용 32색 art/palette/style32.gpl (STYLE.md 규칙 6, 과제 20261027-1504). PIL 만.
    python tools/asset-gen/style32_palette.py
게임 컷 docs/qa/evidence/scene-a4e/b_game.png 의 나무·땅·풀·길·하늘·둔덕 크롭 픽셀을 모아 중앙값 분할 48색 → 많이 쓴 색부터 서로 거리 14 넘는 것만 24색.
나머지 12칸 = 집 앵커(벽 5색 × 해/옆 2단 + 청록 그늘 + 간판 빨강): 집 색을 가장 가까운 환경 색 쪽으로 30% 당겨서(환경 안의 가까운 칸).
"""
import os
from PIL import Image
R = 'D:/Github/F1Game/'
SRC = R + 'docs/qa/evidence/scene-a4e/b_game.png'
CROPS = dict(tree_l=(130, 170, 520, 540), tree_r=(1450, 150, 1910, 690), grass_l=(0, 560, 500, 800), grass_r=(1430, 650, 1920, 800),
             road=(720, 640, 1120, 1080), sky=(300, 0, 1500, 70), hill=(0, 350, 220, 560), wall=(0, 640, 430, 1000))
WALLS = dict(cream=(248, 216, 180), apricot=(242, 192, 152), ochre=(238, 204, 146), lteal=(172, 210, 200), white=(244, 236, 216))
TEAL = (150, 190, 192)  # opus_houses.py TEAL — 옆면 단 = 벽 × .88 × .84 + TEAL × .88 × .16 (opus_houses_f.py light() 와 같은 식)
side = lambda c: tuple(round(v * .88 * .84 + t * .88 * .16) for v, t in zip(c, TEAL))
HOUSE = dict(**WALLS, **{k + '_side': side(c) for k, c in WALLS.items()}, shade_teal=(70, 112, 118), sign_red=(176, 70, 56))
NENV = 32 - len(HOUSE)


def build():
    im = Image.open(SRC).convert('RGB'); parts = [im.crop(b) for b in CROPS.values()]
    px = [c for p in parts for c in p.get_flattened_data()]
    flat = Image.new('RGB', (len(px), 1)); flat.putdata(px)
    q = flat.quantize(colors=48, method=Image.Quantize.MEDIANCUT); pal = q.getpalette(); cnt = sorted(q.getcolors(), reverse=True)
    env = []  # 많이 쓴 색부터, 이미 고른 색과 거리 14 안이면 버림(하늘 3칸 겹침 방지)
    for n, i in cnt:
        c = tuple(pal[i * 3:i * 3 + 3])
        if all(sum((a - b) ** 2 for a, b in zip(c, e)) > 14 ** 2 for e in env): env.append(c)
        if len(env) == NENV: break
    near = lambda c: min(env, key=lambda e: sum((a - b) ** 2 for a, b in zip(c, e)))
    anchors = {k: tuple(round(.7 * a + .3 * b) for a, b in zip(c, near(c))) for k, c in HOUSE.items()}
    return env, anchors


if __name__ == '__main__':
    env, anchors = build()
    env.sort(key=lambda c: .299 * c[0] + .587 * c[1] + .114 * c[2])
    os.makedirs(R + 'art/palette', exist_ok=True)
    lines = ['GIMP Palette', 'Name: style32 (b_game 환경 %d + 집 앵커 %d, D-175)' % (len(env), len(anchors)), 'Columns: 8', '#']
    lines += [f'{r} {g} {b} env{i}' for i, (r, g, b) in enumerate(env)] + [f'{r} {g} {b} {k}' for k, (r, g, b) in anchors.items()]
    open(R + 'art/palette/style32.gpl', 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
    sw = Image.new('RGB', (8, 4)); sw.putdata(env + list(anchors.values())); sw.resize((8 * 40, 4 * 40), Image.NEAREST).save(R + 'art/palette/style32.png')
    for k, c in anchors.items(): print(k, HOUSE[k], '->', c)
    print('env', env)
