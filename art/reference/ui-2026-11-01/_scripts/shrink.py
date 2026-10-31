"""1:1 잘라낸 조각(관찰용)을 저장소 규칙(저해상도만)에 맞게 가로 760 으로 줄여 덮어쓴다. 사용: python -I shrink.py <file> ..."""
import sys
from PIL import Image
for p in sys.argv[1:]:
    im = Image.open(p).convert("RGB")
    w, h = im.size
    im.resize((760, round(760 * h / w)), Image.LANCZOS).save(p, quality=70)
    print(p, im.size, "->", (760, round(760 * h / w)))
