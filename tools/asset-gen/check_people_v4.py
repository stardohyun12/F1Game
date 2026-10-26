from pathlib import Path
R=Path(__file__).resolve().parents[2]
s=(R/'tools/asset-gen/check_people_fbx.py').read_text(encoding='utf8').replace('bayloop_people_v01','bayloop_people_v04').replace('2026-10-26-terrain-people/people','2026-10-26-people-v4').replace('tris<=1500','300<=tris<=800').replace('1.6<=height<=1.7','1.6<=height<=1.75')
s=s.replace("checks.update(tri_budget=", "checks['seventeen_bones']=len(rig.data.bones)==17\n    checks.update(tri_budget=")
exec(compile(s,str(R/'tools/asset-gen/check_people_fbx.py'),'exec'))
