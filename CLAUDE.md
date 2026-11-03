# CLAUDE.md — F1 게임 프로젝트

> `harness-core/CLAUDE_TEMPLATE.md` v0.1 기반.
> `[project]` 부분은 이 프로젝트에 맞춰 채움.
> `[universal]` 부분은 템플릿 그대로 유지.

---

## 0. 이 프로젝트 정보 [project]

- **프로젝트 이름**: F1 게임 (가칭)
- **장르 / 도메인**: F1 레이싱 시뮬레이션
- **Unity 버전**: 6000.3.9f1
- **GitHub repo**: https://github.com/stardohyun12/F1Game.git
- **시작일**: 2026-05-14
- **목표**: 모두창업 산출물 (A축) + 하네스 검증 사례 (B축)

---

## 1. 두 축

### A축: F1 게임 자체
- 자체 그래픽 스타일 (기존 F1 게임 모방 아님)
- 자체 UI 구성
- 실제 플레이 가능 목표

### B축: 하네스 검증
- `harness-core`의 첫 번째 적용 사례
- 작업 중 발견된 빈틈은 `harness-core/REJECTED_IDEAS.md` 또는 새 룰로 반영
- **F1 콘텐츠는 하네스에 박지 않음**

---

## 2~5번은 템플릿과 동일

`harness-core/CLAUDE_TEMPLATE.md` 2~5번 참조. 변경 없음.

요점만:
- 태그 시스템: `[universal]` / `[project]`, AI 제안 + 도현 확정
- 룰셋 v0.1: #1 시각 컨텍스트, #2 참조 필수, #3 검증 후 푸시, #4 작업 단위 커밋, #5 백로그 큐잉, #6 컨텍스트 분할, #7 태그 제안
- 관찰자 시스템: `harness-core/observer/prompt.md`

---

## 6. 운영 환경

### 인프라 [project]
- **본가 데탑 24시간 가동** — 메인 작업 머신
- **Tailscale** — 비상시 원격 접속용
- **메인 컴퓨터** — 도현이 외부에서 git으로 진행 관찰
- 하이브리드: 평소엔 git 확인, 막힘 감지되거나 도현 개입 필요 시 Tailscale

### 작업 흐름
```
본가 데탑:
  Claude Code (이 가이드) → unity-cli → Unity 에디터
            ↓
       git commit + push (작업 단위)
            ↓
도현 (외부):
  GitHub diff + 스크린샷
  관찰자 보고 별도
```

---

## 7. 폴더 구조

```
f1-game/
├── CLAUDE.md                    ← 이 파일
├── backlog.md                   ← AI가 관리하는 작업 큐
├── session_handoff.md           ← 세션 인계 (있을 때만)
├── Assets/                      ← Unity 프로젝트
├── references/
│   ├── external/                ← F1 23, F1 Manager 등 참조 [project]
│   └── internal/                ← 이 게임 자체 스크린샷 [project]
│       ├── good/                ← 도현이 마음에 든 결과
│       └── bad/                 ← 피해야 할 패턴
├── project_rules/               ← F1 한정 룰 [project]
│   ├── REJECTED_IDEAS.md
│   └── ...
└── .ai_logs/                    ← 작업 캡처/로그
```

별도 위치:
```
harness-core/                    ← 별도 repo, F1 외 프로젝트와 공유
```

---

## 8. 살아있는 문서

- 작업 중 발견 패턴 → 룰 추가
- `[universal]` 룰은 `harness-core` repo로 PR
- `[project]` 룰은 이 repo `project_rules/` 에
- 수정 권한: 도현

---

## 9. 거절된 접근

- **하네스 공통**: `harness-core/REJECTED_IDEAS.md` 참조
- **F1 한정**: `project_rules/REJECTED_IDEAS.md` 참조

---

## 10. 규칙 모듈 [project] — 읽는 사람에 따라 다르다 (D-277, 2026-11-03)

**팀원(역할 세션 — `scripts/team.sh` 가 띄우고 `TEAM_ROLE` 환경변수가 있음)은 브리프 한 장만 읽는다.** 과제 글(`scripts/brief.py` 출력 = 과제 + 카드 + 필수 규칙 + 세계 스냅숏 + 파일 지도 + 에디터 치트)이 시작 글의 전부이고, 아래 문서는 열지 않는다 — 규칙은 글이 아니라 훅(`.claude/hooks/guard-pre.py`)이 강제하며, 아래 문서 읽기도 훅이 막는다. 기준 한 장은 `docs/team/WORKFLOW-v2.md`(PD 용).

**도현이 직접 여는 세션(TEAM_ROLE 없음)**은 필요한 것을 연다(자동 로드 `@` 는 팀원 세션에도 같이 먹어 7만 자를 매 과제 읽게 하므로 뗐다 — 목록으로만):

- `PIPELINE.md` — 팀 운영 규약
- `project_rules/scope.md` · `asset.md` · `session.md` · `communication.md` · `no-compromise.md` · `REJECTED_IDEAS.md` · `roles.md`
- @.claude/activeContext.md

universal 룰 (별도 repo `harness-core` — 텍스트 참조, 자동 로드 아님):

- `harness-core/rules/universal/ai-blindspots.md` — AI 약점 회피 10개
- `harness-core/rules/universal/working-norms.md` — 의사결정·안전 규범

## 11. AionUi 팀 모드 [project] (2026-10-18)

이 저장소는 AionUi 팀(총괄 PD · 기획 · 아트 · 프로그래밍 · QA)으로도 작업한다. 팀 운영 규약은 `PIPELINE.md`. **단 team.sh 가 띄운 팀원(역할 세션)은 §10 대로 브리프만.** Codex 팀원은 `AGENTS.md`를 읽는다. 혼자 세션으로 일할 때도 PIPELINE §2 산출물 위치, §4 에디터 토큰 개념, §5 모델링 규격은 그대로 적용한다.

---

버전: v0.3 (2026-11-03, D-277 §10 자동 로드 해제 — 팀원은 브리프만) · v0.2 (2026-10-18, AionUi 팀 모드 §11 추가) · v0.1 (2026-05-14)
