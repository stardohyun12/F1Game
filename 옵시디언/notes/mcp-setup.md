# MCP / 도구 정리 가이드

> **영상 2단계 적용:** 도구가 많을수록 Cowork의 인스트럭션 예산이 설명으로 채워져서 실제 명령 처리 공간이 줄어든다.
> 앤트로픽이 MCP 도구 검색 기능을 실험적으로 낼 정도로 심각한 문제.
> OpenAI 프로덕션은 딱 **Read / Write / Edit / Bash** 네 개만 썼다.

---

## 원칙

1. **지금 쓰는 것만 켜둔다.**
2. **CLI가 있으면 MCP 대신 CLI 사용** (Cowork가 shell 접근 가능하므로 대부분 CLI가 더 가벼움).
3. **지금 안 쓰는 MCP는 끈다.** 필요해지면 그때 켠다.

---

## PIT CREW 1~4주차 동안 **반드시 켜둘 것**

| MCP / 도구 | 용도 |
|---|---|
| Read / Write / Edit | 파일 편집 (기본) |
| Bash | 셸 명령, git, Unity CLI (기본) |
| Grep / Glob | 코드 검색 (기본) |
| cowork | 파일 공유 (`present_files`, `request_cowork_directory`) |
| AskUserQuestion / TaskCreate | 대화·작업 관리 (기본) |

---

## PIT CREW 기간 동안 **꺼둘 것**

| MCP | 이유 |
|---|---|
| `notion-*` | 도현은 Notion을 사용하지 않음 |
| `Claude_in_Chrome__*` | 브라우저 자동화, 1~4주차 작업에서 필요 없음 |
| `blender__*` | Kenny 에셋만 사용 확정 (D-005, D-009), 3D 모델링 불필요 |
| `meshy-ai__*` | 3D 생성 불필요 (Kenny 고정) |
| `calendar (e508...)` | 일정 조회·예약은 개발 세션과 무관 |
| `scheduled-tasks` | 자동화 반복 작업 없음 |
| `mcp-registry`, `plugins` | 설치 탐색용. 필요할 때만 켜기 |

---

## Cowork에서 MCP 끄는 방법

Cowork 앱 설정 → Connectors / MCP → 위 목록의 MCP 비활성화.
(정확한 경로는 Cowork 버전에 따라 다를 수 있음. 설정에서 직접 찾아볼 것.)

---

## 예외: 이런 상황에선 다시 켠다

| 상황 | 일시적으로 켤 MCP |
|---|---|
| Unity/Kenny 에셋 관련 웹 문서 조사 | `WebFetch` (도구는 항상 있음) |
| 새 MCP 찾기 | `mcp-registry`, `plugins` |
| 플레이테스트 피드백 정리 (나중에) | 선호에 따라 Notion 또는 로컬 마크다운 |
| 트레일러 영상 편집 (MVP+) | `Claude_in_Chrome` 혹은 전용 툴 |

---

## 팁: CLI가 MCP를 대체할 수 있는 경우

- **Git 조작:** MCP 대신 `git` CLI (Bash 한 줄)
- **GitHub 이슈/PR:** MCP 대신 `gh` CLI
- **파일 조작:** MCP 대신 Read/Write/Edit + Bash
- **이미지 처리:** MCP 대신 `ffmpeg`, `ImageMagick` CLI (Cowork 샌드박스에 있음)

CLI는 MCP보다 **컨텍스트에 설명이 자동 추가되지 않아 토큰 절약**.
