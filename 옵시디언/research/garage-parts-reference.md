# 차고/부품 복원 시스템 레퍼런스 리서치

> 작성일: 2026-05-15 · PitCrew 결정 채널 7 (F1 머신 복원 트리) 설계용
> 목적: 비슷한 게임들이 "차량/기계를 부품 단위로 복원·강화하는 메타 진행"을 어떻게 설계했는지 조사 → PitCrew 데모(6주, 부품 2개) 스코프에 맞는 패턴 도출

---

## 1. 게임별 분석

### Jalopy — PitCrew와 가장 유사한 루프
낡은 동독제 차(Laika 601)를 부품 단위로 수리하며 동유럽을 여행한다. **컴포넌트 카테고리 7종**: 엔진 / 카뷰레터 / 에어필터 / 연료탱크 / 점화코일 / 워터탱크 / 배터리. 각 카테고리마다 4티어가 있고, 상위 티어는 단순 상향이거나 트레이드오프(예: 2-스트로크 엔진은 출력은 좋지만 연료에 오일을 섞어줘야 함)가 있다. 부품은 **여행 중 들르는 상점에서 돈으로 구매**해 교체하고, 손상된 부품은 Repair Kit(소모품, 내구도 3포인트 회복)이나 트렁크에 설치하는 **Tool Rack 업그레이드**(브론즈/스틸/골든 렌치 = 33%/66%/100% 수리)로 고친다. 게이팅은 순수하게 "돈 + 상점 도달"이다.

루프 구조 자체가 PitCrew의 거울상이다 — 한 구간 운전 → 도착 → 정산/수리/교체 → 다음 구간. **다만 설계 결함 사례가 교훈**: 집에 무한 귀환하며 와인을 공짜로 챙기고 수리도 공짜로 하는 익스플로잇이 가능했다. 즉 자금/수리 게이팅이 루프를 강제하지 못하면 메타 진행이 무의미해진다. 리뷰들은 수리 자체는 분위기 있지만 반복되면 단조롭다고 평가 — 수리에 "서사적 보상"이 안 붙으면 클릭 노동이 된다.
출처: https://jalopy.fandom.com/wiki/Components · https://techraptor.net/gaming/reviews/jalopy-review-communist-clunker · https://steamcommunity.com/sharedfiles/filedetails/?id=799620871

### Pacific Drive — 런 사이 차고 메타의 교과서
스테이션 왜건을 거점(Garage)에서 정비·강화하고 Zone으로 원정을 나간다. 핵심은 **차 부품 업그레이드와 "차고 자체(Auto Shop) 업그레이드"를 분리**한 것. 차고 업그레이드(작업대 티어업, Matter Deconstructor, 보관함 확장 등)가 메타 진행의 척추이고, 차 부품 업그레이드(오프로드 타이어, 연료 합성기 등) 다수가 차고 업그레이드를 **선행 조건**으로 요구한다. 총 143개 블루프린트가 **3중 게이팅**으로 잠겨 있다: (1) 선행 업그레이드, (2) 리서치 요구(특정 아이템을 리서치해야 해금), (3) 크래프팅 재료. 차고에는 작업대/보관소/카 리프트가 물리적으로 배치돼 있고, 차고를 확장하면 새 제작 스테이션이 열린다.

핵심 reward loop: **"콘텐츠 클리어 → 업그레이드 → 더 어려운 콘텐츠 푸시"** — PitCrew 매크로 루프와 동일한 형태. 다만 143개는 1인 6주 데모엔 명백히 과하다. 가져올 건 *구조*(허브가 물리 공간, 부품/허브 업그레이드 분리, 다단 게이팅)지 *볼륨*이 아니다.
출처: https://progameguides.com/pacific-drive/all-garage-upgrades-how-to-unlock-them-in-pacific-drive/ · https://screenrant.com/pacific-drive-best-upgrades/ · https://www.escapistmagazine.com/best-car-upgrades-to-unlock-first-pacific-drive/

### Car Mechanic Simulator — "조립 깊이"의 상한 경고
수천 개 개별 부품을 분해/진단/복원한다. 리뷰 양면 평가: 접근성은 좋다는 의견과 "분해/설치가 우습게 단순화됐다"는 의견이 공존하지만, **공통 결론은 "차 열몇 대 고치면 클릭 노동이 되어 지루해진다"**. 부품 단위 시뮬레이션의 깊이는 그 자체가 게임의 목적일 때만 유지된다. PitCrew에서 부품 복원은 *수단*이고 목적은 F1 복귀 서사다 → **조립/분해를 인터랙티브 미니게임으로 만들면 안 된다.** 복원은 "선택 → 확정 → 연출" 한 번의 결정이어야 한다.
출처: https://www.gamepressure.com/editorials/reviews/car-mechanic-simulator-2021-review/zf46e · https://www.xboxtavern.com/car-mechanic-simulator-review/

### Motorsport Manager / F1 23 My Team — F1 부품 트리의 카테고리화
Motorsport Manager: 차는 섀시 + 컴포넌트로 나뉘고 컴포넌트는 **최대 6종**, 각각 다른 성능 facet을 담당(엔진·기어박스가 가장 중요). **티어 게이팅**: 약한 부품을 먼저 만들어야 더 좋은 부품을 만들 수 있다. F1 23 My Team: R&D를 **4개 영역으로 단순화** — Aerodynamics / Chassis / Powertrain / Durability. 각 영역이 명확히 다른 성능에 매핑(에어로=직선속도, 섀시=무게·안정성·타이어마모, 파워트레인=엔진·연료). 부품은 챌린지/레이스 보상으로 받은 Tech Point로 구매. **시리즈가 거듭되며 트리를 단순화한 방향성**(복잡한 RP 누적 → 성과 보상 직결)이 1인 개발에 주는 시사점이 크다.
출처: https://motorsportmanagerpc.fandom.com/wiki/Car_parts · https://simracingsetup.com/f1-manager/f1-manager-2024-car-development/ · https://screenrant.com/f1-23-how-to-upgrade-every-car-part/ · https://racinggames.gg/article/f1-23-career-mode-teams-drivers-tracks-development-more

### Slay the Spire / FTL / Hades — 로그라이트 메타 진행 페이싱
세 게임의 설계 철학이 명확히 갈린다. **FTL**: 새 함선을 해금하지만 난이도·런은 그대로 — 메타가 "옵션 추가"일 뿐. **Slay the Spire**: 메타 진행을 의도적으로 최소화, 첫 세이브로도 클리어 가능, 해금은 복잡도를 점진적으로 여는 역할. **Hades**: 메타 진행을 *서사 프레임 자체*로 삼음 — 같은 콘텐츠를 반복하는 게 스토리의 일부이고, V자 난이도 곡선, 영구 업그레이드와 음성 연기가 런마다 "이야기가 진행된다"는 감각을 준다. **PitCrew는 Hades 모델에 가장 가깝다** — 반복되는 배달 런이 서사(F1 복귀)를 향해 누적되는 구조. 단 Hades 비판도 존재("승리하려면 그라인드를 강제당한다") → 복원이 너무 느리면 배달이 노동처럼 느껴진다.
출처: https://www.resetera.com/threads/do-you-like-meta-progression-in-your-roguelikes-roguelites.1341955/ · https://news.ycombinator.com/item?id=43667333 · https://steamcommunity.com/app/646570/discussions/0/1649917420745034167/

### Spiritfarer / Dave the Diver — 복원·시설 업그레이드가 서사인 게임
**Spiritfarer**: 배(barge)를 시설 단위로 짓고 업그레이드. 시설 업그레이드는 효율 상승 또는 새 가능성 해금. 핵심은 **Albert's Shipyard라는 전용 허브에서 "블루프린트 스테이션"을 Glims+재료로 구매**하는 단계적 구조 — 그리고 배의 물리적 외형이 업그레이드마다 눈에 보이게 변한다. **Dave the Diver**: 초반 핵심 업그레이드가 산소탱크/슈트/카고박스/작살로 카테고리가 명확하고 적음. 둘 다 공통점은 **"업그레이드 = 가시적 변화 + 서사 진행"**이고 카테고리 수를 적게 유지한다는 것.
출처: https://www.neoseeker.com/spiritfarer/walkthrough/Upgrading_your_ship · https://steamcommunity.com/sharedfiles/filedetails/?id=3232516012 · https://primagames.com/gaming/what-to-upgrade-first-in-dave-the-diver

---

## 2. PitCrew에 적용 가능한 패턴 표

| 패턴 | 어느 게임에서 | PitCrew에 어떻게 | 데모 스코프에 맞나 |
|---|---|---|---|
| 부품 카테고리를 4~6개로 제한 | F1 23(4개), Motorsport Manager(6개), Jalopy(7개는 다소 많음) | 엔진/변속기/서스펜션/에어로/타이어/도색 중 풀게임 = 6개, 데모 = 2개만 복원 가능하게 노출 | O — 데모는 2개, 카테고리 골격만 |
| 부품 ≠ 허브 업그레이드 분리 | Pacific Drive | 기존 `UpgradeSystem`(감각형 업그레이드, 정산 카드)은 그대로 두고, "복원 트리"는 **별도 차고 화면**으로 분리. 복원은 서사 노드, 업그레이드는 수치 튜닝 | O — 기존 시스템 재사용, 신규는 차고 화면 1개 |
| 복원 = 한 번의 결정 + 연출 (조립 미니게임 아님) | Car Mechanic Sim의 반면교사, Spiritfarer | 차고에서 부품 선택 → 자금 확인 → 확정 → "엔진 살아났다" 마일스톤 컷 1회. 분해/조립 인터랙션 없음 | O — 미니게임 신규 개발 회피 |
| 다단 게이팅 (자금 + 선행 조건) | Pacific Drive(3중), Motorsport Manager(티어 선행) | 데모는 2단으로 충분: (1) 자금 임계값, (2) 선행 부품. 예: 변속기는 엔진 복원 후 해금 | O — 2단이면 충분, 3중은 과함 |
| 가시적 변화 (업그레이드가 눈에 보임) | Spiritfarer(배 외형), Dave the Diver | 차고에 놓인 F1 머신 모델이 부품 복원마다 해당 부위가 회색→정상으로. Kenney 에셋 제약 안에서 머티리얼 스왑 수준이면 OK | △ — 머티리얼 스왑은 가능, 신규 모델 제작은 금지 |
| 반복 런이 서사로 누적 (Hades 모델) | Hades | 배달 런 = 자금, 자금 임계 도달 시 차고에서 복원 가능 알림. 복원할 때마다 서사 한 줄 진행 | O — 알림 + 텍스트 컷이면 구현 가벼움 |
| 트레이드오프 있는 상위 티어 | Jalopy(2-스트로크 엔진) | 데모에선 보류 권장. 풀게임에서 "복원 부품에 옵션 분기" 고려 | X — 데모 스코프 밖, ideas-parking 행 |
| 허브가 물리 공간 | Pacific Drive, Spiritfarer(Shipyard) | 차고를 실제 씬/공간으로. 이미 차고 개념 있으면 거기에 F1 머신 + 복원 패널 배치 | △ — 전용 씬이면 OK, 새 3D 공간 구축이면 비용 주의 |

---

## 3. 안티패턴 / 스코프 위험 (1인 6주 데모에서 피할 것)

- **조립/분해 미니게임화**: Car Mechanic Sim 경로. 부품을 끌어다 끼우는 인터랙션은 신규 시스템이고 금세 클릭 노동이 된다. 복원은 "버튼 한 번 + 연출"로 끝낸다.
- **부품 카테고리 7개+ 또는 티어 4단계**: Jalopy가 7종 4티어인데 데모엔 과하다. 데모는 2개 복원, 풀게임도 6 카테고리 × 단일 복원(또는 1~2 강화 티어)로 충분.
- **3중 게이팅을 데모에 그대로**: Pacific Drive의 선행+리서치+재료 3중은 143개 콘텐츠가 있을 때 얘기. 데모는 자금+선행 2단.
- **복원 트리를 기존 `UpgradeSystem`에 욱여넣기**: 감각형 업그레이드와 서사형 복원은 페이싱·UI·연출이 다르다. 섞으면 둘 다 어정쩡해진다. 차고 화면을 별도로.
- **익스플로잇 가능한 자금 루프**: Jalopy의 무한 귀환 사례. 복원 게이팅이 배달 루프를 실제로 강제하는지 확인. 데모에서도 "공짜 반복"이 없는지 점검.
- **신규 3D 차고 공간 구축**: 전용 씬·카메라·인터랙션을 새로 만들면 6주 안에 위험. 기존 정산 화면 흐름에 "차고 패널"을 얹는 정도가 안전.
- **복원이 너무 느린 페이싱**: Hades 비판처럼 그라인드 강제가 되면 배달이 노동이 된다. 데모 시나리오 3런 안에 최소 1개 부품 복원이 체감되도록 비용 곡선 설정.

---

## 4. 핵심 권고 (PitCrew 차고/부품 시스템이 최소한 가져야 할 것)

1. **별도 차고 화면 1개** — 기존 `UpgradeSystem`(감각형, 정산 카드)은 건드리지 말고, F1 머신 복원 트리는 차고 패널로 분리한다. 복원 = 부품 선택 → 자금 확인 → 확정, 끝. 조립 미니게임 없음.
2. **데모는 부품 2개만, 카테고리 골격은 6개 표시** — 엔진→변속기 순으로 자금+선행 2단 게이팅. 잠긴 4개는 회색으로 보여줘 풀게임 트리를 암시(F1 23식 단순 R&D 트리 시각화).
3. **복원마다 서사 마일스톤 컷 1회** — "엔진이 다시 돌았다" 같은 텍스트/연출 + 차고의 F1 머신 모델 해당 부위 머티리얼 스왑. Hades·Spiritfarer 모델: 반복 런이 서사로 누적되는 감각.
4. **복원 게이팅이 배달 루프를 실제로 강제하는지 검증** — Jalopy 익스플로잇 반면교사. 데모 3런 안에 1개 부품 복원이 체감되도록 비용 곡선을 맞추고, 공짜 반복 경로가 없는지 점검.
5. **부품 효과는 "감각형 업그레이드"와 다르게** — 복원 부품은 서사 진행 + 약한 패시브(또는 새 콘텐츠 해금) 정도. 세밀한 수치 튜닝은 기존 `UpgradeSystem`이 담당. 역할을 겹치지 않게.
