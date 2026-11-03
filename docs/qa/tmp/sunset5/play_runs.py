# Play 앉은 상태에서: (태그, _Style, 격자) 마다 pcap.cs 를 보내고 8초 측정이 끝날 때까지 기다림
import subprocess, time, sys
RUNS = [tuple(int(x) if x.isdigit() else x for x in a.split(':')) for a in sys.argv[1:]] or [('seated_B1a', 1, 1), ('seated_B1b', 1, 1), ('seated_B1c', 1, 1), ('seated_B0', 1, 0),
        ('seated_A1a', 0, 1), ('seated_A1b', 0, 1), ('seated_A1c', 0, 1), ('seated_A0', 0, 0)]
src0 = open('docs/qa/tmp/sunset5/pcap.cs', encoding='utf-8').read()
for tag, st, gr in RUNS:
    src = src0.replace('__TAG__', tag).replace('__STYLE__', str(st)).replace('__GRID__', str(gr))
    r = subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', src], capture_output=True, text=True, encoding='utf-8')
    print(tag, r.stdout.strip()[-300:], r.stderr.strip()[-300:], flush=True)
    time.sleep(11)
