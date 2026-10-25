# A1 2차 사진·가공 기록

Powered by Poly Haven. AI 생성 이미지·에셋·검토판 배경 **0**. 검토 장면은 `proof/material-stage.blend`의 실제 Blender 렌더이며 비교판은 그 렌더와 글자만 합쳤다.

## 공식 원본과 선택

| 용도 | 사진 / CC0 원본 | 선택·변경 |
|---|---|---|
| 목재 최종 | [Wooden Rough Planks](https://polyhaven.com/a/wooden_rough_planks), Rob Tuytel, 2m | 판 폭 차이·옹이·실제 마모가 있는 전체 반복 단위를 선택. 90° 회전, 재착색 없음. |
| 목재 1판 | [Wooden Planks](https://polyhaven.com/a/wooden_planks), 2m | 사진 이음·못 흔적을 보존했지만 가는 줄이 재료보다 앞서 보여 최종에서 교체. |
| 목재 대안 | [Wood Planks](https://polyhaven.com/a/wood_planks), [Wood Peeling Paint Weathered](https://polyhaven.com/a/wood_peeling_paint_weathered), [Wood Planks Dirt](https://polyhaven.com/a/wood_planks_dirt) | 전체 반복 구조·박락 크기·얼룩 밀도 비교. 원본과 공급자 메타데이터 보존. |
| 돌포장 유지 | [Stone Pavers](https://polyhaven.com/a/stone_pavers), Amal Kumar, 2m | 이전 albedo 바이트 그대로. 사진 밝기의 회색 사다리·실제 줄눈 손질도 이전판 그대로. |
| 회벽 바탕 | [Painted Plaster Wall](https://polyhaven.com/a/painted_plaster_wall), Amal Kumar, 2m | 넓은 벽 바탕과 기존 손상 보존. |
| 회벽 국소 박락 | [Rough Plaster Broken](https://polyhaven.com/a/rough_plaster_broken), Rob Tuytel, 1.5m | 사진 속 실제 박락 두 곳 윤곽을 따라 마스크. 주변 사진 영역도 포함해 연결. |
| 회벽 대안 | [Worn Cracked Plaster](https://polyhaven.com/a/worn_cracked_plaster), [Plastered Wall 05](https://polyhaven.com/a/plastered_wall_05) | 전면 균열 밀도·단조로움 때문에 미선택. |
| 기와 최종 | [Ceramic Roof 01](https://polyhaven.com/a/ceramic_roof_01), Rob Tuytel, 3.5m | 큰 가로 겹침·아래 음영·굽은 테두리로 선택. 2m 크롭. 재착색 없음. |
| 기와 대안 | [Clay Roof Tiles 02](https://polyhaven.com/a/clay_roof_tiles_02), [Grey Roof Tiles](https://polyhaven.com/a/grey_roof_tiles) | 다른 사진 3종을 같은 1m 크롭·64px·무디더·팔레트로 비교. 상세는 [후보 비교](ROOF-CANDIDATES.md). |

각 `source_*.json`, `sources/**/info.json`, `files.json`에 공식 직접 다운로드 URL·공급자 MD5·SHA·실측·저자를 보관했다. `art/provenance.csv`는 사용 원본, 탈락 사진 후보, 최종 albedo, 검증 GLB, 시험 PNG 교체 이력을 기록한다. 기존과 같은 원본 경로는 중복 행을 만들지 않았다.

## 재현과 적용

1. 기존 `photo_to_pixel.py`를 수정 없이 사용: 실제 사진 → 실측 크롭 → BOX 축소 → `pitcrew_d123_32` 최근접 RGB 양자화, 디더 없음. 손질 전 결과는 `baseline/`, 원본 축소는 `original_reduced/`에 보존.
2. 최종 네 종은 128×128 / 2×2m / 64px/m. 목재는 전체 원본의 실제 반복 단위를 사용. 기와 최종 크롭은 `[216,185,801,770]`이며 정수 오차 약 0.03%.
3. 목재·기와는 색 재조정 없이 사진 구조를 교체했다. 회벽 바탕 RGB+[53,43,28], 박락 RGB+[60,65,80]. 실제 박락 마스크를 원본에서 55px 확장·12px feather하여 주변 벽까지 포함한 뒤 물리 크기로 축소했다. 원본 사진 픽셀의 합성이고 생성형 채우기나 합성 잡음을 쓰지 않았다.
4. 새 세 종은 대응 경계 2px 색 보정 후 다시 공용 팔레트로 양자화했다. 이것이 기하학적 반복까지 완벽하게 이어짐을 보장하지는 않는다. `review/*_repeat_3x3.png`에 반복 확인판을 보존.
5. 실제 2m GLB UV 양 축은 64px/m. 회벽 추가 사진은 독립 재계산에서 63.05~64.25px/m. 두 번째 주변부 아래 7px가 캔버스 밖에서 잘렸으며 배율 변경은 없다.

제작: `tools/asset-gen/kp_textures_v2.py --revision 2` → `kp_texture_preview.py` Blender background → `kp_textures_v2.py --provenance` → `check_asset.py --texture-manifest …/manifest.json --out …/check_asset.json`. 인계 완료 뒤 `--provenance`를 단독 재실행하면 시험 복사 행을 제거할 수 있으므로 인계 장부를 먼저 보존해야 한다. `kp_v2_handoff.py`는 기존 파일 SHA 확인 후 1회 교체하고 중복 실행을 거부한다.

## 라이선스·확인 한계

[공식 라이선스](https://polyhaven.com/license)는 자산 CC0를 명시한다. [공식 FAQ](https://docs.polyhaven.com/en/faq)는 텍스처가 사진 기반이며 절차 생성·생성 AI를 쓰지 않는다고 설명한다. 이는 공급자 설명이며 개별 촬영 방식·촬영 장소·목재의 수리 이력은 확인하지 않았다. 교토·포지타노 현지 촬영 사진이라고 주장하지 않는다. 저자별 사진 제작 전 과정을 재현한 것도 아니다.

**32색 검사는 조명 전 원본 albedo PNG 기준**이다. 다운로드 사진·축소 원본·조명/안개 적용 렌더는 32색 검사의 대상이 아니다. 최종 게임 화면 검사는 별도이며 Unity 실행·임포트·움직이는 화면 검증은 하지 않았다.
