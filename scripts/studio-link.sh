#!/usr/bin/env bash
# F1Studio(건물 스튜디오 전용 Unity 프로젝트) 를 git 체크아웃에서 다시 만든다 — 정션(공유) + 복사(환경).
#   사용: bash scripts/studio-link.sh            # 만들기/갱신 (멱등)
#         bash scripts/studio-link.sh --check    # 상태만 출력
# 공유(정션, cmd mklink /J — 실파일은 F1 쪽 하나뿐, 두 에디터가 같은 폴더를 본다):
#   F1Studio/Assets/Art/Library  -> F1/Assets/Art/Library   (건물 프리팹·재질·텍스처·fbx, WallClock.cs, A7LitPoint_garage.shader)
#   F1Studio/Assets/Art/SceneA7  -> F1/Assets/Art/SceneA7   (SceneA7Lit.shader, SA7_* 재질 템플릿, A7_RPAsset)
# 복사(.meta 포함 → GUID 동일; 스튜디오 쪽이 바뀌어도 메인에 안 번진다):
#   Assets/Settings/ (URP 파이프라인·렌더러·볼륨), Studio_Building 씬이 쓰는 SceneA2/SceneA3 텍스처·셰이더 몇 개,
#   ProjectSettings/ (Graphics/Quality/URP 등 — 처음 만들 때만; 이미 있으면 건드리지 않음)
# git 에 들어가는 것: F1Studio/Assets/Scenes, Assets/Scripts, Packages/manifest.json, ProjectSettings, README.md (.gitignore 참조).
set -eu
cd "$(dirname "$0")/.."
ROOT_W=$(pwd -W 2>/dev/null || pwd)
S="F1Studio"; M="F1"
CHECK=0; [ "${1:-}" = "--check" ] && CHECK=1

JUNCTIONS=( "Assets/Art/Library" "Assets/Art/SceneA7" )
# 씬 Studio_Building_Mats 재질이 참조하는 메인 쪽 파일(정션 폴더 밖) — 경로 그대로 복사(.meta 포함)
COPY_FILES=(
  "Assets/Art/SceneA2/Gen/sky_a2d.png" "Assets/Art/SceneA2/Gen/yard_ground_soft.png"
  "Assets/Art/SceneA2/GenF/Gen/road_a2f.png" "Assets/Art/SceneA2/MatsD/d_sky.asset"
  "Assets/Art/SceneA3/Gen/road_r2.png" "Assets/Art/SceneA3/Gen/sky_r2.png" "Assets/Art/SceneA3/SceneA3Look.shader"
)
COPY_DIRS=( "Assets/Settings" )

win() { echo "$ROOT_W/$1" | sed 's#/#\\#g'; }
is_junction() { python -c "import os,sys; sys.exit(0 if os.lstat(sys.argv[1]).st_file_attributes & 0x400 else 1)" "$ROOT_W/$1" 2>/dev/null; }  # FILE_ATTRIBUTE_REPARSE_POINT

status=0
for j in "${JUNCTIONS[@]}"; do
  src="$M/$j"; dst="$S/$j"
  if is_junction "$dst"; then echo "ok   junction $dst"; continue; fi
  if [ -e "$dst" ]; then echo "FAIL $dst 가 있는데 정션이 아님 — 지우고 다시"; status=1; continue; fi
  [ $CHECK = 1 ] && { echo "MISSING junction $dst"; status=1; continue; }
  mkdir -p "$(dirname "$dst")"
  cmd /c "mklink /J $(win "$dst") $(win "$src")" >/dev/null && echo "made junction $dst -> $src"   # git-bash: cmd //c 는 명령을 안 받는다 — /c 그대로
done
# 폴더 .meta(Art/, Art/Library.meta, Art/SceneA7.meta …)는 메인 것을 복사해 GUID 를 맞춘다
for mf in "Assets/Art.meta" "Assets/Art/Library.meta" "Assets/Art/SceneA7.meta" "Assets/Art/SceneA2.meta" "Assets/Art/SceneA2/Gen.meta" "Assets/Art/SceneA2/GenF.meta" "Assets/Art/SceneA2/GenF/Gen.meta" "Assets/Art/SceneA2/MatsD.meta" "Assets/Art/SceneA3.meta" "Assets/Art/SceneA3/Gen.meta" "Assets/Settings.meta" "Assets/Scenes.meta" "Assets/Scripts.meta" "Assets/Scripts/Visual.meta" "Assets/Scripts/Debug.meta"; do
  [ -f "$M/$mf" ] || continue
  if [ ! -f "$S/$mf" ]; then [ $CHECK = 1 ] && { echo "MISSING $S/$mf"; status=1; continue; }; mkdir -p "$(dirname "$S/$mf")"; cp "$M/$mf" "$S/$mf"; echo "copied $mf"; fi
done
for f in "${COPY_FILES[@]}"; do
  if [ ! -f "$S/$f" ]; then [ $CHECK = 1 ] && { echo "MISSING $S/$f"; status=1; continue; }; mkdir -p "$(dirname "$S/$f")"; cp "$M/$f" "$S/$f"; cp "$M/$f.meta" "$S/$f.meta"; echo "copied $f"; fi
done
for d in "${COPY_DIRS[@]}"; do
  if [ ! -d "$S/$d" ]; then [ $CHECK = 1 ] && { echo "MISSING $S/$d"; status=1; continue; }; mkdir -p "$S/$d"; cp -r "$M/$d/." "$S/$d/"; echo "copied dir $d"; fi
done
if [ ! -d "$S/ProjectSettings" ]; then
  [ $CHECK = 1 ] && { echo "MISSING $S/ProjectSettings"; status=1; } || { cp -r "$M/ProjectSettings" "$S/ProjectSettings"; sed -i 's/^  productName: F1$/  productName: F1Studio/' "$S/ProjectSettings/ProjectSettings.asset"; echo "copied ProjectSettings"; }
fi
[ -f "$S/Packages/manifest.json" ] || { echo "MISSING $S/Packages/manifest.json (git 에 있어야 함)"; status=1; }
[ -f "$S/Assets/Scenes/Studio_Building.unity" ] || { echo "MISSING $S/Assets/Scenes/Studio_Building.unity (git 에 있어야 함)"; status=1; }
[ $status = 0 ] && echo "studio-link: OK ($S)" || echo "studio-link: 문제 있음"
exit $status
