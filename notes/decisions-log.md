# 결정 히스토리 (Decisions Log)

> 주요 기획 결정을 날짜 순으로 기록한다.
> 왜 그렇게 정했는지를 남겨 나중에 재검토 가능.

---

## 2026-04-18 — 프로젝트 시작

### D-001. 프로젝트 시작
- **결정:** PIT CREW (가칭) 개발 시작.
- **근거:** 도현이 Kenny 에셋 기반 자동차 게임으로 완성 가능한 작은 프로젝트를 원함.
- **대안 검토:**
  - Car Survivors → 속도감 부족
  - Toy Car Rampage → 쿨함 부족
  - Coaster Crash → 컨트롤 부족
- **채택 근거:** "쿨/박살 + 5~15분 런 + 속도감/컨트롤" 4축을 모두 만족.

### D-002. MVP 4주 스코프 확정
- **결정:** 4주 안에 코어 루프 완성.
- **근거:** ExulMundi 스코프 부풀림 패턴 방지. 완성 경험 축적이 최우선.

### D-003. 경로 계획 단계 포함
- **결정:** 단순 배달이 아니라 **출발 전 경로 설계**를 핵심 메커닉으로.
- **근거:** 이 요소가 게임의 정체성을 정의. 기존 배달 게임과의 차별화.
- **트레이드오프:** 스코프 +1주 예상. 그러나 빠지면 평범한 게임 됨.

### D-004. 개발 기반 Unity 6
- **결정:** Unity 6 LTS 사용.
- **근거:** ExulMundi와 동일 엔진, 경험 재사용.

### D-005. Kenny 3D 에셋만 사용
- **결정:** 커스텀 에셋 제작 금지, Kenny 에셋 라인업 안에서만 해결.
- **근거:** 에셋 제작에 시간 소모 방지. 제약이 곧 창의성.

---

## 2026-04-18 — 세션 1 (Open Questions 해소)

### D-006. 카메라 시점 (Q1)
- **결정:** 3인칭 쇼울더 뷰 단일. 1인칭 콕핏 뷰는 Post-MVP.
- **근거:**
  - Kenny 로우폴리 미학은 3인칭에서 가장 잘 보임.
  - 전환 시스템(A↔C)은 물리 튜닝·UI·인풋을 두 세트로 만들어야 해서 4주 MVP 스코프 초과.
  - "계획→실행→정산" 코어 루프와 직접 관련 없는 연출 레이어이므로 단일로 고정하는 게 완성 확률을 높임.
- **관련 GDD 문서:** GDD/overview.md (Camera 섹션 추가 예정)
- **파킹:** 콕핏 뷰 → notes/ideas-parking.md (Post-MVP)

### D-007. 드라이빙 스타일 (Q2)
- **결정:** 아케이드 + 약간의 무게감 (Forza Horizon / Art of Rally 결).
- **근거:**
  - 순수 아케이드는 Shock/Spin 화물 긴장감 약화 → 게임의 고유 매력 훼손.
  - 세미 시뮬은 튜닝 비용 과다 + "배달"의 경쾌함과 부조화.
  - 중간 지점이 "머리 + 손 모두 의미 있다" 원칙과 가장 잘 맞음.
- **1주차 튜닝 가이드라인:**
  - 즉각 반응 유지 (입력 딜레이 최소).
  - 코너 진입 시 관성 피드백: 핸들링과 드리프트 사이 톤.
  - 급가감속 시 화물 Shock 수치 가산 → 경로 스타일이 화물 손상에 직접 반영.
- **관련 GDD 문서:** GDD/systems/cargo.md (Shock 트리거 연동), 1주차 작업.

### D-008. 맵 스케일 (Q3)
- **결정:** 스테이지형 (시작/끝 있는 런). 각 스테이지는 가상의 도시의 한 구역(다운타운, 항구, 공업지대 등)으로 디자인.
- **근거:**
  - 도시형 오픈(20~30 km)은 4주 MVP 완전 초과. 도로 네트워크·웨이포인트·미니맵·LOD 작업만으로 스코프 소진.
  - 스테이지형이 5~15분 런 구조와 가장 잘 맞고, Kenny Road Tiles로 모듈 조립이 용이.
  - "도시 분위기"는 여러 스테이지를 서로 다른 구역으로 구성해 **런 누적 경험**에서 구현.
- **1차 고려 사항 (Q3 답변 직전 도현이 도시형 선택 → 스코프 리스크로 재조정):** ExulMundi 패턴 감지.
- **관련 GDD 문서:** GDD/systems/route-planning.md, GDD/content/circuits.md (아직 없음 → 생성 예정).

### D-009. 아트 톤 (Q5)
- **결정:** (A) 코믹 × 쿨의 갭. 밝은 낮 + Kenny 로우폴리 귀여움 + 팀 로고/UI만 F1 브로드캐스트처럼 진지.
- **추가 제약:** 도현이 명시 — **Kenny 에셋 거의 그대로 사용, 커스텀 제작 최소화.**
- **근거:**
  - GDD overview.md에서 이미 이 톤을 게임 매력으로 규정.
  - 커스텀 제작 최소화는 CLAUDE.md 제1원칙 "제한이 곧 재미"와 일치. ExulMundi 실패 방지 핵심.
- **파생 제약:**
  - 커스텀 셰이더 금지 (Kenny 기본 머티리얼만).
  - 캐릭터 디자인 불필요 — 드라이버는 익명.
  - UI는 F1 스타일 조합으로 해결 (Figma/웹 레퍼런스 참고해 Unity UI 빌더로 조립).
  - 4주차 폴리싱은 "에셋 교체/추가" 가 아니라 "에셋 배치/조명/카메라" 튜닝 중심.
- **관련 GDD 문서:** GDD/overview.md.

### D-010. 차량 종류 (Q4)
- **결정:** MVP는 F1형 1종만. Kenny Car Kit 중 싱글시터/오픈휠 모델 1대 고정.
- **근거:** 차종 추가는 물리 프리셋·업그레이드 라인 분기 유발. MVP 스코프에서 제외.
- **파킹:** 랠리형/픽업트럭 → Post-MVP.

### D-011. 사운드/음악 (Q6)
- **결정:** Kenny Audio 팩만 사용. 무료 음원 추가/외주 사운드 금지.
- **근거:** D-009의 "Kenny 거의 그대로" 원칙 연장선. 4주차 폴리싱에서 믹싱/볼륨만 조정.

### D-012. 진행 구조 (Q7)
- **결정:** 순수 로그라이트 — 매 런 독립, 영구 업그레이드만 축적.
- **근거:** 커리어 서사·시즌 구조는 4주 안에 완성 불가. 로그라이트 단일 구조가 재미 검증에 가장 적합.
- **파킹:** 커리어/시즌/그랑프리 → Post-MVP.

### D-013. 실패 런 처리 (Q8)
- **결정:** 최소 보상(위로금). 완전 실패해도 다음 런은 진입 가능.
- **근거:** 로그라이트 표준. 진입장벽 낮춤. 재도전 의지 유지.

### D-014. 차량 커스터마이징 (Q9)
- **결정:** 색상 프리셋 3~5종만. 데칼·풀 커스텀 모두 파킹.
- **근거:** D-009 원칙 연장. Kenny 머티리얼 색 스왑 수준.

### D-015. 게임 제목 (Q10)
- **결정:** PIT CREW 가칭 유지. 제목 변경 논의는 MVP 완성 후.
- **근거:** 지금 브랜딩에 시간 쓸 필요 없음. 재미 검증이 우선.

## 2026-04-18 — 세션 2 (하네스 정비)

### D-016. 하네스 엔지니어링 1~3단계 적용
- **결정:** 영상 기반 감사 후 CLAUDE.md 슬림화(172→82줄), 세션 관리 파일 3종 도입(session-log, feature-list, session-opener), MCP 가이드, git init.
- **근거:** Cowork 컨텍스트 예산 관리, 세션간 인수인계 장치 확보, 조기 종료 방지.
- **이관:** 하네스 4~6단계(검증 파이프라인, 멀티에이전트, 유지보수)는 MVP 완성 후 도입. `dev-plan/feature-list.md` "추후" 섹션에 기록.
- **관련 문서:** `CLAUDE.md`, `notes/session-log.md`, `dev-plan/feature-list.md`, `notes/session-opener.md`, `notes/mcp-setup.md`, `notes/git-setup.md`.

### D-017. 프로젝트 루트에 단일 git 리포
- **결정:** PitCrew/ 전체를 하나의 git 리포로. Unity 프로젝트(F1/)는 하위에 포함.
- **근거:** 기획 문서와 코드가 같은 역사 축. 특정 기능 구현이 어떤 GDD 수정을 동반했는지 한 번의 `git log`로 추적.
- **대안 검토:** Unity 규범(F1/만 리포)은 솔로 + 문서 중심 프로젝트에선 오버헤드. 두 리포 분리(문서/코드)는 관리 부담 증가.

## 2026-04-19 — 세션 3 (Day 2 진입)

### D-018. 1주차 스코프 밖 Kenny 에셋 유지
- **결정:** `F1/Assets/Model/` 에 선행 임포트된 `kenney_city-kit-commercial`, `kenney_city-kit-industrial`, `kenney_city-kit-suburban`, `kenney_train-kit` 4종을 **삭제하지 않고 그대로 둔다.**
- **근거:** 도현 명시 선택. 재임포트 왕복 비용 회피.
- **전제 (위반 시 스코프 원칙 재위배):**
  - 1주차 씬/프리팹에서 이 4종은 **참조 금지.** 차량 + Road Tiles + 평면 외 배치 금지.
  - 4주차 이전에 "이 에셋 써보자"는 충동이 생기면 즉시 `notes/ideas-parking.md`로 이동.
  - Unity 빌드 용량에 들어가지만 1주차에는 측정 불필요.
- **관련 원칙:** CLAUDE.md 규칙 1(제한이 곧 재미), 규칙 3(비주얼 폴리싱 4주차 이후).

## 2026-04-20 — 세션 4 (Day 3~5 진행 중)

### D-019. F1 리얼리즘 요청 재확인 — D-007 유지
- **결정:** Day 5 드리프트 구현 후 "F1처럼 만들고 싶다"는 도현 요청에 대해 **D-007(아케이드 + 무게감) 방침 유지.** Tier A(수치 튜닝)만 수용, Tier B(기어박스/다운포스/ERS/슬립스트림/서스펜션) 전부 `ideas-parking.md`로, Tier C(풀 시뮬 물리)는 기각.
- **근거:**
  - D-007 근거 그대로 유효: 화물 Shock 긴장감, 튜닝 비용, 배달 톤 부조화.
  - "유치함" 피드백의 진짜 원인은 수치 튜닝 거친 상태일 가능성 높음 (드리프트 방금 넣음, 브레이크 한 번 조정).
  - 1주차 재미 판정 기준은 "계속 놀고 싶나", 사실감이 아님.
  - 도현 패턴 3번째 반복 관찰 (세션 1 카메라 전환, 세션 3 에셋 쓸어담기, 세션 4 F1 리얼리즘) → 방어선으로 D-007 재확인.
- **수용한 Tier A 튜닝:** 고속 조향 반응 더 둔화(커브 끝값 0.35→0.2), 공기저항 추가(airDrag 0.35), 브레이크 속도 곡선화(저속 1.0 / 고속 0.6), Rigidbody angularDamping 2.5, 카메라 maxFov 75→80, 코너 롤 ±5°.
- **파킹 항목:** `notes/ideas-parking.md` "2026-04-20 F1 시뮬 요소 검토 결과" 블록.
- **관련 문서:** `F1/Assets/Scripts/ArcadeCarController.cs`, `F1/Assets/Scripts/FollowCamera.cs`, `notes/ideas-parking.md`, 플랜 `C:\Users\stard\.claude\plans\happy-puzzling-raccoon.md`.

### D-020. 차량 엔진 사운드 외부 CC0 허용 (D-011 보정)
- **결정:** 차량 엔진 루프 **1개 한정**으로 Kenny 외 CC0 에셋 허용. 출처는 freesound.org CC0 필터 우선, 다른 SFX/UI/음악은 D-011 유지.
- **근거:** Kenny Audio 라인업에 엔진 음원 부재. 엔진 사운드는 주행 체감의 약 50% 비중이라 생략 불가. AI 생성(generate_sfx)은 현재 401 오류 + 라이선스/퀄리티 불확실 → 실사 녹음 CC0가 퀄리티·안전성 모두 우위.
- **제약:**
  - 엔진 루프 1개만. 추가 외부 SFX 요청 발생 시 재심사.
  - 파일 경로: `F1/Assets/Audio/EngineLoop.wav` (또는 유사).
  - 출처 URL과 라이선스(CC0) 확인 문서를 같은 폴더 `Audio/LICENSE.txt`에 기록.
- **관련 문서:** `F1/Assets/Scripts/EngineAudio.cs`, `dev-plan/week1.md` Day 4.

## 2026-04-22 — 세션 6: 트랙 시스템 재정비

### D-021. Kenny city-kit-roads 유지 결정 (세션 5 파킹 정정)
- **결정:** city-kit-roads 그대로 사용. racing-kit/커스텀 Plane 교체 불필요.
- **근거:** 세션 5에서 "코너 흰 띠"를 crosswalk 마킹 한계로 추정해 D-021 파킹했으나, 세션 6 측정 결과 진짜 원인은 **road-curve.fbx의 native size가 2×2 (직선은 1×1)**여서 동일 TileScale로 깔면 코너만 8×8 유닛으로 깔리는 mesh 크기 불일치 문제였음. 타일별 native size 정규화로 해결.
- **관련:** `F1/Assets/Editor/TileMapAuto.cs` `NativeSize` dict, `_Recovery/MeasureTiles.cs`.

### D-022. 그리드 마스크 + 자동 추론 기반 트랙 시스템 (TileMapAuto)
- **결정:** 트랙 빌더는 셀별 회전 수동 지정(TileMapTrack) 방식 폐기, **그리드 마스크 + 인접 셀 분석 자동 추론** 방식으로 전환. 사용자는 `'#'`(도로) `'.'`(빈칸) 마스크만 정의 → 시스템이 도로 인접 수에 따라 직선/코너/T자/4갈래/end 자동 결정 + 회전 매핑 lookup.
- **근거:** 셀별 수동 회전은 시행착오에 취약(세션 5/6 합쳐 6회 오답). 자동 추론은 (Kind, ExitSet)→rotation 메타데이터만 검증되면 영구 정확. 향후 레이아웃 확장(S자, 분기, T자) 시 마스크 수정만으로 자동 처리.
- **메타데이터 검증 방법:** 톱다운 정사영 캡처 (isometric은 시각 착시 위험 — 세션 6 두 번 오판).
- **관련:** `F1/Assets/Editor/TileMapAuto.cs`, `_Recovery/TopDownView.cs`/`TopDownCloseup.cs`.

### D-023. 에셋 도입 워크플로우 — native size 측정 + 메타데이터 등록 의무
- **결정:** 신규 키트(racing, suburban, commercial 등) 도입 시 첫 단계로 `MeasureTiles.cs`로 native size 측정 → `TileMapAuto.NativeSize` (또는 향후 ScriptableObject)에 등록. 회전 매핑은 빈 씬 4회전 톱다운 캡처로 검증 후 `RotationFor`에 등록.
- **근거:** 같은 키트 내에서도 타일별 native size가 다를 수 있음(road-straight 1×1 vs road-curve 2×2). 측정 없이 동일 스케일 적용하면 mesh 크기 불일치로 도로 폭 어긋남. 한 번 측정 + 등록하면 영구 사용.
- **관련:** `_Recovery/MeasureTiles.cs`, `TileMapAuto.cs` `NativeSize` dict.

### D-024. road-curve → road-bend 교체 (D-021/D-023 보정)

- **결정:** `Kind.Curve` 소스 타일을 `road-curve.fbx` → `road-bend.fbx`로 교체. `NativeSize[Curve]` 2 → 1.
- **근거:** D-021에서 "native size 정규화로 해결"로 판정했던 것이 반쪽 진단이었음. 실제로는 **외곽 bounds만 맞추고 도로 stripe 자체 폭은 맞추지 못함**.
  - `road-straight`: native 1×1, 도로가 타일 ~100% 폭
  - `road-curve`: native 2×2, 도로는 가운데 가는 원호 ~50% 폭
  - 동일 셀(4m)에 넣으려 scale 보정: straight ×4 → 도로 4m / curve ×2 → 도로 ~2m. **이음매마다 도로 폭 단차.**
  - `road-bend`: native 1×1, 도로가 타일 전체 폭. straight와 동일 스타일 한 쌍.
- **교훈 (D-023 보정):** 외곽 bounds 측정만으로 "정규화 완료" 판정 금지. 같은 키트 안에서도 스타일 믹스가 섞여 있을 수 있음. 신규 타일 도입 시 **기존 타일과 side-by-side 렌더** 후 이음매 클로즈업 비교 의무. `_Recovery/PreviewBend.cs`가 그 패턴.
- **관련:** `F1/Assets/Editor/TileMapAuto.cs` `SourcePath`/`NativeSize`, `_Recovery/PreviewBend.cs`.

### D-025. 레이아웃 데이터 SO 외부화 + BuildingPlacer Phase 1

- **결정:** 트랙 레이아웃을 하드코드 배열에서 ScriptableObject(`TrackLayout`)로 외부화. 건물 자동 배치 시스템 Phase 1 도입 (1×1 footprint, suburban + commercial 키트).
- **근거:** 레이아웃 수정할 때마다 코드 재컴파일 불필요. 여러 트랙 프리셋(Default/Complex 등) 병존 가능. 건물은 2-layer 마스크(도로 + 건물)로 관심사 분리 — TileMapAuto 로직 건드리지 않고 확장.
- **데이터 스키마:**
  - `BuildingDef`: prefab + nativeSize + entryLocalDir + category
  - `BuildingCatalog`: maskChar('S','C') → BuildingDef 풀, (c,r) 해시로 결정론적 선택
  - `TrackLayout`: roadMask[] + buildingMask[] + catalog + deliveries + startCell
- **메뉴:** `PitCrew/Setup Layout Assets`(일괄 생성) + `PitCrew/Build Everything From Layout`(도로+건물 재빌드 + 톱다운 자동 프레이밍).
- **관련:** `F1/Assets/Scripts/Layout/*.cs`, `F1/Assets/Editor/BuildingPlacer.cs`, `BuildFromLayout.cs`, `SetupLayoutAssets.cs`, `SetupComplexScene.cs`, `Assets/Data/*`.

### D-026. 자동 배치 검증 의무화 (침묵 실패 방지)

- **결정:** `BuildingPlacer`에 포스트빌드 `VerifyPlacements()` — 각 인스턴스 (a) localRotation.eulerAngles의 X/Z가 1° 초과면 "flipped/tilted" (b) world bounds.min.y < −0.05면 "sunk" 경고. 빌드 로그에 `"all verified"` 또는 `"N FAILED verification"` 명시.
- **근거:** 이번 세션 `Quaternion.FromToRotation(+Z, -Z)`가 X축 180°를 리턴해 건물 절반이 **땅 아래 뒤집혀 박힘**. 톱다운에서는 정상으로 보여 2단계 검증 없으면 발견 불가. 향후 동종 버그(pivot 오프셋, 잘못된 입력 회전 등) 조기 탐지.
- **보조책:**
  - `BuildFromLayoutMenu`가 빌드 직후 SceneView를 톱다운 정사영으로 자동 프레이밍 → 시각 검증 한 스텝에 완료
  - 수학적 원인: `FromToRotation(A, -A)`는 180° 회전축이 여러 해(X/Y/Z 모두 정당) → Unity가 Y를 보장하지 않음. 해결: `atan2(x, z)`로 Y-yaw만 직접 계산
- **관련:** `F1/Assets/Editor/BuildingPlacer.cs` `VerifyPlacements`, `BuildFromLayout.cs` `FrameSceneViewTopDown`.

## 2026-04-22 — 세션 8: 월드 스케일 리밸런스

### D-027. 월드 스케일/비율 재조정 (Cell 6m, Car 0.7x, 건물 fit-to-cell)

- **결정:**
  - `CellSize`/`TileScale` 4m → **6m** (도로 50% 넓어짐 → 주행 여유)
  - `Race.prefab` localScale 1 → **0.7** (1.8×0.84m 미니카 비율)
  - `BuildingPlacer` 고정 4× 스케일 폐기 → `scale = CellSize × Min(w,d) × 0.9 / nativeSize` (각 건물이 자기 footprint에 맞게 피팅, 도로 침범 없음)
- **근거:** 세션 8 체감 검증에서 ①기존 fixed 3.2m 스케일이 native 변이 모두 찌부 ②fixed 4× 스케일은 변이 보존하지만 도로 침범 ③차(1m)가 4m 도로에 비해 작고 도시 풍경 초라. 세 증상을 각각 독립 변수로 분해해서 고침.
- **관련:** `F1/Assets/Editor/{BuildingPlacer, TileMapAuto, SetupComplexScene, SetupRaceInScene}.cs`, `F1/Assets/Prefabs/Race.prefab`.

### D-028. 멀티셀 건물 + 자동 콜라이더 (BuildingDef cellsWide/cellsDeep)

- **결정:**
  - `BuildingDef`에 `cellsWide`, `cellsDeep` (기본 1×1). `BuildingPlacer`는 점유 그리드로 겹침/도로 겹침 자동 검증 후 풋프린트 중앙에 배치.
  - `BuildingCatalog`에 K(2×2 상업) + X(2×2 주택) 풀 추가. Kenny native 1.6~2.3 건물(building-e/j/k/n, type-b/d/n)을 2×2로 분류.
  - `BuildingPlacer`가 배치 후 **MeshCollider 자동 부착** (Kenny FBX는 기본 콜라이더 없음 → 차가 건물 통과하던 버그 해결).
- **근거:** 1×1만 쓰면 도시 볼륨 단조로움. Kenny 빅빌딩 쓰려면 footprint 메타 필요. 콜라이더 누락 버그는 세션 8 플레이 테스트에서 발견.
- **관련:** `F1/Assets/Scripts/Layout/BuildingDef.cs`, `F1/Assets/Editor/{BuildingPlacer, SetupLayoutAssets, SetupComplexScene}.cs`.

## 2026-04-22 — 세션 9: 기획 재검토 + 연구 기반 재정립

### D-029. 2주차 시스템 수치는 레퍼런스 기반으로 재정립

- **결정:** 2주차 구현(화물 + 타이어 + 연료 + 부스터 + 기어 자동) 시작 전에 핵심 수치를 레퍼런스 게임/실제 데이터 기반으로 재정립. 구현은 세션 11 이후로 연기. 연구 페이즈 타임박스 **2세션 초과 금지**.
- **근거:**
  - 세션 9 초반 기획 대화에서 내놓은 수치(마모율 %/s, 부스터 충전 속도 등)가 경험적 감에서 나옴 → 도현 지적.
  - 기존 `GDD/systems/*.md` 수치(tires.md, fuel.md, cargo.md)도 세션 0~1 초기 플레이스홀더라 동일 문제.
  - 근거 없는 수치 기반으로 구현 시작하면 튜닝 루프가 "감 vs 감"으로 수렴 불가.
- **분담:**
  - 도현: NotebookLM 3개 노트북 (Burnout / Forza / F1) — 자가 작업
  - Claude: WebSearch로 수치 중심 자료 1차 완료 → `GDD/research/claude-web-findings.md` 저장
  - 공동(세션 10): 양측 교차검증 → GDD 6개 문서 일괄 업데이트
- **판정 결과 (세션 9 1차 조사):**
  - **유지:** 아케이드 + 자동 기어 기본값 (FH5 근거), 화물 무게 페널티 개념 (F1 10kg당 0.3~0.4s/랩 실수치)
  - **수정:** 타이어 컴파운드 델타를 **랩당 1.5~2초(~8%)**로 과장, Soft 수명 **2~4분 주행**으로 공격 축약, 부스트 % 비중은 **비공개 + 감각 튜닝** (Criterion/Codemasters 관행)
  - **기각:** 기존 GDD의 "드리프트 +5~10/s 타이어 마모"는 런 시간(5~15분) 스케일 재검토 후 수정 예정 (세션 10)
- **미해결 5건(U1~U5):** Burnout 행동별 % 비중, 부스트 지속시간 초수, FH5 Auto/Manual 비율, F1 게임 연료 축약률, FH5 기어 시프트 연출 디테일. NotebookLM 단계에서 커버 시도.
- **도현 패턴 관찰:** 세션 9에서 **기어 수동 조작 제안 3번째** (D-019 이후 재등장). 이번엔 "경영 감각" 프레이밍. 1번(스트레스 회피)과 2번(신경쓸 요소 증가)이 같은 메시지에서 충돌한 점을 지적 → 자동 기본 + 수동 토글(Post-MVP)로 절충. FH5 조사 결과가 이 절충을 뒷받침.
- **관련:**
  - 플랜: `C:\Users\stard\.claude\plans\whimsical-juggling-kernighan.md`
  - 1차 조사: `GDD/research/claude-web-findings.md`
  - 교차참조: D-007(아케이드 정체성), D-019(F1 시뮬 기각), D-020(엔진 사운드 외부 예외)

## 2026-04-22 — 세션 10: 2차 조사 + 2주차 시스템 수치 확정

### D-030. K1~K5 결정 — 부스트/기어/연료/타이어 핵심 수치

- **결정 요약 (K1~K5):**
  - **K1. 부스트 충전 = 이중화.** "완수 이벤트 100% 즉시충전" (배달 완료·완벽 정차) + "위험주행 점진충전" (드리프트·근접회피). Burnout의 Takedown=풀충전 / Oncoming=점진 패턴 차용.
  - **K2. 풀부스트 지속시간 = 6~10초 범위, 기준값 8초.** Burnout Speed Boost 풀바 정확히 8초가 다중 위키 출처 일치값. 5~15분 런에서 런당 3~6회 발동을 자연 빈도로 잡음.
  - **K3. 기어 = 자동 기본 + Manual 토글에 부스트 충전율 +10% 인센티브.** 클러치는 생략 (패드 UX 희생 대비 이득 작음). FH5 정성 사실(WR 100%=Manual+Clutch, 캐주얼=Auto) 근거.
  - **K4. 연료 축약 = "한 런 안에 주유 결정 2~4번" 감각 기준.** 수치 기준(분당 소비량) 아닌 의사결정 빈도 기준. F1 게임은 1:1 시뮬이라 레퍼런스로 부적합 → 독자 설계 필요.
  - **K5. 기어 시프트 연출 = 샘플 조합 방식 3종 세트.** 엔진 피치 순간 드롭 + 0.1초 카메라쉐이크 + HUD 기어숫자 플래시. FH5의 Granular Synthesis 방식은 Kenny+아케이드 스케일에 과한 솔루션.
- **근거:**
  - 2차 조사(`GDD/research/claude-web-findings-v2.md`) U1~U5 심화. NotebookLM 단계는 도현 결정으로 생략, Claude WebSearch 2차로 대체.
  - 결정적 발견: F1 공식 게임도 시간 시뮬 축약 없이 1:1 시뮬 + 랩 수만 축약. PIT CREW가 따라할 모델 아님.
  - FH5 오디오는 Playground Games(Turn 10 아님) 담당이며 Granular Synthesis로 시프트를 표현 — 우리가 흉내내기엔 과함.
- **U1 정량치(행동별 부스트 % 비중)는 추가 추적 포기.** Criterion 비공개 + 리버스엔지 문서 없음. K1 이중화 구조만으로 충분, % 수치는 MVP 구현 후 플레이 튜닝으로 결정.
- **GDD 반영:**
  - 신설: `GDD/systems/boost.md` (K1+K2)
  - 신설: `GDD/systems/gears.md` (K3+K5)
  - 수정: `GDD/systems/fuel.md` (방향 A=통합 안 폐기, K4 반영, 부스트는 boost.md로 분리)
  - 유지: `GDD/systems/tires.md` (D-029 1차 조사 결과 이미 부합)
- **관련:**
  - 2차 조사: `GDD/research/claude-web-findings-v2.md`
  - 1차 조사: `GDD/research/claude-web-findings.md`
  - 교차참조: D-007(아케이드), D-019(F1 시뮬 기각), D-020(엔진 사운드), D-029(연구 페이즈 정의)
- **도현 패턴 관찰:** 세션 10에서 Q3(U1 추가 추적 여부) "해보면서 정하자"로 답함 — 연구 페이즈 종료 의지 명확. 세션 11에서 D-029의 "타임박스 2세션 초과 금지" 약속 지키며 구현 진입 가능.

### D-031. 부스트 시스템 폐기 → 운전자 자원(집중력)으로 전환 + USP 명문화

- **결정:**
  - **부스트(차량 자원) 폐기.** D-030 K1/K2 의 "이중 충전 + 8초 풀바" 구조 폐기 (코드 미구현 상태에서 결정, 비용 작음).
  - **신설: Driver Concentration (운전자 집중력 자원).** EDC 의 이중 게이지(차량+운전자) 패턴 차용.
    - 소모: 고속(>70%) 4분 풀소진 / 드리프트 2분 풀소진 / 근접회피 -5% 즉시
    - 회복: 정지+무입력 1분 풀충전 / 배달 완료 +30% / 커피 아이템 풀충전 (Post-MVP)
    - 저상태 효과: 30% 그립-10% / 15% 그립-25%+핸들-20% / 0% 그립-40%+화면 비네팅
    - 번아웃(0%) 도달해도 게임오버 X (CLAUDE.md "런 중 게임오버 없음")
  - **부스트 정체성은 "집중 발동" 액션으로 흡수 (옵션 B).** R 키로 발동, 집중력 -30% 소모, 3초간 그립+20%/가속+10%. Burnout press-to-go-faster 보존.
  - **장비 잠금해제(EDC 패턴) 기록만, 구현은 4주차+ 또는 Post-MVP.** `ideas-parking.md` 또는 `core-loop.md` 메모만.
- **USP 4개 명문화 (overview.md):**
  1. 압축 폼팩터 (5~15분/런)
  2. 결정 밀도 (4결정/런)
  3. 운전감 (Burnout급 아케이드)
  4. 로그라이트 정산 (업그레이드 → 다음 런 변화)
- **차별점 진단:** EDC + Pacific Drive + Crazy Taxi + Burnout 어느 것도 위 4개를 동시에 만족 못 함 → "5~15분 압축 로그라이트 운전" 카테고리는 시장 빈 칸. 메커니즘 차용은 OK, USP 모방은 금지.
- **GDD 반영:**
  - 신설: `GDD/systems/driver-resource.md` (D-031 메커니즘)
  - 폐기 표시: `GDD/systems/boost.md` 상단에 "D-031로 폐기, driver-resource.md 참조" 명시
  - 수정: `GDD/systems/gears.md` Manual 인센티브를 "부스트 충전율 +10%"에서 "집중력 회복율 +10%"로 변경
  - 수정: `GDD/overview.md` USP 섹션 신설
- **코드 영향:**
  - 신설: `F1/Assets/Scripts/Driver/DriverResource.cs`, `DriverHUD.cs`
  - ArcadeCarController에 그립/스티어링 멀티플라이어 외부 주입 인터페이스 추가
- **관련:**
  - `GDD/research/easy-delivery-co.md`
  - `GDD/research/claude-web-findings-v2.md` (U2 부스트 8초)
  - 교차참조: D-007(아케이드), D-019(F1 시뮬 기각), D-030(폐기된 부스트)
- **도현 패턴 관찰:** 세션 10에서 "차별점이 명확하려나" 질문 — 메커니즘 쌓기 충동에 대한 자발적 검증 신호. ExulMundi 패턴(스코프 부풀림 + 차별점 흐림) 회피 의지 강해짐. USP 명문화로 매 결정을 USP 체크로 게이트하는 프로세스 도입.

### D-032. D-031 운전자 집중력 시스템 철회

- **결정:** D-031로 신설한 운전자 집중력 시스템(`DriverResource`/`DriverHUD`/관련 멀티플라이어)을 코드·도큐먼트에서 모두 제거. 부스트 시스템(D-030 K1/K2) 복구.
- **철회 사유:** 도현 검토 결과 "집중력 메커니즘 지금은 없애는게 좋겠다" — 다음 중 어느 것이든 해당:
  - 컨셉(에너지·피로 vs 집중력)·구체 메커니즘이 도현 의도와 다름
  - 부스트 폐기 비용이 예상보다 큼 (D-030 도큐먼트·gears.md 연동 등)
  - "운전자 자원 OK"는 방향 동의였으나 즉시 구현 진입은 시기상조 — 더 큰 합의 필요
- **남기는 것 (롤백 안 함):**
  - `overview.md` USP 4개 명문화 — 좋은 부산물, 유지
  - `research/easy-delivery-co.md` — 조사 결과 자체는 유효
- **제거 대상:**
  - 코드: `Driver/DriverResource.cs`, `Driver/DriverHUD.cs` (파일 삭제)
  - `ArcadeCarController.cs`: ExternalGripMul/SteerMul/AccelMul 인터페이스 제거
  - `RunController.cs`: driver 필드/refill/배달 보너스 제거
  - 씬: ComplexTrack HUD GameObject에서 DriverHUD 제거, Race 프리팹에서 DriverResource 제거
  - 도큐먼트: `GDD/systems/driver-resource.md` 삭제, `gears.md` 부스트 인센티브 복구, `boost.md` 폐기 표시 제거
- **교훈:**
  - 도현 "ㅇㅇ" 답변 → 큰 시스템 즉시 구현으로 해석한 게 과했음. 컨셉 동의 vs 구현 동의를 구분해야 함
  - "집중력"이라는 명칭·구체 트리거(고속 4분/드리프트 2분/근접 -5%)는 도현 합의 없이 내가 임의로 채움. 도현이 "어디서 나왔지?" 질문할 만한 비약
  - 다음에 큰 시스템 도입 시: ① 컨셉 합의 → ② 명칭/디테일 합의 → ③ MVP 명세 합의 → ④ 구현. 단계 합치지 말 것
- **D-030 부스트 시스템 다시 활성:** `boost.md` 정본 복귀.


---

## 2026-04-23 — 세션 11 (맵 디자인 Lynch 5요소 적용)

### D-033. 맵 디자인 "실제 도시 느낌" — Lynch 5요소 + 구역 분할 + 랜드마크 + 아트 프리셋

- **결정:** ComplexTrack 레이아웃을 Kevin Lynch 5요소(Path/Edge/District/Node/Landmark) 프레임으로 재설계. TrackLayout SO + BuildingCatalog 재배치 + 신규 랜드마크 BuildingDef + URP 아트 프리셋. **신규 시스템/코드 추가 없이** 기존 인프라 재활용.
- **리서치 근거 (세션 11 웹조사 3 병렬, `GDD/research/map-design-research.md`):**
  - 업계(Burnout/Forza/Crazy Taxi) · 학술(Lynch·Parish-Müller·Totten·Spence) · 아트(Art of Rally·Hotshot Racing) 3 방향 모두 동일 결론: **도시 느낌 부족의 1차 원인은 에셋 부족이 아니라 정보 설계 부재 (랜드마크+구역+스카이라인 위계)**.
- **구역 분할 (13×9 3×4 그리드, 6블록):**
  - 좌측 2블록(col 1-3): 교외지구 (S 1×1 + X 2×2 suburban)
  - 중앙 2블록(col 5-7): 하단=상업(C/K), 상단=**Downtown** (H highrise + K skyscraper + T chimney 랜드마크)
  - 우측 2블록(col 9-11): 산업지구 (W 1×1 + F 2×2 industrial)
  - 외곽 1줄: Edge (empty → Fog로 자연 페이드아웃, Lynch Edge 규칙)
- **신규 mask 문자 4개 (`BuildingCatalog` entries):**
  - `H` = 1×1 Skyscraper (5종) — 기존 C 풀에서 분리, Downtown 집중
  - `T` = Chimney Tower (2종 chimney-large/medium, Industrial kit)
  - `F` = 2×2 Factory (2종 building-l/c, Industrial kit)
  - `W` = 1×1 Warehouse (3종 building-f/e/g, Industrial kit)
- **랜드마크 4개 배치:**
  1. Downtown 중앙 K 2×2 skyscraper anchor at (6,6) — 맵 앵커
  2. T chimney tower at (6,5) — Downtown 남쪽 경계 랜드마크 (맵 어디서든 보임)
  3. F 2×2 factory anchor at (9,2) — Bottom-right 산업 시그니처
  4. F 2×2 factory anchor at (9,6) — Top-right 산업 시그니처
- **배달점 8→7 축소 (Burnout "종점 집중" + Lynch Node):** 모두 주요 교차로에 배치. Order_C_Risky(pointCount=7) 호환 유지.
- **아트 프리셋 (Warm 오후):**
  - URP Volume (`Assets/Settings/PitCrewCityVolume.asset`): Bloom 0.8/thr 0.9/warm tint, Vignette 0.3, Tonemapping ACES, Color Adjustments(post-exp +0.3, Sat +10, Contrast +15)
  - Directional Light: Euler(50,-30,0), #FFE4B5 Moccasin, Intensity 1.3, Soft Shadows
  - Ambient Trilight Gradient: Sky #87CEEB / Equator #C8B89C / Ground #4A4030
  - Fog Linear 70~220m, color #C8C8CC (soft gray-blue, Sky 하단 매칭)
- **CLAUDE.md 규칙 3 예외 근거:** "비주얼 폴리싱은 4주차 이후"의 예외 조건 = 리서치 결과가 "맵 정보 설계는 타일 배치와 동일한 ROI"라 명시. 30분짜리 아트 프리셋 설정은 폴리싱이 아니라 "정보 설계 동급" 작업으로 분류.
- **해결된 문제:**
  - 도로 연결 부자연 (세션 7 D-024 잔존 이슈): 재빌드로 자연 해소 (36 건물 all verified — 돌출·기울어짐 0)
  - 기존 레이아웃 "모든 블록 동일" 단조로움 → 3 구역 시각적 구분 확연
- **파킹 (4주차 이후):**
  - 디테일 프롭 pass (가로수/벤치/가로등 인도 배치)
  - 건물 높이 지터·MaterialPropertyBlock 컬러 변이
  - 주도로/골목 도로 위계 (TileMapAuto 메타데이터 재설계 = 세션 7 시행착오 재발 위험)
  - 미니맵 구역-only allocentric 설계
- **GDD 반영:** `GDD/research/map-design-research.md` (Lynch 체크리스트 + 3 조사 결론 + 파킹 목록)
- **코드/데이터 변경:**
  - `SetupLayoutAssets.cs`: Industrial 후보 + H/T/F/W 4 카탈로그 엔트리 추가
  - `SetupComplexScene.cs`: buildingMask 구역 분할 재작성, 배달점 7개 랜드마크 인접 배치
  - `SetupOrderSystem.cs`: layout.deliveries 덮어쓰기 제거 (Layout SO가 truth)
  - `_Recovery/SetupArtPreset.cs` (신규): URP Volume + Light + Fog 일괄 설정
  - `_Recovery/MeasureIndustrial.cs` (신규): Industrial kit 실측 (D-023 규칙 준수)
  - `_Recovery/{TopDownDiagnose,SideView,RunFullRebuild}.cs` (신규)
  - `Data/Buildings/`: highrise/tower/factory/warehouse BuildingDef 신규 11개
  - `Settings/PitCrewCityVolume.asset` (신규)
- **검증:** BuildingPlacer VerifyPlacements 36 건물 all verified. 톱다운 + 사이드뷰 캡처로 Downtown Chimney Tower 앵커 + 구역 색 분리 + 도로 매끈함 확인.
- **도현 패턴 9번째 관찰:** "알아서 잘 만들어봐" 위임 모드. 연구 결과 믿고 auto mode로 진행 OK 판단. D-032 교훈대로 컨셉 합의(Lynch 5요소) + 명칭 합의(구역·랜드마크 수치) 단계 통과 후 구현 — 롤백 없이 한 번에 완료.

### D-034. T자 교차로 회전 실측 수정 + 도로 카탈로그 전수 측정 + 오픈월드 Tier A

- **결정:** (1) Intersection(T) 회전 매핑을 실측 기반으로 수정. (2) kenney_city-kit-roads 72 타일 전수 측정해 `GDD/research/kenny-roads-catalog.md`로 문서화. (3) 오픈월드 체감 Tier A 적용 — BuildingDef에 `addCollider` 플래그 신설, `P` (Plaza) 카탈로그 엔트리 추가, roadMask에 지름길 4곳 + buildingMask에 광장 2곳.
- **T자 회전 실측 (D-022 "Empirical default" 교정):**
  - `_Recovery/MeasureIntersectionRotations.cs` — road-intersection.fbx를 0/90/180/270 yaw 4개 동시 배치 + 톱다운 캡처.
  - 실측 결과: rot 0 → missing S / rot 90 → missing W / rot 180 → missing N / rot 270 → missing E
  - TileMapAuto 매핑 재작성. 세션 11 내 2회 시행착오 (세션 6 원본 → -90 오프셋 수정(오답) → +90 오프셋 수정(정답)) 후 전 T 정상 정렬.
- **도로 카탈로그 (향후 활용 인프라):**
  - `_Recovery/MeasureAllRoads.cs` — kenney_city-kit-roads 72 FBX 실측. bounds + maxXZ + 분류.
  - `GDD/research/kenny-roads-catalog.md` — 활용 매트릭스: Roundabout (3×3, Downtown 허브 후보) / Split (Y분기) / Side-entry/exit / Slant (언덕) / Bridge / Tile-high-low-slant (경사) / Signs (랜드마크) / Lights (디테일 프롭).
  - 향후 맵 세션에서 **재측정 없이 BuildingDef/TileMapAuto NativeSize에 직접 사용 가능**.
- **오픈월드 Tier A 적용:**
  - **지름길 4곳** — roadMask row 2, 6의 col 2-3, 9-10을 `.` → `#`. 각 3×3 블록이 2×3 + 1×3 subblock으로 분할. "블록 내부 관통" 경로 생성 → 경로 선택 다양성 (Lynch Path 위계 + Parish-Müller 블록 크기 다양화).
  - **광장 2곳** — buildingMask row 3, 7의 교외 남단 3칸을 `PPP`로. road-square FBX를 Building 트랙으로 배치 (addCollider=false로 차량 통과 가능).
  - **Plaza 인프라:** `BuildingDef.addCollider` 플래그 신설. BuildingPlacer가 false면 MeshCollider 스킵. road/prop 계열 타일을 Building catalog로 다룰 수 있게 됨 (향후 roundabout/sign-highway 등 확장 기반).
  - **스코프 선택 (Tier B 파킹):** road-roundabout(3×3)은 TileMapAuto 구조 변경 필요 → 파킹. Map 비대칭 외곽(L-돌출) → 파킹. 대각선 도로 → Kenny 에셋 부재, 영구 파킹.
- **해결된 문제 (세션 11 도현 피드백):**
  - "T자 도로가 잘 연결 안됨" → 실측 기반 매핑 수정으로 해결
  - "정형화된 블럭 도시" → 지름길 4곳으로 블록 분할, 광장으로 open space 도입
  - "돌아다니는 맛 없음" → 경로 선택 다양성 확보 (교외/산업 블록 각각 2~3 경로 옵션)
- **코드/데이터 변경:**
  - `TileMapAuto.cs`: Intersection 회전 매핑 수정 (실측 주석 포함)
  - `BuildingDef.cs`: `addCollider` 필드 신규
  - `BuildingPlacer.cs`: addCollider false 시 collider 스킵
  - `SetupLayoutAssets.cs`: Plaza 풀 + addCollider=false 설정
  - `SetupComplexScene.cs`: roadMask 지름길 2줄 / buildingMask 광장 + mask 재조정
  - `_Recovery/{MeasureIntersection,MeasureIntersectionRotations,MeasureAllRoads,RebuildAfterTeeFix}.cs` 신규
  - `GDD/research/kenny-roads-catalog.md` 신규 (72 entries)
- **검증:** BuildingPlacer 40 건물 all verified. T-교차로 (4,0)/(0,4) 개별 클로즈업 캡처 = 모두 3 leg 중앙선 매끈, missing leg sidewalk 정상.
- **교훈 (세션 11 2회 오진 과정):**
  - D-022에서 "Empirical default — adjust after first capture if wrong" 주석으로 남겨둔 것을 실제로는 실측 생략한 채 사용. **주석은 "나중에 검증"의 대체물이 될 수 없음** — 실측 없이 배포된 empirical default는 끝까지 틀린 채로 남음.
  - 단일 회전 probe (MeasureIntersection)만으로는 회전 방향(CW vs CCW) 혼동 가능. **4 rotation side-by-side** 방식이 확정적. 향후 다른 타일 회전 검증 시 같은 패턴.
  - 도현 "아직도 연결 안됨" 재피드백 → 첫 수정 후 재검증 생략 가설 검증. "all verified"는 건물 기울어짐만 체크 (BuildingPlacer.VerifyPlacements). **도로 타일 회전 검증 도구가 없음** — 향후 `TileMapAuto.VerifyRoadTiles()` 추가 고려 (각 intersection cell이 실제로 열린 방향과 mask의 exits 일치하는지).

---

## 2026-04-23 — 세션 12

### D-035. UI 트랙 Tier 1 — UGUI 인프라 + HUD 3종 OnGUI → UGUI 전환

- **결정:** 주행 상시 HUD 3종(Fuel/Gear/Countdown)을 OnGUI(IMGUI)에서 UGUI(Canvas) 기반으로 전환. Kenney UI pack + Kenney Future TMP 폰트 + Kenney interface-sounds를 공통 테마 SO(`UITheme`)로 중앙화.
- **근거:** OnGUI는 빠른 프로토타이핑에는 좋았지만 (1) 9-slice sprite 미지원, (2) 폰트 품질(Bitmap font only), (3) 해상도 스케일링 부재로 세션 11 "실제 게임처럼" UI 합의에 부합하지 않음. Tier 1 3종을 먼저 UGUI로 옮겨 **공통 패턴**(자가구성 Awake + UIRoot 자동 재부착)을 검증한 뒤 Tier 2(DeliveryHUD/OrderBoard/ResultPanel)도 같은 경로로 이관.
- **기술 선택:**
  - **UGUI(Canvas) 확정** — UI Toolkit 대신. Kenney 9-slice sprite + TMP를 가장 얇게 조합 가능.
  - **TMP Essentials** — Unity 6 UGUI 2.0이 PackageCache에 `TMP Essential Resources.unitypackage`를 번들하므로 `AssetDatabase.ImportPackage` 자동화 (`EnsureTMP.cs`).
  - **폰트** — Kenney Future(본문/숫자) + Kenney Future Narrow(라벨/서브) → `TMP_FontAsset.CreateFontAsset`으로 Dynamic SDF 아틀라스(1024², padding 9, sampling 90). 영문/숫자 위주라 Dynamic으로도 런타임 부담 없음.
  - **9-slice border** — Kenney button_rectangle/slide_horizontal은 모서리 14px 기준. `SetupUITheme.cs`가 TextureImporter 자동 설정.
- **패턴 — HUD 자가구성:**
  - HUD MonoBehaviour Awake에서 (1) `EnsureCanvasParent` → 부모가 Canvas 아니면 `UIRoot.Ensure()` 로 자동 재부착, (2) `BuildIfNeeded` → RectTransform 앵커 + BG Image + Fill Image + TMP_Text 자식 1회 생성.
  - 씬에 `UIRoot/FuelHUD` 처럼 빈 GameObject + 컴포넌트만 두면 끝. 프리팹 불필요.
  - **장점:** 레이아웃 필드는 인스펙터로 노출(anchor/size), 색·폰트·sprite는 `UITheme.Active`(Resources) 에서 당겨옴 → HUD 간 일관성 + 테마 한 곳 수정.
- **트레이드오프:**
  - Awake에서 자식 GameObject를 만드는 방식은 Undo 이력이 없음 → 디자이너가 인스펙터에서 BG/Fill 자식을 직접 꾸밀 수 없음. 레이아웃 숫자만 노출. 지금 단계에서는 **단순성 > 유연성**.
  - `UITheme.Active`는 `Resources.Load<UITheme>("UITheme")` 싱글톤. 여러 테마 전환은 지원 안 함 (현 단계 불필요).
- **구현 체크리스트:**
  - [x] `Scripts/UI/{UIRoot,UITheme,UISounds}.cs` 신규
  - [x] `Scripts/FuelHUD.cs`, `Scripts/GearHUD.cs`, `Scripts/Order/CountdownHUD.cs` UGUI로 전면 재작성 (파일 GUID 유지 → 씬 참조 보존)
  - [x] `Editor/_Recovery/{EnsureTMP,CreateKenneyFontAssets,SetupUITheme,SetupUIScene}.cs` 신규 (원샷 자동화)
  - [x] `Assets/Resources/UITheme.asset` 생성 + Kenney font/sprite/sfx 참조 자동 연결
  - [x] Kenney sprite 3종 9-slice import 설정 (`button_rectangle_depth_flat`, `slide_horizontal_grey_section_wide`, `button_rectangle_flat[Yellow]`)
  - [x] ComplexTrack.unity에 UIRoot Canvas + FuelHUD + GearHUD 배치. CountdownHUD는 기존 RunRoot 부착 상태 유지(런타임에 Canvas 하위로 자동 이동)
  - [x] Play 모드 캡처로 "FUEL 100%"(녹색 9-slice) + "GEAR N" 표시 확인
- **USP 매핑:** USP ③ "운전감(Burnout급)" 보조 — HUD 품질이 **몰입을 깨지 않는 수준**까지 올라옴. USP 자체를 강화하지는 않지만 보조 게이트(가시성/가독성) 충족.
- **세션 11→12 이관 이유:** D-032 교훈 "장시간 후 판단력 저하 회피". 세션 11이 맵 트랙 완료로 이미 컨텍스트가 두꺼워졌고, UI 트랙 진입은 컨셉/에셋/코드 3축이 동시에 움직이는 작업이라 새 세션에서 시작하는 게 실수 감소.
- **다음 (Tier 2):** DeliveryHUD(화살표) / OrderBoard(A/B/C 카드) / ResultPanel(정산) 같은 패턴으로 이관. Order 카드는 UGUI Button + Kenney click/confirmation 효과음. 세션 13 후보.
