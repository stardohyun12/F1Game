"""Read-only fingerprint of scene/shared materials/settings; writes review evidence only."""
from pathlib import Path
import hashlib,json,sys
root=Path(__file__).resolve().parents[3]
paths=[root/'F1/Assets/Scenes/Map_TownV6.unity',root/'docs/design/tutorial/placements.yaml']
for folder in ['F1/Assets/Scenes/Map_TownV6_Mats','F1/Assets/Art/Library','F1/Assets/Art/SceneA7','F1/ProjectSettings']:
    paths.extend(p for p in (root/folder).rglob('*') if p.is_file() and (p.suffix in ['.mat','.asset','.shader','.prefab'] or 'ProjectSettings' in p.parts))
data={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
out=root/'art/_review/2026-10-29-field_patch'
target=out/('map_hashes_'+sys.argv[1]+'.json')
target.write_text(json.dumps(data,indent=2),encoding='utf-8')
if sys.argv[1]=='after':
    before=json.loads((out/'map_hashes_before.json').read_text())
    changed=[k for k in set(before)|set(data) if before.get(k)!=data.get(k)]
    print(json.dumps({'files':len(data),'changed':changed}))
else:print('fingerprinted',len(data))
