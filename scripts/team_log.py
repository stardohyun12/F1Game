#!/usr/bin/env python
"""팀원 실행 스트림(JSON lines)을 사람이 읽는 실시간 로그로 바꾼다.

사용: <claude --output-format stream-json --verbose | codex exec --json> | python scripts/team_log.py <role> <live.log> <result.json>
- live.log: "[HH:MM:SS] 역할 | 내용" 한 줄씩 (tail -f 로 본다)
- result.json: 마지막 결과 요약(성공 여부·비용·최종 답·세션 id)
"""
import json
import sys
import time

role, live_path, result_path = sys.argv[1], sys.argv[2], sys.argv[3]
live = open(live_path, "a", encoding="utf-8", buffering=1)
result = {"role": role, "final": "", "session_id": None, "cost_usd": None, "ok": None}


def out(msg: str) -> None:
    msg = " ".join(str(msg).split())
    if len(msg) > 220:
        msg = msg[:217] + "..."
    live.write(f"[{time.strftime('%H:%M:%S')}] {role:<10} | {msg}\n")


def tool_brief(name: str, inp: dict) -> str:
    if not isinstance(inp, dict):
        return name
    for key in ("file_path", "path", "pattern", "command", "description", "query"):
        if key in inp and inp[key]:
            return f"{name}: {inp[key]}"
    return name


for raw in sys.stdin:
    raw = raw.strip()
    if not raw:
        continue
    try:
        ev = json.loads(raw)
    except Exception:
        out(raw)
        continue
    t = ev.get("type")
    # ---- Claude Code stream-json ----
    if t == "system" and ev.get("subtype") == "init":
        result["session_id"] = ev.get("session_id")
        out(f"시작 (model={ev.get('model')})")
    elif t == "assistant":
        for c in ev.get("message", {}).get("content", []):
            if c.get("type") == "text" and c.get("text", "").strip():
                out("💬 " + c["text"])
            elif c.get("type") == "tool_use":
                out("🔧 " + tool_brief(c.get("name", "?"), c.get("input", {})))
    elif t == "result":
        result.update(ok=ev.get("subtype") == "success" and not ev.get("is_error"), final=ev.get("result", ""),
                      cost_usd=ev.get("total_cost_usd"), session_id=ev.get("session_id") or result["session_id"])
        out(f"끝: {'❌ 오류' if ev.get('is_error') else ev.get('subtype')} · {ev.get('num_turns')}턴 · ${ev.get('total_cost_usd') or 0:.2f}")
    # ---- Codex exec --json ----
    elif t == "thread.started":
        result["session_id"] = ev.get("thread_id")
        out("시작 (codex)")
    elif t in ("item.started", "item.completed"):
        it = ev.get("item", {})
        kind = it.get("type")
        if kind == "agent_message" and t == "item.completed":
            result["final"] = it.get("text", "")
            out("💬 " + it.get("text", ""))
        elif kind == "command_execution" and t == "item.started":
            out("🔧 " + str(it.get("command", "")))
        elif kind == "file_change" and t == "item.completed":
            out("✏️ " + ", ".join(c.get("path", "?") for c in it.get("changes", [])))
        elif kind == "reasoning" and t == "item.completed" and it.get("text"):
            out("🧠 " + it["text"])
    elif t == "turn.completed":
        u = ev.get("usage", {})
        result["ok"] = True
        out(f"끝: 입력 {u.get('input_tokens')} / 출력 {u.get('output_tokens')} 토큰")
    elif t in ("turn.failed", "error"):
        result["ok"] = False
        out("❌ " + json.dumps(ev, ensure_ascii=False))

json.dump(result, open(result_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
