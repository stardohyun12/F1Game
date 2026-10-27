# 정비소 다듬기 — 도현 승인 필요

QA/프로그래밍 임포트 필요. 승인 전 F1/Assets에 넣지 않음.

- `garage_v5_combo.fbx` + PNG 전부 함께 교체. 건물·기존 소품 형상/원점 유지, 간판 UV 변경/내부 소품 추가.
- 기본 Garage_Albedo → `garage_baked32.png`; Garage_Sign_Photo → `garage_sign.png`; Garage_Details_Photo → `garage_details.png`; 사진/게시판 재질은 같은 이름 PNG. `.fbm`은 참조 사본.
- `dirt_v5_combo.fbx`·`dirt_baked32.png` 변경 없음.
- F2/F3 아틀라스는 Garage_Albedo 슬롯의 선택적 교체용. 기본 미리보기는 현재 garage_baked32 기준이다.
- 모든 PNG sRGB/Point/압축 없음. 실내 새 물건 64 texel/m, 기존 건물 UV 유지. BaseMap/_BaseMap(Base Color)에만 연결. AO·구운 해/하늘빛·normal/emission 맵 없음. 기존 실시간 셰이더 적용, `_Sat` ≤1.2.
- 추가 헬멧·사진·의자·게시판·키 걸이는 정비소 메시 안에 포함. 게임 사장/NPC·대회 신청 상호작용은 제작하지 않음.
- `ceiling_photo.png`는 원재료/검증용이며 실제 지붕색은 garage atlas에 전사됨.
- 표면 재료 원본/좌표/해시는 `art/source/q2-garage/material-sources.json`, 기하는 geometry-validation.json.
