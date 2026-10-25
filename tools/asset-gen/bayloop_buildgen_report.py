"""Write final D140 report from actual checks, gates and delivery ledger."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bayloop-variety';O=ROOT/'art/models/bayloop_variety_v01'
read=lambda p:json.loads(p.read_text(encoding='utf8'))
m=read(O/'manifest.json');g=read(R/'gate-summary.json');delivery=read(R/'trial-delivery.json');checks=read(R/'checks.json');fbx=read(R/'fbx-checks.json')
rev='../../../art/_review/2026-10-25-bayloop-variety/';src='../../../art/models/bayloop_variety_v01/'
lines=['# D-140 매개변수 건물 생성기 — 20261025-2023','',
f"**63개 제작, 55개 시험 투입, 8개 보류. 전체 톤 독립 판정 예·{g['tone']['score']}/10.** 기존 a/b/c 포함 102파일 해시 보존. Unity 실행·씬/코드/.meta 수정·커밋·push 없음.",'',
f'[960 격자]({rev}grid.png) · [고해상도 격자]({rev}grid-full.png) · [전체 검토판]({rev}index.html) · [실제 투입 장부]({rev}trial-delivery.json)','',
'## 유형별 결과','',
'| type | 제작/투입 | tris 범위 | 대표 독립 점수 | 결과·증거 |','|---|---:|---:|---|---|']
for typ,gate in g['types'].items():
 group=[a for a in m['assets'] if a['type']==typ];accepted=sum(a['id'] in delivery['accepted'] for a in group)
 scores=', '.join(f"{x['id'].rsplit('_',1)[1]}: {x['score']}" for x in gate['representatives'])
 links=' · '.join(f'[{p}]({rev}{p})' for p in gate['report'].split(' + '))
 lines.append(f"| {typ} | {len(group)}/{accepted} | {min(a['tris'] for a in group)}–{max(a['tris'] for a in group)} | {scores} | {'시험 투입' if accepted else '보류'} · {links} |")
lines += ['',
'단일 시설 5종은 자산이 각각 1개라 front/side/back 3방향으로 판정했다. 이를 독립 변형 3개라고 세지 않았다. 다변형 유형은 3개 대표를 실제 열어 별도 리뷰어가 판정했으며, 평균이 7이어도 대표 하나가 미달하면 보류했다. 파일명으로 유형은 알 수 있었지만 제작 의도·이전 점수·코드는 제공하지 않았다. 점수는 정적 렌더의 판단이며 인게임 승인과 다르다.','',
'## 산출물·입력 계약','',
'- 생성기: [bayloop_buildgen.py](../../../tools/asset-gen/bayloop_buildgen.py), 공유 사진 아틀라스 생성기·시설 부품·검사/검토판/보호 복사 스크립트는 같은 폴더.',
f'- [SPEC]({src}SPEC.md) · [63개 입력 recipe]({src}recipes.json) · [manifest]({src}manifest.json). FBX/GLB/개별 blend/실제 geometry JSON은 `art/models/bayloop_variety_v01/`.',
'- 시험 투입: `F1/Assets/Art/BayLoop/Buildings/<type>_<nn>.fbx` 55개 + `BayLoopShared_01~04.png` 4장. 프리팹은 만들지 않았다. 보류 8개는 이 폴더에 넣지 않았다.',
'- [building-types.md](../../map/building-types.md)에 town_house·detached_house·chapel_bell·school_small·bus_stop·fuel_stand 추가, 기존 farmhouse/cafe_kiosk 등 규칙 유지. 폭·층·구역·피벗을 표 형식으로 기록.',
'- 주택 입력: type, 폭4–10m, 깊이2–14m, 층1–4, 지붕4종, 1층4종, 개구부 리듬, 발코니, 덧창, 차양, 회벽, 마모, 간판/실외기/화분/빨래, seed. terrace_living는 선택 bool로 창/문 교대와 발코니 생활층, shop_display는 선택 bool로 실제 상품 진열대를 더한다.',
'- 시설은 용도별 고정 형상 프로필이며 폭·깊이·팔레트만 조절한다. 지원하지 않는 층/지붕/생활 입력은 거부한다. 모든 시설이 주택의 모든 입력을 지원한다고 주장하지 않는다.',
'- 세계32px/m·공용팔레트·사진 기반 마모를 생성기에 고정. 회벽 선택8개는 팔레트에서 가능한 6색 계열+명도 순서를 뒤집은 2변형이며 독립된 파스텔8색은 아니다. 분홍 안료는 없어 rose는 바랜 갈색 계열.',
'- 아틀라스 4장×1024², 각 모델 1장 공유. 유효252px/256px 칸. 한 면을 늘려 붙이지 않고 실제 길이 기반 UV를 분할한다. 32색 검사는 **조명 전 원본 albedo** 기준이며 최종 렌더 색수 검사는 아니다.',
'- 피벗 본체 바닥 앞면 가운데, +Z 앞, 1단위1m. 마당·담장·차양·헛간은 실제 bounds에 포함. dense/medium/sparse는 manifest 메타데이터이며 밀집/성긴 구역을 씬에 배치하지는 않았다. 공용 보도와 경계 배치는 프로그래머 담당.','',
'## 조사 → 재현 → 적용·개선','',
f'[참고 및 접근 실패 범위]({rev}references.md)에 원문 링크·라이선스를 기록했다. 공개 CGA의 매스→층→개구부 분할과 Blender seed 원리를 참고했고 논문 구현을 재현했다고 부르지 않는다. 실사진은 이전 Procida/Atrani/Positano 비례 참고 자료를 재사용했다. 게임 표면은 기존 CC0 사진 가공을 재사용하며 새 AI 이미지·배경은 0개다.',
'기준 재현은 기존 bayloop_buildings/italy의 metre UV·부품·축 변환·렌더 파이프라인이다. 여기에 재현 가능한 입력 조합, 실제 앞면 개구부, 층후퇴, 마당/대문, 헛간, 시설 형상을 붙였다. 새 모델은 예전 통과 점수를 물려받지 않고 다시 검사했다.',
'개선: 유리 면이 벽에 묻힌 문제와 동일 평면 겹침 수정, 사진의 명암 중앙값 양자화로 작은 주요면 단색 탈락 해소, 상점 문턱 바닥 침범 수정, 단독주택04 지붕/회벽 수정, 층층집06/12 창·문 리듬/생활층 수정, 상점01 상품 진열 및 간판 추가. 검사 임계값은 완화하지 않았다. 이전 판정은 이력이며 현재 유효 판정은 gate-summary의 파일·픽셀해시 기준이다.','',
'## 실제 검증','',
f'- [GLB 전수검사]({rev}checks.json): 63개 실행, **62 통과 / 카페1 미통과**. palette 밖0, POT, 실제 삼각형 양축32px/m±10%, tris≤4000, 원점/치수/앞면/출처 검사. 카페는 면적0.05125m² 주요삼각형4개의 단색 판정으로 탈락. 작은 면 예외 기준은 기존 검사 그대로다.',
f'- [FBX 재임포트]({rev}fbx-checks.json): **63/63 통과**. 새 Blender에서 외부 PNG 없는 임시 FBX를 읽어 내장 albedo 복원, GLB 정점/크기/축/바닥/삼각형 수를 대조했다. 모든 현재 FBX/GLB/PNG SHA가 검사 결과와 일치한다.',
f'- [사진 아틀라스 검사]({src}texture-checks.json): 4장 모두 공용팔레트 밖0·32색 이하·POT·칸별 비단색. [provenance.csv](../../../art/provenance.csv)에 출처 URL·라이선스·가공·원본/시험 파일 행 기록.',
f'- [실제 재현성]({rev}determinism.json): 독립 Blender 두 번으로63개 형상·UV SHA 동일, 형상 해시63개 고유, 최대3,928tris. [입력 계약]({rev}generator-contract-recheck.json):63개 유효·10개 오류입력 거부. 동일hash는 시각적으로 충분히 다르다는 보장이 아니다.',
f'- [최종 톤]({rev}tone-gate-r2.md): 예·7.5/10. 각 유형 대표 결과와 현재 픽셀 SHA는 [gate-summary]({rev}gate-summary.json). 시험투입은 type 전체 기술검사+대표 모두≥7+전체톤≥7 조건을 적용.',
'- 기존 BayLoop 102개 파일은 작업 전후 SHA 일치. 새 시험 파일59개는 원본과 SHA 일치. 씬·코드·.meta·기존 a/b/c 수정 없이 신규 FBX/PNG만 복사.','',
'## 보류·남은 결함','',
'| 보류 type | 결함 3개 | 다음 단계 |','|---|---|---|',
'| farmhouse 4개 | 지붕 겹침/재질 불명확 · 벽 전체 균일한 얼룩 · 발코니/문 주변 어두운 뭉침 | 지붕 사진/UV 방향과 국소 마모를 다시 설계 |',
'| chapel_bell | 균일한 회벽 얼룩 · 긴 측후면 개구부 부재 · 종 매달림 구조 불명확 | 측후면 창과 종 지지부 보강 |',
'| school_small | 학교 식별 단서 없음 · 후면 생략 · 동일한 어두운 교실 창 | 학교 표식과 실제 후면 입면 제작 |',
'| bus_stop | 얇은 지붕 연결부 · 벤치/벽 명도 겹침 · 노선 표식 부재 | 프레임 접합과 표지 정보층 제작 |',
'| cafe_kiosk | 카페 식별 부족 · 차양/간판 명도 겹침 · 후면 생략 | 음료/메뉴와 후면 창, 단색 면 UV도 수정 |',
'',
'요청 우선순위 주택·단독주택·상점·층층집을 끝냈고, 빌라와 주유소까지 통과분을 투입했다. 후순위 미달5type는 추가 제작 반복을 하지 않고 이 판에서 보류했다. 모든63개를 통과시켰다는 뜻이 아니다.',
'통과분에도 같은 윤곽·창 반복, 사진 마모의 균일성, 막대형 식재, 흰 커튼 패널, 관 끝처리 문제가 남는다. 일부 높은 건물은 검토 카메라에서 옥상 끝이 잘리며 별도 top뷰로 확인 가능. 축소 격자는 톤 비교용이고 개별 작은 부재 평가의 대체물이 아니다.','',
'**QA/프로그래밍 임포트 필요. 도현 최종 승인 필요: 인게임960×540 캡처.** 재질명이 BayLoop_Photo32로 공통이므로 manifest의 각 texture에 따라 4개 재질로 명시적으로 매핑해야 한다. Point·sRGB·압축없음·1024 유지, 앞면/크기·마당 돌출·콜라이더·LOD·실제 주행 카메라와 조명을 확인한다. 이번에는 Unity 임포트·Play·실제 배치·프리팹·충돌·성능을 검증하지 않았다.','',
'## 개별 파일','',
'| ID | tris | 원본 FBX | 정면 렌더 | 시험 투입 |','|---|---:|---|---|---|']
for a in m['assets']:
 name=a['id'];lines.append(f"| {name} | {a['tris']} | [FBX]({src}{name}.fbx) | [PNG]({rev}{name}_front.png) | {'예' if name in delivery['accepted'] else '보류'} |")
(ROOT/'docs/team/reports/art-20261025-2023.md').write_text('\n'.join(lines)+'\n',encoding='utf8')
spec=O/'SPEC.md';text=spec.read_text(encoding='utf8')
if '`shop_display`' not in text:spec.write_text(text+'\n선택 입력 `shop_display`: bool, 기본 false. 상점 앞 실물 진열대·상품·상단 간판을 추가한다. 최종 shop_house_01에 적용.\n',encoding='utf8')
print('REPORT_WRITTEN',len(delivery['accepted']),len(delivery['withheld']))
