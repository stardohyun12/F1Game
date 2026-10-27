# 과제 1704: h2_i_j.png(A6·A6b·A6c 같은 B 컷) + 집 영역 어두운 쪽 숫자
# p5 = 크롭 픽셀 휘도(0.299R+0.587G+0.114B)의 하위 5% 값, dark40 = 휘도 40 미만 픽셀 비율(%)
from PIL import Image, ImageDraw
R = 'D:/Github/F1Game/docs/qa/evidence/'
SC = [('A6 h2', 'scene-a6'), ('A6b i', 'scene-a6b'), ('A6c j', 'scene-a6c')]
ims = [Image.open(R + d + '/b_game.png').convert('RGB').resize((960, 540), Image.NEAREST) for _, d in SC]
V = Image.new('RGB', (960 * 3 + 20, 570), (30, 30, 30)); dr = ImageDraw.Draw(V)
for i, (im, (nm, _)) in enumerate(zip(ims, SC)): V.paste(im, (i * 970, 30)); dr.text((i * 970 + 8, 8), nm, fill=(255, 255, 255))
V.save(R + 'scene-a6c/h2_i_j.png')

def dark(path, box):
    im = Image.open(path).convert('RGB').crop(box); L = sorted(0.299 * r + 0.587 * g + 0.114 * b for r, g, b in im.getdata())
    return L[len(L) * 5 // 100], 100 * sum(1 for l in L if l < 40) / len(L), L[len(L) // 2]
CROPS = {'b_game CAFE-side house (1220,440,1430,640)': ('b_game.png', (1220, 440, 1430, 640)),
         'b_game left 3-storey house (300,180,640,520)': ('b_game.png', (300, 180, 640, 520)),
         'FISH close house body (650,230,1300,860)': ('house_fish.png', (650, 230, 1300, 860))}
for nm, (f, box) in CROPS.items():
    row = []
    for lab, d in SC:
        try: p5, d40, med = dark(R + d + '/' + f, box); row.append(f'{lab}: p5 {p5:.0f} dark<40 {d40:.1f}% med {med:.0f}')
        except FileNotFoundError: row.append(f'{lab}: -')
    print(nm + ' | ' + ' | '.join(row))
for lab, d in [('A4e e', 'scene-a4e'), ('A6c j', 'scene-a6c')]:
    p5, d40, med = dark(R + d + '/house_rooms.png', (300, 0, 1500, 900)); print(f'ROOMS close (300,0,1500,900) {lab}: p5 {p5:.0f} dark<40 {d40:.1f}% med {med:.0f}')
