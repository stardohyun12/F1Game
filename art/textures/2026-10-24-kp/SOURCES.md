# A1 사진 출처·재현 기록

Powered by Poly Haven. 확인일 2026-10-24.

공식 API의 Diffuse 1k PNG를 CDN에서 직접 다운로드했다. 최종 네 원본 모두 공급자 MD5와 일치하며 SHA-256은 아래 원본 메타데이터 및 `art/provenance.csv`에 기록했다. 사이트 미리보기 이미지를 질감으로 사용하지 않았다.

Poly Haven은 자산을 [CC0](https://polyhaven.com/license)로 배포하며, [공식 FAQ](https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning)는 사진 기반 질감 제작과 비생성형 방식을 설명한다. 공급자의 설명을 근거로 채택했으며, 개별 자산의 photogrammetry/photometric-stereo 하위 방식이나 촬영 장소는 독립 확인하지 못했다. 교토·포지타노 현지 촬영 사진이라고 주장하지 않는다.

| 용도 | 공식 원본 / 작가 | 원본 반복 크기 | 직접 다운로드·검증 |
|---|---|---|---|
| 교토 세로 목재 | [wood_plank_wall](https://polyhaven.com/a/wood_plank_wall) / Dimitrios Savva | 1m 정사각 | [PNG](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/wood_plank_wall/wood_plank_wall_diff_1k.png) · [메타데이터](source_wood.json) |
| 교토 돌포장 | [stone_pavers](https://polyhaven.com/a/stone_pavers) / Amal Kumar | 2m 정사각 | [PNG](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/stone_pavers/stone_pavers_diff_1k.png) · [메타데이터](source_stone.json) |
| 포지타노 회벽 | [painted_plaster_wall](https://polyhaven.com/a/painted_plaster_wall) / Amal Kumar | 2m 정사각 | [PNG](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/painted_plaster_wall/painted_plaster_wall_diff_1k.png) · [메타데이터](source_plaster.json) |
| 포지타노 기와 | [clay_roof_tiles](https://polyhaven.com/a/clay_roof_tiles) / Amal Kumar | 4m 정사각 | [PNG](https://dl.polyhaven.org/file/ph-assets/Textures/png/1k/clay_roof_tiles/clay_roof_tiles_diff_1k.png) · [메타데이터](source_roof.json) |

## 가져온 부분 / 바꾼 부분 / 검증 범위

- 기존 `photo_to_pixel.py`를 수정 없이 실행해 BOX 축소 → 공용 32색 nearest 양자화 → 무디더 기준판을 `baseline/`에 보존했다. 외부 작가의 전체 제작 과정을 재현한 것은 아니다.
- 최종본은 같은 사진 축소본에서 색 사다리를 재배치했다. 목재는 90° 회전과 갈색 밝기 사다리, 돌은 중성 회색 사다리와 **사진에 이미 있는 줄눈만 직접 따라 그린 1px 보정**, 회벽은 실제 얼룩 밝기 구간, 기와는 0.55px 저역 보정과 흙빛 사다리다. 좌표·수치는 `kp_textures.py`와 manifest.process에 있다.
- 가장자리 3px의 대응 색을 평균하고 팔레트로 재양자화했다. 무작위 잡음·AI 생성 이미지·AI 배경·노멀/AO 재료 합성은 0이다.
- 반복을 깨던 1m 크롭을 폐기하고 원본 반복 크기(1/2/2/4m)를 보존했다. 64²/128²/128²/256²는 모두 64px/m이다. `research/first-crop-trial/`은 미사용 이력이다.
- 기준 장면: 직접 만든 Blender 시험면. 실제 게임 캡처·주행·원거리 안개·미프 동작을 재현하지 않았다. 32색 검사는 **원본 albedo PNG(조명 전 납품 텍스처)**에만 적용하며 고해상도 사진과 최종 화면 색 검사는 별도다.
- 구도 참고는 `art/reference/mood-2026-10-23/board_kyoto_main.png`, `board_positano.png`의 실제 사진뿐이며, 이 사진을 픽셀 재료로 사용하지 않았다. AI 기준 원화도 사용하지 않았다.

## 미사용 후보

- Stone Pathway: 갈색 자연돌이 강함. Grey Stone Path: 불규칙 돌로 주요 관광길의 정형 판석과 다름. Wood Plank Wall과 Stone Pavers를 최종 채택.
- Worn Plaster Wall: 큰 박락·이음새가 강함. Plastered Wall 02: 미장 패널 구분. White Plaster 02: 지나치게 균일. Painted Plaster Wall의 실제 잔얼룩을 채택.
- 후보 원본·공식 info/files JSON은 `sources/`에 남겼으며 최종 manifest에는 채택한 4종만 들어 있다.

## 재생성

```powershell
py -3.13 tools/asset-gen/kp_download.py
py -3.13 tools/asset-gen/kp_textures.py
& D:/Tools/Blender/blender.exe -b -t 8 --python tools/asset-gen/kp_texture_preview.py
py -3.13 tools/asset-gen/kp_textures.py --provenance
py -3.13 tools/asset-gen/check_asset.py --texture-manifest art/textures/2026-10-24-kp/manifest.json --out art/textures/2026-10-24-kp/check_asset.json
```
시험 복사는 위 검사 통과 후 `py -3.13 tools/asset-gen/kp_handoff.py`. 재생성은 art/ 산출만, 마지막 handoff가 허용된 PNG 4개만 F1에 복사한다. PNG 자체는 Point 필터를 강제할 수 없다. GLB 시험면은 Nearest/Repeat/unlit를 내보내고 실제 값을 검사했다. Unity 설정은 `ATLAS-PROPOSAL.md`를 인계한다.
