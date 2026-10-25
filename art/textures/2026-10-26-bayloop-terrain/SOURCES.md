# CC0 원천 — 2026-10-26 직접 다운로드

Poly Haven 원문: https://polyhaven.com/license — 에셋 모두 CC0. 제작 방법 FAQ: https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning . 원본 사진 diffuse, 생성 AI 없음. 실제 다운로드 주소·API 치수·작가·MD5는 `sources/*_metadata.json`; 파일 SHA256과 확정 사용 출처는 `manifest.json`.

| 사용 | 원본 | 실측 치수 |
|---|---|---|
| 풀 | https://polyhaven.com/a/sparse_grass | 2000×2000mm |
| 흙 | https://polyhaven.com/a/dirt | 2000×2000mm |
| 마른 풀 | https://polyhaven.com/a/withered_grass | 약2000×2000mm(API 부동소수 오차) |
| 바위 비탈 | https://polyhaven.com/a/rock_face_04 | 2000×2000mm |

비교 후 미사용: https://polyhaven.com/a/leafy_grass , https://polyhaven.com/a/grass_ground , https://polyhaven.com/a/grass_path_2 . 후보 원본/metadata는 sources에 보존, 최종 PNG/아틀라스에 포함하지 않음. 위치·특정 지역의 암석으로 주장하지 않음.

검증된 기존 `photo_to_pixel.py`의 BOX→정확 팔레트 양자화와 A1의 metric GLB 제작을 재사용. 원본 재현은 `baseline/`이며 추가 색조 변화 없이 그대로 최종 채택. `bayloop_terrain_proof.py`는 기존 `kp_texture_preview.py`의 앞쪽 metric proof 부분만 추출한 것(고정 목재/회벽 전시 렌더는 제외). AI 배경·사진 합성 생성 없음.
