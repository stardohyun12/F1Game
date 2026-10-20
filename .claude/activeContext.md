# Active Context — PitCrew (지금 상태 한 화면)

> 매 세션 자동 로드(CLAUDE.md @import). **상태만, 짧게.** 지난 기록은 `옵시디언/notes/activeContext-archive-2026-10-20.md`(세션 18~34 원문)와 `옵시디언/notes/session-log.md`.
> 결정은 `옵시디언/notes/decisions-log.md`(D-번호), 팀 운영은 `PIPELINE.md`.

## 지금 (2026-10-20 밤, AionUi 팀 모드)

- **팀**: PD · 기획 · 프로그래밍(생성기) · 아트(Codex) · QA(Codex) · 리서치. 에디터 토큰 1개. 운영 PIPELINE §10·§11(전역 규약 보충).
- **목표 = D-116**: 상자 A(아말피→콘카, OSM 길·건물 + DEM ×0.3)를 실제 배달 맵으로 만들고 **메인 씬 교체**(ComplexTrack 은 백업). 루트 B(윗길 고리), 배달지 30. 스펙 `docs/superpowers/specs/2026-10-20-amalfi-map-game-design.md`, 계획 `docs/superpowers/plans/2026-10-20-amalfi-map-game.md`.
- **끝난 조각**: 10 OSM 건물(D-114) · 11 중심 길·광장 · 12 차로·광장 입구 정차(D-115) · 13 길 폭=건물 틈, 겹침 0 · 14 길 한 장(합집합−건물), z-fighting 0 · 15a 윗길 9,850m(R1 +45.7% FAIL, M3·M7 FAIL).
- **진행 중**: 15a-2(태스크 01a156a6) 실제 OSM 콘카 입구(R1 +15.7% 예상)·경계 잇기·헤어핀 ≥8m·콘카 안 차도 정리. 윗길 마을 = A(콘카 바로 위) 가정 — **도현 확인 대기**.
- **다음**: 15b → 8 성능 → 9 주행 시간 → 16 가게+배달지 30 → 17 배선(씬 경로 인자 메뉴) → 18 주문·구역·카메라 → 19 회귀 → 20 메인 씬 교체.
- 씬 `F1/Assets/Scenes/TownGen/TownGen_Structure_v15*.unity`, 상세 `tools/town-gen/STRUCTURE-*.md`. 스쿠터·걸어서 배달 = ideas-parking.

## 커밋 상태

- 커밋됨: BUG-002 `6b826249`(ZoneUnlock 배선·중복 카드·회귀 가드), 문서 `c7d72de7`, 훅 `d09d674b`, E0 roadgen·OSM/DEM 캐시 `8b1cb9a5`. **push 안 함.**
- 미커밋: TownGen 코드(`F1/Assets/Editor/TownGen/`, `Data/TownGen/`, `Scenes/TownGen/`), `tools/town-gen/`, docs/qa(spline), 스펙, 이 정리.

## 도현 판단 대기

- 없음(마을 완성 후 씬 뷰 판정 1회 예정). 보류 결정: 구역별 주문 재분배(나중), "다시" 차 위치·가게 복귀(D-109 보류), 새 F1 차 원화(전장 5.0~5.5m, 원화 때 승인).

## 알아둘 함정

- 폰트 SDF 3개(`KoreanFallback`·`Malgun`·`LiberationSans Fallback`)는 Play 할 때마다 TMP 가 바꾼다 → 커밋에서 빼고 `git checkout --`.
- 이 노트북 `scripts/ucli.sh` 는 HTTP 대체(exec/play/stop/console/status 만). 메뉴는 exec 로 `EditorApplication.ExecuteMenuItem`.
- Play 입력: 에디터 포커스 없으면 키 무시(`PointersAndKeyboardsRespectGameViewFocus`). QA 합성 입력 도구 `docs/qa/tools/`.
- 맵 리빌드 후 **시스템 복원 8단계**(보관본 "시스템 복원 체인", 8번 = `PitCrew/W1/Wire Scene (Zones)`). 회귀 `CheckCoreServicesPresent` 10개가 지킨다.
- 회귀 기준선: 12 PASS / 1 FAIL(BUG-001 화물 고폴리, 기존).
- AionUi 에선 **PD 턴이 끝나면 그 자체가 도현 알림** — 도현 판단이 필요 없으면 턴 안에서 기다린다.
