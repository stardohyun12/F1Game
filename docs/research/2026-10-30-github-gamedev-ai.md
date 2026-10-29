# GitHub `#gamedev #ai` + 길 생성 · 에이전트 도구 조사 (2026-10-30)

조사 범위: GitHub 검색 `#gamedev #ai`(별 순) **367개 전부**(웹 1~37쪽과 같은 질의를 REST 검색 API 100개×4쪽으로 받음 — 이름·별·언어·라이선스·마지막 push 전수 목록) + 목적 검색 16개(unity road generation / spline road / road terrain / procedural road network / road generator terrain / blender mcp / unity mcp screenshot / playtest agent / claude code game studio / RoadArchitect 등). 후보 53개는 **README 원문을 직접 받아 읽었고**, 길 관련 7개는 LICENSE 파일과 핵심 소스 파일(교차로·지형 맞춤·높이)을 직접 열었다. 활동일 = GitHub `pushed_at`. 아무것도 설치하지 않았다.

## 결론 5줄

1. **길 문제의 정답에 가장 가까운 공개 코드는 `Acid3croco/berat-racer`(MIT, Unity, 2026-10-06)** — 실제 지도 길을 LiDAR 지형 위에 까는 운전 게임으로, 우리가 RoadNetV2 에서 하려는 것(필렛·교차로 다각형·구배·깎기/메우기)을 이미 끝까지 해 두고 검사기까지 있다. 코드를 통째로 가져오지 말고 단계별 규칙·수치를 RoadNetV2 에 옮긴다.
2. 교차로 다각형은 berat(팔 정렬 → 연석이 안 겹칠 때까지 잘라냄 → 합집합, 짧은 간선은 삼킴)와 `SeloSlav/spline-based-procedural-dirt-road-system`(MIT, 별 모양 윤곽) 두 방식이 우리 "갈림목 겹침"을 직접 푼다. 리본 접힘은 berat 의 "커브 안쪽은 반경의 70% 까지만" 한 줄 규칙이 답이다.
3. **브리지는 바꾸지 않는다.** 우리는 이미 ucli(exec·스크린샷·테스트)와 Unity 공식 CLI MCP(`com.unity.pipeline 0.8.0-exp.1`, Hermes `unity_editor` 160개 도구 = 이것)를 둘 다 갖고 있다. CoplayDev·IvanMurzak·Glade·Funplay 등은 같은 기능의 다른 포장이고, 바꾸면 team.sh·fb_shot·build_asset·fb_geom 전부 손봐야 해 2~4일 + 회귀 위험에 비해 얻는 게 없다.
4. 에이전트 쪽에서 빌릴 것은 도구가 아니라 **규칙 3개**: "산출물 파일이 없으면 끝나지 못하게 하는 훅"(Claude-Code-Game-Studios), "어느 검증 단계까지 했는지 밝히기 + 깨진 화면은 실패"(unity-ai-skills), "텍스트 시나리오 + 단언 + 빈 결과 = 실패"(Unity Biome MCP PlayTest DSL).
5. Blender MCP 류(mcp-for-blender 32k★ 등)는 GUI 소켓 조작 + AI 3D 생성(Hunyuan3D·Tripo·Hyper3D) 중심이라 우리 규칙(사진 기반·AI 메시 금지)·헤드리스 파이프라인과 안 맞는다. 쓸 것은 "50개 헤드리스 Blender 테스트" 같은 검사 습관 정도.

## 바로 쓸 것 top 5 (무엇을 · 어디에 · 어떻게 · 품)

- **1. berat-racer 길 파이프라인 규칙 → `F1/Assets/Editor/RoadNetV2.cs` (MIT, 2026-10-06)**
  - 무엇: `docs/roads.md` + `tools/roads/{junction,profile,terrain,config}.py` 의 규칙. ① 교차로: 팔을 반시계로 정렬 → 이웃 팔과 연석이 안 겹치는 지점 + 모서리 곡선 길이만큼 팔을 잘라냄 → 표면 = 팔 토막 ∪ 모서리 둥근 쐐기 ∪ (연석이 안 만나는 쪽) 매끈한 곡선 아래 면, 길 끝 정점과 교차로 입구 정점을 **같은 정점으로 공유**(겹침·깜빡임 0) → 두 끝의 잘림이 서로 만나 남는 게 없는 짧은 간선은 "삼켜서" 두 교차로를 하나로. ② 종단: 표본마다 높이 + 교차로마다 평면 하나를 미지수로, 지형 추종 + 3차 미분 최소, 제약 = 최대 구배·최소 볼록/오목 종곡선 반경, 팔은 입구까지 교차로 평면 위. ③ 지형: 길 가장자리마다 1.5m 어깨(가장자리 4cm 아래) → 1:2 깎기/메우기 → 최소 6.5m, **커브 안쪽으로는 반경의 70% 이상 못 가게(FOLD_FRACTION 0.7)**, 다른 길과 1m 이상 떨어짐, 6m 앞치마로 원지형 복원 / 리본 아래 격자 정점은 6cm 내림 / 길이 닿는 격자 셀 모서리는 길 접평면 − cell²/2R 아래로(bench).
  - 어디: RoadNetV2 의 4(갈림목)·3(높이)·5(지형) 단계. 우리 상수와 대조: 우리 R=10m 필렛·구배 11%·어깨 2m·블렌드 6m 는 berat street 등급(구배 16%, 모서리 3.5m)·local(15%, 4m)과 같은 대역이라 수치는 그대로 두고 **구조**(정점 공유·짧은 간선 삼키기·70% 접힘 방지·접평면 bench)만 옮긴다.
  - 어떻게: 프로그래머 과제 2개로 나눔 — (a) 교차로 잘라냄·합집합·정점 공유·삼키기 (b) 리본 70% 규칙 + bench. QP 는 C# 에 솔버가 없으니 지금의 "구배 클램프 + 2차 차분" 반복을 유지하고, 교차로 평면 공유만 넣는다. 파이썬 평면 단계(`tools/map`)에서 미리 풀 거면 scipy/clarabel 설치가 필요 — 도현 승인 사항.
  - 품: 읽기 0.5일(PD/리서치) + [EDITOR] 과제 2개(각 25~40턴). 라이선스: MIT 고지 한 줄을 파일 머리에 남기면 일부 코드 번역도 허용.

- **2. berat 검사기 방식 → `scripts/fb_geom.py` 확장 또는 새 `tools/map/check_roads.py`**
  - 무엇: `tools/check_roads.py`(260줄) — 길 사각형·교차로 삼각형마다 **지형 메시가 길 면보다 위에 있으면 실패**(뚫고 나옴), 얼마나 아래인지(뜬 길) 분포, **모든 길 끝이 교차로 정점과 정확히 만나는지**. + `python -m roads gallery`(교차로 전부를 복잡한 순으로 48개씩 한 장) + `inspect --at x,z`(평면 + 높이 단면 한 장) + `shot.sh fork x z`(그 지점 게임 캡처 3장).
  - 어디: 지금 fb_geom 은 코멘트 반경 안만 본다. 맵 전체 전수 검사를 하나 두면 "내가 그린 곳만 고쳐졌다" 문제(같은 원인 남은 곳)를 숫자로 잡는다. 교차로 갤러리는 도현에게 보내는 한 장 진행 보고로도 쓴다.
  - 어떻게: Unity 쪽 C#(ucli exec, HideAndDontSave 임시 콜라이더 규칙 준수)로 길 메시·지형 메시를 읽어 JSON 출력 → 파이썬이 FAIL 목록 + 교차로 갤러리 PNG. 기준: 지형>길 0건, 길 끝↔교차로 정점 거리 0(1mm), 뜸 >5cm 0건.
  - 품: 과제 1개(25턴). 코드는 번역 수준이라 라이선스 문제 없음(MIT).

- **3. "산출물 없으면 못 끝남" 훅 → `scripts/team.sh` 의 Claude 실행 (`--settings` Stop 훅)**
  - 무엇: Donchitos/Claude-Code-Game-Studios(MIT, 2026-10-19)의 `.claude/hooks/validate-agent-artifact.sh` — 과제가 "결과를 `<경로>` 에 써라"로 끝나면, 에이전트가 멈출 때 그 파일이 디스크에 없으면 실패로 처리. 같은 저장소 `session-stop.sh`·`pre-compact.sh`·`detect-gaps.sh` 도 같은 계열.
  - 어디: 우리 고질병 "턴 한도에 걸려 보고서 없이 끝남"(스킬에 이미 '남은 개선: Stop 훅'으로 적혀 있음). team.sh 가 과제 원문에서 보고서 경로를 뽑아 Stop 훅 스크립트에 넘기고, 파일이 없거나 "작성 중"이면 exit 2 로 돌려보낸다.
  - 어떻게: 훅 스크립트 1개(30줄) + team.sh 에 `--settings` 한 줄. 팀원이 안 돌 때만 team.sh 를 고친다(바이트 오프셋 함정).
  - 품: 반나절. 우리 코드로 새로 쓰므로 라이선스 무관.

- **4. 검증 사다리 + 깨진 화면 실패 → `.claude/team/roles/*.md`·`lessons/*.md`, `scripts/fb_shot.sh`**
  - 무엇: Xenoneqq/unity-ai-skills(MIT, 2026-10-21) `skills/unity-agent-worker/references/verification.md` — 단계 0 컴파일 / 1 깨진 GUID 참조(에디터 없이 .meta 대조 파이썬) / 2 시작 시 예외 없음 / 3 빌드 / 4 사람 확인, **"어느 단계까지 갔는지 보고에 쓴다, 테스트 없는 프로젝트의 초록불을 '동작함'으로 읽히게 하지 않는다"**. `unity-scene-habits/references/visual-checks.md` — 캡처가 검정·하얗게 날아감·평평(분산 낮음)이면 테스트 실패.
  - 어디: 우리 고통 "숫자로만 확인하고 화면을 안 봄". 보고서 템플릿에 "도달 단계: 숫자(fb_geom) / 같은 카메라 전·후 / 판정자 2 / 도현" 한 줄을 의무화하고, fb_shot.sh 에 프레임 평균·분산 검사(검정·빈 화면이면 FAIL)를 넣는다.
  - 품: 문서 수정 1시간 + fb_shot 검사 20줄.

- **5. 텍스트 시나리오 플레이테스트 형식 → `TownPathBot` 시나리오 파일화**
  - 무엇: german-krasnikov/unity-biome-mcp(MIT, 2026-10-26) `docs/features/playtest.md` — `.playtest` 파일(`TELEPORT`·`WAIT`·`ASSERT 경로|컴포넌트|필드 > 0`·`ASSERT_CONSOLE_CLEAN`, SETUP/TEARDOWN), 성공 단계는 결과에서 지우고 실패·스냅샷만 남김, 묶음 실행 결과 `SUITE: 5/6`, **아무 시나리오도 안 잡히면 실패**, 실행 전 정적 lint. 보조로 kevinnie2003/playtest-agent(MIT, 2026-09-18)의 "게임 무관 오라클(끼임·벽 통과·무적…)을 모든 상태 전이에 적용, critical 이면 non-zero 종료".
  - 어디: 지금 봇 판정은 과제 글마다 다르게 적힌다. 배달지 쌍·코멘트 지점별 시나리오(출발→도착, 끼임 0, 수직가속 p95, 화물 충격)를 `docs/qa/playtests/*.txt` 로 두고 ucli exec 러너 하나가 읽게 하면 "같은 검사를 매번 같게" 된다. MCP 를 설치하지 않고 형식만 빌린다.
  - 품: 러너 C# 1개 + 시나리오 5개, 과제 1개(30턴).

## 참고만 (보고 배울 것은 있지만 지금 넣지 않음)

- `IvanMurzak/Unity-MCP` (Apache-2.0, 4.5k★, 2026-10-29) — 참고만: `screenshot-isolated`(물체 하나를 정해진 각도에서 단독 렌더·2×2)와 "프로젝트에 맞춘 스킬 자동 생성"은 좋은 아이디어지만 우리 fb_shot·studio_cap 이 같은 일을 한다. 전환 비용 2~4일.
- `CoplayDev/unity-mcp` (MIT, 15.5k★, 2026-10-19) — 참고만: 가장 큰 Unity MCP, 도구 그룹(필요한 묶음만 노출)으로 토큰 절약. Python uv 서버 추가 운영 부담, 우리 기능과 중복.
- `Glade-tool/glade-mcp` (MIT, 2026-10-21) — 참고만: `look_at_game_view`·런타임 이벤트 스트림·"play-verify heal gate"·CC0 에셋 라이선스 감사가 우리 방향과 같다. RAG·메모리는 유료 키. 형식(검증 게이트를 도구가 강제)만 참고.
- `FunplayAI/funplay-unity-mcp`·`kitwright/unity-mcp` (MIT, 2026-10-28/10-11, 사실상 같은 코드) — 참고만: `execute_code` 응답에 `{logs, created, modified, destroyed, returnValue}` 를 붙여 "무엇이 바뀌었나"를 재질의 없이 보여 줌, 스크린샷 diff. **이 응답 형식은 ucli exec 래퍼에 흉내 낼 만하다**(씬 변경 추적 → 팀원 보고 대조).
- `canakbass/unity-mcp` (MIT, 2026-08-30) — 참고만: "토큰 적게" 원칙이 구체적(도구 19개만 ≈3.1k vs 52개 ≈8.8k 토큰, 콘솔 중복 접기, 스크린샷 ≈2k 토큰이라 시각 확인에만, 물체 단독 렌더). 우리 team.sh 프롬프트 원칙으로 옮길 만함.
- `creativeandcreative9/EditorEyeStandAlone` (README 저장소, 본체는 Asset Store EULA, 2026-10-24) — 참고만: 공식 Unity CLI 대비 토큰 56% 절감 벤치(7과제, 1회씩, 벤더 자체 측정). 원인은 "대화 턴 수 → 누적 캐시 입력". 교훈 = 한 호출에 결과·진단을 몰아 주기. 파일 기반·Asset Store 라 설치 안 함.
- `AnkleBreaker-Studio/unity-mcp-server` (README 라이선스 불명확, 2026-10-23) — 참고만: 여러 에디터·여러 에이전트 동시 운용(에디터별 쓰기 직렬화, 액션 히스토리 대시보드). 우리 editor.lock + 대시보드와 같은 문제의식.
- `takashicompany/playcaller` (MIT, 2026-03-26) — 참고만: "Unity 용 Playwright", URP HDR 에서 ScreenCapture 가 멈추는 알려진 문제를 문서화 — 우리 캡처가 RT 렌더인지 확인할 근거.
- `kevinkicho/agent-vision-unity` (MIT, 2026-04-29) — 참고만: 0.5초마다 프레임+JSON 상태 기록, 분홍(셰이더 없음)/검정/밝기 픽셀 진단. 작가 스스로 "WIP, 쓸 수준 아님".
- `Donchitos/Claude-Code-Game-Studios` (MIT, 28.7k★, 2026-10-19) — 참고만(훅 1개는 top5): 49 에이전트·74 스킬 위계는 우리 규모엔 과함. `/gate-check`·`/test-evidence-review` 같은 이름 있는 검증 단계 정도.
- `Xenoneqq/unity-ai-skills` (MIT, 2026-10-21) — 참고만(일부는 top5): 배치 모드 기본·"YAML 손편집 금지"·장면 충돌 기준으로 일 나누기(`unity-manage-work`). 우리 [EDITOR] 직렬 규칙과 같음.
- `KilledByAPixel/LittleJS-AI` (MIT, 2026-10-14) — 참고만: 스킬 description 을 "REQUIRED before … TRIGGER whenever …"로 써서 자동 발동, 헤드리스 고정 스텝(`setHeadlessMode`+`engineStep(n)`)으로 시간 의존 로직 테스트, 플러그인 변경마다 버전·CHANGELOG 강제. 우리 스킬·교훈 파일 운영에 참고.
- `HermeticOrmus/claude-code-game-development` (MIT, 2026-10-04) — 참고만: 87 플러그인·80장 매뉴얼, 넓고 얕음. 엔진 감지·비밀키 확인 훅 정도.
- `ersingvn/ludra`·`SummerEngine/summer` (MIT, 2026-10-28) — 참고만: "에이전트용 엔진" — `play --screenshot 1s,end`·`test`(입력 스크립트+단언)·`review`(슬롭 판정) 3단 루프가 우리가 원하는 하네스의 모범형. 엔진 자체는 안 맞음.
- `limoaCatherine/game-studio-harness` (MIT, 2026-09-20) — 참고만: 사람 승인 게이트·`verify-report.json` 증거 묶음·컨텍스트 예산 주입. 중국어 문서, 무겁다.
- `becool3000/unity-dev-assistant-skills` (라이선스 없음, 2026-03-23) — 참고만: "컴파일 유휴일 때만 에디터 변경", 브리지 상태 점검·복구 스킬. 라이선스 없어 문장 복사 금지, 아이디어만.
- `r1n7aro/Locus` (GPL-3.0, 758★, 2026-10-25) — 참고만(외부 도구로만 가능): Roslyn 핫리로드(도메인 리로드 없이 Play 유지)·별도 프로세스 Roslyn 진단·에이전트 변경 되돌리기. Unity 쪽 패키지가 GPL 이라 게임 프로젝트에 넣으면 안 됨, 별도 앱으로만. 초기 테스트 단계.
- `polyblank66/Nyamu` (MIT, 2026-08-25) — 참고만: "컴파일 확인 하나만 잘하는" 최소 MCP. 우리는 Unity 6 + 공식 CLI 가 있어 불필요.
- `Yecats/unity-mcp-toolkit` (CC0, 2026-04-19) — 참고만: 공식 Unity MCP(`[McpTool]` 속성)에 우리 도구를 붙이는 예제. 공식 CLI MCP 에 fb_geom 같은 우리 검사를 도구로 올릴 때 본보기.
- `HoldMyBeer-gg/blenderwright` (MIT, 2026-10-28) — 참고만: 원시 Python 기본 꺼짐·OpenGL 빠른 뷰포트 캡처·메시 품질 분석. Blender 5.2 전용, GUI 소켓.
- `EL4CTEO/blender-skills` (MIT, 2026-10-19) — 참고만: 스킬 22개 + **헤드리스 Blender 테스트 50개**·스킬 토큰 예산 검사. build_asset.py 단계 테스트를 만들 때 구조 참고.
- `arjun988/blender-skills` (MIT, 2026-07-10) — 참고만: "사진 분석 → 카메라 맞춤 → 스크린샷 비교 루프 → 체크리스트 게이트" 순서. 실행은 Blender MCP 전제.
- `simoninithomas/awesome-ai-tools-for-game-dev`(CC0, 2024-11 정체)·`SubmitGame/awesome-ai-game-dev-tools`(CC0, 2026-10-29, 별100+·1년 내 활동만 수록) — 참고만: 목록. 후자는 갱신이 살아 있어 분기마다 훑을 가치.
- `pboechat/roadgen` (MIT, 2024-03) — 이미 `F1/Assets/Editor/ThirdParty/roadgen/` 에 들어와 있음. 평면 길망 생성기라 지형·교차로 메시 문제는 못 푼다.
- `ikpil/DotRecast` (Zlib, 2026-10-03) — 참고만: Recast/Detour C# 포트. 행인·지나가는 차 경로가 필요해질 때.

## 안 맞음

- `ahujasid/mcp-for-blender` (MIT, 32.7k★, 2026-10-29) — 안 맞음: GUI 안 소켓 서버 + Meshy·Tripo·Hyper3D·Hunyuan3D AI 3D 생성이 주력, 우리 규칙(AI 메시 금지)과 충돌. 다중 각도 뷰 검증은 헤드리스 bpy 로 이미 함. 스킬 결론("GUI 조작은 bpy 와 같은 천장")과 같음.
- `scenario-labs/blender-plugin`(GPL-3.0)·`Ludo-AI/ludo-mcp`·`felippeomgt/mymeshy`·`T0M13/golem`·`KOLONEX/trellis-api` 등 — 안 맞음: AI 이미지·3D 생성.
- `undreamai/LLMUnity`·`luxkun/ReGoap`·`bitbrain/beehave`·npc/behaviour tree 계열(목록의 약 40%) — 안 맞음: 게임 안 AI(NPC·행동트리). 지금 문제(길·검증·아트 파이프라인)와 무관.
- Godot/Unreal/Roblox/GameMaker MCP 들(`IvanMurzak/Godot-MCP`, `youichi-uda/godot-mcp-pro`, `Natfii/UnrealClaude`, `kevinpbuckley/VibeUE` 등) — 안 맞음: 엔진 다름.
- `llamacademy/ai-series-*` (MIT, 2021~23) — 안 맞음: NavMesh 튜토리얼 조각.
- `Saivarma-afk/Spline-Road-Generator` (2026-08-17) — 안 맞음(라이선스): README 기능(N갈래 교차로·필렛·뱅킹·청크)은 딱 맞지만 LICENSE.md 가 "Standard Unity Asset Store License, All rights reserved" — 코드 복사·이식 금지.
- `otto-link/AlphaModelRoadNetwork`·`Joako360/osm2terrn`·`FLHerne/mapgen` — 안 맞음(GPL): 게임에 코드 못 넣음.
- `HardikBasera/Agen-Link`·`Vidosen/ryx-sidekick-lite`(GPL)·`AlexeyPerov/Unity-Open-MCP`·`Signal-Loop/UnityCodeMCPServer`·`matthewagi/unityxclaude` — 안 맞음: 에디터 안 채팅/또 하나의 MCP, 우리 헤드리스 팀 구조에 추가 가치 없음.
- `IvanMurzak/Unity-AI-ProBuilder`·`Unity-AI-Animation`·`Unity-AI-ParticleSystem` — 안 맞음: 모델은 Blender 파이프라인에서 만든다.
- `gamedevpl/www.gamedev.pl`(GPL), `AbdulHannan031/gamesmith-ai-game-generator` 등 웹 게임 생성기 — 안 맞음.

## 길 생성 관련 코드 사례 (알고리즘 요약 + 라이선스)

- **berat-racer** (`Acid3croco/berat-racer`, MIT, Unity + Python, 2026-10-06, 원문 `docs/roads.md`·`tools/roads/*.py`·`tools/check_roads.py` 열람)
  - 그래프: 끝점이 만나는 곳 = 마디, 8m 미만 막다른 토막 삭제, 마디에서 꺾임 40° 미만·같은 등급 팔끼리 짝 → 이어진 "획(stroke)" = 관통 도로.
  - 평면: 획 하나를 조사선에 대한 최소제곱 + **곡률 변화(3차 차분) 벌점**으로 하나의 매끈한 곡선(직선·원호·완화곡선처럼) — 원래 선에서 1.2~2m 안에 머물 때까지 가중치 조정. 중요한 획부터, 지나는 마디가 따라 움직여 뒤 획을 고정.
  - 교차로: 위 top5-1 참조(정렬 → 연석 교차까지 잘라냄 + 모서리 반경 길이 → 팔 토막 ∪ 둥근 쐐기 ∪ 매끈한 곡선 아래 면, 입구 정점 공유, 짧은 간선 삼킴). shapely 사용.
  - 종단: 높이 + 교차로 평면을 한 QP 로(Clarabel), 지형 추종(멀리 떨어진 표본은 가중치 낮춤) + 3차 미분 최소, 등급별 최대 구배·최소 볼록/오목 반경, 횡경사도 같은 QP(v²/127R 의 절반, ≤7%, 교차로에선 평면 경사). 3.2km 타일 + 600m 후광, 체커보드 4라운드 병렬.
  - 등급표(일부): street 구배 16%·모서리 3.5m, local 15%·4m, collector 12%·5m, track 24%·2.5m. 지형이 등급보다 가파르면 지형 구배의 1.2배까지 허용, 절대 최대 45%.
  - 지형: 어깨 1.5m(가장자리 −4cm) → 1:2 깎기/메우기 → ≥6.5m, 커브 안쪽 반경의 70% 까지만, 다른 길과 ≥1m → 6m 앞치마. 리본 아래 정점 −6cm, 길 닿는 셀 모서리를 접평면 − cell²/2R 아래로. "6m 블렌드는 4m 격자에서 대각선 길에 계단이 됐다 → 리본으로 교체"라는 실패 기록이 우리 2m 격자·6m smoothstep 에도 해당.
  - 검사: 지형이 길·다리 위로 안 나옴, 길이 얼마나 뜨나, 길 끝 = 교차로 정점, 교차로 갤러리, 지점 inspect, 게임 캡처 3장, 오토파일럿 150초 "보이지 않는 멈춤 3→0, 수직 거칠기 0.15 m/s²".
  - 라이선스: MIT(LICENSE 파일 확인, GitHub 표시는 NOASSERTION). 지도 데이터(IGN·OSM)는 별개 — 코드·규칙만 가져온다.
- **spline-based-procedural-dirt-road-system** (`SeloSlav/…`, MIT, TypeScript/three.js, 2026-08-29, `src/roads/RoadJunctionBuilder.ts`·`RoadMeshBuilder.ts` 열람)
  - 교차로 = **허브 원 ∪ 팔 직사각형 입구의 별 모양 합집합 윤곽**: 균일한 극좌표 표본(72~팔×28개)에 각 팔의 좌우 ±90° 각도와 입구 앞 모서리 두 각도를 **명시적으로 추가**한 뒤, 각도마다 반경 = max(허브, min(반폭/sin, 도달거리/cos)) — 표본 사이로 모서리가 깎여 "지형 쐐기가 보이는" 문제를 막는다. 허브 반경은 반폭 이상(작으면 둔각 바깥에 삼각형 구멍).
  - 리본 접힘 완화: 단면마다 중심선 정점을 하나 둬서 반경 < 반폭 커브에서도 **안쪽 반쪽만 접히고** 바깥 반쪽은 UV·덮임 유지.
  - 지형: 깎기/메우기 없이 정점마다 지형 높이 + 오프셋으로 덮음(얕은 지형용) — 이 부분은 우리 비탈엔 부족.
  - 라이선스: MIT(파일 확인). 윤곽 함수 ~80줄은 C# 로 옮겨도 됨(고지 한 줄).
- **streets-gl** (`StrandedKitty/streets-gl`, MIT, 1.1k★, 2025-08-21, `src/lib/road-graph/IntersectionPolygonBuilder.ts` 열람)
  - OSM 교차로 다각형: 각 방향 세그먼트의 좌·우 가장자리 선을 만들고 각도순 정렬 → 이웃 세그먼트의 "왼쪽 가장자리 × 다음 오른쪽 가장자리" 교점 → 교점을 반대편으로 폭만큼 투영 → 가장자리 위 위치(alpha)로 정렬해 가장 안쪽 교점들로 다각형, 각 세그먼트는 그 점까지 잘라냄(trimTo). 3갈래 이상만. 평면 합성기(`tools/map/compose_osm_town.py`) 쪽 교차로 정리에 그대로 쓸 수 있는 가장 짧은 구현(195줄).
- **Unity-SplineRoadUtils** (`elrod/…`, MIT, 31★, 2025-05-09 — 활동 멈춤, `Runtime/SplineRoad.cs`·`Editor/ConformTerrainToRoadOverlay.cs` 열람)
  - Unity Splines 위 길·교차로: 교차로 = 들어오는 팔 끝 가장자리 쌍을 중심 기준 각도로 정렬 → 이웃 가장자리 사이를 중점과 중심을 잇는 2차 베지어(곡률 슬라이더)로 채운 부채꼴. 우리 RoadNetV2 의 "2차 베지어 모서리"와 같은 계열이고 잘라냄이 없어 팔끼리 겹칠 수 있음.
  - 지형 맞춤: 길 삼각형마다 높이맵 점이 삼각형 안이면 무게중심 보간 높이로 lerp(가장자리 거리 falloff) — **삼각형 밖 falloff 가 없어** 바깥 경사면이 안 생김. Unity Terrain 전용. 참고만.
- **RoadArchitect** (`FritzsHero/RoadArchitect`, MIT, 367★, 2026-10-08 — 원본 `MicroGSD/RoadArchitect` 988★ 2020 정체)
  - 완성형 Unity 도로 도구(교차로·다리·가드레일·지형 맞춤·Jobs 멀티스레드). 기본값 `maxGrade = 0.08`(최대 구배 켬)·`matchHeightsDistance = 50m`(Unity Terrain 높이맵을 50m 폭으로 길 높이에 맞춤). Unity Terrain·LTS 2020.3 기준이라 우리 메시 지형엔 직접 못 씀. 교차로 메시(`Scripts/RoadIntersection.cs`, 1000줄)는 차선·회전차로 중심이라 마을 골목엔 과함. 참고만.
- **Unity-Terrain-Path-Toolkit** (`Ruffy-Git/…`, MIT, 2026-09-22, v0.1) — 참고만: 알고리즘은 단순 띠지만 **"생성 메시 지문(fingerprint) → 손댄 흔적이 있으면 재생성 차단 → '현재 메시를 기준으로 채택' / '수동 수정 버리고 재생성' 명시 선택"** 이 우리 "씬 메시를 직접 고친 과제는 빌더 재실행에 사라진다" 문제의 깔끔한 해법.
- **Unity-Terrain-Under-Mesh-Road-Helper** (`JeremieConstant/…`, CC0, 2026-01-19) — 참고만: SplineExtrude 길 + MeshCollider 레이로 아래 지형을 올림(파괴적·되돌릴 수 없음). 너무 단순.
- **Unity-Terrain-Snapper-Tools** (`CharReanimation/…`, MIT, 2026-10-23) — 참고만: 스플라인 노트를 지형 높이로 스냅. 우리 높이 단계보다 못함.
- **bfjord-tools** (`brianwang9100/bfjord-tools`, MIT, 2026-09-16) — 참고만: Unity 6 지형·길·다리 레시피 도구. 매 판 "178/178 Unity Editor checks 통과 + 같은 카메라 증거 캡처·영상"을 README 에 남기는 **증거 기반 진행 보고 형식**이 도현용 진행 보고 본보기. 알고리즘 자체는 Unity Terrain 기반.
- **AuraLite world generator** (`AlexanderNyr/…`, Apache-2.0, 2026-06-17) — 참고만: Unity 6 URP, A* 유기적 길 + 침식 지형. 길 메시·교차로 품질 근거 없음.
- **blender-racemap-generator-addon** (`robertkardos/…`, 라이선스 없음, 2021) — 참고만(복사 금지): 길 곡선 + 교차로 구 → 길 메시 → 교차로 채움 → **땅과 길을 한 메시로 Triangle(Shewchuk) 삼각분할로 이음** — "길과 지형을 따로 두지 않는" 방식(d안)의 예. 작가 스스로 매우 불안정하다고 씀.
- **Unity Splines 2.9 샘플** (Unity Companion License, 우리 PackageCache 열람) — `LoftRoadBehaviour.cs`·`HighwayExample.cs` 뿐, **교차로·지형 통합 없음**(기존 조사 결론과 같음).
- **Sebastian Lague Path-Creator** (MIT, 2.1k★, 2024-05 정체) — 참고만: 베지어 경로 + 길 메시 예제. 교차로 없음.

## 브리지 판정 (전환 안 함 — 근거)

- 지금 가진 것: ① `youngwoocho02/unity-cli`(MIT, 2026-08-28) — Go CLI ~800줄 + C# 커넥터 ~2,300줄, `exec`(임의 C#)·scene/game 스크린샷·`test`(EditMode/PlayMode)·console·`[UnityCliTool]` 확장 ② Unity 공식 CLI 1.0.0-beta.11 `unity mcp` + F1 의 `com.unity.pipeline 0.8.0-exp.1`(F1Studio 엔 없음) = Hermes `unity_editor` 160개 도구(capture_game_view·run_tests·eval·bake 등).
- 후보들이 내세우는 "내장 스크린샷/테스트 루프"는 둘 다 이미 있다. 실제 차이는 토큰 효율(호출 수)인데, 우리는 이미 ucli exec 한 번에 여러 단계를 넣는 방식이라 EES 벤치가 말하는 이득(턴 수 줄이기)을 같은 방법으로 얻는다.
- 전환 비용 추정: team.sh allowedTools·`fb_shot.sh`·`fb_geom.py`·`build_asset.py unity_exec`·`studio_cap.sh`·`iter_cap.sh`·역할 프롬프트·교훈 파일 전부 수정 + 두 에디터 재검증 = 2~4일, 그동안 [EDITOR] 과제 정지. 이득이 측정으로 보이기 전엔 하지 않는다.
- 하나만 할 만한 것: F1Studio 에도 `com.unity.pipeline` 을 넣어 Hermes `unity_editor` 를 스튜디오에서도 쓰게 할지 — 도현 결정(패키지 추가).

## 모르는 것 / 한계

- berat-racer 는 별 0개의 개인 저장소다 — 문서가 매우 정밀하지만 다른 사람이 검증한 코드는 아니다. 실제 실행은 안 해 봤다(설치 금지).
- 각 MCP 의 토큰 절감 수치는 모두 벤더 자체 측정(1회 실행)이다.
- 웹 검색 화면이 아니라 같은 질의의 REST API 결과를 썼다 — 정렬·총수(367)는 같지만 웹 쪽 순서가 미세하게 다를 수 있다.
