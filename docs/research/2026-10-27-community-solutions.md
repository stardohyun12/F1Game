# 저해상도 로우폴리를 "싸구려·장난감" 아니게 + AI 에이전트로 3D 아트 품질 올리기 — 커뮤니티·오픈소스 조사 (2026-10-27)

조사: 하위 에이전트(리서치). 커밋 안 함.
표시: **확인** = 원문을 직접 열어 읽음 / **스니펫** = 검색 결과 요약만 봄 / **[추정]** = 내 해석.
앞 문서와 관계: `2026-10-27-quality-gap.md`(PD 진단)의 원인 1·3·5를 바깥 증거로 받쳐 준다. 겹치는 처방은 다시 쓰지 않고 "그래서 무엇을 바꾸나"만 적는다.

---

## 0. 한 줄 결론

- 기준 게임 작가들은 **디테일을 모델이 아니라 텍스처·카메라 셰이더·안개 색에 넣고, 최종 화면 필터를 켠 채로 모델링한다.**
- 우리 bpy 파이프라인은 반대다 [추정]: 형태를 좌표로 정밀하게 쌓고, 표면은 단색·깨끗하고, 판정은 게임 밖(블렌더 렌더)에서 한다 → "레고 디오라마".
- AI↔Blender(MCP) 사례는 공통으로 **"배치·반복·셋업은 빠르다 / 미감 판단·손맛은 못 한다"**. 품질을 올린 연구는 모두 **시각 검증자(verifier) 루프**를 붙였다.

---

## 1. 채널별 발견 표

### 1-1. 개발자 인터뷰·기사 (기준 게임 본인 발언)

| 출처 | 상태 | 핵심 한 줄 | 우리에게 |
|---|---|---|---|
| [80.lv — Inside the Making of Easy Delivery Co.](https://80.lv/articles/inside-the-making-of-easy-delivery-co) | 확인 | 지오메트리 최소, 디테일은 텍스처. 조작 대상(트럭·상자·사람)=귀여운 픽셀 텍스처, 환경=사진 photobash. 그림자·해 없음, 주변광 한 색=안개 색, 음영은 텍스처에 칠하고 SSAO 살짝. "가상의 옛 엔진 제약"을 스스로 정함. 도구 Blockbench+Photoshop→Aseprite. | **매우 큼.** 층 나누기(차·화물 vs 세계)는 이미 우리 규칙과 같다. 빠진 건 "음영을 텍스처에 칠함"과 "평평한 빛=안개 색". |
| [Kakuchopurei — Crow Country 인터뷰](https://www.kakuchopurei.com/2024/06/crow-country-exclusive-interview-adam-vian-indie-sfb/) | 확인 | "모든 것을 이 카메라 셰이더로 보면서 모델링·텍스처링했다 — 최종 모습을 늘 알았다." | **매우 큼.** 우리는 블렌더 렌더로 판정해 왔다(quality-gap 원인 5). |
| [r/CrowCountry — Crow Country Artstyle](https://www.reddit.com/r/CrowCountry/comments/1cijdch/crow_country_artstyle/) (Adam Vian 댓글) | 확인 | Unity ProBuilder 로 직접 모델링, 모서리를 버텍스 스무딩으로 부드럽게, 전부 "꽤 복잡한" 카메라 커스텀 셰이더를 거침. 댓글: 실제 폴리곤은 PS1보다 꽤 많음. | 큼. 각진 박스 대신 "모서리 뭉개기"가 장난감 느낌을 줄인다 [추정]. |
| [GamesRadar — 인디가 말하는 로우폴리](https://www.gamesradar.com/games/survival-horror/indie-devs-discuss-why-low-poly-works-so-well-for-horror-i-actually-think-those-limitations-encourage-weird-unique-compromises/) | 확인 | Crow Country 환경은 진짜 PS1보다 훨씬 디테일하지만, 셰이더 덕에 프리렌더 배경처럼 보임. "제약이 이상하고 독특한 타협을 낳는다." | 중. "적은 폴리곤"이 목표가 아니라 "한 겹 필터 뒤의 밀도"가 목표. |
| [X — @adamgryu (A Short Hike) 2019-04-02](https://x.com/adamgryu/status/1113101222570024960) | 확인(fxtwitter API로 본문) | 픽셀 느낌 = 월드를 저해상도 RenderTexture 로 렌더. GBCamera for Unity 에서 배움. | 작음 — 이미 1280×720 Nearest 로 하고 있음. |
| [Game Developer — Wheel World 오픈월드](https://www.gamedeveloper.com/design/cycling-through-the-open-world-design-of-wheel-world) | 확인 | 시각 영감 = 옛 투르 드 프랑스·여행 포스터, 굵은 손그림 윤곽·대담한 형태, 뫼비우스, 사전트, 호크니. | 중. "색·소박함" 기준(⑤)의 출처가 포스터 그래픽이란 점. |
| [Creative Bloq — Wheel World 와 Houdini](https://www.creativebloq.com/3d/video-game-design/how-wheel-worlds-massive-rolling-landscapes-were-made-possible-with-houdini) | 확인 | 아트 디렉터가 울타리 같은 반복물을 "길이·색만 넣는 도구"로 만들어 디자이너에게 줌. 실외기·위성안테나 같은 소품은 손으로. | **큼.** 절차적은 반복물·배치만, 고유 형태는 손 — D-162 와 같은 결론, 업계 사례. |
| [Hyperlific — Adam Vian Doesn't Need More Polygons](https://www.hyperlific.com/articles/adam-vian-doesnt-need-more-polygons/) | 확인 | "손맛은 폴리곤 수가 아니라 선, 팔을 너무 가늘게·턱을 너무 날카롭게 하는 *결정*에 있다." 3D를 2D 그림처럼 칠하고 흔들리는 윤곽 셰이더를 씀. | 중. 과장 결정이 없으면 로우폴리여도 무난·무개성. |
| [Wikipedia — Easy Delivery Co.](https://en.wikipedia.org/wiki/Easy_Delivery_Co.) / [YouTube Devlog 2](https://www.youtube.com/watch?v=iK6v53sX-h8) | 스니펫 | 1인 개발, 데브로그 2편이 아트 스타일·스코프. | 영상 미시청. |

### 1-2. 커뮤니티 (Reddit / Polycount / itch / YouTube)

| 출처 | 상태 | 핵심 한 줄 | 우리에게 |
|---|---|---|---|
| [r/ps1graphics — PSX 텍스처 어떻게 만드나](https://www.reddit.com/r/ps1graphics/comments/1g1gsjw/how_do_you_guys_makeget_psx_style_textures/) | 확인 | ① Blender 로 큰 색·명암·스텐실 → Aseprite 로 픽셀 정리. ② 비유기물은 사진 편집. ③ 1024 아틀라스 1장에 64×64 텍스처 32개, UV를 아틀라스 칸에 끌어다 씀. ④ 무료 도구로 8/16비트 + **팔레트 맞춤**. ⑤ 머티리얼 인스턴스에 색을 곱해 재활용(석고→콘크리트→차고문). ⑥ 노이즈 층 + 때·얼룩을 덧칠 → 축소하면 "디테일 같은 픽셀 변화". | **매우 큼.** 에이전트가 그대로 따라 할 수 있는 절차. |
| [r/gamedev — 로우폴리 어떻게 좋아 보이게](https://www.reddit.com/r/gamedev/comments/jhbm0h/how_do_i_make_low_poly_look_good_in_my_game/) | 확인 | 평면 UV + 노이즈 섞은 색, **AO 굽기("자기 그림자가 큰 차이")**, 강한 방향광 하나 문제, "형태 언어를 분해해 비례·디테일 수준을 밀어붙여야". | 큼. |
| [Polycount — Learning handpainted textures](https://polycount.com/discussion/74367/learning-handpainted-textures) | 확인 | 노멀맵·스펙 없는 게임이라 **광원을 정하고 깊이를 텍스처에 칠해야** 함. | 큼 — EDC 와 같은 말. |
| [Polycount — Hand Painted Props](https://polycount.com/discussion/91502/hand-painted-props-advice-needed) | 스니펫 | 텍스처 대비를 더, 재질마다 재질답게(금속 딱딱한 하이라이트, 나무는 약하게). | 중. |
| [Polycount — 로우폴리 비밀](https://polycount.com/discussion/55236/what-is-the-secret-to-low-poly-models-looking-good) | 스니펫(본문 열기 실패) | 아주 낮은 폴리는 다른 팔레트 + 1px 포인트 브러시, 선·디테일 대비. | 중. |
| [r/low_poly — 예쁜데 못생겼다는 말 들음](https://www.reddit.com/r/low_poly/comments/1enwkb7/i_think_my_low_poly_game_looks_pretty_sweet_but/) | 스니펫 | "로우폴리에 사실적 빛·그림자는 별로 — 스타일화해야", 조명을 버텍스 컬러로. | 큼 — 우리 URP 기본 빛 [추정]. |
| [r/low_poly — 내 플랫폼 게임 별로?](https://www.reddit.com/r/low_poly/comments/1q6lxdx/my_little_lowpoly_3d_platform_game_does_it_look/) | 스니펫 | 그림자·AO 없음 + **저대비 파스텔 = 납작해 보임**. | 큼 — "모바일 캐주얼" 증상과 같음 [추정]. |
| [r/gamedev — Does low-poly look bad?](https://www.reddit.com/r/gamedev/comments/oguns7/does_lowpoly_design_look_bad/) | 스니펫 | 조명 먼저, 금속성 머티리얼 빼라. | 중. |
| [r/blender — PS1 The Thing 환경](https://www.reddit.com/r/blender/comments/1tcclhe/playstation_1_style_environment_inspired_by_the/) | 스니펫 | 버텍스 라이팅은 Itera Tools 3(지오노드), 레벨 편집은 Anvil 애드온. | 작음(유료/확인 안 함). |
| [r/blenderhelp — 그냥 저화질 말고 PS1 텍스처](https://www.reddit.com/r/blenderhelp/comments/16zmz02/how_can_i_achieve_ps1_style_textures_instead_of/) | 스니펫 | 보간 Closest. "저화질 사진 ≠ PS1 텍스처" — 색 수·선택이 다름. | 중. |
| [r/godot — PS1 팁](https://www.reddit.com/r/godot/comments/1jitleh/any_tips_tricks_to_nailing_that_ps1_style/) / [r/lowpoly — 첫 PS1 환경](https://www.reddit.com/r/lowpoly/comments/1lwxktd/this_is_my_first_time_making_a_ps1_style/) | 스니펫 | 안개 듬뿍, 해·달은 모델 대신 텍스처. | 중. |
| [r/Unity3D — A Short Hike 재현](https://www.reddit.com/r/Unity3D/comments/cpbbsn/anyone_have_ideas_of_how_to_recreate_the_art/) / [r/IndieDev](https://www.reddit.com/r/IndieDev/comments/fmscd8/how_can_you_make_graphics_like_a_short_hike_with/) | 스니펫 | 작은 RT → 쿼드, AA 끔. | 이미 적용. |
| [davidhol.land — 3D Pixel Art Rendering](https://www.davidhol.land/articles/3d-pixel-art-rendering/) | 확인 | t3ssel8r 계열. "3D 효과를 2D처럼 보이게가 일의 대부분", 비·물보라 등은 Pixel Composer 로 만든 2D 루프 텍스처. | 중 — 파티클을 2D 텍스처로. |
| [r/low_poly — Blockbench 인디 게임](https://www.reddit.com/r/low_poly/comments/1ogkiv5/indie_game_made_with_blockbench_models_some/) | 스니펫 | Blockbench 에서 모델·칠·애니, 디테일은 Aseprite, FBX→Unity. | 중 — EDC 와 같은 도구열. |
| [itch — PSX Style Game Assets devlog](https://toastycatstudios.itch.io/ps1-style-game-assets/devlog/392396/psx-style-game-assets) | 스니펫 | 사람이 만든 PSX 팩(나무·벽·표지판). | 처방 C 후보 시장. |

### 1-3. X(트위터)·인스타그램

| 출처 | 상태 | 핵심 한 줄 | 우리에게 |
|---|---|---|---|
| [X — @adamgryu](https://x.com/adamgryu/status/1113101222570024960) | 확인 | 위 1-1 참조. | — |
| [X — @TheMirzaBeig PS1 URP 셰이더 스레드](https://x.com/TheMirzaBeig/status/1817655127513407961) | 확인(본문만, 노드 그래프 이미지 못 봄) | 지글거림=정점 정수 스냅, 와블=어파인 매핑의 원리 설명 + 셰이더그래프. | 작음 — D-149 로 정점 스냅은 끔. |
| [X — @blockbench 로우폴리+픽셀 텍스처 튜토리얼](https://x.com/blockbench/status/1645102527346556928) | 스니펫 | 입문 튜토리얼. | 작음. |
| [X — @PS1Aesthetics](https://x.com/PS1Aesthetics) | 스니펫 | PS1·유사 인디 스샷 큐레이션 계정. | 레퍼런스 보드용(우리 기준 화면은 고정이라 참고만). |
| [X — growthsuck "PS1 Arbitrage"](https://x.com/growthsuck/status/2073933382065528905) | 스니펫 | AI 생성으로 PS1 스타일 광고 찍어내기. | **반면교사** — "PS1 = 싸게 찍어내는 포맷"으로 소비되는 중 → 필터만으론 차별 없음 [추정]. |
| [Instagram reel — PS1 Style Tree Tutorial](https://www.instagram.com/reel/DTvu6NQkcAf/) | 확인(댓글만, 영상 못 봄) | 댓글 질문: "조명은 전부 emission + 그림자 손으로 굽나, 아니면 실제 라이팅?" — 커뮤니티에서 굽기 방식이 흔함을 시사. | 작음. |
| [Instagram — vaporwave_1980](https://www.instagram.com/vaporwave_1980/) / [s1lax.art reel](https://www.instagram.com/reel/DWG9JHIjAvv/) | 스니펫 | AI 생성 이미지→"PS1 영상" 계정들. | **투입 금지 대상(AI 생성).** 참고 안 함. |

**X·인스타 한계:** 로그인 벽으로 스레드 본문·캐러셀 설명을 거의 못 열었다. 열린 것은 fxtwitter API 로 단일 트윗 본문 2개, 인스타 릴 댓글 1개뿐. 인스타 "로우폴리 PS1 아티스트 캐러셀 설명"은 **확보 실패**.

### 1-4. AI↔Blender 사례 (4절에서 정리)

| 출처 | 상태 | 핵심 한 줄 |
|---|---|---|
| [ahujasid/mcp-for-blender](https://github.com/ahujasid/mcp-for-blender) | 메타 확인(gh api) | MIT, ★32k, 2026-10-24 push. 스크린샷·씬 조회·bpy 실행. Hyper3D/Hunyuan3D 생성, Sketchfab·Poly Pizza 다운로드 도구 포함([PyPI](https://pypi.org/project/mcp-for-blender/1.9.1/)). |
| [Vagon — Blender MCP 가이드](https://vagon.io/blog/how-to-use-blender-mcp-with-anthropic-claude-ai) | 확인 | "첫 한계는 창작 판단 — '프리미엄' 재질이 평범해지고 '사실적' 방이 비어 보임." 큰 원샷 금지, **블록아웃→재질→조명→디테일 패스**. 깨끗한 토폴로지·게임 에셋을 마법처럼 못 만듦. |
| [r/blender — Claude+Blender MCP 실사용 테스트](https://www.reddit.com/r/blender/comments/1syv8w2/tested_claude_blende_mcp_heres_what_actually/) | 확인 | 셋업은 빠름, 스캔 정리는 수치만 줄이고 품질은 안 됨. 댓글: "인상적인 결과는 대개 큐브 배열", "AI 는 스케치까지", 결정적인 유료 애드온을 더 믿는다. MCP 제작자가 직접 피드백 요청. |
| [r/LocalLLaMA — 좋은 결과 얻는 사람?](https://www.reddit.com/r/LocalLLaMA/comments/1k2ilye/blender_mcp_can_anyone_actually_get_good_results/) | 확인 | "LLM 은 3D 공간 감각이 형편없다", 성공작은 "같은 크기 큐브만 쓰라"고 제약 건 배. |
| [r/ClaudeCode — Claude→Blender 에셋에 감탄](https://www.reddit.com/r/ClaudeCode/comments/1xc130o/i_think_im_most_impressed_by_the_claude_to/) | 확인(일부) | 댓글: "MCP 대신 API 스크립트가 더 빠르고 쌌다." |
| [r/ClaudeCode — Blender MCP 에셋 팁?](https://www.reddit.com/r/ClaudeCode/comments/1xd2pam/what_are_your_tips_for_creating_assets_via/) / [r/ClaudeAI — 정직한 결과](https://www.reddit.com/r/ClaudeAI/comments/1syxt40/i_tested_claude_blender_mcp_for_real_3d_workflows/) / [r/ClaudeAI — GTA6 Day 23](https://www.reddit.com/r/ClaudeAI/comments/1u90nmg/day_23_of_building_gta_6_using_claude/) | 스니펫 | 특정 스타일 캐릭터가 매력적으로 안 나옴 / "성공 한 번 앞의 수많은 실패는 말 안 함" / "AI 는 아직 능숙한 3D 아티스트 아님". |
| [dev.to — Blender MCP 토큰 실측 72회](https://dev.to/jaehyun_cho_0dff271e0d2e5/how-many-tokens-does-blender-mcp-really-use-72-claude-code-runs-and-a-racing-scene-for-047-5hl3) | 확인 | Opus 5.5 로 로우폴리 레이싱카+코너+추격 카메라 = 약 10요청·93초·$0.47, 10회 모두 작동. 스크린샷 기본 768px. 비용 대부분은 Claude Code 자체 지시문. (품질 판정은 없음) |
| [arXiv LL3M](https://arxiv.org/abs/2508.08228) | 확인(초록) | 여러 LLM 에이전트가 계획·검색·작성·디버그·**코드와 시각으로 자기비평** → Blender 스크립트. Blender API RAG. |
| [arXiv BlenderAlchemy](http://arxiv.org/abs/2404.17672) | 확인(초록) | VLM 편집 생성기 + **상태 평가자**로 행동열 탐색. "단순하지만 지루한" 편집(재질·조명)에 효과. 참조 이미지로 접지(그들은 생성 이미지 — 우리는 금지, 실제 기준 스샷으로 대체). |
| [arXiv BlenderGym](https://arxiv.org/abs/2504.01786) | 확인(초록) | 최신 VLM 도 사람에게 쉬운 편집에서 고전. **검증자에 추론을 더 쓰면 개선**, 생성과 검증 사이 연산 배분이 중요. |
| [arjun988/blender-skills](https://github.com/arjun988/blender-skills) | 메타 확인 | MIT, ★915, 94개 Blender 스킬(Claude Code/Codex용), `lowpoly-style`·`ps1-aesthetic` 참조 포함. 본문 미열람. |
| [ceeyang/blender_mcp](https://github.com/ceeyang/blender_mcp) | 메타 확인 | MIT, Blender 5.2, 159개 타입드 도구(UV 포함). ★4 — 검증 안 됨. |

---

## 2. "로우폴리/PS1 이 싸구려·장난감처럼 보이는 이유"와 피하는 법 (작업자 발언 모음)

| # | 원인 (작업자 말) | 피하는 법 | 출처 | 우리 해당? |
|---|---|---|---|---|
| 1 | 최종 화면 필터 없이 만든다 | 최종 카메라 셰이더를 켠 채 모델링·텍스처링 | Crow Country 인터뷰 | **예** — 블렌더 렌더 판정(quality-gap 원인 5) |
| 2 | 디테일을 형태로만 넣고 표면이 깨끗함 | 지오메트리 최소, 디테일·음영은 텍스처에 | EDC 80.lv, Polycount | **예** [추정] — bpy 박스 + 평면색 |
| 3 | 텍스처에 명암·광원이 없음 | 광원 방향 정하고 깊이를 칠해 넣기, 대비 올리기 | Polycount 74367·91502 | 예 [추정] |
| 4 | 노이즈·때 없는 평면 색 | 노이즈 층 + 때·얼룩 덧칠 → 축소 시 "디테일 같은 픽셀 변화" | r/ps1graphics, r/gamedev | 예 |
| 5 | 그림자·AO 없음 + 저대비 파스텔 = 납작 | AO 굽기, 대비 확보 | r/gamedev, r/low_poly | **예** — "모바일 캐주얼" 증상과 동일 [추정] |
| 6 | 사실적 실시간 빛(강한 방향광 하나)을 로우폴리에 | 스타일화: 평평한 주변광=안개색 / 버텍스컬러 조명 굽기 | EDC, r/low_poly, r/gamedev | 확인 필요(Unity 현재 Look 값) |
| 7 | 오브젝트마다 제각각 색 | 공용 아틀라스 1장 + 팔레트 맞춤 + 틴트로 재활용 | r/ps1graphics | 부분 — 32px/m 규칙은 있으나 아틀라스 공유는 [추정] 없음 |
| 8 | 각진 박스 모서리 그대로 | 버텍스 스무딩으로 모서리 부드럽게(폴리 조금 더 써도 됨) | Crow Country 댓글 | 예 [추정] |
| 9 | 형태 과장 결정이 없음 | 형태 언어 분해, 비례·디테일 수준을 "밀어붙이기" | r/gamedev, Hyperlific | 예 — SHAPE.md 진행 중. 단 v5 처럼 과장 = 채도·둥글기로 가면 장난감 [추정] |
| 10 | PS1 필터 = 이제 흔한 찍어내기 포맷 | 필터가 아니라 "가상의 옛 엔진 제약"을 일관되게 | EDC, X growthsuck(반면교사) | 참고 |
| 11 | 모든 걸 절차적으로 | 반복물(울타리)만 도구, 고유 소품은 손 | Wheel World | 예 — D-162 와 일치 |

---

## 3. 바로 쓸 수 있는 오픈소스 도구·셰이더

메타는 2026-10-27 `gh api` 로 확인(라이선스·마지막 push·★). "Unity 6 URP"는 README 기준, 실제 임포트는 안 해 봄.

### 3-1. Unity URP 셰이더·포스트

| 도구 | 라이선스 | 마지막 push | Unity 6 URP | 쓸 부분 |
|---|---|---|---|---|
| [Colby-O2/VNTG](https://github.com/Colby-O2/VNTG) | Unlicense | 2026-09-02 (★22) | **명시 지원**(6000.x+, 지형은 6.3+) | **텍셀 단위 조명 스냅(per-texel lighting)** — 빛·그림자가 텍스처 픽셀 격자에 맞춰 계단짐 → "손으로 칠한 음영"처럼 보임 [추정]. PSX·CRT 렌더 피처는 끔(D-149). 최우선 시험 후보. |
| [AlexeyNazariev/PS1-Graphics-Kit-URP](https://github.com/AlexeyNazariev/PS1-Graphics-Kit-URP) | MIT | 2026-05-15 (★3) | 2021.3+ 표기, Full Screen Pass 사용 | 레이캐스트 방울 그림자(데칼/쿼드) — 차 그림자용. 검증 적음. |
| [NullTale/DitherFx](https://github.com/NullTale/DitherFx) | NOASSERTION(라이선스 파일 확인 필요) | 2025-06-09 (★77) | URP 볼륨 | 팔레트·패턴 디더. 하늘·안개 그라데이션 띠 정리용. 라이선스 확인 전 투입 금지. |
| [yasib48/URP-Pixel-Outline-Shader](https://github.com/yasib48/URP-Pixel-Outline-Shader) | MIT | 2026-01-20 (★0) | URP 렌더 피처 | 깊이·노멀 외곽선 + 색 양자화. 윤곽은 기준 화면에 없음 → 양자화만 참고. |
| [Dasfaust/PixelEngine](https://github.com/Dasfaust/PixelEngine) | MIT | 2026-10-25 (★1) | Unity 패키지 | t3ssel8r식 3D 픽셀아트. 초기 단계. |
| [Math-Man/URP-PSX-FORKED](https://github.com/Math-Man/URP-PSX-FORKED) | MIT | 2024-01-28 (★311) | URP 14(2022.3) — Unity 6 는 포팅 필요 [추정] | 원조 Kodrin 의 업데이트판. 참고용. |
| [Kodrin/URP-PSX](https://github.com/Kodrin/URP-PSX) | MIT | 2024-05-27 (★922) | Unity 2019.3 기준 — 그대로 안 됨 [추정] | VNTG 가 이 조명 로직을 가져다 씀. |
| [dsoft20/psx_retroshader](https://github.com/dsoft20/psx_retroshader) | MIT | 2022-04-09 (★1237) | Built-in 용 [추정] | 원리 참고만. |
| [whateep/unity-simple-URP-pixelation](https://github.com/whateep/unity-simple-URP-pixelation) | **없음** | 2022-07-29 | — | 라이선스 없음 → 사용 불가. 이미 자체 저해상도 있음. |

### 3-2. 텍스처·모델 도구 (에이전트가 스크립트로 부를 수 있는 것 위주)

| 도구 | 라이선스 | 마지막 push | 쓸 부분 |
|---|---|---|---|
| [sedthh/pyxelate](https://github.com/sedthh/pyxelate) | MIT | 2026-01-18 (★1704) | 이미지→픽셀아트, **팔레트 전이·디더**. CC0 사진/스크립트 텍스처를 공용 팔레트로 낮추는 단계에 파이썬으로 바로. |
| [ucupumar/ucupaint](https://github.com/ucupumar/ucupaint) | GPL-3.0 (애드온 — 게임에 코드 안 들어가면 무관 [추정]) | 2026-10-26 (★2279) | Blender 텍스처 레이어 스택(AO·곡률 마스크, 노이즈, 때 층) → 굽기. "노이즈+때+AO" 층을 bpy 로 쌓기 좋음. |
| [RodZill4/material-maker](https://github.com/RodZill4/material-maker) | MIT | 2026-10-25 (★6025) | 노드 기반 절차 텍스처(벽돌·석회·나무결) → 소스 이미지. 사진 대신 쓰는 재료. |
| [JannisX11/blockbench](https://github.com/JannisX11/blockbench) | GPL-3.0 | 2026-10-26 (★6107) | EDC 의 실제 도구. 로우폴리+픽셀 텍스처를 한 화면에서. 플러그인 JS 로 자동화 가능 [추정]. |
| [aseprite/aseprite](https://github.com/aseprite/aseprite) | 소스 공개(EULA, 바이너리 유료) | 2026-10-22 | EDC·r/ps1graphics 공통 마무리 도구. Lua 스크립트·CLI 로 팔레트 적용 배치 가능. |
| [arjun988/blender-skills](https://github.com/arjun988/blender-skills) | MIT | 2026-07-10 (★915) | Claude/Codex 용 Blender 스킬 94개. 우리 art 역할 프롬프트에 `lowpoly-style` 참조를 비교해 볼 만함(본문 미열람). |
| [ahujasid/mcp-for-blender](https://github.com/ahujasid/mcp-for-blender) | MIT | 2026-10-24 (★32k) | 열린 Blender 스크린샷 루프. **Hyper3D/Hunyuan3D 생성 도구는 끌 것(규칙 위반)**. |
| [JulienVanelian/lowpolyzer](https://github.com/JulienVanelian/lowpolyzer) | GPL-3.0 | 2021-07-08 | 메시를 로우폴리화. 낡음, 비추천. |

---

## 4. AI 에이전트 ↔ Blender 로 품질을 올린 사례와 한계

**된 것 (출처 있는 것만)**
- 셋업·블록아웃·조명·재질 테스트·반복 정리: 빠르다 (Vagon, r/blender 테스트).
- 간단한 로우폴리 차+트랙+카메라: Opus 5.5 로 $0.47·93초, 10/10 작동 (dev.to). 단 "작동"이지 "좋다"는 판정 없음.
- 연구: LL3M(다중 에이전트 + 코드·시각 자기비평), BlenderAlchemy(편집 생성기 + 평가자), BlenderGym(검증자에 연산을 더 주면 개선). → **공통: 생성과 따로 도는 시각 검증 루프가 있어야 오른다.**
- 실무 댓글: MCP 보다 API 스크립트(우리 방식)가 빠르고 쌈 (r/ClaudeCode).

**한계 (반복적으로 나옴)**
- 창작 판단·취향: "프리미엄 재질이 평범, 사실적 방이 텅 빔" (Vagon). 매력적인 캐릭터 안 나옴 (r/ClaudeCode 스니펫).
- 공간 감각 약함 — 제약을 극단적으로 걸 때(같은 큐브만) 오히려 잘 됨 (r/LocalLLaMA).
- 원샷 큰 장면은 지저분 → 패스 분리 (Vagon).
- 사람에게 쉬운 편집도 최신 VLM 이 고전 (BlenderGym).
- 깨끗한 게임 토폴로지·손맛은 못 줌; "성공 한 번 앞의 많은 실패" (Vagon, r/ClaudeAI 스니펫).

**우리 상황 해석 [추정]**
- 5~6점 정체는 모델 능력 + **검증자가 같은 AI·같은 렌더(블렌더)** 라는 구조 문제가 겹친 것. 연구 사례는 "검증 쪽 강화"로 올렸다.
- r/LocalLLaMA 의 "제약이 셀수록 잘 됨" = EDC 의 "가상의 옛 엔진 제약"과 같은 방향. 에이전트에게 자유 대신 **좁은 부품 문법**(아틀라스 칸·고정 팔레트·정해진 모서리 스무딩)을 주는 게 맞다.
- 손맛은 결국 "결정"(Hyperlific). 에이전트는 결정을 못 하니, **결정은 기준 화면 측정치·도현 판정으로 외부에서 주고, 에이전트는 실행·반복만** 하게 해야 한다.

---

## 5. 결론 — 당장 해 볼 해결책 5개

| 순위 | 해결책 | 근거 | 노력 | 예상 효과 |
|---|---|---|---|---|
| 1 | **"게임 눈으로 만들기"** — 아트팀 판정 렌더를 Unity 실제 화면(1280×720 Nearest·안개·Look)으로만. Blender 에 같은 조건 미리보기(저해상 렌더→Nearest 4배, 안개=배경 한 색, 팔레트 양자화)를 스크립트로 만들어 **모든 중간 판정에 강제**. | Crow Country "셰이더로 보며 모델링", quality-gap 원인 5 | 小 (반나절, bpy 컴포지터 + pyxelate) | 中~大. 장난감 느낌의 상당 부분(깨끗한 면·쨍한 색)이 최종 필터에서 다르게 보임을 일찍 앎 [추정] |
| 2 | **디테일을 텍스처로 옮기기** — 모델은 더 단순하게, 표면에 ①광원 방향 명암 ②AO·곡률 굽기 ③노이즈 층 ④때·얼룩·물때 ⑤공용 팔레트 축소(pyxelate). bpy 로 "베이크 → 층 합성 → 팔레트" 파이프라인화. 사진은 EDC 처럼 환경에만, 큰 얼룩 위주(점무늬 금지 — "더럽다" 이력). | EDC, Polycount, r/ps1graphics, r/gamedev | 中 (1~2일 파이프 + 정비소 1채 재작업) | **大** — "평면색 박스"가 가장 직접적인 레고 원인 [추정] |
| 3 | **조명 스타일화** — 실시간 강한 방향광 의존을 줄이고: 평평한 주변광 = 안개 색(EDC) + 약한 SSAO, 혹은 VNTG 텍셀 단위 조명을 씬 전용 재질로 A/B. 공유 .mat 변경 금지. | EDC 80.lv, r/low_poly, VNTG README | 小~中 (토큰 1회, A/B 캡처) | 中 — 색 무리·공기감. 정비소 하나 안 고쳐도 화면 전체에 먹음 |
| 4 | **에이전트 루프 재설계** — ①패스 분리(블록아웃→표면→빛→소품), 패스당 판정 ②검증자는 별도 세션·다른 모델, 입력 = 기준 화면 같은 배율 크롭과 우리 크롭 나란히 + 측정치(면당 색 수·텍스처 블록 크기·대비) ③모서리 스무딩·아틀라스 칸·팔레트를 "좁은 문법"으로 고정. MCP 도입은 선택(스크린샷 루프용), 생성형 3D 도구는 끔. | LL3M·BlenderAlchemy·BlenderGym, Vagon, r/LocalLLaMA | 中 (역할 프롬프트·검사 스크립트 1일) | 中 — 6→7 은 가능, 손맛 상한은 그대로 [추정] |
| 5 | **사람 손 에셋을 바탕으로** — 스타일 맞는 사람 제작 PSX/로우폴리 팩(itch 등, AI 아님·라이선스 확인)을 사서 우리 아틀라스·팔레트로 재칠, 에이전트는 Wheel World 식 "반복물 도구"(울타리·전선·난간 길이/색 파라미터)와 배치만. | Wheel World(Houdini 도구 + 손 소품), quality-gap 처방 C, D-162 | 中 (팩 선별 반나절 + 재칠 파이프는 2번 재사용) | 大 — 손맛 상한 문제를 직접 우회. 단 팩 선택은 도현 취향 판정 필요 |

**추천 순서:** 1 → 2(정비소 1채로 전후 비교) → 3 은 2와 병렬(에디터 토큰) → 같은 장면 점수가 두 판 안 움직이면 5 를 도현에게. 4 는 2·3 과제 프롬프트에 바로 섞는다.

---

## 6. 빈 곳 (못 본 것)

- X 스레드 본문·인스타 캐러셀 설명: 로그인 벽, 단일 트윗 2개·릴 댓글 1개만 확인.
- Haunted PS1 디스코드·포럼: 공개 글 못 찾음.
- YouTube(EDC Devlog 2, A Short Hike 포스트모템, 버텍스 컬러 강연): 자막 미열람.
- 도구는 임포트·실행 안 함. Unity 6 호환은 README·버전 표기 기준.
- Reddit 여러 글은 첫 댓글만(로그인/차단으로 전체 스레드 못 봄).
