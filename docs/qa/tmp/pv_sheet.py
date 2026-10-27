# 과제 0535: preview-1028 캡처를 한 장으로 (확인용)
import sys
from PIL import Image
d = 'D:/Github/F1Game/docs/qa/evidence/preview-1028/'
fs = sys.argv[2:] or ['street_without', 'street_with', 'house_without', 'house_with', 'garage_without', 'garage_with', 'depot15', 'depot5', 'room_door', 'room_desk', 'room_window']
ims = [Image.open(d + f + '.png').resize((640, 360)) for f in fs]
rows = (len(ims) + 1) // 2
W = Image.new('RGB', (1280, 360 * rows), 'white')
for i, im in enumerate(ims):
    W.paste(im, ((i % 2) * 640, (i // 2) * 360))
W.save(sys.argv[1])
