import json, sys, collections
d = json.load(open('docs/map/parcels-home.json', encoding='utf-8'))
print(d['grid'])
print(json.dumps(d['stats'], ensure_ascii=False))
print(json.dumps(d['check'], ensure_ascii=False))
e = [p for p in d['parcels'] if p['mass'] is None]
fit = [p for p in e if p['empty'].startswith('덩어리')]
def b(xs, edges):
    c = [0] * (len(edges) + 1)
    for x in xs:
        i = 0
        while i < len(edges) and x >= edges[i]: i += 1
        c[i] += 1
    return c
print('fail fitW bins <1,1-2,2-3,3-4,4-5,5+ :', b([p['fitW'] for p in fit], [1, 2, 3, 4, 5]))
print('fail depth bins <3,3-4.5,4.5-6,6-9,9+ :', b([p['depth'] for p in fit], [3, 4.5, 6, 9]))
print('fail frontW bins <5,5-7,7-9,9+ :', b([p['frontW'] for p in fit], [5, 7, 9]))
print('fail area bins <30,30-60,60-120,120+ :', b([p['areaM2'] for p in fit], [30, 60, 120]))
print('fail road:', collections.Counter(p['road'] for p in fit))
