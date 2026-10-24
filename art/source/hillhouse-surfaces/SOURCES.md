# 언덕집 표면 사진 출처

확인일: 2026-10-23 · Powered by Poly Haven.

공식 라이선스: https://polyhaven.com/license (CC0-1.0). 원본 사진·확산색 맵은 아래 저자 자료이며, 생성 AI는 사용하지 않았다.

원본의 제공 치수(mm)를 사용해 2m×2m 사진 영역을 반복/크롭 → 기존 `photo_to_pixel.py` BOX 64×64, 32px/m, 디더 없음으로 원본 조건의 기준판(baseline/)을 먼저 보존했다.

적용판은 같은 사진의 64px BOX 명도 면적 평균을 소재별 색 사다리로 재노출한 뒤 photo_to_pixel에 다시 넣었다. 서브픽셀 직조가 단일 중간색으로 뭉개지는 문제를 해결하기 위한 명시적인 변경이다. 원본 사진과 같은 결과라고 부르지 않는다. 이후 위치에 이유가 있는 마모·Galmuri 글자만 픽셀로 덧썼다.

원본 사진보다 무늬를 키우지 않았다. 천 직조처럼 1픽셀보다 작은 무늬는 자연스럽게 소실된다. 첫 rough_linen 원천은 32px/m에서 단색이 되어 보관만 하고, 실제 체크 면직물 fabric_pattern_05로 바꿨다. 사진 캡처 현장·원본 RAW는 직접 확인하지 않았으며 공개 diffuse map·reference 사진·저자 메타데이터에 근거했다.

## worn_plaster_wall

- URL: https://polyhaven.com/a/worn_plaster_wall
- 파일: `art/source/hillhouse-surfaces/photos/worn_plaster_wall_diff_1k.jpg`
- 저자: {"Dimitrios Savva": "All"}
- 제공 치수(mm): [1800.0000715255737, 1800.0000715255737]
- SHA256: `f769bee35504ff713e89e89d2b64c1eb3c373dd184973ac8706310f19fd63efe`
- 라이선스: CC0-1.0, https://polyhaven.com/license

## wood_peeling_paint_weathered

- URL: https://polyhaven.com/a/wood_peeling_paint_weathered
- 파일: `art/source/hillhouse-surfaces/photos/wood_peeling_paint_weathered_diff_1k.jpg`
- 저자: {"Rob Tuytel": "Processing", "Dimitrios Savva": "Photography"}
- 제공 치수(mm): [761.9999647140503, 762.0000243186951]
- SHA256: `7311e60ec0ca3de10383dd5f2d398819e2198ea78244cf4f6627d1cb26cf4b53`
- 라이선스: CC0-1.0, https://polyhaven.com/license

## clay_roof_tiles

- URL: https://polyhaven.com/a/clay_roof_tiles
- 파일: `art/source/hillhouse-surfaces/photos/clay_roof_tiles_diff_1k.jpg`
- 저자: {"Amal Kumar": "All"}
- 제공 치수(mm): [4000, 4000]
- SHA256: `aac0f4e742a2cce21afb5dfb523b47c720c6a8e2782a312b823608fe3f80eae6`
- 라이선스: CC0-1.0, https://polyhaven.com/license

## rectangular_paving

- URL: https://polyhaven.com/a/rectangular_paving
- 파일: `art/source/hillhouse-surfaces/photos/rectangular_paving_diff_1k.jpg`
- 저자: {"Dimitrios Savva": "All"}
- 제공 치수(mm): [2000, 2000]
- SHA256: `23f3da5644a95fbcc1916165c8b03ac74d0836ce4b6f5bf2a28b0266dcdf84c5`
- 라이선스: CC0-1.0, https://polyhaven.com/license

## worn_asphalt

- URL: https://polyhaven.com/a/worn_asphalt
- 파일: `art/source/hillhouse-surfaces/photos/worn_asphalt_diff_1k.jpg`
- 저자: {"Amal Kumar": "All"}
- 제공 치수(mm): [2000, 2000]
- SHA256: `3bf71ad290730092ad2aee85e246b2e43318bc1f32261826ba91250e815b5f72`
- 라이선스: CC0-1.0, https://polyhaven.com/license

## stone_brick_wall_001

- URL: https://polyhaven.com/a/stone_brick_wall_001
- 파일: `art/source/hillhouse-surfaces/photos/stone_brick_wall_001_diff_1k.jpg`
- 저자: {"Dimitrios Savva": "Photography", "Rico Cilliers": "Processing"}
- 제공 치수(mm): [2500, 2500]
- SHA256: `15fb31a178401eda2a2b86da539fa39021a08c7a43e902af785f99e18cba6311`
- 라이선스: CC0-1.0, https://polyhaven.com/license

## fabric_pattern_05

- URL: https://polyhaven.com/a/fabric_pattern_05
- 파일: `art/source/hillhouse-surfaces/photos/fabric_pattern_05_col_01_1k.jpg`
- 저자: {"Rob Tuytel": "All"}
- 제공 치수(mm): [500, 499.9999403953552]
- SHA256: `b9ece89a45044945ce4d1402672cfd605f1f074b313cb3cc12b7b2ea66fad7fe`
- 라이선스: CC0-1.0, https://polyhaven.com/license

## 글자

- Galmuri11: `art/_review/2026-10-23-d123/fonts/Galmuri11.ttf`, OFL-1.1. 실제 글리프만 사용.
- 라이선스 파일: `art/_review/2026-10-23-d123/fonts/Pretendard-OFL.txt`, `art/_review/2026-10-23-d123/fonts/LICENSE.txt`

## 변경 범위

기존 `lofi_pilot.py`·`F1/Assets/Art/LofiPilot/LofiAtlas.png`는 읽기만 수행. 셀 51~58에만 붙인 `merged_preview.png`에서 다른 픽셀이 같음을 검사했다. 실제 적용은 프로그래밍팀. 이전 셀 색은 공용32색 규격이라고 주장하지 않는다. 신규 확장 아틀라스만 32색 규격이다.

## R2 processing clarification

Foundation block boundaries are traced from the measured 2m photographic crop of stone_brick_wall_001; each block is simplified using its photographed mean value. Paving joints follow photographed 20cm by 40cm blocks. Roof color uses the clay_roof_tiles photograph with only five long photographed highlight bands retained, and no added random noise. See art/_review/2026-10-23-hillhouse/surfaces/r2-changes.md.
