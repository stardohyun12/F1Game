from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
root=Path('D:/Github/F1Game');out=root/'art/models/bayloop_chapel_market_v01'
p=np.array(json.load(open(root/'art/source/palette/pitcrew_d123_32.json'))['colors'],dtype=np.uint8)
a=np.array(Image.open(root/'art/models/bayloop_variety_v01/BayLoopShared_01.png').convert('RGB'))
src=Image.open(root/'art/textures/2026-10-24-kp-v2/original_reduced/plaster_photo_64ppm.png').convert('RGB').resize((64,64),Image.Resampling.BOX)
s=np.asarray(src); lum=s@np.array([.2126,.7152,.0722]);mask=lum<np.quantile(lum,.16);pat=np.where(mask[:,:,None],p[19],p[25]);pat=np.tile(pat,(4,4,1));a[0:256,0:256]=pat
Image.fromarray(a).save(out/'ChapelMarketPhotoAtlas.png')
