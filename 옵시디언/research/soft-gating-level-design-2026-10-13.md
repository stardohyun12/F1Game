# 소프트 게이팅 레벨디자인 사례 — "막는 문"이 아니라 "비싼 길" (2026-10-13 밤)

> 도현: "높이와 진행 단계를 묶는 건 좋은데, 선택을 억지로 막는 레벨디자인은 불쾌할 수 있다. 젤다 등 사례를 조사하자." 조사는 web-search 에이전트.

## 바로잡은 전제
- BotW "삼각형 규칙"은 GDC 2017 강연이 아니라 일본 강연(CEDEC 2017 추정, 2차 요약)에서 나옴. GDC 2017 주제는 "벽 없애기"와 multiplicative gameplay.
- "gravity" 개념은 확인 안 됨(GMTK 는 "points and lines → 볼거리로 끌어당기기").

## 정의·합의
- 하드 게이트 = 조건 전엔 못 지나감 / 소프트 게이트 = 들어갈 수 있지만 비싸거나 위험.
- 소프트 게이트는 막지 않고 경고한다 → 오픈월드에서 더 납득됨(비평·포럼 수준 근거).
- Nintendo 는 BotW 에서 "못 지나가는 벽"과 "정해진 순서"를 의도적으로 없앰(GDC 2017).
- 너무 친절한 길안내도 실패: 초기 "탑과 선" 방식에서 테스터가 레일 위 느낌 → 멀리 보이는 다양한 보상 지점으로 끌어당기기로 해결(GMTK).

## 사례
- BotW: 지형(삼각형 언덕)+스태미나. 넘을지 돌아갈지 매번 선택. 봉우리가 시선을 끌고 뒤를 가려 벽이 아니라 궁금증. 시작의 대지 = 닫힌 튜토리얼 안에서 자유를 먼저 맛보게.
- Elden Ring: 난이도가 게이트. "질 수는 있지만 이길 경우가 있어야 공정".
- **SnowRunner**: 진흙·경사·강이 게이트, 타이어·윈치·차종이 열쇠. "갈 수는 있는데 박힌다". 원인이 업그레이드 이름으로 읽혀 납득. (우리와 가장 닮음)
- **Death Stranding**: 지형×짐 무게×균형. 지형을 파랑/노랑/빨강으로 미리 보여줌. 길·장비 업그레이드로 같은 길이 쉬워짐. 비판: "첫 10시간 느림", "돌 하나에 짐이 날아감" — **우리와 가장 닮은 실패 위험**.
- Metroid Dread: 능력 열쇠 하드 게이트, 열쇠를 자물쇠 가까이·작은 구역에 가둬 헤매지 않게.
- Outer Wilds: 지식이 게이트, 처음부터 다 갈 수 있음.
- Mario 3D Land/World: 기승전결 — 안전하게 소개 → 조금 어렵게 → 비틀기 → 숙달. Cakewalk Flip 은 처음엔 떨어져도 안 죽는 낮은 곳에서.
- ETS2/ATS, Pacific Drive, Lake: 출처 미확보.

## 손실 회피
- 잃는 건 얻는 것보다 세게 느낀다(Engelstein, Achievement Relocked 2020). USask 300명 실험: 게임 속 금화에서도 강한 손실 회피 → 벌점이 보상보다 크면 그 행동 자체를 피함.
- → **언덕 파손 벌금이 지름길로 아끼는 시간보다 크면 아무도 언덕에 안 간다.**

## PitCrew 규칙 8개 ([근거]/[추론])
1. 언덕은 절대 막지 않는다. 대가는 화물 파손뿐. [근거 BotW·SnowRunner]
2. 첫 언덕은 짧고 보이게, 화물은 싸고 덜 깨지는 것(케이크 말고 계란). [근거 Mario 기승전결]
3. 미끄러짐은 트렁크캠으로 그 순간, 정산 화면에 원인을 말로("○○ 언덕 경사에서 −₩800"). [근거 DS·SnowRunner / 정산 문구는 추론]
4. 업그레이드는 잠김→열림이 아니라 **비쌈→지름길**. 패드는 같은 언덕의 손상률을 낮춤. [근거 SnowRunner·DS]
5. 언덕 벌금 < 지름길 이득. 수치는 측정 필요. [근거 USask·Engelstein / 수치 추론]
6. 언덕 꼭대기에 랜드마크(등대·카페) — 벽이 아니라 목적지. [근거 BotW]
7. 평지 우회로는 늘 하나. 언덕이 강제가 아니라 선택. [추론 BotW]
8. 구역 해금은 벽이 아니라 거리와 보수. 절벽 도로는 처음부터 열되 멀고 비싸게. [추론 Elden Ring·Outer Wilds]

## 링크
GDC 2017 https://gdcvault.com/play/1024562/Change-and-Constant-Breaking-Conventions · GMTK https://gmtk.substack.com/p/how-nintendo-solved-zeldas-open-world · 80.lv https://80.lv/articles/the-design-secrets-of-breath-of-the-wild/ · Kotaku https://kotaku.com/when-miyamoto-first-played-zelda-breath-of-the-wild-h-1793017234 · Miyazaki https://blog.playstation.com/2022/01/28/an-interview-with-fromsoftwares-hidetaka-miyazki/ · SnowRunner https://blog.playstation.com/?p=223636 · DS https://www.tomsguide.com/news/death-stranding-review-roundup · Mario https://www.gamedeveloper.com/design/the-secret-to-i-mario-i-level-design · Metroid https://blog.hslu.ch/games/files/2021/11/Metroidvania_Analysis.pdf · Outer Wilds https://gdcvault.com/play/1027008/Independent-Games-Summit-Sparking-Curiosity · USask https://harvest.usask.ca/handle/10388/13478 · https://game-wisdom.com/?p=17372 · https://www.mapcore.org/topic/20013-player-gating-in-level-design/

---
## (별건) 멀티에이전트 가시화 OSS — 2026-10-13 밤 조사
도현: "Claude·Codex 창 왔다갔다 귀찮고 누가 어디서 뭘 하는지 안 보임. UX 좋은 거나 오픈소스 없나."
- **Vibe Kanban**(BloopAI, Apache-2.0, ~27.9k★, 2026-04): 칸반 카드마다 Claude Code/Codex/Gemini CLI 등 실행자 선택, 워크트리·터미널·diff 리뷰·MCP 서버. `npx vibe-kanban`. **추천.**
- Claude Squad(smtg-ai): tmux TUI, Claude Code·Codex·Aider, 워크트리. 라이선스 AGPL-3.0 로 적힌 곳 있음(미확인).
- Crystal(stravu, MIT): Electron GUI, 2026-02 중단(후속 Nimbalyst).
- 한계: 셋 다 "자기가 띄운 에이전트"만 보임. 채팅 안 서브에이전트는 앱 Tasks 패널 + studio-dashboard 피드.
