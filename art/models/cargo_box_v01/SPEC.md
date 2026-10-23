# cargo_box_v01 — 제작 후보 / 승인 전

- 제조사 TART 외측 규격 400×300×300mm. 직접 실측하지 않았다.
- 12 tris, flat normal, 바닥 중심 pivot. Blender 앞 -Y → GLB 앞 +Z. .blend/.glb 모두 모델만 포함한다.
- 64×64 RGB, 공용 32색 중 9색 사용, Closest/Point. normal map 없음, metallic 0, roughness 1.
- 앞/뒤/위/아래 13×10px, 좌/우 10×10px. 32.5~33.33px/m. 빈 아틀라스 공간은 밀도를 유지하기 위해 남겼다.
- 각 면 UV는 atlas_manifest.json에 별도 rect, 1px 복제 gutter. 앞 송장 숫자 1은 Galmuri7 기본 8px 비트맵. 10px 높이에 긴 한글 주소를 꾸며 넣지 않았다. 우측 화살표는 스크립트 기호, 위는 테이프와 플랩 이음선.
- 실제 CC0 사진의 무인쇄 접힘 영역 crop=(55,145,220,235), BOX 축소, 디더 없음. 전체 노이즈를 추가하지 않았다. 사진의 골은 재료 정보이며 인위적 마모는 바닥 접촉 모서리에만 있다.
- 기준 원화의 픽셀을 팔레트에만 샘플링. 생성 이미지를 텍스처에 굽거나 변환하지 않았다. 출처 장부: art/provenance.csv. AI 생성 여부 no는 모델·사진 기반 아틀라스에 대한 표기이며, 색 참고 원화까지 비AI라고 주장하지 않는다.
- Blender 리뷰는 단일 태양광과 동일 환경광, 5개 카메라 렌더. 렌더 조명 결과는 팔레트 제한 대상이 아니며 원본 아틀라스만 32색 검사한다.

## 재현
Python 3.13 + Pillow + NumPy; Blender 5.2.2 LTS 사용.

```powershell
py -3.13 art/source/palette/extract_palette.py
py -3.13 art/models/cargo_box_v01/build_atlas.py
& D:/Tools/Blender/blender.exe -b -t 4 -P art/models/cargo_box_v01/generate.py
py -3.13 tools/asset-gen/check_asset.py --model art/models/cargo_box_v01/cargo_box_v01.glb --texture art/models/cargo_box_v01/cargo_box_atlas_64.png --palette art/source/palette/pitcrew_d123_32.gpl --out art/_review/2026-10-23-cargo-box/check.json
py -3.13 art/_review/2026-10-23-cargo-box/negative-tests/run_negative_tests.py
py -3.13 art/_review/2026-10-23-cargo-box/build_review.py
```

텍스처 내용 변경 시 출처 장부 SHA256도 검토 후 갱신해야 한다. checker는 장부 경로·해시·비AI 표기를 검사한다. 법적 출처의 진실성이나 모든 출시 에셋을 자동 보증하는 도구는 아니다. 임포트 시 Point, 압축 없음부터 확인. QA/프로그래밍 임포트 필요.
