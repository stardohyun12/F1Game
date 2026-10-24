# LofiAtlas 교체 제안 — 시험안, 미적용

현재 `F1/Assets/Art/LofiPilot/LofiAtlas.png` 512², 8×8칸·칸당64px. 이름·번호는 `tools/asset-gen/lofi_pilot.py`에서 확인했다. 아래 좌표는 PNG 왼쪽 위가 원점이며 0부터 센다.

| 최종 재료 | 기존 칸 | PNG 사각형 x,y,w,h | 적용할 곳 | 공유 영향 / 제약 |
|---|---:|---|---|---|
| wood | 4 `wood` | 256,448,64,64 | 교토 세로 외벽 | 문틀·의자 등 공유. 전역 교체 전 별도 재료 슬롯 권장 |
| stone | 41 `quaystone` | 64,128,64,64 | 교토 관광길 판석 | 기존 석축까지 판석으로 바뀔 수 있음. 바닥 전용 분리 필요 |
| plaster | 0 `plaster` | 0,448,64,64 | 포지타노 밝은 외벽 | 건물 대부분 공유. 구간별 재료 분리 필요 |
| roof | 44 `roof_clay` | 256,128,64,64 | 포지타노 점토 기와 | 교토 회색 지붕용3 `roof`는 유지. 기와 시각 판정은 보류 |

**이번에는 기존 atlas·코드·씬을 바꾸지 않았다.** `atlas_cells/*_60plus2.png`는 중앙60px 발췌+2px 가장자리 확장안이다. 최종 전체 반복 타일과 동일한 완성도/연속성을 보증하는 파일이 아니다.

기존 생성기의 UV는 셀 [2,62]의60px를 사용한다. 64px/m를 유지하려면 이60px가 **0.9375m**여야 한다. 현재 최대2m 분할면은 30px/m이므로 단순 PNG 교체로 64px/m가 되지 않는다. 기존 월드32px/m 지침을 일괄 변경했다는 뜻도 아니다.

권장 시험은 이번 독립 PNG를 Repeat로 사용하고 1 UV 반복에 wood1m / stone2m / plaster2m / roof4m를 배정하는 것이다. 기존64칸을 반드시 쓴다면 60px 연속 조각 여러 칸과 대응 UV가 필요하다. 이를64px로 다시 축소해 한 칸에 넣으면 큰 석판·기와 반복 비례와 밀도가 깨진다. 새 칸을 사용하기 전 타 팀 예약 상태도 확인해야 한다.

## 프로그래밍/QA 임포트 인계

- 시험 파일: `F1/Assets/Art/ProtoCourse/Textures/A1_KP_{wood,stone,plaster,roof}_64ppm.png` 네 개. 바이트 일치 증거는 `trial-copy.json`.
- Texture Type Default / sRGB ON / Filter Mode **Point** / Wrap Mode Repeat / Compression None / 원본 크기 유지(64,128,128,256). 화면 시험 첫 기준은 Mipmap OFF. 이는 엔진 적용 전 권고 설정이며 직접 적용하지 않았다.
- GLB 시험면은 Nearest·미프 없음·Repeat·white factor·unlit를 실제 내보내고 검사했다. Blender 정지 렌더는 같은 PNG를 Closest로 사용하고 조명은 별도다.
- 960×540 실제 화면에서 정면·사선·원거리·주행 떨림/모아레를 확인한다. 필요하면 미프 ON과 비교하되 평균화된 최종 화면에 albedo32색 기준을 잘못 적용하지 않는다.
- QA/프로그래밍 임포트 필요. 이번 작업은 Unity 실행·자동 임포트 확인·`.meta` 편집·씬/코드 변경을 하지 않았다. 정적 검사 통과는 적용 승인이나 시각 통과가 아니다.
