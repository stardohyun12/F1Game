from pathlib import Path
from PIL import Image
import importlib.util,json
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-27-scene-a-v5-combo'
spec=importlib.util.spec_from_file_location('audit_lite',R/'docs/qa/tmp/audit_lite.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
for n in ['game','game_raw_bake']:
 Image.open(V/(n+'_720.png')).resize((1920,1080),Image.Resampling.NEAREST).save(V/(n+'_1080.png'))
Image.open(V/'game_1080.png').save(V/'sheet.png')
rows=[]
for name,path in [('V5 combo32','art/_review/2026-10-27-scene-a-v5-combo/game_1080.png'),('V5 prequantization','art/_review/2026-10-27-scene-a-v5-combo/game_raw_bake_1080.png'),('V4 baked24','art/_review/2026-10-27-scene-a-v4-baked/game_1080.png')]:
 row,_=audit.measure(name,path);rows.append(row);print(row,flush=True)
(V/'metrics.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
