"""sheet.png: 위 = 1차(field_terrace/new_*), 아래 = 2차. compare_main_vs_copy.png: 본 에디터 map_B_eye vs 사본 새 B_eye + 창고 벽 픽셀 비교.
Writes art/_review/2026-10-30-field_terrace2/{sheet,compare_main_vs_copy}.png only."""
from PIL import Image, ImageDraw, ImageFont
import numpy as np
d1='D:/Github/F1Game/art/_review/2026-10-30-field_terrace/'
d2='D:/Github/F1Game/art/_review/2026-10-30-field_terrace2/'
dm='D:/Github/F1Game/art/_review/2026-10-29-field_patch/'
cols=[('A 도현 카메라','A_high'),('B 도현 카메라','B_high'),('A 눈높이(집 마당 위)','A_eye'),('B 눈높이(집 마당 위)','B_eye'),('A 카트 눈높이','A_cart')]
W=520; pad=14; head=60; lab=26
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18); big=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',26)
def fit(p):
    im=Image.open(p).convert('RGB'); h=int(im.height*W/im.width); return im.resize((W,h),Image.Resampling.LANCZOS)
rows=[('1차 목업 (사본 에디터 · 흙 섬·판·단일 작물)',d1,'new_'),('2차 목업 — 땅에 붙임·1.3m 타일·판 제거·작물 3종',d2,'new_')]
imgs=[[fit(r[1]+r[2]+c[1]+'.png') for c in cols] for r in rows]
rowh=[max(i.height for i in r) for r in imgs]
H=head+sum(rowh)+len(rows)*(lab+pad)+pad
sheet=Image.new('RGB',(pad+len(cols)*(W+pad),H),(232,228,218))
dr=ImageDraw.Draw(sheet); dr.text((pad,12),'밭 목업 — 1차(위) vs 2차(아래), 같은 컷끼리  ·  임시 배치, 씬 저장 없음',font=big,fill=(50,45,40))
y=head
for r,ims,rh in zip(rows,imgs,rowh):
    dr.text((pad,y),r[0],font=font,fill=(90,80,70)); y+=lab
    for ci,(im,(cap,_)) in enumerate(zip(ims,cols)):
        sheet.paste(im,(pad+ci*(W+pad),y)); dr.text((pad+ci*(W+pad)+6,y+4),cap,font=font,fill=(255,255,255),stroke_width=2,stroke_fill=(40,40,40))
    y+=rh+pad
sheet.save(d2+'sheet.png'); print('sheet',sheet.size)
# 본 에디터 vs 사본: B 눈높이(창고가 보이는 같은 카메라)
a=Image.open(dm+'map_B_eye.png').convert('RGB'); b=Image.open(d2+'new_B_eye.png').convert('RGB')
reg=(250,150,560,280) # 창고 벽
def stat(im):
    x=np.asarray(im.crop(reg)).reshape(-1,3).astype(float); return x.mean(0)
def stat2(im,box):
    x=np.asarray(im.crop(box)).reshape(-1,3).astype(float); return x.mean(0)
sa,sb=stat(a),stat(b)
print('warehouse wall mean main',sa.round(1),'copy',sb.round(1))
grass=(60,420,300,600)
ga,gb=stat2(a,grass),stat2(b,grass)
print('grass mean main',ga.round(1),'copy',gb.round(1))
sky=(700,20,1200,120); ka,kb=stat2(a,sky),stat2(b,sky); print('sky main',ka.round(1),'copy',kb.round(1))
c=Image.new('RGB',(1280*2+30,720+80),(232,228,218)); c.paste(a,(10,70)); c.paste(b,(1290+10,70))
dd=ImageDraw.Draw(c)
dd.text((12,10),'본 에디터 (2026-10-29 map_B_eye) · 창고 벽 평균 %d/%d/%d · 풀(좌하) %d/%d/%d'%(*sa.round(),*ga.round()),font=font,fill=(50,45,40))
dd.text((1300,10),'사본 에디터 (새 B 눈높이) · 창고 벽 평균 %d/%d/%d · 풀(좌하) %d/%d/%d'%(*sb.round(),*gb.round()),font=font,fill=(50,45,40))
dd.rectangle((10+reg[0],70+reg[1],10+reg[2],70+reg[3]),outline=(255,255,0),width=2); dd.rectangle((1300+reg[0],70+reg[1],1300+reg[2],70+reg[3]),outline=(255,255,0),width=2)
dd.rectangle((10+grass[0],70+grass[1],10+grass[2],70+grass[3]),outline=(255,0,255),width=2); dd.rectangle((1300+grass[0],70+grass[1],1300+grass[2],70+grass[3]),outline=(255,0,255),width=2)
c.save(d2+'compare_main_vs_copy.png'); print('cmp saved')
