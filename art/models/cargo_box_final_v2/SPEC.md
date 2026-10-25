# 택배 상자 최종 후보 v2 — PD 1720

이번 변경은 오른쪽 ↑↑만이다. 첫 수정의 채운 7×4 머리/짧은 줄기는 독립 평가에서 나무로도 읽혔다. 두 번째 수정은 폭7px의 열린 대각선 머리와 세로축9px로 바꿨다. `round-01/visual-gate.md`에서 방향 전달 예·상자8점. 실제 크기·UV 밀도·모델 형상은 기존 최종 후보와 같다. PNG 오른쪽43텍셀만 바뀌었고 다른5면·메시정점·UV 동일을 `preservation-proof.json`으로 확인했다.

400×300×300mm, 144 tris, 바닥 중심 피벗. GLB와 Blender 원본 동봉. 실물 규격에 맞춘 모델이며 직접 실측한 물건은 아니다.

- v03의 앞뒤 테이프 끝 입체 24 tris를 제거했다. 윗면 테이프 두께는 3mm→0.5mm, 끝은 30mm 길이의 텍스처로 표시한다.
- 오른쪽 붉은 표식은 검은 삼각 머리 ↑↑로 교체했다. 왼쪽은 깨짐주의 잔 아이콘이다. 작은 송장 105×148mm, 주소선 3개와 바코드선은 팔레트 픽셀로 그렸다.
- 사진 명암 변화량을 0.30→0.18로 줄이고 인접 팔레트 색으로 섬유층을 제한했다. 네모 얼룩의 대비는 낮아졌지만 시각 게이트에서 잔여 반복감은 별도 판정한다.
- 5mm 단일 베벨, 4mm 플랩 틈, 2.5mm 중앙 처짐과 국소 눌림 유지. 6면 UV 분리, 모든 삼각형 64px/m, 64² Point PNG, 공용 32색.
- 원천은 `art/source/photos/cargo-box/`의 ambientCG CardboardSet001 CC0 사진. `SOURCES.md`와 사진 해시, `art/provenance.csv`에 추적 정보가 있다. 생성형 이미지가 텍스처에 포함되지 않는다.
- 사진→축소→양자화/약한 디더→인쇄·마모 스탬프→단색 창만 보정→접힘/접지 AO 순서. 레이어는 `patches/`, 규칙은 `atlas_manifest.json`.

재현: 저장소 루트에서 `py -3.13 art/models/cargo_box_final_v2/build_atlas.py`, 이어 Blender `-b -P art/models/cargo_box_final_v2/generate.py -- --export-only`.

검사: `art/_review/2026-10-23-cargo-set-v2/validation/box.json`. 미리보기와 독립 평가는 같은 review 폴더. D-124의 밀도 승인은 확정; 이 최종 후보의 미리보기 승인은 별개다. Unity 실행/임포트 미실시. F1/Assets 미투입.
