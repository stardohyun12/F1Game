# 에셋/맵 자동 배치 작업 규칙 (D-024/D-026 교훈) [project]

## 아트 제작·적용 승인 순서 (2026-10-13 사용자 직접 지시)

- 실제 아트 에셋 제작 전에 원화나 분위기·형태를 보여주는 그림을 먼저 제시한다.
- 사용자가 원화를 승인한 뒤에만 모델·텍스처 등 실제 에셋을 제작한다.
- 제작 후에도 게임에 적용하기 전에 미리보기를 보여주고 적용 승인을 받는다.
- 순서: 참고 자료 → 원화 → 사용자 승인 → 에셋 제작 → 적용 전 미리보기 → 사용자 승인 → 게임 적용.
- 제작 요청이나 방향에 대한 긍정을 이후 단계 전체의 포괄 승인으로 해석하지 않는다.


- **신규 타일·프리팹 도입 시:** mesh 외곽 bounds만 보고 "정규화 완료" 판정 금지. 기존 타일과 **side-by-side 렌더 + 이음매 클로즈업** 의무 (`_Recovery/PreviewBend.cs` 패턴).
- **시각 검증은 톱다운 정사영 + 사이드뷰 두 축으로.** 톱다운만 보면 상하 뒤집힘 등 Y축 이슈 못 잡음 (세션 7 건물 땅 속 박힘 사례).
- **회전은 Y-yaw만 명시적으로 계산.** `Quaternion.FromToRotation(A, -A)`는 180° 축이 여러 해 → Unity가 X축 선택하면 상하 뒤집힘. `atan2(x, z)` 패턴 사용.
- **자동 배치 스크립트에는 post-build verification 포함 의무.** `BuildingPlacer.VerifyPlacements` 패턴 (euler X/Z 검사 + bounds.min.y 검사). 침묵 실패 방지.
- **맵 레이아웃은 ScriptableObject (`TrackLayout`)로 관리.** 코드 하드코드 금지. 여러 씬에 각자의 Layout SO.
- **roadMask 수정 시 `TrackConnectivityVerifier` 자동 실행 필수** (세션 13 D-038). `RunFullRebuild` Step 6에 내장. BFS로 고립/도달불가/End 자동 리포트. 수동: `PitCrew > Verify Track Connectivity`. End 개수가 의도와 다르면 즉시 확인.
- **heightMask 도입 시 (D-050)**: 도로 cell은 heightMask='.' (0) 유지 권장. 의도된 elevation은 slope tile (`n/s/e/w/=/+`) 사용. 일반 도로 cell에 height 값 넣으면 인접 도로와 단절 step 발생.
  - **D-098(2026-10-13)로 범위 축소**: 위 규칙은 **마을 타일 격자 안에서만**. 마을 사이 해안도로는 타일이 아닌 스플라인 메시로 깔고 고도(언덕·내리막)를 넣는다. 격자와 스플라인의 이음새 높이는 맞춰야 한다.

## 에셋 제약
**Kenney 에셋 라인업 안에서만.** 커스텀 모델 제작 금지. 미사용 모델 활용이 원칙 (industrial 21 + low-detail 14 + decor 6 = 세션 17에 41개 추가됨).
