from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent;repo=R.parents[2]
fontpath=repo/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'
font=ImageFont.truetype(str(fontpath),24)
small=ImageFont.truetype(str(fontpath),18)
out=Image.new('RGB',(2560,1360),'#ded8c9');d=ImageDraw.Draw(out)
d.text((24,18),'D-123 / 기준 원화',font=font,fill='#30362f')
d.text((1304,18),'택배 상자 / 씬 추적캠 값 · Blender 크기 검토',font=font,fill='#30362f')
ref=Image.open(repo/'art/concept/lofi-styles-2026-10-13/02_warm_street.png').convert('RGB');ref.thumbnail((1280,720))
out.paste(ref,(0,64));out.paste(Image.open(R/'box-chase-1x.png'),(1280,64))
d.text((24,802),'원화는 색 추출·비교에만 사용. 텍스처로 사용하지 않음.',font=small,fill='#30362f')
d.text((1304,802),'1280×720 원본 1:1 / 상자 약 59×52px / 적재 높이 가정·Unity 미검증',font=small,fill='#30362f')
out.paste(Image.open(R/'box-close.png').resize((640,360)),(24,860))
out.paste(Image.open(repo/'art/models/cargo_box_v01/cargo_box_atlas_64.png').resize((384,384),Image.Resampling.NEAREST),(710,860))
out.paste(Image.open(repo/'art/source/palette/pitcrew_d123_32.png'),(1150,860))
d.text((24,1254),'근접 확인 / 400×300×300mm / 12 tris',font=small,fill='#30362f')
d.text((710,1254),'64px 아틀라스 · 정수 6배',font=small,fill='#30362f')
d.text((1150,1135),'공용 32색 / 기준 원화 실제 픽셀 추출',font=small,fill='#30362f')
d.text((1730,880),'사진: ambientCG CardboardSet001 / CC0\n표시: Galmuri7 + 스크립트\n32.5~33.33px/m / 팔레트 밖 0\n앞: 송장 1 / 옆: ↑↑ / 위: 테이프\n회색 차량은 크기 확인용 임시 형상\n도현 승인 필요',font=small,fill='#30362f',spacing=16)
out.save(R/'compare.png')
sheet=Image.new('RGB',(1200,660),'#ded8c9');sd=ImageDraw.Draw(sheet)
for i,name in enumerate(['front','right','top','back','left','bottom']):
 x=30+(i%3)*400;y=25+(i//3)*320
 im=Image.open(repo/f'art/models/cargo_box_v01/patches/{name}_marked.png');sheet.paste(im.resize((im.width*20,im.height*20),Image.Resampling.NEAREST),(x,y+40));sd.text((x,y),name+' / 20×',font=small,fill='#30362f')
sheet.save(R/'six-faces.png')
