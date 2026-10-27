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

---

## 2026-04-24 — 세션 13

### D-036. 맵 확장 15×9 + 도로 시각 변종 (Crossing/Dead-end) + 동쪽 L 돌출

- **결정:** 13×9 → 15×9 확장. 동쪽 cols 13-14에 ㅗ자 L 돌출. Straight 일부를 Pedestrian Crossing으로 시각 변종. road-end.fbx → road-end-round.fbx 교체.
- **근거:**
  - 도현 피드백: "맵이 단조롭다, 좀 더 넓게 + 높낮이 + 배치 단조로움 해결".
  - 높낮이/배치 UX는 스코프 초과 → ideas-parking.md "v1 MVP 이후 고도화 파이프라인" H1~H5로 이관.
  - 맵 확장 + 도로 다양성은 **TileMapAuto의 `#` mask char 체계 안에서 해결 가능** → 저비용으로 즉시 실행.
- **mask char 체계 확장:**
  - `#` 일반 도로 (기존)
  - `X` pedestrian crossing (Straight 시각 변종, 연결성 동일)
  - `D` driveway 진입로 (End 변종, 1 exit 강제)
  - IsRoad()가 세 char를 전부 road로 취급 → 이웃 탐색 정상. Kind 결정 후 char 기반 override.
- **새 Kind 등록:** `Kind.Crossing`(road-crossing.fbx) / `Kind.Driveway`(road-driveway-single.fbx). Dead-end `Kind.End`는 기존이지만 source를 `road-end-round.fbx`로 교체 — 교외 분위기 round cap.
- **맵 15×9 구조:**
  - 동쪽 L 돌출 (cols 13-14, rows 3-5): row 4에서 main road가 동쪽으로 1칸 뻗은 뒤(`col 13 row 4`) 북/남 분기(`col 14 rows 3/5` dead-end). "ㅗ자" = Intersection at (14,4) + End at (14,3), (14,5).
  - Crossing 9개 (cols 1/5/9 × rows 0/4/8) — 남/중/북 main horizontal 각 3개.
  - 지름길 유지 (row 2, 6 shortcut, D-034 Tier A).
- **배달점 7개 재배치:** `FactoryEast(12,2)` / `EastBranchEnd(14,4)` 신규(L 돌출 T 교차점) / `NorthGate(6,8)` / `DowntownHub(6,4)` / `SouthGate(8,0)` / `SuburbWest(0,6)` / `SuburbSouth(0,2)`. 이전 `IndustryNorth(12,6)`를 EastBranchEnd로 교체 — 맵 확장을 배달 동선에 반영.
- **RunFullRebuild에 SetupUIScene.Execute 추가:** SetupComplexScene이 새 씬을 만들면서 UIRoot 날리는 문제를 자동으로 해결 — Step 5 단계로 FuelHUD/GearHUD GameObject 재생성.
- **검증 결과 (계층):**
  - `Tile_Crossing_{1,5,9}_{0,4,8}_r0` 9개 ✓
  - `Tile_Intersection_14_4_r270` ✓ (east L T)
  - `Tile_End_14_3_r270` + `Tile_End_14_5_r90` ✓ (dead-ends)
  - `Tile_Straight_13_4_r0` ✓ (main → L 연결)
  - 동쪽 확장 건물 8개 (S_13_1, S_14_1, S_13_2, S_14_2, H_13_6, H_14_6, H_13_7, H_14_7) 배치 ✓
- **USP 매핑:** USP ② "경로 계획 결정 밀도" 보조 — dead-end = 왕복 비용, crossing = 시각 랜드마크로 경로 기억 돕기. USP ③ "운전감"은 맵 다양성으로 주행 반복성 완화.
- **트레이드오프:**
  - **+** 비대칭 L 돌출로 "완벽 격자" 느낌 탈피. Dead-end 2개는 "막다른 골목 = 경로 실수 비용" 체감 유도.
  - **+** Crossing 9개가 주행 중 "여긴 어디쯤" 시각 앵커 역할. Kenney crossing FBX는 straight와 정확히 동일 footprint → 이음매 깨짐 0.
  - **−** 15 cols × 9 rows = 90m × 54m. 이전 78m × 54m보다 11m만 확장 (체감 크지 않을 수 있음). 필요 시 17×11로 추가 확장 (세션 14 후보).
- **파킹된 대안들 (ideas-parking.md H4~H5 + 신규):**
  - **맵 높낮이**: ideas-parking H4. 3주차 경로 계획 단계로.
  - **Roundabout 3×3**: ideas-parking H5. TileMapAuto 멀티셀 지원 필요.
  - **Split Y분기 (road-split, 1×2)**: 기록 안 된 파킹 — TileMapAuto 2셀 앵커 로직 필요. 세션 14 이후 검토.
  - **비대칭 외곽 (L-돌출 더 과감한 버전)**: 이번 세션은 2-cell 범위로만. 추가 확장은 재미 검증 후.

### D-037. v1 MVP 이후 고도화 파이프라인 명문화

- **결정:** 도현의 세션 13 제안(화물 배치 UX + Shock 물리 고도화 + 허브 씬) 4개를 `ideas-parking.md`에 **"v1 MVP 이후 고도화 파이프라인"** 섹션 H1~H5로 통합 기록.
- **근거:**
  - 제안들이 전부 MVP 스코프 초과이지만 **서로 맞물리는 시스템 군** (허브 씬 = 배치 UX의 무대 = Shock 고도화의 피드백 공간). 개별 파킹보다 **묶음 파이프라인**으로 관리가 명확.
  - D-031 철회 교훈: 큰 시스템 군을 MVP 중간에 끼워넣지 말 것. 대신 v2 레이어로 명시적 약속.
- **H1~H5 요약:**
  - H1. 허브 씬 (별도 씬) — 4주차 "허브 → 런 → 허브" 메타 루프와 통합
  - H2. 차량 내부 화물 공간 + 배치 UX — Pacific Drive 스타일 Tetris-like
  - H3. Shock 물리 고도화 — Rigidbody + Collider 실시간 시뮬
  - H4. 맵 높낮이 — 3주차 경로 계획에 자연 편입
  - H5. Roundabout 3×3 — Downtown Hub 시각 앵커
- **언제 재검토:** MVP 완성 후 (5주차 시작). 그 시점에 유저 피드백(내부 테스트) + 상대적 우선순위로 재평가.
- **이번 세션 선택:** 도현이 "맵 단조로움"만 지금 해결 요청 → 저비용 확장(D-036)만 실행. H1~H5는 기록만.

### D-038. TrackConnectivityVerifier — 맵 빌드 후 자동 구조 검증 (재발 방지 인프라)

- **결정:** `F1/Assets/Editor/TrackConnectivityVerifier.cs` 신규 에디터 스크립트. RunFullRebuild Step 6으로 자동 실행. roadMask를 BFS로 훑어 (1) 고립 cell(exits=0), (2) startCell 도달 불가 road, (3) 도달 불가 End 여부를 리포트.
- **발단:** 도현 세션 13 피드백 "타일 끝나는 지점이 이상함" + "재발 방지 조치".
  - 진단 과정: 톱다운 캡처로 우측 cols 13-14 고립 건물 먼저 발견(이전 D-036 후속 fix). 그 다음 End 4개(`Tile_End_{2,10}_{2,6}`)가 지름길 `.###` 패턴의 **spur**로 작동 — 외곽 main에서 직접 접근 불가(`col 1/11 = '.'`). 회전은 맞지만 구조적으로 어색.
  - 수정: 지름길을 `#####` 관통 형태로. End 4개 → 0개, 87 road cells 전부 도달 가능.
- **방지 시스템 구현:**
  - `TrackConnectivityVerifier.Verify(layout, logPrefix)` — BFS로 도달 가능 집합 계산 → 비교.
  - 리포트 필드: `ok / totalRoad / reachable / orphans / ends / endsUnreachable / isolated`
  - OK 시 `✓ 연결성 OK` 한 줄. 실패 시 cell 좌표 리스트 LogError.
  - `PitCrew > Verify Track Connectivity` 수동 메뉴 + `RunFullRebuild` Step 6 자동.
  - `IsRoad`는 `#/X/D` 세 char 모두 road 취급 (TileMapAuto와 일치).
- **설계 원칙 (D-026 "침묵 실패 방지"와 동일):**
  - 새 타일/마스크 도입 시 **"하나라도 이상하면 콘솔에 경고"**. 시각 확인은 보조.
  - BuildingPlacer.VerifyPlacements(건물 기울어짐)와 쌍을 이룸: 건물은 기울어짐, 도로는 연결성.
  - Rebuild 실행 시 항상 리포트 나오므로 **"어? End 개수 왜 늘었지?"** 같은 감지 가능.
- **제한사항:**
  - 회전 매핑(rot 0/90/180/270 → 실제 exits)은 검증 안 함. D-034 T-교차 같은 회전 버그는 별도 도구(`MeasureIntersectionRotations`) 필요.
  - 타일 mesh 이음매(crosswalk 중복 등)는 시각만 확인 가능. 자동화 비현실.
  - BuildingPlacer.addCollider=false 건물과 road 겹침은 의도된 케이스(광장 'P'), 현 검증에서 못 잡음. 필요 시 별도.
- **향후 확장:**
  - 회전 sanity (TileMapAuto의 `RotationFor` 딕셔너리와 실 배치 tile의 rotation 비교)
  - 각 tile의 `Kind`와 exits 짝 검증 (예: Intersection인데 exits가 2개면 이상)
  - `TrackLayout.OnValidate` 훅으로 에디터 필드 변경 즉시 검증 (이중 안전망)
- **세션 13 결과 (수정 + 검증 후):**
  - 15×9 맵, 87 road cells, Ends 0, Crossing 11, 동쪽 cols 13-14 완전 ring.
  - 지름길 2곳(row 2/6) 관통 구조로 성숙.
  - TrackConnectivityVerifier가 매 rebuild에서 `Ends=0 ✓` 확인.

---

## 2026-04-25 — 세션 14 (기획 재검토: 1주차 재미 판정 + 마을 배송 컨셉 전환)

### D-039. 부스트 시스템 최종 철회

- **결정:** D-030 K1/K2 "부스트 충전 이중화 + 8초 풀바" 최종 폐기. D-031(집중력 대체) → D-032(복구) 경로 모두 거친 후, 1주차 Day 7 재미 검증 결과에 따라 도현이 **"부스터 시스템은 없는 게 좋을 듯"**으로 완전 철회 결정.
- **근거:**
  - 세션 13까지 부스트 코드 미구현 — 철회 비용 0.
  - 1주차 Play 결과: "주행 자체의 재미 부족" + "주행 중 신경쓸 요소가 필요". 부스트는 "더 빠르게"만 주는 도피 장치 → 근본 문제 미해결.
  - D-040 컨셉 전환(레이싱 배송 → 마을 배송)과 부스트 장르 문법이 불일치. 마을 배송에는 레이싱 가속 아닌 "환경 상호작용"이 긴장 요소.
- **반영:**
  - `GDD/systems/boost.md` 상단에 "D-039 최종 폐기" 표시 (다음 세션 코드 작업 시 정리).
  - `GDD/systems/gears.md` Manual 인센티브 "부스트 충전율 +10%" 제거 (다음 세션).
  - `dev-plan/feature-list.md` 2주차 부스트 체크리스트 삭제.
  - `notes/ideas-parking.md`에 "v2 재검토 가능" 수준으로 이관 — 완전 기각 아님, "집중 발동" 같은 환경 상호작용 형태로 재등장 여지.
- **교훈:** D-030 → D-031 → D-032 → D-039 경로는 "큰 시스템 추가는 코드 작업 전 검증 루프 최소 2회" 룰 정당화. 코드 미구현 단계 철회는 비용 0 — 같은 방식 계속.
- **교차참조:** D-030, D-031, D-032, D-040.

### D-040. 컨셉 전환 — "레이싱 배송" → "현실 마을/시골 배송"

- **결정:** PIT CREW 정체성을 "5~15분 압축 로그라이트 **레이싱** 배송"에서 **"실제 있을 법한 마을/시골에서의 배송"**으로 전환. 레이싱 서킷 문법 포기, 시뮬+탐험 각도 도입.
- **1주차 Day 7 재미 판정: "그럭저럭/NO".** 도현 피드백 3건:
  1. "맵 돌아다니기 자체의 재미 없음"
  2. "주행할 때 신경쓸 요소(실시간 화면 관리)가 있으면 좋겠음"
  3. "맵 크기/생김새가 비슷하고 오픈월드에서 느끼는 돌아다니는 재미 없음"
- **근거:** CLAUDE.md 규칙 2 "1주차 끝: 차 몰기 자체가 재밌는가? NO면 튜닝"의 실제 발동. 튜닝이 수치 조정 범위가 아닌 **컨셉 재편** 수준이어야 해결.
- **유지되는 핵심 (기존 D-XXX 그대로):**
  - 5~15분 런 + 3단계 루프 + 로그라이트 + Kenney 에셋 한정 + 아케이드+무게감 + USP ①②④.
- **변경되는 것:**
  - 레퍼런스: Forza/Burnout/Crazy Taxi → **Euro Truck Simulator 2 / Snowrunner / Pacific Drive / Death Stranding / Short Hike** 추가.
  - 평가축: 랩타임/드리프트 점수 → **배달 완수 + 화물 상태 + 시간 여유**.
  - 긴장 요소: 레이싱 메커닉(부스트/드래프트) → **환경 긴장** (보행자/신호등/협로/공사/열차/화물 Shock).
  - 맵 문법: 격자 서킷 → **마을 지형 모사** (랜드마크 밀도 + 고저차 + 구역 정체성 + 열차 같은 거대 이동 요소).
- **MVP 재편입 (이전 파킹):**
  - **H4 맵 고저차 → MVP 편입.** 3주차 "경로 계획" 단계에 통합. 경사 = 연료↑ 속도↓ + 경로 선택지 다양화. TileMapAuto Y offset + road-slant/tile-slantHigh 활용 (D-034 카탈로그).
  - **H1 허브 씬 → MVP 편입 (세션 14 도현 결정).** 원래 4주차 "허브 → 런 → 허브" 메타 루프 항목과 통합해 **"시작 전 + 정비/정산 공간"**으로 구체화. 리서치 A Top 3에 재부각된 것 수용.
- **MVP 미편입 (세션 14 도현 결정):**
  - **열차 → v2 유지.** 도현 "나중에 움직이는 걸로". 리서치 A/B 둘 다 v2 추천과 일치. Kenney train-kit은 v2 진입 시 즉시 활용 가능 (D-018 잔류 유지).
  - **주인공 차량 교체 (F1 → delivery/van/truck) → 보류.** 도현 "일단 만들면서 결정". 2~3주차 화물 시스템 구현 과정에서 "F1 톤이 배달과 맞는가" 체감 후 재검토.
- **ideas-parking.md "오픈월드 기각" 방침 정정:**
  - "무한 배회형 오픈월드"(경계·시간제한 없음)만 기각.
  - "마을 느낌의 닫힌 스테이지 + 탐험 가능성"은 **D-040 이후 허용**. 런 경계(시작/종료/시간제한)는 유지.
- **USP 재검토 필요 (다음 세션):**
  - USP ③ "운전감(Burnout급 아케이드)" — 레이싱 문법. "운전의 무게감 + 환경과의 상호작용"으로 rephrase 검토.
  - USP ② "결정 밀도" — 주문 선택 외 "경로/주행 스타일 결정"이 환경 긴장과 엮이는 방향으로.
- **리서치 페이즈 (세션 14 현재, background 에이전트 4개):**
  - 트랙 0: Kenney 에셋 전수 카탈로그 → `GDD/research/kenney-asset-inventory.md`
  - 트랙 A: 마을 배송 컨셉 레퍼런스 → `GDD/research/driving-fun-research.md`
  - 트랙 B: 화물 Shock 명세 + 환경 긴장 요소 → `GDD/research/realtime-tension-research.md`
  - 트랙 C: UI 고급화 → `GDD/research/ui-polish-research.md`
- **다음 세션 진입 조건:** 리서치 4 문서 완성 → 도현과 우선순위 합의 → 2주차 구현(화물 Shock 우선) 진입.
- **교훈:**
  - Day 7 "차 몰기 자체 재미" 판정은 **정말로** 완성 실패 예방 게이트. NO 나왔을 때 수치 튜닝으로 도피하지 않고 컨셉 재평가.
  - D-029/033 "연구→합의→구현" 패턴 재사용. 세션 11 D-033(Lynch) 성공 경험 반복.
  - D-032 교훈 연속 적용: 큰 시스템 전환은 "컨셉 합의 → 리서치 → 명세 → 구현" 4단계 분리.
- **교차참조:** D-007(아케이드), D-033(Lynch 리서치), D-036(15×9 맵), D-037(v2 파이프라인), D-039(부스트 철회), H1~H5(ideas-parking).

---

## D-041 — Packing(충격흡수재) 시스템 (세션 15, 도현 신규)

- **결정:** 박스 안 충격흡수재 4단계(None/Bubble/Foam/Hardcase) 도입. 임계값 ×1.0/1.3/1.6/2.0.
- **이유:** 일반 주행에도 화물 데미지 = 좌절. 임계 튜닝만으론 못 풀음. 출발 전 "포장 등급" 결정으로 운전 자유도 구매.
- **USP 정합:** "결정 밀도 4개 → 5개" (주문/경로/세팅/업그레이드 + 포장).
- **구현:** `Cargo/PackingTier.cs` enum + Helper, `CargoBox.packing` 필드, `OrderJob.defaultPacking`, RunController 자동 적용.
- **MVP:** 기본 Bubble (×1.3) — 좌절 즉시 해소.

## D-042 — CargoShake 패턴 C 재설계

- **결정:** 박스 회전 비활성(default) + 마찰 강화 + 사면 중력 추가.
- **이유:** 회전 Slerp(restRot 복귀)가 갈대 느낌의 근본 원인. 도현 진단 1~3차.
- **결과:** 회전 X + 위치 미끄러짐만 + 멈춘 자리 유지.

## D-043 — 화물 타입 즉시 인식 폴리싱

- **결정:** CCTV 박스 색상 화물별 차별화 + IntegrityHUD 타입 라벨 + 충돌 flash.
- **이유:** 도현 시각 인식 강화. 5종 화물 색깔로 구분.

## D-044 — 사면 중력 + Trunk 회전 매칭

- **결정:** 박스 사면 중력 시뮬 추가 (`Physics.gravity` → local frame). Trunk visual이 차량 회전 매칭.
- **이유:** "바닥에 있는 화물" 느낌 — 차 정지 + 사면이면 박스 사면 따라 미끄러져야.

## D-045 — 충돌 카메라 shake

- **결정:** `CargoBox.OnImpact` → FollowCamera 0.35s shake (속도 비례, decay).
- **이유:** 충격감 강화.

## D-046 — 박스 Floor 정착 + 마찰 강화

- **결정:** Box localY=0.325 (floor top 정확히), staticFriction 0.30, kinetic 3.0, accelScale 0.7.
- **이유:** "시뮬레이션 느낌" 진단. 박스가 floor 안에 박혀있고 마찰 약해 자유롭게 미끄러짐.

## D-047 — SimCam Worldspace Fixed

- **결정:** SimCam을 CargoSimRoot 자식에서 분리, `CargoSimCamRig`(worldspace fixed)로 이동.
- **이유:** trunk가 차량 회전 매칭이라 카메라도 함께 기울어 시점 자체가 기울어짐 → "평지에서도 박스 기울어 보임".

## D-048 — Trunk Yaw-Only Match

- **결정:** Trunk + Camera 둘 다 차량 yaw만 매칭. pitch/roll 무시 → trunk 항상 평면.
- **이유:** "트렁크처럼 평평한 곳" 진단. 차량 가속/사면에도 trunk visual 평면 유지. 박스만 평면 위에서 미끄러짐.

## D-049 — 박스 회전 강제 안전장치

- **결정:** `CargoShakeSimulator.LateUpdate`에 `if (!enableRotation) boxTransform.localRotation = restRot` 강제. SetupCargoView 메뉴 시 enableRotation/matchCarRotation SerializedObject reset.
- **이유:** 도현 8차 진단 "여전히 박스 기울어짐". 외부 영향 차단.

---

**세션 15 결정 클러스터:** D-040 컨셉 전환 후 화물 시스템 (Phase 1 시그니처) 풀 구현 + 도현 직접 피드백 8차 진단 처치.

---

## 2026-04-30 — 세션 17

### D-050. TileMapAuto/BuildingPlacer heightMask 통합

- **결정:** `TrackLayout.heightMask` 신규 필드 + Slant Kind 4종 + mask char `n/s/e/w/=/+`로 도로 elevation.
- **이유:** "갈수 있는 길자체 높낮이" 도현 요청. 빌딩 plateau가 아니라 운전 가능한 ramp.
- **결과:** col 14 N-S 오버패스 4 타일 정확 위치. 단 `road-slant.fbx`가 좁은 wedge라 시각 한계. 다음 세션에 road-bridge + cube ramp 대체안.

### D-051. 컨셉 정정 — F1 복귀 서사 (마이크로 vs 매크로 루프)

- **결정:** 피치 정정. **"망한 F1 팀이 배달로 자금을 벌어 머신을 복원하고 F1 복귀"**. 배달은 수단, F1 복귀가 목적.
- **이유:** 도현 진술. "Pit Crew" 이름 + `F1/` 폴더 + 빨간 머신 모두 F1 복귀 서사를 향함을 명시. 결정 채널 #7 ("기본 망가진 → 복구") = F1 복원 트리.
- **결과:** `GDD/overview.md` 피치 + 매크로 루프 섹션 갱신. 매크로(머신 복원) ↔ 마이크로(한 런 5~15분) 직각 관계.

### D-052. 한 런 플로우 정정 — 랩핑이 경로 계획 앞에

- **결정:** 한 런 마이크로 루프 순서를 **주문 선택 → 랩핑 → 경로 계획 → 출발 전 세팅(타이어) → 주행 → 정산 → 업그레이드/F1 복원**으로 정정.
- **이유:** 도현 진술. 기존 overview는 "출발 전 세팅"에 포장이 묶여 경로 계획 뒤였음. 정정 후 포장 결정이 경로 계획 정보(무게·속도 영향)로 들어감 — 결정 의존 그래프 일관됨.
- **결과:** `GDD/overview.md` "한 런의 구조" 섹션 갱신. `Order/PreRunSetupPanel.cs`는 이미 화물·포장 선택 패널이라 코드 변경 영향 작음. 다만 RoutePlannerPanel(미구현)이 PreRunSetupPanel **뒤**에 와야 함.

### D-053. 결정 채널 9개 확정

- **결정:** 메타 진행 dial 9개 확정 — 1.속도 / 2.외형 / 3.맵 접근도 / 4.배송물자 종류 / 5.보관박스 크기 / 6.랩핑 / 7.기본 기능(F1 복원 트리) / 8.지도/내비 / 9.연료탱크.
- **이유:** "역치 빈도 조절 요소" 프레임. #9는 스타듀밸리 스테미나 비유 — 한 런 행동량 상한.
- **결과:** `GDD/decision-channels.md` 작성. baseline×multiplier×additive 구조가 자연스럽게 SOLID Strategy/Decorator로 매핑 (보류한 리팩터 Phase 3 후크).

---

**세션 17 결정 클러스터 (2026-04-30):** OpenWorld_v2 신규 씬 + heightMask + 컨셉 정정(F1 복귀) + 결정 채널 9개 + 플로우 정정.

---

## D-050 — 산업 표준 인프라 도입 (.gitattributes + asmdef + conventions, 세션 16)

- **결정:** 워크트리 → main 머지 + Git LFS(Conservative) + asmdef 2개(Runtime/Editor) + 자산/코드 관리 규약 1장(`notes/conventions.md`).
- **이유:** "스크립트·에셋 많이 들어올 텐데 산업계 방식으로 관리". 4주 MVP 안에서 수용 가능한 최소 단위로.
- **구현:**
  - `.gitattributes`: Unity YAML → Smart Merge driver(`merge=unityyamlmerge`). 바이너리(FBX/PNG/WAV/TTF) → LFS. 텍스트 eol=lf.
  - `git config merge.unityyamlmerge.driver` 1회 등록 (사용자 환경, `UnityYAMLMerge.exe`).
  - `PitCrew.asmdef` (InputSystem + TMP 참조) + `PitCrew.Editor.asmdef` (Editor 전용 + URP/Core 참조). 컴파일 5~10s 단축, Editor → Runtime 단방향 의존.
  - `notes/conventions.md` — 폴더 구조 / 네이밍 / 워크플로우 / 자산 추가 체크리스트.
- **결과:** 기존 자산 24MB 보존(Conservative migrate 보류). 신규 바이너리 자동 LFS 진입. asmdef 컴파일 통과 (URP Volume/VolumeProfile 참조 1회 fix).
- **교차참조:** 4주차 후 LFS migrate import + asmdef 세분화(Cargo/Vehicle/UI 분리) 검토.

## D-051 — TireSystem ArcadeCarController 통합 (Day 20, 세션 16)

- **결정:** ArcadeCarController가 씬 TireSystem 자동 참조 → `EffectiveMaxSpeedMps` (Soft +5%, Hard −5%) + `ApplySideGrip` 그립 배율 (Soft ×1.4, Medium ×1.0, Hard ×0.7) + 마모 반영.
- **이유:** TireSystem.cs 클래스 단독 존재했지만 controller에 효과 미적용 = 죽은 시스템. 세션 15 잔여 중 가장 즉시 처치 가능.
- **구현:** Awake에서 `GetComponent<TireSystem>() ?? FindFirstObjectByType<TireSystem>()` null 허용. `ApplyAirDrag/ApplyThrottle/ApplySteering`에 `EffectiveMaxSpeedMps` 사용. SetupPhase1Integration에 TireSystem singleton 자동 추가 + Verify 추가.
- **교차참조:** 다음 단계 — 출발 전 타이어 선택 UI (PreRunSetupPanel과 통합 후보).

## D-052 — CargoWarningHUD 반응형 경고 (Day 24, 도현 #3 게임플레이 처치)

- **결정:** 4 화물 데미지 소스(Long G / Lat G / Spin / Tilt) 중 가장 위험도 높은 것 표시. 위험도 = current / warnThreshold. 0.7+ 노랑 → 1.0+ 빨강 깜박. signed로 방향(◀코너 / 코너▶ / 급제동 / 급가속 / 스핀 / 경사).
- **이유:** "맵 단조" 피드백 게임플레이 측면 1차 처치. 예측형(NodeGraph 기반)은 리스크 큼 → 반응형으로 시작.
- **위치:** 화면 상단 중앙 속도계 아래.
- **결과:** 코너·사면 진입 시 즉각 노랑 신호. 일반 주행 중엔 숨김 (HUD clutter X).
- **교차참조:** Phase 2 — 예측형 (NodeGraph 다음 노드 곡률·경사 기반 사전 경고).

## D-053 — 컨트롤 4차 튜닝 (도현 1·2·3·4차 피드백 누적 인하)

- **결정:** 130km/h → 110 → 85 km/h. accelTime 2.0 → 1.4 → 1.0 → 1.6 → 2.2. steerLag 0.12 → 0.06 → 0.025. throttleLag 0.08 → 0.03 → 0.01. lowSpeedRamp 0.5/2.8m → 0.8/1.5m → 0.95/1.0m → 0.85/1.5m. maxTurnDegPerSec 110 → 135. driftMaxSpeedKmh 90 → 85 → 65 (비례).
- **이유:** "묵직 → 너무 빠름" 양 끝 사이 균형. 세션 15 130km/h는 도현 "속도감 부족"에 대한 응답이었지만 전체 톤(배달 게임, 정밀 운전, 화물 손상도) 대비 과함.
- **결과 (4차 도달):** 즉각 응답(거의 0 lag) + 실제 속도 85km/h(맵 사이즈에 적합) + 정지 출발 무게감 약간(0.85 ramp).
- **교차참조:** D-038(세션 14 "운전 안됨") 이후 운전감 6번째 튜닝 라운드.

## D-054 — 한글 폰트 Fallback (NotoSansKR-VF 실패 → malgun)

- **결정:** TMP_FontAsset.fallbackFontAssetTable에 한글 SDF 등록. 1차 NotoSansKR-VF.ttf (OFL) → Variable Font 호환성 문제로 실패 → 2차 malgun.ttf로 재생성.
- **이유:** 도현 #2 "UI 한글 깨짐" + #2-3 "여전히 안 보임". KenneyFuture SDF는 영어만, 한글 글리프 없으면 □ 표시.
- **구현:** `SetupKoreanFallback.cs` editor 메뉴. `Windows/Fonts/`에서 자동 탐지 → 프로젝트 import → `TMP_FontAsset.CreateFontAsset(Dynamic, 4096, multi-atlas)` → `KenneyFuture SDF` + `KenneyFutureNarrow SDF`의 `fallbackFontAssetTable.Add(koreanFallback)`. ApplySession16Tuning이 자동 호출 + `TMP_Text.ForceMeshUpdate` 강제.
- **잔여 의심 (Plan B1 처치 대상):** `EditorUtility.SetDirty + SaveAssets` 강제 호출 누락 가능성 → 직렬화 안 됨. 신규 `ForceRegisterKoreanFallback` 메뉴로 강제 + Verify 검사 추가 예정.
- **라이선스:** malgun.ttf (MS 권한, 개인 빌드만) — 배포 전 OFL 폰트(NotoSansKR-Regular static) 교체 필요.

## D-055 — Suburban Props 자동 배치 (도현 #3 시각 처치)

- **결정:** 기존 TrackLayout 변경 X. 별도 "Props" GameObject root에 도로 인접 빈 셀 55% 확률로 결정론적 배치. Pool: tree-large/small/planter/fence/path-stones.
- **이유:** "맵 단조" 시각 측면. props 없는 맨 셀이 단조함의 큰 비중.
- **구현:** `PlaceSuburbanProps.cs` editor 메뉴. `(c, r) hash`로 deterministic seed → 재실행 시 동일. Collider 제거 (차량 통과 자유, gameplay 영향 X). 기존 Buildings 점유 셀 자동 회피 (이름 패턴 `B_{ch}_{c}_{r}_{w}x{d}` 파싱).
- **결과:** 빈 셀이 풍경으로 채워짐. Layout SO 변경 없어 다른 맵에 동일 메뉴 재사용 가능.

## D-056 — TrafficLight 시스템 (Day 25, 도현 #3 게임플레이 처치)

- **결정:** 1교차로 단순화로 시작 (D-040 트리거 방안 1). Red 5s → Yellow 1s → Green 5s 사이클. 4-way 교차로 자동 검출 후 최대 2개 배치.
- **이유:** D-040 컨셉 전환 시 "신호등/열차" 논의됐지만 1교차로 제한으로 흐름 끊김 회피. 위반 시 `ApplyExternalDamage(15)` → 게임플레이 결정 추가.
- **구현:** `Traffic/TrafficLight.cs` (자식 sphere 3개 자동 생성 + emission 토글) + `Traffic/TrafficLightArea.cs` (BoxCollider trigger, Red + 8km/h 초과 = 위반). `CargoBox.ApplyExternalDamage(dmg, flashSpeedKmh)` public API 신설 — TrafficLightArea + 미래 페널티 진입점.
- **위치:** Editor `PlaceTrafficLight.cs` — TrackLayout roadMask 4-way 자동 검출, idx 짝수 Red 시작 / 홀수 Green 시작 (직교 신호 시뮬).
- **도현 후속 결정:** "신호등 기능 지금은 필요없음" — Plan 폐기, 코드만 보존(ApplyExternalDamage API는 미래 사용 가능).

## D-057 — ApplySession16Tuning 통합 메뉴

- **결정:** SerializeField 기본값 변경이 기존 씬 인스턴스에 자동 반영 안 됨. 한 번 클릭으로 모든 처치 일괄 적용하는 통합 메뉴.
- **이유:** 도현 #1·#2 처치 후 Play해도 변경 안 보였음. 코드만 변경 → 이미 저장된 GameObject는 옛 값 유지하는 Unity 직렬화 패턴.
- **구현:** `Assets/Editor/ApplySession16Tuning.cs` (`PitCrew/Phase 1/Apply Session 16 Tuning`).
  1. ArcadeCarController 17 필드 SerializedObject 강제 설정 (속도/lag/조향/드리프트)
  2. AudioManager + EngineAudio 볼륨
  3. 5 HUD 자식 삭제 → 다음 Play에서 새 fontSize로 BuildIfNeeded 재실행
  4. SetupKoreanFallback (D-054) 자동 호출
  5. PlaceSuburbanProps + PlaceTrafficLight 자동 호출
  6. TMP_Text 전부 `ForceMeshUpdate(true)` (한글 fallback 즉시 반영)
  7. Play 모드 가드 (시작 시 isPlaying ⇒ 거절)
- **결과:** 도현 워크플로 단순화 — ▶ 종료 → 메뉴 1번 → ▶ 시작.
- **교훈:** 코드 변경 시 SerializedObject 반영 패턴을 표준으로 채택. 향후 튜닝 라운드도 이 메뉴에 추가만 하면 됨.

## D-058 — `/loop` 자체 페이싱 모드 운영 (세션 16)

- **결정:** Coplay 자가 검증 + 도현 직접 Play 평가 + 즉시 처치를 단일 세션에서 다회 라운드 진행. 사용자 "계속 단계 진행해봐" 트리거로 8 tick 자동 실행.
- **이유:** 도현 ↔ Claude 핸드오프 비용 누적. 한 라운드(처치 + 검증 + 다음 단계 결정)를 자체 페이싱으로 압축.
- **구현 패턴:** ScheduleWakeup 270~600s. tick마다 Unity 응답 시도 → 응답 시 메뉴 실행 → 결과 verify → 커밋. 응답 없으면 wakeup 갱신.
- **결과:** 도현 입력 1회당 평균 3~5 작업 단위 진행. 단순 튜닝 ↔ 검증 사이클은 자동.
- **한계:** Unity 메인 스레드 점유 시 MCP 타임아웃 (60s) → 다음 tick 대기 필요.

---

**세션 16 결정 클러스터:** Coplay 자가 검증 활성화 + Phase 1 통합 검증(31 OK / 0 FAIL) + 도현 4 라운드 플레이 피드백 처치(컨트롤·UI·오디오·드리프트·한글) + 산업 인프라(.gitattributes/asmdef/conventions) + 잔여 시스템(TireSystem 통합/CargoWarningHUD/Traffic) 일괄 처치.

---

## D-059 — Plan B 4영역 일괄 구현 (세션 16 후반)

- **결정:** 도현 러닝 동안 Auto 모드로 Plan A(문서) + Plan B 4영역(B1~B4) 일괄 구현.
- **B1 한글 fallback 강제 등록:** `ForceRegisterKoreanFallback.cs` editor 메뉴. SerializedObject로 m_FallbackFontAssetTable에 직접 추가 + SetDirty + SaveAssets. Verify 36항목으로 강화.
- **B2 ResultPanel IMGUI → UGUI:** 600×460 → 1600×1000. TMP_Text 사용 → 한글 fallback 자동. 타이어 마모 막대 + 포장 효과 + 시간 보너스 + 화물 이야기.
- **B3 PreRunSetupPanel:** 화물·포장 4 카드 선택 모달. OrderBoard.PickOffer → 모달 → CargoBox.SetPacking → BeginRun. UI 신설, 데미지 감소 로직(packMul)은 이미 작동.
- **B4 도로 마스크 확장:** TileMapAuto Kind.Bridge('B') + PlaceStreetLights (도로 모서리 30% 확률 가로등).
- **결과:** main 33 커밋, Plan B 모두 코드 완료.

## D-060 — 한글 Atlas Texture sub-asset 처치 (도현 #2-6)

- **결정:** TMP_FontAsset.CreateFontAsset 결과의 atlas texture/material을 명시적으로 sub-asset 추가 (AssetDatabase.AddObjectToAsset).
- **이유:** 누락 시 .asset 파일이 dangling reference 보유 → Console "MissingReferenceException: m_AtlasTextures... reassign". TMP가 fallback chain에서 한글 glyph render 못 함.
- **추가 발견:** Apply가 매번 KoreanFallback SDF를 삭제·재생성 → KenneyFuture SDF의 fallback table에 등록된 참조가 missing(null)으로 남음. 첫 fallback이 null이면 chain 끊김. → ForceRegister에서 null 일괄 정리 + UITheme.fontPrimary 직접 교체 (B5 우회).
- **라이선스:** malgun.ttf (MS 권한, 개인 빌드만) — 배포 전 NotoSansKR-Regular 정적 OFL 폰트로 교체 필요.
- **결과:** Play 캡처 "상자 100%" 한글 정상 렌더링.

## D-061 — EventSystem 자동 보장 (도현 #2-11)

- **결정:** ApplySession16Tuning + ResultPanel/PreRunSetupPanel.Awake 모두에서 EventSystem 자동 추가.
- **이유:** 씬에 EventSystem GameObject 없어 UGUI 버튼 클릭 디스패치 안 됨. 도현 "다시 버튼 안 눌림". 기존 OrderBoard는 IMGUI(OnGUI)라 EventSystem 없어도 동작했지만 신규 ResultPanel/PreRunSetupPanel은 UGUI라 필수.
- **구현:** UnityEngine.InputSystem.UI.InputSystemUIInputModule 사용 (프로젝트가 InputSystem 1.18 사용 → StandaloneInputModule 비호환).
- **교차참조:** Verify에 EventSystem 검사 추가.

## D-062 — 화물 손상도 입력 Spike 완화 (도현 #2-15)

- **결정:** CargoBox.FixedUpdate dv 계산을 저역필터(smoothing 0.30) + 저속 lowSpeedRelief 적용.
- **이유:** 도현 "화물 손상도가 차량 속도 아니라 플레이어 입력에 따라 결정 → 초반 돈 없을 때 컨트롤로 극복 어려움". throttleLag 0.01s 즉각 반응 + 정지 출발 = 큰 dv → 큰 G → 데미지.
- **구현:**
  - `smoothedDv = Lerp(smoothedDv, dvRaw, 0.30)` — 5프레임 평균. 단발 spike 흡수.
  - `lowSpeedRelief = Lerp(0.3, 0, speedKmh / 30)` — 0~30km/h 구간 G 임계 +0.3g 헤드룸.
- **결과:** 일반 가속·제동·코너 데미지 거의 0, 진짜 급격한 입력만 손상. 초반 운전 학습 보호.

## D-063 — 3D 회전 포장 단계 미래 요구 (도현 #2-12)

- **결정:** Phase 1 PreRunSetupPanel은 단순 4 카드 모달 유지. Phase 2/3에 별도 PackingStage scene/panel 신설.
- **이유:** 도현 "물건을 선택하고 포장하는 단계가 따로 있어서 완충제를 어떻게 싸고 뭘 쓸지 물건을 3D로 돌려가면서 포장해야함".
- **설계 (Phase 2/3 진입 시):**
  - 화물 prefab 3D 표시 (RenderTexture isolated camera).
  - 마우스 드래그 회전, 줌.
  - 완충제 layer 토글/이동 (반투명 mesh overlay로 Bubble wrap 시각화).
  - 적용 후 BeginRun.
- **현재 구현:** PreRunSetupPanel docstring에 미래 요구 명시. ideas-parking 등록.

## D-064 — OrderBoard 카드 가독성 (도현 #2-16)

- **결정:** cardWidth 360→480, height 280→320, 배경 alpha 0.55→0.88, 글자색 강한 대비 + fontSize ↑.
- **이유:** "글자가 패널을 벗어나고 패널과 글자 색이 똑같아 잘 보이지 않음".
- **구현:**
  - statValueStyle 색 흰→노란 (1, 0.92, 0.40) 강조 — 보상/지점/시간 등 수치 즉시 인식.
  - tipStyle 별도 노란 + word wrap + 2줄 분리 (화물 가이드 / 포장 가이드).
  - 색상 띠 8px→12px + hover시 우측 6px 보조 띠.

---

**세션 16 후반 결정 클러스터 (D-059~D-064):** Plan B 4영역 일괄 구현 + 도현 다회 플레이 피드백 처치 + UGUI 인프라(EventSystem) 정착 + 화물 손상 모델 개선(저역필터+저속 보호) + 미래 3D 포장 단계 설계 보존.

## D-065 — Unity 검증 도구: Coplay MCP → unity-cli (2026-05-13)
Coplay MCP 반복 타임아웃으로 폐기. github.com/youngwoocho02/unity-cli 채택. Editor 켠 채로 HTTP 통신 → batch lock 회피, 실시간 검증 가능. `~/.claude.json` mcpServers={}, `.claude/settings.local.json` mcp 권한 제거. session.md 룰 갱신.

## D-066 — Obsidian Vault 분리 (2026-05-13)
repo 루트가 Obsidian vault 루트였던 구조 → `옵시디언/` 폴더로 vault 이동. `.obsidian/`, `GDD/`, `notes/`, `dev-plan/`, `code/`, `🗺 Maps/`, `_archive/` 모두 `옵시디언/` 아래로. repo 루트는 Unity 프로젝트(F1/) + Claude harness(.claude/) + scripts/ + 진입점 문서만. 경로 참조 전부 갱신.

## D-067 — `_Recovery/` 솎아내기 (2026-05-13)
108개 일회성 Editor 스크립트 → 15개 재사용 유지 + 93개 `_Recovery/_archive/`로 이동. 기준: [MenuItem] + 회귀 진단 가치 (AuditScene, Fix*Floating, DiagnoseCarFully, RestoreCarPhysicsDefault 등). 일회성 setup/diagnose/fix/measure는 archive.

## D-068 — 화물 손상 인과 변경 (2026-05-13)
이전: 차 G → 직접 CargoBox HP 깎임. 박스 시뮬은 시각 장식.
이후: 차 G → 박스 슬라이드 → trunk 벽 박음 → HP 깎임. CargoBox.FixedUpdate의 peakLong/peakLat 직결 데미지 제거. CargoShakeSimulator 벽 clamp 시 simVelLocal로 ApplyExternalDamage. `boxWallDamageMul`, `boxWallSpeedThreshold` 새 필드. 인과 정정으로 박스 시뮬과 손상이 일관됨.

## D-069 — Vertical Slice 데모 6주 (2026-05-13~2026-06-24)
**Steam 데모 출시 목표** (퍼블리셔 피칭 포맷). 6주 안에 풀게임의 모든 시스템이 mechanically 작동하는 30분 슬라이스. 콘텐츠는 1 도시 + 화물 5종 + F1 부품 2개 복원까지. 채널 2 (외형) 제외, 1·3·4·5·6·7·8·9 모두 작동. 메타 루프 닫힘 (정산→자금→부품 복원→다음 런 체감). 일정: W1 채널3 / W2 채널5+8 / W3 F1복원메타 / W4 시나리오 큐레이션 / W5 사운드+HUD / W6 QA+Steam페이지. 풀 spec: `[[2026-05-13-vertical-slice-design]]`.

D-069 의의: MVP 4주 룰의 자연스러운 후속. ExulMundi 회피 = "데모는 풀게임 슬라이스" 마음가짐 유지하되 새 시스템 추가는 ideas-parking 직행 룰 강제.

## D-070 (2026-05-13): 3D 포장 미니게임 도입

**결정:** PackingTier 4단계 SO 시스템을 폐기하고 3D 자유 랩핑 미니게임으로 갈아치움.

**Why:**
- 마이크로 루프 USP (한 잡의 깊이) 강화.
- 기존 4단계 카드 선택은 결정 비용 1초, 손맛 0.
- 3D 랩핑은 비용↔보호도↔보상 트레이드오프를 손맛으로 만든다.

**How:**
- WrapMaterialDef SO 3종 (부블랩/폼/하드케이스, 가격 ∝ 흡수율).
- WrapState 컴포넌트 + 256×256 RenderTexture, R/G/B 채널 = 재료별 커버리지.
- WrapStripBlit 셰이더 (UV 두 점 사이 띠 그리기). WrapOverlay 셰이더 (표면 시각).
- WrapPainter (raycast UV → strip Blit + cost 누적). EndStroke 콜백.
- PackingScreen (procedural UGUI + 다물건 1~3 탭). PackingScreenInput (New InputSystem 사용 — WASD/마우스/휠/1·2·3).
- Services.Packing 등록 (GameContext 12→13).
- AudioCueBus에 PackingMaterialSelect/StrokeStart/StrokeEnd/Confirm 4 cue 추가.
- 기존 PackingTier enum, PackingTierHelper, PackingModifier, PreRunSetupPanel, PackingCardUI 삭제.

**관련:**
- spec: [[2026-05-13-3d-packing-design]]
- plan: [[2026-05-13-3d-packing-plan]]

**Follow-up (Task 17 미완료):**
- PackingScreen이 wrap한 GameObject → cargo prefab 인계가 미구현. RunController.BeginRun(job, items)는 items 받지만 cargo prefab에 WrapState 전달 안 함. 결과: 실 런 중 QueryAbsorption은 항상 0 반환 → 포장 보호도가 손상 계산에 반영 안 됨.
- 다음 세션 첫 작업.

---

## D-071 — 하네스 운영: 규칙 즉시 발효 + B 트랙 평가 분리 (2026-05-14)

**결정:** AI 약점 패턴이 incidents 3건+로 검증되면 해당 draft-rule을 `project_rules/`·`CLAUDE.md`에 **즉시 발효** (이전 "형 검토 후 발효" 합의 뒤집음 — 옵션 가). 규칙의 적절성 평가·정리·형 검토는 **B 트랙(하네스, "다른 팀")이 사후로** 담당. A 트랙(게임 작업)은 평가를 기다리지 않음.

**Why:** 하네스는 멈춰 있는 게 아니라 게임 개발과 나란히 계속 굴러가야 함 (도현). "보류 = 정지" 프레이밍이 그 정신과 어긋났음. 빨리 "기록"에서 "예방"으로 넘어가려면 발효를 게이팅하면 안 됨. 잘못된 규칙 위험은 B 트랙 사후 평가가 거름.

**How:**
- `project_rules/session.md`에 "B 트랙 — 하네스" 섹션 추가 (게임 작업 중 트리거 / B 트랙 평가 분리).
- 세션 종료 절차에 하네스 패스(harness-usage-log 블록) 추가.

**관련:** `옵시디언/하네스/incidents.md`, `draft-rules.md`, `harness-usage-log.md`.

---

## D-073 ~ D-082 — 차고/F1 부품 복원 시스템 (2026-05-19, 세션 27)

W3 USP 핵심 작업. 6 부품 복원 = 매크로 진행 = 그랑프리 복귀 서사.

### D-073 — 트리 깊이 = **B. 6 부품 × 1 티어**
풀게임에서 티어 추가 가능. 데모는 깊이 X 폭 우선.

### D-075 — 차고 시각 = **A. 사이드뷰 분해도**
빠진 자리 회색 / 복원되면 색칠. 1주 안에 구현 가능.

### D-077 — 부품 효과 = **메커니즘 unlock형** (단순 stat boost X)
도현 요구: "유저가 다른 경험을 하게, 지루함 늦추기".
- 엔진 = stat boost (즉시 체감)
- 변속기 = 수동 변속 모드 unlock
- 서스 = 점프대/험지 unlock
- 에어로 = 부스트 게이지 unlock
- 타이어 = 종류 선택 메뉴 unlock
- 도색 = 그랑프리 모드 unlock (서사 클라이맥스)

### D-078 — 데모 슬라이스 한정 X = **풀게임 완성 목표, 6 부품 모두 구현**
W3 1주는 골조까지. 폴리시는 후속.

### D-079 — 부품 가격 = **별도 밸런싱 문서로 분리**
첫 부품 = 엔진, 가격 싸게. 나머지는 데이터로 별도 관리.
문서: `옵시디언/dev-plan/2026-05-19-restore-economy.md`

### D-080 — 가격 곡선 = **A. 지수형**
1k → 4k → 4k → 8k → 12k → 25k. Hades 패턴 (후반 클라이맥스).

### D-081 — 게이팅 = **A. 자유**
자금만 있으면 아무 부품. 트리 게이팅 X. 풀게임에서 추천 UI 추가 가능.

### D-082 — 첫 엔진 가격 = **1000₩**
한 잡(B 균형 잡 = 900~1100₩) 끝나면 도달. 메타 진행 즉시 도파민.

**관련:**
- 기획 캔버스: `옵시디언/dev-plan/2026-05-19-garage-realtime-planning.md`
- 밸런싱: `옵시디언/dev-plan/2026-05-19-restore-economy.md`
- 리서치: `옵시디언/research/garage-parts-reference.md`

### D-083 — 자금 carryover = **완전 누적**
런 사이 자금 유지, 재시작에도 유지. Hades 패턴, 메타 진행 필수.

### D-084 — 소비 페널티 = **정산액 비례 감소**
화물 깨진 비율만큼 정산 ↓. 자연 페이스 조절. 현재 ResultPanel 구현과 호환.

### D-085 — 정산→차고 흐름 = **Yes/No prompt**
ResultPanel 닫은 후 "차고 가시겠어요?" 선택. 자동 강제 X. 살 게 없을 땐 닫기.

### D-086 — 차고 화면 레이아웃 = **머신 가운데 + 카드 하단**
F1 머신 사이드뷰가 화면 중앙 ~60% 차지. 부품 카드 6장 화면 하단 가로 나열. Hades 거울 UI 패턴.

### D-087 — 잠긴 카드 표현 = **회색 + "X원 부족" 태그**
자물쇠 X. 희망 표현 (얼마 더 모으면 살 수 있음). 클릭은 가능 (디테일만 보임).

### D-088 — 복원 컷씬 = **2~3초, 짧은 활기**
회색→색칠 페이드 + flavorLine 한 줄 + 살짝 엔진음. 카메라 줌인/암전 X.

### D-089 — 부품별 수치/키바인딩 확정
- 엔진: 최고속 +20%, 가속 +30%
- 변속기: **K=다운 / L=업** (오른손, WASD와 분리)
- 에어로: 게이지 부스트 (Space, +50% 가속 2초, 재충전 5초)
- 타이어: 출발 전 4종 선택 (드라이/웨트/소프트/하드)
- 도색: 외형 + 그랑프리 모드 unlock
- 서스: TrackLayout slope tile 위치만 활성

### D-090 — 맵 디자인 학습 트랙 분리
별도 학습 캔버스 `옵시디언/dev-plan/2026-05-19-map-design-canvas.md`. R1 측정→R2 판단 흐름.

### D-091 — 타이어 4종 + 영구 마모 시스템 (2026-05-19)

타이어 unlock 후 메커니즘:

**4종 정적 효과** (마모 무관):
- 드라이: 디폴트 ±0%
- 웨트: 최고속 -5%, 그립 +25%, 화물 손상 ×0.8
- 소프트: 가속 +15%, 최고속 +10%, 그립 -10%, 화물 손상 ×1.5
- 하드: 가속 -10%, 그립 +5%, 화물 손상 ×0.5

**마모 = 영구 (런 사이 유지, 도현 안)**:
- 마모는 효과를 깎지 않음 — 최고속 미세 -3~5% 정도
- 핵심 = "한 런 더 뛸까, 지금 고칠까" 결정 압박
- 마모 30% 이하 = 펑크 위험 (매 초 5~15%)
- 마모 0% = 매 초 20% 펑크
- **펑크 = 즉시 잡 실패** (정산 0₩, Result 직행)

**수리 비용** = 잔여 마모% 비례 (드라이/웨트 500₩, 소프트 800₩, 하드 600₩ 풀수리).
"엔진 1000₩ 살래, 소프트 800₩ 수리할래" 정산금 트레이드오프 자연 발생.

**플레이 페르소나**:
- 초보 = 드라이만, 자주 풀수리
- 중수 = 잡마다 맞추기
- 도전 = 닳을 때까지 굴리는 도박러

### D-092 — 그랑프리 모드 = **후속 (데모 X)**, 도색 효과 외형으로 한정
도현: "최종 엔딩이니까 지금은 생각만. 유저별 부품 업그레이드 커스텀 → 그랑프리 랩타임 대결 메타게임으로 구상."
데모 도색 = 머신 색칠 + "그랑프리 복귀 준비 완료" 서사 한 줄. 실제 트랙 모드 X.
파킹: `옵시디언/notes/ideas-parking.md`

### D-093 — 서스 점프대 배치 = **맵 디자인 트랙과 같이**
도현: "맵 기획하고 같이". heightMask slope tile (D-050) 활용. R1 측정 후 도로 동선 보고 박을 위치 결정.
파킹: `옵시디언/dev-plan/2026-05-19-map-design-canvas.md` §4.5

### D-094 — 아트 스타일 = **A+C** (Kenney 플랫 로우폴리 바탕 + 미니어처 디오라마 한 겹) (2026-10-12)
도현: "출시하려면 아트 스타일 통일이 제일 중요. 백지에서 예시 이미지로 정하자." 같은 장면 6결 보드(A 플랫/B 통통카툰/C 디오라마/D PS1/E 셀코믹/F 페이퍼) 중 **A+C**.
Unity 실측: 후처리+낮 무드(v2)만으론 목표의 3할, **구역별 팔레트(v3)** 가 진짜 레버. 산출물 `art/concept/style-board-2026-10-12/`, 메뉴 `PitCrew/Art/*`.
도현 추가 진단: "렌더링보다 모델링이 큼지막하고 디테일이 없다" → D-095.

### D-095 — 건물은 **Blender 생성기로 자체 제작** (Kenney-only 규칙 해제), 북극성 = **Alba 비례·디테일** (2026-10-13)
도현: "같은 결의 실제 게임 레퍼런스를 보고 Blender 로 건물·게임 느낌을 잡아 계속 만들어 가자." "Art of Rally 퀄리티면 괜찮지만 그건 시점이 높고 멀어서 디테일을 안 봐도 되는 것. **Alba 정도의 디테일**이면 좋겠다."
- `project_rules/asset.md` 의 "Kenney 라인업 안에서만, 커스텀 제작 금지"는 **해제**. 이유가 제작 시간이었는데 AI+Blender 로 한 채 몇 분이 됐다. Kenney 는 도로·차량·소품에 계속 쓴다.
- 규칙: 건물 2~3층 · 1층 문/창/차양은 사람 크기 · 집마다 하나씩 다르게(색·간판·발코니·덧창) · 플랫 색 · 텍스처 없음 · 구역 팔레트(D-094).
- 레퍼런스: Alba(지중해 항구, 비례·디테일) · Lake(배달 소도시, 운전 시점) · Townscaper(모듈 실루엣) · Art of Rally(원거리 품질 기준).
- 구현: `tools/asset-gen/proc_building.py`(파라미터 → FBX) → 항구 블록 4채 파일럿 → 추적캠 비교 → 통과 시 BuildingPlacer 연동.

### D-096 — 아트 스타일 = **Codex 줄기 "따뜻한 로파이 해안"** 으로 확정, D-094 의 "텍스처 없음" 폐기 (2026-10-13)
도현: "Codex 하고 만든 아트 스타일로 가는 게 맞을 듯." 같은 날 두 AI 가 두 줄기(플랫 Alba / 텍스처 로파이)를 만들었고 **로파이 쪽이 실제 씬에 설치돼 있어** 그걸 채택.
- 규칙(Codex `art/source/lofi-pilot/README.md` 기준): 저폴리 실루엣 + **512 공용 아틀라스(64px 칸, Point 샘플링)** 로 진열·간판·얼룩 표현, 벽·길 UV 약 2m 단위, 베이지 벽·청록 간판·따뜻한 창색, 안개 + 약한 방향광 그림자. 참고 Easy Delivery Co.(원본 재현 아님).
- **유지**: D-095 의 비례·밀도 규칙(2~3층 · 사람 크기 문/창/차양 · 건물당 소품 5개 · 집마다 하나씩 다르게)과 Blender 생성기 경로. 생성기는 이제 `lofi_pilot.py` 의 아틀라스 규칙을 따른다. 세계 척도 B(차 기준 0.35)도 유지.
- **폐기/보류**: D-094 의 "단색·텍스처 없음". 디오라마 후처리 한 겹(그레이딩·원경 흐림·비네트)은 보류(로파이 안개·방향광과 겹침, 필요하면 재검토). `ApplyZonePalette`/`proc_building.py` 플랫 출력/`race_v2` 는 **접음**(파일은 남김).
- 팀 첫 판정(2026-10-13): verifier PASS(컴파일 0·회귀 12/1·스모크 8/8) / graphic-gate **FAIL 4/10**(소품 없는 큰 벽·도로 빈약·톤 불일치) → 다음 손 = 건물당 소품·비례·톤 통일.

### D-097 — 건물 세트 = **Codex 4차 판**(벽돌 기단·쿼인·네 면 창·지붕 3색·변형 7종) 채택, 변형은 "간판 교체" 금지 (2026-10-13 밤)
같은 브리프를 Codex·Claude 가 따로 만들어 비교(도현 "나누지 말고 둘 다"). graphic-gate: Codex **7/10 PASS**(첫 통과) / Claude 5/10 FAIL. 도현이 Codex 쪽 변형 모음(`codex4_08_building_variants.png`)을 보고 "이게 더 좋다, 이런 느낌으로 다양하게".
- **기준(도현)**: "간판·색만 바꾼 복붙은 임시로만. 결국 실제 건물처럼." + "TEA/MART 는 책방 건물에 간판만 — 가게는 가게 종류가 읽혀야." → 변형은 **실루엣(폭·층수·지붕 형태)** 과 **정면 기능(진열·가구·창)** 이 달라야 한다.
- 비용: 건물당 삼각형 600~900 → 3,000~4,100 (예산 4,500). FPS 미측정.
- 보관: Claude 판 코드 `art/_compare/2026-10-13-building-faces/claude/`, Codex 판 `codex/`. 씬엔 Codex 판 설치. 다음 브리프 `art/_briefs/2026-10-13-building-variety.md`(집 3종 추가·가게 3종 정면 차별·정비소 지붕색).
- 역할: 아트 손 = Codex(키트 주인), 통합·게이트·치수 동기화(`CoastalTownDensity.Body()`) = Claude. 확인 시점엔 도현에게 알림.

### D-098 — 맵 구조 = **마을 여러 개 + 굽이치는 해안도로**, 고도(언덕·내리막) 허용 — D-050 의 "도로 셀 높이 0" 해제 (2026-10-13 밤, 기획 대화)
도현: 소렌토(절벽 위 마을 + SS145 해안도로) 사진으로 "마을 속에서 바쁘게 움직이다가 멀리 이동도 할 수 있게". 고도 금지는 당시 AI 의 기술 한계 때문이었고 "이제는 상관없을 것 같아 지워도 됨, 언덕·내리막 넣어도 됨".
- 구조(확정): 마을 = 지금 6m 타일 격자(구역별 블록 크기 차등, 연결 검사기 유지) / 마을 사이 = 해안도로(타일 아님, 스플라인 메시 — RoadArchitect MIT 또는 자체, 곡선·고도 자유). 한 런 = 마을 골목(1~2분) + 해안도로(2~3분) + 다음 마을 → 5~8분, "한 판 5~15분" 갭을 구조로 메움.
- 고도: 해안도로에 오르내림. 마을↔도로 이음새 높이 정렬이 새 과제. 채널 5(트렁크 물리)가 내리막·코너에서 비로소 작동.
- 아직 미정: 마을 개수·해안도로 길이·마을 간 거리(다음 대화 주제).
- `project_rules/asset.md` 의 heightMask 항목(D-050)은 "마을 격자 안에서만" 으로 범위 축소.

### D-099 — 마을은 대체로 평지 + 일부 경사, **경사 = 보이지 않는 난이도·경제 장치** (2026-10-13 밤, 도현 아이디어)
도현: "마을은 어느 정도 평지로. 마을에도 높이차 있는 곳과 평평한 곳이 있으니, 초반에 뭣도 모르고 경사진 곳에 가면 물건이 많이 상하고, 나중에 돈 벌어 미끄럼 방지 패드 같은 업그레이드를 하면 손실 없이 벌 수 있다. 보이진 않지만 의도적인 레벨디자인으로 스토리 전개도."
- 마을 2개(D-098): 길 찾기는 6m 칸, 모양은 스플라인 두 겹(논문 레시피 `옵시디언/research/urban-morphology-2026-10-13.md`). 마을 안은 대체로 평지, 경사 구간 일부.
- 경사 → 채널 5(트렁크 물리)·채널 8(경로 계획: 평평한 먼 길 vs 가파른 짧은 길)이 실제 선택이 됨. **미끄럼 방지 패드** 는 차고 부품 체계의 새 항목 — 백로그(`ideas-parking.md`), 지금 구현 아님.
- 조건(Claude 제안, 도현 확인 전): 첫 손실은 보여야 함(트렁크 캠 미끄러짐 + 정산 한 줄 "내리막에서 미끄러짐").
- 미정: 높이를 진행 단계(항구 평지→언덕 마을→해안도로 너머)와 묶을지.

### D-100 — 높이 = 진행 단계(항구 평지 → 언덕 마을 → 해안도로 너머), 맵 생성 규칙 셋 확정 (2026-10-13 밤)
도현 "ㅇㅇ 괜찮아 보인다" (소프트 게이팅 조사 `옵시디언/research/soft-gating-level-design-2026-10-13.md` 뒤).
- 생성기에 구조로 박는 규칙: ① **언덕은 절대 막지 않는다**(대가는 화물 파손뿐) ② **언덕마다 평지 우회로가 하나는 있다** ③ **언덕 꼭대기엔 랜드마크**(등대·카페 등).
- 기획·수치 쪽(생성기 밖, 후속): 첫 언덕은 짧고 싼 화물(계란)로 · 미끄러짐은 트렁크캠 + 정산 문구로 원인 표시 · 패드 업그레이드는 "비쌈→지름길" · 언덕 벌금 < 지름길 이득(측정) · 해금은 벽이 아니라 거리·보수.
- 근거 사례: SnowRunner(닮은 성공) / Death Stranding(닮은 실패 위험) / BotW(벽 제거·삼각형) / USask 손실 회피.

### D-101 — 색감 기준 = **아말피 한낮**(흰 빛·짧고 진한 그림자·채도 높은 바다), 시간대 변화는 후속 (2026-10-13 밤)
도현: "한낮 느낌으로. 어차피 시간은 계속 변해야 하니 시간대마다 다르게 나와야 함. 지금은 너무 어둡고 음산해서 별로."
- 진단: 해 세기 1.05·회청색 주변광(.68,.72,.73)·회청색 안개·베이지 벽(#b8ac91) → 흐린 오후로 읽힘. 손잡이 넷: 해(세기·색·높이) / 주변광 → 하늘빛 흰색 / 안개 → 멀리·밝은 하늘색 / 벽 칸 → 크림, 바다 채도 ↑. 그림자 세기를 같이 올려 명암 유지.
- 시간대: 세션 28 의 MoodProfile SO 구조(복제로 황혼·야간)를 쓰되 지금은 한낮 프로필 하나. 하루 주기는 백로그.

### D-102 — 추적 카메라 = 뒤 5m·높이 2.6m·시선 1.6m (2026-10-13 밤)
도현: "카메라 시점이 너무 아래로 향해 배경이 잘 안 보이고 차가 너무 중앙에 있는 느낌." 이전 (0,2,-3.5)/시선 0.45 = 아래로 약 24° → (0,2.6,-5)/1.6 = 약 11°. 지평선·먼 건물·바다가 들어오고 차는 아래 1/3. 값은 FollowCamera(씬) + TownLayout.asset(cameraOffset/cameraLookHeight, 설치기가 복원). 도현 최종 판정 대기.

### D-103(대기) — 전체 축척: 건물 0.64배 vs 1.0배(실제 치수) (2026-10-13 밤)
경위: 도현이 별도 Codex 세션에서 거리 소품(승인 원화 → 벤치 1.8m·쓰레기통 .95m, 참고 Furnitubes/OMOS)·골재 도로·차 3.785m(이전 1.76m) 를 메인 씬에 적용(23:26, `art/source/street-furniture/STATUS.md`). 이어 "건물과 비율이 안 맞는다"로 실제 치수 안(`REAL_SCALE.md`: 건물 1.0 = 층고 2.8m·문 2.15m, 차 4.885m, 차도 6m) 을 검토 씬에만 둠. 도현 "다른 세션 멈추면 다 옮겨봐" → Claude 가 설치기에 승인 자산 연결 확인(다른 세션이 이미 연결), 건물 1.0 을 실제 맵에 설치해 운전 시점 캡처(`scale_064_vs_100.png`). 카메라 기본값 (0,3.2,-7)/1.15 로 변경(CoastalTownLayout.cs).
- 상태: 씬에 **건물 1.0 실험값**(`CoastalTownInstaller.BuildingScale=1f`, 0 이면 옛 .62~.64). 도현 판정 대기. 차 4.885m·차도 6m 는 미적용(셀 6m 구조 변경 필요 — 새 마을 생성기(D-098) 에서 셀 크기와 함께 정할 것).

### D-104 — 첫 목표 = 마을 생성기(P2), "실제 도시처럼" (2026-10-19)
도현 제안. 기획팀 추천 순서(런 길이 측정 → 해안도로 프로토 P1 → 생성기 P2)를 바꿔 **생성기 P2를 먼저** 한다. 런 길이 재측정은 QA 재기동 점검에 그대로 남는다(기준값).
- D-103 축척은 도현이 **비교 그림을 보고** 정한다(에셋 전체 크기와 두 안의 장단점). 아트팀이 비교 시트를 만든다. 생성기의 셀 크기가 이 결정에 묶여 있다.

### D-105 — 도시 배치 알고리즘 = 기존 공개 구현을 최대한 활용 (2026-10-19)
도현 제안. 조사 노트의 1순위였던 "직접 구현(약 170줄)" 대신 GitHub 등 이미 있는 생성기를 찾아 쓴다. 기획팀이 후보를 조사(라이선스·Unity 적용성·격자/스플라인 적합·연결성 검증 연동)하고, 도현이 고른다.

### D-106 — 아트 제작 방식 = Codex 원화 → Blender 등 실제 도구로 모델링 (2026-10-19)
도현 제안. PIPELINE §5 우선순위 1(코드 생성 로우폴리)을 "원화 먼저, 그다음 Blender(bpy 스크립트 포함) 등 실제 도구로 제작"으로 구체화. asset.md 승인 순서(원화 → 승인 → 제작 → 미리보기 → 승인)는 그대로.

### D-107 — OSM 라이선스 수용 + 마을은 실제 마을을 게임에 맞게 섞는다 (2026-10-19)
도현 답(기획팀 조사 `옵시디언/research/city-generator-oss-2026-10-19.md` 뒤).
- OSM 을 쓰면: 크레딧 출처 표기 + 가공한 도로 데이터(roadMask) ODbL 공개 — 수용("ㅇㅇ").
- 어느 마을: 아말피·포지타노 하나를 그대로 쓰지 않고 "적당히 섞어 우리 게임에 맞게".
- 미정: OSM 과 roadgen 을 둘 다 실험할지 — 도현이 생각을 물음, 착수 전.

### D-103(확정) — 축척 = 실제 치수. F1 차와 건물 모두 실물 크기에 맞춘다 (2026-10-19)
도현(비교 시트 `art/_compare/2026-10-19-scale/index.html` 뒤): "실제 F1 차와 건물의 크기와 비슷하게." → 건물 1.0(층고 2.8m·문 2.15m). 차는 지금 3.785m 가 아니라 실제 F1 치수에 가깝게(정확한 목표치는 아트·기획 스펙에서). 셀 크기·도로 폭은 마을 생성기(P2) 스펙에서 이 차 크기 기준으로 정한다.

### D-108 — 생성기는 실제 지도(OSM)와 roadgen 둘 다 실험하고, 결과를 보고 정한다 (2026-10-19)
도현: "어떻게 되는지 보고 결정해야 할 듯." 한쪽만 고를지, 마을마다 다른 방식을 쓸지도 결과를 본 뒤. 실험 조건(같은 땅 넓이·칸·승격 규칙·카메라·연결성 검사)은 기획 스펙이 고정한다.

### D-109 — 생성기 실험의 첫 판정 = 씬 뷰에서 "실제 마을처럼 느껴지나" (2026-10-20)
도현: "굳이 플레이되는 것보다 돌아다닐 수 있고, 내가 씬 뷰에서 마을을 보며 실제 마을처럼 느껴지는지 판단해 보는 게 먼저."
- 실험(D-108)의 1차 산출물 = 도현이 씬 뷰로 둘러볼 수 있는 마을 두 판(OSM / roadgen). 지형·도로·건물이 실제 마을처럼 읽히는지가 판정 기준.
- 플레이 측정(한 판 시간·끼임·배달지 거리)은 그 판정 뒤로 미룬다.
- 시작 지점: "어디든 상관없다. 지형을 실제처럼 구성하는 게 맞다." → 정산 뒤 "다시" 때 차 위치 유지는 지금 버그로 다루지 않는다. 가게 복귀로 런 끝내기도 보류.
- 땅 크기: 480×400m 로 작게 먼저("일단 작게 이렇게 만들어 봐").
- 구역별 주문 재분배: 나중에.

### D-110 — 생성기 실험 1차 판정: roadgen(높이 0.3배)이 가장 좋음, OSM 판을 조금 섞는다 (2026-10-20)
도현(씬 뷰 4판 열람 뒤): "TownGen_RoadGen_h03 과 TownGen_OSM 둘 약간 합치면 괜찮을 듯. RoadGen_h03 이 가장 느낌이 좋긴 한데, 바다에 길이 있는 것과 차도가 애매한 것 — 실제 차가 주행하기 애매한 시골 마을 느낌 — 빼고는 괜찮다."
- 기준판 = roadgen + 고도 배율 0.3.
- 고칠 것: ① 바다 위 도로(버그) ② 차도가 차도로 안 읽힘 — 큰길과 골목의 차이가 없어 시골길 느낌.
- 섞을 것: OSM 판에서 가져올 요소(기획팀 제안 → 도현 확인 대기).

### D-111 — 마을 큰길 = OSM 실제 차도를 자동으로 골라 곡선(스플라인)으로 깐다, 칸 스냅은 큰길에서 뺀다 (2026-10-20)
도현: "실제 이탈리아나 내가 말한 지역의 지형·분위기에서 우리 게임에 알맞게 도로와 지형을 가져오고 싶다. Easy Delivery Co 느낌이 조금." → PD 제안(실제 지형 + 실제 길을 본뜬 곡선 큰길 + 길 따라 땅 깎기/메우기 + 집은 길 따라 줄지어) 뒤 큰길 고르기 = "OSM에서"(자동).
- 지형: 실제 DEM, 높이 배율 0.3~0.5(튜닝).
- 큰길: OSM 차도(primary·secondary·tertiary·residential 등)를 골라 칸이 아닌 곡선으로. 길 밑 지형은 길 높이에 맞춰 깎고 메운다.
- 집: 길을 따라 줄지어, 비탈은 계단식. 골목은 짧게.
- D-110 의 칸 방식 섞기 규칙(스펙 §14)은 이 방향으로 대체. 바다 칸 규칙은 유지.
- (D-111 보충, 2026-10-20) 도현 "ㅇㅇ 좋고": Easy Delivery Co 에서 가져올 것 = 굽은 산길 + 화물 흔들림. 색감·계절 = 아말피 한낮(D-101) 유지. 확인은 집·골목까지 "다 만들어서" 한 번에(그 전엔 부르지 않음).

### D-112 — 곡선 마을 1차 판정: 느낌은 맞다, 가까이서 이질감 — 받침대·구심점·건물 반복 (2026-10-20)
도현(TownGen_Spline 씬 뷰): ① 굽은 산길·집·바다 "어느 정도 느낌은 맞다" ② "계단 구조물만 너무 크고 많다" ③ 석축·깎인 비탈 "나쁘지 않다".
"전체 느낌은 비슷한데 가까이서 보면 이질감·이상한 것이 많다": 집이 무조건 받침대 위 / 마을 구심점 없음 / 모든 집을 특정 에셋으로만 돌려 씀.
→ 방향(D-111) 유지. 다음 손 = 집 앉히기·계단 축소 / 마을 중심(광장·랜드마크) / 건물 다양화(새 에셋은 asset.md 승인 순서: 원화 → 승인 → 제작).

### D-113 — 마을 구조 = 밀도 지도: 복잡한 중심 → 가장자리 → 풍경 산길(외딴집) → 비탈 위 작은 마을, 중심↔작은 마을 3~5분 (2026-10-20)
도현: "마을의 복잡함 사이를 달리다가 조금 떨어진 곳을 가야 할 때 비탈도 있고 풍경도 볼 수 있고 비탈에 조그만 마을도 있고 중간중간 동떨어진 집도 있는 식. 지금은 도로 옆에 건물 붙여 넣은 것밖에 안 된다." 거리 = "3, 5분 정도".
- 집 배치를 "큰길 양옆 채우기"에서 밀도 지도(중심 거리·경사·바다 조망)로. 낮은 밀도 = 외딴집, 작은 마을 자리 = 모아 놓기.
- 3~5분 ≈ 굽은 길 2.3~3.9km(평균 13m/s 가설) → 땅 약 1.5~2.5km. 실제 SS163 구간 후보. D-098(두 마을 + 해안도로)과 같은 그림.
- 건물 아트 A/B 는 "느낌 차이 크지 않음" → 배치 구조가 정해진 뒤 필요한 건물 종류로 다시.


## D-114 (2026-10-20) 집 배치 = OSM 실제 건물 윤곽
- 도현 판정: 콘카 v9 "길은 나쁘지 않은데 마을 생김새·도로 순환·건물 배치가 현실 같지 않음".
- PD 제안(도현 승인 "이대로 해보고 이상하면 또 수정"): 지금까지 OSM 에서 길만 받고 건물은 규칙으로 찍었음 → OSM building 윤곽을 받아 그대로 앉히고, 빈 구역만 규칙 집으로 채운다.

## D-115 (2026-10-20) 마을 길 = 웬만하면 차로, 광장은 입구까지
- 도현: 웬만하면 차로 다닐 수 있게. 광장은 입구까지만 차, 광장 안은 보행. 배달은 내려서 전해 주는 컨셉.
- 적용: living_street·service 는 좁은 차로(차 통과). 광장 6곳은 둘레 입구에 차 멈춤 자리, 안쪽 보행 전용. 계단·footway 는 보행.
- 걸어서 가는 배달 퀘스트 = 새 시스템 → ideas-parking (scope 규칙 1).

## D-116 (2026-10-20) 상자 A 배달 맵: 윗길 고리 + 메인 씬 교체
- 도현: 갈림1 = B(윗길 Via Pimenio→Via Primo Maggio↔콘카 Via Miramare 경계 22m 이어 고리, 해안 4,524m vs 윗길 5,318m). 갈림2 = 메인 씬 교체.
- 적용: ComplexTrack.unity 는 지우지 않고 보관(되돌림용), 빌드 첫 씬을 아말피 맵으로. 회귀 CheckCoreServicesPresent 등 기준선은 새 씬 기준으로 다시 잡는다. 스펙 docs/superpowers/specs/2026-10-20-amalfi-map-game-design.md.

## D-117 (2026-10-22) 프로토타입 2주(11/5) — 핵심 재미 먼저, 맵은 달릴 수 있는 최소만
- 도현: "프로토타입이면 핵심 재미가 먼저", "운전할 수 있는 최소한의 맵만 구성하고 핵심 메커니즘을 정하자".
- 핵심 재미 = 시간 안에 깨지기 쉬운 화물을 깨뜨리지 않고 배달(빠르게 vs 조심히). 포장 = 피트스탑. 풍경 = 무대·보상.
- 맵 = 한 줄 코스(배달지 3~5), 실제 지도 일치도 측정·생성기 개선 중지. D-116 의 조각 15b~20(배달지 30·메인 씬 교체)은 프로토타입 뒤로 보류.
- 프로토타입에서 끔: 연료·타이어·경로 계획·구역 해금·부스트·교통신호.
- 스펙 docs/superpowers/specs/2026-10-22-prototype-core.md.
- (D-117 보충) 도현: 메커니즘 시험 = ComplexTrack 먼저, 맵은 10/25부터. 수리 = 서스펜션 vs 엔진 플레이어 선택.

## D-118 (2026-10-22) 긴급 전화로 시작 — 시계는 전화 수락부터, 포장 단계부터 급박함
- 도현(ComplexTrack 첫 플레이 뒤): 처음에 긴급 전화로 "어디서 무엇이 필요한지" 상황을 보여 주고 제한 시간 안에 배달하게 하면 포장부터 급박해서 더 재밌을 것. 맵·조작감·아트는 차차 개선.
- 적용(프로토타입 모드): 주문 카드 고르기 대신 무작위 주문 1통을 긴급 전화로(발신자·상황·배달지·화물·보상·제한 시간). "바로 갈게요!" = 시계 시작 → 포장 → 주행. 목표 시간 = 포장 여유 40s + 주행 목표(거리 기반). 포장 중에도 카운트다운 표시.
- 가설: 포장 여유 40s. 카드로 화물 고르기(취약 vs 튼튼)는 빠짐 — 필요하면 "두 통 중 하나 받기"로 되살림.
- 미뤄 둠: 맵·조작감·아트 개선(10/25 최소 맵 이후 차차).

## D-119 (2026-10-22) 제한 시간 = 주문판부터, 긴급 전화 = 이벤트 (D-118 수정)
- 도현: 원래는 제한 시간 안에 여러 목적지 중 원하는 걸 골라 배달하는 것. 긴급 전화는 이벤트로 넣고, 제한 시간은 물건 고를 때부터.
- 적용(프로토타입): 주문판이 열리는 순간 판 제한 시간 시작(가설 90s, ComplexTrack 기준) — 고르기·포장·주행 모두 한 시계. 넘으면 런 종료(완료 비율 절반 보상, 기존 규칙). 주문판·포장에서 0 이 되면 출발 즉시 종료.
- 긴급 전화 = 주문판 위 이벤트(확률 40%). "받을게요" → 그 주문이 선택되고 카드에 "긴급", 배달 성공 시 기본 보상 +50%. "못 가요" → 그냥 주문판.
- D-118 의 "전화 수락부터 시계"·포장 여유 40s 는 폐기.

## D-120 (2026-10-22) 핵심 재미 판정: 있다 → 최소 맵으로
- 도현(ComplexTrack 직접 플레이, D-119 빌드): "핵심 재미는 있는 것 같다."
- 확인된 루프: 주문판부터 도는 제한 시간 + 긴급 전화 이벤트 + 포장 + 화물 흔들림.
- 다음: 최소 맵(한 줄 코스, 배달지 3~5)에서 같은 루프. 제한 시간(90s)·긴급 확률(40%)은 새 맵에서 다시 잡는다. 맵·조작감·아트 개선은 그 뒤 차차.

## D-121 (2026-10-22) 출시 퀄리티 방향: 종이 전표 UI · 둥근 제목체+Pretendard · 장난감 실루엣 화물
- 도현(계획서 docs/superpowers/specs/2026-10-22-release-quality-art-ui-plan.md §5 뒤): UI = 종이 전표·도장·영수증, 한글 = 둥근 제목체(Jua/Do Hyeon) + 본문 Pretendard, 화물 = 장난감처럼 과장된 실루엣.
- 맑은 고딕(KoreanFallback SDF 원본)은 배포 불가 → OFL 폰트로 교체(라이선스 파일 확인 후 채택).
- 순서: 폰트 교체 + UITheme 디자인 토큰(하드코딩 색 51곳) → 화물 5종 Blender 코드 재제작(≤2,000 tris, BUG-001 닫기, 원화 승인 먼저) → 화면별 전표 디자인.
- 함께: 포장 "보이는 보호" 묶음(docs/superpowers/specs/2026-10-22-packing-polish-design.md), 조작감 1차(902cb6f7) 도현 판정 대기.

## D-123 (2026-10-23) 아트 방향 v2 = "옛 기억 속 해안 동네" — D-121 의 장난감·둥근 톤 철회
- 도현(D-121 시안 본 뒤): "초반 방향보다 아기자기하다" → "Easy Delivery Co 같은 느낌, 풍경을 보며 예전을 생각할 수 있게."
- 기준 원화 = D-096 `art/concept/lofi-styles-2026-10-13/02_warm_street.png`. 실제 비례 + 64px 픽셀 질감 + 바랜 한낮 색 + 풍경 중심.
- 화물: 실물 비례·픽셀 질감, 단 포장 화면에서 면이 읽히게 "적당히"(6면 다른 UV·라벨). UI: 주행 중 거의 없는 작은 픽셀 HUD + 멈춘 화면은 스캔한 듯한 진짜 종이 전표(A+B).
- 상세: `art/_briefs/2026-10-23-art-direction-v2.md`. D-121 의 Jua·장난감 실루엣 폐기, 슬롯 구조·OFL 원칙 유지.

## D-124 (2026-10-23) 화물만 텍셀 64px/m 예외 · 화면 렌더 960×540 Point 유지
- 도현: 화물 64px/m "ㅇㅇ", 해상도 "960". 근거: 상자 v03 게이트 32px/m 7점 vs 64px/m 8점(32 에선 A6 송장이 3×5 텍셀로 뭉개짐), 저해상도 시험(docs/qa/evidence/lowres-compare.png)에서 성능 이득은 원→960 구간에서 다 나옴(7.1→3.0ms).
- 세계(건물·도로)는 32px/m, 화물만 64px/m. 렌더는 현 PC_RPAsset(renderScale 0.5 + Point) 그대로 — 변경 없음. 팔레트/디더 화면 후처리는 보류.

## D-125 (2026-10-23) 경험 문장 = "급하게 달리다 바다를 보고, 끝나면 천천히 돌아온다"
- 도현: 후보 중 1("바쁜 여름 아르바이트 — 급하게 달리다가도 코너를 돌면 바다가 보인다")과 3("배달 끝나고 돌아오는 길이 진짜 게임")이 좋다.
- 합친 문장: **"바쁜 여름 배달 아르바이트. 갈 때는 급하게, 코너마다 바다가 스치고 — 끝나면 천천히 돌아오는 길이 보상이다."**
- 게임 쪽 함의: 갈 때(제한 시간·긴장, D-120 핵심 재미) / 돌아올 때(시간 제한 없음·풍경·음악, 여유) 두 박자. 아트는 둘 다 같은 세계, 차이는 빛·음악·카메라·HUD 로.
- 형용사 3·금지어 3은 PD 초안(가설, 도현 수정 가능): 따뜻한·바쁜·바랜 / 귀여운·깨끗한·웅장한.

## D-126 (2026-10-23) 레퍼런스 역할 분담: 속도감 = F1 더 무비(카메라·소리), 규모감 = Easy Delivery Co(세계·안개)
- 도현: "이지딜리버리의 속도감과 어디까지인지 모를 규모, F1 더 무비의 속도감 — 섞는 게 어려워 보인다."
- PD 정리: 섞지 않고 층을 나눈다. F1 더 무비 → 차 주변(낮은 카메라·FOV·흔들림·가까이 스치는 물체·엔진/바람 소리) = '갈 때'. EDC → 세계(안개로 끝을 숨김·멀리 이어지는 길·먼 실루엣·낮은 해상도) = 항상, 특히 '돌아올 때'.
- 충돌 주의: 지금 맵 끝 옹벽·노란 가드레일(12d3342c)은 '끝이 보이는' 장치라 규모감과 반대 → 후속으로 안개·지형 감쇠·시야 차단(언덕·건물)으로 대체 검토. 모션블러는 저해상도 Point 와 충돌 → 쓰지 않는다(가설).

## D-127 (2026-10-23) 목표 기억 한 장면 = "시원하고 아늑한 가을날, 탁 트인 시야" + 사람 기척 소리 · 차/화물은 장난스러워도 됨
- 도현(분위기 대화, 근거 docs/research/2026-10-23-atmosphere-reading.md): EDC 의 "겨울 해질녘 혼자" 에 해당하는 우리 장면 = **기분 좋은 시원함과 아늑함이 느껴지는 가을날, 탁 트인 시야**.
- 소리 = 오토바이, 사람들이 적당히 떠드는 소리, 어릴 때 친구들과 떠들던 소리 → EDC(고립)와 반대로 **사람 기척이 있는 동네**.
- "귀여운" 금지 해제: EDC 처럼 조작 대상(F1 차·화물)은 조금 장난스럽게, 동네는 사실적으로 — 대비로 따뜻함.
- 바뀌는 것: D-125 "여름 배달 아르바이트" → **가을**. D-101 아말피 한낮(강한 여름빛) → 맑고 서늘한 가을 빛(높은 하늘·선명한 공기·낮게 기우는 해)으로 재해석. 금지어는 깨끗한·웅장한 둘 남김.

## D-128 (2026-10-23) 웅장함은 풍경 스케일로만 허용 · 동네 표면 = "세월의 흔적"
- 도현: 금지어 분리 "ㅇㅇ" — 과장된 웅장함(음악·연출)은 금지, 풍경 스케일의 웅장함은 허용.
- 도현: 교토에서 남은 건 "옛날의 흔적이 보이는 보도와 건물"(구조는 기억 안 남) → 동네는 일본풍 형태가 아니라 **오래 쓰인 표면**(닳은 보도·손때·바랜 덧칠·간판 위 간판·수리 자국)으로 그리움을 만든다.
- 음악: J-pop·발라드·록 두루 → 기준 곡은 도현이 고른다(대기).

## D-129 (2026-10-23) 음악 두 박자: 갈 때 = 록/J-pop, 속도 기준 = 탑건·F1 더 무비 / 올 때 = 잔잔한 J-pop·팝·여운 긴 발라드
- 도현: 갈 때는 록이나 J-pop, 속도감 음악은 탑건·F1 더 무비 영화 생각하면 됨. 올 때는 잔잔한 J-pop·팝, 또는 여운이 긴 발라드. 구체 곡은 들어보고 정함(대기).
- 손잡이(리서치 two-beat): 갈 때 리듬·드럼 층 + 엔진/바람, 올 때 층을 빼고 어쿠스틱·보컬 여운. 같은 곡의 두 편곡(풀 밴드 ↔ 어쿠스틱)으로 이어 붙이는 방식이 가장 매끄러움(가설).
- 라이선스: 실제 곡 사용 불가 — 레퍼런스로만, 게임용은 직접 제작/라이선스 음원.

## D-130 (2026-10-23) 원화 재현 슬라이스 비주얼 합격 → 프로토 코스의 기준 장면
- 도현(텔레그램, 캡처 docs/qa/evidence/slice-vs-concept.png): "괜찮네… 비주얼적으론 합격." 직접 운전 판정은 귀가 후.
- 분위기 원인은 조명이 아니라 구도·지형·밀도였다는 진단 확인. Slice_PostcardStreet 를 프로토 코스(약 1km, 배달지 4곳)로 확장 중. ComplexTrack 은 시스템 시험장으로만.

## D-131 (2026-10-25) "함께" = 받고 건네는 순간의 NPC 상호작용 · 반복으로 친숙해지는 동네 사람
- 근거: TASTE.md 해체 2(데스티니 레이드 입장 — 압축 뒤 개방 + 거대한 것의 소리 + 옆에 우리). 1인 게임의 '우리' 빈칸을 도현이 설계.
- 도현: 상호작용은 배달 중이 아니라 **화물을 받을 때·건넬 때(배달 완료)**. 세계의 NPC 역할에 플레이어가 친숙해지게.
- PD 정리(가설): 같은 동네 사람이 같은 역할로 반복 등장(보내는 사람·받는 사람·단골) → 얼굴·이름·짧은 반응이 쌓임. 주행 중엔 끼어들지 않음(긴장 유지).
- 도착 장면 연결(가설): 꼭대기에서 열릴 때 받는 사람이 문 앞에 서 있는 게 멀리 보임 / 돌아오는 길에 만났던 사람들이 다시 보임(해소+인정, 해체 1 ③).
- 기존 연결점: OrderJob.callerName·urgentSituation(긴급 전화) — 같은 인물로 묶는다.
- 확정(도현 "좋다"): 건넬 때 = 평소엔 다가와 받기 + 말풍선 한 줄·몸짓(1~2초), 판의 마지막 배달·긴급 배달만 짧은 컷(카메라가 사람 쪽, 3~4초). 큰 순간은 드물어야 크다. 반응은 화물 상태(무사/파손)에 따라 다르게. 코스당 고정 인물(가설).

## D-132 (2026-10-25) 맵 제작 = 지형 먼저 + 스플라인 길(A) · 교토 구간은 탑 빼고 길바닥 높이에 집중
- 원인 진단: 지금 코스(BuildPostcardSlice 조각 이어 붙이기)는 길이 먼저고 지형·건물을 길에 붙임 → 고정 반경 굽이·띠 같은 길·평행 한 줄 건물·높이차 없음.
- 도현: A(지형 먼저 → 제어점 스플라인 길 → 옹벽·단차·건물은 지형 높이에 맞춰 배치)로 간다. 첫 적용 = 언덕 위 도착 구간 ~200m(D-131 도착 장면과 같이), 합격하면 교토·포지타노로 확장. 해안 절벽·바다 유지.
- 도현: 교토 구간의 야사카 탑(길 끝 랜드마크) 장면은 애매 → **빼고, 아래쪽 도로(길바닥 높이의 상점가·처마·소품·사람 기척)에 집중**. 길을 읽게 하는 장치는 탑 대신 길 자체의 굽이·경사·가게 앞 밀도로(가설).
- 도현: 가져다 쓸 오픈소스·퀄리티 도구 리서치 → research 과제.

## D-133 (2026-10-25) 지형 생활감 = "사람이 깎은 길" 의 논리로 만든다
- 도현(1차 Hilltop 캡처 보고): 지형 자체는 괜찮은데 적당한 현실감·생활감이 필요. 후보(계단식 밭·축대 / 고르지 않은 식생 / 닳은 표면 / 전봇대·난간) **전부 포함해서, 결국 사람이 깎은 길이니까 그걸 생각해서 만들면 생활양식·자연스러움이 나온다.**
- 규칙(가설, 생성기에 그대로): 길을 낸 이유와 공사 흔적이 지형에 남는다 —
  산 쪽 = 깎은 면(맨흙·바위 노출, 위는 덤불) + 짧은 석축 / 바다 쪽 = 메운 면 + 난간 / 길가 배수로·굽이 안쪽 배수구 /
  헤어핀 사이를 사람이 걸어 내려가는 계단 지름길 / 평평한 땅 = 사람이 만든 곳(집·계단식 밭·축대·작은 주차) /
  닳음은 쓰는 곳에만(바퀴 자국, 굽이 바깥 흙, 가게 앞) / 전봇대는 길 따라, 전선은 집으로 / 식생은 손 안 닿는 곳(벼랑·급경사)만 무성.
- 적용: Hilltop 3차(2차 = 꼭대기 열림 끝난 뒤). 같은 규칙을 교토·포지타노 구간 확장에도.

## D-134 (2026-10-25) 맵 전체 구성 먼저 = A "만(灣)을 도는 고리" · 평면도 → 빌더 순서
- 도현(Hilltop 3차 씬을 직접 둘러봄): 주행 구간은 나쁘지 않지만 이 화면만 쓸 게 아니다 — **맵 전체 구성이 먼저**. 자세히 보면 길 끝이 낭떠러지, 갈 수 없는 곳에 장식 건물(길 없는 바둑판 마을).
- 선택: A — 항구 가게(바닥) → ① 교토식 비탈 상점가 오름 → ② 언덕 꼭대기 열림(Hilltop) → ③ 포지타노 비탈 마을 내려감 → ④ 해안 절벽길로 항구 복귀. 갈 때/올 때 다른 길, 돌아오는 길은 바다를 보며(D-125). 프로토 약 1km, 배달지 4~5.
- 규칙(가설, D-133 을 맵 단위로): 지형(만·언덕·절벽)이 지도를 정한다 / 보이는 건물은 차길·골목·계단 중 하나로 닿는다, 못 닿는 건 안개 너머 원경만 / 길 끝엔 이유(부두·전망대·공사 바리케이드·터널) — 낭떠러지로 끊긴 길 금지.
- 순서: 위에서 본 평면도(데이터 + 그림) → 도현 판정 → 빌더가 평면도에서 생성. Hilltop 슬라이스는 ② 구간으로 편입, 슬라이스 QA·커밋은 평면도 확정 뒤.

## D-135 (2026-10-25) 맵은 트랙이 아니라 동네 길망 — 지름길·가로지르는 길
- 도현(평면도 v1 판정): "괜찮긴 한데 실제 마을 같으려면 지름길·가로지르는 길이 있어야 — 이건 그냥 트랙".
- 고리 본선은 뼈대로 남기고, 구간 사이를 잇는 마을 차길·가로지름·골목을 더해 **배달지 사이 길이 2개 이상**(빠르지만 거친 길 vs 느리지만 순한 길)이 되게 한다 — 핵심 재미(빠르게 vs 조심히)를 길 선택으로.
- 블록(길로 둘러싸인 동네 덩어리)이 생기고 교차로가 있는 길망. 플레이어가 길을 고르고 외우는 맛(반복 6).
- 같은 회차에 만 윤곽(북쪽 곶)도 오목하게 고친다.

## D-136 (2026-10-25) 길망보다 먼저 = 운전 리듬 (직선·탁 트인 구간·속도 낼 곳)
- 도현(평면도 v2 판정): "조금 괜찮긴 한데 게임에 안 어울림" → 이유 = "운전이 재미없어 보임 — 직선·탁 트인 구간·속도 낼 곳이 없음".
- 평면도는 동네 지도이기 전에 코스다: 본선에 속도 구간(긴 직선·완만한 고속 굽이, 바다가 탁 트이는 곳)과 기술 구간(헤어핀·골목)이 번갈아 오는 리듬을 둔다.
- 지름길은 "빨라서" 뿐 아니라 "달리는 맛"(내리막 직선·점프성 둔덕·좁은 틈)이 있어야 고를 이유가 된다. 길 수는 늘리기보다 의미 있는 갈림 몇 개.
- check_plan 에 리듬 검사(직선 길이, 속도 구간 비율, 고속/저속 교대)를 넣는다. 수치는 리서치(운전 게임 코스 설계) 후 확정.

## D-137 (2026-10-25) 세계 = "어디인지 모를 기억 속 항구"(키키 배달부 코리코식) · 하루가 흐르는 빛
- 도현(v1 캡처·새 건물 본 뒤): "너무 일본 느낌, 이탈리아 느낌도". 원인 = 아트가 교토 마치야 형태를 그대로 만듦(D-128 은 교토에서 '표면'만 가져오기로 했었다).
- 선택 B: **이탈리아 해안 마을의 형태**(포지타노·아트라니·프로치다 — 파스텔 회벽·아치·테라코타·덧창·계단 골목) + **일본식 생활의 층**(전선·자판기·작은 간판 여러 개·화분 줄·실외기·우체통) 을 한 동네로 섞는다. 구간별로 나누지 않는다(D-134 의 ① "교토식" 구간 이름은 "상점가 오르막"으로 성격만 남김).
- 비율 가설: 실루엣·지붕·벽 = 이탈리아 8, 길가 눈높이 소품·간판·전선 = 일본 생활. "어딘지 모르겠지만 그리운" 이 목표, 관광 엽서 금지.
- 빛: 도현 "1,2,3,4 다" → **하루가 흐른다**(아침 안개 → 맑은 가을 오후 → 해 질 녘 주황 역광 → 창에 불). PD 해석(가설): 판마다 시간대가 넘어가 같은 동네가 다르게 보인다, 돌아오는 길은 다음 시간대로 기운다. 프로토에선 무드 프리셋 4개 + 판 순서로 전환.

## D-138 (2026-10-25) 코스가 아니라 "은근히 자유로운 동네" — 땅·길찾기·끝 모름·멈춤 넷 다
- 도현(BayLoop v2 캡처 본 뒤): "전체 맵은 괜찮은데 마을보다 트랙 같다. EDC 의 은근히 자유로운 느낌이 하나도 없다, 정해진 곳을 다니는 느낌, F1 트랙과 마을의 애매한 경계". 해석 후보 넷(A 길 밖 / B 길을 내가 찾음 / C 끝을 모름 / D 아무 데나 멈춤) → "다".
- PD 발견: EDC 길망은 사실상 외길이다(리서치 similar-games §8). 자유로움은 길 수가 아니라 **허락**에서 온다 — 눈밭 어디로든 차가 감, 미니맵·내 위치 없음, 안개 너머 규모, 시계 없는 시간. 우리 맵은 길은 많은데 허락이 없다(볼라드·일정한 폭·모든 집이 본선을 봄·배달지가 본선 위).
- 규칙: ① 차가 갈 수 있는 길 아닌 땅(광장·부두·해변·밭·공터·주차장)을 둔다, 막는 건 자연물(벼랑·바다·건물·계단)만, 볼라드 금지(계단 위 화분 등 이유 있는 것만) ② 배달지는 동네 아무 데나, 경로선 없이 주소·랜드마크·표지판으로(미니맵은 가설로 유지 여부 시험) ③ 길 몇 갈래가 안개 속으로 수백 m 이어진다(옆 마을 산길·등대 곶길·터널) ④ 주문 사이·정산 뒤는 시계 없이 어슬렁.
- D-136 리듬은 "코스"가 아니라 "길의 성격"으로 남긴다(해안 직선 = 밟는 길). 본선 고리 개념·고리 기준 검사(R1·R4)는 경고로 내린다.

## D-139 (2026-10-25) 길과 집 사이에 사람이 있는 층 — 인도·경계·앞마당
- 도현: "도로 옆에 바로 도로(건물)가 있는 마을은 거의 없다. 인도도 있고 구조물이 사이에 있어서 사람이 있는 공간도 있어야 현실처럼 느껴진다."
- 길 단면 = 차도 | 경계(낮은 경계석 8~12cm·흰 선 길가띠·배수로) | 인도·길가띠(1.2~3m) | 건물 앞 공간(계단·화분·가게 앞 진열·테이블·자전거·자판기) | 건물. 길 종류마다 단면을 다르게:
  큰길(해안도로) = 연석 인도 + 가로등·벤치·난간 / 동네 차길 = 흰 선 길가띠 + 전봇대·화분(일본식 생활 층, D-137) / 광장·부두 = 차·사람 함께 쓰는 돌포장 + 카페 테이블·노점 / 골목·계단 = 사람만.
- D-138 과의 관계: 경계석은 낮아서 차가 넘을 수 있다(넘으면 화물이 흔들림) — 자유는 유지하고 대가는 화물. 사람이 있는 공간 = 나중 NPC·보행자 자리(D-131).

## D-140 (2026-10-25) 동네에 강약 — 좁은 골목·빽빽함은 일부 구역만, 건물은 많이·다양하게·같은 톤
- 도현(생활 보드 판정): 4번(야나카 긴자 계단 상점가)은 특정 지역에만 있는 느낌, 2·3번(야나카 골목·우시마도 항구 골목)은 너무 좁다 — 그런 구역이 있는 건 괜찮지만 지금은 모든 길이 그 느낌. 빽빽한 구역이 있으면 성긴 구역도 있어야 한다. 건물이 똑같을수록 별로 — 최대한 다양하되 톤이 맞는 건물을 많이.
- 구역 강약(가설): 밀집 = 항구 앞·상점가 옛 중심(좁은 골목·붙은 집, 지도 면적 ~25%) / 보통 = 마을 차길(집 사이 틈·마당·담장, ~40%) / 성김 = 언덕 밭·곶·해안 빌라(띄엄띄엄 집, 큰 마당·나무·빈터·밭, ~35%). 길 폭도 구역 따라(밀집 3.5~4.5m, 보통 5~6m, 큰길 6~8m), 좁은 골목은 밀집 구역에만.
- 건물: 손으로 2~3채씩 만드는 대신 **같은 톤 규칙의 매개변수 생성기**(폭·층·지붕·개구부·발코니·차양·색·마모 조합)로 type 마다 수십 변형 + 성긴 구역용 새 type(마당 있는 단독주택·농가·작은 성당/종탑·학교·주유소·버스 정류장 등 동네 생활 시설). 같은 모양 반복이 눈에 띄지 않는 것이 목표, 톤(팔레트·마모·32px/m)은 하나.

## D-141 (2026-10-25) "사람이 사는 길" 규칙 — 잔디 앞마당·평행 줄 세우기·매끈한 차로가 원인
- 도현(v4 캡처): "괜찮긴 한데 건물들이 왜 이렇게 부자연스럽지, 도로가 사람이 사는 도시라는 느낌이 안 든다."
- PD 진단(캡처 04·05·07 대조): ① 인도와 건물 사이가 잔디 띠 — 실제 마을은 땅이 거의 다 딱딱한 면(돌·콘크리트·자갈), 초록은 화분·마당·밭뿐 ② 건물이 같은 후퇴·같은 높이·같은 방향으로 수백 m 평행 줄 — 영화 세트 ③ 옆길·골목 입구가 안 보이는 긴 벽 복도 ④ 차로가 매끈한 아스팔트 + 양쪽 끝까지 흰 선(성긴 구역은 중앙 점선·잔디 섬) = 서킷 ⑤ 생활 흔적 0(주차된 차·스쿠터·빨래·횡단보도·반사경·땜질·맨홀·차고 입구 턱) ⑥ 성긴 구역 집이 잔디밭 위 상자 — 마당 담장·진입로·밭이 없음.
- 규칙(가설): 동네 안 지면 기본 = 포장(초록은 이유 있는 곳만) / 건물은 인도 가장자리에 붙고(밀집 0~0.5m, 보통 0~3m 앞마당은 포장·담장), 높이·후퇴·지붕선이 들쭉날쭉 / 밀집·보통 구역은 30~60m마다 옆길·골목 입구 / 흰 선은 큰 해안도로만, 마을 길은 노면 표시 거의 없음(횡단보도·정지선 정도) + 땜질·색 다른 포장 / 길가 주차·스쿠터·생활 소품 / 성긴 구역 집은 담장·대문·진입로·텃밭과 한 세트.

## D-142 (2026-10-25) 이야기 = 레이서 지망생의 F1 배달 아르바이트 · 게임 시작 = 시네마틱 + 첫 배달 튜토리얼
- 도현: "스토리로 왜 이 차로 배달을 하는지 보여 주면 될 듯 — F1 레이싱에 나가고 싶은 사람이 연습도 하고 돈을 벌려고 시작한 F1 배달일."
- 맵이 덜 만들어져 어디서 시작할지 애매 → 실제 게임의 오프닝 시네마틱·튜토리얼을 만든다고 생각하고, 그 동선에 필요한 구간부터 완성한다.
- 함의: 정산 = 돈 + 꿈(레이스 참가)까지의 거리, 해안 직선 = 연습 랩(밟는 이유), 화물 조심 = 프로의 정확함. 정비소 PitCrew = 차를 고쳐 주는 동네 사람들(D-131 함께).
- 차: F1 유지(이야기의 중심). 표면은 동네에 맞춘다 — 후보 = A 정비소가 고쳐 쓴 중고 F1(바랜 도장·테이프·짐칸) / C 올드 랠리 감성 쪽으로 더 민 개조 F1. 실제 마을 캡처 위에 놓고 도현이 고른다(D-143 대기).
- 같이: 카메라 흔들림이 부자연스럽고 각도가 불편, 길이 이상하게 이어진 곳 → 튜토리얼 동선 구간부터 고친다.

## D-143 (2026-10-26) 카메라 = B(높고 멀리) · 차 = A·C 둘 다
- 도현: 카메라 "B 좋고", 차 "A, C 둘 다 좋은 것 같은데".
- 카메라 기본 B(높이 2.8·거리 5.6·피치 10.7°·FOV 52→64) 확정. 떨림 수정(조향 추종·위치 흔들림 제거)은 직접 플레이 판정 대기.
- 차: A(정비소가 고쳐 쓴 중고 F1)와 C(올드 랠리 개조 F1) 모두 채택. PD 가설: A = 시작 차, C = 정비소에서 개조해 가는 다음 단계(D-142 이야기 — 돈 벌어 고쳐 가는 차). 진행 방식은 이야기·메타 설계 때 확정.

## D-144 (2026-10-26) 게임 안 차 = Blender 합성 시안 느낌 그대로
- 도현(A/C 합성 시안 art/_review/2026-10-25-car-candidates/village-compare.png 를 다시 보내며): "차도 이전에 뽑은 이 이미지 느낌대로 ㄱㄱ".
- 기준 = 합성 시안: A 선명한 적갈색·주황 패널 + 회색 롤케이지·짐 트레이, C 아이보리 차체 + 갈색 짐 랙, 바퀴 흰 림. 게임 안 캡처(opening-v0/car_A_hill.png)는 차체가 어두운 회색 얼룩으로 뭉개짐 → 색·밝기·질감 대비가 시안과 같아질 때까지 맞춘다(시안 = 판정 기준 장면).

## D-145 (2026-10-26) 그림체 재검토 — 같은 장면 3가지 그림체 비교 후 결정 · 해안은 실제 구조물로
- 도현(직접 플레이 스샷 docs/qa/evidence/dohyun-1509/): 해안 비탈이 "그림판에 색칠한 느낌", 실제 해안도로처럼 돌 같은 에셋을 놓아야 / 차가 "도트긴 한데 안 어울림" / 사람도 실제 사람처럼보다 "우리 게임에 알맞게".
- PD 진단: 사진→픽셀 질감(D-123 규칙 2·3)이 모든 면에 깔려 노이즈처럼 보이고, 형태는 단순한데 질감만 복잡 → 형태·질감 정보량 불일치. 해안은 경사 질감만 있고 구조물(갯바위·옹벽·테트라포드·석축·난간)이 없음.
- 결정: 같은 해안도로 장면(차·사람 포함)을 그림체 3가지로 찍어 나란히 보고 도현이 고른다(D-123 표면 규칙 재검토 — 고른 뒤 브리프 개정). 해안 구조물 배치는 그림체와 무관한 형태 작업이라 먼저 진행.

## D-146 (2026-10-26) 그림체 = 노이즈 없는 저해상도 레트로(PS1/드림캐스트, 큰 텍셀) · 사람 = 4~5등신 조금 귀엽게 · 빛 = 시간대마다 다르게
- 도현(스샷 docs/qa/evidence/dohyun-1940/): 점무늬 사진 질감은 "레트로 감성보다 더럽게 느껴진다". 사람은 SD 냐 귀엽게냐 — 게임 분위기를 자세히 정해야.
- 표면: 저해상도 레트로는 유지하되 **노이즈 없이** — 큰 텍셀(가설 8~16px/m), 색 수 적게, 한 면은 2~3색 정도의 그려진 디테일(창틀·벽돌 줄·풀 몇 가닥)만, 사진 점무늬 금지. D-123 규칙 2(CC0 사진→픽셀) 폐기, 규칙 3(공용 팔레트·한 픽셀 격자)는 유지.
- 사람: 4~5등신, 큰 머리·점 눈·단순한 얼굴, 옷 색면(A Short Hike·Lil Gator). D-127 "동네는 사실적"은 건물·길 비례에만 적용, 사람은 조금 귀엽게.
- 빛·색: 하나로 고정하지 않고 시간대마다(D-137 하루가 흐름) — 단 지금처럼 전체가 어둡고 탁한 것은 어느 시간대든 금지(오후 = 맑고 밝게, 해 질 녘 = 따뜻하게, 아침 = 옅은 안개).
- 같이 고칠 것(스샷): 건물 기단·포장이 2m 계단 모양 조각, 층무늬 바위판이 깨진 판자처럼 보임, 언덕 면이 초록/노랑 삼각형으로 찢어짐.

## D-147 (2026-10-26) 기준으로 되돌아감 — Easy Delivery Co. + PS1 느낌 · 사람 v3·Retro-Clean 보류
- 도현: "v3 별로, 게임 톤과 안 맞음. 새로 칠한 배경도 그렇고 Easy Delivery Co. 가 레퍼런스였는데 점점 멀어지는 느낌. PS1 느낌도 조금 주고 싶다."
- PD 반성: D-146 해석(큰 색면·노이즈 제거·귀여운 4~5등신)이 A Short Hike 쪽으로 미끄러졌다. EDC 의 실제 화면 문법(저해상도 렌더·PS1 정점/텍스처 특성·안개·디더·투박한 로우폴리 사람)을 먼저 분석하고 그 위에서 "더럽지 않게"를 풀어야 했다.
- 진행: EDC 실제 스크린샷으로 화면 문법 해체(렌더 해상도·텍셀 밀도·색 수·디더·정점 흔들림·어파인 왜곡·안개·사람 모델 스타일) → 우리 캡처와 같은 표로 대조 → PS1 렌더 효과(Look 5)를 원래 질감 위에 얹은 판으로 비교. 사람 v3 교체·Retro-Clean(Look 4) 은 보류.

## D-148 (2026-10-26) 밝은 분위기 + EDC 화면 문법 · 사람 = PS1 시절 로우폴리 실물 비례
- 도현: EDC 는 "너무 분위기가 어두운데, 밝은 분위기의 저런 느낌을 주고 싶다", 사람은 "PS1 느낌".
- 화면: Look 5(EDC 문법: 저해상도·부드러운 큰 얼룩 텍스처·하늘=안개·평평한 빛·정점 스냅)는 유지, 단 밝고 맑게 — 1차가 뿌옇고 물 빠짐 → 대비·하늘 파랑·해 쪽 따뜻함을 살리고 업스케일은 선명한 Nearest.
- 사람: PS1 시절 게임 사람(각진 로우폴리, 실물 비례 6.5~7등신, 저해상도 손픽셀 얼굴·옷 텍스처, 12fps 끊기는 동작). 귀여운 4~5등신(v3)·동물 주민은 안 함.
- 비용: team.sh 의 --max-budget-usd 가 지켜지지 않음($10 걸고 $50.70) — 턴 수만 지켜짐(약 $0.44/턴). 비용은 턴으로 묶는다.

## D-149 (2026-10-26) 지글거림 금지 — 은은하게 뭉개진 몽환적 저해상도
- 도현(직접 플레이, docs/qa/evidence/dohyun-2110/1.png): 지글지글한 곳이 많아 "성능 이상한 게임"으로 보인다. 어느 정도 뭉개면서 몽환적·은은한 느낌이어야.
- 결정: 저해상도는 유지하되 선명한 Nearest 계단·정점 흔들림·깜빡이는 가장자리는 빼고, 부드러운 확대 + 약한 블룸/헤이즈로 "기억 속 화면"처럼. 움직일 때 깜빡임(시간적 안정성)이 정지 화면보다 우선 기준.

## D-150 (2026-10-26) 게임 톤 기준 확정 — docs/design/TONE.md
- 도현: 사람 작업 멈추고 톤·분위기를 정확히 정한 뒤 그걸 중심으로 만들자.
- 한 문장: "늦가을 오후, 혼자 달려도 외롭지 않은 배달길 — 서늘한 공기, 동네 사람 기척(키키 배달 장면)".
- 기억 장면: "배달을 바쁘게 하고 돌아오면서 느껴지는 한적한 마을과 자연" → 감정 곡선 = 바쁨→해소, 돌아오는 길이 보상.
- 은은함 = 밝은 안개·헤이즈(멀수록 하얗게 녹음) 하나. 필름 번짐·CRT 안 씀.
- 사람 v4 작업 중단, 사람 비례·얼굴은 톤 기준 장면 위에서 다시 판정.
- 다음: "돌아오는 길" 기준 장면 한 컷(해안 언덕길, 늦가을 오후~노을)을 TONE.md 판정 질문 5개로 맞춘다.

## D-151 (2026-10-26) 해상도 960×540 · 재미 3기둥 · 맵 = 엽서 장소 6~8곳부터(A)
- 도현: 270 은 오히려 안 좋음, 540 이 적당(Play 중 직접 비교). → PCLookScene.lowResHeight 540.
- 도현: 재미 = ①맵이 볼만함 ②배달에 어느 정도 서사 ③업그레이드·레벨 디자인 구성. 지금 ①부터 안 됨.
- PD 진단: 맵 전체를 생성기로 고르게 깔고 표면만 전역으로 바꿔 와서 "여기 좋다" 장소가 없다(톤 장면 3회 5/10 고정 = 레버가 표면이 아니라 구성).
- 결정 A: 기억에 남는 엽서 장소 6~8곳을 먼저 정해 손으로 구성(장소마다 한 문장 장면·소리·그곳의 배달 하나), 나머지 길은 그 장소를 잇는 연결부(열리는 순간·랜드마크 시선). 판정 = "이 장소가 엽서가 되나". 장소의 배달·사람이 ②서사, 장소 해금이 ③레벨 디자인의 뼈대.

## D-152 (2026-10-26) 엽서 장소 1호 = 주민 지역 비탈 스위치백 마을 — docs/design/PLACES.md
- 도현: 항구·상점가 대신 "그냥 주민지역 먼저", 비탈 쪽 = 계단 골목 스위치백(포지타노), 자연 4곳 = "다 좋은 듯".
- 차는 계단을 못 가므로 차 길 = 헤어핀 스위치백, 계단 골목 = 사람 지름길(보이기만). 꼭대기 굽이에서 마을·바다 열림.
- 진행: 블록아웃(구성 판정) → 도현 판정 → 표면·소품. 구성 판정 전에는 표면 다듬기 금지.

## D-153 (2026-10-26) 질감·마을 기준 = 키키·붉은 돼지 배경 + 우리 게임의 도로·비탈
- 도현(art/reference/ghibli-2026-10-26/): 마녀 배달부 키키·붉은 돼지의 질감과 마을, 그러면서 우리 게임에 맞는 도로·비탈 느낌을 원래 원했다.
- 변경: 표면·색 기준을 EDC 에서 지브리 배경으로 옮긴다. EDC 는 화면 문법 일부(하늘·안개 한 덩어리, 지글거림 없음)만. TONE.md 색 항목("바랜 가을색") 재검토 — 지브리는 맑고 진한 바다·흰 적운·주황 기와 군집.
- 순서: 지브리 배경 문법 해체(색·채도·구름·지붕 군집 밀도·붓질 크기·빛) → 실시간 3D 구현 사례 조사 → Look 6 시험. 엽서 01 블록아웃은 구성 작업이라 그대로 진행.

## D-154 (2026-10-26) Look 6 지브리 — 자연은 진하게·마을은 바래게, 원경은 청록으로 가라앉음
- 근거: docs/research/2026-10-26-ghibli-town-grammar.md(규칙 12, 레퍼런스 측정).
- 도현: 바다 깊은 청록·하늘 진한 파랑·벽길 바랜 색 / 원경은 청록 대기 원근, 지평선 한 줄만 하얗게. TONE.md 3·5 개정.
- 금지(분석의 실패 3): 화면 전체 채도 올리기, 화면 공간 페인터리 필터(Kuwahara 등), 검은 윤곽선·툰 램프·동글 나무 반복.

## D-155 (2026-10-26) 플레이 먼저 — 배달 주행이 재미있는 맵을 회색 박스로 확인한 뒤 비주얼 (docs/design/PLAY.md)
- 도현: 마을 분위기도 중요하지만 실상은 게임플레이에 집중할 수 있어야. 차 몰고 배달하는 행위 자체가 재미있는 맵을 먼저, 비주얼은 그 다음.
- 재미 4기둥(도현 전부 선택): 화물 긴장 · 길 고르기 · 운전 실력 · 지형 변화.
- 진행: Proto_Play01 회색 박스(차고+배달점 4, 배달마다 큰길/거친 지름길) → 도현 직접 플레이 판정 → 통과한 길 배치를 BayLoop 길망으로, 엽서 장소·Look 6 은 그 위에.
- 팀원은 사람처럼 운전해 판정하지 않는다(시간·턴 낭비, 엽서01 블록아웃 71턴 실패) — 경로 봇으로 끼임·시간·화물 충격만 숫자로.
- 엽서 01 블록아웃(평지 S자 트랙)은 폐기·미커밋. Look 6 은 보류.

## D-156 (2026-10-27) 마을이 먼저, 길은 마을의 길 — 실제 마을 층 합성(코리코식)
- 근거: 실제 마을 8곳 + BayLoop 같은 축척 지도 docs/map/real-towns/sheet.png. BayLoop v4 = 고리 하나 + 220채 줄 세움(트랙). 실제 마을 = 지형이 정한 길 그물 + 블록을 채운 건물 500~2000채.
- 도현: Proto_Play01(조건 맞추려 지은 코스) 거부 → 실제 마을 지도를 띄워 보고 정하자 → PD 조합안 수락 + "격자와 해안선을 따라 생기는 길, 그리고 부드러움을 잘 조합".
- 조합: 항구 심장 = 도모노우라(만 안쪽 촘촘한 옛 격자) / 비탈 = 카몰리·포지타노(등고선 평행 길 2~3겹 + 헤어핀 + 계단, 위로 갈수록 성김) / 바깥 = 아말피(곶 도는 해안 큰길 → 터널·등대 곶·꼭대기 밭).
- 부드러움: 격자도 직각 바둑판이 아니라 지형·해안 따라 휘는 유기적 격자, 길 모서리·교차는 둥글게.
- 방법: 고리를 먼저 그리지 않는다 — 땅·항구·블록 → 사람이 다닐 길 → 그 다음 재미 4기둥(PLAY.md) 검사. 2D 평면에서 도현 판정 후 Unity.
- 크기 700~800m 사방, 건물 600+.

## D-157 (2026-10-27) 맵 뼈대 = v6-OSM(실제 지도 합성) 채택
- 비교 docs/map/v6-vs-v5c.png. 도현: "1안이 좋을 것 같긴 해". 생성기(v5~v5c)는 보류.
- 다음: v6b 평면 보강(지름길 5쌍 중 4쌍 이상 — 실제 사람길을 좁은 돌길로, 이음매 정리, 경사 현실화) → 회색 박스로 Unity 에 지어 도현 직접 주행 판정. 비주얼(Look 6 지브리·엽서 장소)은 그 뒤.
- 라이선스: OSM ODbL, 크레딧 "© OpenStreetMap contributors"(docs/map/ATTRIBUTION.md).

## D-158 (2026-10-27) 게임 구조 — 축제 날 해안도로 대회가 최종 목표 · 단골 이야기 3종 · 마을 열림+바깥은 이야기로 (docs/design/WORLD.md)
- 도현: F1 꿈 줄기 "크게"(대회 출전이 최종 목표, 배달은 준비), 단골 이야기 세 종류 전부, 진행은 섞기(마을 다 열림, 바깥만 이야기로).
- 도현 수락("그렇게 해도 괜찮을 것 같기도"): 출전 조건 = 차(업그레이드)·실력(시험)·마을의 응원(단골 후원) → "혼자 달려도 외롭지 않은"이 규칙. 대회 코스 = 매일 달린 해안 큰길·헤어핀(홈 코스, 숙련으로 뒤집기).

## D-159 (2026-10-27) 오프닝 = 주인공 방에서 라디오 F1 중계, F1 포스터, 오늘부터 배달 시작
- 도현 지시. 2026-10-25-opening-tutorial.md v1 로 반영: 방(30~60초 둘러보기) → 라디오 지역 소식으로 축제 대회 떡밥(D-158) → 현관 밖 첫 엽서 장면 → 정비소 사장이 키 건넴 → v0 첫 배달 튜토리얼.
- 라디오 소리 출처는 미정(실제 중계·AI 음성 금지).

## D-160 (2026-10-27) 차가 자란다 — 고카트 → 포뮬러 주니어 → F1 개조차
- 도현: 처음부터 F1 로 배달은 말이 안 되니 아이들 고카트로 시작. PD 후보 중 3단계 성장안 선택.
- 카트 뒤 작은 짐상자, 초반 배달은 작고 가벼움. 차급 전환 = 업그레이드·꿈 줄기의 큰 마디(QUESTS 업그레이드 표·오프닝 v1 반영).
- 할 일(나중): 차급별 조작 수치 3세트, 고카트·주니어 모델(아트), 기존 차 A/C 는 F1 개조차 단계.

## D-161 (2026-10-27) 건물 = 배경 절차 생성(OSM 바닥 모양) + 이야기 건물 손 모델링 · 아트팀 2(Claude Opus) 추가 (docs/design/BUILDINGS.md)
- 도현: 건물을 어떻게 만들고 아트 스타일대로 맵을 어떻게 구성할지 + 아트팀 astra(Codex) 하나뿐인데 Opus 도 같이 쓰자.
- 분담: art2(Opus) = 절차 생성·텍스처 스크립트·배치 규칙 / art(Codex) = 손 모델링(이야기 건물·차·사람).
- 입히는 순서 7단계, 먼저 항구 블록 하나를 끝까지 만들어 판정.

## D-162 (2026-10-27) 배경 건물 = 손으로 만든 모듈 키트 + 자동은 배치만
- 도현: 지금까지 절차적으로 만든 아트 중 퀄리티 좋았던 게 없다.
- PD 동의: 규칙이 형태·질감까지 만들면 평균적인 결과(v1~v5 맵, 매개변수 건물, Retro-Clean)가 나왔다. 품질은 손으로 공들인 조각에서, 자동은 어디에 둘지만.
- 시험: 항구 키트(15~25 조각)로 블록 하나를 손 조립(품질 천장) → 자동 조립과 비교. art2 절차 생성 과제는 중단.

## D-163 (2026-10-27) 아트 스타일 = 도트(픽셀) 기반 로우폴리 — 지브리는 색·빛·분위기
- 도현(정비소·고카트 v0 를 보고): "도트 기반 로우폴리인데 둘 다 아닌 것 같다" + Claude Opus 에게도 맡겨 보자.
- PD 정리: 형태 = 로우폴리(면 적고 각진 실루엣, 매끈한 곡면·베벨 없음) / 표면 = 손으로 찍은 도트 텍스처(큰 텍셀·Point, 색 수 적게, 사진·노이즈·그라데이션 없음 — D-146 "노이즈 없는 레트로") / 색·빛·하늘·그늘 = 지브리 규칙(D-154). 화면은 960×540(D-151), 지글거림 금지(D-149).
- 진행: 정비소·고카트를 아트 1(Codex)과 아트 2(Opus)가 같은 사양으로 각각 만들어 나란히 비교 → 도현이 고름. 항구 키트도 이 스타일로(비교 뒤 재개).

## D-164 (2026-10-27) 아트 = EDC 방식 + 밝게, 화면 1280×720 — D-163 수정
- 도현: "EDC 느낌이 도트 로우폴리 아닌가?" → PD: EDC 의 도트는 텍스처가 아니라 저해상도 화면에서 나온다(측정: 텍스처는 부드러운 큰 얼룩, 텍셀 네모 안 보임). 팀은 반대로 텍스처에 손도트를 찍었다(D-163 해석 오류).
- 도현 선택: EDC 방식 + 밝게, 해상도는 "아까 재 봤을 때 720 이 적당" → 내부 해상도 1280×720(1080 화면이면 1.5배, 부드러운 확대). D-151 의 540 대체.
- 규칙: 로우폴리 형태 + 단순·부드러운 텍스처(큰 색면·저주파 얼룩·필요한 곳만 그린 디테일, 손도트·노이즈·사진 점무늬 금지) + 720 저해상도 화면 + 지브리 색·빛(바랜 마을, 진한 자연, 청록 그늘). 기준 화면 = EDC 스크린샷(art/reference/edc-2026-10-26/)의 밝은 버전.

## D-165 (2026-10-27) 기준 화면 = 보드 ①⑤⑥⑦⑫ · 확대는 계단(Nearest) 960×540 2배 · 정비소·고카트 v2 거부
- 도현: 보드 1·5·6·7·12 선택(A Short Hike·Wheel World·EDC·Jalopy·Crow Country — 역할표 art/reference/dotlp-2026-10-27/PICKS.md). 부드러운 확대 "별로"(D-164 의 720 부드러운 확대 철회). 정비소·고카트 v2 "솔직히 별로, 퀄리티가 더 낮아졌다".
- 화면: 960×540 → 1080 화면 정확히 2배 Nearest(정수배).
- 아트: 이후 모든 아트 과제는 PICKS.md 번호로 지시하고 같은 조건(540 렌더 + 2배 Nearest)으로 기준 칸과 나란히 판정.
- **D-165 정정 (도현):** "해상도 720 으로 하자니까 왜 계속 540 으로 하나" — 내부 해상도 = **1280×720 확정**, 확대 = Nearest(부드러운 확대 금지). PD 가 "부드러운 확대 별로"를 540 으로 잘못 옮겼음. 540 지시는 모두 무효.

## D-166 (2026-10-27) 품질 처방 = 장면 하나를 기준 화면 수준까지(A) + 형태 언어 시트 먼저(B)
- 진단 docs/research/2026-10-27-quality-gap.md: 품질은 에셋 단품이 아니라 장면(밀도·구도·안개·색 무리)에서 나온다 / 형태 과장 규칙 없음 / 코드 모델링의 한계 / 기준 7번 바뀜 / 게임 밖 판정.
- 도현: A + B (C 에셋 팩 구매는 안 함).
- B: 기준 칸 ①⑤⑥⑦⑫에서 비례·과장 측정 → docs/design/SHAPE.md 한 장. 이후 모든 모델은 이걸 따름.
- A: Map_TownV6 항구 길 게임 카메라 고정 컷 1장을 소품·전선·안개·색 무리로 채워 ①·⑥과 나란히 "같은 게임처럼 보이나"까지. 판정은 Unity 실제 화면(720 Nearest)에서만. 에셋 단품 판정 중단.
- 기준 화면은 이제 고정한다(PICKS.md) — 바꾸지 않는다.

## D-167 (2026-10-27) 형태 언어 승인 — 건물 실제 크기, 사람·소품·나무만 키움 (docs/design/SHAPE.md)
- 기준 측정(①⑥⑦⑫): 사람이 벽만큼 큼, 지붕 끝 두께 벽의 0.22, 처마 0.28, 나무 벽의 3~4배, 소품 1.3~1.9배·화면당 ~22개, 전신주 10~12° 기울기.
- 도현: 건물은 실제 크기 유지(맵 축척 보존), 사람÷문 ≈0.8·소품 1.3배·나무 벽의 3배. 지붕·처마·기울기 과장은 적용.
- 다음: 장면 하나(A) — Map_TownV6 항구 길 고정 컷을 SHAPE 규칙 + 기준 화면으로 채움, Unity 720 화면에서만 판정.

## D-168 (2026-10-27 새벽, 도현 수면 중 자율) 아트 개선 시도 — 같은 장면 4판(V0 현행·V1 EDC 포토배싱·V2 ASH 팔레트·V3 V1+면 변화), Unity 720 에서 숫자 게이트 후 판정
- 도현: "찾은 방법 중 시도해 볼 만한 거 시도해서 아트 에셋 개선해 봐". 근거·계획 docs/design/ART-TRIAL-0427.md. 구매·외주는 아침에 후보로만.

## D-169 (2026-10-27) 연구 결론 적용 점검 — 핵심 2개를 안 했다 → 지금 적용
- 도현: 원인을 찾았는데 또 안 되는 건 적용 안 했기 때문이면 적용하고, 했는데도 안 되면 더 찾아서 가능한 방법 다 동원.
- 점검: (1) "AI 는 무에서 모델링에 약하고 조립에 강하다" → 여전히 무에서 모델링 중 = 미적용. (2) "최종 화면(Unity) 안에서 만들고 판정" → 아트팀은 여전히 블렌더 렌더로 판정 = 미적용. (3) "AI 판정자가 도현과 따로 논다" → 앵커 세트 미구축 = 미적용. (4) 디테일을 텍스처에(V1·V4) = 적용 중.
- 조치: 무료 사람 제작 CC0 에셋(Kenney Retro Urban 등, AI 표기 제외) 수집·조립 / 도현 앵커로 판정자 보정(docs/qa/judge/) / 아트 판정은 Unity 720 화면에서만 / V4 빛 굽기·V1b 계속.

## D-170 (2026-10-27) 화면 기준 = ⑥ EDC 렌더링 + ① A Short Hike 질감·분위기
- 도현 원문: "6랜더링에 1 질감과 분위기 정도면 괜찮을것같은데" (직전: "이번엔 또 톤이 너무 어둡지 않나..")
- 렌더링(⑥ EDC): 저해상도 720 Nearest, 그림자 끔·주변광=안개, 거리 안개, 텍스처에 명암 칠/굽기.
- 질감·분위기(① A Short Hike): 밝고 따뜻한 색, 채도 있는 팔레트 색면 + 계단 명암, 맑은 공기.
- 판정 기준 이미지: ⑥(렌더링)·①(색·질감·분위기). ⑫ Crow Country 는 어두운 공포 톤이라 색·분위기 기준에서 뺀다.

## D-171 (2026-10-27) 부드러운 색감
- 도현 원문: "너무 쨍한 느낌이 있는데 부드러운 색감이면 괜찮을듯" (A2d/A2f 를 보고. 직전 "2,3번 괜찮긴한데")
- 측정: 쨍함 ≠ 채도(우리 0.30 < ① 0.55~0.66). 원인 = 밝은 쪽 날아감(p95 220 vs ① 180~194) + 화면이 한 빛으로 안 물듦 + 큰 면 경계 칼같음.
- 처방: 하이라이트 롤오프, 따뜻한 오후색 전체 물들이기 + 그늘 한 색 청록, 면 경계 명도 차 한 단 축소(Nearest 유지). 장면 A3s.

## D-172 (2026-10-27) 직접 플레이 판정 — 환경 OK, 건물·차가 별로
- 도현 원문: "계속 내가 실제로 해봤을 때 환경 요소 적인 것들은 나쁘지 않은데 건물이 아직 별로고 차량도 이전걸 사용해서 별로임 이거 반영해서 다시 시작해봐"
- 유지: Scene_A3 환경(밀도·지형 기복·계단 명암·접지, 나무·소품·전선, 하늘·색). → 판정자 긍정 앵커에 Scene_A3 컷 추가.
- 고침: (1) 건물 — 도시팩 형태·어두운 텍스처 → 해안 마을 집으로 (2) 차 — 플레이 차가 옛 외형 → 새 카트(V5 방식) 외형으로.

## D-173 (2026-10-27) 건물 = 깔끔한 외벽 + 도트
- 도현 원문: "건물이 저 사진들과 너무 다르지 않아? 깔끔한 외벽 느낌이나 도트 느낌도 잘 안오는데" (집 6채 시트의 ①·⑥ 크롭 대비)
- 건물 표면은 사진 얼룩·회반죽 무늬를 쓰지 않는다: 깨끗한 칠 단색 + 면 단위 2~3단 계단 명암.
- 도트 = 낮은 텍셀 밀도(1m ≈ 8~10텍셀, 720 화면에서 텍셀 1개 = 2~4px) + 손도트 디테일(창틀·문틀·처마 끝·모서리 1텍셀 선, 덧창 살, 기와 줄).
- "면 안 단색 ≤50%" 게이트는 건물 벽에는 적용하지 않는다 — 변화는 얼룩이 아니라 도트 디테일·계단 명암에서.

## D-174 (2026-10-27) 카트 크기·바퀴, 건물 AI 티
- 도현 원문: "고카트 치고 차에 크기가 너무크고 제대로 굴러가지 않아 그리고 건물에서ai티가 너무 많이 남" (Scene_A4c_Town 직접 플레이)
- 카트: 실제 고카트 치수(≈1.8×1.25m)로, 콜라이더·바퀴 위치도 맞춤, 바퀴 분리해 굴러가고 앞바퀴 조향. 카메라 B 유지.
- 건물: 손도트 기호·고른 창 배치·대칭·같은 문법 반복이 생성물처럼 보임(가설) → EDC 처럼 실제 건물 정면 CC0 사진 포토배싱 → 저텍셀 + 32색. "벽 전체 사진 얼룩"(D-173 거부)과 다름.
- 이 판도 AI 티면 다음 수는 사람 제작 에셋 팩 구매·재칠 또는 외주(도현 결정 필요).

## D-175 (2026-10-27) 한 화면 한 그림체
- 도현 원문: "계속 느끼는건데 건물의 그림채와 환경 요소적 그림체가 너무 다른것 같아" (Scene_A4e_Town)
- 진단: 한 화면에 문법 5개(집 = 매끈 파스텔+잔 손도트, 나무 = 굵은 블록 덩어리, 땅·길 = 큰 부드러운 얼룩, 덤불·바위 = 텍스처 없는 각진 면, 소품 = 흐린 사진 노이즈).
- 규칙 docs/design/STYLE.md: 텍셀 1m≈10~12 통일, 면 안 = 큰 덩어리 2~4색, 명암 계단 3단, 2px 미만 디테일 금지, 공용 style32 팔레트. 기준 = 도현이 괜찮다고 한 나무·땅 쪽.

## D-176 (2026-10-27) 환경은 이전(A4e)이 맞음 — 건물·차만 고친다
- 도현 원문: "환경적인 요소들은 이전게 더 잘맞았던것 같은데 오히려 건물이 문제임 건물,차 빼고는 나머진 쓸만해" (A4e vs A5)
- 환경(땅·길·풀·나무·덤불·소품·전신주·정비소·하늘·색)은 A4e 그대로 고정 — 기준이 된다. A5 의 소품 STYLE 재칠(art/export/scene-a2-kit-style)은 쓰지 않는다.
- D-175 방향 수정: 환경을 바꿔 맞추지 말고 **건물·차를 환경(A4e = V5 방식: 사진 텍스처 × 구운 늦가을 빛, 32색 unlit, 정비소와 같은 표면)에 맞춘다**. 건물의 이야기·간판·덧창·손댄 흔적(e판)은 유지.

## D-177 (2026-10-27) 건물 = EDC 건물 그래픽 기법
- 도현 원문: "이런 건물 느낌 나게 만들어봐 그래픽적으로 분위기 말고" (EDC 스크린샷, art/reference/dohyun-2026-10-27/edc-buildings.png)
- 기법: 단순 상자 + 벽 한 색 평면(저채도, 노이즈 0) + 짙은 유리 창 + 1텍셀 밝은 창틀·살 격자 + 처마 짙은 띠 + 1층 가게 유리 띠·색 띠, 구운 그림자 없음, 1m≈8텍셀, 색 적음. 분위기(눈·안개)는 제외.

## D-178 (2026-10-27) 건물 형식 = 환경 재료 + EDC 정면 구성 (PD 제안)
- 도현 원문: "오히려 단색인게 더 안맞는것 같기도 함 우리 게임에 어울리게 만들려면 어떤 형식이 가장 좋아보여?"
- 근거: 오늘 도현이 OK 한 것은 전부 같은 재료(A4e 환경·정비소 = 사진 결 + 구운 늦가을 빛 + 32색). 건물이 튄 판은 모두 재료가 달랐다(c 매끈 파스텔, d 사진 정면, f/g 큰 얼룩 팔레트, i 평면 단색).
- 형식: 재료는 h2(정비소와 같은 결·빛·팔레트), 정면 구성은 EDC(창 격자·처마 띠·가게 유리 띠·옅은 줄눈), 이야기·간판은 e판. 집 j판.

## D-179 (2026-10-27) A6c 판정 — 괜찮음 + 세 가지
- 도현 원문: "괜찮긴한데 1.지금 가로등을 보면원래 그냥 검은색 이여야 하는데 이렇게 노이즈 끼듯이 나와가지고 이런 부분은 고치고 2.카트도 초록색으로 보이게 나옴 3.전체적인 채도를 낮추는것보다 약간의 노이즈가 있듯이 표현하는게 좋아보이는데"
- 집 j판 + A4e 환경 방향 승인(괜찮음) → 판정자 긍정 앵커 후보.
- 검정·회색 물체(가로등·금속 기둥·타이어)는 색 노이즈 없이 숯색 단색 + 아주 약한 명도 결.
- 카트 색: 플라스틱 바랜 주황빨강(PD 가설), 타이어·프레임 짙은 회색.
- 화면: 채도 깎기 대신 약한 노이즈 — 표면 고정 결(기본) / 정적 화면 그레인 토글로 비교.
