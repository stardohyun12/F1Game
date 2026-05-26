# 맵 넓히기 — 한 런 8~12초 vs 5~15분 갭 해소

- 현상: 현재 도시 78×54m, RunMetricsRecorder 3런 측정 결과 한 런 8~12초 / 이동거리 49~110m. 데모 목표(5~15분) 대비 200~900배 갭. 이 갭이 닫히지 않으면 escalation·화물·차고 폴리싱은 전부 의미 없음.

- 원인 가설: 맵 자체가 너무 작음이 본질(세션 26 RunMetricsRecorder 측정 확정). 배달지 간 거리가 짧아 1잡=1분 이하. 맵 확장 + 배달지 재배치 없이는 런 길이 목표 달성 불가.

- 완료 조건 (측정 가능):
  1. RunMetricsRecorder 5런 연속 측정 → 한 런 평균 **90초 이상** (현재 10초 → ×9 목표, 전체 목표 5분의 절반을 중간 체크포인트로)
  2. 배달지 간 평균 거리 **≥ 300m** (현재 ~50m)
  3. TrackConnectivityVerifier PASS (297/297 연결성 유지)
  4. 회귀 하네스 12 PASS / 0 FAIL (CargoPolyBudget 제외 — 별도 항목)
  5. zone 입장 2초 안에 바닥색으로 구역 식별 가능 (아트 기준: 운전시점 즉시 구분)
     - 다운타운: 밝은 회색 콘크리트 바닥
     - 항구: 모래 베이지 단일 쿼드 (PlaceZoneGround 항구 띠 전체 덮기)
     - 공업: 어두운 갈색 (채도↑, "흙"답게)
  6. zone별 드라이빙 리듬 구현 확인:
     - 다운타운: 블록 간격 좁음 + 중밀도 격자 → 잦은 코너 (꺾임 빈도 ≥ 10회/500m)
     - 항구: 도로 폭 좁음 + 막힌 구간 포함 → 저속 강제 (폭 1~2타일)
     - 공업: 직선 대로 1개 이상 (≥ 200m 무교차 직선) + 단발 위협 요소 (교차로 돌발)

- 관련 파일/메뉴:
  - 기존 spec: `docs/superpowers/specs/2026-05-16-map-expansion-r1-design.md`
  - roadMask: `F1/Assets/Editor/` — TrackLayout SO 수정
  - 검증: `unity-cli menu "PitCrew > Verify Track Connectivity"`
  - 바닥색 적용: `unity-cli menu "PitCrew/Map/Place Zone Ground"` (PlaceZoneGround — 항구 쿼드는 띠 전체 덮도록 범위 확장 필요)
  - buildingMask zone별 재배치: 다운타운=H고층+C상업 / 공업=F공장+W창고+T굴뚝 / 항구=W창고
  - 맵 룩 재적용 체인 (리빌드 후 필수 — PlaceZoneGround를 Align 직전에 추가):
    Build Everything → Plain Asphalt → Flatten Junction Curbs → Place Road Center Lines → Auto-Place Sidewalks → Auto-Place Suburban Props → Auto-Place Street Furniture → Auto-Place Street Vehicles → Apply Warm Grade → **Place Zone Ground** → Align Map To Ground
  - 시스템 복원 체인 (맵 리빌드 시 8개 시스템 날아감, `.claude/activeContext.md` 참조):
    Cargo CCTV → PackingScreen → Garage Wire → Wire Core Singletons → Board Layout → FixOrderBoardWiring exec → OrderBoard.runController 할당 + SaveScene
  - 측정: `RunMetricsRecorder` (세션 26 세팅됨)

- 게이트: 화면 변경 포함 → 그래픽 게이트 + 기획 게이트 둘 다. QA 실플레이 5런 측정 의무.

- 도현 결정 필요 (착수 전 확인):
  - 목표 런 길이 중간 체크포인트: 90초 먼저 확인 후 단계적 확장 진행? (추천) 아니면 한번에 330×230m 적용?
  - zone 분할 비율 (다운타운/항구/공업 면적 비중) — R1 spec에 초안 있으나 도현 확인 필요
  - ① 항구 바닥 색온도: 따뜻한 베이지 vs 차가운 회모래 (단답)
  - ② 공업 갈색 채도: 현재보다 더 진하게 — 어느 수위? ("조금 더" / "많이 더" / "현행 유지")
  - ③ 다운타운 바닥: 회색 콘크리트로 교체 vs 잔디 유지 + 건물 실루엣만으로 차별화
