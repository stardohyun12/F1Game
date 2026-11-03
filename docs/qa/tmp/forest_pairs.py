"""artlive 20261031-2009: 무리 전/후 쌍 그림. 사용: python -X utf8 docs/qa/tmp/forest_pairs.py"""
import shutil
from PIL import Image, ImageDraw
E = 'D:/Github/F1Game/docs/qa/evidence/iter-forest/'; V = 'D:/Github/F1Game/art/_review/2026-10-31-forest/'
def pair(a, b, la, lb, out):
    A = Image.open(E + a).convert('RGB'); B = Image.open(E + b).convert('RGB'); w, h = A.size
    im = Image.new('RGB', (w * 2 + 8, h + 24), (20, 20, 20)); im.paste(A, (0, 24)); im.paste(B, (w + 8, 24)); d = ImageDraw.Draw(im); d.text((6, 6), la, fill=(255, 255, 255)); d.text((w + 14, 6), lb, fill=(255, 255, 255))
    im.save(E + out); shutil.copy2(E + out, V + out)
pair('s07_F1_far.png', 's08_F1_far.png', 'BEFORE s07 F1 far (7 tiers, 25 trees 3.8x4.4)', 'AFTER s08 F1 far (8 tiers wider, 36 trees by rule)', 's08_pair.png')
pair('s07_F1_kart.png', 's08_F1_kart.png', 'BEFORE s07 F1 kart', 'AFTER s08 F1 kart', 's08_pair_kart.png')
pair('s08_F1_old_kart.png', 's08_F1_kart.png', 'new shape, old layout (25 trees)', 'new shape + grove rule (36 trees)', 's08_pair_shape_vs_rule_kart.png')
for n in ('F1_far', 'F1_kart', 'F2_far', 'F2_kart', 'MIX_far', 'MIX_kart', 'F1_old_far', 'F1_old_kart', 'F1_single', 'F2_single'): pass
