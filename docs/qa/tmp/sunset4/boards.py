# 4판 비교판: 1판 | 3판 | 4판 | A Short Hike ① — 모두 360 줄, 점확대 2배. 같은 카메라끼리 한 줄.
from PIL import Image, ImageDraw
E1, E3, E4 = 'docs/qa/evidence/sunset/', 'docs/qa/evidence/sunset3/', 'docs/qa/evidence/sunset4/'
REF = 'art/reference/dotlp-2026-10-27/a-short-hike_2.jpg'


def to360(path):
    im = Image.open(path).convert('RGB')
    if im.height != 360:  # RT 1280x720(2x2 블록) → 640x360 실픽셀
        im = im.resize((round(im.width * 360 / im.height), 360), Image.NEAREST)
    return im


def row(ims, labels, out, scale=2):
    w = sum(i.width for i in ims) + 8 * (len(ims) - 1)
    bd = Image.new('RGB', (w, 360 + 14), (20, 20, 20)); x = 0
    d = ImageDraw.Draw(bd)
    for im, lb in zip(ims, labels):
        bd.paste(im, (x, 14)); d.text((x + 4, 1), lb, fill=(255, 255, 255)); x += im.width + 8
    bd = bd.resize((bd.width * scale, bd.height * scale), Image.NEAREST)
    bd.save(E4 + out); print(out, bd.size)


ref = Image.open(REF).convert('RGB')
ref = ref.resize((round(ref.width * 360 / ref.height), 360), Image.LANCZOS)
ref = ref.crop(((ref.width - 640) // 2, 0, (ref.width - 640) // 2 + 640, 360))
row([to360(E4 + 'cam180_v1.png'), to360(E4 + 'cam180_v3.png'), to360(E4 + 'cam180_v4.png'), ref],
    ['v1 Cam_Sunset 180', 'v3 same camera', 'v4 same camera', 'ref A Short Hike #2 (360 rows)'], 'cmp_cam180_v1_v3_v4_ref.png')
row([to360(E4 + 'cam151_v1.png'), to360(E4 + 'cam151_v3.png'), to360(E4 + 'cam151_v4.png'), ref],
    ['v1 pose yaw151', 'v3 same camera', 'v4 same camera', 'ref A Short Hike #2'], 'cmp_cam151_v1_v3_v4_ref.png')
row([to360(E1 + 'seated_fmid.png'), to360(E3 + 'seated_v3_rt.png'), to360(E4 + 'seated_v4a_rt.png'), ref],
    ['v1 seated Play (v1 pose)', 'v3 seated Play rot 3,180', 'v4 seated Play rot 3,180', 'ref A Short Hike #2'], 'cmp_seated_v1_v3_v4_ref.png')
# 물 4배(앉은 눈 아래쪽 320x120)
s3, s4 = to360(E3 + 'seated_v3_rt.png'), to360(E4 + 'seated_v4a_rt.png')
for tag, im in (('v3', s3), ('v4', s4)):
    im.crop((160, 200, 480, 320)).resize((1280, 480), Image.NEAREST).save(E4 + 'water4x_' + tag + '.png')
to360(E4 + 'cam180_v4.png').crop((0, 170, 320, 260)).resize((1280, 360), Image.NEAREST).save(E4 + 'shore4x_v4.png')
s4.resize((1920, 1080), Image.NEAREST).save(E4 + 'seated_v4_3x.png')
print('done')
