"""D141 CC0 photo atlas. No generated images/no stochastic surface noise."""
from pathlib import Path
import json,hashlib,urllib.request
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01';R=ROOT/'art/_review/2026-10-25-bayloop-props2'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def fetch(url):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'PitCrewArt/1.0 CC0 texture study'}),timeout=45).read()
def main():
 O.mkdir(parents=True,exist_ok=True);R.mkdir(parents=True,exist_ok=True);S=O/'sources';S.mkdir(exist_ok=True)
 info=json.loads(fetch('https://api.polyhaven.com/info/asphalt_02'));files=json.loads(fetch('https://api.polyhaven.com/files/asphalt_02'))
 (S/'asphalt-info.json').write_text(json.dumps(info,indent=2));(S/'asphalt-files.json').write_text(json.dumps(files,indent=2))
 item=files.get('diff',files.get('Diffuse'))['1k']['jpg'];photo=S/'asphalt_02_diff_1k.jpg'
 if not photo.exists():photo.write_bytes(fetch(item['url']))
 source=dict(id='asphalt_02',path=photo.relative_to(ROOT).as_posix(),url='https://polyhaven.com/a/asphalt_02',download_url=item['url'],license='CC0-1.0',sha256=sha(photo),dimensions_mm=info['dimensions'])
 (O/'asphalt-source.json').write_text(json.dumps(source,indent=2))
 old=ROOT/'art/models/bayloop_buildings_v01';meta=json.loads((old/'texture-manifest.json').read_text(encoding='utf8'));pal=np.array(json.loads((ROOT/meta['palette']).read_text())['colors'],dtype=np.uint8)
 atlas=np.asarray(Image.open(ROOT/'art/models/bayloop_variety_v01/BayLoopShared_01.png').convert('RGB')).copy()
 metalpath=ROOT/'art/source/photos/course-signs/rusty_painted_metal_diff_1k.png';mi=json.loads((old/'metal-info.json').read_text())
 metal=Image.open(metalpath).convert('RGB').resize(tuple(round(x/1000*32) for x in mi['dimensions']),Image.Resampling.BOX)
 # Quiet intact paint between corrosion runs: crop in source pixels and retain
 # its real 2.2m/1024 physical scale. No invented grain or whole-body rust.
 rawmetal=Image.open(metalpath).convert('RGB');crop_rect=(340,260,650,620)
 bodyphoto=rawmetal.crop(crop_rect).resize((round(310/1024*2.2*32),round(360/1024*2.2*32)),Image.Resampling.BOX)
 asphalt=Image.open(photo).convert('RGB').resize(tuple(round(x/1000*32) for x in info['dimensions']),Image.Resampling.BOX)
 def colour(im,ladder):
  lum=np.asarray(im)@np.array([.2126,.7152,.0722]);cuts=np.quantile(lum,np.linspace(0,1,len(ladder)+1)[1:-1]);return pal[np.array(ladder)[np.searchsorted(cuts,lum)]]
 def tile(i,a):
  a=np.tile(a,(int(np.ceil(252/a.shape[0])),int(np.ceil(252/a.shape[1])),1))[:252,:252];x=i%4*256;y=i//4*256;atlas[y:y+256,x:x+256]=np.pad(a,((2,2),(2,2),(0,0)),mode='edge')
 # Close-value real paint tonal variation; rust remains a separate edge/hinge tile.
 specs=[(0,'body_cream',[19,14]),(3,'body_blue',[1,18]),(10,'body_ochre',[29,30]),(6,'metal',[7,8]),(7,'glass',[2,26]),(9,'trim',[19,14]),(11,'teal',[26,27])]
 for idx,name,ladder in specs:
  a=colour(bodyphoto if idx in (0,3,10,9,11) else metal,ladder);tile(idx,a)
  meta['tiles'][idx]=dict(id=name,index=idx,source_url='https://polyhaven.com/a/rusty_painted_metal',source_path=metalpath.relative_to(ROOT).as_posix(),physical_photo_repeat_m=[x/1000 for x in mi['dimensions']],palette_ladder=ladder)
  if idx in (0,3,10,9,11):meta['tiles'][idx].update(source_crop_xyxy=list(crop_rect),physical_photo_repeat_m=[310/1024*2.2,360/1024*2.2])
 tile(15,colour(asphalt,[2,9,5]));meta['tiles'][15]=dict(id='asphalt',index=15,source_url=source['url'],source_path=source['path'],physical_photo_repeat_m=[x/1000 for x in info['dimensions']],palette_ladder=[2,9,5])
 # Sign cell remains plain photographed wood, no inherited readable shop name.
 atlas[768:1024,256:512]=atlas[0:256,512:768]
 path=O/'Props2PhotoAtlas.png';Image.fromarray(atlas).save(path)
 aliases=dict(zip(['plaster','wood','wood_dark','roof','roof_clay','stone','metal','glass','cloth','trim','rose_wall','teal','stonewall','sign','rust','darkroof'],range(16)))
 aliases.update(body=0,asphalt=15,paint=9,rubber=15,wear=14,body_blue=3,body_ochre=10)
 meta.update(atlas=path.relative_to(ROOT).as_posix(),aliases=aliases,process='CC0 photographed metal 2.2m and asphalt provider scale BOX to32px/m; exact palette luminance ladders; existing CC0 wood/stone/cloth cells; no AI/no random noise; symbols actual geometry')
 (O/'texture-manifest.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf8')
 unique=set(map(tuple,np.unique(atlas.reshape(-1,3),axis=0)));assert unique<=set(map(tuple,pal))
 check=dict(pass_=True,size=[1024,1024],palette_colors=len(unique),outside_palette_colors=0,sha256=sha(path),albedo_only=True,photo_repeat_metal=mi['dimensions'],photo_repeat_asphalt=info['dimensions'])
 (O/'texture-checks.json').write_text(json.dumps(check,indent=2));print(check)
 Image.fromarray(atlas).resize((512,512),Image.Resampling.NEAREST).save(R/'atlas-proof.png')
if __name__=='__main__':main()
