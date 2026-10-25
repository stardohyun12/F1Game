import hashlib, glob, json, sys
h = {p: hashlib.md5(open(p, 'rb').read()).hexdigest() for p in glob.glob('F1/Assets/**/*.mat', recursive=True)}
if sys.argv[1] == 'before':
    json.dump(h, open('docs/qa/tmp/op_mat_before.json', 'w')); print('mats', len(h))
else:
    b = json.load(open('docs/qa/tmp/op_mat_before.json'))
    ch = [p for p in h if b.get(p) != h[p]]
    print('changed', len(ch), ch[:30])
