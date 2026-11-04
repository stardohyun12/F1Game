from PIL import Image
d = 'docs/qa/evidence/pack-visible/'
ims = [Image.open(d + f'beads-{k}.png').convert('RGB') for k in range(4)]
box = (560, 300, 1140, 780)
cr = [i.crop(box) for i in ims]
cw, ch = cr[0].size
sheet = Image.new('RGB', (cw * 2, ch * 2))
for k, c in enumerate(cr):
    sheet.paste(c, ((k % 2) * cw, (k // 2) * ch))
sheet.save('docs/qa/tmp/beads_crops.png')
print(ims[0].size, sheet.size)
