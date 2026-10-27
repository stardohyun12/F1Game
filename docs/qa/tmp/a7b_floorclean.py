# 과제 0236-1: 정비소 아틀라스 바닥 UV 구역의 짙은 남색 동그라미를 주변 콘크리트 결로 메운 씬 사본
# 원본 art/export/scene-albedo-kit/garage_tin_F2_atlas.png 는 그대로. 결과 F1/Assets/Art/SceneA7/Tex/garage_tin_F2_atlas_clean.png
import os, math, random
from PIL import Image
R = os.path.join(os.path.dirname(__file__), "../../..")
im = Image.open(os.path.join(R, "art/export/scene-albedo-kit/garage_tin_F2_atlas.png")).convert("RGBA")
W, H = im.size; px = im.load()
# 바닥 UV (u 0.001~0.0791, v 0.0039~0.3789) → 픽셀 (y 는 위가 0)
x0, x1 = 0, min(W, int(math.ceil(0.0791 * W)) + 2)
y0, y1 = max(0, int((1 - 0.3789) * H) - 2), H
before = im.crop((x0, y0, x1, y1))
def blob(p):
    r, g, b, a = p
    return b > r + 12 and (r + g + b) / 3 < 95          # 남색 얼룩
m = set((x, y) for y in range(y0, y1) for x in range(x0, x1) if blob(px[x, y]))
n0 = len(m)
for _ in range(2):                                      # 테두리 고리까지 2px 넓힘
    m |= set((x + dx, y + dy) for (x, y) in m for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if x0 <= x + dx < x1 and y0 <= y + dy < y1)
rnd = random.Random(7)
for (x, y) in m:                                        # 근처 깨끗한 픽셀을 그대로 가져와 결 유지
    for _ in range(200):
        sx = min(x1 - 1, max(x0, x + rnd.randint(-45, 45))); sy = min(y1 - 1, max(y0, y + rnd.randint(-45, 45)))
        sx = min(x1 - 4, max(x0 + 4, sx)); sy = min(y1 - 4, max(y0 + 4, sy))   # 아틀라스 테두리 검은 선 피함
        if (sx, sy) not in m and sum(before.getpixel((sx - x0, sy - y0))[:3]) > 180: px[x, y] = before.getpixel((sx - x0, sy - y0)); break
print("region", x0, x1, y0, y1, "blob px", n0, "filled px", len(m))
im.save(os.path.join(R, "F1/Assets/Art/SceneA7/Tex/garage_tin_F2_atlas_clean.png"))
ba = Image.new("RGBA", ((x1 - x0) * 2, y1 - y0)); ba.paste(before, (0, 0)); ba.paste(im.crop((x0, y0, x1, y1)), (x1 - x0, 0))
ba.resize((ba.width * 2, ba.height * 2), Image.NEAREST).save(os.path.join(R, "docs/qa/tmp/a7b_floorclean_ba.png"))
