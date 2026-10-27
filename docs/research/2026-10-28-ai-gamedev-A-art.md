# AI 에이전트로 게임 아트·맵 만들기 — 2025~26 외부 사례 → 우리 파이프라인 개선 (2026-10-28, 리서치 A: 아트)

표기: [확인] = 이 조사에서 페이지를 직접 열어 본 것 / [추정] = 검색 요약·내 판단.
이미 다룬 것은 반복하지 않는다(BlenderGym·LayoutVLM·LL3M·BlenderAlchemy 일반론, EDC·ASH 제작 방식, 팩·외주 비교 → `2026-10-27-*` 문서).
이번 문서는 **새로 찾은 것만** 적는다.

---

## 1. 업계 현황 — 에이전트를 어디에 쓰나 (2025~26)

- 구글 클라우드·해리스 조사(2025-08, 615명): 개발자 87%가 AI 에이전트를 쓴다. 쓰임 1위는 "에셋·콘텐츠 최적화"(44%). [확인] https://www.gamesindustry.biz/87-of-game-developers-are-using-ai-agents-in-their-workflows-says-new-survey
- GDC 2026 업계 보고(2,300명+): 52%가 생성형 AI가 업계에 해롭다고 답했다. 2년 전 18%에서 세 배. 긍정은 7%. [확인] https://www.implicator.ai/game-developers-anti-ai-sentiment-hits-record-high-in-gdc-2026-survey/
- 같은 보고서: 업계 36%가 일에 생성형 AI 도구를 쓴다. [추정 — PDF 검색 요약] https://images.reg.techweb.com/Web/UBMTechweb/%7Bfbdfe6c4-e33f-458e-a8ac-96db55fda684%7D_541400_GDC26_PDF_SOTI_Report_Final.pdf
- 뜻: 플레이어·개발자 모두 "AI 아트"에 반감이 크다. 우리 "AI 생성 에셋 0" 규칙은 시장에서도 맞다. 에이전트는 **최적화·검사·조립** 쪽이 받아들여진다. [추정]

### 1-1. 2026 Blender MCP 흐름 — "원격 조종"에서 "렌더 보고 고치기 + 예산 검사"로
- `blender-asset-mcp`(2026-09): 도구 126개. 에이전트가 `render_preview`·`render_turntable` 로 실제 렌더를 보고 고친다. "렌더를 보기 전엔 완료라 하지 말라"가 기본 지시. `validate_game_asset` 으로 삼각형·재질 수·UV·적용 안 된 변환·n-gon 을 검사. 작업 전 체크포인트 → 실패 시 되돌림. [확인] https://github.com/yi00it/blender-asset-mcp
- `blender-iso-mcp`(2026-09): **팔레트 색 번호로만 칠하는 NPR 재질** + 고정 카메라 + albedo/normal/depth/그림자 패스 분리 출력. 스타일을 도구 쪽에 고정한다. [확인] https://github.com/fr4ncisx/blender-mcp
- MCP for Blender 문서: 게임용 쓰임새를 Poly Pizza·Poly Haven·Sketchfab **에셋 검색·가져오기 + 배치**로 소개. [추정 — 검색 요약] https://www.mcp-for-blender.com/docs/use-cases/gaming
- Claude/Codex 용 Blender 스킬 묶음(감독 스킬: 분석→계획→예산→브리프)이 GitHub 에 여럿 생겼다. [추정 — 검색 요약] https://github.com/arjun988/blender-skills/blob/main/.claude/skills/blender-director/SKILL.md
- 트림 시트 굽기 가이드: "모듈 키트 10개가 2K 트림 텍스처 1장을 공유, 조각마다 트림 띠 위에 UV 섬". [추정 — 검색 요약] https://github.com/PoBruno/mcp-blender-agent/blob/main/.claude/docs/research/pipelines/B4-uv-and-baking.md
- 우리에게: MCP 자체는 bpy 와 같은 천장(10-27 결론 유지). 가져올 것은 **도구 쪽 고정 장치** 세 개 — ① 팔레트 번호로만 칠하기 ② 렌더 확인 전 완료 금지 ③ 예산·UV·변환 자동 검사. [추정]

---

## 2. 사진 → 게임 텍스처 (delight·타일·팔레트)

### 2-1. 빛 빼기(delighting) — 생성형 없이
- "AI delighter" 도구가 많다(PlayTex 등). 대부분 학습 모델이라 우리 규칙에선 쓰지 않는다. [추정] https://www.playtex.ai/tools/material-delighter
- 고전 방식 1 — **큰 모양 지우기(equalization)**: 사진을 잘라 원근 펴고 → 이음매 도장 → 웨이블릿 분해로 큰 스케일 층(2~4K 기준 300~400px 이상)을 지운다. 큰 명암·그라데이션이 타일 반복을 드러내기 때문. [확인] https://mango.blender.org/artwork/w-i-p-tileable-textures/
- 고전 방식 2 — 같은 글 댓글: 복제 층을 가우시안 블러(폭 1/3) → "Grain extract" → 블러의 평균색을 "Grain merge" 로 되돌림. = **원본 ÷(또는 −) 저주파 + 평균색**. 스크립트 몇 줄로 된다. [확인] 같은 URL
- 고전 방식 3 — 그림자·하이라이트 슬라이더로 줄인 뒤 **재료별 명도 목표에 맞추기**(예: 바위 sRGB 0.6). "albedo 엔 방향광·AO 가 없어야 조명이 바뀌어도 맞다". [확인] https://shadermap.com/tutorials/creating-an-albedo-map-from-a-photographic-texture/
- 우리에게: 부품 크롭 뒤 `delight.py`(저주파 나누기 + 평균색 + 재료별 명도 목표 클램프)를 고정 단계로. 지금 "빛 평평하게"는 손 지시라 판마다 다르다. [추정]

### 2-2. 타일·변형 — 생성형 없이
- Embark `texture-synthesis`: 예시 기반 **비신경망** 합성(Multiresolution Stochastic Texture Synthesis). 사진 한 장에서 변형·이어 붙이기. 2023-10 보관(읽기 전용)됐지만 바이너리·코드는 그대로. [확인] https://raw.githubusercontent.com/EmbarkStudios/texture-synthesis/main/README.md
- 학습 모델이 아니라 우리 "AI 생성 0" 규칙과 충돌하지 않는다. 단 결과를 "사진 변형"으로 장부(`provenance.csv`)에 적는다. [추정]
- 무료 이음매 제거 튜토리얼(Krita·Photopea·Ucupaint)도 2025~26에 많다. 핵심은 같다: 오프셋 → 이음매 도장 → 큰 명암 빼기. [추정 — 검색 요약] https://www.youtube.com/watch?v=r3D-JcWx3Pg
- 주의: Dream Textures 같은 "무료 Blender 텍스처 애드온"은 Stable Diffusion 이다. 금지. [추정 — 검색 요약] https://3d-agent.com/blender-ai/texturing

### 2-3. 명도·팔레트 기준표
- Physically Based 데이터베이스: 재료별 albedo 값(sRGB 선형). 예: 콘크리트 0.51, 벽돌 0.26/0.10/0.06, 풀 0.10/0.13/0.04, 새 아스팔트 0.04, 판지 0.35/0.21/0.11. JSON API 도 있다. [확인] https://physicallybased.info/ · https://github.com/AntonPalmqvist/physically-based-api/blob/main/deploy/v2/materials.json
- Unity 에도 PBR Material Validator(albedo 명도 범위 밖이면 색 표시)가 있다. [추정 — 검색 요약] https://docs.unity3d.com/2022.1/Documentation/Manual/MaterialValidator.html
- 우리에게: 재료별 명도 목표를 이 표에서 잡고, 양자화 **전에** 부품 평균 명도를 맞춘다. 스타일 게임이라 절대값보다 **재료끼리 순서**(아스팔트 < 벽돌 < 회벽·콘크리트)가 중요하다. [추정]
- k-means 팔레트 추출 스크립트는 흔하다(오픈소스 다수). 팔레트를 사진에서 뽑을 땐 이미 아는 함정(따뜻한 색 사진 섞기)을 유지. [확인 — 검색 결과 저장소 목록만] https://github.com/Chipdelmal/colorPaletteExtractor

---

## 3. 스타일 일관성 게이트 — 자동 수치 · VLM 판정의 함정

### 3-1. VLM 판정자의 알려진 편향 (2025~26 논문)
- **위치 편향이 크다.** 4B VLM 판정자는 첫 이미지를 49% 골랐다(우연 28%). 순서를 바꾸면 60% 프롬프트에서 선택이 바뀌었다. CLIP 유사도 기준선보다도 못했다. [확인] https://arxiv.org/html/2610.01243v1
- 같은 논문: **두 순서에서 같은 답이 나온 결정만** 남기면 무작위보다 훨씬 낫다. 반면 "약한 두 번째 판정자와의 합의"는 틀린 답을 남겼다. 판정자를 바꾸면 필터를 다시 검증해야 한다. [확인] 같은 URL
- 3D 메시 판정 프로토콜(2026-06): 고정 24뷰 렌더 + **서로 다른 두 VLM 계열** + 두 순서 모두 묻고 순서 일관 판정만 사용. 두 계열 일치 κ=0.66. 렌더 CLIP 은 **우연 수준**, 기하 검사 수치는 눈에 띄는 결함일 때만 맞았다. [확인] https://arxiv.org/html/2606.18451v1
- **시각 조작에 속는다.** 밝기·감마·여백(검은 패딩)·덧씌운 글자·박스만 바꿔도 점수가 오른다. 프롬프트로는 일부만 막힌다. [확인] https://ar5iv.labs.arxiv.org/html/2505.15249
  - 우리에게: 두 컷의 밝기·크롭·여백이 다르면 판정자는 내용이 아니라 그 차이로 고를 수 있다. 비교 전에 **같은 노출·같은 크롭·같은 크기**로 맞춘다. [추정]
- BlenderGym(CVPR 2025): 최신 VLM 도 "Blender 사용자에겐 쉬운" 편집에서 고전. 후보를 많이 뽑고 **검증자로 고르는** 추론 확장은 검증자가 믿을 만할 때만 효과. 검증자 자체도 여러 번 돌리면 좋아진다. [확인] https://openaccess.thecvf.com/content/CVPR2025/papers/Gu_BlenderGym_Benchmarking_Foundational_Model_Systems_for_Graphics_Editing_CVPR_2025_paper.pdf
- 3DCodeBench(2026-06): VLM 12종을 절차 3D 코드 모델러로 평가. 자동 수치만으론 부족하다고 보고 **사람 쌍대 선호 Elo(3DCodeArena)** 를 따로 둔다. [확인 — 사본 페이지] https://github.com/will-rice/llm-self-improvement-papers/blob/main/papers/arxiv-2606-01057--aec825775a38.md
- 3DGen-Bench·Eval3D·Gen3DEval(CVPR 2025)도 사람 선호 데이터로 판정 모델을 맞춘다. 결론은 같다: **사람 앵커 없이 판정자를 믿지 말 것.** [추정 — 검색 요약] https://arxiv.org/html/2503.21745

### 3-2. 지금 우리 판정(JUDGE.md 쌍대비교)과의 차이
| 외부 권장 | 우리 지금 | 고칠 것 |
|---|---|---|
| 두 순서 모두 + 순서 일관만 채택 [확인 2610.01243, 2606.18451] | 이미 함: 갈림 = 판정 불가 → 도현 (JUDGE.md:38) [확인 — 내부 파일] | 유지 |
| 판정자 계열 둘(κ 확인) [확인 2606.18451] | Sonnet 하나 | Codex(GPT 계열)를 두 번째 판정자로. 둘 다 순서 일관 + 서로 일치일 때만 "통과" [추정] |
| 같은 노출·크롭 [확인 2505.15249] | 컷마다 밝기 다름 | 비교 전 크기·크롭 고정, 밝기 차가 크면 경고 표시 [추정] |
| 고정 뷰 세트 [확인 2606.18451] | B 컷 1장 + 근접 1 | 고정 카메라 4~6개(이름 붙은 빈 오브젝트) 묶음으로 판정 [추정] |
| 판정자 바꾸면 재검증 [확인 2610.01243] | 앵커 21장 검증 1회 | 모델 버전 바뀔 때마다 앵커 재시험 [추정] |

---

## 4. 일관된 스타일 — 트림 시트·타일·데칼 (사람 환경 아티스트 방식)

- 한 벽 단위 = **4방향 타일 텍스처 1장 + 트림 텍스처 1장**. 세트 대부분을 "타일 벽 / 트림 / 바닥" 3종으로 만들었다. 트림이 다양성과 **일관성**을 같이 준다. 아틀라스는 트림에만 썼다(나중에 UV 바꾸기 어려워서). [확인] https://www.thiagoklafke.com/tutorials/modular-environments/
- 트림 시트 = 한 축으로만 타일되는 띠 묶음. 텍스처를 늘리지 않고 복잡함을 더한다. 반복이 눈에 띄면 **데칼 덧붙이기**로 숨긴다. 순서: 아트 스타일 정하기 → 타일·트림·모듈 키트·소품을 그 스타일로 함께 설계. [확인] https://create.roblox.com/docs/tutorials/curriculums/environmental-art/develop-polished-assets
- Level Design Book: "로우폴리 스타일은 디테일·구조를 **벽 텍스처에 직접 칠하라** — 모델링·굽기가 필요 없다." 트림 시트는 "마법이 아니다, 오히려 쓰기 어렵다"는 경고도. 새 흐름 = 핫스팟 UV(Half-Life Alyx). [확인] https://book.leveldesignbook.com/process/env-art/texturing
- 우리에게: 지금은 에셋마다 bpy 가 텍스처를 새로 만든다 → 판마다 결이 다르다. **"공용 재료 3~4장(사진에서 만든 회벽 타일·트림 띠·바닥·지붕) + 데칼(녹·빗물·간판·포스터)"** 로 바꾸면 에이전트는 UV 를 띠에 맞추고 데칼을 놓는 일만 한다 — 에이전트가 강한 쪽(조립·배치). [추정]
- 핫스팟 UV 는 "면 크기에 맞는 칸으로 자동 UV" 라 bpy 로 짜기 쉽다(사각형 칸 목록 + 면 종횡비 매칭). [추정]

---

## 5. 사람의 몇 분을 가장 잘 쓰는 방식 (human-in-the-loop)

- **Design Galleries**(SIGGRAPH 1997): 매개변수를 슬라이더로 맞추게 하지 말고, 컴퓨터가 **서로 충분히 다른 변형들을 미리 뽑아 갤러리로 보여 주고 사람이 고르게** 한다. 지금도 "가장 잘 알려진 보조 도구"로 인용된다. [추정 — 검색 요약] https://dl.acm.org/doi/10.1145/258734.258887 · https://open.library.ubc.ca/media/download/pdf/24/1.0051462/1
- Paradox(Bloodlines 2) 콘셉트: 엔진 안에서 큰 형태 → 임시 빛·안개 → **카메라 몇 대를 세워 아트 디렉터가 고르게** → 렌더 위에 빠르게 페인트오버. 엔진 컷이라 다른 각도를 다시 안 그려도 된다. [확인] https://www.paradoxinteractive.com/games/vampire-the-masquerade-bloodlines-2/news/concept-art-in-unreal
- 혼합 주도(mixed-initiative) 레벨 도구 3중 맹검 연구: 사람이 만든 첫 맵의 패턴을 읽어 제안하는 도구가 무작위 제안보다 몰입·아이디어를 높였다. 사람은 "디자인만" 하고 도구가 제안. [확인] https://arxiv.org/pdf/2005.07478v2
- 업계 콘셉트 흐름: 레퍼런스 보드 → 썸네일 → … → **3D 블록아웃 위 페인트오버**가 표준 단계. [추정 — 검색 요약] https://game-ace.com/blog/video-game-concept-art/
- 우리에게: 페인트오버(D-183)는 이미 최선. 빈 곳은 **"고르기"** — 도현에게 "어때?"가 아니라 같은 `Cam_PO_*` 카메라에서 **한 축만 다른 4판**(예: 처마 띠 두께, 사진 결 세기, 데칼 밀도)을 한 장에 번호로. 답은 "3" 한 글자. [추정]
- 축은 한 번에 하나. 두 축을 섞으면 무엇이 좋은지 모른다. [추정]

---

## 6. 맵·배치 — 에이전트가 강한 쪽

- SIGGRAPH Asia 2025: LLM 이 **물체를 하나씩, 앞서 놓은 물체 기준 함수로** 놓는 명령형 배치 + "원래 배치에서 최소한만 움직이는" 오류 수정 → 지각 실험에서 제약 선언형 방식 대비 82%·94% 선호. 사람 선호와 맞는 간단한 자동 지표도 제안. [확인] https://arxiv.org/abs/2510.16147
- Meta WorldGen(2026): 텍스트 → 레이아웃 계획 → **블록아웃 + 참조 이미지 + 내비메시** → 메시. 블록아웃·내비메시를 먼저 고정하는 단계 구조. 생성 메시 부분은 우리 규칙상 금지, 단계 순서만 참고. [확인] https://ar5iv.labs.arxiv.org/html/2511.16825
- 2026 개인 개발자 사례: Claude 가 짠 계획이 "CC0 키트(KayKit·Quaternius·Kenney)로 시작 → 빈 곳만 Blender 스크립트 키트배싱". 같은 계획에 이미지 생성 모델도 넣었다 — 우리는 그 부분 금지. [확인] https://forum.quartertothree.com/t/vibe-coding-a-game/168570/86
- Claude Code 용 Blender 스킬(2026-09): 참조 이미지 → 에셋을 **단계 게이트**로, 각 단계는 "렌더되고 측정된 증거"로만 통과. [확인] https://github.com/majidmanzarpour/blender-game-skills
- Anthropic 공식 Blender 커넥터(2026-04): Python API 자연어 인터페이스·문서 탐색 용도. [확인] https://www.anthropic.com/news/claude-for-creative-work
- 우리에게: 소품·데칼 배치는 **"벽 밑·길가·문 옆" 같은 기준물 상대 규칙**으로 하나씩 놓고, 끝에 겹침·길 막힘만 최소 이동으로 고치는 스크립트. 무작위 흩뿌림(공터 소품) 문제의 직접 처방. [추정]

---

## 7. 최종 — 개선 7개 (기대 효과 ÷ 품 순)

| # | 무엇을 바꾸나 (한 줄) | 효과 | 품 | 근거 |
|---|---|---|---|---|
| 1 | 판정 전 두 컷 **노출·크롭·크기 맞추기** + 고정 카메라 4~6컷 묶음 + 두 번째 판정자(Codex) 일치 시에만 통과 | 중 | 小 | 2505.15249 · 2606.18451 · 2610.01243 [확인] |
| 2 | 사진 부품 처리에 **`delight.py` 고정 단계**(저주파 나누기 + 평균색 + Physically Based 재료 명도 순서) → 그다음 팔레트 | 중~大 | 小 | mango.blender.org · shadermap · physicallybased.info [확인] |
| 3 | 도현 요청을 "어때?" 대신 **같은 카메라·한 축 4판 번호 고르기**(페인트오버와 번갈아) | 大 | 小 | Design Galleries [추정] · Paradox [확인] · 2005.07478 [확인] |
| 4 | 건물 텍스처를 에셋별 bpy 생성 → **공용 재료 3~4장(타일 벽·트림 띠·바닥·지붕, 도현이 고른 사진에서) + 데칼**로 | 大 | 中 | thiagoklafke · Roblox · Level Design Book [확인] |
| 5 | bpy 라이브러리에 **스타일 잠금**: 재질은 팔레트 번호만, 텍셀 32px/m·삼각형·UV·팔레트 밖 색 자동 검사, 게임 카메라 렌더 보기 전 완료 금지 | 중 | 中 | blender-asset-mcp · blender-iso-mcp [확인] |
| 6 | 소품·데칼 배치를 **기준물 상대 명령형 + 최소 이동 오류 수정** 스크립트로 | 중 | 中 | arXiv 2510.16147 [확인] |
| 7 | 도현이 고른 사진에서 **Embark texture-synthesis(비신경망)** 로 변형을 떠 6채 반복 금지(같은 부품 ≤2) 충족 | 小~중 | 小 | Embark README [확인] |

---

## 8. 못 본 것 / 한계
- GDC 2026 보고서 PDF 원문은 열지 않았다(기사로 확인). 36% 수치는 [추정].
- Design Galleries 원문은 열지 않았다(인용 페이지만).
- 트림 시트 실제 효과를 우리 720 Nearest 화면에서 잰 적 없다 — 4번은 기준 장면 한 블록으로 먼저 A/B 해야 한다.
- texture-synthesis 는 2023년 보관됨 — Windows 바이너리 동작은 확인 안 함.
- X·Reddit 개인 사례는 이번엔 거의 안 봤다(10-27 community 문서가 다룸).
