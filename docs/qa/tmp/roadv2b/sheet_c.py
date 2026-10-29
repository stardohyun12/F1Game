# 1d: fb_shot 7건 → evidence/roadv2/c/ 복사 → sheet_c.png (원본 | 1단계 after | 지금)
import os, shutil, subprocess
from PIL import Image, ImageDraw
B = 'docs/qa/evidence/roadv2/'
ids = ["230810-268e", "230823-7da7", "230848-453e", "230939-4143", "231007-66ad", "231130-94a3", "032740-45c3"]
os.makedirs(B + 'c', exist_ok=True)
for i in ids:
    r = subprocess.run(["bash", "scripts/fb_shot.sh", i], capture_output=True, text=True, encoding="utf-8", errors="replace")
    print(i, (r.stdout + r.stderr).strip().splitlines()[-1:] )
    src = f'tools/viewer/feedback/shots/{i}_after.png'
    if os.path.exists(src): shutil.copy(src, B + f'c/{i}.png')
print("before:", sorted(os.listdir(B + 'before'))[:8])
W = 480
rows = []
for i in ids:
    cols = []
    for d in ['before', 'after', 'c']:
        p = B + d + '/' + i + '.png'
        im = Image.open(p).convert('RGB') if os.path.exists(p) else Image.new('RGB', (W, 270), (80, 0, 0))
        cols.append(im.resize((W, int(im.height * W / im.width))))
    h = max(c.height for c in cols); r = Image.new('RGB', (W * 3, h + 18), (20, 20, 20))
    for k, c in enumerate(cols): r.paste(c, (k * W, 18))
    ImageDraw.Draw(r).text((4, 2), i + "   before | stage1 (after/) | 1d now", fill=(255, 255, 0))
    rows.append(r)
S = Image.new('RGB', (W * 3, sum(r.height for r in rows)))
y = 0
for r in rows: S.paste(r, (0, y)); y += r.height
S.save(B + 'sheet_c.png'); print(S.size)
