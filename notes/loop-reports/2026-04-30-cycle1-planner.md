# Planner Report — Cycle 1 (2026-04-30 main)

## 세션 16 후속(A) 리팩터 후 상태

**완료된 큰 작업 (세션 15~16 + 후속):**
- Phase 1 시그니처(화물) 풀 구현 — 5종 + 3축 민감도 + Packing 4단계 + Trunk yaw-only + 충돌 카메라 shake.
- B 시스템 — 주문 보드(8개 풀 → 3 픽) + PreRunSetupPanel(포장 4 카드) + ResultPanel UGUI(1600×1000 + 화물 이야기 + 등급 + 업그레이드 5).
- TireSystem ArcadeCarController 통합(D-051), CargoWarningHUD 4-source 반응형(D-052), 컨트롤 4차 튜닝 안정(D-053, 85km/h).
- 인프라 — .gitattributes/LFS, asmdef 2개, conventions.md, ApplySession16Tuning 통합 메뉴(D-057), EventSystem 자동 보장(D-061).
- UI — 한글 fallback malgun 안정(D-054/060), Suburban Props + StreetLights + Bridge mask, OrderBoard 가독성(D-064).
- 후속(A) 리팩터 — 6 스크립트 책임 분리 + Obsidian 코드리뷰 vault(`notes/code-review/`).

**feature-list.md 즉시 체크 가능:**
- 2주차 "주문 A/B/C 선택 차이 체감" — PreRunSetupPanel + ResultPanel 동선으로 사실상 작동(체크포인트 평가만 남음).
- 3주차 "타이어 3종" — Soft/Med/Hard 효과 통합 완료(선택 UI는 잔여).

**잔여 항목 핵심 5개:**
1. **F2 Brake-to-Drift 입력 힌트** (2주차 UI/HUD)
2. **F1 죽은 조합 UI 가드** (PreRunSetupPanel — 자금 부족/임계 미달 카드 비활성)
3. **F3 무결성 등급 tick** (IntegrityHUD 100/80/60/40/20 단계 표시)
4. RoutePlannerPanel UI(3주차, NodeGraph 인스턴스 후)
5. 신호등/맵 고저차/Hub Scene — 모두 큰 작업, 본 루프 영역 밖

## 다음 세션 1순위 추천

**작업명:** F1 — PreRunSetupPanel 죽은 조합 UI 가드(자금 부족 + 화물 임계 미달 카드 비활성화 + 사유 툴팁).

**근거:**
- 도현이 cycle4 처치(`fix(orderboard): 카드 텍스트 겹침 + 포장 시스템 초기 선택 자금부족 문제`, 4d364cb 직전 3440c65)에서 자금부족 일부 처치 완료. 그러나 PreRunSetupPanel은 OrderBoard와 별 동선이라 UI 가드가 분산되어 있음. 한 모듈로 통합하면 "왜 이 카드 못 고름" 인식 즉시.
- ResultPanel prefab화는 도현 영역 — 손대지 않음.
- F2(드리프트 힌트)는 시각/입력 폴리싱이라 4주차 이후. F3(등급 tick)는 IntegrityHUD 시각 폴리싱 — 우선순위 더 낮음.
- 신규 시스템 무 — 기존 PreRunSetupPanel + OrderJob.minPackingTier 활용만.

**스코프 검증:**
- MVP 4주? YES — 2주차 "주문 A/B/C 선택 차이" 체크포인트의 실효성 보강.
- 3단계 루프? YES — "계획" 단계 UX 핵심.
- 신규 시스템? NO — 기존 인스펙터 필드 + Button.interactable 토글만.
- 비주얼 폴리싱? NO — 기능적 가드(왜 못 고름 텍스트).

## P0 후보 우선순위 (F1/F2/F3) 갱신

- **F1 (죽은 조합 UI 가드) — P0 최우선.** Cycle4 자금부족 fix 후속. PreRunSetupPanel 통합으로 OrderBoard ↔ Packing 동선 일관성 확보.
- **F2 (Brake-to-Drift 힌트) — P1 강등.** 도현 컨트롤 4차 튜닝 후 드리프트 자체가 안정. 입력 힌트는 튜토리얼 영역 → 4주차 이후도 가능.
- **F3 (무결성 등급 tick) — P2.** IntegrityHUD는 이미 색 변화 + flash 작동. tick 5단계는 시각 폴리싱(규칙3 위반 위험).

## 위험 플래그

- 본 루프(2026-04-30)의 사이클 1은 **Task Y(FindActiveCar 캐시)** 수행이 권장 시퀀스(loop-tasks.md 표). 본 planner 보고서는 "다음 도현 세션" 1순위만 제안 — 본 루프 코드 변경 영역과 분리됨.
- F1 구현 시 OrderJob 인스펙터 필드(현재 노출된 minPackingTier 등) 추가 의심되면 즉시 도현 결정 영역으로 회피.
