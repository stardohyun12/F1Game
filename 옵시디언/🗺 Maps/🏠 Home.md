---
tags: [hub, hub/root]
---

# 🏠 Home

> **이 문서는 "허브"입니다.** 직접 정보가 아니라 다른 노트로 가는 표지판 역할.
> vault 전체의 진입점. 4개 지도(허브)와 빠른 진입 표 + 그래프 보기 모드.

---

## 4개 지도 (Maps of Content)

각 지도도 "허브" — 한 주제의 모든 관련 노트를 한 페이지에 모음. 그래프에서 **빨간 노드**로 보임.

- [[🎮 시스템 지도]] — 시스템(자동차 dial 9개 + 게임 루프) 한 줄 표.
- [[💻 코드 지도]] — 57 스크립트 카테고리별 목록.
- [[📋 결정 지도]] — D-001~D-064 영구 결정 인덱스.
- [[📚 학습 지도]] — Phase 1~6 학습 노트 + 패턴 카탈로그.

---

## 그래프 보기 모드 (분류별 토글)

옵시디언 그래프 view에서 search 박스에 아래 query 복사. 또는 URI 링크 클릭(옵시디언 1.7+):

| 모드 | Search query | 보이는 것 |
|---|---|---|
| 💻 **코드만** | `path:code/` | 시스템 의존만 (default 권장) |
| 🎮 **코드 + 핵심 스펙** | `path:code/ OR path:GDD/systems OR path:GDD/architecture` | 코드+디자인 연결 |
| 📋 **결정 + 학습** | `path:notes/decisions-log OR path:GDD/architecture OR tag:#hub` | 트레이스 |
| 🗂 **운영 흐름** | `path:notes/ OR path:"🗺 Maps"` | 세션·결정 등 운영 |
| 📊 **전체** | (비움) | 모든 것 |

> 옵시디언 graph default는 **`-path:GDD/research -path:notes/session-log -path:_archive`**로 잡파일 자동 제외.
> 보고 싶을 때만 search 박스에서 그 폴더 명시.

---

## 빠른 진입 (가장 자주 갈 곳)

| 목적 | 시작 노트 |
|---|---|
| 게임 컨셉 한 줄 | [[GDD/overview]] |
| 메인 게임 루프 | [[code/Order/RunController]] |
| 핵심 기믹 (화물 시뮬) | [[code/Cargo/CargoBox]] |
| 와이어링 (Service Locator) | [[code/Core/Core]] |
| 결정 채널 9 | [[GDD/decision-channels]] |
| 이번 세션 상태 | [[.claude/activeContext]] |

---

## 새 노트 어디 둘지 (결정 트리)

| 새 만드는 것 | 위치 | 비고 |
|---|---|---|
| 새 C# 스크립트 노트 | `code/<카테고리>/<이름>.md` | 자동 스캐폴딩 (`pwsh scripts/generate-obsidian-notes.ps1`) |
| 새 시스템 디자인 스펙 | `GDD/systems/<이름>.md` | 시뮬레이션 수치·룰 |
| 새 아키텍처 학습 노트 | `GDD/architecture/phaseN-<주제>.md` | Phase 7 이상 |
| 새 결정 D-XXX | `notes/decisions-log.md` 안 헤딩 | 분리 X |
| 세션 로그 한 블록 | `notes/session-log.md` 누적 | 분기 누적되면 `notes/session-log/<YYYY-Q>.md`로 분리 |
| 마케팅·포트폴리오 | `GDD/marketing/<이름>.md` | 폴더 신규 OK. `.obsidian/graph.json`에 색 그룹 한 줄 추가 |
| 리서치 참고 | `GDD/research/<이름>.md` | 기본 그래프에서 제외됨 (회색·희미) |
| 폐기·6개월 안 본 노트 | `_archive/<원래 경로>` | 그래프에서 자동 제외 |

---

## 그래프 색 분류

`.obsidian/graph.json`의 color group:

| 색 | 영역 |
|---|---|
| 🔴 빨강 | `🗺 Maps/` — 허브(지도) |
| 🔵 파랑 | `code/` — 코드 |
| 🟢 초록 | `GDD/systems` — 핵심 시스템 스펙 |
| 🟦 청록 | `GDD/architecture` — 학습 노트 |
| 🟠 주황 | `notes/decisions-log` — 결정 |
| 🟡 노랑 | `notes/` — 운영 로그 |
| ⚫ 회색 | `GDD/research` — 참고용 (배경) |
| 🔘 진회색 | `tag:#archive` — 보관 |

---

## 작성 규칙

- 파일명 = 클래스명 또는 주제명. frontmatter `script:`/`tags:` 필드로 검색 가능.
- wikilink는 `[[stem]]` 사용 (옵시디언이 경로 자동 해석).
- 코드 변경 후 `pwsh scripts/generate-obsidian-notes.ps1` 1회 → `last_synced` 갱신.
- 본문은 손으로. 자동 생성은 frontmatter + 빈 섹션만.
- 허브 노트(`🗺 Maps/`)는 최상단에 한 줄 자기 설명 의무.
