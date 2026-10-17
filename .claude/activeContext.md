# Active Context — PitCrew

> 매 세션 시작 시 읽고, 끝날 때 갱신.
> 안정 규칙은 `.claude/rules/`에. 여기는 **상태**만.

---

## 세션 34 저녁 (2026-10-13) — 밀도 2차 커밋 + 바닥·원경 3차 + 도시 알고리즘 조사

- **커밋 `906f899`** = 밀도 2차(소품 16종, 2053 배치, 게이트 5/10·verifier PASS).
- **3차(바닥·원경, 미커밋)**: 소품 15종 더(연석·가로등·쓰레기통·가판·소화전·풀·덤불·바위·꽃·얼룩·땜질·석축·계류·잔교·배), `WorldSurface.shader` 에 `_Patch/_Tile2` 덩어리 무늬(값노이즈 step), 구역 바닥 흙/잔디/모래, FarGround·언덕 13·마을 밖 나무 ~500, 부두(석축 MeshCollider·잔교 3·배), 횡단보도 643→47(2칸 폭 도로 오판 수정), 아스팔트 균열 덩어리(3차-b). 배치 2752, 스모크 8/8, validation 이슈 0. **게이트: 3차 6/10 FAIL(verifier PASS) → 3차-b 판정 대기.** 기록 `docs/gates/2026-10-13.md`, 경위 `art/source/coastal-town/README.md`, 비교 `v3_vs_v2_vs_codex.png`.
- **다음 레버(게이트·내 눈)**: 다운타운 2칸 폭 도로 = 12m 빈 아스팔트(중앙 분리대·주차선·가로수 섬), 건물 반복(집 3종), 02 서쪽 바깥이 공원처럼 빈 들판(울타리·밭), 석축 0.3m 낮음.
- **4차 건물 벽 단면 = Codex·Claude 둘 다**(도현 "나누지 말고 둘 다, 차이 보자"). Codex: 벽돌 기단·쿼인·네 면 창·지붕색 3종·변형 7종, 삼각형 5배(3~4k). Claude: 띠·쿼인·뒷창·변형 8종(간판/차양/덧문/발코니 교체 = **도현 "복붙으로 보임, 임시만 OK"**), 1.2k. **지금 씬엔 Claude 판 설치**, Codex 판은 `art/_compare/2026-10-13-building-faces/codex/`(패치+스크립트)로 복원 가능. 비교표 `art/source/coastal-town/codex4_vs_claude4.png`. 게이트 판정 `docs/gates/2026-10-13.md` v4 줄. **도현 결정 대기: 어느 판으로 갈지 / 다음은 실루엣(폭·층수·지붕)이 다른 진짜 변형.** 확인 필요 시 PushNotification(Remote Control 꺼져 있으면 데스크톱만).
- **밤 기획 대화 → D-098~D-102**: 마을 2개 + 굽은 해안도로(고도 허용, D-050 범위 축소) / 마을 대체로 평지+일부 경사 = 보이지 않는 난이도(패드 업그레이드 백로그) / 높이=진행 단계, 생성 규칙 셋(막지 않기·우회로·꼭대기 랜드마크) / 색감 아말피 한낮 / 카메라 뒤 5m. 스펙 `docs/superpowers/specs/2026-10-13-two-towns-coastal-road.md`. 조사 노트 3개 `옵시디언/research/` (city-layout·urban-morphology·soft-gating). 해상도 640×360×3 적용됨(PC_RPAsset). 건물 5차 = Codex 11종 설치(게이트 7/10 PASS). 한낮 v2+카메라 게이트 대기. **전부 미커밋.**
- **도시 배치 알고리즘 조사** → `옵시디언/research/city-layout-algorithms-2026-10-13.md`. index.opensource 는 도시용 거의 없음(WFC·MarkovJunior·mapgenerator 릴스 3개). 추천 1순위 = 직접 구현(구역→블록 이등분→연결, ~170줄). **도현 결정 대기.**
- 캡처 PNG 는 git-lfs — `git show` 로 꺼내면 포인터만 나온다. `git cat-file -p rev:path | git lfs smudge > out.png`.

## 세션 34 (2026-10-13) — Codex 줄기 채택(D-096) + 에이전트 팀 가동

- **아트 스타일 최종 = Codex "따뜻한 로파이 해안"(D-096).** 어제 제 플랫 Alba 줄기(D-094)는 접음. 비례·밀도 규칙(D-095)은 유지.
- **Codex 산출물이 저장된 ComplexTrack 에 설치돼 있음**: `CoastalTown`(417 배치, Buildings/Props/StreetFurniture 비활성), 플레이어 차 = `FormulaGameplayVisual.prefab`(낡음/복원 재질, `FormulaVehicleVisual`), 백업 `F1/Assets/Art/CoastalFormula/ComplexTrack.before-formula.unity`. 재생성: `tools/asset-gen/lofi_pilot.py`, `coastal_formula.py`; 설치 메뉴 `PitCrew/Art/Coastal Town/Install`, `Coastal Formula/Install Gameplay Vehicle`; 스모크 `CoastalTownSmoke.Run()`(배달 경로 라벨 버그 고침 → 8/8).
- **에이전트 팀**(`.claude/agents/`): graphic-gate(외부 눈, sonnet) · design-gate(기획, sonnet) · verifier(계측, sonnet). 판정은 `docs/gates/YYYY-MM-DD.md` 한 줄씩. Stop 훅은 그 기록이 변경 코드보다 새로우면 조용함. 팀 구성 대화 미완: "기획이 합격 기준 5줄을 먼저 쓸지" 도현 답 대기. asset-maker(아트 손) 파일 미작성.
- **첫 게이트 결과**: verifier PASS / graphic-gate FAIL 4/10 → 다음 손: 건물당 소품·비례·톤 통일(로파이 규칙 안에서).
- **미커밋 큰 덩어리**: Codex 작업 전부(씬 5만 줄·Art/·Editor 9개·tools) + 제 도구들 + 에이전트 파일. 커밋은 도현 허락 뒤 기능 단위로.

## 세션 33 (2026-10-12, 4.5개월 공백 뒤 재개) — 아트 스타일 결정

- **도현 결정: 아트 스타일 = A+C ("Kenney 플랫 로우폴리" 바탕 + "미니어처 디오라마" 한 겹).** 보드 6종 → A+C 선택 → Unity 실측 v1/v2/v3.
  - 보드·캡처: `art/concept/style-board-2026-10-12/` (`style_board.png` 6결, `unity_diorama_v2.png`, `unity_palette_v3.png`, `unity_palette_v3_zones.png`). 케이크 파일럿: `art/concept/cake-pilot-2026-10-12/`.
  - 메뉴(에디터, 씬 저장 안 함): `PitCrew/Art/Apply Diorama Look`(세션28 무드 보장 + 그레이딩·원경 흐림·비네트) / `Apply Zone Palette`(건물 137채 colormap 을 구역별 변형으로 교체) / 각각 Revert. 캡처: `ArtCapture.CaptureSet` / `CaptureChaseAt`.
  - 팔레트 데이터 = `tools/asset-gen/palette_colormap.py`(구역별 목표색) → `F1/Assets/Rendering/Palette/colormap_<kit>_<zone>.png`.
  - **다음 결정 대기**: 팔레트 v3 를 씬에 **저장**할지(지금은 미저장·되돌림 상태), 도로색(거의 검정 → 따뜻한 회색) / 땅 톤 / 가로수 밀도 조정, `art/STYLE.md` 한 장 작성.
- **환경 (이 노트북)**: Unity 6000.3.19f1 로 열림(원 6000.3.9f1 미설치). `com.unity.pipeline` 추가. Blender 5.2 `D:\Tools\Blender`. ComfyUI 는 `C:\Users\rtx40\ComfyUI` 만 뜸, Hunyuan3D 체크포인트는 D 드라이브. `tools/asset-gen/README.md`.
- **하네스 수정**: `scripts/ucli.sh` 디스패처(본가 exe 없으면 `ucli-http.sh` 로 connector HTTP 직접 호출). stop.sh / regression-check.sh 가 이걸 씀. 이 머신에서 회귀 12 PASS / 1 FAIL(기존 CargoPolyBudget).
- **리포 손상 수습**: 미커밋 .meta 12개 커밋(`1d0630d`) — 새 체크아웃 도로 자홍색 원인. ComplexTrack 씬엔 MoodController 가 없어 그림자·안개·하늘이 꺼진 상태가 "기본"이다.
- **미커밋**: tools/asset-gen, Editor 스크립트 3개(ApplyDioramaLook/ArtCapture/ApplyZonePalette), scripts/ucli*.sh, 훅 수정, art/ 보드, Rendering/Palette, Packages/manifest.json.

## ⭐ 운영 모델 전환 (세션 32, 도현 결정 — 최우선 숙지)

**역할 분리 + 자동검사 유지** (`project_rules/roles.md`, 커밋 `5238b4e`). 룰은 행동 못 바꿈이 증명돼서 **게이트**로 전환:
- **코딩(나)은 기획 에이전트 + 그래픽 에이전트가 통과시키기 전엔 "됐다"도 커밋도 못 한다.** 컨텍스트 0 외부 눈 = 작성자≠판정자 구조화.
- 도현 진단 3종(이 모델이 막는 것): ①목표만 보고 주변 헤집음 ②게임 기본흐름 무시 ③혼자 과대평가 "됐다".
- 작동 증명된 자동검사(회귀 하네스)는 유지·확장. 안 지켜지던 룰(D-074 트리거 등)은 게이트로 대체.
- [[feedback-orchestrator-mode]] 갱신됨(룰→게이트). **"혼자 빨리"가 떠오르면 = 게이트 안 건 신호.**

## 다음 세션 첫 작업 (세션 33) — 우선순위 (도현 판단 필요, 야간 자율로 안 건든 것)

> 세션32에 풀 게임루프 복원·정렬 끝. 남은 건 대부분 도현 시각/기획 영역:
> 1. **경로계획 경로선** — 야간 조사 결과 **버그 아님, 도현 본인 설계**(2026-05-19 "선택 순서대로 줄 잇기"). 코드(RoutePlanScreen.cs:432-437): start(차)+0.5y → 선택 목적지들 직선 LineRenderer. 건물 위 관통은 직선 웨이포인트선의 본질. **결정만 하면 됨**: (A) 현 직선 유지(방향 힌트로 충분) (B) 도로추종 A* 페이브 구현(중간 공수, 사실적) (C) 점선/화살표로 "방향 힌트"임을 명확히(소공수). 추천=C(저비용으로 "드라이브 경로 오해" 해소). **도현 판단**.
> 2. **화물 고폴리**(CargoPolyBudget FAIL, 회귀 유일 red) — Eggs53k/Glass27k/Fish75k/Cake54k. CargoDecimator로 깎되 도현 승인한 룩 망칠 위험(세션21 케이크 속뚫림 전례) → **도현 시각 영역**, 깎고 그래픽게이트.
> 3. **포장 화면** — 빈 상태(₩0/0%) "멈춘 듯"(그래픽게이트 사소). 폴리시. ('랩 폭' 라벨 잘림은 세션32 야간에 **측정 결과 안 잘림**=게이트 false-positive, 라벨 Y195-255 vs 패널 165-915. 장식 "▬▬▬"+muted색 오판. 추격 불필요.)
> 4. **맵 구성(도현 #3)** — 구조 재설계 + 아트 3회 반복으로 **그래픽 게이트 5→6/10**. 지구 톤(`PlaceZoneGround` 메뉴, 커밋 `ac496cd`)으로 공업↔다운타운 구분은 개선. **다음 우선 fix(게이트 잔여)**: ⓐ**항구 ground가 lot별 얼룩**(갈색/초록 섞임) → zone 전체 균일 모래톤으로 재칠(PlaceZoneGround 항구 쿼드가 도로/건물 사이만 덮어 들쭉 — 쿼드를 항구 띠 전체 덮게 키우거나 grass 자체를 zone별 머티리얼로) ⓑ**바다↔도시 맞닿음**(톱다운서 바다 안 잡힘, 떠보임) ⓒ공업 카키 채도↑("흙"답게). **PlaceZoneGround도 리빌드 후 룩 재적용 체인에 추가**(Align 직전).
> 구조 재설계 1차 내역: 커밋: 구조 `2ee9ffd` / 바다 prominence `af62fc3` / 지구별 건물 실루엣 `b853776`+`9669198`. ✅된 것: 항구 바다 보임, 다운타운 고층 클러스터 구분, 3지구 roadMask 리듬, 연결성 297/297, 회귀 12 PASS. **남은 아트(도현 방향 필요 — 혼자 추측 천장)**: ①**바다가 도시서 떨어져 떠보임**(항구 수변과 안 맞닿음 — HarborSea 위치/잔디평면 경계 조정 or 해안선) ②**공업·항구가 둘 다 녹색 저층마당이라 구분 약함**(부감서 2지구처럼) → **per-zone ground 톤이 핵심 레버**(공업 갈색/항구 모래색 — 현재 zone별 ground 틴트 메커니즘 없음, 신규 필요) ③직사각 바다 가장자리 인공적 ④운전시점 건물 단색 박스벽(디테일 없음). **다음: per-zone ground 톤 신규(공업/항구 구분) + 바다 맞닿음. 도현 색 방향 정하면 빠름.** buildingMask는 footprint 2x2(C,F)는 큰블록에만, 1x1(H/K/W/T)은 어디나 — 프로그램 생성 `exec`로 재생성 가능. **"아쉬움" 원인 Top3**: ①(최유력) **지구 정체성 시각 0** — zoneMask는 공업/다운타운/항구 3분할인데 buildingMask가 전 지구 똑같은 교외주택(S) → 어딜 가나 같은 동네. ②배달지 11곳이 **시각 메시 없는 빈 비콘**(CityHall인데 건물 없음). ③균일 격자+전부 저층 → 단조·스카이라인 평평. **이미 미실행 plan 있음**: `docs/superpowers/plans/2026-05-24-map-structure-redesign.md`(6 Task, 새 자산 0·기존 Kenney/카탈로그만). **결정 옵션**(공수 순): (A 최저비용·최대체감) buildingMask를 zone별로 — 다운타운=H고층+C상업 / 공업=F공장+W창고+T굴뚝 / 항구=W창고+바다쿼드 (데이터 편집만). (B) 랜드마크 메시 자동배치 `PlaceLandmarks.cs` 신규(역=train-kit 기차/선로, 항구=컨테이너스택, 공업=굴뚝/탱크 — plan에 빠진 Task, 추가 필요). (C) roadMask 지구별 리듬 재작성(plan Task1, TrackConnectivityVerifier 의무). **⚠ 주의: Run Full Rebuild는 복원한 룩+8시스템 다 날림 → 리빌드 후 룩 재적용+시스템 복원 체인 재실행 필수.** **도현 방향 결정 필요**(A부터 점진 추천).
> 5. 배치(트렁크 적재) 단계 런타임 검증 — 기획게이트가 코드OK 확인했으나 실제 진입 미검증.

---

## (세션 32 완료 기록) 우선순위 결과

> 맵은 플레이어 운전시점에서 6/10. 색·도로(중앙선) OK. 남은 것:

1. ~~**교차로 "네모 블럭"**~~ **✅ 해결 (세션 32, `926234c`)**. root cause = Kenney junction 타일(crossroad/intersection)의 **4 코너 6cm 솟은 연석**(corner localY=0.020 vs 도로면 0.010). 2x2 클러스터에서 안쪽 코너 4개가 도로 중앙에서 만나 솟은 사각 패드 → 민무늬 아스팔트(c0cea6a)로 텍스처 맥락 사라져 어두운 네모로 드러남 (clue ③ 적중). fix: `FlattenJunctionCurbs` 메뉴 — 같은 메시 유지, 솟은 verts만 0.010으로 클램프(외곽/UV 불변=새 이음매 0, 이전 swap 실패와 다름). 206 타일 평탄화, before(네모 9+)→after(0) 스샷+Play 에러0 검증. **교훈: 표층 swap 2회 실패 후 geometry 측정→시각 둘 다로 root cause 적중(no-compromise §4 준수). 단일 진단 루프로 30분.**
2. ~~**OrderBoard wiring 자동 가드**~~ **✅ 해결 (세션 32, `ac1250b`)**. `HarnessChecks.CheckOrderBoardWiring()` 추가(reflection 5필드 + cardPrefab.OrderCardUI 단언, Play 불필요=저장씬 검사) → regression-check.sh BOARD 섹션에 박음. **재발 원인 정체: runController가 NULL이었음** — 세션31이 panelRoot 등은 저장했지만 runController는 런타임 auto-find로 카드만 뜨고 씬엔 안 박힘. RunRoot 할당+SaveScene으로 영구화, 재확인 PASS. 회귀 10 PASS/1 FAIL(기존 CargoPolyBudget만). **루프 정체 = 런타임 fix를 씬에 영구 저장 안 한 것.**
3. **맵 구성(도현 #3 "아쉬움")** — 미착수. 지구 구분/랜드마크/흐름 중 뭔지 먼저 확인.
4. 매 검증에 **실제 Play 포함**(UI·주행) — 정적 스샷만으론 UI-loss 못 잡음. [[feedback-qa-review-full-loop]] [[feedback-test-real-flow-not-reflection-harness]].

**맵 룩 재적용 메뉴**(리빌드 시 룩 날아감 → 순서대로 재실행): `Build Everything From Layout` → `Plain Asphalt Roads` → `Flatten Junction Curbs` → `Place Road Center Lines` → `Auto-Place Sidewalks` → `Auto-Place Suburban Props` → `Auto-Place Street Furniture` → `Auto-Place Street Vehicles` → `Apply Warm Grade` → **`PitCrew/Map/Align Map To Ground`**(땅=0 정렬, 마지막). (현재 씬엔 다 적용+커밋됨, 리빌드 안 하면 유지.)

**시스템 복원 체인** (세션32 발견: 맵 리빌드가 8개 시스템 GameObject + OrderBoard 배선을 조용히 날림 → 포장/차고 흐름 죽음 + 주문 UI 안뜸). 리빌드 후 순서대로:
1. `PitCrew/Cargo/Setup Cargo View (CCTV)` → 2. `PitCrew/Packing/Add PackingScreen To Active Scene` → 3. `PitCrew/Garage/Wire Scene Objects` → 4. `PitCrew/Restore/Wire Core Singletons`(UpgradeSystem/TireSystem)
5. `PitCrew/Channel8/Board Layout — 카드 우측 스트립` → 6. exec `FixOrderBoardWiring.Execute()`(panelRoot/cardsContainer/cardPrefab) → 7. exec로 OrderBoard.runController=RunRoot 할당 + SaveScene.
검증: 회귀의 `CheckCoreServicesPresent`(9 시스템) + `CheckOrderBoardWiring`(5필드) 자동 단언 — 빠지면 FAIL. (Phase1 통합 메뉴는 부작용 커서 회피.) **세션32 맵 리빌드 때 5~7 빠뜨려 OrderBoard NULL 재발→가드가 잡음.** 다음엔 이 7단계 다.

---

## 지난 세션 요약

- **세션 31 (2026-05-24~25): 하네스 엔지니어링 + 사용자환경 + 맵 플레이어시점 재설계 + UI 복구.**
  - **하네스 (B 트랙)**: 활동 피드(`89db39c`) — hook(`activity-log.py`)이 에이전트 start/done/turn을 `activity.jsonl`에 자동 적재 → studio-dashboard 팀 탭에 흐름(state.json 수동갱신 대체). Windows cp949 stdin 함정=raw bytes UTF-8 디코드. **외부 눈 UX 리뷰어**(`b561ab5`, `docs/harness/ux-reviewer-prompt.md`) — 컨텍스트 0 에이전트가 스샷만 보고 판정, `--post`로 피드 게시(👁). 대시보드 **관제탑(CONTROL.md) 탭**(`11dcf33`, server.py mini-markdown). **D-074**(`185a8c4`, session.md) — 멀티에이전트 트리거 규칙(화면commit→리뷰어/wiring→D-073/독립2개+→병렬), "혼자가 편해서 안 켜는 것 금지". 메모리 [[feedback-external-eye-reviewer]] [[feedback-orchestrator-mode]] 갱신.
  - **사용자 환경**: 도현 cmd 휑함 → VS Code 확장 + Simple Browser로 7878 도킹 결정. 대시보드가 정리+에이전트+맵 한 화면.
  - **맵 재설계 (판정기준=플레이어 운전시점, 부감 폐기)**: 리뷰어 4→6/10. 격자깨기 roadMask(`185a8c4`후 별도, Python BFS 사전검증+TrackConnectivityVerifier) / 절차적 보도 362(`PlaceSidewalks`) / **도로 중앙선**(`PlaceRoadLines`, "일반도로 같지않음" 해결) / 민무늬 아스팔트(`PlainAsphaltRoads`, 횡단보도 마킹 텍스처라 단색머티리얼로 제거) / 따뜻 그레이딩+잔디색(`ApplyWarmGrade`) / 차량·가로등·가로수·마당나무 placer. **교훈: scatter는 4→5 천장, 진짜 레버는 도로 외형. width는 내 과장카메라(size15) 탓—실제 추적캠(차뒤6m FOV50)선 비례 합리적, 도현 맞음.**
  - **UI 복구**(`fix(ui)` 커밋, ComplexTrack.unity): manual play "UI 안뜸" → 내가 Play해서 잡음. **OrderBoard.panelRoot/cardsContainer/cardPrefab 등 전부 NULL**(재발 D-073 wiring 손실). `FixOrderBoardWiring`(_archive, exec 호출)+`Channel8BoardLayoutSetup`(우측스트립)로 복구, Play+ScreenCapture 검증(카드 3장 렌더).
  - **도현 manual play 피드백**: ①색감 좋음 ②도로 "일반도로 같지않음"=중앙선으로 해결 ③맵 구성 아쉬움(미착수). + **교차로 네모블럭** + UI안뜸(고침). "이런거 말 안해도 되게"=내가 Play테스트로 잡아야.

- **세션 28 (2026-05-20): 혼자검증 + W3 차고 마무리 + 비주얼 폴리시 시작 + AI 자산 파이프라인.**
  - git 노이즈 264파일(.meta 재직렬화 등) 청소 + 누락 meta 보완(`e156826`).
  - **차고 검증**: verifier subagent 12/12 ALL GREEN. 헤더 겹침·잘림 수정(`139fb8e`). 차고 캔버스 "안 보임"은 **내 인위적 테스트(MainMenu 캔버스 살린 채 Show)+ScreenCapture 다음프레임 타이밍** 문제 — 실제 플로우(정산→차고로)에선 정상(서브에이전트 확인). order 4000→7000 안전망(`faf7fc2`). 교훈 [[feedback-test-real-flow-not-reflection-harness]] / [[feedback-clean-replay-before-services-diag]].
  - **비주얼 폴리시 진단**: "진짜 게임 느낌" = 에셋 아니라 **아트디렉션**. 라이팅 shadows=None/Default-Skybox/ambient1.0/fog off가 평평함 80%.
  - **낮 무드 시스템**(`1fa1da1`): MoodProfile SO + MoodController(Awake 적용, [ExecuteAlways]). 그림자+따뜻한 태양+Flat앰비언트 대비+fog+포스트 그레이딩. "황혼/야간 나중에 다"는 SO 복제로 가능.
  - **그라데이션 스카이박스**(`0152e8e`): GradientSky.shader 자가 작성(URP HLSL 3색).
  - **차고 F1 머신**(`f008d25`): placeholder 텍스트 → Kenney race.fbx RenderTexture 렌더 + 회전. 메인캠 못 보는 먼 곳 스테이지.
  - **카드보드 화물**(`5ce021a`): 회색 큐브 → 프로시저럴 카드보드 박스(CargoBoxAppearance, 텍스처 코드 생성). 메시·콜라이더 불변→포장 무관.
  - **AI 자산 파이프라인**: Meshy API 키 연결(`.env.local`, gitignore됨, 잔액 2080). **교훈: Meshy 등 AI-3D는 각진 박스에 흐물 → Kenney/프로시저럴이 우월. 유기적 hero만.** 2D 아이콘은 gpt-image-1(키 미설정). OpenAI 키 받으면 부품·화물·UI 아이콘 세트.
- 세션 18 (2026-05-08): Phase 1 DIP/Service Locator + Phase 2 Template Method + Phase 3 Modifier Chain.
- 세션 19 (2026-05-11~12): Phase 4 AudioCueBus + Phase 5 TireTypePresets + Phase 6 Peer Review 6건. 미커밋.
- 세션 20 (2026-05-12~13): Unity 검증 도구 Coplay MCP → **unity-cli** 전환. Obsidian vault `옵시디언/` 분리. `_Recovery/` 솎아냄 (108→15). 차/도로 부유·기울임·z-fight 해결. 한글 폰트 atlas/material sub-asset 복구. **화물 손상 인과 변경** (차 G→박스 슬라이드→trunk 박음→손상). 알림 사운드 hook. Superpowers 플러그인 설치. 기획서 .pptx 생성. **Vertical Slice 데모 6주 결정 (D-069)**.
- 세션 21 (2026-05-13): 기획서 W1~W6 폐기, 3D 포장 미니게임 17 Task 구현 (Subagent-driven). D-070 결정. wrap 핸드오프까지 완료 (`RunController.TransferWrapStateToCargo`). 다물건(1~3) items[1+] 폐기는 후속.
- **세션 23 (2026-05-14, 자율 2시간 루프)**: CaptureFullScreen.cs. **W1 채널 3 (맵 해금) 완료** — `Zone`/`ZoneRegistry` SO, `ZoneUnlockSystem` (wallet 차감·auto-unlock·GetZoneOf), `OrderBoard.PickOffer` ZoneUnlockSystem 우선, `OrderCardUI` zone 라벨/tint. Seed: Downtown/Harbor/Industrial. ZoneUnlockPanel UI는 **도현 요청으로 제거** → autoUnlockOnAffordable. W2 brainstorm doc 작성.
- **세션 24 (2026-05-14~15)**: 하네스 점검 + 버그 5건 수정 (차 스폰 겹침 / concave collider / HUD 전화면 표시 / **포장 물건≠트렁크 물건** / PreRunSetupPanel). 세션 18~24 일괄 커밋 (`0a39d66`) 후 기능 단위 전환. **W2 채널 8 완료** — MiniMapHUD + DirectionArrow. **D-071 하네스 운영 확정** — 규칙 3건+ 검증 시 즉시 발효, 평가는 B 트랙 사후 (`session.md`에 B 트랙 섹션 추가). **채널 5 Plan 1 완료** (`bd53ea2`) — 다물건 트렁크 물리: N개 큐브 각자 슬라이드·충돌·손상 (CargoShakeSimulator 단일 박스→N 아이템). 채널 5 설계 결정: 따로 포장 / 최대 3개 / 플레이어 드래그 배치. AI 약점 2건 incidents.md 기록.
- **세션 25 (2026-05-15)**: **채널 5 Plan 2** (트렁크 자유 배치 — 포장→배치→출발 흐름, 마우스 드래그로 트렁크 바닥에 큐브 놓기, `714d489`). **채널 5 Plan 3** (적재함 크기 업그레이드 — TrunkExpand1/2, BoxCapacity 파생, OrderCard 회색 잠금, ResultPanel 5→7 카드, `a0c5a92`). **채널 5 전체 완료** (Plan 1·2·3). 그 다음 도현 결정: "오더보드/차고 시스템은 기획을 조금 더" → brainstorming 진입. 차고/부품 시스템은 레퍼런스 리서치 (`옵시디언/research/garage-parts-reference.md` 완료) 후 후속, OrderBoard 개편 spec/plan 작성 + 인라인 구현. **채널 8 (주문→경로 계획 흐름) 완료** — `OrderJob.destinationLabels` 데이터 모델, `DeliveryManager.ActivateRoute` 경로 기반, `RoutePlanScreen` 신규, `OrderBoard` 맵 중심 레이아웃 A. D-070(목적지 1개) 부활.
- **세션 26 (2026-05-15~16)**: manual play 피드백 폭주 → fix·polish 사이클.
  - **모델 정정** — 1잡=1곳 (`9578bea` `ea2946a` `ae69ca2`). OrderBoard 다중선택 UI + 출발 버튼.
  - **자기검증 3종 즉시 발효** (D-071, `f7d48dd`) — `project_rules/session.md` 코드 변경 commit 전 compile/runtime/visual 셋 다 통과 의무. 같은 세션 4회 반복 → incidents.md 등재. 메모리 [[feedback-self-verify-loop]] + [[reference-input-system-mode]].
  - **폴리시 4영역 완료** — 보드/경로 (`5b60634` `39389f6`), 포장 (`02cd082`), 트렁크캠+미니맵+화살표 (`69c09b9`), 정산 다물건 (`a48cedc`). subagent dispatch 1회 (출발 버튼 overflow 진짜 원인 = VLG childControlHeight).
  - **status audit + 기획서 14슬라이드 현행화** (`83286df` `efb20c7`).
  - **수치 audit 시스템** (`0f1a577`) — RunMetricsRecorder + CSV. 3런 측정 결과 **한 런 8~12초 / 거리 49~110m**. 데모 목표 5~15분과 200~900배 갭. **도시 78×54m 너무 작음이 본질** → 다음 세션 "맵 넓히기" 결정.
  - 배치 단계 Q/E 회전 (`c056910`).
  - 도현 메타 자각: 결단 회피 → 정리/audit/리서치로 회피 패턴. 해법은 작게 쪼개기.

---

## 현재 북극성

**Steam 데모 출시 — Vertical Slice — 2026-06-24 (6주)**.
풀게임의 모든 시스템 mechanically 작동 + 콘텐츠 1 슬라이스 (1 도시, 화물 5종, F1 부품 2개 복원).
풀 spec: `옵시디언/dev-plan/2026-05-13-vertical-slice-design.md`.

### 6주 일정

| 주차 | 작업 | 기간 |
|---|---|---|
| **W1** | 채널 3 (맵 해금) | 5/13~5/19 |
| **W2** | 채널 5 (박스 크기) + 채널 8 (내비) | 5/20~5/26 |
| **W3** | F1 복원 메타 (차고 + 부품 시스템) | 5/27~6/2 |
| **W4** | 정산→복원 연결 + 데모 시나리오 3런 | 6/3~6/9 |
| **W5** | 사운드 + HUD + onboarding | 6/10~6/16 |
| **W6** | QA + Steam 페이지 자산 | 6/17~6/24 |

---

## 세션 27 진행 상태 (2026-05-19, 도현 외출로 일시 정지)

**경로 전환**: 맵 넓히기 brainstorm → W3 차고 시스템으로 피벗 (도현 결정). R1 맵은 측정 단계에서 적용 예정.

**기획 마감 (D-073~093, 21개 결정)** — `옵시디언/dev-plan/2026-05-19-garage-realtime-planning.md` + `2026-05-19-restore-economy.md`:
- 6 부품 1 티어, 사이드뷰 분해도, 메커니즘 unlock형
- 가격 1000/4000/4000/8000/12000/25000 (지수형, 자유 게이팅, 엔진 1000 첫 도파민)
- 자금 누적 / 정산액 비례 페널티 / Yes/No 차고 prompt
- UI: 머신 가운데 + 카드 6장 하단 / 회색+부족 / 2~3초 컷씬
- 엔진 +20%/+30% / 변속기 K(다운)/L(업) / 부스트 Space / 타이어 4종+영구마모+펑크=잡실패
- 그랑프리 후속 / 서스 점프대 맵 디자인 트랙으로

**구현 진행 (Task 1~11/13 완료, plan: `docs/superpowers/plans/2026-05-19-garage-restore-system.md`)**:
- Task 1: F1Part SO + Catalog + F1PartId (`5be654a`, 경로 fix 포함)
- Task 2: 6 F1Part asset + Catalog asset (`ec25cb7`)
- Task 3: F1RestoreSystem + Services.Restore + GameContext (`8298703`)
- Task 4: 엔진 효과 +20%/+30% (`06bddc1`)
- Task 5: 변속기 K/L 수동 모드 (`41ddc23`)
- Task 6: 에어로 BoostSystem Space (`db5e93f`)
- Task 7: 타이어 + 영구 마모 + 펑크=잡실패 (`8b49de2`)
- Task 8: 도색 머티리얼 swap + 서스 flag (`69058d2`)
- Task 9: GarageScreen UI + Cutscene (`a2a00ba`)
- Task 10: ResultPanel '차고로' 버튼 (`c327645`)
- Task 11: ComplexTrack 씬 wiring — 4 GameObject + GameContext (`9ed1534`) — `Services.Restore=True Boost=True TireWear=True` ALL GREEN

**남은 Task (다음 세션 첫 작업)**:
- **Task 12**: 통합 verifier subagent — 12 항목 검증 (Services 매핑·6 부품 TryRestore·엔진 ratio·Gears.ManualUnlocked·Boost.IsUnlocked·Tires.SelectionUnlocked·TireWearStore 동작·CanAfford·Play 에러 0·integrity·자금 carryover)
- **Task 13**: session-log + activeContext 갱신 (이 파일)

**발견된 이슈 (다음 세션 결정 필요)**:
1. **TireType 실제 3종** (Soft/Medium/Hard) — D-091 기획 4종(드라이/웨트/소프트/하드)과 어긋남. Subagent가 3종으로 매핑 (Soft 1.5 / Medium 1.0 / Hard 0.5). 결정: (A) 3종 유지 (B) Dry/Wet 추가 (C) Medium→Dry rename + Wet 추가.
2. `IUpgradeSystem.OnWalletChanged` 인터페이스 미노출 → GarageScreen Update 폴링 우회. 잠재 부채.
3. `IUpgradeSystem.Spend(int)` Task 3 도중 인터페이스에 추가 (이전 누락).
4. Task 4: 코드 변수 `accelForce`라 plan `effectiveAccelTime` → `effectiveAccelForce`로 적응. +30% 가속 달성.
5. RunStage namespace 전역 — BoostSystem에서 `Services.Run as RunController` 캐스팅. 잠재 부채.

**manual play 빚 (차고 흐름 한 번도 안 굴림)**:
- 정산 → "차고로" 클릭 → GarageScreen 진입
- 엔진 복원 후 다음 런 속도 체감
- K/L 수동 변속 작동
- Space 부스트 (게이지 UI HUD 없음 — 후속)
- 펑크 = 잡 실패 트리거 (마모 30% 강제 후 굴려보기)

**다음 세션 첫 행동**:
1. Task 12 dispatch (verifier subagent) — ALL GREEN 확인
2. Task 13 (session-log + activeContext)
3. manual play 5~10분 — 차고 전체 흐름
4. TireType 4종 결정 (도현 답변)
5. R1 맵 적용 + RunMetricsRecorder 5런
6. 측정 결과 보고 가격 재튜닝 여부

---

## (구) 다음 세션 첫 작업 — 맵 넓히기

세션 27이 차고로 피벗했음. 맵은 측정 단계에서 적용 (이미 R1 spec 완료: `docs/superpowers/specs/2026-05-16-map-expansion-r1-design.md`).

1. **R1 맵 적용** — 도시 17배 (78×54 → ~330×230 어림). 큰 작업이라 brainstorm 한 라운드 후 진입 권장:
   - zone 3개 (다운타운/항구/공업) 영역 어떻게 분할
   - 도로 자동 배치 (`Assets/Editor/_Recovery/` 의 BuildingPlacer/RoadBuilder 패턴, asset.md 규칙 준수)
   - 배달지 8개 → 더 멀게 재배치 + zone별 분포
   - 마커 간 평균 거리 목표: ~500m (한 잡 1~2분)
   - roadMask 수정 → `TrackConnectivityVerifier` 자동 실행 의무 (asset.md)
   - heightMask 도입 시 slope tile만 사용 (D-050)
2. 맵 확장 후 reward/잡/zone unlock cost 모두 재튜닝 필요 (현재 8초 기준이라 17배 시간이면 보상도 비례).
3. **다시 RunMetricsRecorder 측정** — 맵 넓힌 후 5런. 한 런이 1~2분으로 늘면 그 다음에 escalation 박을 시간 생김.
4. **이후 순서 (도현 직관 기반)**: 맵 → 측정 → escalation brainstorm → 차고/부품 → 화물 비주얼.

### 미해결 / 메모

- **세션 26 manual play 빚 (여전히)**: 채널 5 Plan 1/2/3 + 채널 8 풀 흐름 + 폴리시 4영역 + Q/E 회전. 맵 넓히기 전 한 번 굴려보면 더 정확한 base.
- **데모 컨셉 갭**: 한 런 8~12초 vs 목표 5~15분. 200~900배 차이. **거리 확장 + escalation 둘 다 필요** (거리만으론 부족).
- 도현 결단 회피 패턴 — 큰 결정을 1시간 prototype으로 쪼개기. 맵 확장도 "33×23 → 일단 절반 (160×110) → 측정 → 더 늘림" 단계로.
- 채널 5 슬라이드 튜닝, 미니맵 목표 강조 (영역 3에서 일부 처리됨), 화물 비주얼 회색 큐브 — 세션 26 status audit 참조.
- `EditorApplication.Step()`은 에디터를 멈춤 → 런타임 거동 검증 전 frameCount 확인 ([[feedback-unity-step-pauses]]).

### 미해결 / 메모

- **채널 5 슬라이드 튜닝**: velocityDeadZone(0.02)이 약한 사면(25°)의 프레임당 증분보다 커서, 약한 슬로프에선 화물이 안 미끄러짐. 운전 G(급제동·코너)면 작동. 옛 단일 박스 코드도 동일했던 **선재(先在) 튜닝 이슈** — Plan 1에서 안 건드림. 데모 슬로프에서 화물 안 미끄러지면 deadzone 낮추거나 accelScale 올릴 것.
- 미니맵: 배달 **목표** 마커가 다른 마커와 구분 안 됨 — 폴리시 때 강조 (W4+).
- ResultPanel 라벨↔값 간격 큼 — 도현 눈으로 확인.
- 포장 박스가 밋밋한 회색 큐브 — 폴리시 (W4+).
- `EditorApplication.Step()`은 에디터를 멈춤 → 런타임 거동 검증 전 frameCount 확인 ([[feedback-unity-step-pauses]]).

---

## 잠재 부채 (Phase 7~9 — 데모 일정에 따라 보류 또는 인터리브)

- `IRunController` 추상화 새는 곳 — `as RunController` 캐스팅 4-5곳 (Phase 7)
- `UpgradeSystem.Instance` 직접 접근 `ResultPanel.cs:365,411` (Phase 7)
- ResultPanel 499라인 God class 분할 (Phase 8 — W3 F1 복원 메타 작업 중 자연스럽게 손볼 수 있음)
- 4 HUD TextHUDBase 통합 (Phase 9 — W5 폴리시 때 인터리브 가능)

---

## 세션 29 (2026-05-20~21): 화물 원물화 B안 — 와인 파일럿 GREEN

**문제(도현)**: 화물이 포장 전부터 카드보드 박스라 포장이 무의미. → **원물(깨지기 쉬운)이 보이고 버블랩으로 싸는 게 보여야** 의미.
**결정**: B안(원물 유지+랩핑) / Meshy 생성 / 버블랩 / 1번 파일럿(와인) 먼저, UV 엉키면 2번(숨긴 프록시) 폴백.
spec `docs/superpowers/specs/2026-05-20-cargo-raw-goods-design.md`, plan `docs/superpowers/plans/2026-05-20-cargo-raw-goods-wine-pilot.md`.

**커밋**:
- `36cac5c` CargoModelCatalog SO (CargoType→원물, Box/미스=큐브 폴백)
- `c4fb963` Meshy 와인병(refine→remesh 27k) + `scripts/meshy-generate.ps1` + **AssetPreviewTool**(중립 배경 1줄 렌더, Kenney side-by-side — [[feedback-asset-review-before-integration]])
- `88bd787` Cargo_Glass 프리팹(초록 유리 #0C4721, root→정규화 자식) + 카탈로그 와이어
- `bab5125` SpawnItem 카탈로그 조회 + WirePainterFor 자식 메시 콜라이더
- `0cca578` 작업대 와인병 크기 0.5→1.0

**검증/버그fix**: 검증 서브에이전트 6/6 통과했으나 **false-GREEN** — DrawStrip(RT 우회)만 봐서 실제 브러시 리본 미검증. 도현 manual play로 "포장 안 됨" 발견. root cause: 프리팹 정규화를 **메시 52x transform 스케일**로 박아 WrapRibbon 미터 임계(부모-로컬=큐브1:1 가정) 깨짐→리본 0. fix(`a139609`): import scale로 메시 1유닛 baking(globalScale 0.523), CargoCatalogSetup에 NormalizeImportScale 멱등 추가. **실제 BeginStroke 경로 재검증 GREEN**(colLossyScale 1.0 / ribbonVerts 525 / 버블랩 병에 감김 스샷 confirm). 도현 OK. incidents.md 등재(verifier 우회=false-green 패턴).

**아트 교훈**: Meshy 사실적(흰 100k)은 Kenney 로우폴리 월드와 결 안 맞음 → remesh 저폴리 + **쨍한 단색**이 톤 핵심. "유기물=Meshy"는 화물(Kenney 옆)엔 재검토. 자산은 통합 전 중립 검수(AssetPreviewTool). [[feedback-asset-review-before-integration]] / [[feedback-one-decision-surface]].

**4종 완성** (`a1feb87`): Glass 와인병 / Eggs 계란판 / Fish 생선 / Cake 케이크 — 다 Meshy→remesh→import정규화→단색→프리팹→카탈로그. Box는 폴백(큐브). CargoCatalogSetup 데이터 테이블(`b7e8e5e`), meshy-generate.ps1 remesh 포함. 3종 다 실제 브러시 경로로 wrap 검증(verts>0). 도현 룩 승인.

**남은 것 (후속 폴리시)**:
- 계란판 흐물흐물(Meshy 구조물 약) — 교체 후보.
- 생선 작업대 정면뜸 — 모델 기본 yaw 90도.
- 포장 작업대 **받침 plane**(원물이 허공에 뜸).
- 버블랩 질감(올록볼록 노멀)·병 어깨 리본 벌어짐·텍스처(라벨/유리).
- 트렁크 배치/운전 중 원물 실제 굴러가는 거 manual play(채널5 다물건).
- **Meshy 잔액 ~2050**.

## 미커밋

없음.

---

## 자주 참조하는 외부 문서

- USP·컨셉: `옵시디언/GDD/overview.md`
- 결정 채널 9: `옵시디언/GDD/decision-channels.md`
- 결정 이력: `옵시디언/notes/decisions-log.md`
- 시스템 스펙: `옵시디언/GDD/systems/`
- 학습 노트: `옵시디언/GDD/architecture/phase{1..6}-*.md`
- 데모 spec: `옵시디언/dev-plan/2026-05-13-vertical-slice-design.md`

---

## 힌트 (다음 세션을 위한 메모)

- Unity 검증은 **반드시 unity-cli**. Editor 켠 채. batch/MCP 금지.
- "데모인데 풀게임 기능 다 넣자" — 마음가짐만. 실제 추가는 W1~W4 표 기준. 신규 시스템은 `ideas-parking.md` 직행.
- Brainstorming → writing-plans 흐름 따를 것 (Superpowers 스킬).
- 6주는 AI 가속해도 1인분 4개월치. 시각 폴리시 양보 가능, 코어 시스템 양보 X.
- 화물 손상 인과 변경 (D-068) — Play 테스트 필요. 박스 충돌 빈도 확인 후 boxWallDamageMul 튜닝.
