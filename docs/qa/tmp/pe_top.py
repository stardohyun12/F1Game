"""0604 진단: 판 경계 어긋남 상위. 사용: python -X utf8 docs/qa/tmp/pe_top.py v6h"""
import sys
sys.path.insert(0, 'tools/map')
import drive_check as DC, road_check as RC
tag = sys.argv[1] if len(sys.argv) > 1 else 'v6h'
d, rs = RC.load(tag)
pe = sorted(DC.plate_edges(d, rs), reverse=True)
for x in pe[:12]: print(round(x[0], 3), x[2], round(x[3], 1), round(x[4], 1))
byid = {r['id']: r for r in rs}
for j in d['junctions']:
    for a in j['arms']:
        r = byid.get(a['road'])
        if not r: continue
        e = a.get('edgeIndex', 0)
        if e >= len(r['pts']): print('OOR', j['id'], a['road'], e, len(r['pts'])); continue
        off = ((r['pts'][e][0] - a['edge'][0]) ** 2 + (r['pts'][e][1] - a['edge'][1]) ** 2) ** 0.5
        if off > 0.05: print('MOVED', j['id'], a['road'], e, len(r['pts']), round(off, 2), a['edge'][:2])
