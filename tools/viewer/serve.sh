#!/usr/bin/env bash
# 건물 뷰어 서버 — 브라우저에서 http://127.0.0.1:8770/ 열기 (Ctrl+C 로 끄기). 정적 파일 + 코멘트 API(/api/feedback)
python "$(cd "$(dirname "$0")" && pwd -W 2>/dev/null || pwd)/serve.py" "$@"
