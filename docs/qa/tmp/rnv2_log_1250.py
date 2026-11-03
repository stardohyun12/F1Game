import re, sys
t = open('docs/qa/tmp/roadv2_full_1250.log', encoding='utf-8').read().splitlines()
want = set(sys.argv[1].split(',')) if len(sys.argv) > 1 else set()
for l in t:
    if l.startswith('물체 번호'):
        for it in l[len('물체 번호: '):].split(', '):
            k = it.split(':')[0]
            if k in want: print('obj', it)
diag = [l for l in t if '띠 안 진단' in l]
print('diag', len(diag)); [print(' ', l) for l in diag[:12]]
st = []
for l in t:
    m = re.match(r'\s+(.+?): (\d+)m 폭([\d.]+) 최소반경 ([\d.]+)m .*구배 ([\d.]+)% Δ² ([\d.]+)', l)
    if m: st.append((float(m.group(5)), m.group(1), m.group(2), m.group(4)))
st.sort(reverse=True)
print('grade top5'); [print(' ', s) for s in st[:5]]
print('minR<8', sorted([(float(s[3]), s[1]) for s in st if float(s[3]) < 8])[:6])
