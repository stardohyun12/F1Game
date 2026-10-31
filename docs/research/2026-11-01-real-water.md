# 실제 물처럼 보이는 바다 — 무엇이 빠졌나 + 쓸 수 있는 구현 (D-233)

> research 2026-11-01 · 과제 20261031-2358 · 대상: `F1/Assets/Art/Look/DuskSea.shader` 5판 A(= 도현이 "아직 부족"이라 한 쪽)
> 근거 규칙: **원문을 직접 연 출처만 근거**. 못 연 것은 "요약만"으로 따로 적음. WebFetch 는 작은 모델이 요약해 주는 방식이라 인용은 원문 그대로가 아닐 수 있음.
> 보고서(결론 3줄·체크리스트): `docs/team/reports/research-20261031-2358.md`

---

## 0. 지금 바다 (코드에서 읽은 것)

- 정점 파 4개(너울 31 m·17 m, 잔물결 4.3 m·2.4 m) + 노멀 전용 잔물결 2개(1.15 m·0.7 m). 전부 방위 **−34° ~ +24° 안**, 즉 한 방향 원뿔 (`DuskSea.shader:11-17`).
- 파장이 17 m → 4.3 m 사이가 **비어 있음**. 6개 성분의 기울기(kk·A)는 모두 0.04~0.065 로 비슷.
- 반사는 **하늘 함수만**(`DuskSkyColor(R)`, `:171`). 해안 나무·부두·땅은 물에 안 비침.
- 프레넬 F0 0.02 Schlick (`:169`). 해 반사는 Blinn-Phong 꼴 `pow(dot(R,sun), _Shine)` + Toksvig (`:179-180`).
- 평평한 물 기준으로 내가 계산한 프레넬(눈높이 1 m): 수평 거리 2 m → F≈0.07 · 5 m → 0.35 · 20 m → 0.78. **5 m 보다 먼 물(화면 물 면적 대부분)은 35 % 넘게 거울**인데 그 거울에 하늘만 나옴.

---

## 1. 실제 물처럼 보이게 하는 요소 — 목록과 우선순위

우선순위는 **추정**(참고 사진 5장과 우리 A 캡처 1장을 같은 눈으로 비교한 것 + 문헌). 판정은 같은 카메라 전/후로 도현이.

| 순위 | 요소 | 근거(원문 연 것) | 우리 A 상태 |
|---|---|---|---|
| 1 | **장면 반사**(나무·땅·부두가 물에 거꾸로 비침, 잔물결로 세로로 찢김) | [GPU Gems 1장](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models): 큐브맵 반사를 깊이·시선각으로 조절. [Uber Stylized Water](https://github.com/MatrixRex/Uber-Stylized-Water): URP 용 "local planar reflection probe". [HDRP 물 문서](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@17.0/manual/WaterSystem-Overview.html): 한계에 SSR·프로브 언급. 사진 ①(나무 반사) | **없음**(하늘만) |
| 2 | **파 스펙트럼**: 파장 여러 개 + 방향 분산 | GPU Gems: 모양은 4개, 질감은 "약 15개, 주파수·방향 다양". [Catlike Coding](https://catlikecoding.com/unity/tutorials/flow/waves/): 파를 방향 다르게 더함, steepness 합 ≤ 1, 파장은 격자 칸의 2배 넘게. [konstantinkz 블로그](https://konstantinkz.github.io/blog/water): Gerstner 16개, 파마다 주파수 ×1.18·진폭 ×0.82. [HDRP](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@17.0/manual/WaterSystem-Overview.html): 바다는 다중 밴드. 사진 ③⑤(교차하는 잔파) | 6성분이 한 원뿔, 17→4.3 m 빈칸 → **줄무늬(코르덴)** |
| 3 | **세부 노멀**(비주기 잔물결 2겹, 서로 따로 움직임) | GPU Gems: "물 질감 속 잔파가 사실감을 좌우한다", "한 덩어리로 움직이는 복잡한 파보다 따로 움직이는 단순한 파가 낫다", 세부는 거리에 따라 기하 노멀로 수렴. [HDRP](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@17.0/manual/WaterSystem-Overview.html): swell/ripple 분리 | 정점 4 + 사인 2개뿐. 주기적 |
| 4 | **수평선 이음**: 먼 물 색 ≈ 하늘 색, 이음선이 가늘거나 흐림 | 사진 ③⑤. 문헌은 약함(konstantinkz 는 "거리 안개 추가"를 할 일로만 적음) | `_HorizonFade` 650 m 에서 50 % 만 섞임 |
| 5 | **해 반사 형태**: 빛길 폭·가로 줄무늬·수평선 이어짐 | [unitycoder FFT 셰이더](https://github.com/unitycoder/unitywatershaderfft): "Beckmann 미세면 BRDF" 목록 / Uber: 반사 크기·퍼짐·단단함 조절. **둘 다 목록 수준, 수식 못 읽음 → 약한 근거** | 있음(Toksvig). 폭이 좁은 편(사진 ① 쪽) |
| 6 | 파 마루 투과광(역광 SSS) | unitycoder FFT: "근사 SSS". [HDRP](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@17.0/manual/WaterSystem-Overview.html): Scattering 색. 사진 ④ | 없음 |
| 7 | 거품 | [antoniospg](https://antoniospg.github.io/UnityOcean/OceanSimulation): 변위 야코비안 ≤ 0 인 곳(임계값은 직접 고르는 편이 낫다). HDRP: 거품·코스틱은 단색 | 물가 띠 + 마루 듬성 있음 |
| 8 | 굴절·투명(물가) | HDRP: Refraction·Absorption Distance. Uber: 굴절·코스틱·얕은/깊은 색. 사진 ⑤(얕은 물에 바닥 비침) | 깊이 흡수색 있음, 굴절 없음. **눈높이 1 m 바다 시선에선 효과 작음(추정)** |
| 9 | 거리 LOD·밉 | GPU Gems(세부 노멀 거리 소멸), [Crest 문서](https://crest.readthedocs.io/en/stable/about/introduction.html)("LOD 전략·GPU 가속"), Catlike(파장 > 칸×2) | 있음(`_Filt` 필터 + Toksvig) |

**FFT/Tessendorf 는 하지 않는다**(제안): 화면 640×360 에 앉은 시선 한 군데라 비용 대비 이득이 작다고 봄(추정). 원문(Tessendorf 2001/2004)은 이번에 못 엶 — 인증서 오류·PDF 해석 실패라 **검색 요약으로만** 확인(Phillips 스펙트럼의 방향 항으로 바람과 직각인 파를 억제, 작은 파 감쇠). 근거로 세지 않음.

---

## 2. 노을·역광 바다 사진 5장 (전부 WordPress.org Photos — 사이트 약관상 CC0)

> 라이선스 문구: "Photo contributors submit their original content under the CC0 license."(사진 ①②의 페이지에서 직접 확인, 나머지 3장은 같은 사이트 약관). **리포에 복사 안 함, 링크만.** 촬영자가 AI 이미지를 올렸는지는 사이트가 보증하는지 확인 못 함 → 참고용으로만(체크리스트 1).
> 아래 관찰은 **내가 이미지를 열어 본 것**이고 폭은 눈대중.

| # | 사진 | 보이는 것 |
|---|---|---|
| ① | [prathamjain01 · 4096×2304](https://wordpress.org/photos/photo/844694648c/) · 잔잔한 호수(바다 아님) | **빛길 = 좁은 기둥**: 프레임 폭의 3~8 %, 가로 대시가 세로로 쌓임. 대시 하나가 1~2 px 높이. 해안 나무가 물에 **거꾸로 비치고 잔물결로 가로로 끊김**. 먼 물은 하늘색과 거의 같음 |
| ② | [Karina Aguilar Picado · 4080×3072](https://wordpress.org/photos/photo/969e3b7d90/) · 바다, 섬·배 | **빛길 = 넓고 이어짐**: 수평선 바로 아래 노란 띠 → 아래로 갈수록 가로 줄무늬로 갈라짐, 폭 프레임의 20~25 %. 가까운 물은 어두운 붉은/보라 = 위쪽 하늘색을 비춤 |
| ③ | [Priscilla Chacon · 3024×4032](https://wordpress.org/photos/photo/51269e3f2b/) · 해가 수평선에 걸림 | **수평선 아래 물이 하늘과 같은 주황-빨강**, 이음선은 가는 어두운 선 하나. 결이 다른 가로 잔파가 층층이 **교차**. 점 반짝임은 마루에만 2~3 px. 부서지는 파 = 흰 거품 + 어두운 골 |
| ④ | [ishtiyaqkabir · 5712×4284](https://wordpress.org/photos/photo/49868f476c/) · 부서지는 파 뒤에 해 | **역광 마루가 투과광으로 빛남**(해가 거품 속에 비침). 앞 물은 주황이 아니라 **위쪽 하늘의 청회색**을 비춤 → 반사색은 "보는 각도에 맞는 하늘" |
| ⑤ | [Abhishek Kumbhani · 2400×3200](https://wordpress.org/photos/photo/292677cc3b/) · 안개 낀 해 | **수평선이 안개로 하늘에 풀림**. 얕은 물은 투명한 갈녹색(바닥 비침) + 거품 레이스. 해 반짝임은 중간 거리 파면 한 조각만 |

**공통으로 "물처럼 보이게 하는 것"**
1. 먼 물의 색이 하늘 색에 붙는다(③⑤①). 수평선에서 물이 하늘보다 확 어두우면 판처럼 보임.
2. 잔파가 **여러 방향으로 교차**하고 먼 곳까지 가로줄 질감이 남는다(③⑤).
3. 해 반사는 폭이 바람(거칠기)에 따라 달라진다 — 잔잔 ① 좁고 줄무늬, 바람 ② 넓고 이어짐. 점이 아니라 **가로 대시**.
4. 반사는 **하늘만이 아니라 물가 물체**를 비춘다(①).
5. 역광이면 마루가 투과광으로 밝다(④).

**우리 A 캡처**(`docs/qa/evidence/sunset5/cmp_seated_A_B_v4_ref.png` 왼쪽 위)와 비교해서 보이는 것(내 눈, 추정):
- 파가 **한 방향의 긴 평행 띠**로 화면을 가로지름 → ③⑤의 교차하는 잔파와 다름.
- 해안 나무 바로 아래에 **반사가 없음** → ①과 가장 크게 다름.
- 빛길은 ①쪽(좁은 기둥 + 가로 대시)에 가까움. 이건 나쁘지 않음.
- 가까운 물은 어두운 갈보라 → ②와 비슷한 방향.

---

## 3. Unity 6 URP 에서 바로 쓸 수 있는 오픈소스 바다

조사일 2026-11-01. 메타는 GitHub API/페이지를 직접 열어 읽음.

| 후보 | 라이선스 | 최근 활동 | Unity 6 URP | 우리 용도 |
|---|---|---|---|---|
| [wave-harmonic/crest](https://github.com/wave-harmonic/crest) (Crest 4) | MIT | 마지막 push 2026-06-18, ★3,936, 열린 이슈 82 | **이 리포는 "내장 렌더러용"**. URP·HDRP 는 에셋스토어 별도 유료 패키지로 판다고 README 에 있음. HDRP 판은 $100(v4.23.0, 2026-06-18)을 [스토어 페이지](https://assetstore.unity.com/packages/slug/164158)에서 확인. **URP 판 가격은 못 열어 봄**. Crest 5 도 스토어 | **제외**(유료 X + 내장용) |
| KWS Water System | 상용 | — | URP 판 $79, KWS2 $159 — **검색 요약만**(원문 못 엶) | **제외**(유료 X) |
| [gasgiant/Ocean-URP](https://github.com/gasgiant/Ocean-URP) | MIT(LICENSE "Copyright (c) 2023 Ivan Pensionerov") | **마지막 push 2023-11-04**, ★253, 열린 이슈 4. 2026-03 이슈 #8 "README 가 낡았다" | 이슈 어디에도 Unity 6 언급 없음 → **호환 증거 없음**. 릴리스 없음. FFT 계산 셰이더 기반 | **제외**(Unity 6.3 에서 돌아가는지 알 수 없고, 360p B 스타일을 덮으려면 어차피 셰이더를 뜯어야 함) |
| [gasgiant/FFT-Ocean](https://github.com/gasgiant/FFT-Ocean) | MIT | — | 설명이 "프로토타입, 실제 프로젝트에 권장 안 함" | 제외 |
| [MatrixRex/Uber-Stylized-Water](https://github.com/MatrixRex/Uber-Stylized-Water) | **MIT**(API 확인) | **마지막 push 2026-10-05**, ★526, 열린 이슈 0. 커밋 메시지에 "v1.1.6 … Unity 6.0-6.3 shader compatibility"(2026-10-04) | **URP 전용, Unity 6000.0.30+**. 기능: 세계 좌표 Gerstner, 파 마루색, 평면 반사 프로브(+Unity 반사 프로브와 섞음), 굴절, 코스틱, 거품, 반사 크기·퍼짐 조절. 이슈 이력: 6000.1·6.2 에서 **셰이더 변형(variant) 한도 초과 컴파일 오류**(#2·#7, 닫힘), 사각 그림자(#5), 토온 셰이더와 핑크(#4), 평면 반사 뒷면만 렌더(#16, 닫힘) | **부품으로만**(아래) |
| [unitycoder/Uber-Stylized-Water](https://github.com/unitycoder/Uber-Stylized-Water) | MIT | 위 리포의 포크, 3★, 2025-01 | 원본이 더 최신 | 원본을 볼 것 |

**추천: 우리 셰이더를 고친다. 통째로 가져다 쓰지 않는다.**
1. 우리 셰이더에는 **팀 고유 부품**이 박혀 있다: `DuskSkyColor`(노을 하늘과 같은 함수), 구운 깊이 텍스처 `_DepthTex`, 12fps 박자, B 스타일 4단 컷·빛길 3단·물가 2겹. 외부 셰이더로 바꾸면 이걸 다 다시 이식해야 하고(= 어차피 외부 셰이더를 뜯음) 이득이 없음.
2. 쓸 만한 외부 코드는 **평면 반사 한 부품**뿐. Uber 는 MIT 라 읽고 베껴도 됨(라이선스 문구 보존). **다만 그 코드는 이번에 열어 읽지 못했음** — 반사 부품 도입 전에 먼저 읽고(카메라 생성·경사 클립 평면·RT 크기) Unity 6.3 에서 도는지 우리 프로젝트에서 확인할 것.
3. 저해상도 스타일에 덮어쓰기: Uber 문서에 저해상도/픽셀 항목 없음("언급 없음"). 우리는 B 를 최종 색 후처리로 얹는 구조라, 외부 물을 쓰면 그 셰이더 끝에 같은 컷을 넣게 됨 → 고치는 쪽과 같은 일.
4. 변형 한도 오류 이력(#2·#7)은 키워드를 많이 쓰는 셰이더의 문제. 우리 셰이더는 키워드를 안 씀 → 우리 쪽을 고치면 이 위험이 없음.
5. Unity 6000.3.27f2 에서의 호환은 **어느 후보도 직접 확인 못 함**(Uber 는 커밋 메시지로 6.0~6.3 을 주장할 뿐).

---

## 4. 우리 셰이더에 바로 넣을 수정 상위 5 (효과 큰 순 — 추정)

판정은 전부 **같은 카메라(Play 앉은 시점 rot 3,180,0, 640×360) A 전/후**, 그리고 FrameFlickerProbe 재측정(체크리스트 5·14).

### ① 장면 반사 — 해안 나무·부두·언덕이 물에 비치게
- **왜**: 사진 ①에서 가장 큰 "물이다" 신호. A 는 하늘만 비춤(`:171`). 5 m 밖 물은 35 %+ 거울(§0).
- **구현(평면 반사 RT)**:
  - `SunsetLook` 에 반사 카메라 하나(노을 켤 때만 활성). 메인 카메라를 해수면 높이 평면에 대칭시킨 위치·회전, 경사 near-clip 평면.
  - RT = 메인 RT 의 절반(320×180) — 잔물결이 어차피 번지게 하므로 충분(추정). 컬링 마스크는 나무·땅·부두만, 낚싯대·플레이어 제외, 최대 거리 ~300 m. 클리어 알파 0.
  - 셰이더: `_ReflTex` 를 화면 UV + N.xz × (0.03~0.06) 로 읽어 `skyR = lerp(skyR, refl.rgb, refl.a)`. 그 뒤 기존 프레넬 혼합 그대로 → 가까운 물(F<0.1)은 몸 색 유지.
  - URP 17: `UniversalRenderPipeline.SingleCameraRequest`(필드 destination·face·mipLevel·slice, [Unity 문서](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest.html) 확인)를 `RenderPipeline.SubmitRenderRequest` 와 함께 쓰는 것으로 기억하나 **문서에 호출 예가 없어 확인 필요**. 프로젝트에 기존 반사 RT 코드는 검색이 시간 초과로 못 찾음 → 없다고 단정 못 함.
- **위험**: 장면을 한 번 더 그림 → 프레임(지금 8.5 ms) 증가 측정 필요 / 360p 에서 RT 샘플 깜빡임 → 왜곡은 **큰 파 2개의 노멀만** 쓰기 / B 의 4단 컷이 반사색을 평탄하게 함 → 반사는 컷 **앞에서** 섞기 / 12fps 박자와 같이 갱신.
- **싼 대안(추정, 미검증)**: 해수면 높이의 *실시간* 반사 프로브 1개를 노을 켤 때 한 번만 갱신해 `unity_SpecCube0` 로 읽기. 근사지만 두 번째 카메라가 없음. **굽는 프로브는 금지**(체크리스트 4).

### ② 파 스펙트럼 넓히기 — 노멀 전용 파 +8
- **왜**: 6성분이 −34°~+24° 한 원뿔, 17→4.3 m 빈칸 → 평행 줄무늬. 문헌은 여러 파장·방향의 합(GPU Gems 약 15, konstantinkz 16).
- **구현**: `frag` 의 노멀 루프에 `[unroll]` 8개 추가. 파장 = 12 × 1.4^(−i) (12 m → 1.1 m, 간격비 1.4는 konstantinkz 의 ×1.18 을 8개로 줄인 내 값 = 추정), 방위 = 상수 해시로 −40°~+40°, 기울기 kk·A ≈ 0.03~0.05, 속도 √(9.8k)(기존 식), 기존 `_Filt` 필터 재사용. 정점 파는 그대로(4개).
- **위험**: programmer 실측 — 잔물결을 ±50~70° 로 줬더니 **소실점으로 모이는 줄무늬**가 나와서 좁혔음. → 방향은 ±40° 안, 교차 파는 약하게, ③ 으로 주기성을 깨서 보완. 픽셀당 sin·cos 8쌍 추가는 230k 픽셀이라 비용 작음(추정).

### ③ 비주기 세부 노멀 2겹
- **왜**: GPU Gems 가 사실감의 핵심으로 꼽은 부분. 사인 합은 아무리 늘려도 주기적.
- **구현**: 이미 있는 `dkNoise` 로 **미분 가능한 값 노이즈** 두 겹(스케일 ~0.9 m·0.35 m, 서로 ~35° 어긋난 방향, 서로 다른 속도)을 N.xz 에 더함. 거리에 따라 `fp` 필터로 소멸하고 그 기울기는 기존 `var`(Toksvig)로.
- **위험**: 값 노이즈는 축 정렬 격자 티가 남음 → 두 겹을 회전 좌표로. 360p 지글거림 → 12fps 박자(B) + 필터. 텍스처 import 없이 코드만이라 "굽기" 문제 없음.

### ④ 먼 물 → 하늘색으로 이어 붙이기
- **왜**: 사진 ③⑤①은 수평선 바로 아래 물이 하늘과 같은 색. A 는 `_HorizonFade (300, 650, 0.5)` 라 650 m 에서도 50 % 만 섞임(`:176`, 머티리얼 값).
- **구현**: 값만 조정하는 실험 — 섞는 양 0.5 → 0.8, 시작 300 → 150 m 와, 먼 물의 `_Shine`·`var` 에서 거울 항 가중. 코드 변경 거의 없음.
- **위험**: 너무 세게 하면 수평선이 사라져 바다가 하늘에 뜸(⑤처럼 안개 날씨 느낌). 도현이 노을 날씨를 어느 쪽으로 볼지 판정.

### ⑤ 빛길 형태 맞추기(폭·가로 줄무늬·수평선 이어짐)
- **왜**: 사진 ① 잔잔 = 좁은 기둥, ② 바람 = 넓고 수평선 노란 띠로 이어짐. A 는 ①쪽. 어느 쪽을 쓸지는 취향 → **도현 판정**.
- **구현**: `_Shine`(900)·`_Glint`(2.2) 와 파 기울기 합의 관계를 한 번 정리(바람이 세면 `_Shine` 이 작아져야 함). 수평선 쪽에서 빛길이 넓은 노란 띠로 이어지려면 ④ 와 함께 `hz` 블렌드에 해 방향 가중.
- **위험**: 근거가 목록 수준이라 확신 낮음. 후보 비교 컷(잔잔/바람 2장)으로만 판정.

**다음 후보(5위 밖)**: 마루 역광 투과광(사진 ④). B 의 4단 컷과 충돌하고 근거가 약해 보류. 굴절은 눈높이 1 m 시선에서 효과 작음(추정).

**작업 순서 제안**: ① → ②+③(같이) → ④⑤(값 실험). 한 번에 다 하면 60분 넘음(체크리스트 15).

---

## 5. 출처 비율

- **확인(원문을 열어 읽음) 20**: GPU Gems 1장 · Catlike Coding Waves · antoniospg UnityOcean · Unity HDRP WaterSystem 문서 · konstantinkz 블로그 · Crest GitHub(+API) · Crest 문서 · Crest HDRP 스토어 페이지 · Ocean-URP(+API·이슈) · FFT-Ocean · unitycoder FFT 셰이더 · MatrixRex Uber(+API·커밋·이슈) · unitycoder Uber(+API) · SIGGRAPH 역사 페이지(초록) · Unity URP SingleCameraRequest 문서 · 사진 페이지 5개(이미지는 열어 눈으로 봄)
- **요약만 5 (근거로 안 셈)**: Tessendorf 2001·2004(인증서 오류/PDF 해석 실패, 검색 요약) · arXiv 1109.6494 서베이(PDF 해석 실패) · Sea of Thieves SIGGRAPH 슬라이드 PDF(해석 실패, 초록만 읽음) · KWS 가격(검색 요약) · Crest URP 스토어 가격(페이지가 HDRP 판으로 열림)

## 6. 모르는 것

- 해 반사 거칠기(Beckmann/GGX) 원문 근거가 **목록 수준 2개**뿐 — 수식을 열어 읽은 곳 없음. ⑤ 는 사진 비교에 의존.
- Tessendorf 원문과 Sea of Thieves 슬라이드를 못 읽음 → 파 스펙트럼의 방향 항·역광 SSS 의 구체식은 근거 없음.
- Uber Stylized Water 의 평면 반사 코드를 안 읽었음(① 구현 전에 읽을 것). Unity 6000.3.27f2 에서의 호환은 어느 외부 후보도 미확인.
- 기존 프로젝트에 반사 RT 코드가 이미 있는지(검색 시간 초과).
- ① 의 프레임 비용(두 번째 카메라)과 360p 깜빡임은 **측정 전**.
- 순위는 추정 — 같은 카메라 전/후를 보기 전에는 어떤 항목이 큰지 모름.
