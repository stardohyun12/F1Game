# 포장 재료 후보 — D-259 / 2026-11-02

**도현 승인 필요. F1/Assets 미투입.** D-256·259의 선제작/일괄 확인 범위로 만든 원화+로우폴리 후보이며, 원화·미리보기 승인이나 게임 적용 승인을 받았다는 뜻이 아니다.

`../../_review/2026-11-02-packing-materials/index.html`에서 12개 모델을 비교한다. `packing_*_v01.fbx`가 전달용 원본이고, `.blend`는 열림/전시 자세와 근사 조명을 포함한다. `.blend`의 `PREVIEW_*`, 조명, 카메라는 FBX에 없다. Blender bpy로 직접 작성했으며 생성형 이미지·3D 도구를 쓰지 않았다.

## 기존 시제품에 맞춘 크기

모든 표는 Unity X(폭)×Y(높이)×Z(깊이), m. Unity 실행 없이 `PackBenchBuilder`, `PackBox`, `PackBench`, `PackItem`, `BubbleSheet` 코드와 programmer-20261102-1153/1442 보고를 정적 확인했다.

| 모델 | 치수 / 피벗 | 연결 주의 |
|---|---|---|
| box_s | 내경 .320×.100×.320 / 닫힘 외경 .344×.124×.344 / 바닥 중앙 | 시각 벽 .012, 바닥 .012, 뚜껑 총 .012 |
| box_m | 내경 .315×.150×.315 / 닫힘 외경 .339×.174×.339 / 바닥 중앙 | S보다 평면이 약간 작음: 실제 코드 유지 |
| box_l | 내경 .500×.300×.500 / 닫힘 외경 .524×.324×.524 / 바닥 중앙 | 충돌 벽 .050은 외부로 확장된 기존 규칙 유지 |
| bubble_roll | Ø.160×축 길이.320 / 중심 / 축=Unity 로컬Y | 기존 scale(.16,.16,.16), X90°. m 모델을 그대로 해당 루트에 넣으면 이중 스케일 |
| bubble_sheet | 폭 .300, 참고 길이 .600 / 중심 참고본 | **실제 동적 메시 교체 금지**. 게임 길이 .100~1.000, 충돌 두께 .008 그대로 |
| newspaper_stack | .300×.060×.220 / 중심 | 기존 Y8° 유지. 별도 텍스트·실제 기사·로고 없음 |
| newspaper_ball_a/b | **단위 지름1 / 중심** | 기존 루트가 지름 .110→.048로 변함. FBX를 추가로 .110 축소하면 이중 스케일. blend/렌더만 .110 전시 |
| egg_tray | .300×.065×.300 / 콜라이더 중심 | 판+5×6=30알. 달걀 높이만 .050→.049로 줄여 현재 콜라이더 상단 안에 맞춤 |
| tape_roll | Ø.100×높이.025 / 중심 / 축Y | 기존 scale(.100,.0125,.100). 단일 렌더 메시 |
| label_stack | .100×.020×.070 / 중심 | 기존 Y−6°. 단일 렌더 메시. 공급용 |
| address_label | .100×.002×.070 / 중심 | 부착용. 무문자 미색, 주소 데이터는 기존 게임 UI가 담당 |

`adapters/`에는 bubble_roll, newspaper_stack, tape_roll, label_stack, address_label 5개 정규화 FBX가 있다. 기존 primitive의 크기 배율을 유지한 채 연결할 용도다. 실크기 원본과 이 어댑터를 혼용하지 않는다. 테이프·라벨은 `TakeTool()`이 첫 Renderer의 GameObject만 복제하므로 여러 부품을 하나의 메시로 합쳤다. 축/임포터 변환을 포함한 실제 MeshFilter 연결은 **QA/프로그래밍 임포트 필요**이며 Blender 왕복 검사가 Unity 로컬축 검증을 대체하지 않는다.

신문공은 원점 반경 .5 구 안에 들어가는 각진 주름 두 가지다. 기존 구김 시작 스케일(.440,.0275,.440), 끝 지름 .048 양쪽을 계산 검증했다. 기존 SphereCollider·물리·구김 함수를 수정하지 않았다.

## 상자 날개 연결 — 별도 프로그래밍 필요

`packing_box_*_v01` 아래 `HINGE_Left/Right/Front/Back` 각각에 `Flap_*` 메시 한 개가 붙는다. FBX 정지 자세는 **닫힘(회전0)**. minor 좌우 날개 중심은 입구+.003m, major 앞뒤는 입구+.009m라 닫힘 총두께 .012m다. 중앙에는 1mm 이음이 남는다.

| Blender 힌지 | 축 | 열림 회전 | 닫기 순서 |
|---|---|---|---|
| Left | Y | −112° | 먼저 |
| Right | Y | +112° | 먼저 |
| Front | X | +112° | 나중 |
| Back | X | −112° | 나중 |

Blender 좌표는 X폭/Y깊이/Z높이, Unity 변환 뒤 로컬 회전축은 임포트 결과에 맞춰 확인한다. 위 값은 Blender 전시 자세이며 **Unity Euler 값을 그대로 대입하는 지시가 아니다**. 현재 `Shell/LidPivot`은 한 장을 X 0→112°/0.35초로 움직인다. 기존 충돌용 뚜껑·판정은 유지하고 시각 날개 4개를 별도 동기화해야 한다. 애니메이션 클립/런타임 연결은 제작·검증하지 않았다.

## 뽁뽁이 재질 연결 — 별도 프로그래밍 필요

현재 `BubbleSheet`는 vertices/triangles/normals만 생성하고 UV가 없다. 메시나 충돌을 FBX로 바꾸지 말고, 기존 두 면의 같은 정점 좌표에 동일 UV를 공급한다. 폭 방향 `u=x/.024`, 길이 방향 `v=길이좌표/.024`로 실제 길이에 따라 반복한다. 길이가 늘어나도 알갱이가 늘어나지 않는 방식이다. 양면의 UV를 일치시키고, 거울상/끊김은 실제 구김 중 확인한다.

| 텍스처 | 설정 / 의미 |
|---|---|
| bubble_rgba.png (256²) | sRGB, Repeat, Bilinear, RGB 중성 청록 필름 / A 투명도. 소스 사진의 반사광을 albedo에 복제하지 않음 |
| bubble_normal.png (256²) | Normal map / OpenGL +Y. 두 알갱이의 돔 형태. Unity 임포터의 Y방향·강도 확인 필요 |
| bubble_roughness.png (256²) | Linear 데이터. URP Smoothness에는 1−Roughness 변환 필요 |
| bubble_height.png (256²) | Linear 데이터, 참고용 높이. 실제 변위/충돌에 적용하지 않음 |

Blender ??: IOR1.46, transmission .35, normal strength .7, 사진 기반 미세 roughness. Unity: URP Lit Transparent/양면/Alpha 설정 후보, 정확한 셰이더는 프로그래밍 담당. FBX가 Blender 노드 투과/법선 연결을 동일하게 옮긴다고 가정하지 않는다. 겹침 정렬·깜빡임·테이블 위 식별성은 게임 검증 필요. 사진은 출처의 기하·미세 결만 참고했고 빛·그림자 베이크는 없다.

## 선행 사례와 적용 범위

- [FEFCO 원본 코드 북 p.8](https://www.fefco.org/download/file/fid/3064), [Macfarlane Packaging 공식 상자 가이드](https://www.macfarlanepackaging.com/wp-content/uploads/2023/03/Macfarlane-Packaging-Cardboard-Boxes-Guide-2023-Web-Ready.pdf): 0201의 바깥 날개가 중앙에서 만나는 구조를 확인. 이번에는 게임의 정사각 내경·두꺼운 기존 시각 벽에 맞춰 직접 모델링했으며 제조용 전개도·접착 이음·아래 날개는 재현하지 않았다. **FEFCO 규격 인증 모델이 아니다.**
- [Rawpixel CC0 뽁뽁이](https://www.rawpixel.com/image/6040280/photo-image-texture-public-domain-plastic)와 [연결된 원본](https://free-images.com/display/bubble_wrap_plastic_wrapping.html): 4272×2848 사진을 실제 다운로드. `reference/source_page.html`, `license.json`, 원본 jpg·SHA256 보존. 원본의 반사 명암을 그대로 쓰지 않고 관찰한 셀 윤곽을 수동 해석한 돔/알파와 고주파 roughness로 분리했다. 정확한 사진 형상 역복원이라는 주장은 하지 않는다.
- [ambientCG CardboardSet001](https://ambientcg.com/view?id=CardboardSet001): 기존 라이브러리의 인쇄 없는 종이 부분만 잘라 평균 명암 제거·±6.5% 섬유 결·재색칠. 신문 무늬는 사람이 작성한 막대열을 흐리게 했으며 글자·기사·브랜드를 쓰지 않았다. 모든 출처·파생 파일은 `art/provenance.csv`와 `texture_manifest.json`에 기록한다.
- [Blender 공식 FBX API](https://docs.blender.org/api/5.1/bpy.ops.export_scene.html): 축/단위 옵션을 참조. 단순 1m 큐브의 기준 왕복은 ①에서 재현. ②의 3층 상자 계층은 `bake_space_transform=True`에서 날개가 틀어지는 실패를 실제 발견했고, 문서 기본값 False로 바꿔 왕복 외경 오차 0.001mm 미만 확인. 단일 메시 어댑터에는 True를 사용해 별도 왕복 검사한다.

참고 제품·사진의 결과를 통째로 원본 재현한 것이 아니다. 새 다운로드는 CC0 사진 1장, 실행 환경 추가 설치/유료 사용은 없다. 포장재 기능·물리·Unity 셰이더는 정적 추론이며 런타임 미검증이다.

## 재생성 및 검사

```powershell
python tools/art/bin/packing_materials_prepare.py
& D:/Tools/Blender/blender.exe -b --factory-startup -t 6 -P tools/art/bin/packing_materials_build.py
& D:/Tools/Blender/blender.exe -b --factory-startup -t 4 -P tools/art/bin/packing_materials_verify.py
python tools/art/bin/packing_materials_publish.py
```

Blender 뒤 `-- box_s`처럼 한 개 선택, `-- --no-render`는 전부 geometry만. GPU 렌더를 다른 Blender와 동시에 실행하지 않는다(시험 중 GPU 메모리 오류를 확인; 최종본은 직렬). ①의 직접 작성한 mesh/UV/조명 helper를 재사용하므로 `depot_props_build.py`도 보존해야 한다. 원본 사진이 삭제되면 prepare를 재현할 수 없다.

Scene_A7_Light 색·빛 방향을 읽은 Cycles 근사 조명, 1280×720 원본·640×360 축소본. 실제 Game 화면 전후 비교·드래그·포장·날개 애니메이션·투명 정렬 검증은 **하지 않았다**. UI 스프라이트가 아니므로 UITheme/PackingScreen 슬롯 및 9-slice는 해당 없음.
