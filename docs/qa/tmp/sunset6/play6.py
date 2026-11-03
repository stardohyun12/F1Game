# Play 앉은 상태에서: tag:STYLE:REFL:SPEC 마다 pcap6.cs 보내고 8초 측정 끝날 때까지 기다린 뒤 결과 요약
import subprocess, sys, time, json, glob, statistics
src0 = open('docs/qa/tmp/sunset6/pcap6.cs', encoding='utf-8').read()
D = 'docs/qa/evidence/sunset6/play/'
for a in sys.argv[1:]:
    tag, st, refl, spec = a.split(':')
    src = src0.replace('__TAG__', tag).replace('__STYLE__', st).replace('__REFL__', refl).replace('__SPEC__', spec)
    r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', src], capture_output=True, text=True, encoding='utf-8')
    print(tag, r.stdout.strip()[-200:], r.stderr.strip()[-200:], flush=True)
    time.sleep(12)
    try:
        j = json.load(open(D + tag + '_flicker.json', encoding='utf-8'))
        print('  ', {k: j[k] for k in j if not isinstance(j[k], list)}, flush=True)
    except Exception as e:
        print('  no json', e)
