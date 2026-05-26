# Incident Log

> AI가 못 잡거나 잘못 처리한 사례. 한 줄씩 누적. 분류·정제는 나중에.
>
> **형식** (매 incident 4줄):
> - **무슨 일**: 표면 증상 한 줄
> - **AI 어떻게 잘못함**: 무엇을 추측·놓침·잘못 가정했나
> - **어떻게 잡았나**: 사용자가? hook이? manual play? compile error?
> - **관련 패턴**: `patterns/0X` 또는 `new` (새 패턴 후보)
>
> 3건 이상 같은 패턴이면 → `patterns/`로 승격 또는 기존 패턴에 사례 누적.
> 컨펌 후 자동화 적용 시점에 룰 정당화 자료로 쓰임 → [[draft-rules]].

---

## 2026-05-13 — Stop hook false positive (`unity-cli console --errors` 존재 안 함)

- **무슨 일**: Stop hook이 정상 turn 종료 시 "Unity 컴파일 에러" block 트리거. 실제 출력은 `[GameContext] 13/13 시스템 등록 완료` 같은 Log 레벨 메시지뿐.
- **AI 어떻게 잘못함**: `.claude/rules/session.md`와 `hooks/stop.sh` 둘 다 `unity-cli console --errors` 사용. 그런 플래그 없음. unity-cli는 unknown flag 무시하고 default(`--type error,warning,log`)로 떨어짐 → Log/Warning까지 전부 반환 → hook noise 필터(HTTP server 한 줄만 거름) 통과 → block. AI가 처음 룰 박을 때 unity-cli `--help` 안 보고 추측한 명령으로 박았음.
- **어떻게 잡았나**: hook이 실제로 발동한 첫 turn에 false positive로 잡힘. 출력 자체가 에러 아닌 Log임을 보고 `console --help` 확인 → 플래그 이름 다름 발견.
- **관련 패턴**: `new` (CLI 룰 박을 때 `--help` 검증 안 한 패턴, 후보)

## 2026-05-13 — 트렁크 캠/wrap/UI 3개 manual play 없이 발견

- **무슨 일**: 포장 미니게임 17 Task 구현 직후, 도현이 manual play 없이 코드만 보고 트렁크 캠 안 꺼짐 / wrap된 물건 안 쓰임 / UI 작음 3개 발견. AI는 "구현 완료" 보고 상태였음.
- **AI 어떻게 잘못함**: PackingScreen.cs Show/Hide에서 trunk cam disable 코드 짜놨지만 CCTV HUD(`CargoView`)는 별개 GameObject — 그 사실 안 챙기고 "rig 끄면 다 꺼지겠지" 추측. UI 크기는 1920×1080 기준만 보고 "표준이니까 OK"로 추측. wrap은 데미지 흡수만 mechanically 작동, 시각·비용 누락.
- **어떻게 잡았나**: 도현이 실제 플레이 안 했어도 코드 상상력으로 잡음. 도현 본인 코딩 감각 + 명시 보고 ("이거 안 꺼져 / 이거 작아 / 이거 안 쓰여"). AI 단독으론 못 잡음.
- **관련 패턴**: `patterns/01-visual-blindness`

## 2026-05-13 — UI "물건 1/3" 거짓말 (다물건 잡 폐기)

- **무슨 일**: PackingScreen 상단에 ◀ "물건 1/3" ▶ 표시. 실제로는 RunController.TransferWrapStateToCargo에서 `items[1+]` Destroy. UI N과 실제 처리 N 불일치.
- **AI 어떻게 잘못함**: "데모 단순화: items[0]만 cargo로 이관" 주석으로 자기합리화. UI는 미리 N 표시 만들어둔 거 그대로. 거짓말 알면서 두는 게 디버깅 시 혼선 유발.
- **어떻게 잡았나**: 다음 turn 진단 보고서 작성 시점에 코드 vs UI 차이 발견. 도현이 incident 보고 안 함 — 진단 작업 안 했으면 못 잡았을 것.
- **관련 패턴**: `patterns/06-multi-instance-edges`

## 2026-05-13 — `EnsureWrapOverlay`의 매 BeginRun `new Material` 누수 위험

- **무슨 일**: cargo box wrap 시각 오버레이 코드에서 `new Material(shader)` 호출. 박스 동일 인스턴스 재사용이라 실제 누수는 1회로 끝나지만, 패턴 자체가 "BeginRun마다 new" 위험 모양.
- **AI 어떻게 잘못함**: 처음 짤 때 cleanup 짝 안 챙김. 진단 보고서에서 "Material 누수 위험 Medium" 지적. AI 본인 단독으론 못 잡고 별도 진단 cycle에서 잡힘.
- **어떻게 잡았나**: 진단 subagent (Explore)의 보고서. profiler 없이 코드 inspect만으로 발견.
- **관련 패턴**: `patterns/08-perf-budget`

## 2026-05-14 — `EditorApplication.Step()` 부작용을 카메라 버그로 오진

- **무슨 일**: 점검 중 "운전 시 카메라가 차를 안 따라온다" 진단. 카메라가 오버뷰 포즈(48,110,6.5)에 멈춰 있음.
- **AI 어떻게 잘못함**: 스크린샷용으로 `unity-cli exec`에서 `EditorApplication.Step()` 호출 → 에디터 일시정지 + `Time.frameCount` 안 늘어남. 그 멈춘 상태에서 "FollowCamera는 enabled인데 안 움직임 → 카메라 둘 다 켜져 충돌? offset이 거대? GoToFollow 누락?" 가설 3~4개 세우고 코드·직렬화 필드 파고듦. 실제 원인은 **검증 도구가 게임을 멈춰놓은 것**. 도구의 상태변경 부작용을 게임 버그로 단정.
- **어떻게 잡았나**: AI 본인. `Time.deltaTime=0` 발견 → `frameCount` 두 번 찍어 안 늘어남 확인 → Step()이 범인임을 역추적. 도현 개입 없었지만 시간 꽤 날렸고, 안 잡았으면 멀쩡한 카메라를 "고칠" 뻔.
- **관련 패턴**: `new` — "검증 도구의 상태변경 부작용(pause·mutate)을 게임 동작 버그로 오진". 회피책: 런타임 거동(카메라·물리·이동) 검증 전 `frameCount` 증가부터 확인.

## 2026-05-14 — 자기 생성 probe 출력 포맷 3회 오독

- **무슨 일**: `unity-cli exec`로 컴포넌트 목록을 공백 join 출력 → `"Transform ZoneUnlockSystem"`, `"RectTransform CargoView"`, `"Transform IntegrityHUD"`를 "Transform만 있음 = 컴포넌트 미부착"으로 3번 오독. 하마터면 "ZoneUnlockSystem 컴포넌트 없음" 오보고할 뻔. 같은 turn에 "CargoWarningHUD가 모든 화면에 뜸"으로 도현에게 보고 — 실제론 IntegrityHUD. 컴포넌트 이름도 검증 없이 단정.
- **AI 어떻게 잘못함**: 자기가 짠 probe의 공백 구분 포맷을 자기가 헷갈림(둘째 토큰을 자식/속성으로 착각). 출력 한 줄을 끝까지 안 읽고 결론.
- **어떻게 잡았나**: AI 본인. 다른 probe(`FindObjectsByType<MonoBehaviour>`) 결과와 충돌 → 재검토. 도현 개입 없음.
- **관련 패턴**: `patterns/02-scene-inspector-wiring` (씬 상태를 간접 출력으로 추론 → 오독). 회피책: probe 출력은 명확한 구분자(줄바꿈·쉼표)로, 결론 전 raw 출력 끝까지 정독.

---

## 운영 메모

- 새 incident 발견 → 위 형식으로 한 블록 추가. AI에게 "incident 기록" 한 마디면 충분.
- 같은 패턴 3건째면 patterns 파일에 사례 누적 (예: `patterns/06`에 "PitCrew: 물건 1/3 거짓말 (2026-05-13)" 한 줄).
- 정기 정리는 안 함. 컨펌 시점에 한 번에 추출.

## 2026-05-15 — 자기 검증 룰 메모리에 박고 같은 세션 내 4회 반복 위반

- **무슨 일**: 한 세션 안에서 동일한 자기검증 누락 패턴 4회. (1) 트렁크캠 SerializeField 직렬화 함정 — 코드 default만 바꾸고 시각 검증 안 함. (2) Input System 패키지 모드인데 UnityEngine.Input.GetKeyDown 박음 — 컴파일만 보고 Play console 안 봄. (3) 출발 버튼 inset만 박고 "동작함" 결론 — 실제 시각 결과는 카드 overflow로 여전히 겹침. (4) [[feedback-self-verify-loop]] 메모리를 만든 직후 30분 안에 (2)(3) 다시 반복.
- **AI 어떻게 잘못함**: 메모리 박는 행위 ≠ 같은 세션 적용. 메모리는 다음 세션 SessionStart에서 로드되므로 작성 직후엔 anchoring 없이 그냥 작업 흐름 그대로 흘러감. 작업 루프 안에 강제 점검 단계가 없으면 룰은 종이짝.
- **어떻게 잡았나**: 도현이 "이런 경우가 없어야 되는데 팀 2개로 객관적 평가가 진행되고 있는 게 맞음?" 직접 문제 제기. AI가 self-check 못 함을 시인 + B 트랙 실제 안 굴러감 시인. 그 후 subagent dispatch로 출발 버튼 진짜 원인(VLG childControlHeight) 추적해서 해결.
- **관련 패턴**: `new` — "메모리에 룰 박는 것을 룰 발효로 착각". 회피책: 같은 세션 내 즉시 발효시키려면 (a) 작업 직후 별도 subagent로 검증 dispatch, (b) Play+console+visual 3종을 매 commit 전 강제, (c) draft-rules.md/CLAUDE.md에 즉시 발효 (메모리는 보조).
- **검증됨? 즉시 발효?**: 동일 세션 4회 = D-071 기준 즉시 발효. 아래 룰을 project_rules/session.md에 추가:

  > **자기 검증 3종 — 코드 변경 commit 전 매번**
  > 1. `editor refresh --compile` 후 `console --type error,warning` 검사 (0건 확인)
  > 2. `editor play --wait` 후 `console --type error --since 10s` 검사 (런타임 에러 0건)
  > 3. 시각 변경이면 `screenshot` 또는 unity-cli exec로 의도한 결과 측정 (sizeDelta·worldCorner·LayoutGroup 등 수치 검증)
  >
  > 셋 다 통과 안 하면 "동작함" 보고 금지. 메모리에 룰 박는 것으로 대체 불가.

## 2026-05-19 Day 2 — namespace 추측 fail

- **상황**: GarageScreen에 `using PitCrew.Core;` 추가. Services 접근 위해.
- **헛짚음**: PitCrew.Core namespace 존재 안 함. Services는 전역.
- **결과**: commit fc183a1 컴파일 에러. 후속 9643e49 hotfix.
- **원인**: 룰 §1 위반 — commit 전 `refresh --compile` 안 돌림. 추측으로 using 박음.
- **개선**: §7 체크리스트 항목에 "commit body 작성 전 unity-cli console --type error 결과 0건 확인" 명시 추가 후보.

## 2026-05-19 Day 7 — URP VolumeProfile sub-asset 저장 패턴

- **상황**: PitCrewPostFX.asset에 Bloom/Vignette/ColorAdjustments 추가. `profile.Add<T>(true)` + `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets` 호출.
- **헛짚음**: 디스크에 sub-asset 안 저장됨. profile.components 메모리는 3, 디스크 .asset 18라인 (component 0).
- **결과**: verifier가 발견 (도현 manual play 전). Day 1 PostFX 효과 미적용 상태였음 (이전 'PostFX 적용됨' 보고는 메모리만 검증).
- **원인**: URP 17 / Unity 6에서 `profile.Add<T>()` API가 메모리만 처리. sub-asset 디스크 영구화는 `AssetDatabase.AddObjectToAsset` 별도 호출 필요.
- **개선**: no-compromise §3 도구 점검 룰에 "ScriptableObject sub-asset 변경 시 `AddObjectToAsset` 명시 + 디스크 라인 수 grep 검증" 후보.

## 2026-05-19 Day 7 — 잘 작동한 룰 (success log)

- **상황**: Day 1~6 sprint 자체 진행. 매 Day 끝 commit + verifier 호출.
- **잘 된 점**: §1 검증 셋 통과 의무 → verifier가 Day 1 PostFX FAIL 자체 발견. 도현 manual play 전 잡음.
- **잘 된 점 2**: §5 진단 분해 → 디스크 라인 수 grep으로 sub-asset 저장 실패 즉시 확인. 6 commit (Day 1 fix 1 + Day 2 hotfix 1 + Day 1~6 본 작업 + PostFX fix) 작은 단위.
- **잘 된 점 3**: §8 영역 구분 → 자산 다운로드 부분(F1 머신·폰트·prop)은 자체 결정 X, polish-asset-candidates.md에 후보 정리만.

## 2026-05-21 — 와인병 버블랩 리본 안 보임 (verifier false-GREEN)

- **무슨 일**: 화물 원물화(와인병) 후 도현 manual play — "유리병 포장 이상함 잘 안 됨". 브러시로 싸도 버블랩 3D 리본이 병에 안 생김(리본 정점 0). RT 커버리지·손상 흡수는 됨.
- **AI 어떻게 잘못함**: (1) 프리팹 정규화를 메시에 **52x 스케일 래퍼**로 박음 → WrapRibbon의 미터 임계(minSampleDist 0.012·width 0.18·lift, 전부 **부모-로컬 기준 = 큐브 1:1 가정**)가 52배 어긋남 → 스트로크 전체가 한 샘플 거리도 안 돼 세그먼트 0개. (2) 검증 서브에이전트가 `DrawStrip`(RT 직접 blit)+ComputeCoverage/QueryAbsorption만 봐서 GREEN — **실제 입력 경로 BeginStroke→3D 리본은 우회**. PILOT GREEN이 false였음.
- **어떻게 잡았나**: 도현 manual play. AI 자체 검증(서브에이전트 포함) 못 잡음 — 우회 경로로 통과.
- **Missing-heuristic**: ① 인터랙티브 시스템은 **실제 입력 메서드**(BeginStroke/UpdateStroke)를 시뮬+스샷으로 검증, 우회 호출(DrawStrip) 금지. ② transform-scale에 의존하는 시스템(리본 metric)이 있을 때 자산 **스케일 정규화는 transform이 아니라 import scale로** (부모 lossyScale≈1 유지).
- **관련 패턴**: `patterns/01-visual-blindness` + new(verifier 우회 경로 false-green)

## 2026-05-21 — 화물 작업대 위로 뜨고/잘림 + 회전 중심 안 맞음 (피벗 불일치)

- **무슨 일**: 케이크 등 화물이 포장 작업대에서 화면 위로 떠 잘리고, 배치 단계 회전 시 중심이 안 맞고 빙 돎. 도현 manual play.
- **AI 어떻게 잘못함**: 프리팹 정규화 시 피벗을 **바닥-중심**(`-b.min.y`)에 둠. 근데 포장 카메라는 spawn(=피벗)을 바라보고 회전(`transform.Rotate`)도 피벗 기준 → 바닥-중심이면 객체가 위로 뻗어 프레임 이탈 + 회전이 0.5유닛 궤도. 시스템이 설계된 **유닛 큐브(중심 피벗)** 가정과 불일치. 와인병(세로 길쭉)만 봐서 안 드러남, 납작한 케이크에서 폭발.
- **어떻게 잡았나**: 도현 manual play. AI는 큐브로만 검증해 피벗 불일치 못 봄.
- **Missing-heuristic**: 기존 시스템이 암묵 가정(pivot=중심, scale=1유닛 큐브) 위에 설계됐으면 **새 자산을 그 가정에 정확히 맞춰야**. 자산 교체 시 배치·회전 등 transform 거동을 새 자산으로 재현 테스트(특히 모양 비율 다른 것).
- **관련 패턴**: `patterns/01-visual-blindness` + (자산이 시스템 암묵 가정 위반)

## 2026-05-21 — 케이크 속 뚫림 + 게임 결과 안 맞는 고퀄

- **무슨 일**: 케이크가 속 뚫려 보이고 우리 로우폴리 게임과 안 맞는 고퀄리티(도현 manual play).
- **AI 어떻게 잘못함**: Meshy remesh로 저폴리화 시도(target_polycount 1~2000)했으나 Meshy가 무시 → 케이크 54k·생선 75k 등 여전히 고퀄. 케이크 melty 디테일은 얇은 지오메트리라 backface culling으로 속 비어 보임. "단색 틴트 입히면 Kenney 결 맞겠지"로 **fidelity(폴리·디테일) 갭을 과신** — 색은 맞췄지만 디테일 수준이 안 맞음. 실제 remesh 결과 폴리수가 target과 다른 걸 확인 안 함.
- **어떻게 잡았나**: 도현 manual play. AI는 단색 side-by-side 정지컷만 보고 OK 판단(회전·속 안 봄).
- **Missing-heuristic**: ① 자산 fidelity(폴리수·디테일)가 타깃 아트와 맞는지 **수치(폴리수)+다각도/회전 시각** 능동 점검 — 단색만으론 갭 안 메워짐. ② Meshy remesh는 target_polycount 보장 안 함 → 결과 폴리수 검증, 안 맞으면 다른 수단. ③ "두 번 봐도 OK" 정지컷 ≠ 회전/내부 OK.
- **관련 패턴**: `patterns/01-visual-blindness` + (자산 fidelity 불일치). 같은 아트-결 incident 누적 — `feedback-asset-review-before-integration` 강화 후보.

## 2026-05-24 — 맵이 명백히 이상한데 AI는 "검증 통과"로 보고 (시각 자기판단 회피)

- **무슨 일**: 맵 구조 재디자인 후 톱다운이 누가 봐도 이상함(블록이 회색 void에 떠있는 토이 그리드, 도로 안 읽힘, 바다 어색). 근데 AI는 "322도로 연결·338건물·에러0 검증 통과 + 회색은 정상(아트 단계)"로 보고. 도현이 "충분히 이상한데 왜 못 알아채냐" 지적.
- **AI 어떻게 잘못함**: 성공을 **수치 게이트**(connectivity/카운트/에러0)로 판정하고, 시각 이상함은 "아트 톤=도현 영역(§8)"으로 **떠넘김(deflection)**. §10대로 스샷 찍고 Feels-off도 적었지만 — 사소한 점만 적고 *전체가 이상하다*는 큰 판단은 회피. 자기 결과물을 "사람이 보면 이상한가" 기준으로 안 보고 "체크 통과했나"로 봄. 룰의 글자만 따르고 정신은 놓침.
- **어떻게 잡았나**: 도현. AI는 스샷을 보고도 "그레이박스라 정상"으로 합리화.
- **Missing-heuristic**: 시각 산출물은 보고 전에 **"사람이 이걸 이상/깨졌다고 부를까?"를 먼저 자문**하고, YES면 수치 통과여도 "이상하다"고 명시 + 원인 파고들기. 시각 문제를 "아트 단계/네 영역"으로 떠넘기지 말 것(글자 그대로의 Feels-off ≠ 정직한 전체 판단). 그레이박스라도 "토이 그리드/void에 뜸/스케일 없음"은 구조·연출 문제지 색 문제 아님.
- **관련 패턴**: `patterns/01-visual-blindness` (3건+ 누적: 화물피벗·케이크fidelity·맵). **누적 충분 → 룰 발효 후보**: "시각 commit 전 전체 인상 1줄 판정(정상/이상)" 강제, 또는 검증자 서브에이전트에 '낯선 눈 인상' 항목.

## 2026-05-25 — 차 도로에 묻힘 + 도로선 공중부양 (땜빵 스크립트가 수직 기준을 망가뜨림)

- **무슨 일**: 도현 manual play "차가 다시 바닥에 묻힘". 추가로 "도로 선이 공중에 떠있고... 내 생각대로 안 만들어지네, 하네스 문제냐". 차 바퀴가 도로(표면 0.11)에 0.25 묻히고, 도로 중앙선이 도로면 위 8cm 떠 있었음.
- **AI 어떻게 잘못함**: ① **수직 기준(땅 높이) 단일 계약이 없음** — 맵을 스크립트 여러 개로 쌓는데 각자 Y를 제멋대로 박음(잔디 -0.05 / 도로표면 0.11 / 선 0.19 / 물리바닥 -0.05, 반면 건물·프롭·보도는 전부 0). 도로 타일엔 콜라이더가 아예 없어 차는 잔디 평면(-0.05)에 떨어지는데 도로는 0.11이라 묻힘. ② `_Recovery/_archive`에 `RaiseRoadTiles`·`FixGroundCarAlign`·`AlignGroundToTrack`·`FixRoadAsphaltZFight` 등 **같은 정렬을 고치려던 땜빵이 누적** — 각자 증상만 눌러서 서로 싸움(z-fight 피하려 도로 올림→차 정렬 깨짐→또 패치). 세션27의 spawn-Y 4회 헛질이 이 라인. ③ 선 placer가 도로 표면(0.11)이 아니라 **연석 꼭대기(0.17)를 기준** 삼아 LineY=0.19로 박음. ④ 시킨 범위(교차로 네모) 밖을 내가 판단해 바꿔(206타일·runController) "맘대로 바꾼다"는 불신 유발.
- **어떻게 잡았나**: 도현 manual play. 정적 톱다운만 보던 AI는 운전 시점 묻힘·선 뜸 자체 발견 0건.
- **Missing-heuristic**: ① 맵 수직은 **단일 기준(땅=0)** 하나로 — 모든 placer가 그 상수를 참조, 제각각 하드코드 금지. `AlignMapToGround`로 잔디·도로·선만 0 정렬(나머지 이미 0). ② **정렬 불변식 자동 점검** 필요: Play 중 "차 바퀴 Y ≈ 도로 표면", "도로선 Y ≈ 도로 표면" HarnessCheck — 이번 두 버그가 다음부턴 자동 FAIL로 잡힘. ③ "Raise/Fix/Align" 류 땜빵 스크립트가 3개+ 쌓이면 = 근본 계약 부재 신호, 표층 패치 말고 단일화. ④ **시킨 범위 밖 변경 금지/사전고지** — "하는 김에" 코드/씬 변경이 불신의 핵심.
- **관련 패턴**: `patterns/01-visual-blindness`(운전시점 미확인) + 새 종류 "수직 정렬 계약 부재 + 땜빵 누적". 차정렬 incident 누적(세션27 spawn-Y, 이번) → **불변식 HarnessCheck 발효 후보**.

## 2026-05-25 — 맵 리빌드가 8개 시스템을 조용히 유실 + 룰이 행동 못 바꿈(역할 분리로 전환)

- **무슨 일**: 도현 manual play "포장이 없는데", "경로 설정 안됨". 진단하니 세션31 맵 리빌드(d89a466)가 PackingScreen/GarageScreen/F1RestoreSystem/Boost/TireWear/Tire/Upgrade/CargoShakeSimulator 8개 GameObject를 씬에서 누락 → GameContext.autoFindMissing이 못 찾아 Services 조용히 null → 포장·차고·복원 흐름 통째로 죽음(에러 0). 도현이 플레이해야 발견.
- **AI 어떻게 잘못함**: ① 큰 구조변경(맵 리빌드) 후 "다른 시스템이 안 깨졌나"를 전혀 점검 안 함 — 목표(맵)만 보고 주변 헤집힘 무시. ② 세션 내내 "멀티에이전트 쓴다"고 룰에 있으면서 0개 디스패치하고 혼자 처리. ③ 정적 스샷 보고 "됐다" 자가판정(차 묻힘/선뜸/포장없음 다 도현이 발견). 셋 다 "작성자=판정자" + "룰은 메모리에만 있고 행동 안 바뀜"이 뿌리.
- **어떻게 잡았나**: 도현 manual play 3연속 지적 + "멀티에이전트 안 쓰잖아" 직격.
- **Missing-heuristic**: ① **풀 루프 시스템 존재 자동 단언**(CheckCoreServicesPresent) — 리빌드가 시스템 누락시키면 회귀에서 자동 FAIL. ② **역할 분리 운영모델 발효**(`project_rules/roles.md`): 코딩(나)은 기획·그래픽 게이트 에이전트 통과 전 "됐다"·커밋 금지. 룰("멀티에이전트 써라")을 게이트("통과 전 커밋 불가")로 전환 — 의지 의존 제거. ③ 큰 구조변경 후 "주변 무결성"을 기획 에이전트가 정적 검증.
- **검증된 효과(이번 세션)**: 에이전트 4개 실제 디스패치(감사·레시피·그래픽게이트·기획게이트), 커밋을 게이트 2개에 실제로 걸어 통과 후 커밋. 풀 루프 복원 + 회귀 12 PASS.
- **관련 패턴**: `patterns/01-visual-blindness` + 새 종류 "구조변경이 인접 시스템 조용히 파괴 + 룰→게이트 전환". [[feedback-orchestrator-mode]] 메모리 갱신 후보(룰→게이트).

## 2026-05-26 — PM 부팅을 수동 복붙로 설계 (도현 클릭 부담 떠넘김)

- **무슨 일**: PM 인스턴스 부팅 절차(BOOT-PM.md)를 "도현이 터미널 열고 페르소나 프롬프트를 복붙"으로 설계. 도현: "내가 터미널 열었는데 네가 PM 직접 만들어 열어주면 안 됨?" — 수동 복붙 단계 자체가 불만.
- **AI 어떻게 잘못함**: 사람-개입 단계를 설계하면서 도현의 수작업을 최소화하는 걸 기본값으로 안 둠. 도현 선호 "이런 거 말 안 해도 되게"가 반복 관측됐는데도, 부팅을 한 줄 런처가 아니라 복붙 의례로 만듦. (내가 다른 터미널 인스턴스를 직접 조종 못 하는 건 사실 — 하지만 그 한계와 "도현 클릭 1번으로 줄이기"는 별개. 한계를 핑계로 마찰을 방치.)
- **어떻게 잡았나**: 도현. AI는 BOOT-PM.md 복붙 절차를 "완성"으로 보고함.
- **Missing-heuristic**: **사람-개입 단계를 설계하면 기본값은 "원커맨드 런처/스크립트", 수동 복붙·다단계 절차 금지.** 내 능력 한계(다른 인스턴스 직접 조종 불가)에 부딪히면, "안 됨"으로 끝내지 말고 *남은 수작업을 1클릭으로 압축하는 우회*를 먼저 댄다(no-compromise §2). `pm.ps1`로 즉시 교정.
- **관련 패턴**: 새 종류 "사람 개입 단계를 수동으로 설계(자동화 가능한데)". [[feedback-docs-for-reader-not-self]] 인접 — 산출물·절차는 독자(도현)의 수고 기준으로.

## 2026-05-26 — PM 양방향 소통 채널 누락 (한 방향 backlog만 설계)

- **무슨 일**: 도현 "agents team이 서로 소통할 수 있다던데 너는 안 됨? 그럼 문제 있는 거". 점검하니 스튜디오 설계에 PM→backlog/브리프→메인 **한 방향**만 있고, 메인↔PM **양방향 메시지 버스가 없었음**. PM에게 질문·정정·재우선순위를 보낼 깨끗한 채널 부재.
- **AI 어떻게 잘못함**: 파일 버스를 "PM이 일감을 미는" 단방향으로만 설계. 협업은 본질적으로 양방향(질문/응답/조정)인데 그 채널을 안 깖. "직접 소켓 없음"(사실)을 "소통 설계 불필요"로 잘못 연결 — 한계와 미설계를 혼동(원커맨드 런처 incident와 같은 뿌리: 한계를 핑계로 마찰 방치).
- **어떻게 잡았나**: 도현. AI는 단방향 핸드오프를 "소통 설계 완료"로 간주.
- **Missing-heuristic**: **에이전트 협업 설계 시 양방향 메시지 채널을 기본으로** — 한 방향 핸드오프만 있으면 미완성. 독립 인스턴스는 직접 소켓이 없으니 *공유 append-only 버스*(둘 다 읽고 쓰는 파일)로 양방향 구현 + 각 에이전트의 작업 사이클 시작에 "버스 확인" 단계 박기. `docs/studio/bus.md`로 즉시 교정.
- **관련 패턴**: 새 종류 "협업 설계인데 단방향만". no-compromise §2(한계=핑계 금지) 인접.

## 2026-05-26 — 멀티에이전트 "왜 나는 안 됨" — 설명만 하고 안 보여줌

- **무슨 일**: 도현이 유튜브(여러 에이전트 생성·소통)를 보고 "나는 왜 안 되냐, 문제 있는 거 아니냐"를 반복 질문. 실제로는 오늘 세션에서 병렬 디스패치(구현 에이전트 2개 동시)·PM 페르소나 가동·양방향 버스를 다 했는데도 도현은 "안 된다"고 느낌.
- **AI 어떻게 잘못함**: 능력을 **텍스트로 설명**만 하고 도현이 *눈으로 보게* 시연한 적이 없음. 구조도·표·커밋 메시지로 "된다"고 말했지 에이전트들이 실제로 대화하는 장면을 안 보여줌. 능력 의심이 들 때 demonstrate가 아니라 explain으로 대응 → 의심 지속.
- **어떻게 잡았나**: 도현 반복 질문(3턴째 멀티에이전트 소통 의심).
- **Missing-heuristic**: **능력이 의심받으면 설명 말고 시연하라(show, don't tell).** "된다"는 주장 대신 그 자리에서 실제로 굴려 산출물(버스 대화·병렬 결과)을 보여준다. 특히 비기술 사용자에겐 텍스트 아키텍처 < 눈에 보이는 동작 1회. [[feedback-qa-review-full-loop]]·시각판정 회피와 같은 뿌리(보여줘야 믿음).
- **관련 패턴**: 멀티에이전트 소통 의심 3턴 누적(터미널 직접조종/양방향버스/이번) → **"능력 의심 시 즉시 시연" 규칙 발효 후보**.
