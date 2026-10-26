import os, shutil, glob, time
from PIL import Image
time.sleep(35)
D = 'docs/qa/evidence/tone-slice3'
for f in glob.glob(D + '/*__*.png'):
    t = f.replace('__', '_')
    if os.path.exists(t): os.remove(t)
    os.rename(f, t)
for tag in ('afternoon', 'sunset'):
    src = 'docs/qa/evidence/look5c/frames_ts3_' + tag
    dst = D + '/frames_drive_' + tag
    if os.path.isdir(src):
        if os.path.isdir(dst): shutil.rmtree(dst)
        shutil.move(src, dst)
    # 시트: 프레임 0,60,120,180
    ims = [Image.open(f'{dst}/f{i:03d}.png') for i in (0, 60, 120, 180)]
    w, h = ims[0].size
    sh = Image.new('RGB', (w * 2, h * 2))
    for k, im in enumerate(ims): sh.paste(im, ((k % 2) * w, (k // 2) * h))
    sh.save(f'{D}/drive_{tag}_sheet.png')
# 2차 vs 3차 나란히
rows = []
for t in ('afternoon', 'sunset'):
    for i in ('1060', '1085'):
        a = Image.open(f'docs/qa/evidence/tone-slice2/{t}_{i}.png').convert('RGB')
        b = Image.open(f'{D}/{t}_{i}.png').convert('RGB').resize(a.size)
        r = Image.new('RGB', (a.width * 2, a.height)); r.paste(a, (0, 0)); r.paste(b, (a.width, 0)); rows.append(r.resize((r.width // 2, r.height // 2)))
S = Image.new('RGB', (rows[0].width, rows[0].height * len(rows)))
for k, r in enumerate(rows): S.paste(r, (0, k * r.height))
S.save(D + '/slice2_vs_slice3.png')
print(sorted(os.listdir(D)))
