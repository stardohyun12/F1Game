# UI 2판 종이·나무 결: CC0 사진 → 64칸 명암 편차(-1..1 → 0..255) 바이트. 실제 ±% 는 DotScreenUI 가 곱한다(D-197).
# 종이 = ambientCG CardboardSet001 (CC0, art/_review/2026-10-23-ui-final/SOURCES.md 와 같은 crop)
# 나무 = Poly Haven fine_grained_wood (CC0, Rob Tuytel, art/source/q1-room-cc0/sources.json)
import os
from PIL import Image
import numpy as np

R = "F1/Assets/UI/DotUI/Resources/"
os.makedirs(R, exist_ok=True)


def dev(path, box, size):
    im = Image.open(path).convert("L").crop(box).resize(size, Image.BOX)
    a = np.asarray(im, float)
    a = a - a.mean()
    a /= max(1e-3, np.percentile(np.abs(a), 97))
    a = np.clip(a, -1, 1)
    return ((a * 127) + 128).astype(np.uint8), a


p, ap = dev("art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png", (825, 590, 1000, 705), (64, 64))
w, aw = dev("art/source/q1-room-cc0/fine_grained_wood.jpg", (0, 0, 1024, 256), (64, 32))
open(R + "dotui_paper_grain64.bytes", "wb").write(p.tobytes())
open(R + "dotui_wood_grain64x32.bytes", "wb").write(w.tobytes())
Image.fromarray(p).resize((256, 256), Image.NEAREST).save("docs/qa/tmp/uiv2/paper_grain_x4.png")
Image.fromarray(w).resize((256, 128), Image.NEAREST).save("docs/qa/tmp/uiv2/wood_grain_x4.png")
print("paper std", ap.std(), "wood std", aw.std(), len(p.tobytes()), len(w.tobytes()))
