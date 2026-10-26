from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-27-garage-kart'
labels=[('story_garage_v01_front.png','정비소 정면'),('story_garage_v01_threequarter.png','정비소 3/4'),('story_garage_v01_interior.png','작업대 · 공구 · 기록 · 빈 칸 3개'),('story_garage_v01_boards.png','차 · 실력 · 응원 / 승인 전 빈 보드'),('delivery_kart_v01_front.png','첫 배달 카트 정면'),('delivery_kart_v01_threequarter.png','카트 3/4 · 작은 짐상자'),('delivery_kart_v01_side.png','카트 옆 · 950mm 축간'),('delivery_kart_v01_top.png','카트 위 · 1단위=1m')]
sheet=Image.new('RGB',(1600,2300),'#e7dbc6');d=ImageDraw.Draw(sheet);f=ImageFont.truetype(str(R/'art/_review/2026-10-23-course-ui/fonts/Galmuri11.ttf'),22)
d.text((24,16),'항구 정비소 + 고카트 / 도현 승인 필요',font=f,fill='#36595c')
for i,(name,label) in enumerate(labels):
 x=i%2*800;y=60+i//2*555;im=Image.open(V/name).convert('RGB').resize((800,533));sheet.paste(im,(x,y));d.text((x+12,y+530),label,font=f,fill='#36595c')
sheet.save(V/'sheet.png')
