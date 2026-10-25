# Cargo box — 사진·치수 출처

확인일: 2026-10-23. 생성형 이미지 미사용. 아래 사진 자산만 CC0이며 제조사 웹페이지의 사진은 내려받거나 텍스처로 사용하지 않았다.

## 실제 사진 기반 텍스처

- 자산: ambientCG **Cardboard Set 001** (2024-02-13 공개).
- 공식 자산: https://ambientcg.com/view?id=CardboardSet001
- 원본 ZIP: https://ambientcg.com/get?file=CardboardSet001_1K-PNG.zip
- 공식 라이선스: https://docs.ambientcg.com/license/ — CC0 1.0 Universal, 원본 및 수정본의 상업 이용·재배포 허용.
- CC0 본문: https://creativecommons.org/publicdomain/zero/1.0/
- 제공자 표기 제작 방식: **Surface Photogrammetry**, 촬영 범위 약 1.5 × 1.5 m. 우리가 촬영한 사진이나 RAW 원본이 아니라 제공자가 사진 측량으로 만든 color map이다.
- 보관 파일: `CardboardSet001_1K-PNG_Color.png`, 1024 × 1024 px. 배포 ZIP에서 color map만 그대로 추출; 나머지 PBR 맵 미사용.
- SHA256: `199ffb0717e6ea8bec89d72c5b12ad651a9753cf2711ca7d78e7560cb71d01ea`
- API 메타데이터 스냅샷: `CardboardSet001.source.json`; `creationMethod=PBRPhotogrammetry`. API의 일부 설명 필드는 다른 자산에서 서로 불일치하는 것을 확인하여 자산 상세 페이지의 Technique를 함께 대조했다.
- 확인한 영상 내용: 갈색 골판지 조각 16개, 일부 실제 테이프와 인쇄. 모델에 사용할 부분은 무인쇄 면과 테이프만 선택한다. 사진에 보이는 인쇄 글자/기호는 재사용하지 않는다.
- 추천 무인쇄 면 crop (좌,상,우,하; 우/하 제외): **(800,565,1005,705)**.
- 최종 적용 crop: **(55,145,220,235)**. 초기 추천 영역은 축소·양자화 후 거의 단색이어서, 왼쪽 위 조각의 인쇄 없는 접힘 영역으로 변경했다. 상단의 실제 테이프/인쇄 영역은 제외했다.
- 추천 테이프 crop: **(70,55,210,115)**. 실제 테이프 사진의 농담은 유지하되 전체 모델에 무작위 마모를 더하지 않는다.
- crop은 구현 선택이며 제공자의 권장 crop이나 실측 표면 크기가 아니다. 모델 텍셀 밀도는 UV와 모델 외측 치수로 별도 계산한다.

## 상자 실물 치수 기준

- 제조사: **TART, s.r.o.**, 체코.
- 1차 자료: https://www.tart.cz/en/products/packaging-materials/cardboard-products-and-cardboard
- `Standard slotted products` 표에 **External dimensions d × w × h [mm] = 400 × 300 × 300**, 재료 `3VVL (2201B01)` 명시.
- 모델 기준: 외측 **0.400 × 0.300 × 0.300 m**. 제조사가 명시한 실제 제품 규격이며, 우리가 물건을 직접 재서 얻은 실측값은 아니다.
- 상단/하단 플랩을 테이프로 고정하는 표준 slotted box 구조도 같은 제조사 페이지에서 확인했다. 텍스처의 송장과 방향 표시는 별도의 자체 디자인이며 TART 제품 재현/상표 사용이 아니다.

## 검토 후 제외한 대안

- ambientCG Cardboard001/002/003/004: CC0이지만 자산 페이지/metadata에서 procedural 제작으로 확인되어 '실제 사진' 조건에 사용하지 않았다.
- ambientCG Paper005: approximation 제작 표기라 사진 측량 근거가 더 명확한 CardboardSet001을 선택했다.
- Poly Haven도 https://polyhaven.com/license 에서 자산 CC0를 확인했으나 적합한 무인쇄 골판지를 찾지 못해 사용하지 않았다.

## 적용 시 기록할 항목

사용 crop, 축소 해상도, 디더 옵션, 팔레트 파일, 글꼴과 최종 아틀라스의 SHA256은 제작 스크립트/manifest에 남긴다. 이 문서는 원천 파일의 출처·라이선스·치수 근거만 확인한다.
