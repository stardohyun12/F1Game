from pathlib import Path
s=Path(__file__).with_name('export_render.py').read_text()
exec(s.split('for d in items:')[0])
exec(s.split('# Existing garage FBX uses unchanged atlas UVs; render three texture alternatives only.')[1])
