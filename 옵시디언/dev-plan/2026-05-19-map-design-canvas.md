# 🗺️ 맵 디자인 학습 캔버스 — PitCrew

> 2026-05-19 · 세션 27. 도현과 같이 맵 디자인 공부하면서 만드는 살아있는 문서.
>
> **사용법** — 옵시디언에서 열어둠. AI가 Edit 저장 → 자동 리로드. 도현은 직접 타이핑 또는 채팅.
>
> **섹션 마커**: 📖 학습 / 🤖 AI 제안 / 👤 도현 / ✅ 결정 / ❓ 미해결

---

## 0. 출발점 — 우리 맵의 현실

- **현재 (R0)**: 도시 78×54m. 한 런 8~12초 / 거리 49~110m (세션 26 RunMetricsRecorder 측정).
- **R1 spec**: 30×22 (tileSize 11 → 330×242m). 도착·zone 분포 재배치. **아직 미적용 — 측정 대기**.
- **목표**: 마커 간 평균 거리 ~500m, 한 잡 1~2분. 데모 5~15분 한 런.

레퍼런스 spec: `docs/superpowers/specs/2026-05-16-map-expansion-r1-design.md`
관련 코드: `Assets/Editor/_Recovery/RoadBuilder.cs` · `BuildingPlacer.cs` · `TrackLayout.asset` SO

---

## 1. 📖 맵 디자인 원칙 (게임 디자인 일반론)

### 1.1 좋은 도시 맵의 5가지 축 (Open World 레퍼런스)

| 축 | 설명 | PitCrew에 어떻게? |
|---|---|---|
| **랜드마크 (Landmark)** | 멀리서 보이는 고유 시각물. 위치 감각. | F1 그랑프리 트랙, 공장 굴뚝, 항구 크레인, 시청탑 4개 |
| **이정표 (Wayfinding)** | 어디로 가야 할지 시각적 단서. | 도색·간판·zone 컬러 톤 (다운타운=네온, 항구=파랑, 공업=주황) |
| **shortcut/우회** | 같은 목적지 가는 여러 경로 — 플레이어 선택 발생. | 다리·터널·뒷길·고가도로 |
| **risk/reward 지대** | 빠른 길에 위험 (좁은 길, 코너), 느린 길에 안전. | 다운타운=좁은 격자, 외곽=직선 고속 |
| **breathing room** | 빈 공간/공원 — 항상 빡센 게 아닌. | 공원·강·광장 (zone 사이 transition) |

### 1.2 Open World vs Linear vs Hub-Spoke

PitCrew는 **Hub-Spoke** 모델:
- **Hub** = 차고 (시작점, 메타 진행)
- **Spoke** = 배달지 (한 런마다 다른 조합)
- 차이: GTA(open world, 자유 탐사) vs Crazy Taxi(spoke, 미션 기반). PitCrew는 후자에 가까움.

→ **빈 길 면적 < 의미있는 랜드마크 수**가 더 중요. 32×24 칸 다 채울 필요 X.

---

## 2. 🤖 R1 (30×22) 현 spec의 강점과 약점

### 강점
- 대각선 zone 분할 → 직선 격자보다 자연스러움
- 손 ASCII 도로망 → 자동 격자 X, 인간적 비대칭
- 11개 배달지 (R0 8개 → R1 11) — zone당 3~4개 분포

### 약점 (학습 관점)
- 30×22 = **여전히 격자 도시**. Kenney 타일 한계로 어쩔 수 없지만, **시각적 랜드마크 4~5개**가 부족.
- 다운타운/항구/공업 톤 차이가 building 색만으로 부족 → **타일 군집 패턴** 다르게 (다운타운=빌딩 빽빽, 항구=창고+넓은 공터, 공업=공장+굴뚝).
- shortcut 부재 → 다리/터널 한두 개 박으면 경로 선택 의미 생김.

---

## 3. ❓ 학습/결정해야 할 것

### 3.1 R1 vs 더 큰 R2?

| 옵션 | 크기 | 한 런 예상 | 작업 비용 |
|---|---|---|---|
| **R1 (확정 spec)** | 30×22 (330×242m) | 한 잡 1~2분 | ~~이미 spec 완성, 적용만~~ |
| R2 (더 키움) | 50×35 (550×385m) | 한 잡 2~4분 | spec 새로 + 재배치 +1~2일 |

도현 직관: "R1 작아도 일단 측정 → R2 결정". 동의.

### 3.2 zone 시각 차별화 — 어떻게?

| 방법 | 비용 | 효과 |
|---|---|---|
| A. building 색 톤만 (현재) | 0 | 약함 — 멀리서 안 보임 |
| B. 타일 군집 밀도 (빌딩 빽빽 vs 넓은 창고) | 중 | 좋음 — 실루엣 다름 |
| C. 랜드마크 4~5개 (크레인, 굴뚝, 트랙, 시청탑) | 큼 | 최강 — wayfinding |

→ 권장: **A + B 우선, C는 W5 폴리시**. 데모는 측정값 먼저.

### 3.3 shortcut 도입?

R1에 다리·터널 박을까? — **데모는 직선 단순. 풀게임 W7+에 추가**. 지금 박으면 측정 노이즈.

---

## 4. ✅ 현재 결정 (잠정)

- **R1 적용 → 5런 측정 → 결과 보고 R2 판단**.
- zone 시각 차별화: A(색)는 현재, B(밀도)는 R1 적용하면서 같이.
- shortcut/랜드마크: 후속 (W5+).
- **맵 spec 변경 시 항상 `TrackConnectivityVerifier` 통과 의무** (asset.md).

---

## 4.5. 🤖 서스 점프대 — 맵 디자인과 같이 (2026-05-19)

도현 결정: "서스 점프대는 맵 기획하고 같이". → R1 적용 후 맵 측정하면서 같이 박을 항목.

**개념**:
- 서스펜션 부품 = 차의 충격 흡수. 미해금 시 험지/점프 cell 진입 = 화물 손상 + 차 튕김.
- 해금 시 통과 가능 → **새 shortcut 또는 새 픽업지 도달** (이전엔 막힌 길 열림).

**구현 (D-050 heightMask 활용)**:
- TrackLayout slope tile (`n/s/e/w/=/+`) — 이미 있는 시스템.
- `SuspensionSystem.IsUnlocked` 체크해서 미해금 진입 시 페널티.

**R1 배치 후보 (잠정)**:
- 외곽 도로 일부에 작은 언덕 1~2곳 (공업↔항구 연결 shortcut)
- zone 경계에 점프 구간 1곳 (해금 시 zone 간 우회 길)
- 도심 안엔 박지 않음 (운전 어려움)

**검토 시점**: R1 측정 끝나고 도로 동선 안정화 후 박기. R2 가면 위치 재조정.

---

## 5. 📖 학습 — 우리가 더 공부할 주제

- [ ] Open world wayfinding (Hollow Knight 압축 맵 vs Witcher 3 넓은 맵)
- [ ] Hub-Spoke 모델 사례 (Crazy Taxi, Spider-Man PS4, Sleeping Dogs)
- [ ] Kenney 타일 한계 안에서 비주얼 차별 (다른 인디 사례)
- [ ] heightMask 활용 — 점프대/언덕 어디 박을지 (D-050 D-077 서스 unlock과 연결)

---

## 6. 👤 도현 의견 / 학습 노트

(여기에 직접 타이핑)

---

## 7. 📚 참조

- R1 spec: `docs/superpowers/specs/2026-05-16-map-expansion-r1-design.md`
- R1 plan: `docs/superpowers/plans/2026-05-16-map-expansion-r1.md`
- 자산 규칙: `project_rules/asset.md`
- 코드: `Assets/Editor/_Recovery/RoadBuilder.cs` · `BuildingPlacer.cs`
- 측정: 세션 26 RunMetricsRecorder CSV
