from pathlib import Path
import sys,json,csv,re,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kit-shared-v2';V=R/'art/_review/2026-10-28-kit-shared-v2'
E.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-gen'));import delight as dl
P='art/source/photo-picks/';PH='art/source/photos/scene-a/'
jobs={};records=[];cells=[];pal=dl.palette(R/'art/export/kit-shared-v1/palette.gpl').tolist()
bases={'apricot':[191,159,130],'cream':[191,187,157],'mint':[153,177,153],
       'wood':[134,109,77],'frame':[137,163,146],'door':[84,119,107],
       'skirting':[128,128,116],'gutter':[78,81,80],'shutter':[136,157,146],
       'glass':[57,72,72],'stone':[157,152,132],'concrete':[177,175,161],
       'asphalt':[61,60,58],'E2':[157,96,65],'E3':[160,116,83],
       'F2':[156,76,59],'F3':[113,149,171],'paper':[190,182,153],'ink':[64,69,65]}
# Extend, don't replace garage paint slots. Five luminance steps per photographic material.
for name,b in bases.items():
 for f in [.96,.97,.98,.99,1,1.01,1.02,1.03,1.04]:
  c=np.rint(np.array(b)*f).clip(0,255).astype(int).tolist()
  if c not in pal:pal.append(c)
assert len(pal)<=256
(E/'palette.gpl').write_text('GIMP Palette\nName: PitCrew shared v1 - garage40 + photographic paint ramps\nColumns: 8\n#\n'+''.join('%3d %3d %3d\tshared%03d\n'%(*c,i) for i,c in enumerate(pal)),encoding='utf-8')
pal=np.array(pal,dtype='uint8')

def full(path):
 im=Image.open(R/path);w,h=im.size;return [[0,0],[w-1,0],[w-1,h-1],[0,h-1]]
def photo(key,path,base,physical=(8,8),quad=None,kind='paint',tile=(0,1)):
 j={'input':path,'quad_px':quad or full(path),'physical_m':list(physical),'px_per_m':32,
    'material':kind,'base_srgb':bases[base],'amplitude':.04,'shadow_radius_m':1.0,'texture_strength':3.0,'grain_sigma_px':.25,'tile_axes':list(tile),
    'palette':'art/export/kit-shared-v2/palette.gpl','output':'art/source/kit-shared-v2/parts/'+key+'.png'}
 a,info=dl.process(j,R,pal);jobs[key]=j;records.append(info)
 p=R/j['output'];p.parent.mkdir(exist_ok=True);Image.fromarray(a).save(p)
 return a
def expand(a,w=256,h=256):return np.tile(a,(int(np.ceil(h/a.shape[0])),int(np.ceil(w/a.shape[1])),1))[:h,:w]
def cell(sheet,name,xy,a):
 x,y=xy;sheet[y:y+256,x:x+256]=a;cells.append(dict(name=name,rect_px=[x,y,256,256],tile_m=[8,8],ppm=32))

wall=np.zeros((512,512,3),dtype='uint8')
quad=[[380,560],[624,558],[628,740],[382,744]]
for name,xy in [('apricot',(0,0)),('cream',(256,0)),('mint',(0,256))]:
 a=photo('wall_'+name,P+'A/A2_apricot_facade_burano.jpg',name,(2,2),quad,kind='plaster');cell(wall,'wall_'+name,xy,expand(a))
# Fourth cell same material with a different photographed patch, not synthetic noise.
a=photo('wall_apricot_B',P+'A/A2_apricot_facade_burano.jpg','apricot',(2,2),[[390,565],[634,563],[638,745],[392,749]],kind='plaster')
cell(wall,'wall_apricot_B',(256,256),expand(a));Image.fromarray(wall).save(E/'wall.png');wallcells=cells.copy();cells=[]

floor=np.zeros_like(wall)
for name,xy,path,base,kind in [
 ('stone',(0,0),P+'H/H3_old_stone_wall.jpg','stone','paint'),
 ('wood',(256,0),PH+'WoodSiding008/WoodSiding008_1K-JPG_Color.jpg','wood','paint'),
 ('concrete',(0,256),PH+'Concrete042A/Concrete042A_1K-JPG_Color.jpg','concrete','concrete'),
 ('asphalt',(256,256),PH+'Ground081/Ground081_1K-JPG_Color.jpg','asphalt','asphalt')]:
 a=photo('floor_'+name,path,base,kind=kind);cell(floor,'floor_'+name,xy,a)
Image.fromarray(floor).save(E/'floor.png');floorcells=cells.copy();cells=[]
roof=np.zeros_like(wall)
for name,xy,file in [('E2',(0,0),'E/E2_clay_roof_tiles_02.jpg'),('E3',(256,0),'E/E3_clay_roof_tiles_03.jpg'),('F2',(0,256),'F/F2_box_profile_red.jpg'),('F3',(256,256),'F/F3_corrugated_shed.jpg')]:
 q=[[702,365],[792,395],[798,790],[703,820]] if name=='F3' else None
 size=(2,4) if name=='F3' else ((4,4) if name=='E3' else (8,8))
 a=photo('roof_'+name,P+file,name,size,q);a=expand(a);a=dl.remove_isolated(dl.quantize(dl.periodic(a),pal));cell(roof,'roof_'+name,xy,a)
Image.fromarray(roof).save(E/'roof.png');roofcells=cells.copy();cells=[]

trim=np.zeros_like(wall);bands=[];y=0
for name,height,base,source in [('eaves',16,'wood','PaintedWood008C'),('window_frame',16,'frame','PaintedWood008C'),('door_frame',16,'door','PaintedWood008C'),('skirting',16,'skirting','Concrete042A'),('gutter',16,'gutter','Metal021'),('shutter',48,'shutter','B1'),('glass',128,'glass','Metal021'),('wood_door',256,'wood','WoodSiding008')]:
 path=P+'B/B1_shutters_greengrocer.jpg' if source=='B1' else PH+source+'/'+source+'_1K-JPG_Color.jpg'
 q=[[1150,360],[1450,360],[1450,980],[1150,980]] if source=='B1' else None
 a=photo('trim_'+name,path,base,(16,height/32),q,tile=(1,))
 # Painted structural grooves are material boundaries, not light or AO.
 if name=='shutter':
  for k in range(7,height,8):a[k]=dl.quantize(np.array([bases['door']]*512,dtype='uint8'),pal)
 a=dl.remove_isolated(a);trim[y:y+height]=a;bands.append(dict(name=name,rect_px=[0,y,512,height],tile_m=[16,height/32],tile_axes=['U'],ppm=32));y+=height
Image.fromarray(trim).save(E/'trim.png')

# Alpha sheet: material photograph in RGB; vector masks/lettering are original hand-authored layouts.
decal=Image.new('RGBA',(512,512),(0,0,0,0));items=[]
paper=photo('decal_paper',PH+'PaintedWood008C/PaintedWood008C_1K-JPG_Color.jpg','paper',(4,4))
wood=photo('decal_wood',PH+'PaintedWood008C/PaintedWood008C_1K-JPG_Color.jpg','wood',(4,4))
ink=tuple(bases['ink'])+(255,)
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',n)
def sticker(name,rect,lines,bg=paper):
 x,y,w,h=rect;im=Image.fromarray(expand(bg,w,h)).convert('RGBA');d=ImageDraw.Draw(im)
 d.rectangle((0,0,w-1,h-1),outline=ink,width=2)
 for text,yy,sz in lines:
  f=font(sz);b=d.textbbox((0,0),text,font=f);d.text(((w-(b[2]-b[0]))//2,yy),text,font=f,fill=ink)
 if name.startswith('poster_'):
  w//=2;h//=2;im=im.resize((w,h),Image.Resampling.NEAREST)
 decal.paste(im,(x,y));items.append(dict(name=name,rect_px=list(rect),size_m=[w/32,h/32]))
 items[-1]['rect_px']=[x,y,w,h]
sticker('sign_cream',(8,8,192,48),[])
sticker('sign_wood',(216,8,192,48),[],wood)
sticker('poster_festival',(8,80,96,128),[('BAY LOOP',8,14),('AUTUMN',32,12),('26',53,30),('RACE DAY',99,12)])
sticker('poster_harbor',(120,80,96,128),[('HARBOR',8,14),('MARKET',34,15),('SAT / SUN',73,12),('09 - 17',97,13)])
sticker('poster_tea',(232,80,96,128),[('TEA',8,24),('OPEN',48,18),('DAILY',81,14)])
sticker('flyer',(344,80,64,96),[('NOTICE',8,11),('LOCAL',36,12),('POST',58,14)])
sticker('number_07',(424,80,32,20),[('07',0,12)])
# Photo-derived masks, confined to corners/drips, never full-wall grunge.
raw=np.array(Image.open(R/(PH+'PaintedWood008C/PaintedWood008C_1K-JPG_Color.jpg')).convert('L').resize((128,128),Image.Resampling.BOX))
yy,xx=np.mgrid[:128,:128]
chip_source=P+'A/A2_apricot_facade_burano.jpg'
chip=np.array(Image.open(R/chip_source).convert('RGB').crop((340,780,640,900)).resize((128,128),Image.Resampling.BOX)).astype(int)
chipmask=((chip[:,:,0]-chip[:,:,1]<50)&(chip[:,:,1]>130)).astype('uint8')
import cv2
chipmask=cv2.medianBlur(chipmask,7).astype(bool)&((xx<40)|(yy<28))
for name,x,y,mask,col in [('edge_chip',8,240,chipmask,bases['frame']),('rain_trace',152,240,(xx>46)&(xx<76)&(yy>8)&(yy<112)&(raw<np.percentile(raw,55)),bases['skirting'])]:
 rgb=expand(paper);a=np.zeros((128,128,4),dtype='uint8');a[:,:,:3]=col;a[:,:,3]=mask.astype('uint8')*(235 if name=='edge_chip' else 40)
 decal.paste(Image.fromarray(a),(x,y));items.append(dict(name=name,rect_px=[x,y,128,128],size_m=[4,4],alpha_coverage=float(mask.mean())))
 if name=='edge_chip':items[-1].update(source=chip_source,source_crop_px=[340,780,640,900],mask_method='photographed exposed plaster chroma threshold, median7, corner only')
# Small legible sample label; sign bases remain reusable blank paint.
sticker('sample_tea',(8,392,64,24),[('TEA',-1,15)])
a=np.array(decal);mask=a[:,:,3]>0;a[mask,:3]=dl.quantize(a[mask,:3],pal);Image.fromarray(a).save(E/'decals.png')

manifest={'version':2,'px_per_m':32,'size':[512,512],'palette':'palette.gpl',
 'wall':wallcells,'floor':floorcells,'roof':roofcells,'trim':bands,'decals':items,
 'tiling':'Repeat inside named cell only; split mesh at cell boundaries. Trim U only; never repeat full multi-material atlas.',
 'filter':'Point, no mipmaps, no compression; half-texel edge guard',
 'surface':'albedo only; photographic grain; no lighting/AO bake'}
(E/'layout.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(W/'jobs.json').write_text(json.dumps(jobs,indent=2),encoding='utf-8')
(W/'material-records.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
# Persist an independent CLI example that must byte-match its imported-function result.
(W/'example-job.json').write_text(json.dumps(jobs['roof_F3'],indent=2),encoding='utf-8')
urls={}
for line in (R/'art/source/photo-picks/SOURCES.md').read_text(encoding='utf-8').splitlines():
 fields=line.split('|')
 if len(fields)>5 and '`' in fields[2]:
  f=fields[2].strip().strip('`');m=re.search(r'https?://\S+',fields[3])
  if m:urls[P+f]=m.group(0)
source_rows=[]
for path in sorted({r['input'] for r in records}):
 url=urls.get(path,'https://ambientcg.com/view?id='+Path(path).parent.name)
 source_rows.append(dict(path=path,source_url=url,license='CC0-1.0',sha256=dl.sha(R/path),accessed='2026-10-28',use='photo crop / perspective correction / delight / palette; no AI'))
with (E/'provenance.csv').open('w',encoding='utf-8',newline='') as f:
 wr=csv.DictWriter(f,fieldnames=list(source_rows[0]));wr.writeheader();wr.writerows(source_rows)
print('KIT BUILT',len(pal),'palette colors;',len(records),'photo jobs',flush=True)

# Scope: roof and decals stay byte-identical to the approved v1 candidate.
import shutil
for name in ['roof.png','decals.png']:
 shutil.copy2(R/'art/export/kit-shared-v1'/name,E/name)
old=json.loads((R/'art/source/kit-shared-v1/material-records.json').read_text())
records=[r for r in records if not r['job']['output'].split('/')[-1].startswith(('roof_','decal_'))]+[r for r in old if r['job']['output'].split('/')[-1].startswith(('roof_','decal_'))]
(W/'material-records.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
