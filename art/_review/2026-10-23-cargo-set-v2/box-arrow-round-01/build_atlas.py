"""Two density candidates, same physical parcel and material design.
Photo -> downsample -> shared palette -> weak ordered dither -> rule stamps
-> adaptive sparse blue-noise-like fill -> baked contact/fold AO. No AI image.
"""
from pathlib import Path
import sys,json,hashlib,csv,subprocess,argparse,math
import numpy as np
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parent;repo=root.parents[2];sys.path.insert(0,str(repo/'tools/asset-gen'))
from photo_to_pixel import load_palette
p=argparse.ArgumentParser();p.add_argument('--density',type=int,choices=[64],default=64);p.add_argument('--seed',type=int,default=0);args=p.parse_args();D=args.density
R=root;(R/'patches').mkdir(parents=True,exist_ok=True)
palette=repo/'art/source/palette/pitcrew_d123_32.json';meta=json.loads(palette.read_text());colors=load_palette(palette);c={e['name']:tuple(e['rgb']) for e in meta['entries']}
photo=repo/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png'
if D==64:
 specs=[('front',.4,.3,[1,1,26,19]),('back',.4,.3,[29,1,26,19]),('top',.4,.3,[1,22,26,19]),('bottom',.4,.3,[29,22,26,19]),('left',.3,.3,[1,43,19,19]),('right',.3,.3,[22,43,19,19])]
else:
 specs=[('front',.4,.3,[1,1,13,10]),('back',.4,.3,[16,1,13,10]),('top',.4,.3,[1,13,13,10]),('bottom',.4,.3,[16,13,13,10]),('left',.3,.3,[1,25,10,10]),('right',.3,.3,[13,25,10,10])]
ramps={'top':['scene_16','carton_light','scene_00'],'front':['paper_fibre','paper_flat','scene_16'],'back':['paper_fold','paper_fibre','paper_flat'],'right':['paper_fold','carton_mid','paper_fibre'],'left':['paper_fold','carton_mid','paper_fibre'],'bottom':['carton_dark','tape','paper_fold']}
bayer=np.array([[0,8,2,10],[12,4,14,6],[3,11,1,9],[15,7,13,5]],dtype=float)/16-.5
atlas=Image.new('RGB',(64,64),c['carton_mid']);records=[];stage_metrics={}
def luma(a):return np.asarray(a).astype(float)@np.array([.2126,.7152,.0722])
def no_flat(im,ramp):
 # A deterministic farthest-point placement only in otherwise-flat 8px windows.
 # This is sparse blue-noise-like distribution, not a claim to reproduce Pixel8r.
 a=np.asarray(im).copy();height,width=a.shape[:2];placed=[]
 while True:
  bad=[(x,y) for y in range(height-7) for x in range(width-7) if len(np.unique(a[y:y+8,x:x+8].reshape(-1,3),axis=0))<2]
  if not bad:break
  x,y=bad[0];options=[(xx,yy) for yy in range(y+1,y+7) for xx in range(x+1,x+7)]
  q=max(options,key=lambda t:min([(t[0]-s[0])**2+(t[1]-s[1])**2 for s in placed] or [-(t[0]-x-4)**2-(t[1]-y-4)**2]))
  current=tuple(a[q[1],q[0]]);alternatives=[v for v in ramp if v!=current];new=min(alternatives,key=lambda v:sum((int(v[k])-int(current[k]))**2 for k in range(3)))
  a[q[1],q[0]]=new;placed.append(q)
  if len(placed)>width*height:raise RuntimeError('flat fill did not converge')
 return Image.fromarray(a),placed
for face,w,h,rect in specs:
 tw,th=rect[2:];patch=R/'patches'/f'{face}_01_quantized.png'
 dx=(args.seed*7)%11
 subprocess.run([sys.executable,str(repo/'tools/asset-gen/photo_to_pixel.py'),str(photo),str(patch),'--palette',str(palette),'--width-m',str(w),'--height-m',str(h),'--density',str(D),'--dither','none','--resample','box','--source-id','ambientCG/CardboardSet001','--source-url','https://ambientcg.com/view?id=CardboardSet001','--license-url','https://docs.ambientcg.com/license/','--crop',str(55+dx),'145',str(220+dx),'235'],check=True,stdout=subprocess.DEVNULL)
 quant=Image.open(patch).convert('RGB');raw=Image.open(photo).convert('RGB').crop((55+dx,145,220+dx,235)).resize((tw,th),Image.Resampling.BOX)
 raw.save(R/'patches'/f'{face}_00_downscaled.png')
 # Quantized layer drives the material; photo residual only controls weak dithering.
 lum=luma(quant);variation=np.clip((lum-lum.mean())/max(float(lum.std()),1),-2,2)*.18
 ramp=[c[n] for n in ramps[face]];indices=np.clip(np.rint(1+variation+np.tile(bayer,(math.ceil(th/4),math.ceil(tw/4)))[:th,:tw]*.3),0,2).astype(int)
 base=Image.fromarray(np.array(ramp,dtype=np.uint8)[indices]);base.save(R/'patches'/f'{face}_02_ladder_bayer.png')
 im=base.copy();draw=ImageDraw.Draw(im)
 # Tape at x=200mm, slightly diagonal, warmer separate material from cardboard.
 if face in ['top','front','back','bottom']:
  for yy in range(th):
   if face in ['front','back'] and yy>round(.03*D):continue
   center=.2*D+(.025*(.15-yy/D)*D if face=='top' else (-.00375*D if face=='front' else .00375*D))
   a=int(round(center-.026*D));b=int(round(center+.026*D))
   draw.line((a,yy,b,yy),fill=c['scene_16'])
   if yy%4==0:draw.point((a,yy),fill=c['paper_flat'])
  if face=='top':
   # Interior crease shadow on each side of the tape, deliberate material-layer AO.
   for xx in [int(.17*D),int(.23*D)]:draw.line((xx,0,xx,th-1),fill=c['paper_fold'])
 # Small real-proportion label: 105mm wide x 148mm tall, never enlarged to fit text.
 label_rect=None
 if face=='front':
  x,y=round(.065*D),round(.105*D);lw,lh=round(.105*D),round(.148*D)
  label_rect=[x,y,lw,lh];draw.rectangle((x,y,x+lw-1,y+lh-1),fill=c['label'])
  if D==64:
   for yy,ln in [(1,4),(3,3),(4,4)]:draw.line((x+1,y+yy,x+ln,y+yy),fill=c['paper_fold'])
   for xx in [1,3,4,5]:draw.line((x+xx,y+6,x+xx,y+7),fill=c['ink'])
  else:
   draw.line((x,y+1,x+1,y+1),fill=c['paper_fold']);draw.point((x,y+3),fill=c['ink']);draw.point((x+2,y+3),fill=c['ink']);draw.line((x,y+4,x+2,y+4),fill=c['paper_fold'])
 if face=='right':
  # A single separated triangular shipping arrow, not a repeated cross glyph.
  for x in [1,10]:
   y=7
   draw.polygon([(x+3,y),(x,y+3),(x+6,y+3)],fill=c['ink'])
   draw.line((x+3,y+4,x+3,y+5),fill=c['ink'])
 if face=='left':
  x,y=round(.18*D),round(.16*D)
  if D==64:
   draw.line((x,y,x+4,y),fill=c['stamp']);draw.line((x,y,x+1,y+3),fill=c['stamp']);draw.line((x+4,y,x+3,y+3),fill=c['stamp']);draw.line((x+1,y+3,x+3,y+3),fill=c['stamp']);draw.line((x+2,y+3,x+2,y+6),fill=c['stamp']);draw.line((x,y+7,x+4,y+7),fill=c['stamp'])
  else:draw.line((x,y,x,y+1),fill=c['stamp']);draw.line((x+2,y,x+2,y+1),fill=c['stamp']);draw.point((x+1,y+2),fill=c['stamp']);draw.line((x,y+3,x+2,y+3),fill=c['stamp'])
 # Scrape stamps are restricted to handling/corner positions, not scatter wear.
 if face in ['front','left']:
  draw.line((0,1,0,min(3,th-1)),fill=c['carton_light']);draw.point((1,1),fill=c['carton_light'])
 im.save(R/'patches'/f'{face}_03_stamps.png')
 im,noise=no_flat(im,ramp);im.save(R/'patches'/f'{face}_04_sparse_fill.png');draw=ImageDraw.Draw(im)
 # Quantized AO band at bottom contact and the top fold. No vertex-color shader dependency.
 if face not in ['top','bottom']:
  for xx in range(tw):
   draw.point((xx,th-1),fill=c['carton_dark'] if xx%3 else c['tape'])
  for xx in range(tw):
   if abs(xx-.2*D)>.03*D:draw.point((xx,0),fill=c['paper_fold'] if face in ['front','back'] else c['tape'])
 im,extra=no_flat(im,ramp);im.save(R/'patches'/f'{face}_05_final.png')
 x,y,_,_=rect
 for yy in range(-1,th+1):
  for xx in range(-1,tw+1):atlas.putpixel((x+xx,y+yy),im.getpixel((max(0,min(tw-1,xx)),max(0,min(th-1,yy)))))
 records.append({'face':face,'rect_xywh':rect,'physical_wh_m':[w,h],'label_rect_xywh':label_rect,'source_metadata':f'patches/{face}_01_quantized.json','sparse_fill_pixels':noise+extra})
 stage_metrics[face]={'base_mean_luma':float(luma(base).mean()),'final_mean_luma':float(luma(im).mean()),'final_unique_colors':len(im.getcolors()),'sparse_fill_count':len(noise)+len(extra)}
atlas.save(R/'cargo_box_atlas_64.png')
manifest={'name':f'cargo_box_v03_{D}','density_target':D,'texture_size':[64,64],'faces':records,'seed':args.seed,'label_physical_wh_m':[.105,.148],'label_width_fraction':.2625,'color_ladder_metrics':stage_metrics,'layer_order':['CC0 photo','BOX downscale','32-color exact quantization','weak 4x4 Bayer with face color ladder','rule-based stamps','adaptive sparse blue-noise-like fill','quantized fold/contact AO'],'noise_scope':'only flat 8x8 windows; minimum inserted adjacent-palette pixel; not randomized global wear','vertex_color_AO':False,'AO_reason':'baked palette AO; URP Lit vertex color support unverified','source_photo_sha256':hashlib.sha256(photo.read_bytes()).hexdigest(),'palette_sha256':hashlib.sha256(palette.read_bytes()).hexdigest(),'approval':'64px/m is review-only pending Dohyun exception;32 retains baseline'}
manifest['name']='cargo_box_final_v2';manifest['approval']='D-124 cargo density64 approved; final preview/import approval still separate';manifest['final_fixes']=['0.5mm tape; returns texture-only','black paired triangular UP arrows','reduced photo contrast and adjacent palette fibres']
(R/'atlas_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
ledger=repo/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
aid='cargo_box_final_v2_atlas';rows=[r for r in rows if r['asset_id']!=aid]
rows.append(dict(zip(fields,[aid,'art/models/cargo_box_final_v2/cargo_box_atlas_64.png','https://ambientcg.com/view?id=CardboardSet001','CC0-1.0','2026-10-23','photo_to_pixel.py; cargo_box_final_v2/build_atlas.py','no',hashlib.sha256((R/'cargo_box_atlas_64.png').read_bytes()).hexdigest(),'D124 64px/m; CC0 cardboard + subtle palette fibre; thin tape; black paired UP arrows. No generated texture.'])))
with ledger.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print(D,'px/m atlas done',stage_metrics)
