#!/usr/bin/env python3
"""guard-post.py — PostToolUse 훅(Edit|Write|MultiEdit, D-276): F1/Assets/**/*.cs 가벼운 정적 검사. 막지 않고 경고만
(stdout JSON additionalContext — 평문 stdout 은 모델에게 안 보임). 중괄호 짝 · using UnityEngine 누락."""
import sys, os, re, json

UE_TYPES = re.compile(r"\b(MonoBehaviour|GameObject|Transform|Vector[23]|Quaternion|Rigidbody|Debug\.|Mathf\.|Time\.|Camera|Material|Color)\b")


def main():
    try:
        d = json.load(sys.stdin); fp = (d.get("tool_input") or {}).get("file_path", "").replace("\\", "/")
    except Exception:
        return
    if not re.search(r"/F1/Assets/.*\.cs$", fp) or not os.path.isfile(fp):
        return
    src = open(fp, encoding="utf-8", errors="ignore").read()
    code = re.sub(r"//[^\n]*|/\*.*?\*/|\"(?:\\.|[^\"\\])*\"|'(?:\\.|[^'\\])*'", "", src, flags=re.S)  # 주석·문자열 제거(// 는 줄 끝까지만)
    warn = []
    if code.count("{") != code.count("}"):
        warn.append(f"중괄호 짝 안 맞음 {{={code.count('{')} }}={code.count('}')} — 컴파일 전에 확인")
    if UE_TYPES.search(code) and "using UnityEngine;" not in code and "UnityEngine." not in code:
        warn.append("UnityEngine 타입을 쓰는데 `using UnityEngine;` 없음")
    if warn:
        print(json.dumps({"hookSpecificOutput": {"hookEventName": "PostToolUse",
              "additionalContext": f"[guard-post] {os.path.basename(fp)}: " + " / ".join(warn)}}, ensure_ascii=False))


if __name__ == "__main__":
    main()
