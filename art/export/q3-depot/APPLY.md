# Q3 택배 보관소 — 도현 승인 필요

`q3_depot.fbx`의 루트 `Q3_Depot`: 바닥 앞면 가운데 (0,0,0), 정면 Unity +Z, 1u=1m. 본체 10×7×4m. 손수레·외부 상자·포장대 때문에 전체 외곽 깊이는 본체보다 크다. 각각 이름으로 분리되어 있으므로 콜라이더는 가시 메시 전체 볼록 껍질 하나로 만들지 않는다. 셔터 아래와 접수창 개구부를 막지 않도록 벽·소품 단위 콜라이더가 필요하다.

## 배치

- 지정 슬롯 중심: Q3_Depot `(12.8,7.4,-90.3)`.
- 피벗 규약이 슬롯 중심과 다르므로 임포트 시 그대로 대입하지 않는다. yaw=0에서 본체 수평 중심을 슬롯에 맞추는 앞면 피벗 후보는 `(12.8,6.2,-86.8)`이다. y=6.2는 기존 포장 트리거 높이 기준의 제안이며 실제 지형 높이는 QA 확인 필요.
- 기존 포장 트리거 `(13.8,6.2,-83.4)`는 위 후보 기준 로컬 `(1,0,3.4)`. 중앙 폭 5.2m, 정면에서 4m 주차 동선 안에 들어간다. 전면 벽의 얕은 몰딩은 z≈0.26m 이내라, 실제 주차 여유 시작은 z=0.31m로 검사했다. 외부 포장대는 로컬 x=3.75, z=1.3; 손수레·상자는 왼쪽 가장자리다.
- Unity 에디터·실제 지형·충돌·트리거 연동은 실행하지 않았다. **QA/프로그래밍 임포트 필요. 승인 전 F1/Assets에 넣지 않는다.**

## 공용 재질

5개 PNG는 `art/export/kit-shared-v2/`와 SHA256 동일한 납품용 사본이다. 지붕·데칼은 v1부터 보존된 원본이다. 임포트 후 기존 공유 재질에 연결하면 중복 텍스처를 만들 필요 없다.

|슬롯|파일|용도|
|---|---|---|
|Shared_wall|wall.png|회벽, 상자 재료 및 밝은 포장 띠|
|Shared_trim|trim.png|창틀·문·셔터·금속·선반·간판 메시 글자|
|Shared_floor|floor.png|실내 콘크리트 바닥·손수레 바퀴|
|Shared_roof|roof.png|E2 기와|
|Shared_decals|decals.png|기존 빈 간판·전단·번지·국소 칠 벗겨짐|

BaseMap만 연결, sRGB, Point, mipmap/압축 없음. 재질색 white, `_Sat`≤1.2. 데칼만 알파 블렌드. AO·emission·구운 그림자 맵 없음. 공용 키트 `APPLY.md`의 atlas 칸 안 반복 규칙을 유지한다. 기본 UV 32px/m. 작은 공지·번지 등은 기존 칸 전체를 사용하며 글씨 읽힘을 위해 개별 크기로 배치했다. 간판 `BAY PARCEL`은 Blender 기본 글꼴을 변환한 평면 메시로, 기존 trim의 어두운 색 칸만 참조한다. 새 이미지 텍스처가 아니다.

상자에 별도 골판지 텍스처를 추가하지 않았다. 공용 회벽의 살구 칸과 크림 칸으로 포장지·테이프를 표현한 제약을 미리보기에서 확인해야 한다.

## 재현·검사

```powershell
& D:/Tools/Blender/blender.exe -b -t 6 --python art/source/q3-depot/assemble.py
& D:/Tools/Blender/blender.exe -b -t 6 --python art/source/q3-depot/optimize.py
py -3.13 art/source/q3-depot/package.py 2
& D:/Tools/Blender/blender.exe -b -t 6 --python art/source/q3-depot/verify.py
```

Blender 파일은 `art/source/q3-depot/depot.blend`. 프리뷰 땅·해·하늘·카메라는 FBX에 내보내지 않는다. FBX 표면에는 albedo 재질만 저장한다. UI 스프라이트 슬롯은 해당 없음.

?? ???: ?? ? 54.1121% ??, ? ?? 1.35199 > 1.25 ???. ?? ?? 6??6??? ?? ??. ?? ? ? ?? ??.
