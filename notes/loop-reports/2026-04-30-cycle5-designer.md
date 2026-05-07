# Designer Report — Cycle 5 (마무리 통합, 2026-04-30 main)

## 누적 발견 (사이클 1·2·3·4 한 줄씩)
1. **C1**: 세션 16 후속 리팩터로 포장이 별도 모달(`PreRunSetupPanel`)로 분리되어 결정 의식화 ↑, 그러나 출발 화면이 단기 ROI(포장비 vs 보상)만 보여 장기 자금 풀이 미노출 → Hardcase가 "보험"이 아닌 "낭비"로 인지됨.
2. **C2**: 세 옵션(A: Wallet 1줄 / B: 시간축 환산 / C: Wallet 변화 화살표) 비교 후 **옵션 A 추천** — ResultPanel.cs:194-199 패턴 복붙, 코드 ~10줄, 가치값 무변경.
3. **C3**: P0=Wallet 1줄, P1=OnCancel selectedTier 휘발 + PackingCardUI 화물 종속 안내 부재, P2=포장 종류별 차등(Phase 2 표기 이미 존재) — 사이클 1·2 통합.
4. **C4**: OrderBoard 호버→마커 강조는 우측 카드 / 좌측 맵 시야 분리로 **인지율이 의도보다 훨씬 낮을 가능성**, 비호버 상태 1:1 매핑 부재 — P2급 보조 신호 갭.

## 최종 P0/P1/P2

### P0 (즉시 처리, 첫 5분 좌절 유발)
- **Wallet 1줄 노출** (`PreRunSetupPanel`의 `totalCostText` 위/옆에 "보유 자금 ₩X"). 단기 ROI만 보이는 표현 갭 부분 해소.

### P1 (다음 세션, 게임 깊이)
- **OnCancel 시 selectedTier 캐시** (jobId별) — 비교 탐색 인지 부하 ↓.
- **PackingCardUI 화물 종속 안내** — Bind에 cargoType 추가 또는 cargoTipText를 카드 그리드 위로 이동.

### P2 (Phase 2)
- **포장 × 화물 매트릭스 차등** (PackingTier.cs 주석에 이미 표기) → `ideas-parking.md` 보존.
- **OrderBoard 카드별 상시 색 점** — 비호버 1:1 매핑, 호버는 보조 강조로 격하 (사용자 테스트 후 우선순위 재평가).

## 도현 다음 세션 첫 작업 1개 추천
**Wallet 1줄 추가 (PreRunSetupPanel)**. 근거 30~60초: ResultPanel.cs:194-199에 동일 UI 패턴(`walletLabel`/`walletVal` + `UpgradeSystem.Instance.Wallet`)이 이미 검증되어 복붙 가능. SerializeField TMP_Text 1개 + Refresh()에 한 줄(`walletText.text = ups != null ? $"보유 자금 ₩{ups.Wallet:N0}" : "";`) + 인스펙터 와이어링. 실작업 5~10분, 가치값 무변경, 스코프 규칙 위반 위험 0, 매 출발마다 "총자산 대비 포장비 비중"이 보여 사이클 1·2 핵심 갭이 즉시 부분 해소.
