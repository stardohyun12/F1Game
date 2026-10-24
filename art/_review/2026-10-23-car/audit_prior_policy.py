"""Re-evaluate a current car export under the frozen original strict policy.

Evidence only. The source snapshot is the exact checker before policy revision;
the virtual __file__ preserves its original repository path resolution.
"""
import argparse
import json
import sys
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'tools/asset-gen'))
p = argparse.ArgumentParser()
p.add_argument('--out', type=Path, default=HERE/'check_car_r02_policy_v1.json')
arg = p.parse_args()
source = HERE/'check_car_policy_v1.py.snapshot'
namespace = {'__file__': str(ROOT/'tools/asset-gen/check_car.py'), '__name__': 'frozen_car_policy_v1'}
exec(compile(source.read_text(encoding='utf-8'), str(source), 'exec'), namespace)
args = SimpleNamespace(model=ROOT/'art/models/car_v01/car_v01.glb', texture=ROOT/'art/models/car_v01/CarAtlas.png',
                       palette=ROOT/'art/source/palette/pitcrew_d123_32.json', provenance=ROOT/'art/provenance.csv',
                       position_tolerance=.0001, wheel_center_tolerance=.001, min_flat_area=.04)
result = namespace['inspect'](args)
result['audit_policy'] = 'frozen-v1-any-monochrome-8x8-fails'
result['frozen_checker_sha256'] = namespace['sha256'](source)
arg.out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: result[k] for k in ['pass', 'checks', 'model_sha256', 'texture_sha256']}, ensure_ascii=False))
