# 2판 cam_v2 | 3판 cam_v3 점확대 2배 (같은 카메라)
from PIL import Image
a=Image.open('docs/qa/evidence/sunset2/cam_v2.png').convert('RGB'); b=Image.open('docs/qa/evidence/sunset3/cam_v3.png').convert('RGB')
bd=Image.new('RGB',(1288,360)); bd.paste(a,(0,0)); bd.paste(b,(648,0)); bd.resize((2576,720),Image.NEAREST).save('docs/qa/tmp/sunset3/quick.png')
b.crop((0,150,640,360)).resize((1920,630),Image.NEAREST).save('docs/qa/tmp/sunset3/quick_zoom.png')
