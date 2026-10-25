# cargo_box_v03 — 32/64px/m 검토 후보

두 후보는 같은 400×300×300mm·168 tris 형상을 사용한다. **64px/m 예외는 도현 승인 대기**이며 두 후보 모두 엔진 미투입이다. v01/v02는 보존했다.

| 항목 | 구현 |
|---|---|
| 형상 | 1단 5mm 베벨, 덮개 중앙 4mm 틈, 안쪽 2.5mm 처짐, 한 모서리 5/3/3mm 눌림 |
| 테이프 | 기존 돌출 3mm 유지, 300mm 길이에서 7.5mm 비스듬한 부착 |
| 폴리 | 168 tris, 추가 텍스처 디테일용 폴리 증가 없음 |
| 스케일 | TART 제조사 외측 규격, 직접 실측 아님. 바닥 중심 pivot, glTF Y-up |
| 송장 | 설계 105×148mm, 면 폭 26.25%. 실제 픽셀 32판3×5 / 64판7×9, Point에서 반올림 오차 있음. 글자 대신 허용된 바코드·주소선 |
| 팔레트 | 기존 공유 32색 PNG/GPL/JSON 재사용 |
| 텍스처 | 두 판 모두 64×64 아틀라스. 주면13×10/10×10 또는26×19/19×19, 여섯 면 별도 UV |
| 샘플링 | Closest / GLB NEAREST. 모든 삼각형 UV 밀도 검사 |
| AO | 접지·접힘에 팔레트색으로 구운 단계형 AO. 버텍스 컬러 AO는 최신 체크리스트7의 보류 방침 적용; Unity Lit 지원 미검증 |
| 변주 | 생성기 --instance-seed에서 치수±2%, atlas --seed에서 원천 crop 이동. 비교본은 둘 다 seed0. 다른 seed 모델/게임 인스턴스 검증은 하지 않음 |
| 출처 | ambientCG CardboardSet001 사진측량 color map + 스크립트. art/provenance.csv에 두 후보 별도 행 |

## 층과 재현

`build_atlas.py`: CC0 사진 → BOX 축소 → 공유 팔레트 정확 양자화 → 면별 명도 사다리/약한 4×4 Bayer → 규칙 스탬프 → 단색8×8창에만 성긴 보충 픽셀 → 접지/접힘 AO. 단계별 PNG는 각 density 폴더 patches에 있다. 00_downscaled는 중간 사진으로 게임 텍스처가 아니다. 최종 `_05_final.png`와 atlas만 팔레트 제한을 만족하는 제작 후보다.

보충 픽셀은 기존 점과 거리를 벌리는 결정적 배치다. 개발사 Pixel8r/블루노이즈 구현을 그대로 재현했다고 주장하지 않는다. 이번32판은 해당 단색 창이 없어 0픽셀 추가,64판은15픽셀 추가했다. 전체 랜덤 마모를 뿌리지 않았다.

```powershell
py -3.13 art/models/cargo_box_v03/build_atlas.py --density 32
py -3.13 art/models/cargo_box_v03/build_atlas.py --density 64
& D:/Tools/Blender/blender.exe -b -t 4 -P art/models/cargo_box_v03/generate.py -- --density 32
& D:/Tools/Blender/blender.exe -b -t 4 -P art/models/cargo_box_v03/generate.py -- --density 64
```

검사 예시(32판은 경로와 density를32로 바꾼다):

```powershell
py -3.13 tools/asset-gen/check_asset.py --shape beveled-box --max-tris 300 --density 64 --model art/models/cargo_box_v03/density64/cargo_box_v03.glb --texture art/models/cargo_box_v03/density64/cargo_box_atlas_64.png --palette art/source/palette/pitcrew_d123_32.gpl --surface-manifest art/models/cargo_box_v03/density64/atlas_manifest.json --out art/_review/2026-10-23-cargo-box-v03/validation/check-64.json
py -3.13 tools/asset-gen/test_flat_windows.py
py -3.13 art/_review/2026-10-23-cargo-box-v03/build_review.py
```

`check_asset.py --surface-manifest`는 선언한6개주면 내부만 stride1로8×8 창을 전수 검사한다. 빈 아틀라스·거터·이웃면 색으로 단색 면을 통과시키지 않는다. 추가플래그 없는 기존 모델은 `not_requested`를 명시한다. 이 검사는 최소 색수뿐이며 인위적 얼룩·가독성·출처 진실성을 판단하지 않는다.

## 검토 장면 한계

같은 원화 위 같은126×114px 크기에 합성한다. 같은 조명·해무·접촉 그림자·전경 마스크를 사용한다. BOX로 정확히480×270으로 줄인 뒤 Point2×/3×를 별도 저장. 원본 이미지의 불규칙한 픽셀을 진짜 격자로 간주하지 않는다. 이 합성은 Unity 카메라·후처리·실제 접촉·고정·움직임 검증이 아니다.

독립 게이트:32판7/10,64판8/10. 남은 결함:두꺼운 테이프 끝,붉은 표식 오독,네모 얼룩. 64밀도 채택과 적용은 도현 승인 필요. QA/프로그래밍 임포트 필요.
