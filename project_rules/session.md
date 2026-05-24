# 세션 프로토콜 [project]

> unity-cli 섹션은 Unity 프로젝트 공통 → universal 승격 후보.
> 현재 경로(`C:/Users/...`)가 환경 특정이라 project 유지.

## 세션 시작 (자동 — SessionStart hook이 체크리스트 주입)

1. `옵시디언/notes/session-log.md` 마지막 블록 (지난 세션 한 일).
2. `.claude/activeContext.md` (대기 결정·이번 세션 목표·힌트).
3. `옵시디언/dev-plan/feature-list.md` (위치 확인).
4. 오늘 목표 1줄 확정 후 진입.

## 세션 종료 (수동 + Hook 보조)

1. `.claude/activeContext.md` 갱신:
   - 이번 세션 한 일 (요약 1~3줄)
   - 다음 세션 목표·힌트
   - 대기 중 결정 (D-XXX)
2. `옵시디언/notes/session-log.md`에 한 블록 추가 (한 일 / 다음엔 / 메모).
3. 중요한 결정은 `옵시디언/notes/decisions-log.md`에 D-XXX로 기록.
4. 기능 단위 git 커밋 (한 번에 하나).
5. **하네스 패스 (B 트랙)** — `옵시디언/창업/harness-usage-log.md`에 한 블록.

## B 트랙 — 하네스 (게임 작업과 나란히, 매 세션)

> A 트랙(게임) 작업이 B 트랙(하네스)의 재료. 두 트랙 동시 진행.

**게임 작업 중 (A 트랙이 트리거):**
- 헛짚음 발생 → 즉시 `옵시디언/하네스/incidents.md`에 4줄 블록 기록.
- 같은 패턴 incidents 3건+ → 해당 패턴 "검증됨" → draft-rule을 `project_rules/` 또는 `CLAUDE.md`에 **즉시 발효** (D-071: 형 검토 안 기다림, 사후 검토).
- 기존 10패턴에 없는 새 종류 → `옵시디언/하네스/patterns/` 새 파일.

**하네스 평가 (B 트랙 = "다른 팀", 게임 흐름 안 막음):**
- 발효된 규칙이 실제로 도움 되는지·잘못된 건 없는지 평가·정리·수정.
- 형 검토는 여기에 사후로 들어옴.
- A 트랙(게임 작업)은 평가를 기다리지 않음 — 켜고 계속 간다.

## 자기 검증 3종 — 코드 변경 commit 전 매번 (D-071 즉시 발효, 2026-05-15)

> 메모리에 self-verify 룰만 박고 같은 세션 내 4회 위반 → incident 등재 → 즉시 발효.
> 메모리는 다음 세션용 보조. 작업 루프 안에 강제 점검 단계가 있어야 한다.

코드 변경 후 commit 전 셋 다 통과해야 "동작함" 보고 가능:

1. **컴파일 클린**: `unity-cli editor refresh --compile` 후 `unity-cli console --type error,warning` 결과 0건.
2. **런타임 클린**: `unity-cli editor play --wait` 후 `unity-cli console --type error --since 10s` 결과 0건. 컴파일 통과해도 Input System ↔ UnityEngine.Input 같이 런타임에만 터지는 함정 흔함.
3. **시각/수치 검증**: 시각 변경이면 `unity-cli exec`로 RectTransform sizeDelta·worldCorners·LayoutGroup 옵션 등 수치 측정. ScreenSpaceOverlay UI는 screenshot 못 잡으므로 수치로 검증.

셋 중 하나라도 빠뜨리면 자체 발견 못 한다 (실제 사례: 출발 버튼 inset만 박고 "OK" 보고 → 카드 overflow는 수치 확인 안 했음).

큰 구조 변경(레이아웃·UI 시스템·다중 컴포넌트 상호작용)은 subagent dispatch로 진단 분리 권장 — 작성자가 못 본 것을 못 본다. 이번 출발 버튼 겹침이 그 사례 (`VLG.childControlHeight=false`로 컨테이너 rect가 clipping mask 안 되는 Unity 기본 동작).

## 검증 서브에이전트 — 씬/시스템 wiring 변경 시 매번 (D-073, 즉시 발효 2026-05-18)

> 세션 27 사례: 맵 R1 작업 중 GameContext 누락 / OrderBoard.panelRoot 누락 / PackingScreen 누락 / OrderJob-DeliveryPoint 라벨 미스매치 등 5건 연속 발생. 도현이 manual play 해서 알려줘야 발견. 자체 발견 0건.
>
> 원인: "fix 1줄 → commit → 다음" 루프로 속도 내며 도현에게 검증 부담 떠넘김.

**언제 dispatch**:
- 씬 GameObject 추가/삭제/wiring 변경
- ScriptableObject 데이터 변경 (자산 라벨/참조 등)
- 새 시스템 추가 또는 시스템 wiring 재구성
- Editor 스크립트 실행 (RunFullRebuild·SetupOrderSystem 등)

**dispatch 패턴** (fresh subagent, haiku/sonnet):
```
prompt: |
  Editor 켜져 있고 Play 가능 상태. 다음 시스템 wiring·데이터 정합 검증:
  1. unity-cli exec로 Services.* 14개 모두 set 여부
  2. 핵심 컴포넌트 prefab/SerializeField wiring (OrderBoard.panelRoot, cardsContainer, cardPrefab / DeliveryHUD.manager / GameContext.* / 등)
  3. 데이터 정합: OrderJob.destinationLabels ↔ DeliveryPoint.Label 매칭
  4. unity-cli editor play --wait → 콘솔 에러 0건
  5. OrderBoard·RoutePlanScreen·OnConfirm reflection sim → orderedRoute.Count > 0, RC.RunActive=True
  결과 표 + 깨진 항목 좌표.
```

**작성자(나)는 OK 가정으로 commit 금지. 서브에이전트 보고 받고 fix → 재 dispatch → 클린 후 commit.**

작성자(나)와 검증자(서브) 분리 — 같은 사람이 자기 코드 검증 못 한다는 게 일관된 사례.

## 멀티에이전트 기본값 — 트리거 충족 시 도현 말 없이 자동 (D-074, 즉시 발효 2026-05-24)

> 문제: 멀티에이전트가 더 나은 줄 알면서도 도현이 "멀티에이전트 써"라고 말할 때만 씀. 안 그러면 혼자 처리.
> 원인: ① 이번 턴 즉시성 최적화(혼자가 결과 빨리 보임) ② 내 눈 과신(직접 쥐어야 안심 — 5.0 과대평가와 같은 뿌리) ③ 기본 세팅이 "혼자 답하는 비서"라 강제 없으면 튕김.
> 교훈: 자기검증(D-071)·시각판정(외부 눈 리뷰어)과 같은 부류 — 메모리만으론 안 고쳐짐, 루프에 트리거가 있어야 켜진다. [[feedback-orchestrator-mode]]

아래 트리거 중 하나라도 걸리면 **도현 지시 없이 내가 먼저** 켠다:

| 트리거 | 자동 행동 |
|---|---|
| 화면(맵·UI·카메라·마커·HUD·룩) 바꾼 commit 직전 | 외부 눈 UX 리뷰어 (컨텍스트 0, `docs/harness/ux-reviewer-prompt.md`) |
| 씬 GameObject/wiring·SO 데이터·시스템 변경 | 검증 서브에이전트 (D-073) |
| 서로 독립인 일 2개+ 동시 가능 | 병렬 에이전트로 분배 (한 메시지에 multiple dispatch) |
| 진단/분석/리서치가 큰 덩어리 | 작게 쪼개 여러 에이전트 (no-compromise §5) |
| 시각/품질 판정이 필요한 산출물 | 작성자(나)≠판정자 — 다른 에이전트가 판정 |

원칙: **혼자가 편해서 안 켜는 것 금지.** "이건 내가 빨리 하면 돼"가 떠오르면 위 트리거 점검부터. 단 Unity Editor는 1개라 Play 검증은 직렬(병렬 Play 금지). "어느 옵션?" 류 질문 대신 트리거 걸리면 그냥 켜고 진행.

## Unity 검증 — unity-cli (Editor 켠 채로) 필수

씬/컴파일/물리/로그/Play 검증은 항상 `unity-cli`(`C:/Users/stard/AppData/Local/unity-cli/unity-cli.exe`)로 한다. **Editor 켜둔 상태에서 실시간 통신** — Editor 닫기 요청 금지, batch 사용 금지.

설치: 바이너리 + Unity 패키지 `com.youngwoocho02.unity-cli-connector` (이미 manifest 등록). Editor 열면 자동으로 포트 8090에 HTTP 서버 뜸.

자주 쓰는 패턴:
- 상태: `unity-cli status`
- 콘솔: `unity-cli console --type error` / `--type warning` / `--type log` (콤마 조합 가능, default는 셋 다)
- C# 즉시 실행: `unity-cli exec "return ...;"` (return으로 결과 받기)
- 메뉴 트리거: `unity-cli menu "PitCrew/Fix/Diagnose Floating"`
- **재컴파일 (블록 끝까지)**: `unity-cli editor refresh --compile` ← 이거 쓰면 sleep + 포트 변경 fail 안 남
- Play 테스트: `unity-cli editor play --wait` → 측정 → `editor stop`
- 스크린샷: `unity-cli screenshot --view game --output shot.png`
- 도움말: `unity-cli list` / `unity-cli <cmd> --help`

**금지 패턴**: `exec "AssetDatabase.Refresh()"` + sleep + retry. → 항상 `editor refresh --compile` 한 줄로.

규칙:
- "Editor 닫아줘" 요청 금지 — unity-cli는 Editor 켜둘 때만 작동.
- 정적 분석으로 Unity 결과 추측 금지 — 반드시 unity-cli로 검증.
- 진단/수정 스크립트는 `Assets/Editor/_Recovery/`에 두되, [MenuItem] 노출 권장 (`unity-cli menu`로 호출).
- 폐기됨: Coplay MCP (`mcp__coplay-mcp__*`) / Unity batch mode (`-batchmode -executeMethod`).
