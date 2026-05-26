# PM 인스턴스 부팅 (별도 터미널)

## 권장: 원커맨드 런처 (복붙 0)
네가 연 터미널(PitCrew 폴더)에서:

```powershell
.\pm.ps1
```

이거면 끝. 페르소나가 자동 로드된 Claude가 인터랙티브로 뜨고, `시작` 한 단어만 보내면
PM이 부활 의례 → backlog 그루밍 → 브리프 작성을 한다. (메인=나는 다른 터미널 인스턴스를
직접 조종 못 하므로, 너의 수작업을 이 1번으로 압축한 것.)

## 폴백: 수동 (런처 못 쓸 때만)
터미널에서 `claude` 실행 후 아래를 첫 메시지로 붙여넣기.

---
너는 PitCrew 스튜디오의 PM이다. `docs/studio/roles/_standard.md`(전문가 기준),
`docs/studio/roles/pm.md`(페르소나), `docs/studio/protocol.md`(소통 규약)를 먼저 읽고
그 정체성으로 행동하라. 그다음 "부활 의례"를 수행하고, backlog 갱신 + 다음 작업 1~2개의
디스패치 브리프를 `docs/studio/briefs/`에 깔아라. 구현·Unity·판정·commit 금지.
---

## 메인(나)과의 핸드셰이크
- PM이 brief를 깔면 backlog 항목에 `brief: ready`가 붙고, 파이프라인 패널 배지가 초록.
- 메인은 작업 시작 시 `backlog.md` 대기 1순위 + 해당 brief를 읽고 착수.
- 충돌 방지: backlog.md는 PM이 쓰고 메인은 읽기만(완료 표시는 메인이 커밋 후 PM이 반영).
