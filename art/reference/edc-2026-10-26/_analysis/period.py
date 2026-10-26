import sys
import numpy as np
from PIL import Image
for f in sys.argv[1:]:
    a = np.asarray(Image.open(f).convert('L')).astype(np.float32)
    res = []
    for axis in (1, 0):
        g = np.abs(np.diff(a, axis=axis)).sum(axis=1-axis)
        g = g - g.mean()
        n = len(g); x = np.arange(n)
        best = []
        for p in np.arange(1.8, 14.0, 0.01):
            c = np.abs((g * np.exp(-2j*np.pi*x/p)).sum()) / (np.abs(g).sum()+1e-6)
            best.append((c, p))
        best.sort(reverse=True)
        top = best[0]
        res.append((round(float(top[1]),2), round(float(top[0]),3), round(n/top[1],1)))
    print(f.split('/')[-1], 'x:', res[0], 'y:', res[1])
