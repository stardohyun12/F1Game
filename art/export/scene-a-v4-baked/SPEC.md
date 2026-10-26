# Scene A V4 baked — 프로그래밍 인계

## 배치/연결
FBX는 모두 V5 장면의 같은 월드 원점을 사용한다. 각각 위치(0,0,0)·회전0·스케일1로 조립. Blender Z-up/-Y 변환은 FBX Y-up/-Z 규칙으로 내보냈다. 별도 로컬 카트 피벗 변환을 적용하지 않았다.

| FBX | Unlit BaseMap PNG | tris | 실제 텍셀 밀도 설정 |
|---|---|---:|---:|
| garage_v4_baked.fbx | garage_baked24.png | 1580 | 40/m |
| kart_v4_baked.fbx | kart_baked24.png | 560 | 40/m |
| props_v4_baked.fbx | props_baked24.png | 1340 | 40/m |
| ground_v4_baked.fbx | ground_baked24.png | 2 | 32/m |
| dirt_v4_baked.fbx | dirt_baked24.png | 18 | 32/m |
| quay_v4_baked.fbx | quay_baked24.png | 10 | 32/m |
| sea_v4_baked.fbx | sea_baked24.png | 2 | 0.32/m,800m 원경 예외 |

모든 조명/그림자/AO가 PNG에 포함된다. FBX는 셰이더를 이식하지 않으므로 Unlit에 명시적으로 연결해야 한다. BaseMap=sRGB, Tint=white, Filter=Point, 테스트는 압축 없음. 추가광·추가AO·이중색보정 없이 먼저 비교. PNG 전체 공용24색(디더0)이며 화면 하늘색/색관리까지24색이라는 뜻은 아니다. UV0는 고유 lightmap UV, 원래 알베도 UV는 내보내기 전에 제거했다.

## 형태/재현
원본 opus_garage_kart_v5.blend를 복사해 작업했다. 렌더에 반영되는 기존 modifier/변환만 확정하고 형상·배치 보존을 좌표 집합/삼각형 수로 검사했다. 소스 원본/복사본 SHA는 technical-check.json. bake 대상은 임시 결합 복사본이며 최종 메시 객체는311개 보존.
조명: 낮은 따뜻한 Sun + 푸른 World. Cycles DIFFUSE(color/direct/indirect)와 AO를 별도128샘플로 굽고 합성. 원본 컬러 texel과 고유 lightmap UV 분리. 노출+1stop·청록 암부 리프트, 미디언3·0.65texel Gaussian 뒤 공용24색 양자화. 무작위 디더 없음. 팔레트는 게임 카메라의 구운 원본 색을 우선해 산출.

## 확인 파일
art/models/garage_scene_a_v4_baked/baked-lit.blend: 실제 굽기 장면.
같은 폴더 unlit-final.blend: Emission만 사용한 최종 렌더 장면.
art/_review/2026-10-27-scene-a-v4-baked/:1280×720 원본,1920×1080 Nearest1.5배, 비교 시트, 동일 감사 코드 측정.
스크립트 tools/asset-gen/scene_a_v4_{bake,quantize,unlit,measure,verify}.py. 원본 그림은 새 폴더만 쓴다.

## 제한
고정 배치 베이크다. 지면의 카트/소품 그림자는 이동해도 남는다. 동적 카트의 최종 게임 적용에는 카트를 제외한 정적 지면 베이크가 필요하다. 카트 자체의 구운 조명도 방향 회전에 따라 실시간으로 바뀌지 않는다.
Unity·Play·임포트는 실행하지 않았다. 현 결과는 시험 자산이며 최종 도현 판정 전. 카메라는 높이2.8m·뒤5.6m·세로FOV64,1280×720→1080 Nearest. 소개컷은 별도50도.
