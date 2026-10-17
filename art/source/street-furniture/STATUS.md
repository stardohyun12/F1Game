# 도로·소품 적용 후 전체 축척 재검토 중

최신 상태: 사용자가 앞선 비교안을 승인하여 도로·벤치·쓰레기통과 길이 3.785m 차량을
메인 씬에 적용했고 기본 주행/접지/배달 검사를 통과했다. 이후 건물 등 다른 요소와의
비율 문제가 지적되어 확정 취소, 전체 축척을 다시 검토하고 있다.
실제 치수 기준의 최신 시안·캡처는 `REAL_SCALE.md` 참고. **해당 최신 시안은 아직 메인 씬 미적용.**

아래는 앞선 단계의 이력이다.

## 현재 상태

사용자가 원화를 보고 “괜찮아 보이는듯 이런식으로 넣어보자”라고 답해 제작을 승인했다.
직전 요청의 단계별 검토를 유지하여 실제 에셋을 별도 검토 씬에 만들고, 메인 씬 적용 전 보여준다.

- 승인 원화: `approved-concept.png` (imagegen 생성, 실제 게임 캡처 아님).
- 실제 Unity 렌더: `approved_props.png`, `approved_in_context.png`, `approved_road_close.png`.
- 위/옆/도로 이음매 확인: `approved_top.png`, `approved_side.png`, `approved_road_seams.png`.
- 에셋: `F1/Assets/Art/StreetFurniture/Bench.prefab`, `Bin.prefab`, `Asphalt.mat`.
- 별도 검토 씬: `F1/Assets/Art/StreetFurniture/StreetFurnitureReview.unity`.
- 제작/캡처 도구: `StreetFurniturePreview.Build()` — 기존 프리팹이나 메인 씬을 수정하지 않는다.
- 검증 로그: `preview-validation.txt`. 현재 주행 테스트나 메인 씬 적용 완료를 의미하지 않는다.
- 이전 `unapproved/`, `apply.txt`는 철회된 초안의 이력이다.

## 참고·재사용·변경

- Furnitubes RailRoad: 나무판, 철제 지지 구조와 팔걸이 조합 참고.
  https://www.furnitubes.com/street-furniture/product/railroad-inline-seat-bench/
- OMOS s16.2: 빗물 덮개와 투입구를 갖는 금속 쓰레기통의 구조 참고.
  https://omos.ie/product/s16-2-litter-bin/
- Catlike Coding Rendering 2: 축소 시 텍스처 샘플링/밉맵 원리 참고.
  https://catlikecoding.com/unity/tutorials/rendering/part-2/
- Yokohama: 벤치 배치 시 보도 폭과 사용 상황을 고려한다는 운영 사례 참고.
  https://www.city.yokohama.lg.jp/faq/kukyoku/kotsu/jidosha-eigyo/20211015145726604.html
- 외부 CAD/텍스처/코드는 내려받거나 포함하지 않았다. 브랜드 제품의 원본 재현을 주장하지 않는다.
- 기존 프로젝트 Blender → FBX → Unity 파이프라인과 조명 셰이더를 재사용했다.
- 기존 도로를 비교 기준으로 실제 Play 캡처했고, 새 도로는 반복 균열 대신 약한 골재와 밉맵을 사용한다.
- 현재 원화에 맞춰 벤치를 1.8m, 통을 .952m로 직접 설계하고 청록/나무 팔레트와 마모를 조정했다.

## 이전 적용 철회 기록

2026-10-13: 도로·벤치·쓰레기통을 제작/적용하던 중 사용자가 원화 승인 후 제작,
적용 전 별도 미리보기와 승인을 요구했다. 해당 요청에 따라 이번 적용을 철회했다.

- ComplexTrack: 작업 직전 저장한 씬 스냅샷으로 복원.
- Asphalt: BeforeAsphalt 머티리얼로 shader/properties 복원.
- Bench/Bin: 기존 LofiPilot FBX와 LofiShared 재질, 기존 방식 콜라이더로 복원.
- CoastalPropsBuilder/CoastalTownInstaller: 이번 작업에서 넣은 연동 코드 철회.
- 새 모델/재질/Editor 도구: `unapproved/`로 이동, Unity Assets 밖에 보관.
- `apply.txt`와 비교 캡처는 철회 이전의 작업 기록이며 현재 적용 상태가 아니다.
- `street_furniture.py`를 실행하면 Unity Assets에 초안이 다시 생성되므로 승인 전 실행 금지.

당시 다음 단계는 원화 검토였으며, 이후 원화 승인에 따라 위 미리보기 단계까지 진행했다.
