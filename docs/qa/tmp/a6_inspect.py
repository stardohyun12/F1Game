# 과제 1617: h2 parts.json/kit.json 이 g 와 다른지, life 항목·몸체 치수 확인
import json
B = 'D:/Github/F1Game/art/export/'
h2 = json.load(open(B + 'scene-a4-houses-h2/parts.json', encoding='utf8'))
g = json.load(open(B + 'scene-a4-houses-g/parts.json', encoding='utf8'))
print('same parts as g:', h2 == g)
print(json.dumps(h2, ensure_ascii=False)[:2500])
k = json.load(open(B + 'scene-a4-houses-h2/kit.json', encoding='utf8'))
print('---kit'); print(json.dumps(k, ensure_ascii=False)[:1500])
