# 정비소 q2 수정 0557 — 도현 승인 필요

**기본 텍스처는 빨강 F2 판이다. QA/프로그래밍 임포트 필요.** 이전 납품물은 `v1/`에 보존했다. 같은 이름의 FBX·PNG·fbm 사본을 함께 교체한다. Unity/F1/Assets에는 이번 아트 작업으로 넣지 않았다.

|재질 슬롯|BaseMap 파일|
|---|---|
|Garage_Albedo|garage_baked32.png — 기본 빨강 F2|
|Garage_Sign_Photo|garage_sign.png|
|Garage_Details_Photo|garage_details.png|
|race_photo_0_Photo|garage_memory_1.png|
|race_photo_1_Photo|garage_memory_2.png|
|race_photo_2_Photo|garage_memory_3.png|
|festival_registration_Photo|garage_festival.png|
|마당 dirt 재질|dirt_baked32.png|

`garage_tin_F2_atlas.png`는 기본 `garage_baked32.png`와 바이트 동일하다. F3은 선택적 교체용이며 기본으로 연결하지 않는다. A7 실제 `SA7_garage_baked32`의 BaseMap인 `garage_tin_F2_atlas_clean.png`에서 색 배치를 가져왔고 q2의 깨끗한 천장을 유지했다. 바닥·apron은 원형 얼룩 없는 콘크리트 사진 결, dirt는 바랜 흙색 사진 결 ±3%로 교체했다. 파일명 baked32는 호환성 때문에 유지했으며 빛·AO 굽기는 하지 않았다.

## UV 교체 주의

- 두 FBX 모두 첫 채널 `UVMap`만 납품한다. 건물은 기존 활성 `BakedUV`, 추가 물건은 기존 활성 `UVMap` 좌표를 먼저 단일 이름으로 통일한 뒤 합쳤다. 반복용 SourceUV가 UV0을 차지하던 문제와 새 물건 UV가 다른 채널로 밀리던 문제를 함께 제거했다.
- `garage_v5_combo_pvuv.asset`처럼 UV를 복사한 과거 사본 메시 대신 **새 FBX 메시 자체를 사용**한다. `pv_garage_uv.cs`의 UV0←UV1 패치를 새 FBX에 적용하지 않는다. 새 FBX에는 UV1이 없다.
- FBX 재임포트에서 재질 7개 각각의 UV 좌표 집합이 내보내기 전과 동일함을 검사했다. 기하·원점·2,406 tris 유지. 마당도 형상은 유지하고 첫 UV 채널을 명확히 내보냈다.

모든 PNG sRGB·Point·압축 없음, 재질색 white, `_Sat`≤1.2. BaseMap에만 연결하며 normal/emission/AO 맵은 없다. 실내 추가 물건은 기존 64 texel/m. `ceiling_photo.png`는 원재료/검증용이다. 대회 게시판·레이스 사진은 장식이며 신청 상호작용/NPC는 구현하지 않았다.

검증: `delivery-validation-v2.json`, `uv-fix-v2.json`, `texture-fix-v2.json`. 미리보기 `art/_review/2026-10-28-q2-garage/sheet_v2.png`. Unity에서 원본 A7과 다시 비교하고 마당 채도·게시판 슬롯·UV 사본 제거를 확인해야 한다.
