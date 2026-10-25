# v4 캡처 → compare.png(v2 ↔ v4 같은 본선 지점 4쌍) + contact.png(과제 캡처 한 장 보기용)
from PIL import Image, ImageDraw
import os
E = 'docs/qa/evidence'
V2, V4 = E + '/bayloop-v2/', E + '/bayloop-v4/'
pairs = [('01_harbor_straight', 'c1_harbor_straight'), ('02_shop_street_climb', 'c2_shop_street_climb'),
         ('05_slope_village', 'c3_slope_village'), ('06_coastal_straight', 'c4_coastal_straight')]
W, H = 640, 360
def tile(path, label):
    im = Image.open(path).convert('RGB').resize((W, H), Image.LANCZOS)
    d = ImageDraw.Draw(im); d.rectangle([0, 0, 8 * len(label) + 10, 18], fill=(0, 0, 0)); d.text((5, 3), label, fill=(255, 255, 255))
    return im
sheet = Image.new('RGB', (W * 2, H * len(pairs)), (20, 20, 20))
for r, (a, b) in enumerate(pairs):
    sheet.paste(tile(V2 + a + '.png', 'v2 ' + a), (0, r * H)); sheet.paste(tile(V4 + b + '.png', 'v4 ' + b), (W, r * H))
sheet.save(V4 + 'compare.png')
names = ['01_plaza_cross', '02_beach', '03_sparse_fields', '04_dense_shops', '05_village_road', '06_coast_walk_eye', '07_shop_front_eye', '08_mountain_entry']
cs = Image.new('RGB', (W * 2, H * 4), (20, 20, 20))
for k, n in enumerate(names):
    cs.paste(tile(V4 + n + '.png', n), ((k % 2) * W, (k // 2) * H))
cs.save(V4 + 'contact.png')
print('ok', sorted(os.listdir(V4)))
