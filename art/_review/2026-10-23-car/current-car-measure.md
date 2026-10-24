# 현 차량 실측 — 2026-10-23 19:24

Unity는 실행하지 않았다. 저장된 해안 코스 씬·프리팹·메타 GUID를 추적하고, 실제 `F1/Assets/Art/CoastalFormula/CoastalFormula.fbx`를 Blender 5.2.2 배경 모드에서 가져와 월드 정점 좌표로 측정했다. F1 아래 파일은 읽기만 했다.

## 교체 기준

| 항목 | 측정값 / 유지 계약 |
|---|---|
| 모델 전체 폭 × 길이 × 높이 | **1.958909 × 3.785000 × 1.072000 m** |
| Blender bounds 최소 | (-0.979454517, -2.029999971, 0) |
| Blender bounds 최대 | (+0.979454517, +1.754999995, +1.072000027) |
| 모델 원점 | (0,0,0), 타이어 바닥 z=0 |
| 전후 bounds 중심 | Blender y=-0.1375; **원점을 bbox 중심으로 옮기지 말 것** |
| 방향 | Blender -Y 전방 / Z 위; 기존 FBX→Unity 프리팹 +Z 전방 / +Y 위 |
| 원본 tris | **4,872**. 옛 README 4,856와 다름; 이번 FBX 실제 삼각분할값 사용 |
| wheel_front_left | Unity (-0.79,0.35,+1.20); Blender (+0.79,-1.20,0.35) |
| wheel_front_right | Unity (+0.79,0.35,+1.20); Blender (-0.79,-1.20,0.35) |
| wheel_rear_left | Unity (-0.79,0.375,-1.10); Blender (+0.79,+1.10,0.375) |
| wheel_rear_right | Unity (+0.79,0.375,-1.10); Blender (-0.79,+1.10,0.375) |
| 바퀴 반지름 | 전륜 0.35 m / 후륜 0.375 m |
| 회전 계약 | Unity local X 회전; 앞바퀴 Y 조향. 이름 4개 유지 |

기존 FBX 변환은 X 좌표를 뒤집으므로 Blender의 +X에 있는 바퀴가 Unity left이다. 새 GLB·FBX는 각각 내보낸 결과의 방향과 이름을 별도로 검증해야 한다.

## 씬의 크기·피벗 계층

`F1/Assets/Scenes/Proto_CoastCourse.unity`의 `FormulaGameplayVisual`은 `Race` 자식이다.

| 항목 | 값 |
|---|---|
| Visual local scale | (1.4285712,1.4285712,1.4285712) |
| Race 상속 scale | (0.7,0.7,0.7) |
| 최종 월드 scale | (0.99999984,0.99999984,0.99999984), 실질적으로 원본 1 m 그대로 |
| Visual local position Unity | (0,0.1285497,-0.19642855) |
| Race 기준 월드 축 offset | (0,0.08998479,-0.137499985) |
| 저장된 Race position | (1.2,8.275001,5) |
| 저장된 Visual world origin | (1.2,8.36498579,4.862500015) |

새 모델을 원본과 같은 좌표 범위로 만들고 기존 Visual root scale·offset을 유지하면 기존 씬 크기와 바닥 피벗을 보존한다. `art/source/coastal-formula/gameplay-install.txt`의 **0.6632653**은 예전 ComplexTrack 설치값으로 현재 코스에 적용하면 안 된다. runtime 차 위치·차체 기울기는 이번 정적 측정 범위 밖이다.

## 참조 추적과 재질 사실

- 코스 → `FormulaGameplayVisual.prefab` (`ee48b362edabaa245b556832a047788e`) → `CoastalFormula.fbx` (`c098ca4be9a433845a9dfe6306945ea0`).
- 물리 부모 `F1/Assets/Prefabs/Race.prefab` (`356a5e3b096c28641af798c7539095f8`)의 기존 Kenney 시각 렌더러는 코스에서 disabled 처리되어 있다. 현재 화면에 쓰는 새 시각은 자체 제작 CoastalFormula이다.
- 차체 등 9개 렌더러는 코스 재질 `F1/Assets/Scenes/Proto_CoastCourse_Mats/FormulaWorn.mat`를 참조하고, 운전자는 FormulaRestored를 참조한다.
- 실제 worn 재질의 `_MainTex`는 `F1/Assets/Art/CoastalFormula/FormulaAtlasWorn.png`(256×256), Point, mipmap off이다. 따라서 **파일상 무텍스처가 아니라 화면상 무텍스처처럼 보이는 상태**로 구분한다. Play 상태의 복원 재질 적용 여부는 미검증.
- 생성기 `tools/asset-gen/coastal_formula.py:19`는 건물보다 조용한 2px 잔점을 의도했고, 178~184행은 면 크기에 관계없이 각 면을 57×57px 타일 영역에 늘려 편다. 일정 px/m 방식이 아니며, 작은 부품에도 동일 타일을 쓰므로 화면의 마모 크기가 고르지 않을 수 있다. 이는 코드 확인에 근거한 원인 추정이며 Unity 렌더 A/B 실험은 하지 않았다.

## 재현·확인 파일

- 측정 JSON: `current-car-measure.json` — 전체 오브젝트 치수·피벗, 프리팹 transform, source SHA-256 포함.
- 비교용 Blender: `current-car-source.blend` — 실제 FBX mesh를 가져오고 worn atlas / driver restored atlas를 연결한 **검토용 사본**. Unity 전용 셰이더를 완전 재현한 파일은 아니다.
- 이전 Unity 4방향은 `art/source/coastal-formula/01_front.png`, `02_rear.png`, `03_side.png`, `04_top.png`; 이번 코스 캡처와 같은 시점의 화면은 아니다.
- 명령: `D:/Tools/Blender/blender.exe -b --python art/_review/2026-10-23-car/measure_current_car.py`
- FBX와 GLB를 독립 import한 bbox 차이는 약 0.00000012 m 이하. 크기 원본 확인에 둘 다 사용했다.

Unity 임포트·Play·실제 바퀴 회전은 **QA/프로그래밍 임포트 필요**.
