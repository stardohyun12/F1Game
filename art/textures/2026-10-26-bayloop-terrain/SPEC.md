# BayLoop 지형 4종 — 20261026-0131

| 역할 | 파일(albedo/) | 실측 반복 | 스캔 원천 | 적용 제안 |
|---|---|---|---|---|
| 평지 풀 | BL_Terrain_grass_32ppm.png | 64×64px / 2×2m | sparse_grass | 평지/완경사, 짙은 올리브 풀 |
| 흙 | BL_Terrain_soil_32ppm.png | 동일 | dirt | 풀 사이 닳은 흙·길 가장자리 |
| 마른 풀 | BL_Terrain_dry_grass_32ppm.png | 동일 | withered_grass | 햇볕 드는 비탈 |
| 급경사 바위 | BL_Terrain_rock_32ppm.png | 동일 | rock_face_04 | 급경사 노출 암반 |

공용 `pitcrew_d123_32` 팔레트, 원본 diffuse 전체 BOX 축소→직접 RGB 최근접 양자화, 디더 없음. 전역 노이즈·합성 무늬·AI 없음. 풀만 원본 후보4종을 비교: leafy_grass는 축소 후 녹색이 황토색으로 사라짐, grass_ground는 큰 얼룩, grass_path_2는 노출 자갈 비중이 커서 제외. sparse_grass가 실제 풀색/흙을 가장 조용히 남김. 팔레트 녹색 자체가 어두워 풀도 어두움. 기준판 `baseline/`과 납품 albedo는 동일; 색 재도장·경계 땜질 없음.

## 기존 지형 연결점(정적 확인)

- `F1/Assets/Editor/BuildBayLoop.cs:139–140`: `TerrainMesh`와 `FarTerrainMesh`가 `M("Ground")` 사용.
- 같은 파일 `M` 함수: `Ground` = **`F1/Assets/Art/CoastalTown/Ground.mat`**. 셰이더 GUID = `132a233558da53d4cb95e8a1895539ae` = **`F1/Assets/Art/CoastalTown/WorldSurface.shader`**, 이름 `PitCrew/CoastalTown/WorldSurface`.
- 현 Ground.mat: `_MainTex` 기존 아틀라스, `_Tile=(.625,.125,.125,.125)`, `_Tile2=(.25,.625,.125,.125)`, `_Patch=1`, `_PatchScale=.09`, `_PatchThreshold=.56`. 이 값은 자료 조사이며 수정 안 함.
- 이 셰이더는 `_MainTex` **한 장**과 `_Tile`/`_Tile2` **두 칸만** 지원. `_GrassTex`, `_RockTex`, TerrainLayer 슬롯은 없음. `_Patch`는 큰 value-noise 이진 마스크이며 경사 혼합 기능이 아님. 새 사진만 넣어도 기존 큰 위장 얼룩이 자동 해결되지 않음.
- `BL_TerrainAtlas_32ppm.png` = 128×128, 64px 네 칸. Unity UV 원점 아래 기준: grass=(0,.5,.5,.5), soil=(.5,.5,.5,.5), dry_grass=(0,0,.5,.5), rock=(.5,0,.5,.5). `_MainTex`에 이 아틀라스, `_Tile`/`_Tile2`에 해당 좌표를 사용. 단일 PNG 직접 시험은 `_Tile=(0,0,1,1)`, `_Patch=0`.
- **밀도 주의:** 현재 셰이더가 `.035 + frac(worldUV * _WorldScale) * .93`로 내부 93%만 표본화. 64px 전체를 2m로 쓰는 보통 UV(증명 GLB)는 정확32px/m. 현 셰이더에서 `_WorldScale=.5`는 **29.76px/m**(허용±10% 안), 정확32라면 **32/(64*.93)=.5376344**. 이는 원본2m의 가운데1.86m만 사용하므로 반복 경계 잘림은 인게임에서 확인 필요. 가장 바른 향후 수정은 타일 내부 패딩식·세계 반복을 함께 조정해2m의64px를 모두 쓰는 것.
- 월드투영은 `abs(normal.y)>.5`면 XZ, 아니면 ZY/XY. 실제 경사면 표면거리 밀도는 투영으로 줄어들 수 있어 **GLB 쿠폰 밀도와 실제 경사면 밀도는 같다고 주장하지 않음**. 경사 면별 UV 또는 삼면투영 개선 필요.
- 지형 하위재질 `LotMats`와 GroundKind가 섞여 있어 Ground만 바꾸면 모든 땅이 바뀌지는 않음. 별도 생성 단색 Meadow/FieldA/FieldB도 후속 점검 필요.

## 프로그래머 적용 제안(미실행)

Point, Repeat, 무압축, sRGB albedo, 최대해상도 원본 이상. 시험 재질은 기존 공유 Ground를 덮어쓰지 않고 복제해 연결. 첫 비교는 `_Patch=0`으로 사진 자체 반복을 확인. 풀↔흙은 실제 통행 흔적 마스크, 마른 풀/바위는 법선 기반 경사 구분; 임계각은 게임 캡처로 조정. 4종 동시 혼합은 셰이더 또는 지형 하위재질 로직 변경이 필요하며 아트는 코드/씬을 수정하지 않았음.

## 확인

`check_asset.py --texture-manifest .../manifest.json --provenance .../provenance-test.csv --out .../terrain/checks.json`: **4/4 통과**, 각2tris proof GLB의 양 주축 **32.0px/m**, 원본 albedo 팔레트 밖0, Point/Repeat, 내장PNG 원본 일치, 출처·해시 통과. 32색 검사는 **원본 albedo**에 한정하며 조명/안개 합성 화면의 색은 검사하지 않음. 검토판 하단은2×2 반복. Unity 임포트·실제 경사/멀리서의 앨리어싱·마스크 연결 미실행, QA/프로그래밍 임포트 필요.
