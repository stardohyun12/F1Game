# 5판 비교판: 1판 | 4판 | A(실제 물) | B(스타일) | A Short Hike — 모두 360 줄, 점확대 2배. 같은 카메라끼리 한 줄.
from PIL import Image, ImageDraw
E1, E4, E5 = 'docs/qa/evidence/sunset/', 'docs/qa/evidence/sunset4/', 'docs/qa/evidence/sunset5/'
REF = 'art/reference/dotlp-2026-10-27/a-short-hike_2.jpg'


def to360(path):
    im = Image.open(path).convert('RGB')
    if im.height != 360:  # RT 1280x720(2x2 블록) → 640x360 실픽셀
        im = im.resize((round(im.width * 360 / im.height), 360), Image.NEAREST)
    return im


def grid(cells, out, cols=2, scale=2):
    # cells = [(img, label)] → cols 칸씩 줄바꿈
    rows = [cells[i:i + cols] for i in range(0, len(cells), cols)]
    W = cols * 640 + 8 * (cols - 1); H = len(rows) * (360 + 14) + 8 * (len(rows) - 1)
    bd = Image.new('RGB', (W, H), (20, 20, 20)); d = ImageDraw.Draw(bd)
    for r, row in enumerate(rows):
        for c, (im, lb) in enumerate(row):
            x, y = c * 648, r * 382
            bd.paste(im, (x, y + 14)); d.text((x + 4, y + 1), lb, fill=(255, 255, 255))
    bd = bd.resize((bd.width * scale, bd.height * scale), Image.NEAREST)
    bd.save(E5 + out); print(out, bd.size)


ref = Image.open(REF).convert('RGB')
ref = ref.resize((round(ref.width * 360 / ref.height), 360), Image.LANCZOS)
ref = ref.crop(((ref.width - 640) // 2, 0, (ref.width - 640) // 2 + 640, 360))
grid([(to360(E5 + 'seated_A1a_rt.png'), 'A real water (Play seated 3,180)'), (to360(E5 + 'seated_B2a_rt.png'), 'B style on A (same)'),
      (to360(E4 + 'seated_v4a_rt.png'), 'v4 (same pose)'), (ref, 'ref A Short Hike #2 (360 rows)')], 'cmp_seated_A_B_v4_ref.png')
grid([(to360(E5 + 'cam180_A.png'), 'A  Cam_Sunset 3,180'), (to360(E5 + 'cam180_B.png'), 'B  same camera'),
      (to360(E4 + 'cam180_v4.png'), 'v4 same camera'), (to360(E4 + 'cam180_v1.png'), 'v1 same camera')], 'cmp_cam180_A_B_v4_v1.png')
grid([(to360(E5 + 'cam151_A.png'), 'A  yaw151 (pier edge)'), (to360(E5 + 'cam151_B.png'), 'B  same camera'),
      (to360(E4 + 'cam151_v4.png'), 'v4 same camera'), (to360(E4 + 'cam151_v1.png'), 'v1 same camera')], 'cmp_cam151_A_B_v4_v1.png')
grid([(to360(E5 + 'cam180_day.png'), 'sunset OFF (day, saved state) - grid hidden'), (to360(E5 + 'cam180_nogrid.png'), 'B without grid (flat plane, same shading)')], 'day_and_nogrid.png')
# 물 4배(앉은 눈 아래쪽 320x120)
for tag, f in (('A', 'seated_A1a_rt.png'), ('B', 'seated_B2a_rt.png'), ('v4', None)):
    im = to360(E5 + f) if f else to360(E4 + 'seated_v4a_rt.png')
    im.crop((160, 200, 480, 320)).resize((1280, 480), Image.NEAREST).save(E5 + 'water4x_' + tag + '.png')
print('done')
