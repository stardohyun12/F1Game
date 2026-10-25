# hill_house_v01

PD 20261023-1827 · D-127/D-128 · 적용 승인 대기.

| 항목 | 제작값 |
|---|---|
| 용도 | 언덕을 넘어 보이는 들판의 마지막 배달지 |
| 본체 | 7.2m 폭 × 5.8m 깊이, 기단 포함 벽 높이 3.3m |
| 높이 | 지붕마루 5.25m, 굴뚝 6.4m, 단층+지붕 공간 |
| 문 / 창 / 의자 | 0.92×2.08m / 1.05×1.25m / 좌면 높이 0.44m |
| 단위·축 | metre, Blender Z-up → GLB/FBX Y-up |
| 피벗 | 본체 바닥 중심. 오른쪽 빨래줄 때문에 전체 bounds 중심과 다름 |
| 폴리 | ≤2,500 tris. 실측 결과는 geometry.json 및 독립 check-hillhouse.json |
| 텍스처 | HillHouseAtlas.png 512², 공용 32색, Point |
| 밀도 | 전체 표면 32px/m. 모든 삼각형 UV Jacobian 검사 |
| 재료 | 사진 atlas 1장, 일반·발광창·그늘 유리·잎 4재질. Normal/Metallic 없음 |

치수는 사람 크기의 문·창·의자와 기존 건물 규격에 맞춘 설계값이다. 특정 실재 집을 직접 실측하거나 복제했다고 주장하지 않는다.

## 생활 흔적

- 바랜 크림 회벽의 국소 덧칠과 벽 하단 박락, 기단 모르타르 보수.
- 손잡이 쪽 낡은 도장과 문턱, 수리한 의자 등받이.
- 반복 기와 사진 위 굴뚝 아래 한 구간의 얇은 수리 덮개.
- 앞 처마 끝의 낮은 곡면 25개. 지붕 전체 기와를 폴리로 만들지 않고 원거리 윤곽에 필요한 끝단만 추가.
- 깊은 처마, 드러난 서까래 끝, 열린 덧창, 불 켜진 창 하나.
- 문 앞 의자와 화분 두 개, 바람 방향으로 접힌 빨래 세 장. 바람 애니메이션은 없음.

## 기존 아틀라스와의 계약

`tools/asset-gen/lofi_pilot.py`의 512²·8×8·64px 칸·2px inset을 재현했다. 기존 0~50번은 변경하지 않고 확장 51~58번을 사용한다. 셀 번호는 Blender 아래 행부터, PNG는 위부터 저장하므로 Y를 뒤집는다.

기존 2m 면에 60px UV를 다 쓰면 30px/m가 된다. 이번 모델은 한 조각을 최대 1.875m로 분할하고, 짧은 면은 UV를 부분 사용해 32px/m로 맞췄다. 원본 생성기를 바꾸지 않았다.

`art/source/hillhouse-surfaces/tiles.json`에 슬롯·출처·좌표. `merged_preview.png`는 기존 atlas에 붙일 때의 검토용이며 게임 파일이 아니다. 기존 atlas에는 구 팔레트 색이 있으므로 전체를 새 32색이라고 부르지 않는다. 이 모델의 별도 atlas와 새 8칸은 공용 32색만 쓴다.

## 재생성 / 전달

1. `py -3.13 tools/asset-gen/hill_surfaces.py`
2. `D:/Tools/Blender/blender.exe -b --python art/models/hill_house_v01/generate.py`
3. provenance 갱신 후 `py -3.13 tools/asset-gen/check_hill_house.py`
4. `D:/Tools/Blender/blender.exe -b --python tools/asset-gen/check_hill_house_fbx.py`
5. `D:/Tools/Blender/blender.exe -b --python art/_review/2026-10-23-hillhouse/render_house.py`
6. `py -3.13 art/_review/2026-10-23-hillhouse/build_review.py`

QA/프로그래밍 임포트 필요: Point, 압축 없음, 원본 512 보존, scale 1m, 발광창 한 개, 재질 tint 보존, terrain 접지/콜라이더 확인. GLB는 실제 재임포트 렌더로 확인했다. Unity 에디터·Play는 실행하지 않았다. F1/Assets 투입·commit·push 안 함.

FBX 추가 인계: image와 diffuse factor 저장을 실제 재임포트로 확인. `unlit_glass`=(0.12,0.19,0.21), `plant`=(0.42,0.60,0.20). Blender FBX importer는 이미지→Base Color 직결을 만들므로 이 기본 색을 렌더에 자동 곱하지 않는다. Unity 재질에서 **텍스처×tint** 적용을 확인해야 하며, 룩의 기준은 GLB 렌더다. FBX는 Point sampler를 이식 가능한 형태로 보장하지 않아 임포터 설정 필요.
