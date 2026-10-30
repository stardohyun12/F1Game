#!/usr/bin/env bash
# field_patch-only variant of studio_cap.sh; same Play/Render/Present/Stop flow.
set -eu
export UCLI_PROJ=D:/Github/F1Game/F1Studio
python -X utf8 tools/art/bin/field_exec.py tools/art/bin/field_play.cs
trap 'python -X utf8 tools/art/bin/field_exec.py tools/art/bin/field_stop.cs' EXIT
sleep 3
python -X utf8 tools/art/bin/field_exec.py tools/art/bin/field_capture.cs art/_review/2026-10-29-field_patch/capture_log.json
