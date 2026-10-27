from pathlib import Path
from PIL import Image,ImageFilter
import numpy as np,cv2,json
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;S=R/'art/source/photo-picks';E=R/'art/export/scene-a6-photo-kit';V=R/'art/_review/2026-10-27-photo-kit'
for p in [W/'crops',W/'flat',W/'source',E,V]:p.mkdir(parents=True,exist_ok=True)
pal=[list(map(int,l.split()[:3])) for l in (R/'art/export/scene-a-v5b-garage/palette32.gpl').read_text().splitlines() if l[:1].isdigit()]
pal+= [[90,48,36],[125,58,40],[155,72,45],[184,91,53],[192,130,58],[164,114,57],[133,96,54],[76,94,110],[91,122,142],[130,156,163],[57,57,57],[80,80,80],[108,108,108],[142,142,142],[177,177,177],[196,193,175],[181,176,151],[154,147,119]]
pal=np.unique(np.array(pal,dtype='uint8'),axis=0);(E/'palette-photo-kit.gpl').write_text('GIMP Palette\nName: garage32 + photo kit ramps\nColumns: 8\n#\n'+''.join(f'{r} {g} {b} c{i}\n' for i,(r,g,b) in enumerate(pal)),encoding='utf-8')
def quant(a):
 f=a.reshape(-1,3).astype(float);out=np.empty_like(f)
 for i in range(0,len(f),4096):out[i:i+4096]=pal[((f[i:i+4096,None]-pal)**2).sum(2).argmin(1)]
 return out.reshape(a.shape).astype('uint8')
def periodic(a,n=4):
 a=a.astype(float).copy();h,w=a.shape[:2];n=min(n,h//5,w//5)
 for k in range(n):
  f=(n-k)/n*.5;l=a[:,k].copy();r=a[:,-1-k].copy();a[:,k]=l*(1-f)+r*f;a[:,-1-k]=r*(1-f)+l*f
 for k in range(n):
  f=(n-k)/n*.5;l=a[k].copy();r=a[-1-k].copy();a[k]=l*(1-f)+r*f;a[-1-k]=r*(1-f)+l*f
 return np.rint(a).astype('uint8')
items=[
 dict(name='vending_G1',pick='G1',file='G/G1_postbox_vending.jpg',quad=[[896,74],[1381,158],[1321,1005],[870,1114]],size=[1.181,.731,1.83],front=[1.181,1.78],base=[177,174,153],kind='prop'),
 dict(name='vending_G2',pick='G2',file='G/G2_vending_orange.jpg',quad=[[174,120],[908,107],[913,1111],[281,1125]],size=[1.181,.731,1.83],front=[1.181,1.78],base=[183,124,45],kind='prop'),
 dict(name='postbox_G1',pick='G1',file='G/G1_postbox_vending.jpg',quad=[[312,554],[783,543],[781,963],[354,1064]],size=[.81,.56,1.38],front=[.81,.83],base=[155,65,45],kind='prop'),
 dict(name='ac_G3',pick='G3',file='G/G3_ac_outdoor_unit.jpg',quad=[[548,158],[1392,279],[1294,908],[589,724]],size=[.80,.285,.55],front=[.80,.50],base=[177,177,170],kind='prop'),
 dict(name='tin_F2',pick='F2',file='F/F2_box_profile_red.jpg',quad=[[0,0],[1599,0],[1599,1599],[0,1599]],tile_m=[2,2],kind='tile'),
 dict(name='tin_F3',pick='F3',file='F/F3_corrugated_shed.jpg',quad=[[692,425],[824,442],[823,677],[691,699]],tile_m=[1,2],kind='tile'),
 dict(name='stone_H1',pick='H1',file='H/H1_round_stone_wall.jpg',quad=[[50,35],[1550,35],[1550,1040],[50,1040]],tile_m=[2,1.5],kind='tile'),
 dict(name='stone_H2',pick='H2',file='H/H2_stone_wall_dordogne.jpg',quad=[[20,20],[1580,20],[1580,1030],[20,1030]],tile_m=[2,1.3125],kind='tile'),
 dict(name='stone_H3',pick='H3',file='H/H3_old_stone_wall.jpg',quad=[[0,0],[1599,0],[1599,1599],[0,1599]],tile_m=[2,2],kind='tile')]
for d in items:
 src=np.array(Image.open(S/d['file']).convert('RGB'));physical=d.get('front',d.get('tile_m'));ww,hh=[max(4,round(x*32)) for x in physical];size=(ww*8,hh*8);dst=np.array([[0,0],[size[0]-1,0],[size[0]-1,size[1]-1],[0,size[1]-1]],np.float32);M=cv2.getPerspectiveTransform(np.array(d['quad'],np.float32),dst);crop=cv2.warpPerspective(src,M,size,flags=cv2.INTER_CUBIC,borderMode=cv2.BORDER_REPLICATE);Image.fromarray(crop).save(W/'crops'/(d['name']+'.png'))
 # Broad illumination compensation, bounded gain; do not invent stain overlays.
 a=cv2.bilateralFilter(crop,7,20,9).astype(float);Y=a@np.array([.2126,.7152,.0722]);b=cv2.GaussianBlur(Y,(0,0),max(5,min(size)/9));gain=np.clip((np.median(b)/np.maximum(b,1))**.45,.75,1.30);a*=gain[:,:,None];Y=a@np.array([.2126,.7152,.0722]);p2,p98=np.percentile(Y,[2,98]);target=np.clip(58+(Y-p2)*132/max(1,p98-p2),52,195);a*=target[:,:,None]/np.maximum(Y[:,:,None],1)
 a=cv2.resize(np.clip(a,0,255).astype('uint8'),(ww,hh),interpolation=cv2.INTER_AREA)
 if d['kind']=='tile':a=periodic(a)
 Image.fromarray(a).save(W/'flat'/(d['name']+'.png'));q=quant(a);Image.fromarray(q).save(W/'source'/(d['name']+'.png'));d.update(source_pixels=[ww,hh],homography=M.tolist(),timestamp_excluded=d['pick'] in ['G1','G2'])
(W/'manifest.json').write_text(json.dumps(items,indent=2));print('PREPARED',len(items),'palette',len(pal))
