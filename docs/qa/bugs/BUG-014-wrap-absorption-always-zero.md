# BUG-014 — 포장해도 주행 중 충격 흡수가 0 (포장이 손상을 못 줄임)

- 발견: 포장 설계 검토(2026-10-22, 서브 에이전트 정적 분석) → Play 로 확인.
- 원인: `RunController.TransferWrapStateToCargo` 가 트렁크에 싣는 화물의 콜라이더를 `DestroyImmediate` 로 지웠다.
  `WrapState.QueryAbsorption` 은 충격 면 UV 를 MeshCollider 레이캐스트로 얻으므로 콜라이더가 없으면 늘 0 → 포장은 돈·시간만 들고 효과 없음.
- 수정: 콜라이더를 지우지 않고 끈다(`enabled=false`), 조회 순간만 켜서 레이캐스트 후 다시 끔.
- 검증(Play, Box 를 버블랩 커버 64% 로 감고 출발): 트렁크 화물 5방향 흡수 0.25 (수정 전 코드상 0). 회귀 12 PASS / 1 FAIL(BUG-001 기존).
- 남은 것: Box 는 6면이 같은 UV 라 한 면만 감아도 전 방향 보호(설계서 §1.5). 회전·기울기 손상은 포장과 무관.
