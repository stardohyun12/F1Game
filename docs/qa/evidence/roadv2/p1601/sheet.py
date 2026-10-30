"""원본 코멘트 스샷(왼쪽) + 지금 _after(오른쪽) 세 줄 시트. usage: sheet.py out.png"""
import sys, glob
from PIL import Image, ImageDraw
ids = ['230810-268e', '230939-4143', '123628-4100']
rows = []
for i in ids:
    b = Image.open(glob.glob(f'tools/viewer/feedback/shots/*{i}.png')[0]).convert('RGB')
    a = Image.open(glob.glob(f'tools/viewer/feedback/shots/*{i}_after.png')[0]).convert('RGB')
    w = 560; b = b.resize((w, int(b.height * w / b.width))); a = a.resize((w, int(a.height * w / a.width)))
    h = max(b.height, a.height); r = Image.new('RGB', (w * 2 + 4, h), (255, 255, 255)); r.paste(b, (0, 0)); r.paste(a, (w + 4, 0))
    ImageDraw.Draw(r).rectangle([0, 0, 60, 16], fill=(0, 0, 0)); ImageDraw.Draw(r).text((4, 2), i[-4:], fill=(255, 255, 255))
    rows.append(r)
out = Image.new('RGB', (rows[0].width, sum(r.height + 4 for r in rows)), (255, 255, 255)); y = 0
for r in rows: out.paste(r, (0, y)); y += r.height + 4
out.save(sys.argv[1])
