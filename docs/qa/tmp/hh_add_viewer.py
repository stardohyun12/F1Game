import json
p = 'tools/viewer/buildings.json'; d = json.loads(open(p, encoding='utf-8').read())
if not any(e['name'] == 'house_home' for e in d):
    i = [e['name'] for e in d].index('room') + 1
    d.insert(i, {"name": "house_home", "label_ko": "주인공 2층집", "fbx": "art/export/house-home/house_home.fbx", "tex_dir": "art/export/house-home", "textures": {},
                 "default_texture": "house_home.png", "notes": "재질 HOUSE_alb 하나 → house_home.png. 2층 방(room)은 fbx 에 없음(Unity 프리팹 자식 +3.2m). 정면(발코니·현관) = Unity +Z."})
    open(p, 'w', encoding='utf-8').write(json.dumps(d, ensure_ascii=False, indent=2) + '\n')
print([e['name'] for e in d])
