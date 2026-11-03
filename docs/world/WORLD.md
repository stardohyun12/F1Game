<!-- 자동 생성: scripts/world_snapshot.sh — 손으로 고치지 말 것 -->
생성 2026-11-03 13:07 · 커밋 f3949a5e · 브랜치 main

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
| 항구 | (272.2, 1.3, -234.7) | 180 | Near | y 1.30 (Pier) Δ+0.00 | 동쪽 부두 갑판(Pier 272,-235), 바다를 봄 |
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
- 부두 난간 PierRails: 자식 콜라이더 43 · WalkOnlyRail 있음
- HeldPaper(손 전표·칠판 쪽지): 0
- NavMesh 정점: 0 (없음 — 땅 검사는 레이캐스트로)
