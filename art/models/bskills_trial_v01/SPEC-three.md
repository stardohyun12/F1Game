# three_wheeler_01 — vehicle-artist 적용 시험

## 적용·재사용·변경

- 적용 스킬: `D:/Github/_vendor/blender-skills/.claude/skills/vehicle-artist/SKILL.md`, `references/proportion-blockout.md` (MIT). 비례 고정 → 구조 → 부품 순서, 차체·유리·타이어·허브·하중 연결부 분리.
- 원본 재사용: `tools/asset-gen/bayloop_props2_vehicles.py`의 `_prism`, `_wheel`. 원본 파일 수정 없음. 기존 삼륜차 전체를 원본 조건으로 재현했다고 주장하지 않음; 기존 r3 PNG와 실패 기록을 기준으로 구조 변경.
- 새 생성기: `tools/asset-gen/bskills_trial_three.py`, 진입점 `build(p,b)`. 호스트의 metric32 face/box/beam 및 PARTS 사용. Blender 5.2.2 headless에서 실제 실행.
- 이전 실패 근거: `art/_review/2026-10-25-bayloop-props2/last-gate-r3.md`의 적재함 아래 긴 틈·발판 분리. 넓은 단일 차대 대신 두 종방향 레일, 두 연속 크로스멤버, 차실 바닥, 네 발판 브래킷을 구성.
- 사진 아틀라스·색상·32px/m 양축 수치 검증·내보내기·독립 시각 심사는 루트 담당. 임의 얼룩 추가 없음.

## 비례 참고와 고정값

[Piaggio Ape 50 공식 기술도면](https://commercial.piaggio.com/wlassets/ape/master/modelli/ape50/TECH-SPEC/Ape-50-info-tecniche/original/Ape%2B50%2BE4_%2BITA_%2Binfo%2Btecniche.pdf)과 [영국 공식 브로슈어](https://www.piaggiocommercialuk.com/wp-content/uploads/2019/09/HighRes-EN-Ape50-Brochure-05-2019.pdf)의 실제 치수(축간 1.59m, 차체 폭 1.26m)를 참고했다. 도면/사진 파일을 모델에 포함하거나 복제하지 않았다. 공개 수치 참고이며 실제 차량 CAD 재현이 아니다.

| 항목 | 고정/실측 |
|---|---|
| 축간 / 뒤 윤거 / 타이어 반지름 | 1.59 / 1.06 / 0.225m |
| 차체 폭 / 거울 포함 폭 | 1.26 / 1.38m |
| 길이 / 높이 | 2.57 / 1.59m |
| 삼각형 | 1,082 / 예산 1,500 |
| 바닥 최소 Z | -0.00000000745m, 부동소수점 오차 |
| 시스템 파트 수 | 18 |

기존 축간 1.91m를 실제 소형 삼륜차 기준 1.59m로 수정했고 앞바퀴 위치 이동으로 전체 길이가 기존 2.66m에서 2.57m가 됐다. 높이·운전실 실루엣은 이전 형태를 유지했다. 브랜드 표식 없음. 정적 소품이라 리그/LOD/충돌 생성 없음.

## 물리적 접촉 범위 (생성 좌표 m, Blender Z-up)

| 경로 | 범위와 접촉 |
|---|---|
| 좌우 레일 | X 중심 ±0.40, 폭 0.10, Y -0.88~0.91, Z 하단 0.31~0.39 / 상단 0.45 |
| 두 크로스멤버 | Y 0.12/0.84, X -0.62~+0.62 연속, Z 0.43~0.56 |
| 적재함 바닥 | X -0.63~+0.63, Y -0.045~1.265, Z 0.54~0.64 |
| 레일→크로스멤버→바닥 | 각 수직 교집합 0.02 / 0.02m |
| 차실 바닥→차체 | Z 0.43~0.53 → 차체 하단 0.52, 교집합 0.01m; 레일과 0.02m |
| 좌우 브래킷 각 2개 | Y -0.81/-0.33, X 절댓값 0.36~0.60, Z 0.35~0.45; 레일 측면과 0.09m 겹침 |
| 발판 | X 절댓값 0.43~0.65, Z 0.42~0.465; 브래킷과 수직 0.03m 교집합 |
| 뒤 서스펜션 | Z 0.225~0.33, 레일 하단과 0.02m 겹침, 가로 차축에 접촉 |

이는 시각 모델의 기하 접촉이며 물리 시뮬레이션이나 실제 하중 해석이 아니다. 맞물림 면은 내부에 숨긴다. 시스템 간 의도된 교집합은 부품 연결용이며 독립 메시로 내보낸다.

## 검증·남은 일

- 실제 실행 증거: `three-geometry-preflight.json` (Blender 5.2.2, F 기준 삼각형 합·V 기준 bounds·PARTS별 면수). 초기 1,042 triangles 기록이며 수정판 결과는 아래에 별도 기록.
- 재확인: Blender background에서 `bayloop_buildings`를 b로 import하고 body/glass/rubber/metal/trim/darkroof atlas alias 설정 후 `bskills_trial_three.build({},b)` 실행; `sum(len(f)-2 for f in b.F)` 및 V/PARTS 확인.
- 호스트 metric32 UV 사용하지만 최종 UV·atlas 통계, FBX/GLB reimport, PNG 연결부 판정은 아직 수행하지 않았다. Unity 실행/Assets 반영/커밋 없음.
- 최종 독립 시각 심사에서 측면 연결부 가독성, 창 틀, 사진 표면을 확인해야 한다.

## r1 독립 심사 후 1회 한정 수정

- `three-gate-r1.md`: 6.5/10, 아니오. 연결부 개선 이후에도 긴 직사각 차대와 밝은 금속띠가 남았다는 독립 판정에 대응했다. 합격으로 해석하지 않는다.
- 레일 길이 2.22→1.79m, 폭 0.13→0.10m; 단면 하단을 양 끝에서 올라가는 6점 형상으로 바꿨다. 후방 크로스멤버를 Y1.02→0.84로 옮겨 레일 끝과 실제 접촉을 유지한다. 적재함 뒤쪽은 지지점 뒤 약0.365m의 돌출부가 된다.
- 적재함 바닥·보강띠·크로스멤버는 도장 body, 발판·브래킷은 darkroof로 변경하여 큰 밝은 금속띠 면적을 없앴다. 금속 alias는 손잡이·거울·앞 포크·차축 등 제한된 부분에만 남긴다. 사진 타일 자체 수정은 루트 담당이다.
- 앞 포크 상단 가로 연결대와 조향목을 추가해 차실 바닥에 물린다. 서스펜션 상단을 Z0.33으로 올려 얇아진 레일과 0.02m 접촉을 유지한다.
- 수정판 실제 Blender 5.2.2 headless 실행: **1,082tris**, 예산 assert 통과, bounds **1.38×2.57×1.59m** 유지. JSON은 초기판 보존; 이번 요청의 module/SPEC만 수정 범위를 지켰다.
- 최종 PNG·UV/export 재검증과 독립 심사는 미실행. 재심사 실패 시 채택하지 않고 보류 시험 결과로 기록한다.

