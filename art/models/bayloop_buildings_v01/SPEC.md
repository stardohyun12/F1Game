# BayLoop 건물 — 재현·시험 인계 규격

계약 원문은 `docs/map/building-types.md`. 모델은 해당 범위의 **설계 치수**로 제작했으며 사진 속 건물을 측량하거나 복원한 것은 아니다. 실제 사진의 상대 비례·처마·테라스·적재문을 관찰해 적용했다. 사진 출처/저자/라이선스는 `art/_review/2026-10-25-bayloop-buildings/references/REFERENCES.md`.

| type | a 본체 폭×깊이 / 층 | b 본체 폭×깊이 / 층 | 변형 |
|---|---|---|---|
| shop_house | 5.5×8m / 2층 | 6.75×8m / 2층 | 좌우 출입구·진열 폭·처마 깊이·보조 차양·벤치 위치 |
| terrace_house | 5.5×6m / 2층 | 6.75×6m / 3층 | 상부 후퇴·층수·옆 계단과 2.8m 출입구·상부 발코니 살림 |
| harbor_warehouse | 9×8.5m / 1층 | 11×9.5m / 1층 | 지붕 높이·적재 셔터1/2개·차양 폭 |

**피벗은 본체 전면 벽의 바닥 가운데, Unity Y=0, 앞면+Z, 1단위=1m.** 처마·문턱·의자·외부 계단은 본체 footprint 밖으로 나온다. 실제 배치 여유는 `manifest.json`의 bounds_min/max·dimensions_m를 사용하고, 필지 기본 크기는 footprint_m를 사용한다. 외부 계단은 건물 부속이며 지형 전체 기단·옹벽은 넣지 않았다. 지형 기단·계단 하부 지지·길 연결은 빌더 담당이다.

## 재사용한 검증 방법

- `lofi_pilot.py`: 실측 primitive 조립, Blender(x,y,z)→Unity(x,z,-y), FBX 내보내기 규칙, 기존 키트 비교. 기존 무작위 노이즈 아틀라스는 새 모델에 복사하지 않았다.
- `art/models/hill_house_v01/generate.py`: 평면의 정규직교 축에서 폴리곤을 잘라 UV를 실제 길이에 매핑하는 방법. 이번은1024²/256px 칸/2px 여백/252px 유효범위로 적용, 모든 삼각형32px/m.
- A1 2차: 사진 기반 목재·회벽·기와·돌포장 재사용. 64px/m 자료를 BOX로32px/m로 줄이고 공용 팔레트로 재양자화했다. 회벽은 먼저 양자화된 단색을 다시 축소하지 않고 A1의 양자화 전 사진 축소본에서 시작한다.
- 유리처럼 읽히는 면은 투명 셰이더가 아니라 사진 기반 직물의 어두운 창/커튼 표현이다. 실제 투명 유리·실내 공간·문 개폐를 제공하지 않는다.

## 질감 출처와 가공

`texture-manifest.json`은 칸별 원본 경로/URL/실측/팔레트 사다리를 기록한다. 기와는 별도 촬영 `grey_roof_tiles`와 A1 `ceramic_roof_01`, 금속은 `rusty_painted_metal`, 직물은 `fabric_pattern_05`, 기단은 `stone_brick_wall_001` 사진을 사용했다. 공급자 API 원문·직접 다운로드 URL·SHA/MD5는 `manifest.json`의 source_manifests와 그 원본 폴더에 남긴다.

모든 질감 원천은 Poly Haven CC0 실제 diffuse 사진이다. [라이선스](https://polyhaven.com/license), [사진 기반 제작 FAQ](https://docs.polyhaven.com/en/faq). 공급자 사진 제작 설명을 확인했으며 촬영지·개별 촬영 하위기법·실제 수리 이력을 입증했다고 주장하지 않는다. 보충 금속 실측2.2m를70px로 반올림한 사진 내부 밀도는31.82px/m로 허용오차 이내, 기하 UV 밀도는32px/m다.

`bayloop_textures.py`는 사진 밝기를 기존 팔레트 사다리로 대응시킨다. 무작위 얼룩·AI 생성·인페인팅은0. 층층집은 같은 사진의 가까운 명도 흰색 사다리(19/14), 청회색 창(7/18), 직물(19/14)을 별도 atlas로 저장했다. 공용 팔레트 자체는 변경하지 않았다. 간판은 Pretendard Regular(프로젝트 기존 OFL 폰트)로 그렸다. 문자열은 `차 · TEA`와 `창고 · 3`이다.

**32색 검사는 조명 전 albedo 기준.** 실제 렌더의 조명·그림자·안티앨리어싱은 별도다. 검토판은 실제 bpy 렌더, Commons 실물 사진은 비례 참고용으로만 사용. AI 기준 원화/배경은 검토판에 사용하지 않았다. 중간 photo tile은 물리 치수 때문에70/80/96px일 수 있으나 납품 atlas는1024² POT다.

## 재현 명령

```powershell
py -3.13 tools/asset-gen/bayloop_textures.py
& D:/Tools/Blender/blender.exe -b -t 8 --python tools/asset-gen/bayloop_buildings.py -- --type shop_house
& D:/Tools/Blender/blender.exe -b -t 8 --python tools/asset-gen/bayloop_buildings.py -- --type terrace_house
& D:/Tools/Blender/blender.exe -b -t 8 --python tools/asset-gen/bayloop_buildings.py -- --type harbor_warehouse
py -3.13 tools/asset-gen/check_asset.py --building-manifest art/models/bayloop_buildings_v01/manifest.json --out art/_review/2026-10-25-bayloop-buildings/checks.json
& D:/Tools/Blender/blender.exe -b --python tools/asset-gen/check_bayloop_fbx.py -- --out art/_review/2026-10-25-bayloop-buildings/fbx-checks.json
```

재생성은 art/까지만 한다. F1 시험 복사는 게이트·최종 SHA 확인 뒤 별도 인계 스크립트로 수행한다. `check_buildings.py`는 실제 GLB 전체 삼각형 양축 밀도·tris·Point·내장 이미지·바닥·전면·장부를 검사한다. 0.04m² 이상 삼각형은 실제 UV 내부에서2색 이상이어야 하며, 작은 부재는 별도로 보고한다. 작은 부재도 밀도 검사에서 제외하지 않는다. `check_bayloop_fbx.py`는 임시 디렉터리에 복사한 FBX를 실제 재임포트하여 GLB와 정점·bounds·삼각형 수·단위를 대조하고 내장 PNG의 SHA를 확인한다.

## 프로그래밍 인계

FBX 이름 그대로 `<type>_<a|b>.prefab`으로 프리팹화. 루트 scale1/rotation0 유지, 건물 앞 +Z를 길 쪽으로 돌린다. Point·무압축·sRGB·원본1024 크기 확인. PNG/FBX만으로 Unity Point 설정이 보장되지 않는다. 측정용 GLB의 Nearest와 Unity 임포트는 별개다. 충돌·LOD·배달 지점·문 상호작용·게임 내32px/m·960×540 가독성·씬 배치는 이번 작업에서 검증하지 않았다.

## 승인 판 보존

각 type의 .blend 및 변형별 .geometry.json은 해당 게이트 판의 실제 모델을 보존한다. 상점 통과 뒤 공용 shell의 벽 모서리 겹침을 해소했으므로 현재 생성기를 상점에 재실행하면 모서리 내부 형상이 달라질 수 있다. 이번 납품 상점은 게이트를 받은 저장본을 사용했다.
