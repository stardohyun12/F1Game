# Scene A V5 combo — 프로그래밍 인계

V1b 사진 텍스처·원래 UV + V4 낮은 해/하늘빛/AO. Unity 실행·임포트 미확인.

| FBX | 연결 PNG | tris | texel/m | PNG 크기 |
|---|---|---:|---:|---|
| garage_v5_combo.fbx | garage_baked32.png | 1580 | 32 | 4096×1024 |
| kart_v5_combo.fbx | kart_baked32.png | 560 | 32 | 2048×128 |
| ground_v5_combo.fbx | ground_baked32.png | 2 | 32 | 4096×2048 |
| dirt_v5_combo.fbx | dirt_baked32.png | 18 | 32 | 2048×1024 |
| quay_v5_combo.fbx | quay_baked32.png | 10 | 32 | 4096×256 |
| sea_v5_combo.fbx | sea_baked32.png | 2 | 0.32 | 2048×256 |
| props_v5_combo.fbx | props_baked32.png | 1340 | 32 | 2048×1024 |

- **7개 FBX 모두 같은 장면 원점. 같은 부모 아래 위치0·회전0·배율1로 배치.** 카트도 현재 장면 위치를 포함한다. V1b의 개별 로컬 카트 위치/정비소 −6m 보정을 다시 적용하지 않는다. V4 출력과 같은 배치 방식이다.
- Blender Z-up → FBX Y-up, forward −Z. 단위 m. BakedUV가 UV0. 원래 형상·배치 보존, 카트 분리 부품 이름 유지.
- 텍스처 sRGB / Point / 비압축 / 밉맵 끔으로 비교. Unity Unlit BaseMap에 PNG 연결, 틴트 흰색. FBX diffuse 경로는 연결 힌트이며 Unity Unlit 셰이더 자동 설정이 아니다.
- 공용 32색, 디더 없음. 사진 색을 평활화하지 않음. 바다는 원경 단색 배경 예외 0.32texel/m이며 주요 건물·소품·지면은32.
- 고정 장면 그림자를 지면에 구움. 카트 이동 시 기존 그림자가 남으므로 동적 게임 투입 전 카트를 제외한 지면 베이크가 필요.
- 참고 카메라는 V4와 동일(뒤5.6m/높이2.8m/세로FOV64), 1280×720→1920×1080 Nearest. 프로그래머0429 카메라의 바라보는 지점과 다르므로 Unity V1의 수치와 직접 우열 판정 금지.
- 원본/베이크/Emission blend: art/models/garage_scene_a_v5_combo/. 실제 사용 조건 최종 판정은 Unity.
