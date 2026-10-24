# 코스 표지 — 실제 사진 원본

2026-10-23. [Rusty Painted Metal](https://polyhaven.com/a/rusty_painted_metal), Amal Kumar / Poly Haven. [CC0 안내](https://polyhaven.com/license).

- `rusty_painted_metal_diff_1k.png`: 1024×1024 diffuse 원본. 픽셀 재질 입력.
- `rusty_painted_metal_context.jpg`: 실물 현장 사진, 출처 확인용. 게임 에셋 입력 아님.
- 다운로드 URL·날짜·용량·SHA256: [sources.json](sources.json).

제작 파이프라인: diffuse crop → BOX64×64 → `photo_to_pixel.quantize_exact` 공용32색·무디더 → 녹/마모 국소 마스크 → 폰트 글자. 출력·재현 스크립트는 `art/_review/2026-10-23-course-ui/`. 도현 승인 전 Assets 투입 금지.
