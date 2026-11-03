# 2판 비교판: 1판|2판 (같은 카메라), 앉은 시점 1판|2판, ① A Short Hike 기준 컷 옆. 모두 360 줄 → 같은 배율(점확대)로.
from PIL import Image, ImageDraw
E1 = 'docs/qa/evidence/sunset/'
E2 = 'docs/qa/evidence/sunset2/'
REF = 'art/reference/dotlp-2026-10-27/'


def to360(path):
    im = Image.open(path).convert('RGB')
    if im.height != 360:  # RT(1280x720 = 2x2 블록) → 640x360 실픽셀
        im = im.resize((round(im.width * 360 / im.height), 360), Image.NEAREST)
    return im


def pair(a, b, la, lb, out, scale=2):
    w = a.width + b.width + 8
    bd = Image.new('RGB', (w, 360 + 14), (20, 20, 20))
    bd.paste(a, (0, 14)); bd.paste(b, (a.width + 8, 14))
    d = ImageDraw.Draw(bd); d.text((4, 1), la, fill=(255, 255, 255)); d.text((a.width + 12, 1), lb, fill=(255, 255, 255))
    bd = bd.resize((bd.width * scale, bd.height * scale), Image.NEAREST)
    bd.save(E2 + out); print(out, bd.size)


pair(to360(E1 + 'cam_sunset_on.png'), to360(E2 + 'cam_v1pose_v2.png'), 'v1 Cam_Sunset yaw151', 'v2 same pose', 'cmp_cam_v1_v2.png')
pair(to360(E1 + 'seated_fmid.png'), to360(E2 + 'seated_v2_rt.png'), 'v1 seated (Play)', 'v2 seated (Play, yaw180)', 'cmp_seated_v1_v2.png')
# 기준: A Short Hike 스크린샷을 360 줄로 줄여(원 게임도 저해상 렌더) 같은 배율로
ref = Image.open(REF + 'a-short-hike_2.jpg').convert('RGB')
ref = ref.resize((round(ref.width * 360 / ref.height), 360), Image.LANCZOS)
ref = ref.crop(((ref.width - 640) // 2, 0, (ref.width - 640) // 2 + 640, 360)) if ref.width > 640 else ref
pair(ref, to360(E2 + 'seated_v2_rt.png'), 'ref A Short Hike #2 water (360 rows)', 'v2 seated', 'cmp_ref_ashorthike.png')
for t in ('cam_v2', 'edge_left172', 'edge_right215'):
    im = to360(E2 + t + '.png'); im.resize((1920, 1080), Image.NEAREST).save(E2 + t + '_3x.png')
to360(E2 + 'seated_v2_rt.png').resize((1920, 1080), Image.NEAREST).save(E2 + 'seated_v2_3x.png')
print('3x done')
