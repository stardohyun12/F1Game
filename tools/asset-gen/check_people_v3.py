from pathlib import Path
R=Path(__file__).resolve().parents[2]
code=(R/'tools/asset-gen/check_people_fbx.py').read_text(encoding='utf8')
code=code.replace('bayloop_people_v01','bayloop_people_v03').replace('2026-10-26-terrain-people/people','2026-10-26-people-v3').replace('tris<=1500','tris<=1200').replace('1.6<=height<=1.7','1.2<=height<=1.4')
exec(compile(code,str(R/'tools/asset-gen/check_people_fbx.py'),'exec'))
