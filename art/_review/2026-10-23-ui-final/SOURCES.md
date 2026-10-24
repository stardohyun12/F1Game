# 출처와 제작 이력

2026-10-23 확인. 출시 검토 대상은 `sprites/*.png` 10개이며, 참조 이미지·목업을 게임 이미지로 가져오지 않는다. 각 PNG의 해시와 입력 유형은 [ui-manifest.json](ui-manifest.json), 실제 등록은 [art/provenance.csv](../../provenance.csv)의 `ui_final_20261023_*` 10행이다.

| 입력 | 원문 / 로컬 원본 | 확인한 것 / 적용 범위 |
|---|---|---|
| 종이 섬유 사진 자료 | [ambientCG Cardboard Set 001](https://ambientcg.com/view?id=CardboardSet001), [CC0 원문](https://docs.ambientcg.com/license/), [보관 파일](../../source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png) | 원문 기술 분류 **Surface Photogrammetry**, CC0-1.0. 사진 측량으로 만든 표면 색 텍스처다. 생사진 또는 흰 종이 스캔이라고 주장하지 않는다. 무인쇄 골판지 겉종이 섬유를 paperPanel/paperCard에 재색해 사용. |
| 팔레트 | [D-123 공용 32색](../../source/palette/pitcrew_d123_32.json) | 승인 방향의 색상값 재사용. 원화 이미지를 텍스처로 합성하지 않음. |
| 도장·선·패널 | [build.py](build.py) | Pillow 좌표 그리기. 노이즈 생성·이미지 생성 모델 없음. 도장 글자는 PNG에 굽지 않고 폰트 별도 표시. |
| Galmuri11 | [제작자 원문](https://quiple.dev/font/galmuri), [원본 저장소](https://github.com/quiple/galmuri), [동봉 OFL](fonts/Galmuri-OFL.txt) | SIL OFL 1.1. 기존 UI 1/2 조사에서 확보한 TTF/WOFF2 재사용. 원본 폰트 수정 없음. |
| Pretendard Regular | [원본 저장소/라이선스](https://github.com/orioncactus/pretendard#라이선스), [동봉 OFL](fonts/Pretendard-OFL.txt) | 저장소의 기존 TTF를 읽어 복사. 원본 폰트 수정 없음. 목업에서만 12px 단색 마스크로 렌더. |
| 기존 게임 캡처 | [docs/qa/evidence/slice-start.png](../../../docs/qa/evidence/slice-start.png) | 실제 게임 캡처를 960×540 그대로 사용하고 모달 배경을 어둡게 합성. 이번 작업에서 Unity를 실행해 새로 촬영한 것이 아님. 배경용·검토 전용. |
| 포장 상자 | [아트팀 close.png](../2026-10-23-cargo-set-v2/box/close.png) | 기존 투명 렌더를 축소 합성. 모델·텍스처·원본 렌더 수정 없음. |
| D-096 기준 원화 | [02_warm_street.png](../../concept/lofi-styles-2026-10-13/02_warm_street.png) | AI 원화. **옆 비교 전용**, 스프라이트 입력 아님. |
| 이전 D-123 시안 | [mockup-call.png](../2026-10-23-d123/mockup-call.png) | AI 재질을 포함한 이전 시안. 미세 질감 비교 전용. 새 PNG 제작에 사용하지 않음. |
| Tiled 방법 | [Unity uGUI Image.Type 공식 문서](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Image.Type.html) | Sliced의 늘어나는 구간을 Tiled는 반복 채운다. 이 원리를 정적 Pillow 9분할 합성으로 재현. Unity 임포트 결과는 미검증. |

## 사진 → 픽셀 재현

원본 SHA-256: `199ffb0717e6ea8bec89d72c5b12ad651a9753cf2711ca7d78e7560cb71d01ea`.

1. 원본 1024²의 무인쇄 부분 `(L,T,R,B)=(825,590,1000,705)`를 자른다.
2. BOX로 64² 축소하고 실제 명암을 최소값~70백분위 구간으로 정규화한다.
3. D-123의 `#C4A687`~`#E4D6BF` 사이로 재색한다.
4. `tools/asset-gen/photo_to_pixel.py`의 `quantize_exact(..., dither='none')`를 직접 호출한다. 3D용 CLI의 metre/density 값을 UI에 임의로 부여하지 않는다.
5. 64² 사진 패치에는 종이색 4,048셀·섬유색 48셀(1.17%)이 남는다. 패널 원본의 각 저해상도 셀을 Nearest 4배로 저장한다. 코드로 고정 여백과 모서리만 그린다.
6. 스프라이트를 0.5배 표시하면 960 논리 화면에서 섬유 셀이 정확히 2×2px다. 9-slice 내부를 Tiled로 합성하므로 크기가 달라도 섬유를 늘리지 않는다.

사진 처리 중간물은 `materials/`, 입력 스냅샷과 해시는 [sources.json](sources.json). 첫 crop에 있던 접힌 자국이 반복 무늬처럼 보였으므로 무인쇄 안쪽으로 crop을 이동했다. 비교 확대는 [grain-compare.png](grain-compare.png), 정확한 2배 패치는 [patch-new-128@2x.png](materials/patch-new-128@2x.png).

`Paper001`·`Paper005`는 ambientCG 원문에서 Approximation으로 표시돼 사진 입력 후보에서 제외했다. 이번 작업의 종이 원천은 CardboardSet001 하나다. 소리·모션·게임 전체 AI 이미지 포함 여부는 이번 정적 제작으로 검증하지 않았다.

## 검증 범위

`check_asset.py --ui-manifest`는 파일 바이트·팔레트·격자·기존 슬롯 계약·기록된 출처를 검사한다. AI 이미지 여부를 픽셀만 보고 판별하는 도구가 아니다. 제작 코드와 입력 파일을 함께 검토했고, 10개 후보의 이미지 입력은 위 CC0 자료와 직접 그린 도형으로 제한했다. 출시 빌드 전수 검사는 미실행.
