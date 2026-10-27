from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import numpy as np,json,shutil,cv2,csv,hashlib
W=Path(__file__).parent;R=W.parents[2];E=R/'art/export/q2-garage';V=R/'art/_review/2026-10-28-q2-garage';E.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True);PH=R/'art/source/photos/scene-a';sources=[]
def photo(name,size,base,amp=.04):
 p=PH/name/(name+'_1K-JPG_Color.jpg');im=Image.open(p).convert('RGB');a=np.array(im,float);y=a@np.array([.299,.587,.114]);low=cv2.GaussianBlur(y,(0,0),64);grain=y/np.maximum(low,1);grain=Image.fromarray(grain.astype('float32')).resize(size,Image.Resampling.BOX);g=np.array(grain);g=(g-np.median(g))/max(float(np.std(g)),1e-4);factor=1+np.clip(g,-1,1)*amp;out=np.clip(np.rint(np.array(base)*factor[:,:,None]),0,255).astype('uint8');sources.append(dict(photo=str(p.relative_to(R)),source_url='https://ambientcg.com/view?id='+name,license='CC0-1.0',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),output_size=size,base=base,amplitude=amp));return Image.fromarray(out)
# Photo grain retained; remove circular painted stains by resampling clean material only into roof islands.
ceiling=photo('CorrugatedSteel005',(256,256),(164,104,67));ceiling.save(E/'ceiling_photo.png');tile=np.array(ceiling)
old=np.array(Image.open(R/'art/export/scene-albedo-kit/garage_baked32.png').convert('RGBA'));a=old.copy();h,w=a.shape[:2];mask=np.zeros((h,w),bool)
for t in json.loads((W/'roof-jobs.json').read_text()):
 q=np.array(t['uv'])*[w,h];q[:,1]=h-q[:,1];aa,bb,cc=q;lo=np.maximum(0,np.floor(q.min(0)).astype(int));hi=np.minimum([w-1,h-1],np.ceil(q.max(0)).astype(int));yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];x=xx+.5;y=yy+.5;den=(bb[1]-cc[1])*(aa[0]-cc[0])+(cc[0]-bb[0])*(aa[1]-cc[1])
 if abs(den)<1e-12:continue
 u=((bb[1]-cc[1])*(x-cc[0])+(cc[0]-bb[0])*(y-cc[1]))/den;v=((cc[1]-aa[1])*(x-cc[0])+(aa[0]-cc[0])*(y-cc[1]))/den;ok=(u>=0)&(v>=0)&(u+v<=1);p=u[:,:,None]*np.array(t['world'][0])+v[:,:,None]*np.array(t['world'][1])+(1-u-v)[:,:,None]*np.array(t['world'][2]);tx=np.floor(p[:,:,0]*32).astype(int)%256;ty=np.floor(p[:,:,1]*32).astype(int)%256;a[yy[ok],xx[ok],:3]=tile[ty[ok],tx[ok]];mask[yy[ok],xx[ok]]=True
# Expand only island padding, preserve all other artwork exactly.
for _ in range(2):
 n=mask.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  m=~n&np.roll(mask,(dy,dx),(0,1));a[m,:3]=np.roll(a,(dy,dx),(0,1))[m,:3];n[m]=True
 mask=n
Image.fromarray(a).save(E/'garage_baked32.png');np.save(W/'roof-mask.npy',mask);assert np.array_equal(a[~mask],old[~mask]);shutil.copy2(R/'art/export/scene-albedo-kit/dirt_baked32.png',E/'dirt_baked32.png')
# Preserve F2/F3 interchangeable atlas variants, cleaning the same roof pixels.
for name in ['garage_tin_F2_atlas','garage_tin_F3_atlas']:
 b=np.array(Image.open(R/'art/export/scene-albedo-kit'/(name+'.png')).convert('RGBA'));b[mask,:3]=a[mask,:3];Image.fromarray(b).save(E/(name+'.png'))
photo('Ground081',(128,128),(132,124,100)).save(W/'preview_ground.png')
colors=[('PaintedWood008C',(138,104,65)),('Metal021',(77,77,77)),('PaintedMetal004',(164,76,52)),('Rubber004',(57,57,57)),('PaintedWood008C',(183,176,153)),('Metal041B',(124,124,115))]
atlas=Image.new('RGB',(512,256))
for i,(src,c) in enumerate(colors):atlas.paste(photo(src,(128,128),c),(i%4*128,i//4*128))
atlas.save(E/'garage_details.png')
# Original bitmap glyphs over photographic painted wood. No flat fill backgrounds.
G={'A':['01110','10001','10001','11111','10001','10001','10001'],'E':['11111','10000','10000','11110','10000','10000','11111'],'G':['01111','10000','10000','10111','10001','10001','01111'],'I':['11111','00100','00100','00100','00100','00100','11111'],'P':['11110','10001','10001','11110','10000','10000','10000'],'R':['11110','10001','10001','11110','10100','10010','10001'],'T':['11111','00100','00100','00100','00100','00100','00100']}
sign=photo('PaintedWood008C',(256,128),(186,178,151));d=ImageDraw.Draw(sign)
def letters(word,y,scale):
 width=len(word)*6*scale-scale;x=(214-width)//2
 for j,ch in enumerate(word):
  for yy,row in enumerate(G[ch]):
   for xx,bit in enumerate(row):
    if bit=='1':d.rectangle((x+j*6*scale+xx*scale,y+yy*scale,x+j*6*scale+xx*scale+scale-1,y+yy*scale+scale-1),fill=(54,58,54))
letters('PIT',7,3);letters('GARAGE',39,4);sign.save(E/'garage_sign.png')
paper=photo('PaintedWood008C',(128,128),(184,175,144));d=ImageDraw.Draw(paper);ft=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',n)
d.text((8,8),'BAY LOOP',font=ft(19),fill=(66,82,87));d.text((10,34),'AUTUMN 26',font=ft(13),fill=(113,57,40));d.line((8,54,119,54),fill=(66,82,87),width=2);d.text((8,61),'대회 참가 신청',font=ft(15),fill=(57,61,55));d.text((8,86),'REGISTER HERE',font=ft(11),fill=(57,61,55));d.text((8,106),'PIT GARAGE',font=ft(11),fill=(113,57,40));paper.save(E/'garage_festival.png')
for idx,ref in enumerate(['ref02.jpg','ref03.jpg','ref04.jpg']):
 p=R/'art/reference/kart-v6'/ref;im=Image.open(p).convert('RGB')
 if idx==0:im=im.crop((350,350,920,700))
 im=im.resize((60,40),Image.Resampling.BOX);rgb=np.array(im,float);y=rgb@np.array([.299,.587,.114]);rgb=rgb*.58+y[:,:,None]*np.array([.44,.40,.34]);out=photo('PaintedWood008C',(68,48),(180,168,138));out.paste(Image.fromarray(np.clip(rgb,0,205).astype('uint8')).quantize(24,dither=Image.Dither.NONE).convert('RGB'),(4,4));out.save(E/f'garage_memory_{idx+1}.png')
# Gate reference: exact left panel, remove only 22px label strip; no colour processing.
ref=Image.open(R/'docs/qa/evidence/scene-a7b/garage_trio.png');ref.crop((0,22,ref.width//3,ref.height)).save(V/'game_reference_left.png')
(W/'material-sources.json').write_text(json.dumps(sources,indent=2));(W/'texture-changes.json').write_text(json.dumps(dict(roof_pixels=int(mask.sum()),other_pixels_unchanged=True,dirt_byte_identical=True,roof_cause='painted circular stains in original tin_roof.png, inherited through emission-only atlas transfer',light_bake=False),indent=2));print('Q2 GARAGE TEXTURES OK')
