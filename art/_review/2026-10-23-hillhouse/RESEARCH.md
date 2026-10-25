# 조사 → 재현 → 적용 → 개선

2026-10-23. 이 집은 실재 건물 복제가 아닌, 실제 크기로 설계한 게임용 시골집.

| 근거 | 확인한 사실 / 가져온 것 | 적용·변경 |
|---|---|---|
| [HABS IA-193 원자료](https://tile.loc.gov/storage-services/master/pnp/habshaer/ia/ia0400/ia0414/data/ia0414data.pdf), pp.2–6 | 실제 소형 주택의 덧댄 외장, 현관 증축, 창·문틀, 처마·노출 서까래, 내부 굴뚝, 보도 포장 보수 기록 | 덧댄 흔적이 생활의 시간이라는 원리만 참고. 미국 주택 양식·평면·16×50ft 치수는 복제하지 않고 기존 해안 마을과 호환되는 7.2×5.8m 단층으로 설계 |
| `tools/asset-gen/lofi_pilot.py:190` | 기존 512/64/8칸, 2px inset, 2m 면 분할, flat shade | 동일 칸 인덱스와 gutter 재현. 원본의 60px/2m=30px/m를 60px/1.875m=32px/m로 보정. 원본 파일은 수정하지 않음 |
| 설치된 Blender 5.2 glTF exporter `blender/exp/material/search_node_tree.py` get_multiply_factors | 구 MixRGB의 상수 곱은 이 버전의 glTF 색 계수 탐지에서 빠짐. 새 Mix/RGBA/MULTIPLY를 탐지 | 최초 GLB 재임포트에서 어두운 유리 색이 빠진 것을 발견. 지원되는 Mix 노드로 바꾸고 내보낸 GLB를 다시 렌더 |
| `art/source/hillhouse-surfaces/SOURCES.md` | Poly Haven의 실제 사진 diffuse·CC0·제공 실제 크기 | 실제 크기 기준 2m 구간 준비 → 기존 photo_to_pixel → 공용 32색 → 국소 수리·글자·손때 층 |

HABS 일반 갤러리 페이지는 403, Blender 웹 매뉴얼 페이지는 도구 접근 오류로 본문을 읽지 못했다. 대신 접근 가능한 HABS 원문 PDF와 실제 설치된 Blender exporter 코드를 읽었다. 검색 결과만으로 제작법 검증을 대신하지 않았다.

검증은 내보낸 GLB의 실좌표·UV, FBX 재임포트, CC0 출처/해시, 30/45/60m 카메라, 독립 이미지 판정으로 나누었다. AI는 `concept.png`와 `concept-empty.png` 원화/구도 두 장뿐이고 모델 텍스처는 실제 CC0 사진과 스크립트 픽셀이다.

추가 실패·수정: FBX exporter는 같은 Mix 노드에서 텍스처를 찾지 못했다. 설치된 `io_scene_fbx/export_fbx_bin.py`의 diffuse color/texture 수집을 확인하고 FBX 내보내기 직전에만 image 직결 + socket diffuse factor로 펼쳤다. FBX 재임포트에서 image 연결과 저장 factor 복구를 확인했으나, Blender importer 그래프는 tint 곱셈을 생략하므로 Unity에서 곱셈 여부는 별도 확인한다. GLB 형상·재질·해시는 이 수정 전후 동일.
