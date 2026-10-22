# 오픈소스·에셋 후보 — 마을 생성기 보강 (2026-10-20, 리서치팀)

> 조사만 했다(다운로드·설치 없음). 표시: [검산] = gh api/curl 로 직접 확인 · [페이지] = 검색 결과·스토어 문구 · [미확인] = 못 봄 · 공수 = **가설**.
> 제외: roadgen · OSM · Copernicus DEM · `city-generator-oss-2026-10-19.md` 에 이미 있는 것. 이미 프로젝트에 있는 Kenney city-kit 4종(commercial·industrial·roads·suburban)도 제외.

## 1. 도현 링크 (instagram.com/p/DeFVMzjufBg)
- 로그인 없이는 캡션이 안 열린다. 메타로 확인된 것 [검산]: 계정 **@index.opensource** 의 릴스, 섬네일 문구 "OPEN SOURCE MAP GENERATOR", 검은 바탕에 흰 유선 격자와 빨간 점, 옆에 회색 블록 도시와 파란 강·노란 큰길.
- **추정**: 그 그림은 ProbableTrain/MapGenerator(텐서장 도시 지도 생성기, 웹 데모 probabletrain.itch.io/city-generator)의 화면과 같은 구성이다. [검산] LGPL-3.0 · ★1,475 · 마지막 push 2024-05.
- 이건 이미 지난 조사의 ④번이다. 미국식 격자 도시용이고 지형·비탈을 모른다 → 아말피 비탈 마을에는 새로 얻을 게 거의 없다.

## 2. 후보

### ① Kenney Modular Buildings — 건물 반복 문제
- URL: https://kenney-assets.itch.io/modular-buildings · 라이선스 **CC0**(상업 가능, 표기 불필요) [페이지]
- 무엇: 벽·창·문·지붕 조각 90여 개(FBX/glTF). 지금 Kenney city-kit 의 "통째 건물"과 달리 **층·폭·지붕을 조합해서** 집을 만든다 → 같은 집 복붙 문제를 직접 줄인다. Kenney 기준 룩과 같은 계열이라 그래픽 게이트 위험이 가장 낮다.
- 비용(가설): 임포트 반나절 + 조합 규칙(층 1~4·폭 1~3칸·지붕 2종) 하루. 아말피풍은 색·평지붕으로 맞춰야 한다.
- 열어 확인: 검색 결과·itch 문구만. 실제 조각 목록은 미확인.

### ② KayKit City Builder Bits — 중심가 집 밀도·소품
- URL: https://kaylousberg.itch.io/city-builder-bits · **CC0** [페이지, Godot 에셋 라이브러리도 CC0 표기]
- 무엇: 로우폴리 도시 모델 32+개(건물·도로·소품). 중심 광장 주변을 채우는 작은 건물·소품용.
- 비용(가설): 반나절. 단, KayKit 은 둥글고 장난감 같은 룩이라 Kenney 와 섞으면 어긋날 수 있다(그래픽 게이트 필요).
- 열어 확인: 검색 결과만.

### ③ Quaternius Medieval Village MegaKit (무료 Standard 판) — 돌집·비탈 테라스 느낌
- URL: https://opengameart.org/ (Quaternius Medieval Village MegaKit) · **CC0**, 무료판 176 모델, 300+ 원본판은 유료 [페이지]
- 무엇: 돌벽·계단·아치 조각. 지중해 비탈 마을의 옹벽·계단·테라스 단을 표현하는 데 쓸 만한 조각이 들어 있을 가능성. 중세풍이라 그대로 쓰면 아말피가 아니다 — 옹벽·계단만 골라 쓰는 게 현실적.
- 비용(가설): 고르기·임포트 반나절, 색 맞춤 반나절.
- 열어 확인: 미확인(목록 안 봄).

### ④ Unity Splines 패키지(com.unity.splines) — 교차점 노면
- URL: https://docs.unity3d.com/Packages/com.unity.splines@2.8/manual/index.html · Unity 공식 패키지(Unity Companion License, Unity 프로젝트 안에선 사용 가능) [페이지]
- 무엇: 이미 우리 스플라인 도로가 Unity 안에 있다면 공식 샘플에 "도로 만들기" 예제가 있다. 교차점 메시는 직접 짜야 하지만(그래프는 범위 밖이라고 포럼에서 명시) 노면 UV·폭 처리 참고용.
- 비교: 교차점 자체는 지난 조사 ⑦ RoadArchitect(MIT, [검산] 2026-10-08 push, ★364)가 "동적 교차로"를 가진 유일한 후보. 통째로 넣기보다 **교차로 메시 생성 코드만 읽고 따라 하는** 쪽을 권한다.
- 비용(가설): 샘플 읽기 1~2시간, 우리 생성기에 교차로 패치(원판+필렛) 구현 1~2일.
- 열어 확인: 문서 목차만.

### ⑤ 물가 — OceanFlow (URP 카툰 물, 해안 거품)
- URL: https://github.com/zzamjak-cloud/OceanFlow (OpenUPM com.zzamjak.oceanflow) · [검산] OpenUPM 메타 **GPL-3.0-only**
- 무엇: 깊이 차이로 해안 거품을 만드는 URP 물. "물가 단색" 문제를 정면으로 푼다.
- **문제: GPL-3 → 닫힌 상용 게임에 그대로 넣기 어렵다.** 방법만 참고(장면 깊이 − 물 깊이 = 거품 띠)하고 셰이더는 직접 짜는 게 맞다. MIT 대안은 못 찾았다. 유료 대안: Asset Store "Stylized Water 2"(Staggart), Poseidon.
- 비용(가설): 깊이 기반 거품 Shader Graph 직접 작성 반나절~하루.

### 참고 — Buildify (Blender 지오노드 건물 생성기)
- https://paveloliva.gumroad.com/l/buildify · 무료, **라이선스 미확인**(페이지에서 안 보임). Blender OSM 애드온과 같이 쓰는 예가 있다. 라이선스가 확인되면 ①의 Blender 경로 대안.

## 3. 추천 (리서치팀 의견, 결정은 도현)
1. **① Kenney Modular Buildings** — 건물 반복을 조각 조합으로 줄이고, 지금 룩(Kenney)과 같은 계열이라 위험이 가장 작다. CC0.
2. **⑤의 방법만 차용한 직접 거품 셰이더** — 물가 단색은 에셋으로 사면 돈, 오픈소스는 GPL 이라, 깊이 차 거품 한 줄 원리만 가져와 반나절~하루로 직접 짠다.
- 교차점은 RoadArchitect(이미 조사됨) 코드 참고가 가장 빠르다.

## 4. 한계
- 에셋 내용(조각 목록·실제 생김새)은 하나도 열어 보지 않았다 — 스토어 문구 기준. 받기 전에 아트팀이 미리보기 확인 필요.
- 인스타 릴스 정체는 섬네일 근거의 추정.
