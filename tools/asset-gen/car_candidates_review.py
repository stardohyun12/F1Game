"""Direct composition of actual renders and untouched gameplay capture, no inpainting."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-car-candidates'
source=ROOT/'docs/qa/evidence/bayloop-v5b/07_shop_front_eye.png'
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
baseline=Image.open(source).convert('RGBA')
for k in ['A','C']:
 assert (R/f'{k}_overlay.png').stat().st_mtime >= (ROOT/f'art/models/car_candidates_v01/{k}.glb').stat().st_mtime, f'{k} overlay render not finished'
for k in ['A','C']:
 im=baseline.copy();overlay=Image.open(R/f'{k}_overlay.png').convert('RGBA')
 # The capture's existing vehicle remains visible on the left. Same new-car
 # transform in both panels, in the empty middle roadway. Not a Unity capture.
 overlay=overlay.resize((265,149),Image.Resampling.BOX).resize((530,298),Image.Resampling.NEAREST)
 im.alpha_composite(overlay,(267,176))
 d=ImageDraw.Draw(im);d.rectangle((0,0,960,38),fill=(35,39,38,255));d.text((16,8),f'{k} · 실제 마을 캡처 + Blender 합성 / 왼쪽 기존 차 유지 / 인게임 미검증',font=font,fill='white')
 im.convert('RGB').save(R/f'{k}_village-960.png')
sheet=Image.new('RGB',(1920,540));sheet.paste(Image.open(R/'A_village-960.png'),(0,0));sheet.paste(Image.open(R/'C_village-960.png'),(960,0));sheet.save(R/'village-compare.png')
grid=Image.new('RGB',(2880,4*580),(227,224,215));d=ImageDraw.Draw(grid)
for col,k in enumerate(['current','A','C']):
 for row,view in enumerate(['front','side','threequarter','top']):
  d.text((col*960+16,row*580+10),f'{k} / {view}',font=font,fill=(30,30,30));grid.paste(Image.open(R/f'{k}_{view}.png').convert('RGB'),(col*960,row*580+40))
grid.save(R/'views-compare.png');grid.resize((960,773),Image.Resampling.LANCZOS).save(R/'views-compare-960.png')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
(R/'composition.json').write_text(json.dumps(dict(source=source.relative_to(ROOT).as_posix(),source_sha256=sha(source),ai_generated=False,method='Unaltered gameplay capture + actual Cycles render and shadow catcher; no erasing or reconstruction',overlay_size=[530,298],overlay_xy=[267,176],limitations='Manual screen-space placement, approximate perspective/light. Existing left car remains. No claim of Unity camera, correct physics contact or ingame material match.'),indent=2),encoding='utf8')
print('review images ready')
