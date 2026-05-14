# UI Polish Research — "게임 같은 태" 한 단계 올리기

- **작성일:** 2026-04-25 (세션 13 이후)
- **문서 목적:** 도현 피드백 "UI가 조금 더 실제 게임 같은 태가 나진 않음. UI 관련 MCP나 기술을 조금 알아보는 거 나쁘지 않을 것 같음"에 대응. 전면 리팩터 없이 현재 구조(UGUI 2.0 + Kenney UI pack + TMP Dynamic SDF) 위에서 비용/효과가 높은 개선 후보를 정리.
- **스코프 원칙:** MVP 4주(현재 3~4주차) 내 가능 범위 우선. 그 이후 것은 별도 표기.

---

## 1. 배경

### 1.1 현재 상태 요약

- **기술 스택:** UGUI 2.0 / Canvas / TMP Dynamic SDF / Kenney UI pack (5색 변종) / Kenney Future·Future Narrow 폰트 / Kenney interface-sounds 4종.
- **아키텍처:** 자가구성 패턴 (`Awake → EnsureCanvasParent → BuildIfNeeded`). `UIRoot` + `UITheme` SO + `UISounds` lazy singleton.
- **HUD 6종:** `FuelHUD`, `GearHUD`, `CountdownHUD`, `DeliveryHUD`, `OrderBoard`, `ResultPanel` — 모두 단색 Kenney sprite 스왑 기반(세션 13에서 `Image.color` tint → sprite 스왑으로 전환).
- **색 체계:** 어두운 남색 panel(`#1F2330F0`) + 흰 텍스트 + 상태별 Green/Yellow/Red sprite.
- **움직임:** 깜박 blink, shift flash, 카드 fade 수준. 이징·시퀀스·파티클 부재.

### 1.2 "게임 같은 태가 안 나는" 원인 추정

| 결핍 축 | 현재 상태 | 갭 |
|---|---|---|
| 텍스트 질감 | TMP default material (SDF만 사용) | Glow/Outline/Underlay 효과 0개. "종이 위 활자" 느낌 |
| 움직임 이징 | `Mathf.Lerp` + `Mathf.PingPong` | Back/Elastic/OutExpo 등 "튕기고 붙는" 감각 없음 |
| 상태 전이 신호 | 색 변경 + blink | 숫자 카운트 애니, swoosh, 잔상 궤적 없음 |
| 화면 외곽 | Canvas 내부 static 배치 | 슬라이드 인, corner bracket, screen-border flash 없음 |
| 파티클 | 3D에만 존재(드리프트 스모크) | Canvas 위 UI 파티클 0개 |
| 레이어링 | Panel(flat) + Fill(flat) 2층 | Depth/Shadow/Rim 등 "판때기 느낌" 깨는 요소 없음 |

---

## 2. UGUI vs UI Toolkit 판정 — PitCrew MVP 4주 관점

### 2.1 2026-04 현재 성숙도 (리서치 결과)

| 축 | UGUI 2.0 (6.x) | UI Toolkit (6.x) |
|---|---|---|
| Production readiness | 안정, 10년+ 실적, 레거시·라이브 게임 표준 | Unity 6.3/6.4에서 성숙, 복잡 HUD도 커버 가능 |
| 런타임 애니메이션 | Animator/Timeline/Tween 전부 OK | CSS-like transition만, Timeline·Animator 미지원 |
| 9-slice + 커스텀 sprite | Import 설정만으로 즉시 | USS `-unity-slice` 지원, 러닝커브 있음 |
| 파티클 Canvas 통합 | UIParticle(무료)로 해결 | 통합 경로 아직 실험적 |
| Shader Graph UI | `uGUI/ShaderGraph` 공식 지원(6.x) | 지원하나 문서·예제 부족 |
| 데이터 바인딩(MVVM) | 없음 | Unity 6 신규 바인딩 시스템 |
| 에셋 스토어 폴리싱 에셋 | 매우 풍부(mob-sakai 계열 등) | 아직 빈약 |
| 학습 커브 | 낮음(이미 숙지) | USS/UXML 익혀야 함 |

### 2.2 전환 비용 추정

현재 HUD 6종을 UI Toolkit으로 옮긴다면:

- UXML 스켈레톤 6개 + USS 공통 1개 + 바인딩 C# 6개 신규 작성
- TMP → `<Label>` 교체(폰트 SDF 대응 USS 커스텀 필요)
- Kenney sprite → `background-image` + slice 변환
- 파티클·트윈 경로 재설계
- **추정 2~3일 풀타임** (단순 변환), +1~2일 시각 검증/튜닝 — MVP 4주 남은 1주 예산 절반 이상을 잡아먹음.

### 2.3 판정

| 선택지 | 평가 |
|---|---|
| **UGUI 유지 + 폴리싱**  (채택) | 세션 13에서 막 안정화. 에셋/기법 생태계가 압도적. 4주 스코프 존중 |
| UGUI/UI Toolkit 하이브리드 | 메뉴만 UIT로? → PitCrew는 메뉴가 작아 ROI 낮음. 기술 이원화 리스크 |
| 전면 UI Toolkit 이관 | 2~3일 소실 + 파티클 경로 불확실. **기각** |

**결론:** MVP까지는 UGUI 유지. UI Toolkit은 v1 이후 허브 씬(H1)을 만드는 시점에 재평가. 근거는 (1) 세션 13에서 UGUI 기반이 막 안정화 완료, (2) UIT의 런타임 애니 제약이 레이싱 HUD의 "튕기는 느낌"과 상극, (3) 전환 2~3일 지출이 코어 루프 검증 일정과 충돌.

> 참고: [Unity Manual — Comparison of UI systems](https://docs.unity3d.com/6000.3/Documentation/Manual/UI-system-compare.html), [Angry Shark Studio — UGUI vs UI Toolkit 2025](https://www.angry-shark-studio.com/blog/unity-ui-toolkit-vs-ugui-2025-guide/), [Unity — Scalable Performant UI with UI Toolkit](https://unity.com/resources/scalable-performant-ui-uitoolkit-unity-6)

---

## 3. UI 고급 기법 우선순위 (UGUI 유지 가정)

각 항목: 효과 / 구현 난이도(1~5) / Kenney 궁합 / PitCrew 도입 판단.

| # | 기법 | 효과 | 난이도 | Kenney 궁합 | 도입 |
|---|---|---|---|---|---|
| A | **TMP Underlay + Outline** | 텍스트에 soft shadow + 윤곽선. "활자"→"게임 숫자" 전환 즉발 | 1 | 완벽(폰트 교체 없이 material preset만) | **채택(MVP)** |
| B | **TMP Glow** (Bloom 필요) | 기어/남은시간 숫자에 광휘. 저연료 빨간 글로우 | 2 | 좋음(숫자 위주 폰트) | **채택(MVP)** — Bloom은 이미 URP 기본 ON |
| C | **Coffee UIEffect** (무료) | blur/grayscale/dissolve/shiny/gradient 등. TMP도 지원 | 2 | 우수 | **채택(MVP)** — ResultPanel 등장 시 dissolve, OrderBoard 카드 shiny |
| D | **ParticleEffectForUGUI (UIParticle)** (무료) | Canvas 안에 파티클. maskable·sortable | 3 | 우수 | **채택 조건부** — Delivery 완료·기어 시프트 등 비주얼 스플래시 |
| E | **PrimeTween** (무료) | 이징·시퀀스 완전 세트. zero-allocation, DOTween보다 9배 빠름 | 2 | 무관 | **채택(MVP)** — DOTween Pro(€15) 대신 |
| F | **SoftMaskForUGUI** (무료) | Soft 마스크. OrderBoard 카드 모서리 rounded fade | 2 | 좋음 | **보류** — 현재 Kenney 모서리로 충분 |
| G | **Shader Graph UI(uGUI 2.0 공식 연동)** | 스캔라인/홀로그램/스크롤 그라디언트 | 4 | 중(지나치면 Kenney 톤 깨짐) | **보류(v1 이후)** — 허브 씬 UI에서 하나 정도 |
| H | **Animation 키: scale pulse / shake / flash** | "숨 가쁨 호흡" — 임박도에 따라 호흡 속도↑ | 2 | 무관 | **채택(MVP)** — PrimeTween으로 10줄 |
| I | **숫자 카운트업 tick SFX + 이징** | ResultPanel 점수 표기 시 드르륵 카운트 | 1 | 우수 | **채택(MVP)** |
| J | **화면 외곽 경고 (screen-border flash)** | 연료 10% / 시간 10초 시 화면 가장자리 빨강 pulse | 2 | 우수 | **채택(MVP)** |
| K | **Layered panel (Shadow + Rim + Fill)** | Kenney flat 위에 drop shadow + inner highlight 1px | 3 | 상(Kenney 원화와 일체감) | **보류** — UIEffect Shiny로 대체 |

### 3.1 무료 에셋 일괄 도입 후보 (mob-sakai 생태계)

| 패키지 | OpenUPM 경로 | 역할 |
|---|---|---|
| UIEffect | `com.coffee.ui-effect` | blur/dissolve/shiny/gradient/grayscale |
| ParticleEffectForUGUI | `com.coffee.ui-particle` | Canvas 안 파티클 |
| SoftMaskForUGUI | `com.coffee.softmask-for-ugui` | soft 마스크 |

전부 MIT·Apache. Unity 6·URP·TMP 호환 확인됨. 한번에 Package Manager 추가 10분.

> 참고: [mob-sakai/UIEffect](https://github.com/mob-sakai/UIEffect), [ParticleEffectForUGUI](https://github.com/mob-sakai/ParticleEffectForUGUI), [SoftMaskForUGUI](https://github.com/mob-sakai/SoftMaskForUGUI), [OpenUPM — UIEffect](https://openupm.com/packages/com.coffee.ui-effect/), [PrimeTween on GitHub](https://github.com/KyryloKuzyk/PrimeTween), [Unity — Custom UI Effects with Shader Graph](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-ShaderGraph.html)

### 3.2 TMP 효과 레이어링 가이드

| HUD | 권장 조합 | 이유 |
|---|---|---|
| GearHUD 기어 숫자 | Glow(강) + Outline(2px) + 시프트 때 scale 1.3 punch | 운전자 시선 최우선 포커스 |
| FuelHUD 라벨 | Underlay(soft shadow) + crit 시 Glow 빨강 | 배경 대비 가독 + 경고 고조 |
| CountdownHUD 숫자 | Glow(중) + 색 전환 시 Punch scale + tick SFX | 시간 긴박감 |
| ResultPanel 큰 숫자 | Outline + 카운트업 tick + 최종값 punch scale | 정산 쾌감 |
| OrderBoard 카드 텍스트 | Underlay only | 과하면 카드가 노이즈됨 |

> 참고: [TMP Distance Field Shaders](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2/manual/ShadersDistanceField.html), [TMP Shaders 전반](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2/manual/Shaders.html)

---

## 4. 레이싱/시뮬 레퍼런스 + 차용 기법

### 4.1 게임별 HUD 분석

| 게임 | HUD 특징 | PitCrew 차용 가능성 |
|---|---|---|
| **Forza Horizon 5** | 디지털 속도 + 기어, 좌하단 미니맵, 중앙 하단 컴팩트. 커뮤니티는 "더 미니멀" 요구 많음 | 높음 — 현재 PitCrew가 이미 비슷한 배치 |
| **Gran Turismo 7** | 원형 RPM 다이얼 + 랩타임. 프로 시뮬 톤 | 낮음 — 아케이드 지향(D-007)과 상충 |
| **Dirt Rally 2.0** | 페이스 노트 중앙(디에게틱), 미니멀 주변 | 중 — 델리버리 화살표가 페이스노트 성격. 중앙 바 고려 |
| **Euro Truck Simulator 2** | 대시보드 일체형, GPS 미니맵 | 중 — 화물 게임 성격 일치. 허브 씬(H1)에서 재검토 |
| **Death Stranding** | 카고 좌우 밸런스 막대, 원형 아이콘 단순화, 색으로 무게 구분(흰/노/빨) | **매우 높음** — 배달 게임 DNA 일치. 연료/시간/화물을 색 3단계로 통일 |
| **Pacific Drive** | 디에게틱 끝판, 계측기 다수, 대시보드에 모든 정보 | 중 — v1 이후 허브 씬(H1)에서 참고. MVP는 non-diegetic 유지 |

### 4.2 PitCrew 차용 TOP 5

| # | 레퍼런스 | 차용 아이디어 | 실행 |
|---|---|---|---|
| 1 | Death Stranding 색 3단계 | 연료/시간/화물 전부 흰→노→빨 통일(현재 상태 sprite와 이미 정렬) | 라벨·아이콘 색 체계만 일괄 정리 |
| 2 | Forza HUD 위치 스냅 | HUD 진입 시 외곽에서 슬라이드 인 (좌하 ← 왼쪽, 우하 ← 오른쪽) | PrimeTween 1줄 씬 시작 시 |
| 3 | Dirt Rally 중앙 바 | DeliveryHUD 화살표를 중앙 하단 얇은 바로 재위치 가능성 검토 | 레이아웃 실험만, 구현 보류 |
| 4 | Pacific Drive 계측기 그룹화 | FuelHUD를 "fuel + RPM 더미 + boost 자리"처럼 그룹 패널로 시각 통합 | MVP 이후 |
| 5 | Death Stranding 밸런스 막대 | 시간 남음을 좌우 밸런스 막대처럼 (targets vs remaining) | MVP 이후 |

> 참고: [Game UI Database — Forza Horizon 5](https://www.gameuidatabase.com/gameData.php?id=1225), [How Pacific Drive uses diegetic UI](https://www.gamedeveloper.com/business/how-pacific-drive-uses-a-diegetic-ui-to-help-players-fall-in-love-with-a-car-), [Game UI Database — Death Stranding](https://www.gameuidatabase.com/gameData.php?id=371), [Racing Game Design Principles](https://gamedesignskills.com/game-design/racing/)

---

## 5. MCP / 툴 현황 (2026-04 기준)

### 5.1 Coplay MCP (이미 사용 중) — 활용 확대 후보

현재 주로 `play_game` / `get_unity_logs`만 사용. UI 관련 직접 도구:

| 도구 | 현재 활용 | 활용 제안 |
|---|---|---|
| `create_ui_element` | 0 | HUD prefab scaffold 자동화 |
| `set_ui_layout` | 0 | anchors/pivot 일괄 정돈 |
| `set_ui_text` | 0 | 런타임 텍스트 변경 검증 |
| `capture_ui_canvas` | 0 | **즉시 유용** — 테스트 툴 `CaptureUIForReview.cs` 대체 가능 |
| `create_prefab` / `add_component` | 저 | HUD 프리팹 표준화에 유용 |
| `generate_sfx` | 0 | 숫자 tick·swoosh SFX 생성 |

### 5.2 Figma → Unity 파이프라인

- **Unity Importer (Figma community plugin)**, **Unity Figma Importer (cdmvision)**, **UnityFigmaBridge (simonoliver)** 모두 활발.
- 단 Kenney sprite 재사용 + 디자이너 부재 상황에서는 **MVP 도입 ROI 낮음**. v1 이후 허브 씬·커스텀 숫자 레이아웃이 복잡해지면 재고.

### 5.3 에셋 스토어 기타 후보

| 에셋 | 가격 | PitCrew ROI |
|---|---|---|
| Graphy (FPS/램 HUD) | 무료 | 중 — 개발 도구로는 유용 |
| Procedural UI Image | 무료 | 저 — Kenney 9-slice로 충분 |
| UI Effects (Asset Store 263772) | 무료 | UIEffect와 중복 |
| DOTween Pro | €15 | **기각** — PrimeTween 무료로 상위 호환 |

> 참고: [Unity Importer — Figma](https://www.figma.com/community/plugin/1047282855279327962/unity-importer), [TweenPerformance 벤치](https://github.com/AnnulusGames/TweenPerformance), [DOTween vs LeanTween vs PrimeTween 2026](https://omitram.com/unity-tweening-guide-dotween-leantween-primetween/)

---

## 6. Kenney UI Pack 미사용 매트릭스

`F1/Assets/UI/kenney_ui-pack/` 실측. 색: Grey/Blue/Green/Yellow/Red/Extra 6, variant: Default/Double 2. 각 색당 ~80 sprite.

### 6.1 카테고리별 재고

| 카테고리 | 파일 예 | 현재 사용 | PitCrew 활용 후보 |
|---|---|---|---|
| `button_rectangle_*` (border/flat/gloss/gradient/line/depth_*) | Grey/Yellow 계열 | flat만 사용 | **gloss** → Confirm 버튼, **gradient** → 강조 CTA |
| `button_round_*` / `button_square_*` | 전 색상 | 0 | Pause/Resume/Restart 아이콘 버튼, 기어 up/down |
| `check_round_*` / `check_square_*` / `check_square_grey_checkmark` 등 | Grey | 0 | 옵션 토글, 목표 체크리스트(3개 배송 완료 체크) |
| `icon_checkmark` / `icon_cross` / `icon_circle` / `icon_outline_*` | Grey | 0 | ResultPanel 성공/실패 아이콘, OrderBoard 카드 상태 |
| `arrow_basic_{n,s,e,w}[_small]` / `arrow_decorative_*` | 전 색상 | Yellow/arrow_basic_n | **decorative arrow** → 방향 화살표 교체, 장식 분위기 |
| `slide_horizontal_*` / `slide_vertical_*` / `slide_hangle` | Grey | wide section만 | slide handle → 설정 슬라이더(감도/음량) |
| `star` / `star_outline` / `star_outline_depth` | Grey | 0 | ResultPanel 등급(별 1/2/3) |
| `divider` / `divider_edges` (Extra) | - | 0 | OrderBoard 카드 구분선, ResultPanel 섹션 나눔 |
| `icon_arrow_down_*` / `icon_arrow_up_*` / `icon_play_*` / `icon_repeat_*` (Extra) | - | 0 | 기어 up/down 아이콘, Restart 버튼 아이콘 |
| `input_rectangle` / `input_square` (Extra) | - | 0 | 일반 텍스트 입력 없음. 보류 |

### 6.2 space-expansion 팩 (별도 디렉터리)

- `bar_round_*` / `bar_square_*` (large/small, l/m/r 분할) — **Fuel/Time 바 교체 유력**. 현재 slide_horizontal_grey_section_wide보다 더 "계측기" 톤.
- `button_square_header_*` (blade/large/notch + screws) — Sci-fi 톤 강함. Kenney 일반 팩과 이질. **보류** (PitCrew 톤과 불일치).
- `crosshair_color_*` — 레이싱 무관, 보류.

### 6.3 사용률

- **현재 사용:** 약 3~4 sprite
- **잠재 활용 후보:** 약 20~30 sprite (카테고리당 2~3)
- **전혀 안 쓸 것:** space-expansion 중 header/crosshair 계열

### 6.4 즉시 적용 후보 TOP 3

| # | sprite | 적용 위치 | 효과 |
|---|---|---|---|
| 1 | `Extra/divider.png` + `divider_edges.png` | OrderBoard 카드 구분선, ResultPanel 섹션 구분 | "구성감" 즉발 |
| 2 | `Grey/star_outline_depth.png` | ResultPanel 등급 표시(별 0~3) | 정산 쾌감 |
| 3 | `kenney_ui-pack-space-expansion/bar_round_small_*` | FuelHUD 가로 바 교체 | 계측기 톤 상승 |

---

## 7. 착수 Top 5 — 1주 안에 "게임 같은 태"

**전제:** MVP 4주차 1주 내 추가 작업. 신규 시스템 0. 기존 HUD 6종의 표현 레이어만 보강.

| 순위 | 목표 느낌 | 구현 수단 | 투자 | 리스크 |
|---|---|---|---|---|
| **1** | **TMP 전 HUD 질감 업그레이드** — "활자→게임 숫자" | GearHUD/CountdownHUD/ResultPanel에 TMP Glow + Outline 머티리얼 프리셋 (Bloom 이미 ON). FuelHUD 라벨에 Underlay soft shadow | 2~3h | 낮음. material preset 6~8개. 전 씬 즉시 반영 |
| **2** | **PrimeTween 도입 + 이징 패스** — "튕기고 붙는" 피드백 | PrimeTween(OpenUPM) 추가. HUD 진입 슬라이드 인(Back.Out), 시프트 punch scale(Elastic.Out), ResultPanel 펼침(Back.Out) | 3~4h | 낮음. `Tween.Scale` 10줄. DOTween 코드 없으니 마이그레이션 비용 0 |
| **3** | **UIEffect 도입 + 카드/패널 폴리싱** | OpenUPM `com.coffee.ui-effect` 추가. OrderBoard 카드 등장 시 `Dissolve`, 현재 active 카드에 `Shiny` 스윕, 저연료 FuelHUD에 `Gradient` 빨강 오버레이 | 3~4h | 중. 셰이더 variant 증가. URP 호환 확인 필수 |
| **4** | **숫자 카운트업 + tick SFX** — ResultPanel 정산 쾌감 | PrimeTween `Tween.Custom(int)` + `UISounds.Click` 16Hz throttle. 최종값 도달 시 punch scale + `Confirm` SFX | 2h | 낮음. 숫자 1~3개 대상만 |
| **5** | **화면 외곽 경고 + 숨 가쁨 호흡** | 전체 Canvas 테두리 vignette Image 1장. 연료 10%·시간 10초 시 빨강 pulse(초당 호흡수 임박도에 비례). PrimeTween `YoyoStyle.Easy` | 2~3h | 낮음. vignette sprite는 Kenney `button_rectangle_line` 9-slice로 가능 |

**총 12~16시간 = 1.5~2 세션.** MVP 남은 스코프 침해 없음. 전부 기존 구조 위 레이어 추가.

### 7.1 완료 후 남길 연기 목록 (v1 이후)

- Shader Graph UI 스캔라인/홀로그램 (허브 씬 한정)
- ParticleEffectForUGUI 스플래시 (델리버리 완료 폭죽)
- 디에게틱 대시보드 (Pacific Drive 결, H1 허브 씬)
- Death Stranding 밸런스 막대 재설계
- UI Toolkit 전면 이관 재평가

---

## 핵심 3줄 요약

1. **UGUI 유지. UI Toolkit 전환은 v1 이후** — 세션 13에서 막 안정화된 구조를 흔들 이유 없음. 런타임 애니 제약이 레이싱 HUD 요구와 정면 충돌.
2. **무료 3종 세트 + 무료 트윈 도입으로 80% 체감 상승 가능** — Coffee UIEffect / ParticleEffectForUGUI / SoftMaskForUGUI + PrimeTween. OpenUPM 10분, 학습 반나절.
3. **1주 투자 Top 5 순서 = TMP 질감 → PrimeTween 이징 → UIEffect 카드 폴리싱 → 카운트업 tick → 외곽 경고.** 총 12~16시간, MVP 스코프 침해 0, "활자→게임 숫자" 체감 즉발.
