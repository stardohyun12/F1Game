import sys, json
import numpy as np
from PIL import Image
S = 'C:/Users/rtx40/AppData/Local/hermes/cache/scratch/'
sys.path.insert(0, S)
from measure import stats

items = {
 'edc_01': 4.22, 'edc_02': 4.24, 'edc_03': 4.85, 'edc_04': 4.22, 'edc_05': 4.22,
 'edc_06': 4.24, 'edc_07': 4.24, 'edc_08': 5.63, 'edc_09': 5.63,
}
ours = {
 'a_car_look0': 'docs/qa/evidence/retro-v1/a_car_look0.png',
 'a_car_look4': 'docs/qa/evidence/retro-v1/a_car_look4.png',
 'b_street_look0': 'docs/qa/evidence/retro-v1/b_street_look0.png',
 'b_street_look4': 'docs/qa/evidence/retro-v1/b_street_look4.png',
 'c_town_look0': 'docs/qa/evidence/retro-v1/c_town_look0.png',
 'c_town_look4': 'docs/qa/evidence/retro-v1/c_town_look4.png',
 'f_grass_look0': 'docs/qa/evidence/retro-v1/f_grass_look0.png',
 'f_grass_look4': 'docs/qa/evidence/retro-v1/f_grass_look4.png',
}

def native(f, p):
    a = np.asarray(Image.open(f).convert('RGB')).astype(np.float32)
    H, W = a.shape[:2]
    nx, ny = int(W / p), int(H / p)
    xs = ((np.arange(nx) + 0.5) * p).astype(int)
    ys = ((np.arange(ny) + 0.5) * p).astype(int)
    # median of 3x3 around centre to kill jpeg ringing
    acc = []
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            acc.append(a[np.clip(ys+dy, 0, H-1)][:, np.clip(xs+dx, 0, W-1)])
    return np.median(np.stack(acc), axis=0)

out = {}
for k, p in items.items():
    n = native(f'art/reference/edc-2026-10-26/{k}.jpg', p)
    Image.fromarray(n.astype(np.uint8)).save(S + f'native_{k}.png')
    out[k] = dict(native=[n.shape[1], n.shape[0]], **stats(n))
for k, f in ours.items():
    n = native(f, 2.0)
    Image.fromarray(n.astype(np.uint8)).save(S + f'native_{k}.png')
    out[k] = dict(native=[n.shape[1], n.shape[0]], **stats(n))
json.dump(out, open(S + 'm2.json', 'w'), indent=1)
keys = list(next(iter(out.values())).keys())
print('name'.ljust(16), ' '.join(k[:9].rjust(9) for k in keys[1:]))
for k, v in out.items():
    print(k.ljust(16), ' '.join(str(v[x]).rjust(9) for x in keys[1:]))
