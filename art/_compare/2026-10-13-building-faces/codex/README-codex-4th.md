# 4차 — 건물 벽 단면 (Codex)

2026-10-13, `art/_briefs/2026-10-13-building-faces.md` 적용. 기존 D-096 키트의 아틀라스·재질·몸통 치수를 유지하고 건물 3종의 네 면과 변형을 작업했다. 새 그래픽 게이트의 합격 판정은 받지 않았다.

## 바꾼 것

- **벽면**: 집·상점·정비소의 앞/뒤/좌/우에 0.5m 석재 기단, 층 구분 띠, 창틀·창턱·창 상부 테두리, 처마 밑 그림자 띠, 모서리 쿼인을 추가했다. 집은 네 면 모두 층마다 창 2개, 상점은 전후 3개/측면 2개(정면 1층은 기존 진열창 2개와 문). 정비소는 기존 3.8m 단층 높이를 유지하여 하부 창 줄과 상부 고창 줄로 나누고, 정면 개구부는 유지했다. 빈 벽을 나누는 목적이며 실제 실내 2층을 만든 것은 아니다.
- **7개 건물 프리팹**: 집은 기본 / B(얕은 줄리엣 발코니·덧문·굴뚝·갈색 지붕) / C(덧문·굴뚝·녹색 지붕), 상점은 MART·청록 줄 차양 / B(TEA·적갈색 줄 차양), 정비소는 셔터 열림·정면 간판 / B(셔터 닫힘·측면 간판). 별도 FBX·mesh.json·prefab 및 Blender 원본을 갱신했다. 기존 상점의 깊은 차양은 0.33m로 줄였다.
- **배치**: `CoastalTownInstaller.Layout()`의 `Add(asset,...)` 한 줄에서만 `(hash/7)`로 접미사를 선택한다. 앞의 기본 종류 변수는 그대로여서 기존 `scale`, `Body()`, 문/소품 배치가 B/C를 집으로 오인하지 않는다. 실제 배치: 집 기본/B/C **47/41/47**, 상점 기본/B **10/15**, 정비소 기본/B **38/34** — 총 232동. 상세는 `building-variant-counts.json`.
- **아틀라스**: 512×512, 64px 칸, Point·무압축·mipmap 없음 유지. 기존 44칸 뒤에 `roof_clay`, `roof_green`, `awning_rust` 3칸만 추가했다. TEA는 기존 `sign_tea` 픽셀을 재사용하되 건물 간판에서만 글자 부분 UV를 사용한다. 큰 사각형 표면은 기존 2m 분할 방식을 재사용하고 새 건물 띠·차양에도 적용했다. 기존 간판의 전용 UV와 박공 삼각형 매핑은 유지한다.
- **빌드 경로**: `LofiPilotBuilder.BuildBuildingPrefabs()`는 건물 7개만 만들고 FBX와 JSON 크기·원점·UV를 비교한다. `CaptureBuildingFaces()`는 다운타운 캡처와 별도 임시 검토 씬의 근접/변형/뒷면 캡처를 만든다. 변경 없는 mesh.json의 FBX는 재수출하지 않도록 생성기에 비교를 추가하여 소품의 불필요한 FBX 재수입을 피했다.

### 참고·재현·적용 구분

- [Jacob Norris / PurePolygons의 Modular Building Breakdown](https://www.purepolygons.com/blog/modular-building-breakdown): 제작자가 설명한 공용 트림 재사용, 모서리 기둥으로 네 면 연결, 창·문·기단의 모듈화, 엔진에서 치수를 먼저 확인하는 순서를 참고했다. 원본 유료 Maya/UE 에셋은 구입·다운로드·재현하지 않았다. 이 작업에 가져온 것은 방법이며 형상·텍스처 파일은 모두 저장소 자체 코드로 생성했다.
- Houdini의 [Low Poly Building with UVs](https://procegen.konstantinmagnus.de/low-poly-building)도 대안으로 검색했지만 본문 요청이 실패해 `.hip` 제작 과정을 검증하지 못했다. 독립적으로 검증된 근거로 세지 않는다.
- **직접 재현한 기준**은 변경 전 저장소 `lofi_pilot.py`를 같은 Blender·seed 413로 별도 출력 루트에 실행한 43종이다. 출력 경로 외 조건을 바꾸지 않았고, 그 아틀라스가 작업 전 PNG와 바이트 단위로 일치했다. 외부 작품의 충실한 재현이라고 주장하지 않는다.
- 새 유료 도구·패키지·외부 모델/텍스처·라이선스 의존성은 추가하지 않았다. 사용 환경은 Blender 5.2.2 / Unity 6000.3.19f1, 기존 URP 재질 경로다.

### 실제 검증

- 브리프의 Blender 명령으로 생성 완료. `verify_building_faces.py` 결과 **73개 검사 통과**, `building-faces-geometry.json`의 `issues=[]`. 기존 **44개 칸의 모든 픽셀**, 건물 외 **40종의 형상·UV**, Awning~Boat 함수 AST가 기준과 동일하다. 추가 칸 이전 RNG 호출 순서를 바꾸지 않았다.
- 몸통 평면 치수 집 **5×4.8**, 상점 **6×5.2**, 정비소 **6.4×5.6** 유지. 모든 삼각형의 1.9~2.5m 구간을 경계 교차점까지 검사: 돌출은 표면 장식 **1~2mm**뿐이다. 전체 최대 돌출은 집 **0.26/0.32/0.26m**, 상점 **0.33m**, 정비소 **0.26m**, 모두 0.35m 이내. 지붕·굴뚝을 포함한 바닥 원점도 0이다.
- `AssetDatabase.Refresh()` → 컴파일 종료 → `LofiPilotBuilder.BuildBuildingPrefabs()` → 저장된 ComplexTrack에서 `PitCrew/Art/Coastal Town/Rebuild Layout + Install` → `CoastalTownSmoke.Run()` 실행. `building-faces-unity.json`의 7개 FBX 크기·원점·UV 검사 통과, 셰이더 오류 없음.
- 최종 `validation.txt`: 배치 **2752**, 도로 **297/297**, 도로 충돌 샘플 **1554**, 셰이더 오류 **0**, **`Issues=` 비어 있음**. `play-verification.txt`: **8 PASS + SUCCESS**(세 지점 이동/접지, 배달 지점 확인/실제 접근 완료). 최종 콘솔 오류 **0**(`building-faces-console.json`). `Assets/ComplexTrack.unity` 저장, Play 종료, compiling=False, dirty=False 확인.
- `bash scripts/ucli.sh`는 이 세션의 Python PATH 및 Windows 인용부호 전달에서 실패했다. 동일한 8090 Unity 커넥터의 `/command`에 PowerShell JSON POST로 같은 C# 명령을 보냈다. 캡처 헬퍼의 초기 `ClearSceneDirtiness` 호출은 이 Unity API에 없어 컴파일 실패했고, 임시 additive 씬을 닫는 방식으로 수정한 뒤 재컴파일/검증했다.

### 캡처와 확인한 범위

- `01_ingame_neighborhood.png`, `02_ingame_outer_road.png`, `03_ingame_harbor.png`, `04_delivery_complete.png`: 최종 스모크가 덮어쓴 플레이 화면.
- `06_downtown_center.png`: 지정한 `ArtCapture.CaptureChaseAt(path,84,60,0,1600,900)` 결과.
- `07_house_closeup.png`: CoastalHouseB 한 채의 정면·측면 3/4 근접. **마을 배치 현장이 아닌 별도 프리팹 검토 씬**이며 벽걸이 소품은 없다.
- `08_building_variants.png`, `09_building_rear_faces.png`: 7종을 같은 재질로 놓은 정면/측면 및 후면 검토. 집 지붕색·덧문·발코니, MART/TEA, 정비소 셔터·간판 위치를 직접 확인했다.
- 캡처에서 이전 빈 측면/뒷면에 창 줄·기단·층 띠가 보이는 것은 확인했다. 화면 가장자리에서 건물이 크게 잘리는 카메라 구도는 그대로이며, 모든 배치·모든 주행 방향을 시각 검사한 것은 아니다.

## 안 한 것

- **`CoastalTownDensity.cs`, `CoastalPropsBuilder.cs` 수정 없음.** 전자는 작업 시작/종료 SHA-256 동일, 후자는 시작 스냅샷과 바이트 동일. Installer는 위의 배치 에셋 선택 한 줄만 변경했다. 작업 전부터 있던 다른 사람의 수정은 되돌리지 않았다.
- Awning~Boat 소품 함수·기존 아틀라스 칸 수정 없음. 차량·게임 카메라·배달·물리 코드, 도로 폭/배치 규칙, 스타일 변경 없음. 건물 MeshCollider는 기존 빌드 방식대로 새 형상을 사용한다.
- 기본 건물 몸통의 비율·층수·창 배열을 크게 바꾸는 변형, 새 실내, 걸어 나갈 수 있는 깊은 발코니는 만들지 않았다. **같은 상자 몸통과 창 배열의 반복은 남아 있다.**
- git commit 하지 않았다.

## 못 본 것

- 목표 기기 FPS·메모리·배치/드로콜 비용·플레이어 빌드는 측정하지 않았다. 건물당 삼각형 수는 기존 **집 602 / 상점 898 / 정비소 534**에서 **집 3154~4118 / 상점 3498 / 정비소 2998~3014**로 증가했다. 상세 창틀·쿼인의 비용이며 성능이 같다고 보장하지 않는다.
- 모든 벽걸이 소품 조합과 차로 건물에 들이받는 충돌 감각은 수동 검사하지 않았다. 로컬 높이 띠의 기하 돌출과 지정 주행/배달 스모크를 확인한 범위다.
- 새로운 graphic-gate 점수나 외부 리뷰, 야간·다른 렌더 파이프라인·다른 해상도에서의 평가는 받지 않았다. 벽 분할과 변형이 추가된 결과이며 최종 아트 합격이라고 요약하지 않는다.
