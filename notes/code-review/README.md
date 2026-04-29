# 코드 리뷰 마인드맵

> **사용법:**
> 1. Obsidian 다운로드 ([obsidian.md](https://obsidian.md))
> 2. "Open folder as vault" → `notes/` 폴더 선택
> 3. 좌측 사이드바 **그래프 뷰** 아이콘 클릭 → 전체 시각화
> 4. 아무 노트나 클릭 → `[[wiki-link]]` 따라가며 탐색
> 5. `Ctrl+클릭` 또는 호버 → 미리보기 / 새 패널

---

## 시작점

- 어떻게 코드 전체가 연결되어 있나? → [[scripts/index]]
- 사용자 행동 → 함수 사슬 보고 싶다 → [[flows/index]]
- 특정 함수 깊게 보고 싶다 → [[functions/index]]
- 의문/메모 적기 → [[../code-review-questions]]

---

## 스크립트 6개 (책임별)

| 책임 | 스크립트 |
|---|---|
| 주문 보드 orchestrator | [[OrderBoard]] |
| 주문 카드 prefab 컴포넌트 | [[OrderCardUI]] |
| 포장 선택 모달 orchestrator | [[PreRunSetupPanel]] |
| 포장 카드 prefab 컴포넌트 | [[PackingCardUI]] |
| 런 lifecycle (시간/보상) | [[RunController]] |
| 배달 지점 트래커 | [[DeliveryManager]] |

## 사용자 흐름 5개

| 흐름 | 노트 |
|---|---|
| 게임 시작 → 출발 (정상) | [[Flow.GameStart]] |
| PreRunSetupPanel 취소 | [[Flow.Cancel]] |
| 호버 상태 전파 | [[Flow.HoverPropagation]] |
| 배달 완료 → 정산 | [[Flow.DeliveryComplete]] |
| 시간 초과 (실패) | [[Flow.TimeOver]] |

---

## 그래프 활용 팁

**Obsidian 그래프 뷰 단축키:**
- `Ctrl+G`: 그래프 뷰 토글
- 노드 호버: 연결 강조
- 우측 패널 필터: 폴더별 색깔 분리 (scripts/, functions/, flows/)

**Local Graph (현재 노트 주변만):**
- 노트 열고 우측 패널 → "Local Graph" → 1~2 hop만 보임
- "이 함수와 직접 연결된 것만" 보기에 최적

---

## 형식 약속

**스크립트 노트** (`scripts/*.md`):
- 인스펙터 입력 / public API / 협력 객체 / 관련 흐름

**함수 노트** (`functions/*.md`):
- 호출되는 곳 (incoming) / 호출하는 것 (outgoing) / 무엇을 / 왜
- 모든 함수 노트는 부모 [[스크립트]]를 링크함

**흐름 노트** (`flows/*.md`):
- 사용자 액션 → 함수 사슬 (Mermaid 시퀀스 + 텍스트)
- 거치는 모든 함수에 [[link]]

---

## 빠른 진입 (5분 권장)

1. [[Flow.GameStart]] 한번 읽기 — 가장 친숙한 흐름
2. 거기서 [[OrderBoard.Open]] 클릭 → 함수 깊게
3. 거기서 [[OrderCardUI.Bind]] 클릭 → 카드 컴포넌트 이해
4. 뒤로가기 (`Alt+←`) → Flow.GameStart 복귀
5. 다음 흐름 읽기

5분 후엔 그래프 뷰 한번 띄워보고 시각적으로 전체 파악.
