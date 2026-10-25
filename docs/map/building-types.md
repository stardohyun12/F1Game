# 평면도 → 건물 종류 연결표 (PD, 2026-10-25, 가설)

빌더는 필지마다 아래 type 을 정하고 `F1/Assets/Art/BayLoop/Buildings/<type>[_<변형>].prefab` 이 있으면 그걸, 없으면 폴백(LofiPilot 키트)을 쓴다.
아트팀이 프리팹을 넣으면 빌더 재실행만으로 교체된다. 프리팹 규칙: 피벗 = 바닥 앞면 가운데(길 쪽 = +Z), 1단위 = 1m, 바닥 y=0, 기단은 빌더가 만든다.

| type | 어디(구간) | 실물 기준 | 폭×깊이 m · 층 | 폴백 |
|---|---|---|---|---|
| harbor_warehouse | 항구 평지 | 어항 창고·어구 창고, 함석 지붕·셔터 | 8~12×8~10 · 1~2 | RepairGarage, RepairGarageB |
| fish_market | 항구 | 지붕만 있는 위판장·노점 | 10×6 · 1 | Stall |
| shop_house | ① 상점가 | 교토 히가시야마식 2층 상점(처마·격자·차양), 1층 가게 | 5~7×8 · 2 | CornerShop, B, C |
| town_house | ①③ 마을 차길·골목 | 회벽 2~3층 집, 작은 발코니 | 5~6×6 · 2~3 | CoastalHouse A~F |
| terrace_house | ③ 비탈 마을 | 포지타노식 층층 집(평지붕·아치창·파스텔 회벽), 비탈에 계단처럼 | 5~7×6 · 2~4 | CoastalHouse A~F (높이 늘림) |
| cliff_villa | ④ 해안 산 쪽 | 해안 절벽 위 단독 빌라/카페, 테라스 | 7~9×7 · 2 | CoastalHouseE/F |
| farmhouse | ② 우회·밭 | 돌담 농가·헛간 | 7×6 · 1~2 | RepairGarage |
| landmark_* | features 의 landmark | 등대·종탑·크레인·풍차·우체통 등 json 이름 그대로 | 개별 | 블록아웃 |
