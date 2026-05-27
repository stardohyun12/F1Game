# MANUS 협업 규칙 — PitCrew (F1Game)

Manus ↔ GitHub ↔ Claude Code 사이에서 산출물이 안 섞이게 하는 규칙.
GitHub: https://github.com/stardohyun12/F1Game

## 어디에 무엇을 둘까

| 산출물 | 위치 |
|---|---|
| 기획·설계 문서 | `docs/` |
| 컨셉아트 | `art/concept/` |
| UI/HUD 아트 | `art/ui/` |
| 스프라이트/에셋 | `art/sprites/` |
| 사용한 아트 프롬프트 기록 | `art/_prompts/` |
| 레퍼런스 자료 | `references/` (기존) |

> Unity 프로젝트(`F1/`)·코드·`.sln`은 로컬/Claude Code 담당. Manus는 문서·아트 폴더만.

## 작업 흐름

1. **Manus**: 폴더 규칙대로 산출물 생성 → 브랜치 `manus/<주제>` 로 커밋 → PR
2. **Claude Code (로컬)**: `git pull` 후 검토·반영
3. 6/24 스팀 데모 목표 — 데모 직접 관련 산출물 우선

## 금지

- 다른 프로젝트 파일을 이 repo에 넣지 말 것 (repo는 프로젝트별 분리).
- Unity가 생성하는 `Library/`, `Temp/` 등 로컬 산출물 커밋 금지 (`.gitignore` 확인).
