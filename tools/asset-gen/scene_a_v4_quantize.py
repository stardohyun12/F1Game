from pathlib import Path
from PIL import Image,ImageFilter,ImageDraw
import numpy as np,json
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a_v4_baked';E=R/'art/export/scene-a-v4-baked'
meta=json.loads((O/'manifest.json').read_text());samples=[];processed={}
for m in meta:
 name=m['group'];im=Image.open(O/(name+'_baked_raw.png')).convert('RGB').filter(ImageFilter.GaussianBlur(.55));a=np.asarray(im);valid=a.reshape(-1,3);valid=valid[valid.max(1)>12];step=max(1,len(valid)//30000);samples.append(valid[::step][:30000]);processed[name]=im
pool=np.concatenate(samples);n=int(np.ceil(len(pool)/512));canvas=np.zeros((n*512,3),dtype=np.uint8);canvas[:len(pool)]=pool;canvas[len(pool):]=pool[0];palette=Image.fromarray(canvas.reshape(n,512,3)).quantize(colors=24,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE)
colors=palette.getpalette()[:72];pal=Image.new('P',(1,1));pal.putpalette(colors+[colors[i%72] for i in range(768-72)]);counts={}
for name,im in processed.items():
 q=im.quantize(palette=pal,dither=Image.Dither.NONE).convert('RGB');q.save(E/(name+'_baked24.png'));counts[name]=len(q.getcolors(100000))
swatch=Image.new('RGB',(384,64));d=ImageDraw.Draw(swatch)
for i in range(24):d.rectangle((i*16,0,i*16+15,63),fill=tuple(colors[i*3:i*3+3]))
swatch.save(E/'palette24.png');(E/'palette24.gpl').write_text('GIMP Palette\nName: Scene A V4 baked 24\nColumns: 8\n#\n'+'\n'.join('%d %d %d baked_%02d'%(*colors[i*3:i*3+3],i) for i in range(24)))
(E/'palette-check.json').write_text(json.dumps(dict(global_palette_colors=24,dither=False,texture_colors=counts),indent=2));print(counts)
