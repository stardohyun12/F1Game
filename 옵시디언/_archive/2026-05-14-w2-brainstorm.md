# W2 (5/20~5/26) 브레인스토밍 — 채널 5 + 채널 8

> 세션 23 자율 루프 중 작성. 도현 결정 후 정식 spec.

## 채널 5 — 박스 크기 (capacity 2→4)

### 현 상태
- CargoBox + items[] 시스템 있음 (1~3개). OrderJob.itemCount Range(1,3).
- 박스 자체의 "크기" 컨셉은 데이터로 없음.

### 데모 목표
- 시작: 박스 capacity 2 (한 번에 2개까지 포장).
- 업그레이드 1: capacity 3.
- 업그레이드 2: capacity 4.
- 효과: jobOffer 중 itemCount > capacity 잡은 회색/잠금 상태 → "박스 크기 부족" 표시.

### 구현 옵션 비교

| 옵션 | 코드 양 | 장점 | 단점 |
|---|---|---|---|
| A. `UpgradeEffect.CargoCapacity` enum + UpgradeDef 2개 (capacity+1, capacity+2 누적) | 작음 | UpgradeSystem 패턴 그대로 | OrderJob.itemCount > 1 잡 부족하면 더 추가 필요 |
| B. 별도 `BoxCapacitySystem` | 중간 | 깔끔 | 새 시스템 늘어남 |
| C. CargoBox 자체에 capacity 필드 + UpgradeSystem 조회 | 작음 | 데이터 단순 | OrderBoard에서 잠금 표시 위해 잡-capacity 비교 |

→ **A 권장**. UpgradeEffect 기존 패턴. CargoBox.Awake에서 `IsOwned(BoxCapacityPlus1)` 합산해 currentCapacity = base + sum.

### 잡 노출 필터
- OrderBoard.PickOffer: 잡 itemCount > currentCapacity면 풀에서 제외? 아니면 회색으로 표시?
- 회색 표시 권장. "박스 크기 부족"이 업그레이드 동기 부여.

### 작업 큐
1. UpgradeEffect enum에 BoxCapacityPlus1 추가.
2. UpgradeDef 자산 2개 (BoxCapacity_2to3 ₩500, BoxCapacity_3to4 ₩1500).
3. CargoBox.currentCapacity 계산.
4. OrderCardUI에서 itemCount > capacity일 때 회색/락 표시.
5. ResultPanel UpgradeCatalog에 자산 추가.

→ 1~2일 작업 예상.

---

## 채널 8 — 지도/내비

### 현 상태
- DeliveryPointMarker (월드 마커, 카드 hover 시 색 강조).
- 미니맵 / HUD 화살표 없음.

### 데모 목표
- 미니맵: 우상단에 작은 top-down 미니맵. 차 위치 + 배달 마커.
- HUD 화살표: 화면 가장자리 또는 차 앞쪽에 다음 마커 방향 화살표.

### 구현 옵션

#### 미니맵
| 옵션 | 비용 | 비고 |
|---|---|---|
| A. Render Texture + 위에서 보는 두 번째 카메라 | 중 | 정통 미니맵, 깔끔 |
| B. 손그림 2D 이미지 + 동적 마커 점 (월드→UI 변환) | 저 | 빠름, 정밀도 적당 |
| C. 외부 라이브러리 | - | 외부 의존 추가, Kenny only rule 위반 |

→ **A 권장**. Render Texture는 표준. 카메라 1개 더 + RawImage. Layer mask로 차/마커만 보이게.

#### 화살표
- 가장자리 화살표: 카메라 frustum 밖일 때 화면 가장자리에 표시.
- 또는 월드 공간 floating 화살표 (차 위 떠 있음).
→ **월드 화살표 권장** (단순). DeliveryPointMarker에 추가 화살표 prefab.

### 작업 큐
1. MiniMapCamera prefab + RenderTexture.
2. MiniMapHUD GameObject + RawImage + 마커 dot.
3. 월드→UI 마커 변환 스크립트 (DeliveryPointMarker가 미니맵 dot도 보냄).
4. DirectionArrow.cs — 차 위 floating 화살표, 다음 마커 방향.
5. F1 차 위 mount point 추가.

→ 2~3일 작업.

---

## W2 일정 제안

| 일 | 작업 |
|---|---|
| 월 (5/20) | 채널 5 step 1~3 (capacity 데이터) |
| 화 (5/21) | 채널 5 step 4~5 (UI 필터링) + Play 검증 |
| 수 (5/22) | 채널 8 — 미니맵 카메라 + RawImage |
| 목 (5/23) | 채널 8 — 마커 dot 변환 |
| 금 (5/24) | 채널 8 — 화살표 |
| 토~일 | Play 검증 + 폴리시 + W3 준비 |

---

## 도현 결정 사항

- [ ] 박스 크기 부족 잡은 풀에서 제외 vs 회색 표시? (B 회색 권장)
- [ ] 미니맵 위치: 우상단? 좌하단?
- [ ] 화살표 스타일: 가장자리 vs 월드?
