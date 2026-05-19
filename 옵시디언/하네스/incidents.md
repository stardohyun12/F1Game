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
