"""0131 terrain: directly downloaded CC0 diffuse -> 32px/m shared palette."""
import argparse,csv,hashlib,json,sys
from pathlib import Path
from types import SimpleNamespace
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from photo_to_pixel import convert,load_palette,quantize_exact
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/textures/2026-10-26-bayloop-terrain'
REVIEW=ROOT/'art/_review/2026-10-26-terrain-people/terrain'
PALETTE=ROOT/'art/source/palette/pitcrew_d123_32.json'
SPECS=[('grass','sparse_grass'),('soil','dirt'),('dry_grass','withered_grass'),('rock','rock_face_04')]
def rel(p):return p.relative_to(ROOT).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def run():
 for d in ['albedo','baseline','proof']: (OUT/d).mkdir(parents=True,exist_ok=True)
 tiles=[];palette=load_palette(PALETTE)
 for name,id in SPECS:
  src=OUT/'sources'/f'{id}_diff_1k.png';info=json.loads((OUT/'sources'/f'{id}_metadata.json').read_text())
  baseline=OUT/'baseline'/f'{name}.png';out=OUT/'albedo'/f'BL_Terrain_{name}_32ppm.png'
  url='https://polyhaven.com/a/'+id
  convert(SimpleNamespace(input=src,output=baseline,palette=PALETTE,width_m=2.,height_m=2.,density=32.,source_id=id,source_url=url,license_url='https://polyhaven.com/license',crop=None,canvas_size=None,metadata=None,resample='box',dither='none'))
  im=Image.open(baseline).convert('RGB');im.save(out)
  md5=hashlib.md5(src.read_bytes()).hexdigest()
  assert md5==info['download']['md5'],id
  source={'id':id,'path':rel(src),'url':url,'download_url':info['download']['url'],'license':'CC0-1.0','license_url':'https://polyhaven.com/license','authors':info['info'].get('authors'), 'dimensions_mm':info['info']['dimensions'],'method':'photography / photoscan; provider FAQ','method_evidence_url':'https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning','sha256':sha(src),'md5':md5,'provider_md5_match':True,'accessed':'2026-10-26','ai_generated':False}
  tiles.append({'id':name,'albedo':rel(out),'model':rel(OUT/'proof'/f'{name}_metric.glb'),'physical_size_m':[2,2],'density_px_m':32,'source':source,'process':['Direct official CC0 diffuse download; 2m physical scan; provider MD5 verified','photo_to_pixel.py full source BOX to 64x64 at32px/m; exact shared32 palette quantization; dither none'],'ai_generated':False})
 m={'schema_version':1,'density_policy':'world32','palette':rel(PALETTE),'tiles':tiles};(OUT/'manifest.json').write_text(json.dumps(m,indent=2),encoding='utf8')
 atlas=Image.new('RGB',(128,128))
 for i,t in enumerate(tiles):atlas.paste(Image.open(ROOT/t['albedo']),(i%2*64,i//2*64))
 atlas.save(OUT/'albedo'/'BL_TerrainAtlas_32ppm.png')
 sheet=Image.new('RGB',(1024,810),(225,221,210));d=ImageDraw.Draw(sheet)
 for i,t in enumerate(tiles):
  x=i*256;im=Image.open(ROOT/t['albedo']);src=Image.open(ROOT/t['source']['path']).convert('RGB')
  d.text((x+8,8),t['id']+' / 2m / 64px',fill=(35,31,27));sheet.paste(src.resize((256,256)),(x,32));sheet.paste(im.resize((256,256),Image.Resampling.NEAREST),(x,308))
  repeat=Image.new('RGB',(128,128))
  for y in range(2):
   for xx in range(2):repeat.paste(im,(xx*64,y*64))
  sheet.paste(repeat.resize((224,224),Image.Resampling.NEAREST),(x+16,580))
 sheet.save(REVIEW/'texture-contact.png')
def provenance():
 m=json.loads((OUT/'manifest.json').read_text());rows=[]
 for t in m['tiles']:
  for role,p in [('source',t['source']['path']),('albedo',t['albedo']),('proof',t['model'])]:
   p=ROOT/p
   rows.append({'asset_id':'bayloop_terrain_'+t['id']+'_'+role,'path':rel(p),'source_url':t['source']['url'],'license':'CC0-1.0','accessed':'2026-10-26','tool':'bayloop_terrain_textures.py; photo_to_pixel.py; Blender bpy','ai_generated':'no','sha256':sha(p),'notes':'; '.join(t['process'])+'; Original albedo palette only; no Unity test'})
 atlas=OUT/'albedo'/'BL_TerrainAtlas_32ppm.png'
 rows.append({'asset_id':'bayloop_terrain_atlas','path':rel(atlas),'source_url':'; '.join(t['source']['url'] for t in m['tiles']),'license':'CC0-1.0','accessed':'2026-10-26','tool':'bayloop_terrain_textures.py; photo_to_pixel.py','ai_generated':'no','sha256':sha(atlas),'notes':'Four exact64px photo_to_pixel palette quantized tiles packed without rescale; original albedo only32color check; 32px/m at2m tile'})
 (OUT/'provenance-rows.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
 with (OUT/'provenance-test.csv').open('w',encoding='utf8',newline='') as f:
  w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
if __name__=='__main__':
 if '--provenance' in sys.argv:provenance()
 else:run()
