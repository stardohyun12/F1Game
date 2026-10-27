# 과제 1110 — A2d|A2f 나란히(B·소개) + 측정(A2d·A2f·①·⑥)
import subprocess, sys
from PIL import Image, ImageDraw
E = 'docs/qa/evidence/'; R = 'art/reference/dotlp-2026-10-27/'
def tile(p, lab):
    im = Image.open(p).convert('RGB').resize((960, 540), Image.NEAREST); ImageDraw.Draw(im).text((10, 10), lab, fill=(255, 230, 0)); return im
v = Image.new('RGB', (1920, 1080))
v.paste(tile(E + 'scene-a2d/b_game.png', 'A2d B (left)'), (0, 0)); v.paste(tile(E + 'scene-a2f/b_game.png', 'A2f B (right)'), (960, 0))
v.paste(tile(E + 'scene-a2d/intro.png', 'A2d intro'), (0, 540)); v.paste(tile(E + 'scene-a2f/intro.png', 'A2f intro'), (960, 540))
v.save(E + 'scene-a2f/a2d_vs_a2f.png'); print('saved')
subprocess.run([sys.executable, '-X', 'utf8', 'docs/qa/tmp/a2d_measure.py'] + [E + f'scene-{s}/{c}.png' for s in ('a2d', 'a2f') for c in ('b_game', 'intro', 'd')]
               + [R + n for n in ('a-short-hike_1.jpg', 'a-short-hike_2.jpg', 'easy-delivery-co_1.jpg', 'easy-delivery-co_2.jpg')])
