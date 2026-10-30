"""sheet.png: 위 = 이전 목업(같은 환경 재촬영), 아래 = 새 목업. Writes art/_review/2026-10-30-field_terrace/sheet.png only."""
from PIL import Image, ImageDraw, ImageFont
d='D:/Github/F1Game/art/_review/2026-10-30-field_terrace/'
cols=[('A 도현 카메라','A_high'),('B 도현 카메라','B_high'),('A 눈높이(집 마당 위)','A_eye'),('B 눈높이(집 마당 위)','B_eye'),('A 카트 눈높이','A_cart')]
W=520; pad=14; head=60; lab=26
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18); big=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',26)
def fit(f):
    im=Image.open(d+f).convert('RGB'); h=int(im.height*W/im.width); return im.resize((W,h),Image.Resampling.LANCZOS)
rows=[('이전 목업 (같은 사본 에디터에서 재촬영)','old_'),('새 목업 — 질감·형체 고침','new_')]
imgs=[[fit(p+c[1]+'.png') for c in cols] for _,p in rows]
rowh=[max(i.height for i in r) for r in imgs]
H=head+sum(rowh)+len(rows)*(lab+pad)+pad
sheet=Image.new('RGB',(pad+len(cols)*(W+pad),H),(232,228,218))
dr=ImageDraw.Draw(sheet); dr.text((pad,12),'밭 목업 — 이전(위) vs 질감·형체 고침(아래), 같은 컷끼리  ·  임시 배치, 씬 저장 없음',font=big,fill=(50,45,40))
y=head
for (title,_),r,rh in zip(rows,imgs,rowh):
    dr.text((pad,y),title,font=font,fill=(90,80,70)); y+=lab
    for ci,(im,(cap,_)) in enumerate(zip(r,cols)):
        sheet.paste(im,(pad+ci*(W+pad),y)); dr.text((pad+ci*(W+pad)+6,y+4),cap,font=font,fill=(255,255,255),stroke_width=2,stroke_fill=(40,40,40))
    y+=rh+pad
sheet.save(d+'sheet.png'); print(sheet.size)
