"""두 캡처 픽셀 차: 다른 픽셀 %, 16 넘게 다른 픽셀 %, 최대 차. 차이 지도(x4 강조) 저장."""
import sys, pathlib
from PIL import Image, ImageChops
raw = pathlib.Path("D:/Github/F1Game/docs/qa/evidence/look-global/raw")
a = Image.open(raw / f"{sys.argv[1]}.png").convert("RGB"); b = Image.open(raw / f"{sys.argv[2]}.png").convert("RGB")
d = ImageChops.difference(a, b); px = list(d.getdata()); n = len(px)
mx = [max(p) for p in px]
print(f"{sys.argv[1]} vs {sys.argv[2]}: diff>0 {100*sum(m>0 for m in mx)/n:.2f}%  diff>16 {100*sum(m>16 for m in mx)/n:.2f}%  max {max(mx)}")
if len(sys.argv) > 3:
    d.point(lambda v: min(255, v * 4)).save(raw.parent / sys.argv[3])
