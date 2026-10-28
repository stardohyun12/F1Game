# 건물 뷰어 (Unity 없이 브라우저로 건물 둘러보기)

## 도현: 여는 법
1. git-bash 에서 `bash tools/viewer/serve.sh` (= `python tools/viewer/serve.py`, 창은 켜 둔 채로)
2. 브라우저에서 **http://127.0.0.1:8770/** — 특정 건물 바로 열기: `http://127.0.0.1:8770/?b=garage` (room · garage · depot · postoffice)
3. 다 보면 serve 창에서 Ctrl+C

## 조작
| | |
|---|---|
| 왼쪽 목록 | 방 · 정비소 · 택배 보관소 · 우체국 |
| 둘러보기 | 왼쪽 드래그 = 돌기, 오른쪽 드래그 = 옮기기, 휠 = 확대/축소 |
| 걷기 | 화면 클릭 → 마우스 잠김. WASD 이동, 마우스 보기, Shift 달리기, Space 위 / C 아래, Esc 풀기. 눈높이 1.6m, 벽은 통과됨 |
| 그림자 | 실시간 해 그림자 켬/끔 |
| 해 방향 | 해의 방위(높이는 30° 고정) |
| 픽셀 (720) | 켬 = 게임처럼 1280×720 을 Nearest 로 확대, 끔 = 모니터 원래 해상도 |
| 표면 결 (Unity _SN) | 게임 셰이더(`SceneA7Lit.shader` `_SN`)와 같은 식 — 물체에 붙은 1/8m 칸마다 명도 ±세기(기본 0.04 = Unity 기본, 0~0.12). 칸/m = Unity `_SNDens`(8). 건물·땅·길 모두. URL `?sn=0.08&snd=8` |
| 화면 그레인 (정적) | 720 픽셀당 고정 점 오버레이(Unity `A6dGrainToggles.screenGrain`, 기본 꺼짐). URL `?sg=0.03`. `?nograin` = 둘 다 끔 |
| 새로고침 | 지금 건물 glb 를 캐시 없이 다시 읽음(시점 유지) |
| 선택 (Q) | 클릭 = 고르기(노란 강조 + 오른쪽 칸에 이름·재질·텍스처·찍은 점·UV) · **Shift/Ctrl+클릭 = 여러 개 넣기/빼기** · 빈 곳 클릭 / Esc = 풀기 |
| 부품 선택 | 합쳐 납품된 큰 메시(≥300 삼각형 또는 `_shell/_props/_furniture/_interior/_combo/_body/_building`)는 불러올 때 뷰어가 연결 부품 `<이름>_pNNN` 으로 쪼갠다(방 크기 상자는 벽·바닥·천장 한 장씩). 클릭 = 그 부품만, **ㄴ 부모 선택** = 원래 오브젝트 전체. 칸에 "부품: … ← 원본 의 n/N번째". URL `?nosplit` = 끔 |
| 선택 중 시점 | 왼쪽·오른쪽 드래그 = 돌기 · 가운데 드래그 / Shift+드래그 = 옮기기 · 휠 = 확대 · **F / 더블클릭 = 고른 것 가까이** (기즈모를 끄는 동안만 시점 잠금) |
| 옮기기 | G = 이동 화살표, R = 회전 고리 (0.05 m / 5° 단위). 여러 개면 묶음 가운데 기즈모로 같이 움직임. **되돌리기** = 고른 것 원래대로. 옮긴 것은 이 탭에서만 유지(새로고침해도 남음) |
| 코멘트 | 오른쪽 칸에 쓰고 **보내기**(Ctrl+Enter). 고른 물건(여러 개면 전부)·옮긴 양·시점·스샷이 같이 저장. 아무것도 안 고르면 지금 화면 전체에 대한 코멘트. 아래 **저장된 코멘트 보기** / 화면의 빨간 번호 핀을 누르면 그 시점으로 감. 기본 **남은 것만**(`status: done` 은 숨김) / **전부**(적용됨·보관 포함). **적용된 코멘트 지우기** = done 줄·스샷을 `feedback/archive/` 로 옮김 |
| 걷기 + 선택 | 선택 중엔 클릭이 마우스를 잠그지 않고 고르기, WASD 로 걷기 |

빛은 게임 A7 근사(따뜻한 낮은 해 + 하늘색 주변광 + 3단 계단 명암 + 40m~ 안개)라 Unity 화면과 완전히 같지는 않습니다. 최종 판정은 Unity `Preview_Candidates` 로.

## AI 팀원: 에셋이 바뀌었을 때
1. `art/export/...` 의 FBX/PNG 를 새로 납품한 뒤 `bash tools/viewer/export_all.sh` (하나만: `bash tools/viewer/export_all.sh depot`)
   - 결과 `tools/viewer/assets/<이름>.glb` + `manifest.json`, 로그에 재질별 텍스처·삼각형 수가 찍힘
2. 도현 브라우저에서 **새로고침** 버튼 (페이지 F5 도 됨)
3. 새 건물 추가 = `tools/viewer/buildings.json` 에 한 줄(name, label_ko, fbx, tex_dir, textures: 재질 이름 → png). 재질에 이미지가 없으면 이 표로 붙이고, 모든 텍스처는 Closest(Nearest)로 내보냄.
   - 한 덩어리로 합쳐 납품된 메시는 `buildings.json` 의 `"split_loose": ["오브젝트 이름"]` 으로 연결 부품별 자식 `<이름>_pNNN` 으로 나눠 내보냄(정비소·우체국 본체) — 뷰어에서 타이어 하나도 고를 수 있게. 나머지 오브젝트는 합치지 않고 FBX 이름 그대로.
4. 도현 코멘트 읽기·적용: `tools/viewer/feedback/README.md` (`feedback/<건물>.jsonl` + `feedback/shots/`). 적용한 줄엔 `status: "done"`(+`fix`)을 넣는다 → 뷰어 기본 목록에서 사라지고, **적용된 코멘트 지우기** 버튼 또는 `curl -X POST 'http://127.0.0.1:8770/api/feedback/archive?b=<건물>'` 로 `feedback/archive/<건물>.jsonl`·`archive/shots/` 에 보관(GET `&archived=1` 로 같이 읽힘).

검사: 서버를 켠 채 `node tools/viewer/_check/cdp_check.js` (로드·새로고침·모드·픽셀·걷기), `node tools/viewer/_check/select_check.js` (고르기·옮기기·되돌리기·코멘트·핀), `node tools/viewer/_check/select_multi_check.js` (여러 개·선택 중 시점·F/더블클릭·빈 곳 풀기·걷기+선택), `node tools/viewer/_check/sn_shot.js` (표면 결 sn=0/0.04/0.08 세 시점 `sn_*.png` + 부품 선택·부모 선택·코멘트 필터). 검사 코멘트는 feedback/ 에 쌓이니 끝나면 지운다.
three.js 는 `vendor/` 에 r170 고정(인터넷 불필요).

## 기획 뷰어 (`/plan/`) — 지도 위에서 첫 배달 흐름·인물·장소 보고 코멘트

- 열기: 건물 뷰어 왼쪽 **기획 →** 버튼 또는 **http://127.0.0.1:8770/plan/** (같은 serve.py)
- 화면: **가운데 = Map_TownV6 평면 지도**(드래그 이동·휠 확대·**맞춤**), 왼쪽 = 흐름/인물/장소/코멘트 목록, 오른쪽 = 핀을 누르면 열리는 그 장소 칸(장소 메모·레퍼런스·그 자리의 단계 카드(대사·조건·카메라·소리·캡처)·거기 있는 사람 카드·3D 건물 뷰어 링크).
  - 지도 위: 파란 동그라미 = 장소 핀(프리팹 있음) / 회색 = 아직 없음, 주황 선 = 첫 배달 경로(고리), 주황 번호 = 단계 1~7(누르면 왼쪽 흐름 줄 + 오른쪽 칸), ★1 = 배달 목표. 인물 카드의 **지도에서 보기** = 그 사람 자리로 이동. 맨 위 **퀘스트** 선택이 핀·경로·번호를 거른다(지금은 첫 배달 하나).
- 데이터: `docs/design/tutorial/first-delivery.yaml` 한 파일 — `map`(바탕 그림 `docs/map/townv6-plan-base.png` 1600×1600, `world_to_px: [a,b,c,d,e,f]` → px_x = a·x + b·z + c, px_y = d·x + e·z + f, 현재 1px = 0.5m·원점 (−400, 400), 확인점 3개) · `quests[]`(id·targets·`route_world` [x,z] 목록·`steps[]`: place/actors/lines/condition/camera/sound/capture/`marker_world`) · `characters[]`(`place` 로 지도 자리) · `places[]`(`world` [x,z], `library_prefab`, refs). 전부 `status: draft`. 서버가 `GET /api/plan?f=first-delivery` 로 JSON(pyyaml 있으면 pyyaml, 없으면 serve.py 의 작은 파서 — 들여쓰기 맵/리스트·`[a, b]`·`{k: v}`·따옴표·`#` 주석까지만, `|`/`>` 블록 문자열은 안 됨).
  - 바탕 그림 다시 만들기: `python tools/viewer/_check/plan_map_base.py` (docs/map/town-v6b.json → 글자·경로 없는 평면). 좌표 변환은 `docs/qa/tmp/tv6_plan.py` 와 같다.
- 코멘트: 칸마다 **코멘트** 버튼 → 쓰고 보내기(Ctrl+Enter). `tools/viewer/feedback/plan-first-delivery.jsonl` 에 한 줄씩, `target` 이 자리(`quests/first_delivery/steps/3/lines/0` = 4번째 단계 첫 대사, `characters/julia/hair`, `places/teahouse/refs/1`), `comment` 가 말. **남은 것만 / 전부** 는 건물 뷰어와 같은 `status: done` 규칙. 핀의 빨간 숫자 = 그 장소·그 자리 단계에 남은 코멘트 수.
- 레퍼런스 사진: `docs/design/tutorial/refs/<id>/<n>.jpg` (긴 변 ≤1200), 출처·라이선스 확인 방법은 `docs/design/tutorial/SOURCES.md` — **CC0/퍼블릭 도메인이 Commons 파일 페이지에서 확인된 것만**(Unsplash 출처·AI 이미지 금지). `refs/sources.json` 을 고치면 `python tools/viewer/_check/plan_refs_sync.py` 가 SOURCES.md 와 YAML 의 refs 를 다시 쓴다. 서버는 `/repo/docs/...` 로 `docs/` 아래 파일만 읽기 전용으로 내준다(캡처 `docs/qa/evidence/...` 도 같은 길).
- AI 팀원: 기획을 고치면 YAML 만 고치고(새로고침 버튼), 코멘트를 반영한 줄엔 `status: "done"` + `fix` 를 넣는다(건물 뷰어와 같음). 퀘스트를 더하려면 `quests[]` 에 항목 하나(steps 는 places/characters 의 id 를 가리킴).
- 검사: `node tools/viewer/_check/plan_check.js` → `_check/plan_map.png`(지도+핀+경로+번호) · `plan_pin_teahouse.png`(핀 열림) · `plan_marker_step4.png` · `plan_chars.png` · `plan_places.png` · `plan_comment.png` · `plan_allc.png` + 코멘트 왕복 + 콘솔 오류 0. 검사 코멘트(`[검사]`)는 끝나면 jsonl 에서 지운다.
