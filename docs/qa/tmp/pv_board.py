from PIL import Image, ImageDraw
import shutil
d = 'docs/qa/evidence/pack-visible/'
shutil.copyfile(d + 'beads-2.png', d + 'beads.png')   # 쏟은 뒤 0.17 s — 호가 가장 잘 보이는 한 장
names = ['wrap', 'paper', 'beads']
ims = [Image.open(d + n + '.png').convert('RGB') for n in names]
box = (100, 200, 1500, 880)                            # 판 가운데(1× 원본 픽셀 그대로 잘라 가로로)
cr = [i.crop(box) for i in ims]
cw, ch = cr[0].size
sheet = Image.new('RGB', (cw * 3 + 20, ch + 30), (30, 30, 30))
dr = ImageDraw.Draw(sheet)
for k, c in enumerate(cr):
    sheet.paste(c, (k * (cw + 10), 30))
    dr.text((k * (cw + 10) + 6, 8), ['wrap (wrapped parcel bands)', 'paper (hand holding crumpled paper)', 'beads (pouring arc, 0.17 s)'][k], fill=(255, 255, 255))
sheet.save(d + 'compare.png')
print(sheet.size)
