from pathlib import Path
import shutil,json
from PIL import Image
import numpy as np
out=Path('art/models/bayloop_chapel_market_v01');rev=Path('art/_review/2026-10-26-landmarks/chapel-market');it=out/'iterations/r2';it.mkdir(parents=True,exist_ok=True);rr=rev/'iterations/r2';rr.mkdir(parents=True,exist_ok=True)
for p in list(out.glob('FishMarketRoof.*'))+[out/'FishMarketPhotoAtlas.png']:shutil.copy2(p,it/p.name)
for p in rev.glob('FishMarketRoof_*.png'):shutil.copy2(p,rr/p.name)
a=np.array(Image.open(out/'FishMarketPhotoAtlas.png'));pal=np.array(json.load(open('art/source/palette/pitcrew_d123_32.json'))['colors'],dtype=np.uint8)
patch=a[:256,:256];lum=patch@np.array([.2126,.7152,.0722]);a[:256,:256]=np.where((lum<np.median(lum))[:,:,None],pal[14],pal[19])
metal=a[256:512,512:768];lum=metal@np.array([.2126,.7152,.0722]);a[768:1024,512:768]=np.where((lum<np.median(lum))[:,:,None],pal[18],pal[19]);Image.fromarray(a).save(out/'FishMarketPhotoAtlas.png')
p=Path('tools/asset-gen/bayloop_chapel_market.py');s=p.read_text();s=s.replace("for xx in [-4.5,-3,-1.5,0,1.5,3,4.5]:", "for xx in [k*.375 for k in range(-15,16)]:")
s=s.replace(".026,'metal');beam((xx,4,3.76),(xx,8.3,3.21),.026,'metal')", ".04,'metal');beam((xx,4,3.76),(xx,8.3,3.21),.04,'metal')")
s=s.replace("for k in range(6):\n    a=k*math.tau/6;c=(k+1)*math.tau/6", "for k in range(8):\n    a=k*math.tau/8;c=(k+1)*math.tau/8")
s=s.replace("z+r*math.sin(a)","z+r*.38*math.sin(a)").replace("z+r*math.sin(c)","z+r*.38*math.sin(c)").replace("z+r2*math.sin(a)","z+r2*.38*math.sin(a)").replace("z+r2*math.sin(c)","z+r2*.38*math.sin(c)")
s=s.replace("'plaster' if k>=3 else 'glass'", "'plaster' if k>=4 else 'rust'")
s=s.replace("y-.18,z+.05", "y-.12,z+.01").replace("y+.18,z+.05", "y+.12,z+.01").replace("y,z-.03", "y,z-.01")
s=s.replace("y,z+.14", "y,z+.055").replace("y,z+.29", "y,z+.105")
s=s.replace("(x+length*.62,y+.12,z+.01)],'glass')", "(x+length*.62,y+.12,z+.01)],'rust')").replace("(x+.05,y,z+.105)],'glass')", "(x+.05,y,z+.105)],'rust')")
s=s.replace("for dx,dy in [(-.65,-.03),(.55,.19)]:wholefish(cx+dx,.80+dy,.91,1.03)", "for idx,(dx,dy) in enumerate([(-.77,-.15),(.13,.10),(.78,-.10)]):\n   move(lambda:wholefish(0,0,.86,[.75,.65,.8][idx]),cx+dx,.80+dy,angle=[.2,-.35,2.9][idx])")
s=s.replace("box(xx,6.7,0,1.2,.8,.75,'plaster');box(xx,6.7,.75,1.3,.9,.07,'wood')\n  wholefish(xx,6.7,.94,.9)","box(xx,6.7,0,2.7,1.2,.75,'plaster');box(xx,6.7,.75,2.8,1.3,.07,'wood')\n  box(xx,6.7,.82,2.55,1.1,.05,'cloth')\n  for idx,dx in enumerate([-.8,0,.8]):move(lambda:wholefish(0,0,.94,.72),xx+dx,6.7,angle=[.1,3.3,-.3][idx])\n  box(xx,5.6,0,1.1,.6,.4,'teal');box(xx+.55,5.6,.4,1.1,.6,.4,'teal')")
p.write_text(s,encoding='utf8')
