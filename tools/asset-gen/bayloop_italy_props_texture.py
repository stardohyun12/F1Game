"""CC0 photographic material reuse, monochrome typography; no generated imagery."""
from pathlib import Path
import json,hashlib,urllib.request
import numpy as np
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_italy_props_v01';O.mkdir(parents=True,exist_ok=True)
P=json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors']
def get(url):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'PitCrewArt/1.0'}),timeout=60).read()
src=O/'leafy_grass_diff_1k.jpg'
if not src.exists():
 f=json.loads(get('https://api.polyhaven.com/files/leafy_grass'));info=json.loads(get('https://api.polyhaven.com/info/leafy_grass'));d=f['Diffuse']['1k']['jpg'];src.write_bytes(get(d['url']))
 (O/'leaf-source.json').write_text(json.dumps({'id':'leafy_grass','path':src.relative_to(R).as_posix(),'url':'https://polyhaven.com/a/leafy_grass','download_url':d['url'],'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'license':'CC0-1.0','dimensions_mm':info['dimensions']},indent=2))
a=np.asarray(Image.open(R/'art/models/bayloop_buildings_v01/BayLoopPhotoAtlas.png').convert('RGB')).copy()
# Metal close-value ladders. Reuse photographed patches rather than generate speckle.
for idx,mapping in [(0,{17:19,4:14}),(6,{9:5,5:8,8:7,7:18}),(7,{9:2,5:6,17:9,4:5}),(9,{4:19,0:14,25:14}),(10,{15:15,4:28,0:15,25:15}),(11,{2:26,26:27,5:26,18:27}),(14,{9:15,15:21,21:15,4:21})]:
 y=(idx//4)*256;x=(idx%4)*256;old=a[y:y+256,x:x+256].copy()
 for i,j in mapping.items():a[y:y+256,x:x+256][np.all(old==P[i],axis=2)]=P[j]
im=Image.fromarray(a);font=ImageFont.truetype(str(R/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),10)
# Four adjacent sign regions use real type; no pseudo-language marks.
base=im.crop((512,0,768,256));draw=ImageDraw.Draw(base);draw.fontmode='1'
for y,text in [(2,'수리'),(34,'세탁'),(66,'식료품'),(98,'차')]:draw.text((3,y),text,font=font,fill=tuple(P[14]))
im.paste(base,(256,768))
s=json.loads((O/'leaf-source.json').read_text());size=[round(v/1000*32) for v in s['dimensions_mm']];leaf=Image.open(src).convert('RGB').resize(size,Image.Resampling.BOX);lum=np.asarray(leaf)@np.array([.2126,.7152,.0722]);cuts=np.quantile(lum,[.25,.55,.85]);rgb=np.array([P[i] for i in [26,27,27,27]],dtype='uint8')[np.searchsorted(cuts,lum)];rgb=np.tile(rgb,(int(np.ceil(252/size[1])),int(np.ceil(252/size[0])),1))[:252,:252];im.paste(Image.fromarray(np.pad(rgb,((2,2),(2,2),(0,0)),mode='edge')),(0,768));im.save(O/'PropsPhotoAtlas.png')
print('PROPS_TEXTURE_DONE',size)

