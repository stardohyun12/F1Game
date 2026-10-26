import sys
from PIL import Image
D = 'D:/Github/F1Game/docs/qa/evidence/scene-a/'
tag = sys.argv[1] if len(sys.argv) > 1 else ''
W, H = 480, 270
s = Image.new('RGB', (W * 4, H * 2))
for v in range(4):
    for c, n in enumerate(('game', 'intro')):
        s.paste(Image.open(f'{D}V{v}_{n}{tag}.png').resize((W, H), Image.BOX), (v * W, c * H))
s.save(D + '_peek.png')
