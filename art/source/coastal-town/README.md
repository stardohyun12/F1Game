# Coastal Town — 밀도·도로 디테일 2차 (Claude, 2026-10-13)

브리프 `art/_briefs/2026-10-13-town-density.md` 를 Codex 대신 Claude 가 수행한 결과. 출발점은 커밋 `21c7782`(Codex 의 1차 설치, 417 배치).

## 바꾼 것
- **소품 16종 추가** — `tools/asset-gen/lofi_pilot.py` 에 함수만 추가(기존 칸·rng 순서 보존, 아틀라스 칸 8개 신설: awning·sign_tea/ice/rest·zebra·grate·terracotta·cloth). 차양(Awning)·화분(Planter)·화분3개(Pots)·벤치·드럼통·실외기·벽등·빨랫줄·간판 3종·볼라드·상자더미·횡단보도·맨홀·배수구. 전부 1~120 삼각형.
- **프리팹** — `CoastalPropsBuilder.cs`(메뉴 Coastal Town/Build Prop Prefabs). 바닥 소품 MeshCollider, 작은 것 BoxCollider, 데칼·벽걸이는 콜라이더 없음.
- **배치 규칙** — `CoastalTownDensity.cs`. 건물당: 앞(보도) 2~3개 + 옆(도로·건물 없는 쪽) 1~2개 + 벽걸이(집은 창 위 차양, 전부 실외기·벽등) = 4~6개. 집마다 hash 로 조합이 다름. 도로: 교차로 팔마다 횡단보도, 직선 셀에 맨홀(1/3)·배수구(1/2). `CoastalTownInstaller.Layout()` 에서 호출, 메뉴 `Coastal Town/Rebuild Layout + Install`(레이아웃 캐시 삭제 후 설치).
- **설치 결과** — 배치 417 → **2053**, 도로 297/297 연결, 바닥 샘플 1,554 이상 없음, 셰이더 오류 0 (`validation.txt`).
- **스모크 수정 2건** — ① 배달 완료 FAIL 은 더미 런이 route 를 남겨 라벨이 무시되던 테스트 버그 → 라벨로 ActivateRoute. ② 에디터가 뒤에 있으면 Play 가 frame 1 에서 멈춰 차가 스폰 공중(바닥 0.34)에 뜬 채 판정이 틀어짐 → `Application.runInBackground=true`. 결과 8/8 SUCCESS.

## 안 한 것 / 못 본 것
- 톤 통일(브리프 ③): 플레이 캡처 3곳(골목·외곽·항구)에서 Kenney 고층은 안 보였고 건물은 전부 로파이 키트였다. **다운타운 중심부는 따로 캡처하지 않았다** — 거기 남은 게 있는지는 미확인.
- 소품이 보도 위에 놓이므로 길 가장자리 0.5m 는 띄웠지만, **차로 들이받았을 때의 충돌 느낌(벤치·드럼통 MeshCollider)은 시험하지 않았다.** 차 폭 0.9m, 차도 폭 3.4m 는 그대로.
- 교차로 횡단보도는 팔마다 하나씩이라 4거리에서 겹쳐 보일 수 있다(01 캡처 앞쪽). 간격·크기 조정은 판정 뒤.
- 빨랫줄은 집 옆면에만, 상점·정비소엔 없음. 밤/다른 무드는 없음.
- 성능(FPS)·빌드는 측정하지 않았다. 배치 수가 5배가 됐다.

## 캡처
- `before_0*.png` = Codex 1차(커밋 21c7782 시점), `0*.png` = 이번. 같은 자리·같은 스모크 카메라. 비교 한 장 `claude_vs_codex.png`.
- 판정은 `docs/gates/2026-10-13.md` (graphic-gate / verifier).

---

# 3차 — 바닥·원경 (Claude, 2026-10-13 저녁)

2차 그래픽게이트(5/10)가 짚은 두 레버: **바닥**(연석·얼룩·균열, 보도의 가로수·가로등·가판·쓰레기통, 잔디 변화)과 **원경**(땅 끝이 하늘과 바로 만남, 바다·잔디 경계). 출발점 커밋 `906f899`.

## 바꾼 것
- **소품 15종 추가**(`lofi_pilot.py`, 아틀라스 칸 7개: curb·dirt·sand·flowers·stain·quaystone·grass2): 연석·가로등·쓰레기통·가판·소화전·풀포기·덤불·바위·꽃밭(데칼)·기름얼룩(데칼)·땜질(데칼)·석축 12m·계류 말뚝·잔교·배. 12~484 삼각형. 프리팹은 `CoastalPropsBuilder`(31종).
- **도로 규칙**(`CoastalTownDensity.ForRoads`): 연석 = 도로 셀의 도로 아닌 변마다(350개, 보도 안쪽 모서리 1.76m, 충돌 없음). **횡단보도 643→47** — 2칸 폭 도로는 셀마다 이웃이 3이라 전부 "교차로"로 잡혀 깔리던 걸 "직선 셀이 교차로로 들어가는 끝"에만. 방향도 고침(3.4m 가 차도 폭을 가로지르고 줄무늬는 도로 방향). 얼룩 1/4·땜질 1/5 셀. 보도 위 가로등(6셀 주기, 양쪽 엇갈림)·쓰레기통·소화전.
- **빈 마당**(`ForGround`): 도로 아님·건물 없음 셀마다 2~4개 — 다운타운 꽃·풀·덤불 / 공업 바위·드럼통·상자 / 항구 바위·상자·풀. 도로 옆 빈 셀 셋 중 하나에 가로수. 바다 행(z≥120)은 제외.
- **구역 바닥**(`CoastalTownInstaller`): 공업=흙(풀 덩어리) / 다운타운=잔디(짙은 잔디 덩어리) / 항구=모래(흙 덩어리). 덩어리는 `WorldSurface.shader` 의 새 `_Patch/_Tile2` — 월드 좌표 값노이즈(11m·4m 두 겹)를 계단식(step)으로 잘라 두 번째 칸을 섞는다. 경계는 2칸 폭 도로 밑에 숨는다. `_Patch=0` 인 기존 재질은 그대로.
- **원경**: `FarGround`(1400×780m, y −0.06, 충돌 없음, 바다 쪽 z>120 은 안 덮음) + 언덕 13개(서·남·동, 반경 62~70m·높이 14~22m, 안개색) + 마을 밖 나무 띠(10~75m, 9m 격자 중 2/3, 약 500그루).
- **부두**(`ForHarbor`): 석축 16조각(z 119.4, MeshCollider — 차가 바다로 못 떨어짐), 계류 32, 잔교 3(x 36·90·144, 바다로 14m), 배 5~7, 잔교 옆 상자·드럼통.
- 설치 결과: 배치 **2752**, 도로 297/297, 바닥 샘플 1,554 이상 없음, 셰이더 오류 0. 스모크 8/8.

## 안 한 것 / 본 것
- **다운타운 2칸 폭 도로 = 12m 빈 아스팔트**(06 캡처). 중앙 분리대·주차선·가로수 섬이 없다. 이번 바퀴 범위(바닥·원경) 밖이라 안 건드림 — 다음 레버 후보.
- 건물 반복(전부 같은 집 3종)은 그대로.
- 석축 높이 0.3m 라 멀리선 선 하나로 보인다. 배는 잔교 옆에만.
- 성능(FPS) 미측정: 배치 2053→2752, 나무 ~500 추가.
- 바다에 풀·상자가 뜨던 것(첫 설치)은 바다 행 제외로 고침.

## 캡처
- `01/02/03` 스모크 카메라(같은 자리), `05_harbor_quay`·`06_downtown_center` 는 새 자리(에디터 카메라). 비교 3단 `v3_vs_v2_vs_codex.png` (`before_`=Codex 1차, `v2_`=2차).

## 3차-b (게이트 6/10 뒤 즉시 수정)
- 1순위 "도로 아스팔트 민무늬": 아틀라스 칸 `asphalt2`(균열 폴리라인 2·밝은 자갈·짙은 얼룩) 추가, `Asphalt` 재질에 `_Patch`(tile2=43, 문턱 .5) — 도로 전체가 11m·4m 덩어리로 톤이 갈리고 균열이 깔린다.
- 2순위 "03 하단 계단식 얼룩": `stain` 칸 톤을 아스팔트에 가깝게(#4f5557→#5b6165), 땜질 데칼 1.7×1.15→1.3×0.9.
- 재설치(레이아웃 캐시 그대로) 2752 배치, 스모크 8/8.

## 3차-c (게이트 3차-b 6/10 "덩어리가 차보다 큼" 뒤)
- `Mat()` 에 `patchScale` 인자, Asphalt 만 `_PatchScale` .09→.3 (덩어리 11m→3.3m), `asphalt2` 톤 아스팔트에 더 가깝게. 재설치·스모크 8/8. 재판정은 안 걸었다 — 남은 지적(벽 단면·2칸 폭 도로)은 다른 레버라 도현 판단으로.

---

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


---

# 4차 — 건물 벽 단면 (Claude, 같은 브리프를 따로)

도현: "나누지 말고 둘 다 해서 차이를 보자." Codex 결과는 위 절, 코드 보관 `art/_compare/2026-10-13-building-faces/codex/`(패치·lofi_pilot.py·README). 지금 씬에 설치된 건 **Claude 판**(코드는 `claude/`).

## 바꾼 것 (Claude)
- `lofi_pilot.py` 건물 3종에 `bands()`: 기단 0.45 · 층 띠(`trim` 칸, 사방) · 처마 밑 그림자 띠 · 모서리 쿼인 4개. 뒷벽 창(집 4·상점 4·정비소 2), 옆벽 작은 창, 배수관, 굴뚝, 덧문(`shutters`), 발코니(`balcony`).
- 변형: 집 A(2층 덧문+굴뚝) / B(발코니+2층 문) / C(빨간 층 띠+빨간 문+현관 계단), 상점 MART / ICE(줄무늬 차양·빨간 간판) / TEA(덧문), 정비소 열림 / 셔터 내림(간판 왼쪽). FBX 8종, 건물당 862~1394 삼각형(기존 534~898).
- 선택: `CoastalTownInstaller.Layout()` 한 줄 — 집 (hash/7)%3, 상점 (hash/11)%3, 정비소 (hash/13)%2. `CoastalTownDensity.BaseName()` 으로 변형 이름을 기본 이름으로 정규화(몸통 치수·소품 규칙 동일). 프리팹은 `CoastalPropsBuilder` 에 5종 추가(MeshCollider).
- 설치 2752 배치(집 47/41/47, 상점 10/5/10, 정비소 33/39), 도로 297/297, Issues 없음, 스모크 8/8.

## 안 한 것 / 본 것
- **도현 지적대로 복붙 변형이다**: 몸통·층수·창 격자가 같고 간판·차양·덧문·발코니만 다르다. 임시. "실제 건물"이 되려면 실루엣(폭·층수·지붕 형태·돌출부)이 달라야 한다 — 다음 바퀴의 과제.
- Codex 와의 차이(내 눈): Codex 는 벽돌 기단·창 상부 테두리·지붕색 3종·네 면 창이 더 촘촘하고 캡처에서 더 "지어진" 느낌, 대신 삼각형 5배(3,000~4,100). 내 것은 가볍고(1,200 안팎) 띠·쿼인·뒷창으로 단면만 막은 수준. 판정은 게이트·도현.
- Codex 가 추가한 `LofiPilotBuilder.BuildBuildingPrefabs/CaptureBuildingFaces` 는 남겨 뒀지만 이번 설치엔 안 썼다. Codex 의 아틀라스 칸 3개(roof_clay/roof_green/awning_rust)는 내 판 아틀라스엔 없다(Codex 판으로 돌아가면 그 lofi_pilot.py 로 재생성).
- 캡처: `codex4_*` / `claude4_*` 같은 자리, 비교표 `codex4_vs_claude4.png`.

---

# 5차 — 건물 다양성 (Codex)

2026-10-13, `art/_briefs/2026-10-13-building-variety.md` 수행. 이번 출발점은 채택된 Codex 4차 생성기이며, 위의 Claude 4차 절에 적힌 당시 설치 상태와 다르다. 최종 생성·설치·스모크는 이 절의 결과다.

## 바꾼 것

- 기존 `CoastalHouse/B/C` 형상·UV를 그대로 유지하고 **D/E/F**를 추가했다. D는 **4×4.8m, 3층, 급경사 적갈색 박공**, E는 **6×4.4m, 단층, 큰 회색 모임지붕·깊은 처마·현관 포치**, F는 **6×6m, 2층, 모서리를 깎은 L 평면·한쪽 날개·녹색 한쪽 경사 지붕**이다. 포치는 벽걸이 띠를 침범하지 않는 외팔보 차양과 낮은 발판으로 만들었다.
- **MART / CornerShop**: 2층 몸통, 가로로 이어진 2단 과일·채소 상자 선반, 냉장 쇼케이스를 표현한 유리면과 금속 하부, 포스터 3장, 큰 입구 간판, 폭 1.7m 유리문.
- **TEA / CornerShopB**: 단층 모임지붕, 작은 종이창 격자, 입구 양옆 둥근 테이블 2개·의자 4개·찻잔, 나무 처마와 랜턴 3개, 세로 나무 간판·픽셀 찻주전자. 천 차양은 사용하지 않았다.
- **BAKERY / CornerShopC**: 단층 박공, 2단 빵 진열창, 큰 굴뚝(연기 없음), 적갈색 줄무늬 차양. 빵은 작은 타원형 팔각 메시로 만들었으며 실내나 투명 유리 셰이더는 추가하지 않았다.
- 정비소 2종을 유지하고 B의 지붕만 기존 `roof_clay`로 바꿨다.
- **`building-catalog.json`**에 전체 11종의 `name`, `w`, `d`, `floors`, `frontDoorX`를 기록했다. 치수는 지붕·가구·포치를 제외한 몸통의 로컬 미터 값이다. F의 오목한 평면 꼭짓점, 방향·원점, 최대 돌출량도 포함한다.
- Installer에서 집 6종·상점 3종·정비소 2종을 `(hash/7)%종수`로 선택한다. 기존 기본 종류별 `scale` 줄을 그대로 보존했다(집 기본 .62 + 기존 hash 변주, 상점·정비소 .64). Density에는 실제 변형 이름을 전달하지만, 현재 Density는 `BaseName()`으로 정규화하므로 아직 옛 치수를 사용한다.
- 실제 건물 232동: 집 A/B/C/D/E/F **23/22/23/24/19/24**, 상점 MART/TEA/BAKERY **12/7/6**, 정비소 A/B **38/34**. 해시 선택의 동일 가중치이며 유한한 맵의 배치 수를 강제로 같게 맞추지는 않았다. `building-variant-counts.json`에 기록했다.
- 기존 47칸 뒤에 `tea_board`, `bakery`, `paper`, `poster` 4칸만 추가했다. 512×512 아틀라스·64px 칸·Point·무압축·mipmap 없음 유지. 기존 2m 면 분할을 재사용하고 새 모임지붕 끝면과 L자 지붕 삼각형도 최대 변 길이 2m 이하로 분할했다. 간판 등 전용 UV 예외는 유지했다.
- FBX·mesh.json·프리팹·`art/source/lofi-pilot/LofiCoastalKit.blend`를 갱신했다. `LofiPilotBuilder`는 11종 빌드와 전체 모음·상점 정면 캡처를 지원한다.

## 참고 → 재현 → 적용 → 개선

- [Jacob Norris / PurePolygons — Modular Building Breakdown](https://www.purepolygons.com/blog/modular-building-breakdown)의 제작자 설명을 읽었다. 상점 용도에 따라 창·문·진열 공간을 달리 정하고, 공용 트림을 재사용하며, 엔진에서 치수를 확인하는 방법을 가져왔다. 외부 모델·텍스처·유료 원본은 구입하거나 복제하지 않았다.
- 대안으로 [Konstantin Magnus — Low Poly Building with UVs](https://procegen.konstantinmagnus.de/low-poly-building)를 확인하려 했지만 본문 접근에 실패했다. Houdini 원본 워크플로우를 검증하거나 재현했다고 주장하지 않는다.
- 직접 재현한 기준은 **변경 전 저장소 생성기**다. 같은 Blender 5.2.2·seed 413으로 출력 루트만 `files/building-variety-baseline`으로 바꿔 실행했다. 원본 아틀라스와 재생성 PNG가 바이트 단위로 같음을 검사했다. 외부 작품의 재현은 아니다.
- 기존 창·기단·층 띠·아틀라스·프리팹 경로를 재사용하고, 이번에 허용된 몸통·층수·지붕·상점 집기를 자체 코드로 추가했다. 새 도구 설치·외부 다운로드·유료 의존성·외부 에셋 라이선스는 추가하지 않았다.
- 첫 실행에서 Blender 5.2의 `tessellate_polygon` 결과를 좌표로 취급해 오류가 났다. 실제 반환값인 인덱스로 원래 꼭짓점을 조회하도록 고쳤다. 첫 Unity 캡처에서는 랜턴이 처마에 가려지고 BAKERY 글자가 반복되어, 처마 높이와 간판 전용 UV를 수정했다. 전체 모음은 앞 건물에 뒤 건물이 가려지는 문제가 있어 같은 카메라·배율로 한 동씩 찍는 4열 연락판으로 바꿨다.

## 실제 검증

- `D:/Tools/Blender/blender.exe -b -P tools/asset-gen/lofi_pilot.py -- --root D:/Github/F1Game` 생성 완료.
- 새 카탈로그를 읽도록 수정한 `tools/asset-gen/verify_building_faces.py` 실행: **104개 검사 PASS, `issues=[]`**. 기존 **47칸의 모든 픽셀**, 건물 외 **40종의 형상·UV**, Awning~Boat 함수 AST, 기존 집 3종이 기준과 같다.
- 전체 건물 **1,380~4,118삼각형**, 모두 4,500 이하. 집 A/B/C/D/E/F = **3154/4118/3946/3130/1380/2012**, 상점 MART/TEA/BAKERY = **3736/2540/2604**, 정비소 A/B = **2998/3014**. 바닥 원점·UV 유효성 검사 통과.
- 삼각형의 경계 교차점까지 포함해 **1.9~2.5m 벽걸이 띠**를 검사했다. 몸통 사각 경계 밖 돌출은 집·정비소 1mm, 상점 2mm의 표면 장식뿐이다. 띠 밖에서는 요청한 깊은 처마·포치·가구를 허용했다(E 최대 1m, TEA 최대 약 1.459m). F의 오목한 벽을 따라 배치하는 Density 기능은 이번에 구현하지 않았다.
- Unity 8090에서 `AssetDatabase.Refresh()` → 컴파일 종료 확인 → `Build Building Prefabs` → `Rebuild Layout + Install` → `CoastalTownSmoke.Run()` 완료. 11종 FBX와 JSON의 크기·중심·바닥·UV 비교 통과, 셰이더 오류 0(`building-faces-unity.json`).
- 최종 설치: **2752 배치**, 도로 **297/297**, 바닥 충돌 샘플 **1554**, `validation.txt`의 **`Issues=` 비어 있음**. 최종 스모크 20:04:30 기록: **8 PASS + SUCCESS**, 세 지점 이동·접지 및 실제 배달 접근 완료. 콘솔 오류 **0**.
- `Assets/ComplexTrack.unity` 저장. 마지막 상태 **playing=False, compiling=False, dirty=False**, 아틀라스 **Point/Uncompressed/mipmaps=False**(`building-variety-final-state.json`).
- **`CoastalTownDensity.cs`, `CoastalPropsBuilder.cs`는 수정하지 않았다.** 두 파일의 시작·종료 SHA-256이 같으며 `building-variety-protected-files.json`에 기록했다. 차량·게임 카메라·배달·물리·도로 코드 수정 없음. **git commit 하지 않았다.**

## 캡처와 남은 한계

- `01_ingame_neighborhood.png`~`04_delivery_complete.png`: 최종 스모크가 생성한 플레이 캡처. `06_downtown_center.png`: 지정된 `ArtCapture.CaptureChaseAt(path,84,60,0,1600,900)`.
- `08_building_variants.png`: 11종, 같은 카메라·배율의 개별 프리팹 검토. 왼쪽부터 1행 집 A/B/C/D, 2행 집 E/F·MART·TEA, 3행 BAKERY·정비소 A/B. 빈 마지막 칸은 의도한 여백이다.
- `10_shops_front.png`: 별도 검토 씬의 세 상점 정면 근접. 화면 왼쪽부터 BAKERY·TEA·MART. 종류별 진열·가구·창 형태와 간판을 직접 확인했다. `07`·`09`도 다시 생성했다. `05_harbor_quay.png`는 이번 작업에서 갱신하지 않았다.
- **Density가 옛 몸통·문 위치를 사용하므로 새 집 주변 소품 위치, TEA/F의 문 피하기, 기존 외부 차양·간판과 새 전면 집기의 간섭은 해결하지 않았다.** 브리프대로 이 상태로 설치했다. Claude의 카탈로그 적용 후 소품 배치를 다시 검사해야 한다. F는 단순 w/d뿐 아니라 오목한 평면 정보도 고려해야 한다.
- 01~04·06·08·10 화면을 확인했으나 모든 건물 배치·시점·소품 조합을 시각 검사하지 않았다. 실제 주행 화면에서는 작은 빵·포스터·주전자까지 항상 읽힌다고 보장하지 않는다. 유리는 기존 불투명 픽셀 재질의 표현이며 내부 공간은 없다.
- FPS·메모리·드로콜·플레이어 빌드·모든 가구의 충돌 감각은 측정하지 않았다. 이번 결과의 새 graphic-gate 점수나 외부 합격 판정은 받지 않았다.

## 5차 통합 (Claude) — 치수 동기화 + 해상도 실험
- `CoastalTownDensity.Body()/DoorX()` 가 Codex 의 `building-catalog.json`(name/w/d/frontDoorX) 을 읽는다(정규식, 외부 JSON 라이브러리 없음). 없으면 옛 3종 치수. 재설치 2752 배치, 건물 11종(집 23/22/23/24/19/24 · 가게 12/7/6 · 정비소 38/34), 스모크 8/8, Issues 없음.
- 도현: "에셋은 맞는데 화면 해상도와 안 맞아 부족해 보인다" → 진단: 아틀라스가 1m 당 32~43 텍셀로 균일한데 추적캠은 차 앞 5~7m 를 1m 당 100+ 픽셀로 키워 **가까운 바닥은 텍셀이 4~8픽셀 덩어리, 먼 건물은 1픽셀 미만** — 한 화면에 굵은 픽셀아트와 흐린 로우폴리가 섞인다. 실험 `resolution_test.png`: 같은 자리를 1920×1080 / 640×360 ×3 최근접 / 960×540 ×2. 확대 비교 `resolution_test_zoom.png`. (URP 설정 변경 없이 작은 RT 로 찍고 PIL 최근접 확대 = 내부 해상도 낮춤 + Point 업스케일과 동일.) 적용은 도현 판단 — 적용하면 URP 에셋 Render Scale + Upscaling Filter=Point 두 칸.


---

# 6차 — 한낮 색감(D-101) + 카메라(D-102) (Claude)
- `CoastalNoonLook.cs`(메뉴 Apply Noon Light, 설치기도 호출): 그라데이션 하늘(위 진한 파랑), 해 62°·1.7·거의 흰색·그림자 1.0, Trilight 주변광(낮게, 그림자 안이 차지 않게), 안개 75~240 밝은 하늘색, 로파이 재질 _Ambient 흰색·안개 동기화·바닥 _FaceShade .74, Sea·Asphalt·DistantHills 틴트, 후처리 노출+0.2/채도+14/대비+12. 아틀라스: plaster #b8ac91→#e8e1cf, asphalt #666c70→#80827c(+asphalt2·stain), concrete·water 밝게.
- v1(해 1.45·주변광 밝음) 게이트 6/10 FAIL "도로·벽 청회색, 그림자 약함" → v2 로 아스팔트 따뜻하게·해 1.7·주변광 낮춤.
- 카메라: (0,2,-3.5)/0.45 → (0,2.6,-5)/1.6.
- 해상도: URP Render Scale 0.333 + Point(도현 결정, 640×360 ×3 픽셀 룩).
- 캡처: `_noon_before_0*.png`(전) / `0*.png`(후), `noon_before_after.png`.
- **v3 (도현 "아직 햇빛이 안 들어오는 느낌")**: 진짜 원인 = `WorldSurface`·`LofiSurface` 두 셰이더가 해의 색·세기를 안 쓰고 0.55~1.0 고정 명암만 씀 → 해를 1.7 로 올려도 양지가 안 밝아짐. 고침: `lit = _Ambient*0.42 + sun.color*diffuse*shadow*0.75` (그늘 = 하늘빛 _Ambient(.80,.88,1), 양지 = 따뜻한 해). 해 52°·1.3·(1,.96,.88), 바닥 _FaceShade .9. 캡처 `_noon_v2_0*.png`(v2) / `0*.png`(v3).
- **v4 (도현 "쨍쨍함이 없다 · 해상도 너무 낮다")**: 양지 1.0·그늘 .36, 해 1.25·(1,.95,.86), _Ambient (.72,.82,1), 하늘 위 (.14,.38,.80)·지수 1.15, 후처리 대비+22·채도+18·블룸 .35, 아스팔트 #8a867c, **Render Scale 0.5**(960×540 ×2). 캡처 `_noon_v3_0*.png`(v3) / `0*.png`(v4), `noon_v3_v4.png`. 하네스: `옵시디언/하네스/incidents.md` 에 "셰이더가 광원 세기를 안 씀" 기록.
- **v5**: verifier(21:50 FAIL)가 잡음 — 프로필에 넣은 ColorAdjustments·Bloom 이 서브에셋 등록(AddObjectToAsset) 없이 fileID 0 으로 남아 비어 있었고, 메인 카메라 **Post Processing 이 꺼져 있어** 후처리가 애초에 안 그려짐. 둘 다 고침(NoonLook 이 카메라 플래그도 켬). 캡처 `_noon_v4_0*.png`(v4) / `0*.png`(v5), `noon_v4_v5.png`.


---

# 7차 — 다른 세션(거리 소품·축척) 통합 (Claude, 2026-10-13 밤)
- 다른 Codex 세션 산출물: `F1/Assets/Art/StreetFurniture/`(Bench·Bin 프리팹, Furniture.mat, 골재 Asphalt.mat+shader), `Editor/ApprovedStreetAssets.cs`(메인 씬 적용: 소품 교체·얼룩 끔·차 3.785m·카메라)·`StreetFurniturePreview.cs`(검토 씬), `tools/asset-gen/street_furniture.py`, 기록 `art/source/street-furniture/STATUS.md`·`REAL_SCALE.md`. 설치기는 그 세션이 이미 `ApprovedStreetAssets.PrefabPath/RoadMaterial` 로 연결.
- Claude: 건물 축척 1.0 실험(`BuildingScale` 상수), 소품 ps 1.0, NoonLook 에 StreetFurniture 재질 포함, 카메라 기본값 (0,3.2,-7)/1.15. 캡처 `_scale064_0*.png`(0.64) / `0*.png`(1.0), `scale_064_vs_100.png`. 판정 D-103 대기.
