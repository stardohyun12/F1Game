from PIL import Image
im = Image.open('F1/Assets/Art/Library/Buildings/depot/icons.png').convert('RGBA')
print(im.size)
w, h = im.size
for box in [(0, 0, int(w * 0.5), int(h * 0.5)), (0, 0, int(w * 0.55), int(h * 0.6))]:
    sub = im.crop(box)
    print(box, sub.split()[3].getbbox())
