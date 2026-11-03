# 3판 비교판: 2판|3판 같은 카메라(Cam_Sunset 180·앉은 눈 Play), ① A Short Hike 기준 컷 옆. 모두 360 줄 → 같은 배율(점확대 2배).
from PIL import Image, ImageDraw
E2 = 'docs/qa/evidence/sunset2/'
E3 = 'docs/qa/evidence/sunset3/'
REF = 'art/reference/dotlp-2026-10-27/'


def to360(path):
    im = Image.open(path).convert('RGB')
    if im.height != 360:  # RT(1280x720 = 2x2 블록) → 640x360 실픽셀
        im = im.resize((round(im.width * 360 / im.height), 360), Image.NEAREST)
    return im


def row(ims, labels, out, scale=2):
    w = sum(i.width for i in ims) + 8 * (len(ims) - 1)
    bd = Image.new('RGB', (w, 360 + 14), (20, 20, 20)); x = 0
    d = ImageDraw.Draw(bd)
    for im, lb in zip(ims, labels):
        bd.paste(im, (x, 14)); d.text((x + 4, 1), lb, fill=(255, 255, 255)); x += im.width + 8
    bd = bd.resize((bd.width * scale, bd.height * scale), Image.NEAREST)
    bd.save(E3 + out); print(out, bd.size)


ref = Image.open(REF + 'a-short-hike_2.jpg').convert('RGB')
ref = ref.resize((round(ref.width * 360 / ref.height), 360), Image.LANCZOS)
ref = ref.crop(((ref.width - 640) // 2, 0, (ref.width - 640) // 2 + 640, 360)) if ref.width > 640 else ref
row([to360(E2 + 'cam_v2.png'), to360(E3 + 'cam_v3.png')], ['v2 Cam_Sunset yaw180', 'v3 same camera'], 'cmp_cam_v2_v3.png')
row([to360(E2 + 'seated_v2_rt.png'), to360(E3 + 'seated_v3_rt.png')], ['v2 seated (Play)', 'v3 seated (Play, same rot 3,180)'], 'cmp_seated_v2_v3.png')
row([ref, to360(E2 + 'seated_v2_rt.png'), to360(E3 + 'seated_v3_rt.png')], ['ref A Short Hike #2 (360 rows)', 'v2 seated', 'v3 seated'], 'cmp_ref_v2_v3.png')
# 물 질감 4배 확대(앉은 눈 아래쪽 물 320x120 → 1280x480)
s2, s3 = to360(E2 + 'seated_v2_rt.png'), to360(E3 + 'seated_v3_rt.png')
for tag, im in (('v2', s2), ('v3', s3)):
    im.crop((160, 200, 480, 320)).resize((1280, 480), Image.NEAREST).save(E3 + 'water4x_' + tag + '.png')
s3.resize((1920, 1080), Image.NEAREST).save(E3 + 'seated_v3_3x.png')
to360(E3 + 'cam_v3.png').resize((1920, 1080), Image.NEAREST).save(E3 + 'cam_v3_3x.png')
print('done')
