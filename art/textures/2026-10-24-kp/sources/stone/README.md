# 돌포장 원본 — 2026-10-24

- 채택: [Stone Pavers](https://polyhaven.com/a/stone_pavers), Amal Kumar, [CC0](https://polyhaven.com/license).
- 직접 받은 원본: `stone_pavers_diff_1k.png` (1024×1024, 제공자 실측 2000×2000mm). 변환하지 않은 다운로드 바이트 그대로다.
- `stone_pavers_info.json`, `stone_pavers_files.json`은 공식 API 응답. 제공자 MD5와 받은 파일 MD5 일치; SHA256은 `../../source_stone.json` 참조.
- 제작 출처: [공식 FAQ](https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning)는 모든 질감이 사진 기반이며 절차적 생성 및 생성형 AI를 사용하지 않는다고 설명한다. [제작 규격](https://docs.polyhaven.com/en/technical-standards/textures)은 photogrammetry / photometric stereo를 허용한다. 개별 자산의 정확한 스캔 하위 방식은 API에 없으므로 단정하지 않는다.
- 직접 시각 확인: 약 0.65×0.5m의 큰 직사각 회갈색 석판이 어긋나 배열되고, 돌 가장자리에 닳음과 줄눈이 보인다. 교토 기준 보드 중심길의 큰 정형 석판과 대응하므로 불규칙 돌 후보보다 우선했다. 회색 색 사다리로 채도를 낮추되 원본의 돌별 명암을 보존하는 편을 권고한다.
- 64px/m·64×64를 택하면 원본의 1m 영역(512×512 원본 픽셀)을 크롭한다. 2m 원본 전체는 128×128이어야 정확히 64px/m다. 크롭 후 반복 경계 검토·수정은 별도로 필요하다.
- 미채택 비교 원본: `stone_pathway_diff_1k.png`(갈색 우세), `grey_stone_path_diff_1k.png`(회색이지만 부정형 돌이라 가장자리/골목용), `stone_tiles_02_diff_1k.png`(부정형), `slab_tiles_diff_1k.png`(부정형·짙은 흑색·갈색 줄눈). 후보의 직접 다운로드 PNG·API는 비교 근거로만 보존하며 최종 텍스처 입력으로 사용하지 않는다.
- 원본 촬영 장소는 확인되지 않았다. '교토에서 촬영'이라고 쓰지 않는다.
- 기존 LofiAtlas `quaystone` 41번 후보(부두/건물 하부 등에 공유). 새 판석을 석재 보행면에 먼저 시험하고, 현재 큰 석축 블록 용도의 벽면에는 일괄 대체하지 않는다.

## 밀도 주의

기존 64px 칸은 UV 2..62의 60px만 사용한다. 64px/m에서 이 구간은 0.9375m다. 현재 `lofi_pilot.py`의 최대 2m 분할은 단순 PNG 교체 후에도 30px/m이므로, 이번 독립 타일의 64px/m 합격과 기존 메시의 게임 내 밀도는 별개다.
