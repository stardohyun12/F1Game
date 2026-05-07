# Designer Report — Cycle 2 (2026-04-30 main)

## 사이클 1 발견 1줄 참조
"단기 ROI(포장비 vs 보상)만 보이고 장기 시간축(업그레이드 저축)이 안 보임" → Hardcase가 보험이 아닌 낭비처럼 보이는 표현 갭.

## 노출 옵션 비교

| 옵션 | 무엇 | 구현 비용 (시각/코드/위험) | 효과 |
|---|---|---|---|
| **A: Wallet 1줄 추가** | `totalCostText` 위 또는 옆에 "보유 자금 ₩X" 1줄 추가. 선택적으로 "차감 후 ₩Z" 부가. ResultPanel에 이미 같은 패턴(`walletLabel`/`walletVal`, `UpgradeSystem.Instance.Wallet` 조회) 존재 → 복붙 가능. | 시각: 텍스트 한 줄. 코드: PreRunSetupPanel.cs `Refresh()`에 `UpgradeSystem.Instance?.Wallet` 조회 + TMP_Text 인스펙터 슬롯 1개. ~10줄. 위험: Instance null 가드. | 장기 자금 풀이 매 출발 화면마다 보임 → 포장비가 "총자산 대비 비중"으로 인지됨. Hardcase ₩800도 Wallet ₩5000 위에서는 16% → 부담 완화. |
| **B: 시간축 환산 텍스트** | "이 포장 = SuspensionUpgrade ₩1000의 0.8배" 같은 환산. UpgradeCatalog 최저가/평균가 조회 필요. | 시각: 텍스트 1줄. 코드: 카탈로그 ref + 환산식 + 카피 디자인. 위험: 환산식이 가짜 정밀도(어떤 업그레이드 기준?)를 줌. 카피 톤이 설교 같으면 거슬림. | 트레이드오프를 명시적으로 언어화. 단 직관 < 숫자. 도현 톤 ("과도한 감탄 금지") 과 충돌 가능. |
| **C: 출발 후 정산 미리보기 (Wallet 변화 화살표)** | "₩5000 → ₩4200 (포장 후) → ₩4700 (예상 보상 시)" 3-step 미니 다이어그램. | 시각: 화살표 + 3 텍스트. 코드: 예상값 계산 (이미 `packCost`, `reward` 보유) + 작은 레이아웃. ~30줄. 위험: 정산 화면과 중복 인상. baseReward는 integrity에 따라 변동 → "예상"이 빗나가면 신뢰 하락. | A보다 인지 부하 ↑, 효과는 같음. 시간축이라기보다 단일 런 ROI 시각화 강화. |

## 추천 (구현 비용 vs 효과)

**옵션 A 추천.** 근거 3개:

1. **재사용 자산.** ResultPanel.cs:194-199, 405에 동일 UI 패턴 (`walletLabel`/`walletVal` + `UpgradeSystem.Instance.Wallet` 조회) 이미 검증됨. PreRunSetupPanel에 이식하는 비용이 사실상 없음.
2. **최소 침습.** 가치값 무변경. 카피 디자인 부담 없음 (한 글자도 새로 쓸 필요 없음 — "보유 자금 ₩X").
3. **B/C 단점 회피.** B는 환산 기준의 자의성. C는 "예상" 신뢰도 문제 + 정산 화면과 정보 중복 → 정산의 임팩트 희석.

**예상 행동 변화:** Wallet 표시가 출발 화면에 있으면 플레이어는 자기 페이스를 자각하기 시작함. "지금 ₩300 남았는데 Hardcase ₩800은 못 산다" → 자연스러운 자금 관리 결정. "Wallet ₩8000인데 이번 화물 보상이 ₩500이면, 굳이 Hardcase 안 사도 됨" 같은 상황별 판단도 가능. 사이클 1의 "단기 ROI 단순화" 갭이 부분 해소.

**구현은 도현 영역.** PreRunSetupPanel에 SerializeField TMP_Text 슬롯 1개 추가, Refresh()에서 `var ups = UpgradeSystem.Instance; walletText.text = ups != null ? $"보유 자금 ₩{ups.Wallet:N0}" : "";` 정도. 인스펙터 와이어링 도현 직접.

## 가치값 변경 제안 (있으면 명시 — 도현 영역, 자율 루프 금지)
- 없음.

---
**작성:** 2026-04-30 야간 자율 루프 cycle 2 designer (synthesis-only)
