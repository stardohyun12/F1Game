"""Diagnostic copy of the sensor: add sample coordinates; do not change the actual fb_geom gate."""
import pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'scripts'));import fb_geom
d=fb_geom.find(sys.argv[1]);p=d['point']
code=(ROOT/'scripts/fb_geom.cs.txt').read_text(encoding='utf-8')
for key,value in {'__PX__':p[0],'__PY__':p[1],'__PZ__':p[2],'__RAD__':40,'__SCENE__':d['scene'],'__DRYNAME__':'','__DRYDY__':0}.items():code=code.replace(key,str(value))
needle='if (d < -0.02f) covered++;'
assert needle in code
code=code.replace(needle,'if (d < -0.02f) { covered++; sb.Append("BAD\\t" + mf.name + "\\t" + q.x.ToString("F5") + "\\t" + q.y.ToString("F5") + "\\t" + q.z.ToString("F5") + "\\t" + d.ToString("F5") + "\\n"); }')
result=fb_geom.ucli_exec('D:/Github/F1Game/F1',code)
print('\n'.join(line for line in result.splitlines() if line.startswith('BAD')))
