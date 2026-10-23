# 상자 제작 후보 검사

Python 3.13 / Pillow / NumPy. 호출 예시는 `art/models/cargo_box_v01/SPEC.md`에 있다. 모든 검사가 참이면 exit 0, 실패/예외는 JSON 기록 후 exit 1.

- GLB2 바이너리를 읽어 노드 변환 적용 후 삼각형·치수·피벗 계산.
- UV 픽셀 변환 Jacobian의 특이값 두 축을 계산해 32px/m ±10% 검사. 면적 평균만 사용하면 비균일 스케일을 놓치므로 사용하지 않는다.
- PNG 팔레트 밖 색 0, POT·64px, 내장 이미지 일치, 6방향 서로 다른 UV bounds, Point sampler, 금속/노멀맵 부재.
- `art/provenance.csv`의 실제 파일 경로·SHA256·ai_generated=no·source_url/tool 존재 검사. 사람의 라이선스 확인을 대체하지 않는다.
- 기본값은 이 상자(.4/.3/.3m, 64px, 6면) 전용. 임의 모델 전체를 지원하는 범용 검사기가 아니다. Sparse accessors와 삼각형 외 primitive는 지원하지 않고 실패 처리한다.
- `negative-tests/run_negative_tests.py`: 정상 1개와 비정상 5개. 수정 전 팔레트 uint8 직렬화 오류 증거도 보존했다.

Unity 임포트 설정·출시 빌드 전체 에셋·카메라·시각적 어울림은 별도 검증이다.

## v03 단색 창 검사

`--surface-manifest atlas_manifest.json`을 전달하면 `faces[].rect_xywh` 내부의 모든 stride1 8×8 창에서 RGB색이 최소2개인지 검사한다. 거터·빈 아틀라스·옆면 색은 제외한다. 실패하면 overall FAIL, 해당 좌표/면/최소색수는 `flat_windows`에 남는다. 옵션 생략 시 `not_requested`이며 통과했다고 표시하지 않는다. v03에서는 두 후보 모두 이 옵션으로 검사했다.

`py -3.13 tools/asset-gen/test_flat_windows.py`: 단색/변화/영역경계 등9개 fixture. 검사 통과는 시각적 완성도를 보증하지 않는다(v02도 최소조건은 통과했다).
