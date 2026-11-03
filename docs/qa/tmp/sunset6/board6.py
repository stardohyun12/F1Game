# 6판 최종 비교 보드: 최종 A | 최종 B | 5판 B | A Short Hike(예시) | 노을 바다 사진(링크만, 저장소에 복사 안 함)
# 전부 높이 360 실픽셀로 맞추고 2배 최근접 확대. + 바다 4배 크롭(최종 B / 5판 B)
from PIL import Image, ImageDraw
P = 'docs/qa/evidence/sunset6/'
def to360(path):
    im = Image.open(path).convert('RGB')
    if im.height != 360:
        im = im.resize((round(im.width * 360 / im.height), 360), Image.NEAREST)
    return im
def fit640(im):  # 가로 640 로 가운데 자름(예시 그림 비율 차이)
    if im.width > 640:
        x = (im.width - 640) // 2; im = im.crop((x, 0, x + 640, 360))
    elif im.width < 640:
        c = Image.new('RGB', (640, 360), (20, 20, 20)); c.paste(im, ((640 - im.width) // 2, 0)); im = c
    return im
panels = [
    ('final A (1+2)', fit640(to360(P + 'play/finA_a_f000.png'))),
    ('final B (1+2)', fit640(to360(P + 'play/finB_a_f000.png'))),
    ('v5 B', fit640(to360(P + 'play/v5B_a_f000.png'))),
    ('A Short Hike (ref)', fit640(to360('art/reference/dotlp-2026-10-27/a-short-hike_2.jpg'))),
]
ph = Image.new('RGB', (640, 360), (40, 40, 48)); d = ImageDraw.Draw(ph)
d.text((20, 150), 'sunset sea photo (CC0, link only)', fill=(230, 230, 230))
d.text((20, 175), 'https://wordpress.org/photos/photo/969e3b7d90/', fill=(200, 200, 120))
d.text((20, 200), 'research: do not copy into repo', fill=(160, 160, 160))
panels.append(('photo (link)', ph))
S = 2; W = 640 * S; H = 360 * S; L = 28
out = Image.new('RGB', (W, (H + L) * len(panels)), (0, 0, 0)); d = ImageDraw.Draw(out)
for i, (name, im) in enumerate(panels):
    y = i * (H + L); d.text((8, y + 8), name, fill=(255, 255, 255))
    out.paste(im.resize((W, H), Image.NEAREST), (0, y + L))
out.save(P + 'board_final.png')
# 4배 바다 크롭: 아래 가운데 160x90 실픽셀
crops = []
for name, f in [('final B', 'play/finB_a_f000.png'), ('v5 B', 'play/v5B_a_f000.png'), ('final A', 'play/finA_a_f000.png')]:
    im = to360(P + f); c = im.crop((240, 230, 400, 320)).resize((640, 360), Image.NEAREST); crops.append((name, c))
o2 = Image.new('RGB', (640 * len(crops), 360 + L)); d = ImageDraw.Draw(o2)
for i, (n, c) in enumerate(crops):
    d.text((i * 640 + 8, 8), n + ' x4 sea', fill=(255, 255, 255)); o2.paste(c, (i * 640, L))
o2.save(P + 'board_sea4x.png')
print(out.size, o2.size)
