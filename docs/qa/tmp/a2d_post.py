# 과제 1046 — A2c|A2d 나란히(B·소개) + 측정
import subprocess, sys
from PIL import Image, ImageDraw
E = 'docs/qa/evidence/'
def tile(p, lab):
    im = Image.open(p).convert('RGB').resize((960, 540), Image.NEAREST); ImageDraw.Draw(im).text((10, 10), lab, fill=(255, 230, 0)); return im
v = Image.new('RGB', (1920, 1080))
v.paste(tile(E + 'scene-a2c/b_game.png', 'A2c B (left)'), (0, 0)); v.paste(tile(E + 'scene-a2d/b_game.png', 'A2d B (right)'), (960, 0))
v.paste(tile(E + 'scene-a2c/intro.png', 'A2c intro'), (0, 540)); v.paste(tile(E + 'scene-a2d/intro.png', 'A2d intro'), (960, 540))
v.save(E + 'scene-a2d/a2c_vs_a2d.png'); print('saved')
subprocess.run([sys.executable, '-X', 'utf8', 'docs/qa/tmp/a2d_measure.py'] + [E + f'scene-{s}/{c}.png' for s in ('a2c', 'a2d') for c in ('b_game', 'intro', 'd')])
