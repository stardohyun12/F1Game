import sys
import numpy as np
from PIL import Image
for p in sys.argv[1:]:
    a = np.asarray(Image.open(p).convert("RGB")).astype(int)
    h, w, _ = a.shape
    b = a.reshape(h // 3, 3, w // 3, 3, 3)
    d = np.abs(b - b[:, :1, :, :1, :]).max(axis=(1, 3, 4))
    # 오프셋(1,2) 격자도 같이 — 정렬이 3의 배수에 맞는지
    print(p.split("/")[-1], "blocks", d.size, "nonuniform %.1f%%" % (100.0 * (d > 0).mean()), "max diff", int(d.max()), "diff>2: %.2f%%" % (100.0 * (d > 2).mean()))
