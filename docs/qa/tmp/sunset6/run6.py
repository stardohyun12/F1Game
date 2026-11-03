# 사용: python docs/qa/tmp/sunset6/run6.py name:REFL:SPEC:MICRO:HZ:DASH[:FLK] ...  → name_A / name_B 두 장씩(+시간·깜빡임) m6.jsonl
#  HZ = 0(5판 300/650/0.5) | 1(150/650/0.8) | "a|b|c|d"(직접)
import subprocess, sys
src0 = open('docs/qa/tmp/sunset6/m6.cs', encoding='utf-8').read()
T = '20.0'
for a in sys.argv[1:]:
    f = a.split(':')
    name, refl, spec, micro, hz, dash = f[:6]
    flk = f[6] if len(f) > 6 else '1'
    hzv = '150f, 650f, 0.8f, 0f'
    if '|' in hz:
        hzv = ', '.join(x + 'f' for x in hz.split('|')); hz = '1'
    for st, sn in (('0', 'A'), ('1', 'B')):
        kv = dict(TAG='%s_%s' % (name, sn), STYLE=st, REFL=refl, SPEC=spec, MICRO=micro, HZ=hz, HZV=hzv, DASH=dash, FLK=flk, T=T, PITCH='3', YAW='180')
        src = src0
        for k, v in kv.items():
            src = src.replace('__%s__' % k, v)
        r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', src], capture_output=True, text=True, encoding='utf-8')
        print(kv['TAG'], (r.stdout.strip() + ' ' + r.stderr.strip())[-400:], flush=True)
