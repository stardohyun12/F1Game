[EDITOR][OPUS][C-003 그레이박스] 첫 배달 — 칠판 쪽지부터 찻집 문 앞 건네기까지
카드: docs/team/cards/C-003-first-delivery/card.md — 본문은 브리프(이 과제 글) 안에 있다. 카드의 「범위」 파일만 만지고 「하지 말 것」은 건드리지 않는다. 카드 밖 일·아트·다듬기는 하지 않는다.
직전 검증 불통과(3회째) — 판정 원문: 기준 2: 상자는 카트에 실리지만 방향 표시가 찻집을 가리키는지 증거에서 확인 안 됨(깃발 표지는 4칸에만, 5~8칸엔 없음)
근거 프레임: strip 1·2·4·5·7·8칸
먼저 이것부터 고친다.
단계 = 그레이박스: 회색 대역으로 수용 기준이 실제로 돌게만 만든다(겉모습은 나중 — 아트 단계가 따로 있다).
끝낼 것(이 넷이 없으면 미완료). 시간 상자 60분 — 40분이 지나면 start.cs·critN.cs·보고서부터 지금 상태로 쓰고 이어서:
1. docs/team/cards/C-003-first-delivery/start.cs — Play 중 exec 로 실행되는 C# 조각. 카드 「시작 장면」(start_place 11, 하네스가 먼저 순간이동함)에서 수용 기준의 동작을 10초 안에 화면에 보여 준다(입력 흉내·순간이동·EditorApplication.update 콜백 허용, 마지막 줄 return "<한 줄 상태>";). 하네스는 이걸로 10초 클립(프레임 증가·에러 0)을 찍는다. 네가 직접 한 번 돌려 확인.
2. docs/team/cards/C-003-first-delivery/crit1.cs, crit2.cs, … — **수용 기준 번호마다 하나.** 각각 새 Play 에서 start_place 순간이동 뒤 exec 되며, 순간이동·상태 세팅(단계 값·든 물건·카트 위치·시선)으로 **그 기준이 화면에 보이는 순간**을 2초 안에 만든다. 하네스가 2초 뒤 캡처 1장(critN.png, 1×+3×)을 찍고 검증 일꾼은 기준 N 을 그 사진으로 판정한다 — 긴 흐름을 클립 하나에 욱여넣지 마라(지난 불통과의 원인). 마지막 줄 return "<그 기준의 상태 값>";. 네가 각각 돌려 화면을 확인.
3. 보고서(4요소) — 무엇을 바꿨나·파일 목록. 스크린샷은 1~2장만(증거 묶음은 하네스가 tools/evidence/bundle.sh 로 따로 찍는다).
4. 카드의 「기계 확인」 항목을 실제로 돌린 결과를 보고서에.

[브리프 한 장 D-277] 위 과제 + 아래 카드·필수 규칙·세계 스냅숏·파일 지도·에디터 치트가 시작 글의 전부다. 다른 규칙 문서(PIPELINE·project_rules·체크리스트·FLOW)는 열지 않는다.

## 카드 (카드 C-003 · 단계 greybox · 불통과 3회 · start_place 11 · docs/team/cards/C-003-first-delivery/card.md)
# 첫 배달 — 칠판 쪽지부터 찻집 문 앞 건네기까지

## 왜
도현 목표(11/5): 첫 배달. D-250 흐름(보관소 칠판 주문 → 작업대 포장 → 카트 → 문 앞 건네기)을 처음부터 끝까지 한 번에 이어 붙인다. 부분(손에 든 종이 c0c733ac·포장대 be4bba3f·건네기·목적지 4cccf317)은 있으나 한 줄로 이어진 적 없음.

## 범위 (만질 파일)
- F1/Assets/Scripts/OnFoot/OnFootDirector.cs, DepotOrderBoard.cs, Packing/Bench/*, Delivery 건네기 관련 스크립트(이어 붙이기만)
- 새 기능 추가 금지 — 이음새(다음 단계 안내·막힘)만

## 하지 말 것 (잠근 것)
- 코스·길·지형, 차 조작값(ArcadeCarController·DriveFeel), 화물·돈 핵심 수치, 도현이 놓은 건물 위치
- 정한 값(내부 해상도 360·Nearest 등), 손에 든 종이(HeldPaper)·방향 표시·[E] 칩 방식

## 시작 장면
- 씬 Map_TownV6 → Play → 디버그 창 "택배 보관소 칠판"
- 도현이 처음 할 행동: 칠판 쪽지를 E로 집는다

## 수용 기준
1. 도현이 칠판 쪽지를 집으면 손에 전표가 들리고, 작업대 쪽이 [E]로 보인다(다음 할 일을 화면이 알려 줌).
2. 도현이 포장을 마치고 상자를 들고 카트로 가면 상자가 짐칸에 실리고 방향 표시가 찻집(줄리아)을 가리킨다.
3. 도현이 찻집 문 앞에서 E 를 누르면 건네기 장면이 나오고 배달 끝 상태가 된다 — 중간에 막혀 다음 할 일을 모르는 곳 0.

## 기계 확인
- 하네스 증거 묶음 `bash tools/evidence/bundle.sh <ID> after`: 프레임 증가(frames_ok), 클립 동안 에러 0(log_ok) — 자동
- 이 기능이 안 됐으면 실패했을 검사: exec 로 배달 단계 값이 쪽지→포장→실음→도착→건넴 순으로 바뀌는지, 각 단계 사이 순간이동 없이

## 사람 확인 (도현 한 판)
- Play → 칠판 쪽지 집기 → 포장 → 카트로 찻집 → 건네기
- 질문: 다음에 뭘 해야 할지 헷갈린 곳이 있었나?



## 끝 조건
- 도현이 「사람 확인」을 해 보고 통과라고 함 → (아트 →) 통합 → 검증 PASS → 완료.

## 기록 (최근 3줄)
- 2026-11-03T13:05 build→verify · 그레이박스 → 검증 · 증거 묶음 기계 PASS(프레임 +809, 경고 0, 에러 0) → 블라인드 검증
- 2026-11-03T13:07 verify→dohyun · 검증 → 막힘 · 검증 FAIL — 기준 2: 상자는 카트에 실리지만 방향 표시가 찻집을 가리키는지 증거에서 확인 안 됨(깃발 표지는 4칸에만, 5~8칸엔 없음) | 3회 규칙: 3번 불통과 → 멈춤, 도현 2지선다
- 2026-11-03T14:05 pd→build · 막힘 → 그레이박스 · PD: 3회 불통과 원인은 증거 방식(10초 클립)이었음 — 기준별 critN.png 로 바꿔 재투입(D-277)

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
생성 2026-11-03 15:16 · 커밋 47550034 · 브랜치 main

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
- **F1/Assets/Scripts/OnFoot/OnFootDirector.cs** (1648줄): class OnFootDirector:14 · enum Mode:16 · SimInteract():303 · GoToSpot():600 · ForceTalk():819 · SimCalendarClose():904 · SimDiaryClose():905 · SimBoardMove():1032 · …+12
  참조: AmbienceProbe.cs, ArcadeCarController.cs, AudioCueBus.cs, DayLog.cs +29
- **F1/Assets/Scripts/OnFoot/DepotOrderBoard.cs** (198줄): class DepotOrderBoard:10 · struct Line:23 · IsOpenLine():32 · SetLines():38 · NearestNote():78 · PickNote():91 · NoteChipAnchor():100 · Move():103 · …+3
  참조: BuildFontsD121.cs, DepotOrderSetup.cs, DotScreenUI.Texture.cs, FishingSpot.cs +7
- **F1/Assets/Scripts/Packing/Bench/BubbleSheet.cs** (339줄): class BubbleSheet:13 · Create():34 · Pull():49 · Tear():71 · Grab():95 · Hold():113 · Release():120 · Wake():306 · …+1
  참조: DepotPackSpot.cs, PackBench.Qa2.cs, PackBench.cs, PackItem.cs
- **F1/Assets/Scripts/Packing/Bench/PackBench.cs (+PackBench.Day.cs, PackBench.Qa2.cs)** (1308줄): class PackBench[cs]:22 · enum Tool[cs]:79 · SimWheel()[cs]:170 · FindWrapPair()[cs]:450 · Wrap()[cs]:467 · SpawnPaper()[cs]:559 · SpawnBubble()[cs]:576 · Setup()[cs]:605 · …+16
  참조: BubbleSheet.cs, DepotPackSpot.cs, OnFootDirector.cs, OnFootRouteBot.cs +5
- **F1/Assets/Scripts/Packing/Bench/PackBenchCursor.cs** (171줄): class PackBenchCursor:13
  참조: DepotPackSpot.cs, PackBench.cs, PackBenchBuilder.cs
- **F1/Assets/Scripts/Packing/Bench/PackBox.cs** (536줄): class PackBox:17 · Build():74 · TryClose():144 · Open():158 · SnapLid():166 · ResetForNextDay():169 · SeamParam():180 · AddTape():189 · …+12
  참조: BubbleSheet.cs, DepotPackSpot.cs, PackBench.cs, PackBenchBuilder.cs +1
- **F1/Assets/Scripts/Packing/Bench/PackItem.cs** (106줄): enum PackItemKind:6 · enum PackSupplyKind:7 · class PackItem:14 · Wrap():81 · class PackSupply:101
  참조: BubbleSheet.cs, DepotPackSpot.cs, PackBench.Qa2.cs, PackBench.cs +2

## 에디터 명령 치트
- 실행: `bash scripts/ucli-http.sh exec 'return DebugWindow.GoPlace(12);'` (긴 C# 는 파일로 `exec @docs/team/cards/<ID>/start.cs`; `Object` 는 `UnityEngine.Object` 로 완전 한정)
- Play: `bash scripts/ucli-http.sh editor play --wait` → 바로 `exec 'Application.runInBackground = true; return "ok";'` → 끝에 `editor stop`
- 콘솔: `bash scripts/ucli-http.sh console --type error` (에러 0 이어야 끝)
- 순간이동: `DebugWindow.GoPlace(n)` — n 은 아래 세계 스냅숏 「장소」 표 순서(0부터). 카트는 장소의 카트 규칙대로 같이 옮겨진다
- 캡처: Play 중 `ScreenCapture.CaptureScreenshot("docs/qa/tmp/<이름>.png")` 1~2장, 또는 tools/evidence/clip.cs.tmpl 방식(LowResRenderTest.Present → RenderTexture → PNG = 도현이 보는 화면)
