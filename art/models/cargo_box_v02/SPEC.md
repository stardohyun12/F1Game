# cargo_box_v02 — 적용 승인 전 제작 후보

v01은 보존하고 별도 제작했다. PD 20261023-1520 지시와 D-123 브리프 규칙 1~11, research-20261023-1458을 읽었다.

| 항목 | 실제 구현 |
|---|---|
| 치수 | 제조사 TART 외측 규격 400×300×300mm. 직접 실측 아님 |
| 형상 | 168 tris, 5mm 베벨 1단, 덮개 2장·중앙 2mm 틈, 3mm 돌출 테이프·양 끝 접착부 |
| 눌림 | 앞 왼쪽 상단에 X 5 / Y 3 / Z 3mm 국소 변형 |
| 좌표 | m, 바닥 중심 pivot, glTF Y-up. Blender 앞 -Y → glTF +Z |
| UV | 6개 주면 별도 영역, 베벨·테이프 별도 단색 패치. 모든 삼각형 32px/m 기준 |
| 질감 | 기존 CC0 사진의 무인쇄 접힘 crop(55,145,220,235), BOX 축소, 디더 없음, 공용 32색 |
| 정보 | 앞 송장 바코드·주소선, 오른쪽 방향 기호, 왼쪽 잔 기호. 모두 스크립트 픽셀 |
| 글자 제한 | Galmuri11 12px ‘조심’ 시안 `label-font-candidate_NOT_APPLIED.png`는 검토용만. 32px/m 작은 송장에는 읽히게 들어가지 않아 모델 미적용 |
| 재질 | Closest, roughness1, metallic0, normal map 없음 |
| 마모 | 눌린 상단 모서리·바닥 접지면. 전역 랜덤 노이즈 추가 없음 |
| provenance | art/provenance.csv의 cargo_box_v02_atlas. 실제 사진+스크립트; 생성 이미지 텍스처 미사용 |

송장은 여전히 낮은 밀도 때문에 바코드가 굵은 줄로만 보인다. 32px/m 기준을 임의로 바꾸거나 ‘조심’이 실제 모델에 들어갔다고 보고하지 않는다. 고밀도 정보층 여부는 질문 중이다.

## 검증과 재현

```powershell
py -3.13 art/models/cargo_box_v02/build_atlas.py
& D:/Tools/Blender/blender.exe -b -t 4 -P art/models/cargo_box_v02/generate.py
py -3.13 tools/asset-gen/check_asset.py --shape beveled-box --max-tris 300 --model art/models/cargo_box_v02/cargo_box_v02.glb --texture art/models/cargo_box_v02/cargo_box_atlas_64.png --palette art/source/palette/pitcrew_d123_32.gpl --out art/_review/2026-10-23-cargo-box-v02/check-v02.json
py -3.13 art/_review/2026-10-23-cargo-box-v02/build_review.py
```

기존 cube UVMap을 제거한 후 새 UVMap 작성. 눌린 주면도 축투영하고 실제 삼각형 두 주축 밀도를 검사한다. 검사기는 베벨 모드에서 ±축별 최대 주면 6개가 존재하고 UV 영역이 겹치지 않는지 확인한다. 기본 box 모드는 v01 회귀 유지.

Blender 5.2.2, Python3.13/Pillow/NumPy 사용. render-proof.json과 montage-proof.json에 조명·합성 위치 기록. 주변광과 같은 색을 해무에 2.5% 혼합했다. 원화에 맞춘 126×114px 합성은 외관 비교용이며 실제 추적 카메라/차량 적재 검증이 아니다. 원화·합성 PNG는 모델이나 GLB에 포함하지 않았다.

## 근거·범위

- [FEFCO 공식 구조 분류](https://www.fefco.org/technical-information/fefco-code): 플랩식 골판지 상자 구조 참고. 본 모델의 mm 단위 베벨/눌림은 제작 선택이며 FEFCO 실측 주장 아님.
- [Blender Bevel 문서](https://docs.blender.org/manual/en/5.0/modeling/modifiers/generate/bevel.html): 한 세그먼트 베벨을 bpy로 적용. 내부 단순 큐브에서 재현하고 플랩/테이프 조립으로 확장.
- 사진·치수: `art/source/photos/cargo-box/SOURCES.md`; 폰트: `art/_review/2026-10-23-d123/fonts/LICENSE.txt`와 SOURCES.md.
- QA/프로그래밍 임포트 필요. F1/Assets 미투입, Unity/Play/commit/push 미실행.
