# PackingCardUI

**파일:** `Assets/Scripts/Order/PackingCardUI.cs`
**부착 위치:** `Assets/Prefabs/UI/PackingCard.prefab` root
**역할:** 포장 카드 prefab 컴포넌트 — 4 티어 중 하나 표시 + 클릭 콜백

---

## 인스펙터 입력 (prefab에 와이어링)

- **bg** (Image) — 카드 배경. 선택 색 토글
- **colorStrip** (Image) — 상단 색상 띠 (티어별 고정)
- **nameText** (TMP_Text) — "뽁뽁이"
- **multiplierText** (TMP_Text) — "×1.3"
- **descText** (TMP_Text) — "일반 주행 안전. ..."
- **costText** (TMP_Text) — "₩100" or "무료"
- **button** (Button) — 카드 전체 클릭

### 시각 튠
- selectedColor / normalColor

---

## 함수

### public API
- [[PackingCardUI.Bind]] — tier 결정 + 텍스트 set + button 와이어링 (1회)
- [[PackingCardUI.SetSelected]] — bg.color 토글

### 정적 helper
- `TierColor(tier)` — 티어별 색
- `PackingTierGuide(tier)` — desc 텍스트

---

## 협력 객체

- [[PreRunSetupPanel]] — 4장 인스턴스화, onClicked 콜백

---

## 관련 흐름

- [[Flow.GameStart]] — Bind, 선택 변경 시 SetSelected

---

## 핵심 사항

- **wired flag** — Bind 두 번 호출 시 첫 onClicked 사라지고 두 번째만 남음 (RemoveAllListeners + AddListener 패턴). 단, 현재 사용은 1회만 Bind
- **데이터 vs 시각 분리** — 텍스트(이름/배율/설명/가격) 코드에서 결정, 색·폰트·크기는 prefab 디자인 영역
