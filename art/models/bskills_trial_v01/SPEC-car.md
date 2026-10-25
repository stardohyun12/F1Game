# 차량 3종 — blender-skills 적용 시험

상태: Modeling + Geometry preflight 완료; 실제 아틀라스·동일 카메라 렌더·독립 그래픽 게이트·FBX 재열기·Unity 검증은 담당 루트 단계에 남음.

## 읽고 적용한 원문
- `D:/Github/_vendor/blender-skills/.claude/skills/vehicle-artist/SKILL.md` (arjun988/blender-skills, MIT).
- 같은 폴더 `references/proportion-blockout.md`.
- `art/_review/2026-10-25-bskills-trial/briefs/parked_car_01.md` ~ `03.md`.
- 외부 스킬은 실행 가능한 차량 원본 모델을 제공하지 않는다. 원본 조건으로 모델을 재현했다고 주장하지 않으며, 공개 지침을 본 프로젝트 코드에 적용했다.

## 가져온 것과 바꾼 것
- 휠베이스 → 윤거 → 타이어 반경 → 객실/보닛 → 오버행 순서로 `PROPORTIONS` 값을 고정했다.
- body/chassis/glass/lights/trim/개별 4륜에 `PART`를 분리하여 루트 exporter가 각각 별도 mesh로 만들 수 있게 했다.
- 기존 props2의 하부 차체·실제 휠아치와 wheel/rounded_box primitive를 재사용했다. 원본 파일은 변경하지 않았다.
- 기존 불투명 객실 덩어리 위에 유리를 붙이던 구조 대신, 정확한 평면의 유리/기둥이 모서리를 공유하는 패널 구성으로 바꿨다. 앞뒤 유리를 각각 실제 경사면에서 생성하므로 앞 유리 오프셋을 뒤쪽에 잘못 재사용하지 않는다.
- 지붕 스티커 제거, 지붕 크라운 35mm, 옆 유리 상단까지 키움, 상부를 160mm 좁혀 실차 객실 테이퍼 적용.
- 유리 윗부분만 `window_light` 타일 사용. 보닛 경계, 밝은 금속 범퍼, 어두운 그릴을 별도 구성.
- 스킬 MCP/고폴리/투과/PBR 기본값 대신 headless bpy/≤1500tri/기존 32px/m metric UV/opaque atlas를 적용했다.

## 고정 비례 (m)
| 차 | 차체 폭×길이×높이 | 휠베이스 | 윤거 | 바퀴 반경 | 앞/뒤 오버행 |
|---|---|---|---|---|---|
| 01 | 1.48×3.20×1.43 | 1.98 | 1.33 | .265 | .61/.61 |
| 02 | 1.54×3.64×1.44 | 2.28 | 1.39 | .28 | .68/.68 |
| 03 | 1.395×3.295×1.68 | 2.335 | 1.245 | .255 | .48/.48 |

실제 미러 포함 bounds는 preflight JSON에 별도 기록. 이전보다 미러 전체 폭 60mm 감소, 01의 별도 지붕판 제거로 최고 높이 15mm 감소. 나머지 차체 치수/축간거리 유지. Blender Z-up/front -Y, export Y-up/front +Z; 바닥 중심 피벗.

## 실행 증거
명령: `D:/Tools/Blender/blender.exe --background --python art/models/bskills_trial_v01/preflight_cars.py`
Blender 5.2.2 실제 mesh 생성/삼각화 결과: 01=1314tris, 02=1314tris, 03=1386tris; 모두 면적 0인 면 0, 바닥 z≈0.
원시 측정: `art/models/bskills_trial_v01/car-geometry-preflight.json`.
생성 코드: `tools/asset-gen/bskills_trial_cars.py`.

## 남은 제약
- 위 숫자는 실제 Blender geometry 검사 결과이며, 렌더 품질 판정 또는 UV 전체 검사 결과가 아니다.
- 차체는 원래의 직선 하부 구조를 유지했다. 01의 둥근 맛은 16각 바퀴와 얇은 크라운 중심이며, 3/4 카메라에서 박스 느낌이 남는지 독립 판정 필요.
- 유리는 의도적으로 불투명, 실내·애니메이션·LOD·충돌은 만들지 않았다.
- 색/사진 질감/광원·렌더·FBX 재열기는 루트 담당. Unity와 커밋 미실행.


## ?? ??? r1 ?? ? ?? ?? (r2)
- r1: ?? 3? ?? 5~5.5/10?NO. ?? ?? ?? ???? ???? ??.
- ??: B??? ??? ? ??? ???? ?? geometry??? `darkroof`? ??? ?? ??? ????? ????. ????? ?? ?? ???? ?? ?? ?? ?? ??? ???? ??? 75mm ??? ??.
- 01/02? B??? ?? ?? ? y=.28m? ??; 02? ? ??+?? ?? ????? ??. 03? ?? B=.03m, ?? C=.79m? ?? ???? ??.
- ????? ?? ?? ?? `window_light` ? ??, ?? glass ?? ??.
- 03 ?? ?? .255?.280m, ???? ?? ?? ???? ?? ??. ?? ??? ??? ?? ?? ?? ?? ?? ?? ??.
- ?? ??? ?? render/export/?? ???? ?? ????, ?? ? ?? ???? ???? ??.
