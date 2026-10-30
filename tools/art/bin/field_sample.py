from pathlib import Path
from PIL import Image
import numpy as np, json, colorsys
root=Path(__file__).resolve().parents[3]
review=root/'art/_review/2026-10-29-field_patch'
im=Image.open(root/'tools/viewer/feedback/shots/unity-Map_TownV6-20261029-231130-94a3.png').convert('RGB')
rois={'grass':[555,300,665,385],'grass_light':[600,455,720,520],'grass_shade':[650,220,720,265],'soil':[1110,231,1140,261]}
data={}
for name,box in rois.items():
    rgb=np.median(np.asarray(im.crop(box)).reshape(-1,3),axis=0).astype(int).tolist()
    data[name]={'box_xyxy':box,'rgb':rgb,'hex':'#'+''.join(f'{v:02x}' for v in rgb),'hsv':colorsys.rgb_to_hsv(*(v/255 for v in rgb))}
data['terrain_material']={'path':'F1/Assets/Scenes/Map_TownV6_Mats/TV6_Terrain.mat','tint':[.62,.66,.48],'fogStart':90,'fogEnd':950}
data['house_palette']='art/export/house-home/palette.gpl'
data['applied_srgb_targets']={'grass':[145,144,94],'soil':[145,127,99],'ridge':[151,132,104],'crop_c137':[102,112,89],'crop_c168':[109,120,95],'wood_c135':[102,99,91],'stone_c399':[153,148,134]}
data['conversion_note']='Grass interpolates measured mid/light samples; soil reduces saturation; shader Tint compensates the existing A7 sun (1,.86,.68), without a second .linear conversion. Crop/wood/stone are exact house palette entries.'
(review/'color_samples.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
print(json.dumps(data,indent=2))
