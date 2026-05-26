# 차고 시스템 통합 verifier — Task 12

- 현상: 차고 Task 1~11 구현 완료(커밋 5be654a~9ed1534). 그러나 통합 verifier(Task 12) 미실행. 차고 전체 흐름(정산→차고→엔진복원→다음런 가속체감, K/L 수동변속, Space 부스트, 펑크=잡실패)이 실제로 굴러가는지 manual play 포함 검증 0건.

- 원인 가설: 구현 후 검증 없이 다음 작업으로 피벗. Services 매핑·6 부품 TryRestore·타이어 선택 등 wiring 손실 가능성 있음 (세션 27 당시 발견된 이슈 3건: TireType 3/4종 불일치, IUpgradeSystem.OnWalletChanged 미노출, RunStage namespace 누수).

- 완료 조건 (측정 가능):
  1. `unity-cli exec`로 Services.Restore / Services.Boost / Services.TireWear 세 개 ALL SET
  2. 6 F1Part `TryRestore` 호출 → 각 PASS (CanAfford + 차감 + 효과 적용)
  3. 엔진 복원 후 다음 런 `effectiveAccelForce` +20% / +30% 수치 확인
  4. `Gears.ManualUnlocked = True` (변속기 복원 후)
  5. `Boost.IsUnlocked = True`, Space 입력 시 부스트 발동 확인
  6. 타이어 마모 30% 강제 → 펑크 → 잡 실패 트리거 확인
  7. `unity-cli editor play --wait` → console error 0건
  8. 회귀 하네스 12 PASS / 0 FAIL (CargoPolyBudget 제외)

- 관련 파일/메뉴:
  - Plan: `docs/superpowers/plans/2026-05-19-garage-restore-system.md` (Task 12 상세)
  - 씬: `F1/Assets/Scenes/ComplexTrack.unity`
  - 핵심 스크립트: `F1RestoreSystem.cs`, `GarageScreen.cs`, `TireSystem.cs`, `BoostSystem.cs`
  - 발견된 이슈 원본: `.claude/activeContext.md` "발견된 이슈" 섹션 (TireType 3종/IUpgradeSystem/RunStage namespace)

- 게이트: 화면(GarageScreen) + 시스템 wiring → 풀코스. 기획 게이트 + QA 실플레이(차고 전 흐름 1회 이상).

- 도현 결정 필요:
  - TireType 3종(Soft/Medium/Hard) vs 4종(Dry/Wet/Soft/Hard) — 현재 코드는 3종으로 구현됨. 데모에서 4종 필요하면 지금 추가, 아니면 3종 확정 후 닫기.
