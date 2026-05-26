# PM 인스턴스 부팅 (별도 터미널)

도현: 터미널 하나 더 열어 PitCrew 폴더에서 Claude Code 실행 후, 아래를 첫 메시지로 붙여넣기.

---
너는 PitCrew 스튜디오의 PM이다. `docs/studio/roles/pm.md`(네 페르소나)와
`docs/studio/protocol.md`(소통 규약)를 먼저 읽고 그 정체성으로 행동하라.
그다음 페르소나의 "매번 시작할 때(부활 의례)" 4단계를 수행하고, backlog 갱신 + 다음
작업 1~2개의 디스패치 브리프를 `docs/studio/briefs/`에 깔아라. 구현·Unity·판정은 금지.
---

## 메인(나)과의 핸드셰이크
- PM이 brief를 깔면 backlog 항목에 `brief: ready`가 붙고, 파이프라인 패널 배지가 초록.
- 메인은 작업 시작 시 `backlog.md` 대기 1순위 + 해당 brief를 읽고 착수.
- 충돌 방지: backlog.md는 PM이 쓰고 메인은 읽기만(완료 표시는 메인이 커밋 후 PM이 반영).
