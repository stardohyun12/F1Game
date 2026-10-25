from pathlib import Path
import json,shutil,hashlib
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bskills-trial';O=ROOT/'art/models/bskills_trial_v01';OLD=ROOT/'art/models/bayloop_props2_v01';V=Path('D:/Github/_vendor/blender-skills')
IDS=['parked_car_01','parked_car_02','parked_car_03','three_wheeler_01','laundry_line_01'];scores=[6,6,5.5,5.5,6]
(R/'briefs').mkdir(parents=True,exist_ok=True);O.mkdir(parents=True,exist_ok=True);arc=R/'iterations/before';arc.mkdir(parents=True,exist_ok=True)
m=json.loads((OLD/'manifest.json').read_text(encoding='utf8'));assets=[a for a in m['assets'] if a['id'] in IDS]
for a in assets:
 for key in ['model','fbx','texture']:
  p=ROOT/a[key];shutil.copy2(p,arc/p.name)
 for ext in ['.blend','.geometry.json']:
  p=OLD/(a['id']+ext)
  if p.exists():shutil.copy2(p,arc/p.name)
 for view in ['front','side','top']:shutil.copy2(ROOT/'art/_review/2026-10-25-bayloop-props2'/(a['id']+'_'+view+'.png'),arc/(a['id']+'_'+view+'.png'))
(arc/'manifest.json').write_text(json.dumps(dict(m,assets=assets),ensure_ascii=False,indent=2),encoding='utf8')
for name in ['car-gate-r3.md','last-gate-r3.md','roads-final-gate.md']:shutil.copy2(ROOT/'art/_review/2026-10-25-bayloop-props2'/name,arc/name)
shutil.copy2(V/'LICENSE',O/'BLENDER-SKILLS-LICENSE.txt')
props=ROOT/'F1/Assets/Art/BayLoop/Props';(R/'existing-files-before.json').write_text(json.dumps({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in props.rglob('*') if p.is_file()},indent=2))
names=['낮은 둥근 유럽 3도어 해치백','긴 쐐기형 유럽 해치백','좁고 높은 일본 경차','연결 구조가 읽히는 삼륜 화물차','바람에 약하게 처진 창 사이 빨래']
fixes=['두꺼운 지붕 띠·정체불명 지붕판 제거, 얇은 A/B/C기둥과 도어선','범퍼/그릴/차체 명도 단계, 객실 전후 오버행','측창 분할·덜 무거운 지붕·실물 바퀴 비율','적재함-크로스멤버-차대·발판 장착부를 실제 접촉','목/소매 판 같은 꺾임 제거, 큰 부드러운 처짐, 양말 반복 제거']
for name,desc,fix,score in zip(IDS,names,fixes,scores):
 a=next(x for x in assets if x['id']==name);vehicle=name!='laundry_line_01'
 s=f'''## Production Brief: {name}

**Type:** {'vehicle / background prop' if vehicle else 'everyday prop / laundry'} — {desc}
**Target:** Unity / PC game, static roadside dressing, 960×540 review
**Style:** D-123/D-137 실제 비례·바랜 해안 동네, 32px/m 사진 픽셀
**Poly Budget:** ≤1500 triangles (project overrides skill generic budgets)
**Texture Budget:** 공용 pitcrew_d123_32, ≤32 colors, 1024² shared photograph atlas, Point, 주요 면 단색0
**Export:** FBX + GLB + blend; 기존 이름 유지, scale1m, Y-up / front+Z
**Pivot:** whole-asset floor centre
**Animation:** none; vehicle body/glass/wheels/trim systems split for inspection; no rig

### Active Skills
1. blender-director — brief, primary→secondary→detail, same-camera comparison
2. {'vehicle-artist' if vehicle else 'prop-artist'} — {'wheelbase/cabin/overhang first, separate systems' if vehicle else 'real-world garment scale, hanging attachment and silhouette'}
3. uv-workflow — construction seams, metric32px/m on both axes (generic formula rejected)
4. materials + lowpoly-style — master atlas, material value hierarchy, no micro-greebles
5. qa-review + unity-export — screenshots, explicit reject/ship, headless FBX reimport; Unity handoff only

### Reference & Acceptance
- Previous score {score}/10; old PNG/model snapshots: ../iterations/before/ . Previous lighting/camera retained for scoring.
- Previous overall bounds (Unity xyz m): {a['dimensions_m']}. 차량은 wheelbase/track/radius를 SPEC에서 먼저 고정; 의도적 실루엣 수정만 허용.
- Primary fixes: {fix}.
- No brand, no AI image/model/background, no procedural random wear. Photograph source is existing CC0 Poly Haven metal/linen; original source metadata and ledger retained.
- Same independent protocol: fresh reviewer, no brief/previous score supplied, actual front/side images → yes/no + 3 defects + /10. ≥7 AND yes AND technical pass to trial Assets.
- Archive before edits; no Unity, commit, global skill installation.

### Pipeline Phases
- [x] Reference & Planning — previous failed renders/defect records; existing measured dimensions
- [ ] Blockout & Silhouette — scaled chassis/garment; neutral silhouette before small detail
- [ ] Modeling — {fix}
- [ ] Cleanup & Topology — measured budgets, separated functional systems, no floating parts
- [ ] UVs —32px/m ±10%, construction seams, intentional shared-atlas overlap documented
- [ ] Materials/Textures — protected faces quiet; wear only seams/contact
- [ ] Optimization — ≤1500, no subdiv/normal maps, shared opaque material
- [ ] Rig/Animate — N/A static
- [ ] Export & Validation — actual GLB/FBX check + independent PNG gate

### MCP Execution Plan — adapted to headless bpy
1. `blender.exe --background --python bskills_trial.py -- --ids {name}`; collection COL_{name}.
2. Query bpy mesh bounds/tris and garment/chassis connection data; save JSON.
3. Render blockout and final front/side/top with previous camera settings; no viewport/MCP.
4. Reuse check_props2/check_props2_fbx underlying check_asset policy; actual FBX reopen.
5. Gate-passed FBX + atlas only → F1/Assets/Art/BayLoop/Props; programmer handles Unity import.

### Conflicts / Project Overrides
- MCP/session operations → headless bpy; no global installation.
- Generic128–1024px/m, high vehicle budgets →32px/m,1500tris.
- Noise wear/PBR/transmission/weighted normal defaults →CC0 albedo, palette, opaque glass/flat shading; garment normals may be smooth if needed.
- Texture atlas overlap intentional; no lightmap UV/LOD/collision generation for this static visual trial, handed to programmer.
- Export skill requires Unity import test → not executed; QA/programming import required.
'''
 (R/'briefs'/(name+'.md')).write_text(s,encoding='utf8')
print('BRIEFS5 ARCHIVED5')
