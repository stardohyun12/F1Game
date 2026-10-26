import time
from PIL import Image
time.sleep(30)
D = 'docs/qa/evidence/tone-slice3'
rows = []
for t in ('afternoon', 'sunset'):
    for i in ('1060', '1085'):
        a = Image.open(f'docs/qa/evidence/tone-slice2/{t}_{i}.png').convert('RGB')
        b = Image.open(f'{D}/{t}_{i}.png').convert('RGB').resize(a.size)
        r = Image.new('RGB', (a.width * 2, a.height)); r.paste(a, (0, 0)); r.paste(b, (a.width, 0)); rows.append(r.resize((r.width // 2, r.height // 2)))
S = Image.new('RGB', (rows[0].width, rows[0].height * len(rows)))
for k, r in enumerate(rows): S.paste(r, (0, k * r.height))
S.save(D + '/slice2_vs_slice3.png')
print('ok')
