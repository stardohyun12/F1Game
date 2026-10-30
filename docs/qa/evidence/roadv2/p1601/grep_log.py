import sys, re
pat = re.compile(sys.argv[1])
for f in sys.argv[2:]:
    print('===', f)
    s = open(f, encoding='utf-8', errors='replace').read().replace('\\n', '\n')
    for l in s.split('\n'):
        if pat.search(l): print(l[:300])
