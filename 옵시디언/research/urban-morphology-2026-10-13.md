# 실제 마을은 어떻게 생기나 — 형성 원리와 절차적 생성 논문 (2026-10-13 밤)

> 도현: "실제 도시나 마을이 어떻게 만들어지는지에 관한 논문이나 방식에 맞춰 맵을 만들어 보자." 조사는 web-search 에이전트. [확인]=웹에서 확인 / [기억]=에이전트 기억, 원문 대조 필요.

## 한 줄
아말피형 "바다 앞 비탈 작은 마을"에 맞는 논문은 **Emilien 2012(지형 위 마을)** + **Galin 2010(경사 비용 도로)**. 둘 다 "비탈이 비쌀수록 돌아가는 A*"라 C# 수백 줄로 재구현 가능(원본 코드 비공개). 난점 = 6m 칸에서 헤어핀이 계단으로 깨짐 → **칸은 길 찾기에만, 모양은 스플라인** 두 겹.

## 생성기가 가져갈 규칙
- Conzen[기억]: 길·필지 경계는 오래 남고 건물만 바뀜 → 길→필지→건물 순.
- Muratori/Caniggia[기억]: 집은 기본 유형(좁은 정면·깊은 필지)의 지형 변형 → 프리팹 2~3종 + 비탈 따라 층수·기단만.
- Hillier 1993/1996 공간구문론[확인]: 통행량은 길 연결 구조(통합도)가 정함 → **통합도 높은 길에 가게**.
- Alexander "A City is Not a Tree"[기억]: 순환 고리 필요 → 막다른 길만 두지 말고 계단·지름길로 고리.
- Alexander 패턴[기억]: 관문·작은 광장·계단식 대지·길과 목표물 → 입구 관문 1, 큰길 끝 광장 1, 길 끝마다 보이는 목표.
- Lynch 1960[기억]: 길·경계·구역·결절점·랜드마크 → 바다(경계)·종탑(랜드마크)·광장(결절점)이 어디서든 보이게. **배달 게임에 직결.**
- UNESCO 830 아말피[확인]: 낮은 비탈은 계단밭(terrazzamenti), 위는 목초 → 마을 위 비탈은 계단밭 띠.
- 경사 상한[기억]: 차도 8~12%(산악 15%), 그 위는 계단.

## 논문 표 (적합도 순)
- **Emilien et al. 2012 "Procedural generation of villages on arbitrary terrains"** (Visual Computer 28)[확인] — 관심도 지도로 씨앗·도로 성장 → 필지 → 비탈 맞춤 형상문법. 코드 없음. **최상.**
- **Galin et al. 2010 "Procedural Generation of Roads"** (CGF 29)[확인] — 이방성 최단경로(경사·장애물 비용) → 클로소이드 스플라인 → 지형 깎기. LibPath 문서만. **해안 절벽 도로에 최적.**
- Galin 2011 "Authoring Hierarchical Road Networks"[확인] — 큰길→작은길 위계. 중상.
- Parish & Müller 2001[기억] — L-system + 전역 목표/지역 제약. 대도시용, 중.
- Chen 2008 텐서장[기억] — 등고선을 방향장으로 쓰면 등고선 따라 도는 길. 중.
- Vanegas 2012 필지[기억] — OBB 반복 분할. 필지 단계, 중.
- Benes 2014·Weber 2009·Lechner·Aliaga 2008·Nishida 2016·Kelly&McCabe 2007 — 도시 규모, 하.
- Townscaper[기억] — 불규칙 메시 WFC, 비탈·도로 논리 없음. Mizdal&Pozzer 2018[확인] — 고도맵 A* 경사 비용으로 지그재그. **Runevision 2016[확인] 함정: 비용을 "총 오른 높이"로 주면 지그재그가 안 나온다 — 칸마다 경사 벌점.**

## 우리 레시피 8단계 (C# 약 800~1,300줄, 외부는 Unity Splines 하나)
1. 지형 먼저(계곡 1·해안 절벽·항구 평지, 손/노이즈, SO) 50~150줄
2. 해안도로 = 칸 A*(상태=칸+진행방향 8, 비용=거리×(1+k·경사²), 경사>12% 금지, 45° 초과 꺾임 벌점) → Catmull-Rom 스플라인 150~250줄
3. 마을 씨앗 = 바다 가까움+계곡 입구+경사 낮음 최댓값 → 항구 30~60줄
4. 중심 길 항구→광장: 2번 재사용
5. **등고선 지그재그 길**(Emilien 성장, 등고선 나란하면 싸고 가로지르면 비쌈, 띠 바뀌는 곳에 헤어핀) — 가장 어려움. 헤어핀 1개 = 2~3칸, 칸 경로 그대로면 계단 → 스플라인 변환 + 헤어핀 자리 평평한 원형 패드. 200~400줄
6. 필지 = 스플라인 양옆 일정 간격, 좁고 깊게, 높이차는 기단/옹벽 100~200줄
7. 가게 자리 = 길 그래프 BFS 평균 최단거리(통합도 간이판) 상위 길 50~100줄 (검산 depthmapX)
8. Lynch 랜드마크 = 광장 종탑 + 계곡 위 1, 배달지마다 보이는지 레이캐스트 자동 검사 50~100줄
기존 roadMask·TrackConnectivityVerifier 는 "칸 그래프" 층에 그대로, 보이는 도로만 스플라인.

## 링크
Emilien 2012 https://perso.liris.cnrs.fr/egalin/Articles/2012-villages.pdf · Galin 2010 https://perso.liris.cnrs.fr/egalin/Articles/2010-roads.pdf · Galin 2011 https://www.cs.purdue.edu/homes/bbenes/papers/Galin11CGF.pdf · LibPath https://perso.liris.cnrs.fr/eric.galin/Doc/Path/ · Runevision https://blog.runevision.com/2016/03/note-on-creating-natural-paths-in.html · Mizdal&Pozzer https://www.sbgames.org/sbgames2018/files/papers/ComputacaoFull/188241.pdf · Hillier 1993 https://discovery.ucl.ac.uk/id/eprint/1398/ · Hillier 1996 https://discovery.ucl.ac.uk/id/eprint/1403/ · UNESCO https://whc.unesco.org/en/list/830

한계: Emilien 본문 PDF 미열람(수치는 원문 확인 필요). Conzen·Caniggia·Alexander·Lynch·Parish·Chen·Vanegas 는 기억 기반.
