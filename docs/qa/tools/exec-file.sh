#!/usr/bin/env bash
set -eu
# Run a reviewable C# diagnostic through the existing authorized Unity connector.
bash scripts/ucli.sh exec "$(cat "$1")"
