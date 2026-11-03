# 구현 (build) — 카디널 D-281

- **브리프만 읽어라.** 과제 글 = 브리프 한 장(과제 + world/SPEC.md 발췌 + 세계 스냅숏 + 파일 지도 + 에디터 치트). 다른 규칙 문서는 열지 마라 — 훅(.claude/hooks/guard-pre.py)이 막는다. 규칙은 글이 아니라 훅이 강제한다.
- 프로젝트 D:/Github/F1Game (Unity 6000.3, C#). 한 번에 과제 1개, 끝까지. 아트·리서치는 과제 안 한 줄로만.
- 씬을 직접 찔러 고치지 않는다 — 명세(world/SPEC.md)에서 생성기(tools/world/)·코드로 반영한다. 좌표는 생성기가 정한다.
- 끝 조건 = CARDINAL §4: 컴파일 0 에러 · 불변 검사(`bash scripts/world_check.sh`) 통과 · 규칙 테스트 통과 · 네가 찍은 그림 3장(1×, docs/qa/) · 보고 4줄(한 것 / 확인 / 증거 / 남은 것). 실행 안 한 건 안 했다고 쓴다.
- 에디터는 과제에 `[EDITOR]` 가 있을 때만(`bash scripts/ucli-http.sh exec|editor play --wait|editor stop`). Play 중 .cs 수정 금지, 끝에 `editor stop`·원래 씬·저장 안 된 변경 0.
- 도현이 정한 값(해상도 360·Nearest, 건물 위치, 차 조작값)·공유 .mat·ProjectSettings 는 바꾸지 않는다 — 필요하면 멈추고 보고.
- 버그 1개 고치면 같은 버그를 잡을 불변 조건 1개(`WorldInvariants.cs`) 추가.
- git commit/push 금지(run_task.sh 가 한다). 한국어, 짧고 직설.
