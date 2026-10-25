# D141 길가 생활 소품2

## 목적·출력

도로 가장자리에 주차·장사·세탁·관리 흔적을 남기는 정적 소품. 실제 브랜드·AI 이미지 없음. `bayloop_props2.py`가 geometry 부품 두 모듈을 조립해 FBX/GLB/blend/geometry JSON/manifest와 실제 960×540 front·side·top 렌더를 낸다. 기존 bicycle은 재제작하지 않는다. 선택 항목은 mailbox_cluster로 결정했다.

요청 수량은 19개: parked_car3, three_wheeler1, scooter2, laundry_line1, traffic_mirror1, street_sign3, crosswalk_paint1, road_patch2, shop_display1, cafe_set1, parasol1, mailbox_cluster1, garage_door_inset1.

## 규격

- 각≤1500tris. 세계32px/m. 공용 pitcrew_d123_32 팔레트. 공유1024² 사진 아틀라스6장(원본/차량·차고/표지/가구·모터/노면/리넨 기술수정; 납품은 통과품이 쓰는 것만), 셀256²·안쪽252px·테두리2px.
- 피벗=**자산 전체 bounds의 바닥 가운데**, 앞+Z, Y-up, 1unit1m. 건물의 바닥 앞면 피벗과 다르다. 벽 부착도자산 바닥 가운데이며 설치 높이·벽면 위치는 manifest.design 참고.
- 빨래 밑단은 지상2.7m 이상 권장, 우체통 바닥1.1m. 차고 insert는 실제 개구부에 설치하며 솔리드 벽에 붙이지 않는다. decal 메시 충돌 없음, 도로 표면에서 crosswalk2mm/patch2mm 위로 놓는다. 기울어진 도로에는 별도 순응/분할 필요.
- 탈것은 주차용 정적 메시이며 바퀴 회전·조향·운전 리그 없음. 표지판은 무문자 기호만, 실제 교통법규 인증 아님. 거울은 고정된 사진 명암/기호적 하늘·지면 표현이며 실시간 반사 없음.
- 원본 albedo 팔레트검사와 최종 조명 색수는 다른 검사. 실제렌더에는 조명·AA 색이 추가된다.
- 색 seed: `--seed 정수`. 탈것 body는 팔레트 안 3셀을 seed%3으로 선택, 명시 seed는 기타 도장 teal 셀도 green/blue/ochre로 바꾼다. 노면 백색·표지 기호·목재/금속 등 기능 재료는 유지한다. 변형 번호는 실루엣, seed는색 선택이며 seed가 모든 형상을 무작위로 바꾸지 않는다.

## 재생성·검사

```powershell
py -3.13 tools/asset-gen/bayloop_props2_textures.py
py -3.13 tools/asset-gen/bayloop_props2_vehicle_texture.py
py -3.13 tools/asset-gen/bayloop_props2_road_texture.py
py -3.13 tools/asset-gen/bayloop_props2_furniture_texture.py
py -3.13 tools/asset-gen/bayloop_props2_paving_texture.py
py -3.13 tools/asset-gen/bayloop_props2_linen_fix.py
& D:/Tools/Blender/blender.exe -b -t 8 --python-exit-code 1 --python tools/asset-gen/bayloop_props2.py -- --group vehicles
& D:/Tools/Blender/blender.exe -b -t 8 --python-exit-code 1 --python tools/asset-gen/bayloop_props2.py -- --group street
py -3.13 tools/asset-gen/bayloop_props2_prepare.py
py -3.13 tools/asset-gen/check_props2.py --out art/_review/2026-10-25-bayloop-props2/glb-checks.json
& D:/Tools/Blender/blender.exe -b -t 4 --python-exit-code 1 --python tools/asset-gen/check_props2_fbx.py -- --out art/_review/2026-10-25-bayloop-props2/fbx-checks.json
```

`--ids parked_car_01`처럼 일부만 재생성 가능. `--geometry-only`는 geometry·UV 해시 비교용이며 납품물을 쓰지 않는다. 원본 재생성은 F1 복사를 수행하지 않는다. 실제 투입은 현재 검사 SHA와 독립 점수≥7을 확인한 handoff만 수행.

## 참고·한계

SPEC-vehicles.md의 공식 제원·비례 참고, SPEC-street.md의 실제 구조, SPEC-checker.md의 검사 정책 참조. Blender 실제 mesh 비교는 Unity의 임포트·프리팹·Point 설정·콜라이더·주행 가독성·성능을 검증한 것이 아니다. 프로그래머가 Point/sRGB/압축없음/1024·재질 BayLoop_Props2_Photo32 연결을 확인한다. 최종 도현 승인은 인게임 캡처로 필요.


### 검사 중 수정
리넨9×9 축소 사진에 실제 RGB 두 값만 남았고 >=median 분류가 한 색으로 붕괴했다. 중간8bit 반올림을 없애고 float luminance BOX 축소 후 strict >median으로 사진의 소수 밝기 변화를 보존했다. 노이즈 추가 없이 사진 신호를 팔레트19/14로 재매핑. 이미 통과한 다른 소품의 PNG를 바꾸지 않으려고 파라솔만 별도LinenAtlas로 고정했다. 총6장인 현 버전은 재질 일괄 재검증 후 통합할 여지가 있다.
