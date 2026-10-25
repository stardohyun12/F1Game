"""과제 0208: A·C 각각 [Blender 렌더 | 게임 전 | 게임 후] 3칸 시트 + 짐·운전자 판 한 줄."""
from PIL import Image, ImageDraw, ImageFont

E = "docs/qa/evidence/car-look-v1/"
R = "art/_review/2026-10-25-car-candidates/"
W, H, S = 640, 360, 28
try:
    font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
except OSError:
    font = ImageFont.load_default()
rows = [
    ("A", [(R + "A_threequarter.png", "A Blender 렌더"), (E + "A_before_threequarter.png", "A 게임 전"), (E + "A_after_threequarter.png", "A 게임 후")]),
    ("C", [(R + "C_threequarter.png", "C Blender 렌더"), (E + "C_before_threequarter.png", "C 게임 전"), (E + "C_after_threequarter.png", "C 게임 후")]),
    ("X", [(E + "A_after_driver_cargo_threequarter.png", "A + 운전자·가방 (판정용)"), (E + "C_after_driver_cargo_threequarter.png", "C + 운전자·가방 (가방이 짐상자와 겹침)"), (None, "")]),
]
sheet = Image.new("RGB", (W * 3, (H + S) * len(rows)), (30, 30, 30))
d = ImageDraw.Draw(sheet)
for ri, (_, cells) in enumerate(rows):
    for ci, (path, label) in enumerate(cells):
        x, y = ci * W, ri * (H + S)
        d.text((x + 8, y + 4), label, fill=(235, 235, 235), font=font)
        if path:
            sheet.paste(Image.open(path).convert("RGB").resize((W, H), Image.NEAREST), (x, y + S))
sheet.save(E + "sheet.png")
print("sheet", sheet.size)
