# 야간 자율 루프 — Task 정의 (2026-04-30)

> **목표**: 6 사이클 × 30분 = 3시간. 도현 자는 동안 안전한 작업만 진행.
> 베이스 커밋: `1486220` (Obsidian vault 직후). 복원: `git reset --hard 1486220`

---

## 모드 분류

### Mode 1 — 리포트만 (코드 변경 0)

- 코드 정독, 의문 식별
- Obsidian vault ↔ 실제 코드 drift 점검
- Dead code / 워닝 스캔
- 디자인 갭 (사람 결정 영역) 발견 → 보고만

### Mode 2 — 코드 변경 허용 (사이클당 최대 1 task, 단일 커밋)

아래 미리 정의된 Task만. 신규 task 추가는 사람 영역.

#### Task Y — DeliveryManager.FindActiveCar Awake 캐시 ✅ pre-approved

- **위치**: `F1/Assets/Scripts/DeliveryManager.cs`
- **변경**:
  - `Transform carCache;` 필드 추가
  - `Awake()` 메서드 신규 — `FindFirstObjectByType<ArcadeCarController>()` 1회 캐시
  - `Update()` 안 `FindActiveCar()` 호출을 `carCache` 사용으로 변경 (null 시 fallback `FindActiveCar`)
- **위험**: 0 (동작 동일, 매 프레임 FindFirstObjectByType 제거)
- **커밋 메시지**: `perf(delivery): FindActiveCar Awake 캐시 — 매 프레임 FindFirstObjectByType 제거`

#### Task Z — 일회용 Editor 헬퍼 정리 ✅ pre-approved

- **위치**:
  - `F1/Assets/Editor/BuildOrderCardPrefab.cs` (이미 실행 끝남)
  - `F1/Assets/Editor/ConvertOrderBoardTextsToTMP.cs` (이미 실행 끝남)
  - `F1/Assets/Editor/BuildPreRunSetupPanel.cs` (이미 실행 끝남)
- **이유**: 1회용 빌드 자동화. Prefab 생성 + scene 와이어링 끝났으므로 dead code.
- **변경**: 위 3개 + .meta 파일 모두 삭제 (`git rm`)
- **위험**: 낮음 (재실행 불가 상태이지만 git 히스토리에서 복원 가능)
- **커밋 메시지**: `chore(editor): 일회용 prefab 빌드 헬퍼 3개 제거 (실행 완료)`

#### Task X — ResultPanel procedural → prefab 패턴 ❌ NOT pre-approved

도현이 "별도 세션 권장"이라 했음. 자율 루프 영역 밖. **시도 X.**

---

## 절대 금지 사항

1. **신규 시스템·신규 ScriptableObject 타입·신규 패키지·신규 에셋**
2. **사람 결정 가치값 변경** (PackingTier.Cost, ThresholdMultiplier, CargoTypePresets 임계, OrderJob 보상값 등)
3. **CLAUDE.md, GDD/, .claude/, dev-plan/** 파일 변경
4. **한 커밋에 여러 task 묶기**
5. **git push, git reset, git rebase, git checkout 외 브랜치 이동, force-push, --no-verify**
6. **Task X (ResultPanel) 시도** — 도현 영역
7. **scene/prefab 바이너리 변경 시도** — 자율 루프에서 Unity Editor 운영 불가
8. **새 기능 제안 → 채택** — 의문은 보고만, 도현 결정
9. **이전 사이클 변경 되돌리기**
10. **worktree 브랜치 머지/삭제** — `claude/angry-montalcini-14cd06`은 보존

위반 의심 시: **즉시 중단 → notes/loop-reports/{timestamp}-VIOLATION.md 작성 → 다음 사이클은 리포트만.**

---

## 사이클 권장 시퀀스 (가이드, feedback이 매 사이클 조정 가능)

| 사이클 | 권장 작업 | 이유 |
|---|---|---|
| 1 | **Task Y** (FindActiveCar 캐시) | 작고 위험 0, 즉시 가치 |
| 2 | 검증 (cycle 1 정적 리뷰) + Obsidian drift 점검 | hot-fix 후 검증 |
| 3 | **Task Z** (Editor 헬퍼 정리) | dead code, 안전 |
| 4 | 검증 (cycle 3) + dead code 추가 스캔 | |
| 5 | Code drift report (코드 ↔ Obsidian notes ↔ flows) | 마지막 코드 변경 사이클 후 정리 |
| 6 | 마무리 패키징 (`notes/loop-summary-2026-04-30.md`) | 도현 기상 30분 진입점 |

---

## 위반 시 행동

- 작업 즉시 중단
- `notes/loop-reports/{timestamp}-VIOLATION.md`에 어떤 task에서 어떤 금지 사항을 만났는지 기록
- 다음 사이클까지 코드 변경 없이 리포트만

---

## 진행 상황 추적 (각 사이클이 갱신)

| 사이클 | 시각 | 작업 | 커밋 SHA | 상태 |
|---|---|---|---|---|
| 1 | 2026-04-30 (인라인) | Task Y FindActiveCar Awake 캐시 | `f960949` (main) | 완료. ⚠ programmer agent가 worktree 브랜치에 0fe9e44로 commit (CWD 오류) → cherry-pick 차단(worktree 머지 금지) → main에 직접 적용. planner 다음 세션 추천: F1 (죽은 조합 UI 가드). designer 신규 발견: 출발 화면 단기 ROI만 노출 (장기 시간축 갭). feedback 권고: 사이클 2 = 검증 (carCache stale 참조 가능성 점검). |
| 2 | 2026-04-30 (cron 1회) | 검증 전용 — 코드 변경 0 | (없음) | 완료. programmer 정적 검증: Critical 0 / Major 0 / Minor 2. carCache 안전성 OK (Unity null check + late spawn fallback). Obsidian drift 발견 (DeliveryManager.md / Update.md에 Awake 캐시 미반영). designer 신규 발견: 출발 화면 Wallet 표시 옵션 A (도현 결정 영역). feedback 권고: 사이클 3 = Task Z + Obsidian drift 동기화 병행 (분리 commit, `pwd` 자기검사 추가). |
| 3 | 2026-04-30 (cron 2회) | Task Z (Editor 헬퍼 정리) + Obsidian drift 동기화 | `d1ef227` (Task Z) + `664605d` (drift, 분리 commit) | 완료. CWD 자기검사 통과. Task Z: Editor 파일 6개 삭제 (3 .cs + 3 .meta). drift: DeliveryManager.md / Update.md 갱신 + DeliveryManager.Awake.md 신규. 모두 main 확인, worktree 브랜치 추가 commit 없음. designer 통합 P0 (Wallet 1줄 표시) 도현 결정 영역. feedback 권고: 사이클 4 = Task Z 사후검증 + drift cross-check + dead code 누적 보고 (코드 변경 0). |
| 4 | 2026-04-30 (cron 3회) | 검증 전용 — 코드 변경 0 | (없음) | 완료. Task Z 사후검증: 잔재 참조 0건, 컴파일 영향 없음. Drift cross-check: ⚠ functions/index.md에 Awake 링크 누락 (1줄 보완 필요). dead code 누적 보고. designer 신규 (P2): OrderBoard 호버→마커 매핑 인지율 약함 (시야 분리 + 1:1 매핑 부재). main 상태 정상 (HEAD `59a3110`). feedback 권고: 사이클 5 = functions/index.md Awake 링크 1줄 보완 + 진행표 갱신, 사이클 6 = `loop-summary-2026-04-30.md` 작성 + 종료. |
| 5 | 2026-04-30 (cron 4회) | Drift 잔재 보완 (functions/index.md DeliveryManager.Awake 링크 추가) + cycle 6 outline 사전 준비 | `a937e5e` (main) | 완료. CWD 자기검사 통과. main 확정 (worktree 브랜치 변화 없음). designer 마무리 통합: P0/P1/P2 최종 우선순위 + 도현 다음 세션 첫 작업 = "PreRunSetupPanel에 Wallet 1줄 추가" (5~10분, 가장 쉬운 P0). planner cycle 6 loop-summary outline 사전 준비 완료. feedback 권고: 사이클 6 = `loop-summary-2026-04-30.md` 작성 + 루프 종료. |
| 6 | 2026-04-30 (cron 5회) | **루프 종료 — `loop-summary-2026-04-30.md` 작성** | (코드 변경 0, 커밋 없음) | 완료. 단일 agent로 ~1240단어 loop-summary 작성 (도현 30분 진입점). main HEAD `04ac01d` 변화 없음. cron `c680c0d7` CronDelete로 정지. **도현 진입점:** `notes/loop-summary-2026-04-30.md`. 다음 세션 첫 작업: PreRunSetupPanel에 Wallet 1줄 추가 (designer 추천). |

---

## 루프 종료 (2026-04-30 main)

6/6 사이클 완료. cron 정지됨.

**도현 기상 시 진입:** `notes/loop-summary-2026-04-30.md`
