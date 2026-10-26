"""town-v6b.json controlPoints → 높이 회색 + 음영 그림(위=+z). 사용: python -X utf8 docs/qa/tmp/hm.py [출력]"""
import json, sys, math
from PIL import Image
J = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
cps = J['terrain']['controlPoints']; st = J['terrain']['step']
n = int(round(math.sqrt(len(cps)))); x0 = cps[0][0]
H = [[0.0] * n for _ in range(n)]
for x, z, y in cps: H[int(round((x - x0) / st))][int(round((z - x0) / st))] = y
lo, hi = min(min(r) for r in H), max(max(r) for r in H)
im = Image.new('RGB', (n * 2, n))
for i in range(n):
    for j in range(n):
        v = int(255 * (H[i][j] - lo) / (hi - lo + 1e-9))
        dzx = (H[min(n - 1, i + 1)][j] - H[max(0, i - 1)][j]) / (2 * st); dzz = (H[i][min(n - 1, j + 1)] - H[i][max(0, j - 1)]) / (2 * st)
        s = max(0, min(255, int(128 - 300 * (dzx + dzz))))
        im.putpixel((i, n - 1 - j), (v, v, v)); im.putpixel((n + i, n - 1 - j), (s, s, s))
im = im.resize((n * 6, n * 3), Image.NEAREST); im.save(sys.argv[1] if len(sys.argv) > 1 else 'docs/qa/tmp/v6d-hm.png')
print(n, st, x0, lo, hi)
