# terrace2: 풀밭 속 1px 틈 확대(가장 가까운 이웃 4배). 위 c3, 아래 c2.
from PIL import Image
sx = 1280 / 960; sy = 720 / 540
out = Image.new("RGB", (1280, 280 * 2))
for r, (n, box) in enumerate([("c3_cart", (380, 250, 700, 320)), ("c2_cart", (330, 240, 650, 310))]):
    b = Image.open(f"docs/qa/evidence/terrace2/after/{n}.png")
    out.paste(b.crop((int(box[0] * sx), int(box[1] * sy), int(box[2] * sx), int(box[3] * sy))).resize((1280, 280), Image.NEAREST), (0, r * 280))
out.save("docs/qa/tmp/terrace2/zoom_after_stitch.png")
