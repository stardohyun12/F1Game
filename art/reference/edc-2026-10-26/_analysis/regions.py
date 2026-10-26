import numpy as np
from PIL import Image, ImageFilter
S = 'C:/Users/rtx40/AppData/Local/hermes/cache/scratch/'
regions = [
 ('EDC02 도로(눈길)', 'native_edc_02.png', (5, 140, 150, 250)),
 ('EDC06 도로', 'native_edc_06.png', (20, 185, 160, 250)),
 ('EDC06 눈 비탈', 'native_edc_06.png', (330, 150, 450, 250)),
 ('EDC04 붉은 벽', 'native_edc_04.png', (55, 0, 160, 50)),
 ('EDC04 눈길', 'native_edc_04.png', (280, 160, 450, 250)),
 ('EDC03 실내 바닥', 'native_edc_03.png', (150, 150, 260, 220)),
 ('EDC09 눈밭(밤)', 'native_edc_09.png', (0, 150, 120, 190)),
 ('우리L0 도로', 'native_a_car_look0.png', (150, 280, 235, 320)),
 ('우리L4 도로', 'native_a_car_look4.png', (150, 280, 235, 320)),
 ('우리L0 풀', 'native_a_car_look0.png', (0, 190, 150, 240)),
 ('우리L4 풀', 'native_a_car_look4.png', (0, 190, 150, 240)),
 ('우리L0 인도', 'native_a_car_look0.png', (575, 225, 635, 275)),
 ('우리L4 인도', 'native_a_car_look4.png', (575, 225, 635, 275)),
 ('우리L0 벽', 'native_b_street_look0.png', (0, 25, 150, 200)),
 ('우리L4 벽', 'native_b_street_look4.png', (0, 25, 150, 200)),
 ('우리L0 원경산', 'native_a_car_look0.png', (450, 50, 630, 100)),
 ('우리L4 원경산', 'native_a_car_look4.png', (450, 50, 630, 100)),
]
print('영역 | 평균휘도 | 영역std | 고주파std(1px) | 반점% (3x3중앙값서 ±12↑) | 채도 | 고유색(4bit,1%↑)')
rows = []
for name, f, box in regions:
    im = Image.open(S + f).convert('RGB').crop(box)
    a = np.asarray(im).astype(np.float32)
    L = 0.2126*a[...,0] + 0.7152*a[...,1] + 0.0722*a[...,2]
    Li = Image.fromarray(L.astype(np.uint8))
    blur = np.asarray(Li.filter(ImageFilter.BoxBlur(1))).astype(np.float32)
    med = np.asarray(Li.filter(ImageFilter.MedianFilter(3))).astype(np.float32)
    hf = (L - blur)[1:-1, 1:-1].std()
    speck = (np.abs(L - med)[1:-1, 1:-1] > 12).mean()*100
    rgb = a/255; mx = rgb.max(2); mn = rgb.min(2)
    sat = np.where(mx > 0, (mx-mn)/np.maximum(mx, 1e-6), 0).mean()
    q = (a//16).astype(int); k = q[...,0]*256+q[...,1]*16+q[...,2]
    c = np.bincount(k.ravel()); nc = int((c > k.size*0.01).sum())
    r = (name, round(float(L.mean())), round(float(L.std()), 1), round(float(hf), 2), round(float(speck), 1), round(float(sat), 3), nc)
    rows.append(r)
    print(' | '.join(str(x) for x in r))
