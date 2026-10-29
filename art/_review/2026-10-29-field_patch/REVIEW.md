# field_patch 원화 검토 — 2026-10-29

> **이 파일은 반려된 1차 도식 기록이다.** 최신 검토는 `mock_sheet.png` · `index.html` · `docs/team/reports/art-20261029-2328.md`의 게임 화면 목업 A/B. 새 목업도 도현 승인 대기이며 본제작 승인은 없다.

상태: **도현 원화 승인 필요. 에셋 제작 과제는 미완료.**

| 산출물 | 내용 |
|---|---|
| `concept.png` | 1680×1430, 축측도·평면·치수·색 기준 |
| `index.html` | 시트와 현장 캡처 링크 |
| `draw_concept.py` | Pillow 도형으로 시트를 재작성하는 코드. 이미지 생성 모델·외부 이미지 합성 없음 |
| `concept-spec.json` | 제작 예정 수치. 실제 메시 측정값 아님 |

## 무엇을 참고했고 무엇을 바꿨나

| 근거 | 확인 내용 | 적용·변경 |
|---|---|---|
| 현장 `tools/viewer/feedback/shots/unity-Map_TownV6-20261029-231130-94a3.png` | 집 오른쪽과 창고 사이에 열린 풀밭. JSONL 상태 open; 코멘트는 작은 밭 제안이고 특정 원화 승인 기록은 확인 못 함 | 배치 시 접근할 방향만 설정. 현장 좌표·회전·경사 추정값으로 씬을 수정하지 않음 |
| [UMN Extension: Raised bed gardens](https://extension.umn.edu/garden-and-home/yard-and-garden/gardening-in-minnesota/raised-bed-gardens) | 나무 틀 없이 흙만 높이는 이랑, 중앙에 손이 닿는 폭, 통로를 밟고 작업하는 원칙을 설명 | 높이 0.19m 흙 두둑, 폭 0.85m, 고랑 0.55m. 구체 치수는 이 프로젝트 제안이며 원문 그대로 재현한 수치 아님 |
| [타키이: 피망 가정재배 도해](https://www.takii.co.jp/tsk/kateisaien/piman.html) | 이랑·주간 간격과 지지대·유인 도해를 제공 | 집과 시야를 가리지 않도록 높은 작물을 1줄로 제한하고 1.35m 지지대로 단순화. 재배 시험은 하지 않음 |
| `tools/asset-gen/house_home.py` 및 `tools/art/build_asset.py` h_source/h_grain/h_quant | 면·UV·낱개 피벗을 생성기로 만들고, 사진 재료 결을 입힌 뒤 전용 팔레트로 재양자화하는 기존 경로 확인 | 같은 생성기 구조 재사용 예정. 집 생성기 재실행·기존 에셋 변경은 하지 않음. 기존 PASS는 activeContext의 기록이며 이번 세션 재현 결과가 아님 |
| `TASTE.md`, `TONE.md`, 지브리 문법 연구 | 맑은 공기, 생활감, 빈 공간, 진한 자연과 바랜 인공물 | 낮은 작물 3줄·빈 흙 1줄·높은 작물 1줄, 물뿌리개와 호미, 열린 집 쪽. 후기 수확의 적은 열매는 아트 해석 |
| [Poly Haven 라이선스](https://polyhaven.com/license) | 제공 에셋은 CC0이며 상업 사용·변형 가능. 사이트 글·렌더 전체가 CC0인 것은 아님 | 제작 때 원본 albedo만 사용하고 파일별 URL·해시·가공을 provenance.csv에 기록할 예정. 이번 시트에 사진을 사용하거나 신규 외부 파일을 다운로드하지 않음 |

사진 재료 후보: `art/source/q1-room-cc0/fine_grained_wood.jpg` 존재 확인. 돌 후보는 `art/export/scene-a6-photo-kit/provenance.csv`에 기록된 원본을 대조할 예정. 흙·잎 재료 선택과 개별 파일 라이선스/해시 검증은 제작 전 남은 작업. 기존 bake 완료 텍스처를 albedo로 재사용하지 않는다.

## 제작 사양 제안

- 전체 12×8.8m, 14×10m 제한 안. 지표면 중심 피벗, 흙 스커트 -0.30m, 두둑 +0.19m.
- 7.4m 이랑 5개: 낮은 잎채소 3줄 / 빈 이랑 1줄 / 고추형 지지대 작물 1줄. 낮은 잎은 0.30~0.40m, 지지대 상단 1.35m.
- 부분 울타리 높이 0.62m, 집 쪽 진입을 열어 둠. 디딤돌 4개, 물뿌리개·호미 각 1개. 소품별 이름·바닥 피벗.
- 지형·작물·소품 합계 목표 ≤6,000 tris, 개별 소품 목표 ≤500 tris. 원통은 ≥16면. **실측값 없음.**
- 집 쪽은 -X 기준, 납품 때 Unity Y-up으로 변환. 맵에서의 실제 회전은 도현 배치 단계.
- 0.3m 스커트는 작은 지면 높이차를 가리는 용도이며 전체 비탈 적합성을 보장하지 않음. 실제 배치 시 네 귀퉁이 접지·돌출을 별도로 확인해야 함.
- B안은 잎채소 1줄을 제거한 4줄 구성. 별도 시트·제작은 아직 안 함.

## 승인 후 연결할 경로 (아직 수정하지 않음)

1. `tools/asset-gen/field_patch.py` → `art/export/field-patch/`: FBX·layout·measure·전용 albedo/palette·provenance.
2. `tools/art/build_asset.py`에 CFG와 전용 단계 추가. 보관소용 split/snap 단계가 다른 에셋을 건드리지 않도록 그대로 상속하지 않음.
3. `tools/art/check_rules.py`의 등록 테이블과 `tools/viewer/buildings.json`에도 field_patch 등록 필요. 현재 도구는 build_asset 항목만 추가해서 끝나지 않음.
4. F1Studio 전용 import 코드는 field_patch 1개만 갱신하도록 구현. 현재 `F1Studio/Assets/Art/Library`는 본 `F1/Assets/Art/Library`로 연결된 정션임을 실측 확인. 원화/미리보기 승인 전에는 최종 Library 투입 안 함; 미리보기는 실제 F1Studio 로컬 `Assets/Art/Review/field_patch`에 분리할 것.
5. 실제 요청 명령: `python -X utf8 tools/art/build_asset.py field_patch`, `bash tools/art/bin/check.sh field_patch`, `bash tools/art/bin/viewer_export.sh field_patch`, `bash tools/art/bin/studio_cap.sh field_patch 4 calm`.
6. 마지막 미리보기 승인 뒤 최종 `F1/Assets/Art/Library/Props/field_patch/`로 승격. 맵 배치는 도현 단계.

## 실제 검증·한계

- 도식 생성 실행 성공. PNG를 열어 한글·다섯 이랑·빈 흙·부분 울타리·소품 표시 확인.
- 첫 도식의 지면이 뒤쪽 작물을 덮는 그리기 순서 결함을 수정하고 재출력·재확인.
- 원본 PNG 캡처 직접 열어 현장 범위 확인. HTML은 정적 링크 점검만; 브라우저 렌더는 미실행.
- 생성형 이미지·소리 도구 미사용. Unity·Blender·게임 빌드·규칙 검사·뷰어 export·스튜디오 캡처 모두 미실행.
- 그래픽 게이트 PASS를 주장하지 않음. 도식은 실제 재질·빛·작물 가독성의 검증 자료가 아님.

승인 근거: `project_rules/asset.md`의 “사용자가 원화를 승인한 뒤에만 모델·텍스처 등 실제 에셋을 제작한다.” 이번 요청에도 원화→도현 승인→제작 순서가 명시되어 있음.
