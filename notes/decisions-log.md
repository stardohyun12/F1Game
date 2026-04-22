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

### D-XXX. [질문 제목]
- **결정:**
- **근거:**
- **관련 GDD 문서:**
