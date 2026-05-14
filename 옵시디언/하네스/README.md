# AI 게임개발 하네스 노트

> AI(Claude Code)로 게임 개발하면서 발견한 **약점 패턴**과 **회피책** 모음.
> 특정 프로젝트가 아니라 **게임 개발 일반**에 적용 가능한 범용 노트.

## 디렉토리

- `patterns/` — AI가 게임 개발에서 못하는 것 1개당 파일 1개. 증상·원인·회피책.
- `process/` — 워크플로 (언제 plan mode, 언제 brainstorm, 검증 사이클).
- `tools/` — 엔진별 verify 도구 (unity-cli, ue-bp-cli 등) 사용법.
- `templates/` — 새 프로젝트 부트스트랩용 (`CLAUDE.md` base, rules base).

## 패턴 인덱스

| # | 패턴 | 한 줄 |
|---|---|---|
| 01 | [Visual Blindness](patterns/01-visual-blindness.md) | AI는 화면 결과를 못 봐서 시각 버그 추측만 함 |
| 02 | [Scene/Inspector Wiring](patterns/02-scene-inspector-wiring.md) | 씬 YAML에 있는 인스펙터 의존을 못 읽음 |
| 03 | [Asset Opacity](patterns/03-asset-opacity.md) | 메쉬·머티리얼·텍스처·사운드 binary는 이름만 보고 추측 |
| 04 | [Physics Tuning Feel](patterns/04-physics-tuning-feel.md) | 숫자 보고 "적정" 판단 X. 실제 손맛 모름 |
| 05 | [Fun Judgment](patterns/05-fun-judgment.md) | 메커니즘은 짜도 재미·게임필 평가 못 함 |
| 06 | [Multi-Instance Edges](patterns/06-multi-instance-edges.md) | 단일 케이스만 짜고 N 인스턴스 엣지 흘림 |
| 07 | [System Boundary Handoff](patterns/07-system-boundary-handoff.md) | 시스템 경계에서 데이터 누락·생명주기 어긋남 |
| 08 | [Perf Budget Blindness](patterns/08-perf-budget.md) | profiler 없이는 누수·hot path 못 잡음 |
| 09 | [Input Feel](patterns/09-input-feel.md) | 입력 반응성·동시키·우선순위 손맛 추측만 |
| 10 | [Default to Add](patterns/10-default-to-add.md) | "정리해"라 해도 기본값으로 기능 추가를 고름 |

## 노트 1개 형식

```markdown
# [약점 이름]

## 증상
[한 줄]

## 왜 발생 (AI 한계)
[한 줄]

## 회피책
- 즉시: ...
- 워크플로: ...
- 도구: ...

## 적용 신호
이 상황이면 의심:
- ...
```

## 운영 룰

- 세션 끝마다 "오늘 새로 배운 거 1개" 추가. 없으면 PASS.
- patterns 노트는 **실제 incident 발생했을 때만** 깊이 채움. 추측으로 채우지 말 것.
- 분기마다 Notion에 상위 카테고리 요약 → 신청서·연구보고서로.
