# 사람 아티스트 없이 "로우폴리 + 저해상도 화면 + 밝은 마을"을 높은 품질로 — 방식 조사 (2026-10-27)

조사: 하위 에이전트(웹 검색·페이지 직접 확인). 커밋 안 함.
전제: AI 생성 이미지·소리·모델 투입 금지. bpy 코드 아트는 5판째 5~6/10(`2026-10-27-quality-gap.md`).
기준 화면: ①A Short Hike · ⑥EDC · ⑫Crow Country · ⑦Jalopy · ⑤Wheel World(색).
표기: [추정] = 내 판단, 출처 없음. 가격은 2026-10-27 페이지 기준(세일 변동).

---

## 0. 한 줄 결론

- 손맛은 "모델"이 아니라 **사람이 만든 텍스처·형태 + 장면 밀도**에서 나온다. 기준 게임 개발자 셋 다 자기 손으로 단순하게 만들었다.
- 에이전트는 **배치·조립·재칠·변형**에 쓰고, 형태와 질감의 원본은 **사람이 만든 것(팩·CC0 사진)**에서 가져온다.
- 추천 = **안 A(사람 제작 팩 + 사진 텍스처 재칠 + 에이전트 배치)** 로 장면 1컷 시험 → 부족한 "이야기 건물"만 **안 C(소액 외주)**.

---

## 1. 기준 게임은 어떻게 만들었나 (근거)

| 게임 | 제작자 말 | 우리에게 주는 뜻 |
|---|---|---|
| ⑥ EDC | "나는 아티스트보다 프로그래머다… 가능한 한 로우폴리, 픽셀아트 아니면 **사진 짜깁기(photobash) 텍스처**". "지오메트리는 최소, 디테일은 텍스처". 환경은 **Photoshop 사진 짜깁기**, 트럭·상자·사람은 손 픽셀. 그림자·햇빛 없음, **주변광 한 색 = 안개 색**, SSAO 살짝. | 형태는 박스로 충분. 질감 = 실사진을 사람이 고른 것. 빛은 평평. |
| ⑫ Crow Country | "3D 모델링 경험이 많지 않아 단순한 스타일로". 캐릭터는 FF7 풍, 환경은 FF7 프리렌더 배경 느낌. | 단순함은 한계에서 나온 선택. 밀도·구도로 채움. |
| ① A Short Hike | 1인 개발 + 가족·친구가 일부 모델 제작. "크고 바삭한 픽셀" 화면. | 모델 단순, 화면 해상도 층에서 픽셀감. |

출처
- EDC: https://80.lv/articles/inside-the-making-of-easy-delivery-co
- Crow Country: https://www.vg-reloaded.com/interview-crow-country-developer-adam-vian/
- A Short Hike: https://www.gamedeveloper.com/business/road-to-the-igf-adam-robinson-yu-s-i-a-short-hike-i- , https://www.trueachievements.com/news/ta-playlist-wrapup--a-short-hike

→ 셋 다 "형태 단순 + 사람 눈으로 고른 질감 + 장면 밀도". 우리는 형태·질감·판정을 모두 코드·AI 로 해서 "규칙 지킨 평균"에 멈춤.

---

## 2. 사람 제작 에셋 팩 후보 (14개)

### ⚠ 먼저 알 것 — 2025~26 itch PSX 팩은 AI·스크립트 생성이 많다
- itch 는 2025년부터 프로젝트에 **"Generative AI disclosure"** 칸을 둔다. 팩 페이지 `Content` 줄에 `No generative AI was used` 또는 `AI Disclosure: AI Assisted` 가 뜬다.
  https://itch.io/t/4309690/generative-ai-disclosure-tagging
- 우리 테마(항구·일본 골목·주유소)에 딱 맞는 최신 팩 다수가 **AI Assisted** 였다. 일부는 우리와 같은 방식("모델·텍스처 전부 Blender/Python 스크립트로 생성")이라고 직접 적었다. → **제외**(아래 표 맨 밑).
- 필터: itch 에서 `No generative AI was used` 표기 + 가능하면 2023년 이전 출시. Fab 는 "Created with AI" 태그 의무(https://assetforgeindex.com/policies/fab), Unity Asset Store 는 AI 콘텐츠 판매를 허용(https://support.unity.com/hc/en-us/articles/16456407029524) → 스토어 표기만으로는 부족, 출시일·제작 과정 스샷도 본다.
- Kenney·Quaternius 페이지엔 AI 표기 칸이 없다. 둘 다 생성형 AI 이전부터 손으로 만든 스튜디오 [추정: 역사적 사실, 개별 팩 확인은 안 함].

### 후보 표

| # | 팩 | 내용 | 스타일 | 라이선스 | 가격 | AI 표기 | 기준 화면과 맞나 [추정] | 링크 / 스샷 |
|---|---|---|---|---|---|---|---|---|
| 1 | Aquicor — PSX Low-Poly Boats & Fishing Props | 배·어구 소품 | PSX 텍스처 로우폴리 | 페이지에 문구 없음 → 구매 전 문의 | $2.99+ | No generative AI | 항구 소품. 높음 | https://aquicor.itch.io/psx-low-poly-boats-fishing-props-pack / https://img.itch.zone/aW1nLzI1MDgxOTQwLnBuZw==/original/Kc8apH.png |
| 2 | Aquicor — PSX Low-Poly Vehicles | 승용·밴·트럭·버스·오토바이·자전거, 폐차 | PSX | 문구 없음 → 문의 | $3.99+ | No generative AI | 길가 주차·지나가는 차. 높음 | https://aquicor.itch.io/psx-low-poly-vehicles-pack / https://img.itch.zone/aW1nLzI0OTg3MjE5LnBuZw==/original/tWNvR%2F.png |
| 3 | TheLoneGameDev — Retro PSX Style Car Pack | 차 13종(택시·경찰), FBX+.blend | PSX | 상업 OK, 크레딧 불필요, 원본 재배포 금지 | 번들 €50(단품가 페이지 확인) | No generative AI | 차. 중~높음 | https://thelonegamedev.itch.io/retro-psx-style-car-pack / https://img.itch.zone/aW1nLzI3NDcwNDQxLnBuZw==/original/xmDtxF.png |
| 4 | Kasuga — PSX Japanese shop building (+ "30+ Japanese Building/Apartment/Houses SET" 번들) | 일본 상점 건물, 번들에 건물 30+ | PSX 사진풍 텍스처 | 문구 없음 → 문의 | 단품 $0.50~1, 번들 ~$14 | No generative AI | **일본식 눈높이 생활(D-137)** 에 가장 가까움. 높음 | https://kasuga.itch.io/psx-white-japanese-shop-building / https://img.itch.zone/aW1nLzI2MjkxODQyLmpwZw==/original/AttqoZ.jpg |
| 5 | Kasuga — (PSX) 160+ Props Pack | 소품 160+ | PSX | 개별 팩 확인 필요 | 번들 ~$38 | 팩별 확인 | 자판기·간판류 밀도 채우기. 중~높음 | https://itch.io/s/168748/psx-160-props-pack |
| 6 | MarzoDev — PSX Rural Village Starter Pack | 목조 집·지붕·벽 모듈·소품 22 | PSX, glb | 상업·수정 OK, 재판매 금지 | $32 | No generative AI | 시골 목조 — 이탈리아 해안과는 다름. 중 | https://marzodev.itch.io/psx-rural-village-starter-pack / https://img.itch.zone/aW1nLzI2MTcyMTY3LmpwZWc=/original/NuVO4A.jpeg |
| 7 | WizP — Fishing village | 어촌 모델, 재질 2개(팔레트) | 단색 팔레트 로우폴리 | 상업 OK, 재판매 금지 | 이름값(무료 가능) | No generative AI | 단색이라 재칠 필요. 중 | https://wizp.itch.io/fishing-village / https://img.itch.zone/aW1nLzIwMzk3MjA0LnBuZw==/original/Otinez.png |
| 8 | Kenney — Retro Urban Kit | 도시 모듈 120 | 레트로 저해상도 텍스처 | **CC0** | 무료 | 표기 칸 없음 | 형태 뼈대로 좋음, 재칠 필수. 중 | https://kenney.nl/assets/retro-urban-kit / https://kenney.nl/media/pages/assets/retro-urban-kit/b9c649752d-1738147501/sample.png |
| 9 | Kenney — Retro Textures Fantasy | 벽·지붕·바닥 텍스처 115 | PS1 저해상도 | **CC0** | 무료 | 표기 칸 없음 | 텍스처 출발점. 중 | https://kenney.nl/assets/retro-textures-fantasy / https://kenney.nl/media/pages/assets/retro-textures-fantasy/1ac0cad8d6-1768475452/sample.png |
| 10 | Kenney — City Kit (Commercial) / Car Kit | 상가 건물 50, 차 45 | 단색 캐주얼 | **CC0** | 무료 | 표기 칸 없음 | **모바일 캐주얼 — 지금 문제와 같은 쪽.** 블록아웃용만. 낮음 | https://kenney.nl/assets/city-kit-commercial / https://kenney.nl/media/pages/assets/city-kit-commercial/ca60ea54ef-1747481474/sample.png |
| 11 | Quaternius — Ultimate Buildings Pack (+ Cars) | 모듈 건물 76, 아틀라스로 팔레트 교체 (2019) | 단색 로우폴리 | **CC0** | 무료 | (2019 출시) | 형태 깔끔, 손맛 약함. 낮음~중 | https://quaternius.com/packs/ultimatetexturedbuildings.html / https://quaternius.com/assets/images/fullres/ultimatebuildings.jpg |
| 12 | Stay Retro — PSX PS1 Seamless Floors | 바닥 텍스처, 색·해상도 제한 | PS1 텍스처 | CC0 표기 + "재배포·재판매 금지" 문구(모순 → 문의) | $4.79(세일) | No generative AI | 길·광장 바닥. 중 | https://stay-retro.itch.io/ps1 / https://img.itch.zone/aW1nLzI1Nzk3OTg5LnBuZw==/original/87VhIN.png |
| 13 | Mahalle Games — Low Poly Mediterranean Village (Unity) | 집 8·시장 부스 7·통 등, 손그림 지형 텍스처 4 | 로우폴리 손그림 | Unity 표준 EULA | $4.99 | 표기 없음 | 지중해 형태. 품질은 스샷 확인 필요. 중 | https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-mediterranean-village-167902 |
| 14 | Synty — POLYGON Town Pack | 마을 일체 | Synty 단색 폴리곤 | 상업 라이선스 | $49.99 | 표기 없음 | **너무 깨끗한 캐주얼, 누구나 알아봄.** 낮음 | https://syntystore.com/products/polygon-town-pack / http://syntystore.com/cdn/shop/files/POLYGON_Town_ICON_25496185-b060-4c54-9d14-2a014081e471.png |
| (참고) | VOYAGER 3D — Retro Japanese Rural Town (Fab) | 일본 주택가·도로·후처리 | 레트로, 버텍스 스냅 | Fab 표준 | $84.99 | 미확인 | Unreal 용(포스트 머티리얼·Epic 스켈레톤) → Unity 변환 비용. | https://www.fab.com/listings/d0982c97-5bba-40c2-96ff-237775bb9caa |

### 제외 (AI Assisted / 스크립트 생성 표기)
| 팩 | 페이지 표기 |
|---|---|
| heyheythere — PSX Japanese Town Kit / Fishing Village Kit / PSX Boats(항구) | "Made with AI assistance (code, text, models and textures, all generated by code)" — https://heyheythere.itch.io/psx-japanese-town-kit |
| bitkiln — PSX Sea Horror Modular Kit(항구·등대) | AI Assisted — https://bitkiln.itch.io/psx-sea-horror-modular-kit |
| DrakeanvilLabs — PSX Gas Station Kit | "All models and textures are procedurally generated by our own Blender/Python scripts" — https://drakeanvillabs.itch.io/psx-gas-station-kit-night-shift |
| talhamodels — PSX Gas Station | AI Assisted — https://talhamodels.itch.io/psx-gas-station |
| Nortimore — Andean Village Kit | AI Assisted (Graphics) — https://nortimore.itch.io/psx-andean-village-modular-kit |

→ 시사점: 우리 방식(에이전트 bpy)과 같은 결과물이 이미 시장에 넘친다. 그쪽을 사도 품질 천장이 같다 [추정].

### 팩 쓸 때 규칙 [추정, 프로젝트 규칙과 연결]
- 팩은 **형태 뼈대**로만 쓰고, 텍스처는 우리 팔레트·우리 사진 가공으로 **재칠**한다(여러 팩 섞어도 한 세계로 보이게).
- 3개 이하 팩만 섞는다. 같은 작가 팩 우선(형태 언어 통일).
- `art/provenance.csv` 에 팩 URL·라이선스·`ai_generated=no` 근거(페이지 표기 스샷) 기록. CC BY 는 `ATTRIBUTION.md`.
- 페이지에 라이선스 문구가 없는 팩(1·2·4)은 작가에게 "상업 게임, 수정, 빌드 포함" 한 줄 확인 받고 저장.

---

## 3. CC0 사진 텍스처 → PS1/EDC 느낌 가공

### 라이선스
| 출처 | 라이선스 | 메모 |
|---|---|---|
| ambientCG | CC0 1.0 | https://docs.ambientcg.com/license/ |
| Poly Haven | CC0 | https://polyhaven.com/license |
| Textures.com | CC0 아님. 게임에 넣는 건 허용, 원본 재배포 금지 | https://www.textures.com/faq-license — 무료 계정은 해상도·횟수 제한 [추정] |
| Wikimedia Commons | 사진별 상이 | 장소 분위기 보드용(기존 절차) |

### 검증된 방법 (커뮤니티 공통)
- 텍스처 크기: **한 장 64~128px, 중요 물건 256px**, 여러 장은 256 아틀라스로 묶음. (r/ps1graphics 조언, 튜토리얼 다수)
  https://www.reddit.com/r/ps1graphics/comments/1s058ye/i_want_to_get_better_at_lowpoly_psx_graphics/ · https://www.youtube.com/watch?v=PqXTFkYg7R4 · https://www.youtube.com/watch?v=A25NTdPGNaw
- 색: GIMP `Image → Mode → Indexed`(팔레트 생성)로 색 수 줄이기 — Silent Hill 식. (r/gamedev 스레드, 403 으로 본문 미확인: https://www.reddit.com/r/gamedev/comments/euwn93/texturing_silent_hill_1_style_how_exactly/)
- PS1 실기는 15bit 색 + 디더 → 현대 화면에선 "노이즈"로 보임(pwnisher): https://www.youtube.com/watch?v=cUiyph17F3A
- EDC 는 환경을 **사진 짜깁기**로 만들고 그림자는 텍스처에 직접 칠함(§1 출처).

### 우리 버전 순서 (지난 실패 반영) [추정]
지난번 "사진→32px/m Point 점무늬 = 더럽다"는 **고주파(1px 반점)** 가 원인. EDC 문법은 "텍셀 네모가 안 보이는 **큰 얼룩**". 그래서:
1. **고르기(사람 눈 대신 디렉터 1회)**: ambientCG/Poly Haven 에서 면마다 후보 3장 → 도현이 하나 고름. (이 "고르기"가 손맛의 대부분 [추정])
2. **평평하게**: 원본 조명 기울기 제거(큰 반경 하이패스 또는 평균 나누기), 정면 크롭.
3. **얼룩만 남기기**: 메디안/양방향 필터로 잔무늬 제거 → **박스·란초스로 축소**(Nearest 축소 금지 — 반점 생김).
4. **색 줄이기**: 면당 8~16색, 디더 없음(디더 = 지글거림, D-149). 팔레트는 기준 ⑤Wheel World·⑦Jalopy 색에서 뽑은 공용 팔레트로 매핑.
5. **대비·채도**: 명도 대비 소폭↑, 채도는 마을 낮게·자연 높게(D-153).
6. **손 픽셀 정보층**: 그림자 띠·물때·간판 글씨는 스크립트 픽셀(기존 규칙 3·정보층).
7. 측정: `tools/qa/surface_noise.py`(1px 반점 ≤0.3%, 중/저주파 비 ≤0.6) 통과 후 **Unity 실화면**에서만 판정.
- 참고 도구: ps1ify(웹, 텍스처 해상도·색 깊이 슬라이더) https://github.com/rodriabregu/ps1ify — 값 감 잡기용.

---

## 4. Unity PSX 셰이더·후처리 에셋 — 우리 것보다 나은가

| 에셋 | 가격 | Unity 6 / URP | 주요 기능 | 메모 |
|---|---|---|---|---|
| Retro Shaders Pro for URP (Daniel Ilett) | itch $15.99+ / Asset Store 동일 상품 | 6.0~6.5, Render Graph 지원 | Lit/**Texel Lit**/Vertex Lit, 버텍스 스냅, affine, 색 깊이, 디더, CRT, 팔레트 | 활발히 갱신(17일 전), No generative AI. https://danielilett.itch.io/psx-shaders-pro-for-unity-urp |
| ProPixelizer (ElliotB256) | $30 (세일 $15) | Unity 6 Render Graph | 물체별 픽셀화·**외곽선**·팔레트·디더, 정사영 creep 방지 | 탑다운/정사영 픽셀아트용. 원근 차 시점엔 덜 맞음 [추정]. https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/propixelizer-realtime-3d-pixel-art-177877 |
| PSX Shader Kit (Valerie Moza) | 무료 | 2021.3, 마지막 2023-05 | 저해상도·디더·밴딩·스냅·affine·인터레이스 | 오래됨. https://assetstore.unity.com/packages/vfx/shaders/psx-shader-kit-183591 |
| Kodrin URP-PSX | 무료 MIT | 2019.3 | 안개·픽셀화·디더 | 오래됨. https://github.com/Kodrin/URP-PSX |

판단 [추정]
- 이 에셋들이 파는 핵심(버텍스 스냅·affine·디더·CRT)은 **우리가 일부러 끈 것**(D-149: 지글거림 금지, 필름·CRT 금지). 우리 PCLook(1280×720 Nearest + 평평한 빛 + 안개) 이 이미 EDC 쪽 정답에 가깝다.
- 병목은 셰이더가 아니다. EDC 도 "주변광 한 색 + 같은 색 안개 + SSAO 살짝" 뿐(§1).
- 써 볼 가치: Retro Shaders Pro 의 **Texel Lit**(빛을 텍셀 단위로 끊음 → 큰 얼룩 질감과 어울림) 하나. $16 이니 장면 1컷 A/B 로만. 나머지는 사지 않음.

---

## 5. LLM/에이전트 3D 모델링 — 한계와 잘 되는 범위

| 출처 | 내용 |
|---|---|
| BlenderGym (CVPR 2025) | "최고 수준 VLM 시스템도 **사람 Blender 사용자에겐 쉬운 과제**에서 고전한다." https://arxiv.org/abs/2504.01786 |
| 3D-GPT (2023) | "LLM 이 3D 콘텐츠의 모든 요소를 직접 만드는 건 큰 어려움 — 3D 모델링 학습 데이터가 부족." 그래서 **절차 생성기 함수를 고르고 파라미터만 정하게** 함. https://arxiv.org/pdf/2310.12945 |
| LL3M (2025) | 다중 에이전트가 Blender 코드로 자산 생성. 강점은 **읽을 수 있는 코드 → 사람 지시로 고치는 공동 작업 루프**. https://www.alphaxiv.org/abs/2508.08228 |
| LayoutGPT (NeurIPS 2023) | 배치(레이아웃)에서 수치·공간 정확도가 **사람 사용자와 비슷**. https://layoutgpt.github.io/ |
| LayoutVLM (CVPR 2025) | **있는 자산**을 언어 지시로 배치 + 최적화 → 물리적으로 그럴듯한 장면. https://openaccess.thecvf.com/content/CVPR2025/papers/Sun_LayoutVLM_Differentiable_Optimization_of_3D_Layout_via_Vision-Language_Models_CVPR_2025_paper.pdf |

정리
- 잘 됨: **배치·조립**(키트 조각 고르고 놓기), **파라미터 변형**(높이·창 수·색 바꾸기), 일괄 작업(재칠·임포트·LOD·콜라이더), 수치 검사.
- 안 됨: **무에서 형태·질감 만들기**, "의도된 과장·손맛" 판단. 논문들도 직접 모델링 대신 기존 생성기·자산 위에 올린다.
- 우리 5판 정체(5~6/10)와 맞는 결과. → 에이전트 역할을 "만드는 사람"에서 "조립·배치하는 사람"으로 바꾼다.

---

## 6. 사람 외주 시세 (참고치)

| 출처 | 값 |
|---|---|
| Fiverr "retro low poly PSX/PS1 style buildings" (elegantcrow, 4.9★/46) | 기본 **$20**/1건, 3일, 수정 1회, 내외부 +$10 — https://www.fiverr.com/elegantcrow/do-retro-psx-or-ps1-style-buildings-for-your-game |
| Fiverr 레트로 로우폴리 검색(565건) | 시작가 $5~$100 — https://www.fiverr.com/gigs/retro-low-poly |
| r/gameDevClassifieds 판매 글 | "로우폴리 에셋 팩(건물 4종 모듈) 약 **$100**" — https://www.reddit.com/r/gameDevClassifieds/comments/192lw6w/ |
| Whimsy Games(스튜디오) | 모듈 3D 환경 세트 **$3,000~10,000**, 시급 동유럽 $50~100·아시아 $40~80 — https://whimsygames.co/blog/the-complete-guide-to-game-art-outsourcing-costs-timelines-and-how-to-choose-a-studio/ |

[추정] 실사용 가능한 PSX 건물 1채(텍스처 포함, 우리 팔레트·치수 맞춤, 수정 2회): **$50~300**. 4~5종 모듈 키트: **$300~1,500**.
위험: 싼 쪽은 AI 사용 가능성 → 계약에 "생성형 AI 금지" + 작업 중 스샷·.blend 원본 납품 조건.

---

## 7. 방식 조합 3안

| | 안 A: 팩 + 재칠 + 에이전트 배치 | 안 B: 박스 형태(에이전트) + 사진 텍스처(사람 고름) | 안 C: 소액 외주 키트 + 에이전트 변형·배치 |
|---|---|---|---|
| 형태 출처 | No-AI 팩(표 1·2·4·5·8) | 에이전트가 EDC 식 단순 박스 | 프리랜서 건물 3~5종 + 모듈 |
| 질감 출처 | §3 사진 가공으로 재칠 | §3 사진 가공 | 외주 원본 + §3 로 통일 |
| 에이전트 역할 | 임포트·재칠·배치·검사 | 형태·UV·배치 | 변형(높이·창·색)·배치 |
| 비용 | $20~80 + 토큰 | 토큰만 | $300~1,500 + 토큰 [추정] |
| 속도 | 장면 1컷 3~5일 [추정] | 3~5일 | 발주~납품 2~4주 [추정] |
| 예상 품질 | 7/10 [추정] | 6~7/10 [추정] — 형태는 기준 게임도 단순하니 질감이 받쳐 주면 오름 | 7~8/10 [추정] |
| 위험 | 팩끼리 스타일 충돌, 누가 봐도 아는 팩, 라이선스 문구 없음, 이탈리아 형태 팩이 부족 | 같은 천장(코드 형태)에 다시 걸릴 수 있음, 도현 "고르기" 시간 | 프리랜서 편차·AI 몰래 사용·소통 비용, 1인 디렉터 부담 |

### 추천
1. **안 A + 안 B 질감 파이프라인을 바로** — Kasuga 일본 건물 번들(~$14) + Aquicor 배·차($7) + Kenney Retro Urban(무료)로 **항구 길 고정 컷 1장**(quality-gap 처방 A)을 Unity 실화면에서 채운다. 텍스처는 전부 §3 로 재칠, 공용 팔레트.
2. 판정: ①·⑥ 기준 칸 옆 같은 배율. 7/10 넘으면 이 방식으로 확장.
3. 이탈리아 실루엣(D-137) 같은 팩이 없는 건물·랜드마크(정비소·등대·찻집 등 "이야기 건물")만 **안 C** 로 1채 시험 발주($50~150 [추정]) → 품질 보고 키트로 확대.
4. 하지 않을 것: 셰이더 팩 구매(Texel Lit A/B $16 하나만 예외), Synty·Kenney City 같은 캐주얼 팩, AI Assisted 표기 팩, bpy 무에서 모델링 6판째.

### 정할 것 (도현)
- 팩을 사서 재칠하는 것("남이 만든 형태")을 허용하나? 허용이면 위 1번 바로.
- 외주 1채 시험($50~150) 예산 허용하나?
