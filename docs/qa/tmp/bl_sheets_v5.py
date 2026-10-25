# v5 캡처 → compare.png(v4 ↔ v5: 01·04·05·07) + contact.png
from PIL import Image, ImageDraw
E = 'docs/qa/evidence'
V4, V5 = E + '/bayloop-v4/', E + '/bayloop-v5/'
pairs = ['01_plaza_cross', '04_dense_shops', '05_village_road', '07_shop_front_eye']
W, H = 640, 360
def tile(path, label):
    im = Image.open(path).convert('RGB').resize((W, H), Image.LANCZOS)
    d = ImageDraw.Draw(im); d.rectangle([0, 0, 8 * len(label) + 10, 18], fill=(0, 0, 0)); d.text((5, 3), label, fill=(255, 255, 255))
    return im
sheet = Image.new('RGB', (W * 2, H * len(pairs)), (20, 20, 20))
for r, a in enumerate(pairs):
    sheet.paste(tile(V4 + a + '.png', 'v4 ' + a), (0, r * H)); sheet.paste(tile(V5 + a + '.png', 'v5 ' + a), (W, r * H))
sheet.save(V5 + 'compare.png')
names = ['01_plaza_cross', '02_beach', '03_sparse_fields', '04_dense_shops', '05_village_road', '06_coast_walk_eye', '07_shop_front_eye', '08_mountain_entry']
cs = Image.new('RGB', (W * 2, H * 4), (20, 20, 20))
for k, n in enumerate(names):
    cs.paste(tile(V5 + n + '.png', n), ((k % 2) * W, (k // 2) * H))
cs.save(V5 + 'contact.png')
print('ok')
