"""Measure terrain-brown reference colours in the existing baseline (no writes besides stdout)."""
from PIL import Image
import numpy as np, sys
root = r'D:/Github/F1Game/art/_review/2026-10-29-field_patch/'
im = np.asarray(Image.open(root + 'map_baseline.png').convert('RGB')).astype(float)
h, w, _ = im.shape
r, g, b = im[..., 0], im[..., 1], im[..., 2]
# brownish/tan terrain: R >= G*0.97, not orange roof (R-B small enough), mid brightness
mask = (r >= g * 0.98) & (r - b < 70) & (r - b > 12) & (r < 200) & (r > 90)
# restrict to ground area right of barn (earth patch) -> look at where mask is dense
ys, xs = np.nonzero(mask)
print('mask px', mask.sum(), 'of', h * w)
# grid density
for gy in range(0, h, 61):
    print(''.join('#' if mask[gy:gy + 61, gx:gx + 64].mean() > .5 else ('+' if mask[gy:gy + 61, gx:gx + 64].mean() > .2 else '.') for gx in range(0, w, 64)))
if len(sys.argv) > 4:
    x0, y0, x1, y1 = map(int, sys.argv[1:5])
    c = im[y0:y1, x0:x1].reshape(-1, 3)
    print('region median', np.median(c, 0), 'mean', c.mean(0))
