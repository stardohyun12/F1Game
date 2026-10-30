import json, sys
d = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
for r in d['roads']:
    if r['id'] in sys.argv[1:]:
        print(r['id'], r['kind'], r['width'], ' '.join('(%.1f,%.1f,%.1f)' % tuple(q[:3]) for q in r['knots']))
