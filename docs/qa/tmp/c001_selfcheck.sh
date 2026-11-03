#!/usr/bin/env bash
# C-001 자체 확인: 컴파일 → Play → 장소 9 → start.cs → 클립(bundle.sh 와 같은 clip.cs.tmpl, 출력은 docs/qa/tmp/c001_clip) → strip
cd "$(dirname "$0")/../../.." || exit 1
U="bash scripts/ucli-http.sh"
$U editor stop >/dev/null 2>&1
$U exec 'UnityEditor.AssetDatabase.Refresh(); return "refresh";' | tail -1
for i in $(seq 1 40); do
  r=$($U exec 'return UnityEditor.EditorApplication.isCompiling ? "busy" : "idle";' 2>/dev/null | tail -1)
  [ "$r" = "idle" ] && break; sleep 2
done
echo "compile: $r"
$U console --type error 2>&1 | tail -2
$U editor play --wait >/dev/null || { echo "FAIL play"; exit 1; }
$U exec 'Application.runInBackground = true; return "rib";' >/dev/null
sleep 3
echo "place: $($U exec 'return DebugWindow.GoPlace(9);' | tail -1)"
sleep 2
echo "start.cs: $($U exec @docs/team/cards/C-001-repair-one/start.cs | tail -1)"
OUT=docs/qa/tmp/c001_clip; rm -rf "$OUT"; mkdir -p "$OUT/frames"
sed -e "s#__DIR__#$OUT/frames#" -e "s#__SECS__#10#" -e "s#__FPS__#12#" tools/evidence/clip.cs.tmpl > "$OUT/clip.cs"
echo "clip: $($U exec @"$OUT/clip.cs" | tail -1)"
META=""
for i in $(seq 1 70); do
  sleep 1; META=$($U exec 'return UnityEditor.SessionState.GetString("ev.done","");' 2>/dev/null | tail -1)
  case "$META" in \{*) break;; esac
done
echo "meta: $META"
echo "E: $($U exec 'return UnityEditor.SessionState.GetString("c001.e","");' | tail -1)"
$U console --type error 2>&1 | tail -2
$U editor stop >/dev/null 2>&1
FF=$(command -v ffmpeg || ls /c/Users/*/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg*/ffmpeg-*/bin/ffmpeg.exe 2>/dev/null | head -1)
N=$(ls "$OUT/frames"/f*.png | wc -l); K=$(( (N + 7) / 8 ))
"$FF" -v error -y -framerate 12 -i "$OUT/frames/f%03d.png" -c:v libx264 -pix_fmt yuv420p -crf 26 "$OUT/clip.mp4"
"$FF" -v error -y -i "$OUT/clip.mp4" -vf "select='not(mod(n\,$K))',scale=480:-1,tile=4x2" -fps_mode vfr -frames:v 1 "$OUT/strip.png"
echo "frames=$N K=$K"
git checkout -- F1/Assets/UI/Fonts 2>/dev/null
