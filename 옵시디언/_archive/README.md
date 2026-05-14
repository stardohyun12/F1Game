---
tags: [archive]
---

# _archive

> 더 이상 적극적으로 참조하지 않는 노트의 보관소. **그래프에서 자동 제외** (`.obsidian/graph.json`의 default search에 `-path:_archive`).

## 보관 기준

- 6개월 이상 안 본 노트.
- 폐기된 아이디어 / 철회된 결정 / 끝난 research.
- 옛 세션 로그 (분기 단위 파일을 통째로 옮김).

## 사용법

- 보관할 노트의 frontmatter에 `tags: [archive]` 추가 후 이 폴더로 이동.
- 원래 폴더 경로를 그대로 미러링하면 wikilink가 깨지지 않음. 예: `code/Cargo/OldThing.md` → `_archive/code/Cargo/OldThing.md`.
- 옵시디언 `alwaysUpdateLinks: true`라 vault 안에서 이동 시 자동 갱신.

## 복구

다시 활성화하려면 frontmatter에서 `archive` 태그만 제거하고 원래 위치로 이동.
