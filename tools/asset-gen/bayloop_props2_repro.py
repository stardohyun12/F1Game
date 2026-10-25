"""Run actual Blender geometry twice and explicit seed variants."""
from pathlib import Path
import json,subprocess
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bayloop-props2'
exe='D:/Tools/Blender/blender.exe'
def run(tag,extra):
 args=[exe,'-b','-t','4','--python-exit-code','1','--python','tools/asset-gen/bayloop_props2.py','--','--group','all','--geometry-only']+extra
 with (R/(tag+'.log')).open('w',encoding='utf8') as f:subprocess.run(args,cwd=ROOT,stdout=f,stderr=subprocess.STDOUT,check=True)
 value=json.loads((R/'geometry-all.json').read_text());(R/(tag+'.json')).write_text(json.dumps(value,indent=2));return value
a=run('repro-a',[]);b=run('repro-b',[])
assert a==b and len(a)==19
c=run('seed7',['--ids','parked_car_01','street_sign_02','scooter_01','three_wheeler_01','--seed','7'])
d=run('seed8',['--ids','parked_car_01','street_sign_02','scooter_01','three_wheeler_01','--seed','8'])
assert [x['tris'] for x in c]==[x['tris'] for x in d]
assert all(x['geometry_sha256']!=y['geometry_sha256'] for x,y in zip(c,d))
(R/'repro-check.json').write_text(json.dumps(dict(pass_=True,repeat_count=19,byte_identical_geometry_and_parameters=a==b,seed_asset_count=len(c),seed7_tiles=[x['body_tile'] for x in c],seed8_tiles=[x['body_tile'] for x in d],seed_tris_identical=True,seed_uv_colour_changes=True,scope='Actual Blender geometry only; no production files replaced'),indent=2))
print('REPRO_PASS19 SEED_PASS4')
