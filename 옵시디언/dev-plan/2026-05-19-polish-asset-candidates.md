# 🎨 폴리시 Sprint 자산 후보 (Day 3·5·6)

> 2026-05-19 도현 운동 중. AI가 WebSearch로 후보 정리. **다운로드·선택은 도현 영역** ([no-compromise] §8).

---

## 1. F1 머신 (Day 3)

### 후보 A — **Kenney Racing Pack** (가장 안전, 권장)
- URL: https://kenney.nl/assets/racing-pack
- 라이선스: **CC0**, 무제한 상업용
- 자산 수: 420개 (F1 머신·트랙·road·obstacle 다 포함)
- 톤: 현재 Kenney 도시·차와 **완벽 일관**. 시각 충돌 X.
- 단점: 디테일 낮음 (low-poly). 하지만 데모 우선·자산 일관 우선.
- **추천 이유**: 이미 도시·건물이 Kenney이라 같은 작가 같은 톤. 작업 0일 수준.

### 후보 B — Kenney Car Kit
- URL: https://kenney.nl/assets/car-kit
- 라이선스: **CC0**
- 자산 수: 45개. 일반 차량 위주 (F1 X).
- 추천도: F1 컨셉 약함 → 보류

### 후보 C — OpenGameArt Low-Poly Race Car
- URL: https://opengameart.org/content/free-low-poly-3d-race-car-model
- 라이선스: **CC0**, Unity 호환
- 단점: 단일 모델, Kenney 톤과 안 맞을 수도

### 후보 D — Sketchfab "ustoopia F1 collection"
- URL: https://sketchfab.com/ustoopia/collections/f1-a7bf7e5c3af5444fa65a347593790585
- 라이선스: 모델마다 다름 (체크 필요)
- 추천도: 더 디테일 있는 F1 머신. 다만 다운로드/임포트 작업 큼.

### 후보 E — Meshy AI 생성 F1
- URL: https://www.meshy.ai/tags/f1
- 라이선스: CC0 (AI 생성)
- 사용법: prompt 입력 → 생성. 톤 통일 가능.
- 추천도: 도현이 직접 시각 톤 잡고 싶을 때.

**👉 추천 1순위: Racing Pack (A)** — Kenney 톤 일관, 무료, 0일 작업.

---

## 2. 한국어 폰트 (Day 2·통합)

### 후보 A — **Pretendard** (가장 인기, 권장)
- URL: https://github.com/orioncactus/pretendard
- 라이선스: **SIL 오픈 폰트 라이선스** — 게임 상업용 OK
- 톤: Apple SF Pro 닮음. 현대적, 깔끔.
- 다운로드: Pretendard-Regular.otf, Pretendard-Bold.otf, Pretendard-SemiBold.otf

### 후보 B — Spoqa Han Sans Neo
- URL: https://spoqa.github.io/spoqa-han-sans/
- 라이선스: 오픈 폰트 라이선스
- 톤: 본고딕 기반, 한국어 친화. 약간 traditional.

**👉 추천: Pretendard** — 게임 UI에 더 modern.
**Unity 임포트**: .otf 파일 Assets/Fonts/ 넣고 → 우클릭 → Create > TextMeshPro > Font Asset (SDF).
다운로드 안 해도 데모는 현재 KoreanFallback SDF로 진행 OK.

---

## 3. 화물 visual prop (Day 5)

### 후보 A — Kenney Game Assets All-in-1
- URL: https://kenney.itch.io/kenney-game-assets
- 라이선스: CC0
- 다운로드: $19.95 minimum (option for free 가능)
- 내용: 60,000+ 자산. crate·barrel·box·prop 다 있음.

### 후보 B — Kenney 개별 자산
- 메인 사이트 https://kenney.nl/assets — `prop pack`, `crate`, `cargo` 검색
- 무료 개별 다운로드 가능

### 후보 C — Meshy AI 생성 (와인/꽃/유리 등 cargo type별)
- prompt 예: "wooden wine crate with bottles low poly Kenney style"
- 도현이 cargo type 6종 prompt 직접 작성 → 생성

**👉 추천: B (Kenney 개별)** — 톤 일관, 무료, 즉시.

---

## 4. 메인 메뉴 배경 (Day 6)

### 후보 A — Unsplash F1 무료 이미지
- URL: https://unsplash.com/s/photos/formula-1
- 라이선스: Unsplash License (게임 사용 OK)

### 후보 B — Midjourney/SD prompt
- "F1 garage with vintage red Ferrari, dramatic lighting, retro poster style, cinematic"
- "F1 race car silhouette at sunset, low poly, Kenney style"
- 도현이 생성 → 임포트

### 후보 C — 도시 screenshot (이번 게임 자체)
- Unity Editor에서 carefully composed scene 캡처 → 배경

**👉 추천: B (Midjourney prompt) 또는 C (자체 screenshot)** — 게임과 어울리는 정체성.

---

## 도현 액션 (운동 후)

1. **F1 머신**: A/B/C/D/E 중 선택 (추천: A Racing Pack)
2. **폰트**: Pretendard 다운로드 또는 보류 (KoreanFallback 유지)
3. **화물 prop**: Kenney 개별 또는 보류
4. **메뉴 배경**: 자체 screenshot OR Midjourney 시도

선택 알려주면 AI가 즉시 Unity 임포트 + scene 적용.

---

## 참조

- [Free, Low Poly 3D Race car model — OpenGameArt](https://opengameart.org/content/free-low-poly-3d-race-car-model-%F0%9F%8F%8D%EF%B8%8F%F0%9F%8F%81%F0%9F%9A%A6%F0%9F%8F%86)
- [Racing Pack — Kenney](https://kenney.nl/assets/racing-pack)
- [Car Kit — Kenney](https://kenney.nl/assets/car-kit)
- [Pretendard — GitHub](https://github.com/orioncactus/pretendard)
- [Spoqa Han Sans Neo](https://spoqa.github.io/spoqa-han-sans/ko-KR/)
- [Meshy F1 tags](https://www.meshy.ai/tags/f1)
- [Sketchfab F1 collection](https://sketchfab.com/ustoopia/collections/f1-a7bf7e5c3af5444fa65a347593790585)
- [Kenney All-in-1](https://kenney.itch.io/kenney-game-assets)
