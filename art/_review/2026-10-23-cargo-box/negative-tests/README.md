# 검사 실패를 확인하기 위한 비정상 fixture

**이 폴더의 PNG/GLB는 유효 에셋이 아니다. 게임에 임포트하지 않는다.** 원본을 복사·변형한 회귀 검증 데이터이며 신규 화물 제작물이 아니다.

`py -3.13 art/_review/2026-10-23-cargo-box/negative-tests/run_negative_tests.py`로 재현한다(Pillow + NumPy 필요; 이 PC의 기본 Python 3.14에는 NumPy가 없었음). 원본 모델/텍스처는 읽기만 하며 실행 전후 SHA-256 동일 여부를 검사한다.

- baseline: 원본 통과 확인
- outside_palette: 지정 팔레트 밖 픽셀 한 개 삽입
- non_power_of_two: 63×64로 크롭
- wrong_target_density: 정상 32px/m 모델을 64px/m 기준으로 검사
- triangle_budget_10: 정상 12 tris 모델을 10 tris 예산으로 검사
- nonuniform_scale: 상위 GLB 노드 X×2/Y×0.5 변형; 앞뒤 면의 UV/물리 면적비는 유지되지만 두 방향의 픽셀 크기는 불일치

각 실행 JSON 및 통합 `test-results.json`에 실제 반환 코드와 실패 항목을 보존한다. 비균일 스케일 검사는 치수 옵션을 변형 치수로 맞춰서 밀도 검사가 독립적으로 실패하는지 확인한다.

첫 실행에서 팔레트 밖 색의 NumPy uint8 값을 JSON에 직렬화하지 못하는 결함을 발견했다. `pre-fix-results.json`은 수정 전 증거이며 최종 재실행 결과는 `test-results.json`으로 구분한다.
