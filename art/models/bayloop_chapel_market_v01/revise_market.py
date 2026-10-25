"""CC0-photo atlas reuse. Never changes geometry, frozen chapel or iterations."""
from pathlib import Path
import json
from PIL import Image
import numpy as np
out=Path(__file__).resolve().parent
root=out.parents[2]
a=np.asarray(Image.open(out/'ChapelMarketPhotoAtlas.png').convert('RGB')).copy()
pal=np.array(json.load(open(root/'art/source/palette/pitcrew_d123_32.json'))['colors'],dtype=np.uint8)
patch=a[256:512,512:768];lum=patch@np.array([.2126,.7152,.0722])
a[768:1024,768:1024]=np.where((lum<np.median(lum))[:,:,None],pal[11],pal[7])
patch=a[:256,:256];lum=patch@np.array([.2126,.7152,.0722]);a[:256,:256]=np.where((lum<np.median(lum))[:,:,None],pal[14],pal[19])
metal=a[256:512,512:768];lum=metal@np.array([.2126,.7152,.0722]);a[768:1024,512:768]=np.where((lum<np.median(lum))[:,:,None],pal[18],pal[19])
Image.fromarray(a).save(out/'FishMarketPhotoAtlas.png')
