import json, collections
J = json.load(open('docs/map/bayloop-plan.json', encoding='utf-8'))
m = J['roads'][0]
for s in m['widthProfile']:
    xs = s['crossSection']
    print(s['name'], s['from'], s['to'], s['width'], xs['left']['edge'], xs['left']['walk'], xs['right']['edge'], xs['right']['walk'], s.get('why', '')[:60])
print(J.get('mart'))
print([k for k in J])
print(J['terrain']['beaches'])
