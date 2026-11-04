from PIL import Image, ImageDraw
d = "D:/Github/F1Game/docs/qa/evidence/room-daylight/"
ims = [Image.open(d + n + ".png").convert("RGB") for n in ("day", "dusk", "night")]
w, h = ims[0].size
s = 2  # 1920 -> 960 each, nearest
tw, th = w // s, h // s
board = Image.new("RGB", (tw * 3 + 20, th + 40), (20, 20, 20))
dr = ImageDraw.Draw(board)
for i, (im, lab) in enumerate(zip(ims, ("day dusk=0", "dusk dusk=1", "night sun off, lamp only"))):
    board.paste(im.resize((tw, th), Image.NEAREST), (i * (tw + 10), 40))
    dr.text((i * (tw + 10) + 8, 12), lab, fill=(240, 240, 240))
board.save(d + "compare.png")
print(board.size)
