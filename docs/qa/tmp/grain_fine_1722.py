# artlive 1722: 잔결 지표 비교 — 픽셀과 3x3 평균의 차이 / 밝기, 0.25m 칸 평균의 p25·중앙값·p75 (빈 배경 칸 제외)
import numpy as np
from PIL import Image, ImageFilter
def fine(p, B):
    im = Image.open(p).convert('RGB'); a = np.asarray(im, np.float64); l = a @ [.299, .587, .114]
    b = np.asarray(Image.fromarray(l.astype(np.uint8)).filter(ImageFilter.BoxBlur(1)), np.float64)
    d = np.abs(l - b) / np.maximum(l, 8)
    q = (a // 4).astype(np.int64); k = q[..., 0] * 4096 + q[..., 1] * 64 + q[..., 2]; v, c = np.unique(k, return_counts=True); bg = v[c.argmax()]
    h, w = l.shape[0] // B, l.shape[1] // B
    D = d[:h * B, :w * B].reshape(h, B, w, B).transpose(0, 2, 1, 3).reshape(h, w, -1); K = k[:h * B, :w * B].reshape(h, B, w, B).transpose(0, 2, 1, 3).reshape(h, w, -1)
    op = ~(K == bg).all(-1); mm = D.mean(-1)[op]
    return np.percentile(mm, 25), np.median(mm), np.percentile(mm, 75)
print('REF', ['%.4f' % x for x in fine('F1/Assets/Art/SceneA7/houses/house_shop.png', 8)])
for p in ['q1-room/room.png', 'q2-garage/garage_baked32.png', 'q2-garage/garage_props.png', 'q2-garage/garage_details.png', 'q3-depot/wall.png', 'q3-depot/floor.png', 'q3-depot/trim.png', 'q4-postoffice/postbox_G1.png', 'q2-garage/dirt_baked32.png']:
    print(p, ['%.4f' % x for x in fine('art/export/' + p, 16)])
