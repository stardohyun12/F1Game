"""기준 07 과 디오라마 1× 캡처를 같은 높이(720)로 나란히. 축소·확대는 Nearest(우리 그림은 1× 그대로)."""
from PIL import Image, ImageDraw

ref = Image.open("world/taste/interior-candidates/07_easy_delivery_co.jpg").convert("RGB")
ours = Image.open("docs/qa/evidence/room-diorama/1-room.png").convert("RGB")
H = ours.height
ref = ref.resize((round(ref.width * H / ref.height), H), Image.NEAREST)
pad, top = 12, 28
W = ref.width + ours.width + pad * 3
board = Image.new("RGB", (W, H + top + pad), (24, 24, 24))
board.paste(ref, (pad, top))
board.paste(ours, (pad * 2 + ref.width, top))
d = ImageDraw.Draw(board)
d.text((pad, 8), "REF 07 easy_delivery_co (720 high, nearest)", fill=(230, 230, 230))
d.text((pad * 2 + ref.width, 8), "OURS room diorama 1x (pitch 50, dist 4.5 m)", fill=(230, 230, 230))
board.save("docs/qa/evidence/room-diorama/compare.png")
print(board.size)
