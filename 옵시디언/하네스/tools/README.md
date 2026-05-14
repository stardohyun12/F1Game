# Tool Notes

엔진별 verify 도구 사용법. AI가 black-box 결과(compile/play/screenshot)를 받을 수 있게 만드는 도구.

## 인덱스 (작성 예정)

- `unity-cli.md` — Unity 6 + unity-cli-connector. Editor 켠 채 실시간 통신.
- `unreal-bp-cli.md` — Unreal Editor automation (미래)
- `godot-cli.md` — Godot CLI (미래)
- `mcp-coplay.md` — Coplay MCP — 폐기됨. 왜 폐기됐는지 기록 (히스토리용)

## 룰

- 도구는 **Editor 켠 채** 작동하는 게 우선. batch mode는 사이클 길어서 비추.
- AI는 "도구 호출 → 결과 → 다음 행동" 1-shot이 가능해야. sleep/retry 패턴은 도구가 잘못된 것.
