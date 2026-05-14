# PitCrew 파일 구조 맵

> 코드 전체 어디에 뭐가 있는지 1분 안에 찾는 지도.
> 작성: 2026-05-08 (세션 18, Phase 1 시작 시점).

---

## 디렉토리 최상위

```
PitCrew/
├── F1/                    ← Unity 프로젝트 (실 게임 코드)
├── GDD/                   ← 게임 디자인 문서 (영구 보존)
├── notes/                 ← 세션 운영·결정 로그
├── dev-plan/              ← 4주 일정·feature-list
├── .claude/               ← Claude Code 설정 (rules + activeContext + hooks)
└── CLAUDE.md              ← 진입점 (30줄)
```

---

## F1/Assets/Scripts/ — 게임 코드 (카테고리별)

### 1. Core (Phase 1 적용 완료) — 와이어링 + 추상화
| 파일 | 책임 |
|---|---|
| `Core/Core.cs` | 단일 파일에 통합: 인터페이스 12개 (DIP), `Services` 정적 레지스트리, `GameContext` Composition Root MonoBehaviour. AssetDatabase 부분 import 회피용 단일화 (세션 18). |

### 2. Core Loop — 한 런의 진행
| 파일 | 책임 | 라인수 |
|---|---|---|
| `Order/RunController.cs` | 런 시작·종료·보상. `BeginRun` 7개 명명 메서드로 분해 (2026-05-11). | ~250 |
| `Order/OrderBoard.cs` | 주문 카드 풀, 호버, 클릭 | 283 |
| `Order/PreRunSetupPanel.cs` | 화물·포장 선택 | 211 |
| `Order/ResultPanel.cs` | 정산 + 업그레이드 카드 (UGUI 전환 완료) | ~500 |
| `DeliveryManager.cs` | 배달지 활성·진행 추적 | 115 |
| `DeliveryPoint.cs` | 트리거 + 라벨 | 50 |
| `Order/OverviewCamera.cs` | 오버뷰↔팔로우 카메라 트랜지션 | 119 |
| `Order/DeliveryPointMarker.cs` | 기둥+구체 마커 | 161 |
| `Order/OrderJob.cs` · `OrderCatalog.cs` | 주문 SO 데이터 | 46+12 |
| `Order/OrderCardUI.cs` · `PackingCardUI.cs` | 카드 prefab UI | 130+92 |

### 3. Vehicle — 차량 시뮬
| 파일 | 책임 |
|---|---|
| `ArcadeCarController.cs` | 입력→힘 적용, 드리프트, 속도 노출 |
| `Cargo/CargoBox.cs` | G·spin·tilt 측정, 무결성 HP, 데미지. Phase 3 modifier-chain 적용 (`Cargo/Modifiers/`). |
| `FuelSystem.cs` | 연료 소비/재충전 |
| `GearSystem.cs` | 속도→기어 매핑 |
| `Tires/TireSystem.cs` | 그립·스피드 배율 + 마모 |
| `CarVisualLean.cs` | 차체+바퀴 시각 lean |
| `EngineAudio.cs` | 엔진 루프 pitch/volume |
| `DriftVfxController.cs` | trail+smoke 토글 |
| `FollowCamera.cs` | 팔로우 + FOV + shake |

### 4. HUD/UI — 화면 표시
| 파일 | 책임 |
|---|---|
| `FuelHUD.cs` · `GearHUD.cs` · `IntegrityHUD.cs` | 좌하 게이지 (자가 빌드, HOT-5 복붙) |
| `UI/SpeedometerHUD.cs` · `Order/CountdownHUD.cs` | 우상 속도·시간 |
| `UI/DeliveryProgressHUD.cs` · `DeliveryHUD.cs` | 진행도 + 화살표 |
| `Cargo/CargoView.cs` | 트렁크 CCTV |
| `UI/PauseMenu.cs` | ESC 토글 |
| `UI/UIRoot.cs` · `UI/UISounds.cs` · `UI/UITheme.cs` | UI 인프라·테마 |

### 5. Cargo Sim — 화물 시뮬
| 파일 | 책임 |
|---|---|
| `Cargo/CargoShakeSimulator.cs` | 박스 위치·회전, simCam 동기 (HOT-4 GameObject.Find) |
| `Cargo/CargoType.cs` · `CargoTypePresets.cs` | 5종 화물 enum + 정적 매핑 |
| `Cargo/PackingTier.cs` | 4단계 포장 enum + 임계 multiplier |

### 6. Infrastructure — 매니저·인프라
| 파일 | 책임 |
|---|---|
| `Upgrades/UpgradeSystem.cs` | Wallet + owned set 싱글톤 |
| `Upgrades/UpgradeDef.cs` · `UpgradeCatalog.cs` | 업그레이드 SO |
| `Audio/AudioManager.cs` | SFX 풀 + 음악 + 자동 후크 |
| `Layout/TrackLayout.cs` | road·building·height 마스크 SO |
| `Layout/BuildingCatalog.cs` · `BuildingDef.cs` | 빌딩 카탈로그 |
| `Route/NodeGraph.cs` | 경로 계획 SO (미사용, RoutePlannerPanel 미구현) |

---

## F1/Assets/Editor/ — Editor 스크립트
| 폴더/파일 | 책임 |
|---|---|
| `TileMapAuto.cs` | 마스크→도로 타일 배치 (heightMask + Slant 통합) |
| `BuildingPlacer.cs` | 마스크→빌딩 배치 |
| `SetupPhase1Integration.cs` | "PitCrew/Phase 1/Full Integration Setup" 메뉴 |
| `VerifyPhase1Setup.cs` | 41 항목 자동 검증 |
| `SetupOrderSystem.cs` · `SetupRaceInScene.cs` | 씬 자동 setup |
| `_Recovery/` | 1회성 복구 스크립트 (40+ 개) |

---

## GDD/ — 디자인 문서 (영구 보존)
| 파일 | 내용 |
|---|---|
| `overview.md` | 원문장 피치 + USP + 마이크로↔매크로 루프 |
| `core-loop.md` | 한 런의 흐름 상세 |
| `decision-channels.md` | 9개 결정 채널 |
| `architecture/file-map.md` | **이 문서** |
| `architecture/phase1-dip-service-locator.md` | Phase 1 리팩터 학습 포인트 |
| `systems/` | 시스템별 스펙 (cargo / fuel / gears / tires / route-planning) |
| `research/` | 리서치 문서 12편 (Easy Delivery / Death Stranding / 레퍼런스 서베이 등) |
| `ui/` · `content/` | UI·컨텐츠 스펙 |

---

## notes/ — 세션 운영
| 파일 | 내용 |
|---|---|
| `session-log.md` | 세션 한 일 (자동 stub by SessionEnd hook) |
| `decisions-log.md` | D-XXX 영구 결정 (현재 D-053까지) |
| `ideas-parking.md` | 보류 아이디어 백로그 |
| `open-questions.md` | 미해결 설계 질문 |
| `phase1-status.md` | Phase 1 코드 진행률 |
| `code-review/` · `code-review-*.md` | 코드 리뷰 자료 |
| `loop-reports/` | /loop 자동 사이클 리포트 |
| `git-setup.md` · `mcp-setup.md` · `session-opener.md` | 인프라 가이드 |
| `conventions.md` | 명명·작명 규칙 |

---

## .claude/ — Claude Code 설정 (Memory Bank 패턴)
| 파일 | 내용 |
|---|---|
| `activeContext.md` | 매 세션 갱신 (지난 세션·이번 목표·대기 결정) |
| `rules/scope.md` | 스코프 규칙 1~4 + 컨셉 + 채널 9 |
| `rules/asset.md` | 에셋·맵 자동 배치 규칙 |
| `rules/session.md` | 시작·종료 프로토콜 |
| `rules/communication.md` | 톤·의사결정·도현 프로필 |
| `hooks/session-start.sh` | 자동 체크리스트 주입 |
| `hooks/session-end.sh` | 자동 session-log stub |
| `settings.json` | hook 등록 (commit됨) |
| `settings.local.json` | 사용자 권한 (gitignored) |

---

## 의존 그래프 핵심 (Phase 1 Before)

```
RunController ──6 FindFirstObjectByType── DeliveryManager / OverviewCamera /
                                          OrderBoard / ResultPanel /
                                          FuelSystem / CargoBox
                ──FindFirstObjectByType── CargoShakeSimulator (BeginRun 안)
                ──UpgradeSystem.Instance── 싱글턴 직접 접근

7 HUD 각자 ──FindFirstObjectByType── 자기 의존 시스템
AudioManager ──Awake에 3 Find── CargoBox / DeliveryManager / UpgradeSystem
```

**총 36 Find 호출 (27 파일)**.

## 의존 그래프 (Phase 1 After 목표)

```
GameContext.Awake ──[SerializeField] 8개 등록── Services.X 정적 레지스트리
모든 코드 ──Services.Delivery / Services.Board / ...── 동일 레지스트리
```

**Find 호출 0**. 와이어링 한 곳 (GameContext).

---

## 다음 세션에서 자주 갈 곳

- 새 결정 기록 → `notes/decisions-log.md`
- 새 아이디어 → `notes/ideas-parking.md`
- 게임플레이 시스템 추가 → `GDD/systems/<name>.md` 작성 후 `F1/Assets/Scripts/<area>/`
- 코드 리팩터 학습 노트 → `GDD/architecture/<phase>-<topic>.md`
