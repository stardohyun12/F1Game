[EDITOR][OPUS][C-001 그레이박스] 수리 1개 — 부딪혀 상한 카트를 정비소에서 고친다
카드: docs/team/cards/C-001-repair-one/card.md — 본문은 브리프(이 과제 글) 안에 있다. 카드의 「범위」 파일만 만지고 「하지 말 것」은 건드리지 않는다. 카드 밖 일·아트·다듬기는 하지 않는다.
단계 = 그레이박스: 회색 대역으로 수용 기준이 실제로 돌게만 만든다(겉모습은 나중 — 아트 단계가 따로 있다).
끝낼 것(이 넷이 없으면 미완료). 시간 상자 60분 — 40분이 지나면 start.cs·critN.cs·보고서부터 지금 상태로 쓰고 이어서:
1. docs/team/cards/C-001-repair-one/start.cs — Play 중 exec 로 실행되는 C# 조각. 카드 「시작 장면」(start_place 9, 하네스가 먼저 순간이동함)에서 수용 기준의 동작을 10초 안에 화면에 보여 준다(입력 흉내·순간이동·EditorApplication.update 콜백 허용, 마지막 줄 return "<한 줄 상태>";). 하네스는 이걸로 10초 클립(프레임 증가·에러 0)을 찍는다. 네가 직접 한 번 돌려 확인.
2. docs/team/cards/C-001-repair-one/crit1.cs, crit2.cs, … — **수용 기준 번호마다 하나.** 각각 새 Play 에서 start_place 순간이동 뒤 exec 되며, 순간이동·상태 세팅(단계 값·든 물건·카트 위치·시선)으로 **그 기준이 화면에 보이는 순간**을 2초 안에 만든다. 하네스가 2초 뒤 캡처 1장(critN.png, 1×+3×)을 찍고 검증 일꾼은 기준 N 을 그 사진으로 판정한다 — 긴 흐름을 클립 하나에 욱여넣지 마라(지난 불통과의 원인). 마지막 줄 return "<그 기준의 상태 값>";. 네가 각각 돌려 화면을 확인.
3. 보고서(4요소) — 무엇을 바꿨나·파일 목록. 스크린샷은 1~2장만(증거 묶음은 하네스가 tools/evidence/bundle.sh 로 따로 찍는다).
4. 카드의 「기계 확인」 항목을 실제로 돌린 결과를 보고서에.

[브리프 한 장 D-277] 위 과제 + 아래 카드·필수 규칙·세계 스냅숏·파일 지도·에디터 치트가 시작 글의 전부다. 다른 규칙 문서(PIPELINE·project_rules·체크리스트·FLOW)는 열지 않는다.

## 카드 (카드 C-001 · 단계 greybox · 불통과 1회 · start_place 9 · docs/team/cards/C-001-repair-one/card.md)
# 수리 1개 — 부딪혀 상한 카트를 정비소에서 고친다

## 왜
11/5 목표 「한 판(5분) + 수리 1개」 중 수리가 지금 맵(Map_TownV6) 한 판에 하나도 없다(10/30~11/2 목표, 관련 커밋 0 — 비서 점검 2026-11-02). 배달 → 정산 → **수리** → 다음 배달이 한 번은 사람 손으로 돌아야 프로토타입이다. 이야기상 주인공은 정비소 아르바이트생(STORY.md) — 자기 카트를 정비소에서 고치는 게 자연스럽다.

## 범위 (만질 파일)
- 새 `F1/Assets/Scripts/Garage/CartDamage.cs` — 카트 상처 단계 0~3(세게 부딪힌 충격량으로 오름), 회색 대역 표시(앞판 기울기·작은 회색 연기 정도)
- 새 `F1/Assets/Scripts/Garage/RepairSpot.cs` — 정비소 안 정비 자리, 상처 있는 카트가 자리 안에 있고 내려 있으면 [E 고치기]
- `F1/Assets/Scripts/OnFoot/OnFootDirector.cs` — E 칩·누름 연결만(최소 줄 수)
- `F1/Assets/Scripts/OnFoot/DayLog.cs` — 수리 한 줄(그날 번 돈에서 뺌)
- `F1/Assets/Scripts/Debug/DebugWindow.cs` — 버튼 「카트 상처 +1」(검증·도현 확인용)
- 씬 `Map_TownV6.unity` 에 정비 자리 오브젝트 1개(정비소 안, 회색 바닥 표시) — 런타임 생성으로 해결되면 씬은 저장하지 않는다
- 이 카드 폴더 `start.cs`

## 하지 말 것 (잠근 것)
- 코스·길·지형, 차 조작값(ArcadeCarController·DriveFeel·CartBodyMotion), 화물·주문 보상 수치, 도현이 놓은 건물 위치
- 정한 값(내부 해상도 360·Nearest 등), 공유 .mat
- **상처가 운전·속도·화물에 영향을 주지 않는다**(이번 카드는 보이기만 — 영향은 도현 한 판 뒤 따로 정함)
- 아트(모델·텍스처) 만들지 않는다 — 회색 대역만. 수리비는 상수 하나(가설값, 코드에 `// 가설` 주석)

## 시작 장면
- 씬 `Assets/Scenes/Map_TownV6.unity` → Play → 디버그 창(` 또는 F1) 장소 9 「정비소 안」(카트가 정비소 카트 자리에 옴)
- 디버그 창 「카트 상처 +1」 두 번 → 카트를 보고 → 정비 자리에서 E

## 수용 기준
1. 도현이 카트로 벽에 세게 부딪히면(또는 디버그 창 「카트 상처 +1」) 카트에 상한 표시(앞판이 기운 회색 대역 + 작은 연기)가 보이고, 단계가 오를수록 더 심해 보인다.
2. 도현이 상한 카트 곁 정비 자리(정비소 안)에 서면 [E 고치기] 칩이 보이고, E 를 누르면 상한 표시가 사라진다.
3. 고친 순간 수리비 돈 글자(「−숫자」)가 떠오른다.

## 기계 확인
- 하네스 증거 묶음 `bash tools/evidence/bundle.sh C-001 after`: 프레임 증가(frames_ok), 클립 동안 에러 0(log_ok) — 자동
- 이 기능이 안 됐으면 실패했을 검사: start.cs 마지막 return 에 `상처 2→0 · DayLog 수리 −N` 이 찍힌다(exec 로 CartDamage 단계·DayLog 값 직접 읽기)

## 사람 확인 (도현 한 판)
- 배달 한 바퀴 중 아무 데나 세게 부딪혀 본다 → 정비소로 돌아와 카트 내리고 E → 그날 일기에 수리 줄
- 묻는 질문: 「상처가 다음엔 운전에도 영향을 줘야 하나(삐걱·한쪽 쏠림), 보이기만 할까?」

## 아트 (도현 한 판 통과 뒤에만)
- 상한 카트 표시(찌그러진 앞판·연기)의 실제 모양 — 기준 = 지금 카트 표면(V5 같은 재료), 도현 판정 뒤 정함. 지금은 없음.


## 끝 조건
- 도현이 「사람 확인」을 해 보고 통과라고 함 → (아트 →) 통합 → 검증 PASS → 완료.

## 기록 (최근 3줄)
- 2026-11-03T11:10 verify→dohyun · 검증 → 도현 한 판 · 검증 PASS — 상한 표시(기운 회색 대역+연기)·고치기 칩·고친 뒤 −300원 모두 strip 에서 보임, 기계 확인 true
- 2026-11-03T11:39 dohyun→pd · 도현 판정: 수정 — 일단 연기나는것만 만들어 놔도 될듯 (스튜디오 화면) · PD 가 다음 칸으로 옮김
- 2026-11-03T13:12 dohyun→build · 도현 한 판 → 그레이박스 · 도현 웹 판정(11:39) 수정 원문: 일단 연기나는것만 만들어 놔도 될듯 — PD 가 1시간 반 늦게 봄

## 필수 규칙 (훅이 못 막는 것만 — 나머지는 훅이 막는다)
1. 한국어, 짧고 직설. 실행 안 한 것은 "안 했다"고 쓴다.
2. 완료 보고 4요소: 산출물 경로(파일:줄) · 확인 방법 · 검증 증거(실제 돌린 것만) · 남은 이슈.
3. git commit/push 금지(하네스가 함). 폰트 SDF(F1/Assets/UI/Fonts)는 끝에 `git checkout --`.
4. 시간 상자 60분은 상한이지 채울 목표가 아니다. 40분 지나면 start.cs·보고서부터 지금 상태로. 끝나면 바로 마무리.
5. 카드 「범위」 파일만, 「하지 말 것」은 안 건드린다. 카드 밖 개선·아트·도구는 「남은 이슈」 한 줄로만.
6. 확인 = 순간이동(DebugWindow.GoPlace)으로 고친 자리에서 바로 + 캡처 1~2장 + "안 됐으면 실패할 검사 1개". 숫자만으로 "고쳤음" 금지(같은 카메라 전/후).
7. Play 중엔 .cs 를 고치지 않는다(재컴파일 → 상태 날아감). 끝에 `editor stop`, 원래 씬, 저장 안 된 변경 0.
8. 도현이 정한 값(해상도 360·Nearest, 건물 위치·높이·노선, 차 조작값)은 바꾸지 않는다 — 필요하면 멈추고 보고.
9. 아트: 원화·목업 → 도현 승인 → 에셋. AI 생성 이미지·3D·소리 금지, CC0 만(출처 art/provenance.csv). 간판 글자는 도현이 정한 이름만.
10. 두 번 불통과면 같은 방법을 세 번째 반복하지 않는다. 직전 판정 원문부터 고친다.
11. 증거는 하네스가 찍는다(bundle.sh) — 너는 수용 기준 번호마다 카드 폴더에 critN.cs(그 기준이 보이는 순간을 만드는 C#). 스크린샷 수십 장 금지.
12. 이 브리프가 시작 글의 전부다. PIPELINE.md·project_rules·체크리스트·FLOW.md 는 열지 않는다(훅이 막는다).

## 세계 스냅숏 (docs/world/WORLD.md 전문 — 끝에 world_check.sh 가 불변 조건을 검사한다)
<!-- 자동 생성: scripts/world_snapshot.sh — 손으로 고치지 말 것 -->
생성 2026-11-03 14:05 · 커밋 e984006d · 브랜치 main

# 세계 스냅숏 — Map_TownV6
에디터 6000.3.27f2 · 에디트 모드 맞음 · 씬 dirty=False · 루트 오브젝트 28

## 장소 (DebugPlaces.All — 디버그 창 순간이동 자리)
| 이름 | 발 좌표 | yaw | 카트 규칙 | 땅(레이) | 메모 |
|---|---|---|---|---|---|
| 집 방 | (187.1, 28.5, -62.7) | 41 | CartSpot | y 28.48 (RUG_01) Δ-0.02 | 2층 방 가운데, 문 쪽을 봄 |
| 집 앞 | (184.1, 25.4, -66.5) | 10 | Near | y 24.37 (TownV6_Terrain) Δ-1.03 | 1층 현관(DOOR_01 184.5,-63.9) 밖 2.6 m, 집을 봄 |
| 정비소(사장 앞) | (231.1, 26.7, -56.4) | 53 | CartSpot | y 26.71 (GARAGE_shell) Δ+0.01 | 사장(232.7,-55.2) 1.9 m 앞 — E 말하기가 떠야 함 |
| 카트 옆 | (0.0, 0.0, 0.0) | 0 | BesideCar | - | 지금 카트가 있는 곳, 내릴 때 서는 자리 — E 타기가 떠야 함 |
| 첫 배달 찻집 | (93.8, 4.0, -282.0) | 180 | Near | y 3.95 (TownV6_Terrain) Δ-0.05 | DP_tea (93.8,-288.0) 표식 6 m 북쪽 도로에서 표식을 봄 |
| 노을 낚시 자리 | (-9.0, 1.3, -289.9) | 180 | Near | y 1.30 (Pier) Δ+0.00 | 손가락 부두 남쪽 끝 자리 뒤 1.1 m — E 앉기가 떠야 함 |
| 항구 | (272.2, 1.3, -234.7) | 270 | Near | y 1.30 (Pier) Δ+0.00 | 동쪽 부두 갑판(Pier 272,-235), 바다를 봄 |
| 언덕 꺼짐(sink_1031) | (110.0, 18.7, 30.0) | 140 | Near | y 18.67 (TownV6_Terrain) Δ-0.03 | Cam_FB_sink 가 보는 점(120,19,18)을 내려다보는 자리 |
| 큰길 | (175.2, 18.0, -75.2) | 90 | Near | y 18.12 (Walkways) Δ+0.12 | main 도로 중심선 점(175,-75) |
| 정비소 안 | (216.0, 26.7, -55.5) | 90 | CartSpot | y 26.73 (GARAGE_shell) Δ+0.03 | 차고 가운데(벽까지 5~8 m) — 바람·파도가 먹먹해야 함 |
| 언덕 꼭대기 | (244.0, 64.8, 118.0) | 200 | Near | y 64.83 (TownV6_Terrain) Δ+0.03 | 마을 둘레 지형 최고점(y 64.8, 레이 격자 4 m) — 바람이 가장 큼 |
| 택배 보관소 앞 | (270.5, 30.9, -34.0) | 0 | Fixed (273.5, 29.6, -37.4) yaw 90 | y 30.90 (TownV6_Terrain) Δ+0.00 | 열린 셔터(x 268~271.5) 앞 마당, 안을 봄 — 카트는 앞 길, 마당엔 동쪽 회색 경사로(x 277)로 |
| 택배 보관소 칠판 | (268.9, 31.1, -29.4) | 270 | Fixed (273.5, 29.6, -37.4) yaw 90 | y 31.03 (Q3_building) Δ-0.07 | 서쪽 벽 칠판 1.8 m 앞 — E 칠판 보기가 떠야 함 |
보관소 앞 카트 자리 DepotCart = (273.5, 29.6, -37.4)

## 배달 지점 (DP_* — 목적지 깃발 앵커는 DotScreenUI.DestAnchorOf = 위치 + 3 m, 씬 오브젝트 아님)
| 이름 | 위치 | 활성 | DeliveryPoint | 라벨 | 문 앞 대역(DoorHandover ≤12 m) | 땅(레이) |
|---|---|---|---|---|---|---|
| DP_MART | (225.1, 22.0, -78.3) | 켜짐 | 있음 | MART | 없음 | y 22.48 Δ+0.50 |
| DP_depot | (270.0, 32.6, -34.6) | 켜짐 | 있음 | 택배 보관소 | 없음 | y 33.10 Δ+0.50 |
| DP_hill_field | (180.8, 28.3, 6.9) | 켜짐 | 있음 | hill_field | 없음 | y 28.78 Δ+0.50 |
| DP_lighthouse | (-378.0, 2.1, -361.1) | 켜짐 | 있음 | lighthouse | 없음 | y 2.60 Δ+0.50 |
| DP_tea | (93.8, 4.6, -288.0) | 켜짐 | 있음 | tea | Handover_tea 1.9 m | y 5.05 Δ+0.50 |
| DP_top_house | (-295.4, 63.5, 357.9) | 켜짐 | 있음 | top_house | 없음 | y 63.96 Δ+0.50 |

## 상호작용 물건 ([E] 칩을 내는 컴포넌트)
| 종류 | 오브젝트 | 위치 | 활성 |
|---|---|---|---|
| ArcadeCarController | Race | (-86.5, 5.5, -77.7) | 켜짐 |
| DeliveryPoint | DeliverySystem/AlleyEnd | (63.6, 0.7, 92.5) | 켜짐 |
| DeliveryPoint | DeliverySystem/CoastCafe | (174.9, -1.8, 107.1) | 켜짐 |
| DeliveryPoint | DeliverySystem/HairpinOrchard | (283.7, 13.6, 51.1) | 켜짐 |
| DeliveryPoint | DeliverySystem/LoneHouse | (208.7, 20.5, -177.7) | 켜짐 |
| DeliveryPoint | OnFoot_HomeGarage/OnFoot_Depot/DP_depot | (270.0, 32.6, -34.6) | 켜짐 |
| DeliveryPoint | TownV6_Gray/Delivery/STOP_MART/DP_MART | (225.1, 22.0, -78.3) | 켜짐 |
| DeliveryPoint | TownV6_Gray/Delivery/STOP_hill_field/DP_hill_field | (180.8, 28.3, 6.9) | 켜짐 |
| DeliveryPoint | TownV6_Gray/Delivery/STOP_lighthouse/DP_lighthouse | (-378.0, 2.1, -361.1) | 켜짐 |
| DeliveryPoint | TownV6_Gray/Delivery/STOP_tea/DP_tea | (93.8, 4.6, -288.0) | 켜짐 |
| DeliveryPoint | TownV6_Gray/Delivery/STOP_top_house/DP_top_house | (-295.4, 63.5, 357.9) | 켜짐 |
| DeliveryPoint | _TUT_HOLD/TUT_TeaHouse_NPC/TUT_TeaDelivery | (181.4, 27.7, 5.7) | 꺼짐 |
| DepotOrderBoard | OnFoot_HomeGarage/OnFoot_Depot/OrderBoard | (267.1, 32.6, -29.4) | 켜짐 |
| DepotPackSpot | OnFoot_HomeGarage/OnFoot_Depot/PackSpot | (274.4, 32.2, -29.5) | 켜짐 |
| DoorHandover | TownV6_Gray/Delivery/STOP_tea/Handover_tea | (94.0, 4.0, -286.2) | 켜짐 |
| FishingSpot | Sunset_D227/FishingSpot | (-9.0, 1.3, -291.0) | 켜짐 |
| PackBench | OnFoot_HomeGarage/OnFoot_Depot/PackSpot/PackBench/Eye | (273.7, 32.9, -29.5) | 꺼짐 |
| RoomBed | OnFoot_HomeGarage/Room_BedSpot | (185.9, 28.8, -62.5) | 켜짐 |
| RoomCalendar | OnFoot_HomeGarage/Room_Calendar | (189.0, 29.6, -62.1) | 켜짐 |
| 디렉터 슬롯 boss | OnFoot_HomeGarage/OnFoot_GarageBoss | (232.7, 26.7, -55.2) | 켜짐 |
| 디렉터 슬롯 spawn | OnFoot_HomeGarage/OnFoot_Spawn_Room | (187.1, 28.5, -62.7) | 켜짐 |
| 디렉터 슬롯 cartSpot | OnFoot_HomeGarage/OnFoot_CartSpot | (229.5, 26.7, -55.5) | 켜짐 |
| 디렉터 슬롯 firstDrop | TownV6_Gray/Delivery/STOP_tea/DP_tea | (93.8, 4.6, -288.0) | 켜짐 |
| 디렉터 슬롯 orderBoard | OnFoot_HomeGarage/OnFoot_Depot/OrderBoard | (267.1, 32.6, -29.4) | 켜짐 |
| 디렉터 슬롯 packSpot | OnFoot_HomeGarage/OnFoot_Depot/PackSpot | (274.4, 32.2, -29.5) | 켜짐 |
| 디렉터 슬롯 depotDest | OnFoot_HomeGarage/OnFoot_Depot/DP_depot | (270.0, 32.6, -34.6) | 켜짐 |
| 디렉터 슬롯 handover | TownV6_Gray/Delivery/STOP_tea/Handover_tea | (94.0, 4.0, -286.2) | 켜짐 |
| 디렉터 슬롯 calendar | OnFoot_HomeGarage/Room_Calendar | (189.0, 29.6, -62.1) | 켜짐 |
| 디렉터 슬롯 bed | OnFoot_HomeGarage/Room_BedSpot | (185.9, 28.8, -62.5) | 켜짐 |

## 흐름 단계 (OnFootDirector 의 enum)
- Mode: Starting → OnFoot → InCart → Seated → Board → Pack → Calendar → Diary
- 지금 State = Starting (에디트 모드면 Starting)

## 대역 오브젝트 (이름에 Placeholder/placeholder/대역/_tmp/tmp_/TMP_/Dummy)
- 숨긴(꺼진) 대역: 0 · 켜진 대역: 0
- _TUT_HOLD(보류 튜토리얼 물건, D-201): 자식 13 · 꺼짐

## 기타
- 켜진 카메라: 2
- 부두 난간 PierRails: 자식 콜라이더 1108 · WalkOnlyRail 있음
- HeldPaper(손 전표·칠판 쪽지): 0
- NavMesh 정점: 0 (없음 — 땅 검사는 레이캐스트로)

## 파일 지도 (범위 파일마다 멤버:줄 · 이 파일을 참조하는 .cs)
- **F1/Assets/Scripts/Garage/CartDamage.cs** (253줄): class CartDamage:10 · Of():33 · AddStage():40 · Clear():49
  참조: DebugWindow.cs, RepairSpot.cs
- **F1/Assets/Scripts/Garage/RepairSpot.cs** (84줄): class RepairSpot:10 · Contains():58 · Ready():65 · Repair():73
  참조: CartDamage.cs, OnFootDirector.cs
- **F1/Assets/Scripts/OnFoot/OnFootDirector.cs** (1648줄): class OnFootDirector:14 · enum Mode:16 · SimInteract():303 · GoToSpot():600 · ForceTalk():819 · SimCalendarClose():904 · SimDiaryClose():905 · SimBoardMove():1032 · …+12
  참조: AmbienceProbe.cs, ArcadeCarController.cs, AudioCueBus.cs, DayLog.cs +29
- **F1/Assets/Scripts/OnFoot/DayLog.cs** (60줄): class DayLog:9 · struct Visit:13 · ResetAll():33 · Record():39 · RecordRepair():45 · NextDay():52
  참조: DotScreenUI.Diary.cs, OnFootDirector.cs, RepairSpot.cs, RoomCalendar.cs
- **F1/Assets/Scripts/Debug/DebugWindow.cs** (280줄): class DebugWindow:13 · GoPlace():51 · Fish():71 · Recast():72 · ToggleCart():73 · SetSpeed():74 · CartHurt():76 · SetUIStyle():85 · …+3
  참조: AmbienceProbe.cs, DestClarityShots.cs, DotScreenUI.cs, DriveFeelProbe.cs +3

## 에디터 명령 치트
- 실행: `bash scripts/ucli-http.sh exec 'return DebugWindow.GoPlace(12);'` (긴 C# 는 파일로 `exec @docs/team/cards/<ID>/start.cs`; `Object` 는 `UnityEngine.Object` 로 완전 한정)
- Play: `bash scripts/ucli-http.sh editor play --wait` → 바로 `exec 'Application.runInBackground = true; return "ok";'` → 끝에 `editor stop`
- 콘솔: `bash scripts/ucli-http.sh console --type error` (에러 0 이어야 끝)
- 순간이동: `DebugWindow.GoPlace(n)` — n 은 아래 세계 스냅숏 「장소」 표 순서(0부터). 카트는 장소의 카트 규칙대로 같이 옮겨진다
- 캡처: Play 중 `ScreenCapture.CaptureScreenshot("docs/qa/tmp/<이름>.png")` 1~2장, 또는 tools/evidence/clip.cs.tmpl 방식(LowResRenderTest.Present → RenderTexture → PNG = 도현이 보는 화면)
