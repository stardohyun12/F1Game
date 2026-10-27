# 과제 1058 — A2e 텍스처: ① A Short Hike 1·2 에서 뽑은 32색 팔레트로 다시 양자화(디더 없음) + 사진 점무늬 줄이기(축소 → 중앙값 → 팔레트 → 최근접 확대).
# 기준 이미지에서는 색만 뽑는다(팔레트 32색). 이미지 자체는 게임에 안 넣는다. 결과 = Assets/Art/SceneA2/GenE/<원래 이름>.png
import json, os
from PIL import Image, ImageFilter, ImageEnhance
R = 'art/reference/dotlp-2026-10-27/'; A = 'F1/Assets/Art/SceneA2/'; OUT = A + 'GenE/'; EV = 'docs/qa/evidence/scene-a2e/'
os.makedirs(OUT, exist_ok=True); os.makedirs(EV, exist_ok=True)
# 1) 팔레트: ①_1 + ①_2 를 320×180 로 줄여 붙이고 32색 median cut. 회색·청회 그늘이 모자라 길·담용 따뜻한 회색 3색을 더한다(기록)
pal = []
for n, kq in (('a-short-hike_1.jpg', 19), ('a-short-hike_2.jpg', 10)):   # ①_1(숲·단풍·풀)에서 19색, ①_2(바다·배)에서 10색 — 한데 섞으면 풀 초록이 바다 파랑에 밀려 빠진다
    q = Image.open(R + n).convert('RGB').resize((320, 180), Image.BILINEAR).quantize(colors=kq, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal += [tuple(q.getpalette()[i * 3:i * 3 + 3]) for i in range(kq)]
pal += [(150, 140, 128), (122, 114, 108), (176, 166, 150)]   # 추가: 따뜻한 회색 아스팔트·석판(①의 바위색 근처)
pal.sort(key=lambda c: 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2])
json.dump({'source': ['art/reference/dotlp-2026-10-27/a-short-hike_1.jpg', 'a-short-hike_2.jpg'], 'method': '각 320x180 bilinear, PIL MEDIANCUT ①_1 19색 + ①_2 10색 + 따뜻한 회색 3 = 32', 'palette': pal},
          open(EV + 'palette_ash.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
sw = Image.new('RGB', (32 * 24, 24))
for i, c in enumerate(pal): sw.paste(c, (i * 24, 0, i * 24 + 24, 24))
sw.save(EV + 'palette_ash.png')
pimg = Image.new('P', (1, 1)); flat = [v for c in pal for v in c]; pimg.putpalette(flat + flat[:3] * (256 - len(pal)))

# 2) 텍스처별: (경로, 축소 배율, 중앙값 크기, 채도 배율[, 밝기 배율]) — 1차 판에서 나무가 검붉고 길이 점박이라 나무 밝기 ×1.3, 길 축소 2·중앙값 5, 2차에서 소품·원경이 남색 덩어리라 소품 축소 2·중앙값 5·밝기 ×1.25, 원경 밝기 ×1.3(가설)
JOBS = [('Gen/asphalt_tile.png', 2, 5, 1.0), ('Gen/wall_tile_grey.png', 1, 3, 1.0), ('Gen/pole_wood.png', 1, 1, 1.2), ('Gen/wire.png', 1, 1, 1.0),
        ('Gen/grass_tile_soft.png', 8, 5, 1.35), ('Gen/yard_ground_soft.png', 4, 5, 1.3)]
JOBS += [(f'Gen/autumn_tree_0{k}_soft.png', 4, 5, 1.4, 1.3) for k in (1, 2, 3, 5)]
JOBS += [('far/far_town_baked32.png', 2, 3, 1.2, 1.3), ('far/far_terrace_baked32.png', 1, 3, 1.3), ('v5/garage_baked32.png', 2, 3, 1.3, 1.15),
         ('v5/sea_baked32.png', 2, 3, 1.3), ('kart/kart_a2_baked32.png', 1, 3, 1.3)]
JOBS += [('kit/' + n + '.png', 2, 5, 1.0, 1.25) for n in ('background_building_03', 'bench_01', 'bicycle_01', 'cone_01', 'cone_02', 'drum_01', 'drum_02', 'drum_03', 'old_car_01',
                                                   'pallet_01', 'pallet_02', 'pot_01', 'pot_02', 'streetlight_01', 'tires_01', 'tires_02', 'transformer_01')]
for path, k, med, sat, *br in JOBS:
    im = Image.open(A + path).convert('RGBA'); W, H = im.size
    small = im.resize((max(1, W // k), max(1, H // k)), Image.BOX) if k > 1 else im
    rgb = small.convert('RGB'); a = small.getchannel('A').point(lambda v: 255 if v >= 128 else 0)
    if med > 1: rgb = rgb.filter(ImageFilter.MedianFilter(med))
    rgb = ImageEnhance.Color(rgb).enhance(sat)
    if br: rgb = ImageEnhance.Brightness(rgb).enhance(br[0])
    rgb = rgb.quantize(palette=pimg, dither=Image.Dither.NONE).convert('RGB')
    if med > 1: rgb = rgb.filter(ImageFilter.ModeFilter(3))        # 팔레트 뒤 외톨이 1px 지움
    out = rgb.convert('RGBA'); out.putalpha(a)
    if k > 1: out = out.resize((W, H), Image.NEAREST)
    os.makedirs(os.path.dirname(OUT + path), exist_ok=True); out.save(OUT + path)
print('palette', len(pal), 'textures', len(JOBS))
