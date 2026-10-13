# 따뜻한 로파이 해안 에셋 — 실제 제작 1차

사용자가 선택한 02 따뜻한 로파이 / 05 페인터리 이미지를 기준으로 만든 **실제 3D 에셋 파일과 Unity 검토 씬**. 이 폴더의 번호 붙은 PNG는 모두 Unity Camera.Render 결과이며 image_gen 생성 이미지가 아니다. 최종 아트 확정 또는 전체 게임 적용 완료를 뜻하지 않는다.

## 선택한 제작 방법

- 실루엣·문·차양·창틀·지붕은 단순한 메시. 물품 진열·간판·얼룩은 공용 텍스처로 표현.
- 512×512 공용 아틀라스, 64×64 소재 칸, Point 샘플링. 벽·길의 UV를 약 2m 단위로 나눠 무늬가 늘어나는 현상을 줄임.
- 독립적인 픽셀 노이즈 대신 낮은 대비의 색 덩어리를 드문드문 배치. 간판은 직접 정의한 픽셀 글자, 선반은 직접 배치한 색 면. 그림을 생성해 메시로 변환하는 방식은 사용하지 않음.
- Blender Python으로 재생성 가능한 원본 형상/텍스처를 만들고 FBX로 내보낸다. Unity 프리팹은 이 FBX를 참조하며 모두 같은 소재를 사용.
- 그림의 분위기는 낮은 해상도만으로 만들지 않음. 베이지 벽, 청록 간판, 어두운 창, 따뜻한 실내색, 안개와 약한 실제 방향광 그림자를 조합.
- 한 에셋의 장식보다 재사용 가능한 일관된 크기·색·표면 규칙을 먼저 검증.

## 참고 / 재사용 / 변경 구분

1. [Easy Delivery Co. 개발자 인터뷰](https://80.lv/articles/inside-the-making-of-easy-delivery-co): 개발자 설명으로 확인한 것은 저폴리+텍스처 중심 작업, Blockbench/Aseprite/Unity, 주변색과 안개 중심 조명 및 456×256 기준이다. 원본 코드·모델·텍스처에는 접근하지 않았다.
2. [Blockbench 공식](https://www.blockbench.net/): 모델 위 직접 페인팅과 UV/텍스처 편집 기능을 확인. 이 작업은 이미 동작하는 Blender 제작 경로를 사용했다.
3. 저장소 기존 Blender→FBX 제작·Unity 캡처 방법과 race_v2 차량을 재사용. 차량은 이번 신규 에셋 12종 수에 포함하지 않는다.
4. **원작 전체 재현 아님**: 00_ambient_456.png는 공개된 해상도와 주변광 원리를 새 자체 에셋에 적용한 기준 실험. 원작과 동일 조건의 충실도 비교라고 주장하지 않는다. 01과 03은 동일 카메라·640×360에서 주변광 / 방향광 적용 차이 비교.
5. 사용자 상황에 맞춘 변경: 사진 텍스처 대신 자체 코드로 만든 색면/픽셀 텍스처, 겨울·음산함 대신 온화한 해안 색, 원작의 무그림자 방식 대신 약한 햇빛·그림자 추가. 최종 그림체는 사용자 판정이 필요하다.

## 산출물

- Unity: `F1/Assets/Art/LofiPilot/LofiPilot.unity` — 별도 검토 씬, 주행 게임 로직을 넣은 스테이지가 아님.
- 에셋: 같은 폴더의 12개 FBX + prefab, 공용 LofiAtlas.png / LofiShared.mat / LofiSurface.shader.
- 에셋 종류: CornerShop, RepairGarage, CoastalHouse, Wall3m, CoastalTree, UtilityPole, TireStack, Crate, Parcel, DeliveryBag, Road7x8, Sidewalk2x4.
- Blender 원본: `LofiCoastalKit.blend`. 에셋은 검토를 위해 떨어뜨려 놓았으며 각각의 로컬 원점은 바닥 기준. 수동 FBX 재수출 시 원점과 위치를 확인.
- 형상/텍스처 생성기: `tools/asset-gen/lofi_pilot.py`.
- Unity 조립/검사/렌더: `F1/Assets/Editor/LofiPilotBuilder.cs`.
- 배치 원본: `F1/Assets/Art/LofiPilot/layout.json` → LofiPilotLayout.asset. 검토 배치를 ScriptableObject에 저장하며 런타임 TrackLayout/roadMask는 바꾸지 않음.
- `*.mesh.json`: Blender 형상의 독립적인 치수·삼각형·UV 검사 자료. 실제 프리팹은 JSON으로 재구성한 메시가 아닌 Unity FBX importer 결과를 사용.

## 실제 검사 결과

세부 수치: [unity-verification.json](unity-verification.json).

- 12개 FBX의 Unity 크기와 원본 형상 크기 일치, 모든 UV 범위 유효.
- 60개 에셋 배치의 바닥 높이와 X/Z 기울기 검사.
- 27개 도로 이음매 샘플 위치에서 하향 raycast 충돌 높이 0 확인.
- Unity C# 컴파일 실패 없음 및 커스텀 셰이더 컴파일 오류 없음 확인.
- 톱다운 / 사이드 / 간판 근접 / 도로 경계 확대 / 기존 건물 대조 / 소품 근접 실제 렌더를 열어 검사.
- 원래 열려 있던 Assets/ComplexTrack.unity는 작업 후 동일하게 유지되고 dirty=False 확인.
- 실제 운전 감각, 목표 기기 FPS, 완성된 마을의 가독성은 이번 에셋 검증 범위에서 시험하지 않음.

## 캡처 읽는 법

- 00: 주변광 원리 + 456×256 조건 실험.
- 01 / 03: 동일 640×360에서 기본 주변광 / 따뜻한 방향광 비교.
- 02: 960×540 주행 카메라 구도. 실제 주행 중 캡처가 아니라 검토용 카메라 렌더.
- 04: 정비소, 05: 톱다운, 06: 사이드, 07: 상점 근접, 08: 도로·보도 이음매.
- 09: 왼쪽 기존 무텍스처 harbor_0, 오른쪽 신규 상점. 높이를 6.8m로 맞췄고 동일 카메라와 동일 셰이더 사용. 모델/층수/가로폭이 달라 텍스처만의 A/B 비교는 아님.
- 10: 전체 키트, 11: 작은 소품 근접.

## 재생성

저장소 루트에서 PowerShell:

```powershell
& 'D:/Tools/Blender/blender.exe' -b -P tools/asset-gen/lofi_pilot.py -- --root D:/Github/F1Game
& ./tools/asset-gen/lofi_unity.ps1 -Code 'UnityEditor.AssetDatabase.Refresh(); return "refresh";'
# Unity의 스크립트 컴파일이 끝난 뒤
& ./tools/asset-gen/lofi_unity.ps1 -Code 'LofiPilotBuilder.BuildAndCapture(); return "done";'
```

또는 Unity 메뉴 `PitCrew > Art > Lofi Pilot > Build and Capture`. 별도 씬에 조립하고 저장/캡처한 뒤 기존 씬으로 복귀. Play 모드에서는 실행을 거부한다. 생성된 키트와 검토 씬은 재생성 때 갱신되므로 수작업 변형은 별도 복사본에 보관.

## 비용 / 권리 / 호환성

새 유료 도구·패키지·서비스·외부 다운로드 없음. 텍스처/모델은 자체 제작이며 타 게임 이미지에서 추출하지 않았다. Blender는 제작 도구로만 사용했고 결과물에 다른 게임 에셋 라이선스는 섞이지 않는다. 기존 race_v2와 이전 harbor_0는 프로젝트 안의 기존 파일을 참조한다. 실제 확인한 환경은 Blender 5.2.2 / Unity 6000.3.19f1 / URP 17.3.0 Windows. 커스텀 셰이더는 URP 전용이며 다른 렌더 파이프라인은 확인하지 않았다.

## 판단 및 다음 개선점

이 방식은 바닥 원점·공통 소재·텍스처 비율을 통일해 에셋을 늘리는 데 적합하다. 다만 현재 색면 무늬가 주기적으로 반복되고, 건물은 상자 형태가 강하며, 검토 거리는 평지 직선이라 선택한 컨셉의 경사진 생활 공간보다 단순하다. 이것을 최종 룩 완성으로 취급하지 않는다. 다음 시각 개선의 우선순위는 (1) 벽 무늬 반복을 줄이는 변형 타일, (2) 창문/문 배치 변형, (3) 곡선·경사와 배경 실루엣을 갖춘 실제 맵 일부 적용이다. 화면 전체에 강한 픽셀 필터를 더하는 것을 해결책으로 삼지 않는다.

