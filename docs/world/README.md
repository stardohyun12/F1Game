# docs/world — 세계 모델 1층 (D-275)

**무엇**: `WORLD.md` = 과제 시작 직전 에디터에서 읽은「지금 세계 상태」(장소·배달 지점 DP_*·[E] 물건·흐름 enum·대역 수). `INVARIANTS.log` = 과제 끝(커밋 전) 불변 조건 검사 기록. 둘 다 자동 생성 — 손으로 고치지 않는다.
**왜**: 팀원이 코드만 보고 얕게 고쳐 세계와 어긋나는 버그(부두 끝 추락·보관소 출구 막힘·대역 상자 잔류)가 났다. 진실이 씬(바이너리)·스크립트·yaml·문서에 흩어져 있어, 시작 때 상태를 첨부하고 끝에 기계가 검사한다.
**어떻게 도나**: `scripts/team_queue.sh` build 줄이 과제 앞에 `scripts/world_snapshot.sh`(→ `F1/Assets/Scripts/Debug/WorldSnapshot.Dump()`), 과제 끝에 `scripts/world_check.sh`(→ `WorldInvariants.Check()`)를 돈다. 위반(첫 줄 `FAIL k/n`)이면 커밋 없이 `queue/editor/hold/` + `queue.log`「불변 위반 <과제> <조건들>」. 에디터를 못 닿으면(exit 2) 넘어간다.
**손으로**: `bash scripts/world_snapshot.sh` / `bash scripts/world_check.sh [이름]` (에디트 모드, 본 에디터가 비어 있을 때 — `.claude/team/editor.lock` 확인).
**조건 늘리기**: `WorldInvariants.cs` 에 `static List<string> 이름()` 함수(위반 줄 목록, 빈 목록 = 통과; 줄엔 대상·좌표·고치는 힌트)를 쓰고 `Checks` 배열에 `("표시 이름", () => 이름())` 한 줄. 씬을 읽기만 하고 Play·저장·컴포넌트 변경 금지(RegressSweep 와 같은 원칙).
**아직 확인 못 한 조건**: `Soft` 집합(지금 「보관소 출구」)은 위반해도 WARN 으로만 적는다 — 실제 씬에서 한 번 OK 를 보면 `Soft` 에서 뺀다. 의도된 대역을 켜 둬야 하면 `PlaceholderAllow` 에 이름을.
**조건 목록(10)**: DP 땅 2m 안(NavMesh 없으면 레이 5m) · 장소 땅 5m 안(+표 y 와 2.5m 이상 차) · 부두 난간(WalkOnlyRail 콜라이더 + 낚시 자리/항구 앞 8m) · 칠판 1개 · 손 전표 ≤1 · 대역 켜짐 0 · 카메라 ≥1 · DP DeliveryPoint·SphereCollider · 보관소 출구(Soft) · 디렉터 1개. 콘솔 에러 0 은 `tools/evidence/bundle.sh` 몫.
