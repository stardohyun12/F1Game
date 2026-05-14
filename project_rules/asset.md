# 에셋/맵 자동 배치 작업 규칙 (D-024/D-026 교훈) [project]

- **신규 타일·프리팹 도입 시:** mesh 외곽 bounds만 보고 "정규화 완료" 판정 금지. 기존 타일과 **side-by-side 렌더 + 이음매 클로즈업** 의무 (`_Recovery/PreviewBend.cs` 패턴).
- **시각 검증은 톱다운 정사영 + 사이드뷰 두 축으로.** 톱다운만 보면 상하 뒤집힘 등 Y축 이슈 못 잡음 (세션 7 건물 땅 속 박힘 사례).
- **회전은 Y-yaw만 명시적으로 계산.** `Quaternion.FromToRotation(A, -A)`는 180° 축이 여러 해 → Unity가 X축 선택하면 상하 뒤집힘. `atan2(x, z)` 패턴 사용.
- **자동 배치 스크립트에는 post-build verification 포함 의무.** `BuildingPlacer.VerifyPlacements` 패턴 (euler X/Z 검사 + bounds.min.y 검사). 침묵 실패 방지.
- **맵 레이아웃은 ScriptableObject (`TrackLayout`)로 관리.** 코드 하드코드 금지. 여러 씬에 각자의 Layout SO.
- **roadMask 수정 시 `TrackConnectivityVerifier` 자동 실행 필수** (세션 13 D-038). `RunFullRebuild` Step 6에 내장. BFS로 고립/도달불가/End 자동 리포트. 수동: `PitCrew > Verify Track Connectivity`. End 개수가 의도와 다르면 즉시 확인.
- **heightMask 도입 시 (D-050)**: 도로 cell은 heightMask='.' (0) 유지 권장. 의도된 elevation은 slope tile (`n/s/e/w/=/+`) 사용. 일반 도로 cell에 height 값 넣으면 인접 도로와 단절 step 발생.

## 에셋 제약
**Kenney 에셋 라인업 안에서만.** 커스텀 모델 제작 금지. 미사용 모델 활용이 원칙 (industrial 21 + low-detail 14 + decor 6 = 세션 17에 41개 추가됨).
