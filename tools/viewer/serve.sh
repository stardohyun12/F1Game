#!/usr/bin/env bash
# 건물 뷰어 서버 — 브라우저에서 http://127.0.0.1:8770/ 열기 (Ctrl+C 로 끄기)
python -m http.server 8770 --bind 127.0.0.1 --directory "D:/Github/F1Game/tools/viewer"
