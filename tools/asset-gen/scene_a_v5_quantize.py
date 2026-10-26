from pathlib import Path
from PIL import Image,ImageFilter,ImageDraw
import numpy as np,json
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a_v5_combo';E=R/'art/export/scene-a-v5-combo'
meta=json.loads((O/'manifest.json').read_text());samples=[];processed={}
for m in meta:
 name=m['group'];raw=Image.open(O/(name+'_baked_raw.png')).convert('RGB');ao=np.asarray(Image.open(O/(name+'_ao.png')).convert('L'),dtype=np.float32)/255;arr=np.asarray(raw,dtype=np.float32);factor=.70+.30*ao;arr=arr*factor[:,:,None];shade=(1-ao)*.16;arr[:,:,1]+=shade*24;arr[:,:,2]+=shade*28;srgb=np.clip(arr/255,0,1);lin=np.where(srgb<=.04045,srgb/12.92,((srgb+.055)/1.055)**2.4);lift=(1-srgb.max(2))**2;lin=lin*2.0+lift[:,:,None]*np.array([.009,.017,.019]);enc=np.where(lin<=.0031308,lin*12.92,1.055*np.maximum(lin,0)**(1/2.4)-.055);im=Image.fromarray(np.clip(enc*255,0,255).astype('uint8'));im.save(O/(name+'_baked_ao.png'));a=np.asarray(im);valid=a.reshape(-1,3);valid=valid[valid.max(1)>12];step=max(1,len(valid)//30000);samples.append(valid[::step][:30000]);processed[name]=im
pool=np.concatenate(samples)
n=int(np.ceil(len(pool)/512));canvas=np.zeros((n*512,3),dtype=np.uint8);canvas[:len(pool)]=pool;canvas[len(pool):]=pool[0];palette=Image.fromarray(canvas.reshape(n,512,3)).quantize(colors=32,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE)
colors=palette.getpalette()[:96]
# Freeze the delivered palette for deterministic subsequent exports; delete GPL to relearn.
frozen=E/'palette32.gpl'
if frozen.exists():
 rows=[line.split() for line in frozen.read_text().splitlines() if line and line[0].isdigit()]
 if len(rows)==32:colors=[int(v) for row in rows for v in row[:3]]
pal=Image.new('P',(1,1));pal.putpalette(colors+[colors[i%96] for i in range(768-96)]);counts={}
for name,im in processed.items():
 q=im.quantize(palette=pal,dither=Image.Dither.NONE).convert('RGB');q.save(E/(name+'_baked32.png'));counts[name]=len(q.getcolors(100000))
swatch=Image.new('RGB',(512,64));d=ImageDraw.Draw(swatch)
for i in range(32):d.rectangle((i*16,0,i*16+15,63),fill=tuple(colors[i*3:i*3+3]))
swatch.save(E/'palette32.png');(E/'palette32.gpl').write_text('GIMP Palette\nName: Scene A V5 combo 32\nColumns: 8\n#\n'+'\n'.join('%d %d %d baked_%02d'%(*colors[i*3:i*3+3],i) for i in range(32)))
(E/'palette-check.json').write_text(json.dumps(dict(global_palette_colors=32,dither=False,texture_colors=counts),indent=2));print(counts)
