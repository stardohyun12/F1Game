# 기능 목록 (Feature List) — 마스터

> **이 파일은 세션 조기 종료를 막는 장치.**
> 에이전트가 작업 시작할 때 먼저 보고 "다음에 뭘 해야 되지" 파악.
> 영상 기반: 앤트로픽이 클로드.ai 클론 프로젝트에서 쓴 패턴.
>
> **규칙:** 기능 하나 완료 = 체크박스 ☑ + git 커밋 + `session-log.md` 블록 추가.
> 아직 시작도 안 한 것과 진행 중인 것을 구분하려면 `[ ]` / `[~]` / `[x]` 사용.

---

## 1주차 — "차 한 대가 도로를 달리는 게 재밌는가"

### Day 1 — 세팅
- [x] Unity 6 신규 프로젝트 생성 (URP 템플릿, 폴더명 `F1/`)
- [x] Git 리포지토리 초기화 + .gitignore (`git init -b main` + 첫 커밋 완료)
- [x] Kenny Car Kit 다운로드 및 임포트
- [x] Kenny 3D Road Tiles 다운로드 및 임포트
- [x] 빈 씬에 테스트 차량 + 평면 배치 확인

### Day 2 — 차량 물리 MVP
- [x] `ArcadeCarController.cs` 작성 (Rigidbody 기반 아케이드) — `F1/Assets/Scripts/ArcadeCarController.cs`
- [x] 입력 시스템 설정 — 기존 `InputSystem_Actions.inputactions`의 Player/Move 액션 재활용 (WASD + 화살표 + 게임패드 좌측스틱 기본 바인딩). PlayerInput 컴포넌트 연결 완료.
- [x] 가속·제동·조향 기본값 세팅 — 스크립트 인스펙터 필드로 노출 (maxSpeed 60km/h, accelTime 2.5s, mass 100)
- [x] 테스트: 차가 움직이고 멈춘다 — GroundPhysics μ=0.15 적용 후 정상 주행 확인 (기록은 세션 4에서)

### Day 3 — 카메라 + 트랙
- [ ] `FollowCamera.cs` (3인칭 쇼울더 뷰, 부드러운 Lerp)
- [ ] 속도에 따른 FOV 변화 (속도감 표현)
- [ ] Road Tiles로 직선 + 코너 테스트 트랙 배치
- [ ] 테스트: 트랙을 자연스럽게 달린다

### Day 4 — 튜닝 1
- [ ] 가속/제동 곡선 튜닝
- [ ] 조향 민감도 속도 반비례 튜닝
- [ ] 카메라 코너 예측 뷰 튜닝
- [ ] Kenny Audio에서 엔진 루프 배치

### Day 5 — 드리프트 추가
- [ ] 드리프트 입력 (핸드브레이크 또는 브레이크+조향)
- [ ] 드리프트 로직 (사이드 그립 감소)
- [ ] 타이어 스키드 마크 (간단한 것)
- [ ] 드리프트 스모크 파티클 (Unity 기본 또는 Kenny)

### Day 6 — 트랙 확장 + 재미 테스트
- [x] 트랙 시스템 재구축 (세션 6) — TileMapAuto: 그리드 마스크 + 자동 추론(D-022). 현 레이아웃: 7×5 직사각형 루프. native size 정규화로 도로 폭 일치(D-021 정정). 언덕/S자는 재미 판정 후 마스크 수정으로 즉시 추가 가능.
- [x] 배달 시스템 MVP (세션 6) — DeliveryPoint/Manager/HUD + 시작→1→2→3→복귀 루프. 화면 상단 N/3 + 화살표 HUD.
- [x] **도로 폭 근본 수정 + Layout SO 시스템 + Building Phase 1 (세션 7, D-024/025/026)** — road-bend 교체(도로 폭 완벽 일치), TrackLayout ScriptableObject 외부화, BuildingPlacer (2-layer 마스크, 28 BuildingDefs, verification 내장), ComplexTrack 씬(9×7 더블 루프 + T교차로, 35타일+22건물), 차량 배치 자동화(Race.prefab + FollowCamera).
- [x] **세션 8 리밸런스 (D-027/028)** — Cell 6m, Race 0.7x, 건물 fit-to-cell, 멀티셀 2×2(K/X) + 자동 MeshCollider, 13×9 3×4 그리드(63타일+36건물), 건물 통과 버그 해결, 드리프트 2회 튜닝.
- [x] **세션 11 Lynch 5요소 리디자인 (D-033)** — 구역 분할(교외/상업/Downtown/산업), 랜드마크 4개(K 앵커 + T chimney + F×2 factory), 신규 mask char H/T/F/W, 배달점 7개 Node 배치, Industrial kit 11 BuildingDef 신규, URP 아트 프리셋(Bloom/Vignette/ACES/Fog/Warm Light). 36건물 all verified.
- [x] **세션 11 후반 T자 수정 + 도로 카탈로그 + 오픈월드 Tier A (D-034)** — Intersection 회전 매핑 실측 수정(rot 0→S/90→W/180→N/270→E). Kenny 도로 72 FBX 전수 카탈로그(GDD/research/kenny-roads-catalog.md). BuildingDef.addCollider 플래그 + Plaza 'P' 카탈로그. 지름길 4곳 + 광장 2곳 → 6 blocks → 12 subblocks 분할. 40 건물 all verified.
- [~] 30분 이상 직접 플레이 — 맵 복잡도/드리프트 톤 추가 조정 필요. 세션 9에서 기획 재검토 후 판정.
- [ ] 자문: "계속 놀고 싶나?"

### Day 7 — 1주차 체크포인트
- [ ] 친구/지인 5분 플레이시켜 피드백 수집
- [ ] **재미 판정 기록** (YES/그럭저럭/NO)
- [ ] 판정에 따라 다음 액션 결정

---

## 2주차 — "운전에 긴장감이 추가되는가"

- [ ] 단순 화물 시스템 (Shock 민감도만, 박스 1종)
- [ ] 단순 타이어 시스템 (단일 수명, 바퀴 구분 없음)
- [x] 단순 연료 시스템 (소비 + 고갈 시 정지) — `FuelSystem.cs` (세션 10, 5분 풀탱크)
- [x] 단순 기어 시스템 (Auto 5단 + HUD) — `GearSystem.cs` + `GearHUD.cs` (세션 10)
- [x] 간단한 HUD (연료 게이지 + 기어) — `FuelHUD.cs` + `GearHUD.cs` 좌하단/우하단
- [x] 배달지 노드 3~4개 배치 — 8개 (`SetupOrderSystem.cs`)
- [x] 시작 → 배달지 순회 → 복귀 기본 루프 — `Order/RunController.cs` 오케스트레이션
- [x] **B 시스템 — 주문 보드 + 오버뷰 카메라 + 정산 화면** (세션 10, 핵심 결정 루프 첫 진입점)
  - `Order/OrderBoard.cs` 카드 3장(A/B/C 트레이드오프)
  - `Order/OverviewCamera.cs` 탑다운 ↔ 팔로우 전환
  - `Order/RunController.cs` + `ResultPanel.cs` 정산
  - `Order/CountdownHUD.cs` 시간제한 카운트다운
- [ ] 부스트 시스템 (D-030 K1/K2 — 채널 A 즉시충전만 우선)
- [ ] 화물 Shock 시스템

### 2주차 체크포인트
- [ ] 화물 때문에 운전 스타일이 바뀌는가?
- [ ] 긴장감이 있는가?
- [ ] 주문 A/B/C 선택 차이가 체감되는가? (세션 10 추가)

---

## 3주차 — "경로 계획이 실제로 의미 있는가"

- [ ] 노드-엣지 그래프 데이터 구조 설계
- [ ] 경로 계획 UI 프로토타입 (드래그로 순서 지정)
- [ ] 실시간 예측 수치 (거리, 시간)
- [ ] 타이어 3종 (Soft/Medium/Hard)
- [ ] 피트스탑 구현 (QTE 없이 기본만)
- [ ] Spin 민감도 추가 (활어 등)
- [ ] 화물 종류 3~4종

### 3주차 체크포인트
- [ ] 계획 vs 즉흥 주행 결과 차이가 나는가?
- [ ] 20% 이상 성적 차이가 나는가?

---

## 4주차 — MVP 완성

- [ ] 주문 선택 보드 (8개 중 3~5개)
- [ ] 정산 화면
- [ ] 기본 업그레이드 트리 1단계
- [ ] 서킷 2~3개 (카피/변형으로)
- [ ] Tilt 민감도 추가
- [ ] 코너 경고 UI
- [ ] 기본 사운드 배치 (Kenny Audio)
- [ ] 런 반복 메타 루프 (허브 → 런 → 허브)

### MVP 선언 기준 (전부 ☑ 시 MVP 완성)
- [ ] 한 런 5~15분 안에 완료된다
- [ ] 계획 → 실행 → 정산 3단계가 모두 작동한다
- [ ] 실패해도 다시 하고 싶다
- [ ] 최소 3종 화물이 서로 다른 운전 스타일을 강제한다
- [ ] 2~3종 타이어가 전략적으로 의미 있다
- [ ] 사운드/비주얼이 "플레이스홀더" 수준이라도 플레이 가능하다

---

## 하네스 / 메타 작업

### 2026-04-18 세션 2에서 완료
- [x] CLAUDE.md 슬림화 (172 → 77줄)
- [x] 세션 로그 시스템 구축 (`notes/session-log.md`)
- [x] 기능 목록 구축 (이 파일)
- [x] 세션 오프닝 템플릿 (`notes/session-opener.md`)
- [x] MCP 정리 가이드 (`notes/mcp-setup.md`)
- [x] git init + 첫 커밋

### 추후 (2주차 이후)
- [ ] 하위 디렉토리별 CLAUDE.md (unity-project/Assets/Scripts/CLAUDE.md 등)
- [ ] `scope-check` 스킬 생성
- [ ] `gdd-reviewer` 서브에이전트 (문서 수정 일관성 체크)
- [ ] 하네스 4~6단계 (검증 파이프라인, 멀티에이전트, 유지보수)
