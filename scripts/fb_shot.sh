#!/usr/bin/env bash
# D-204 ① 도현 코멘트를 "그때 그 카메라"로 다시 찍는다 — 고친 뒤(after) 스샷.
#   bash scripts/fb_shot.sh <id>        → tools/viewer/feedback/shots/<id>_after.png
# id 는 앞부분만 줘도 된다(예: 034450-9a2a). jsonl 의 camera {pos,target,fov} 로 임시 카메라를 만들고,
# 원본 캡처(PlanIO.RenderPng)와 같은 방식 = 씬 뷰 카메라를 CopyFrom(같은 URP 렌더러·A7 빛·Kuwahara)한 뒤
# 위치·방향·fov 만 덮어 원본 스샷과 같은 크기로 렌더. 선(strokes)은 안 그린다(가리면 판정이 안 된다).
# 씬: Studio_Building → F1Studio 에디터, 그 외 → 본 F1 (UCLI_PROJ 자동). 열린 씬이 다르면 실패(씬을 바꾸지 않는다).
# Play 중이면 실패(Play 화면은 게임 카메라라 구도가 다르다).
set -u
ID="${1:?id 필요}"
ROOT="$(cd "$(dirname "$0")/.." && (pwd -W 2>/dev/null || pwd))"   # Windows python 이 읽게 D:/... 꼴
SPEC=$(python -X utf8 - "$ROOT" "$ID" <<'EOF'
import glob, json, os, sys
root, q = sys.argv[1], sys.argv[2]
hits = []
for f in sorted(glob.glob(os.path.join(root, "tools/viewer/feedback/*.jsonl"))):
    for line in open(f, encoding="utf-8"):
        if line.strip():
            d = json.loads(line)
            if d.get("id") == q or q in (d.get("id") or ""):
                hits.append(d)
if len(hits) != 1:
    print("ERR", f"id 가 {len(hits)}건 맞음: {q}"); sys.exit()
d = hits[0]; c = d.get("camera") or {}
if not c.get("pos") or not c.get("target"):
    print("ERR", "camera 없음(옛 코멘트)"); sys.exit()
w, h = 1280, 720
if d.get("screenshot"):
    try:
        from PIL import Image
        w, h = Image.open(os.path.join(root, "tools/viewer/feedback", d["screenshot"])).size
    except Exception:
        pass
proj = "D:/Github/F1Game/F1Studio" if d.get("scene") == "Studio_Building" else "D:/Github/F1Game/F1"
out = os.path.join(root, "tools/viewer/feedback/shots", d["id"] + "_after.png").replace("\\", "/")
print("OK", d["id"], d.get("scene"), proj, out, w, h, *c["pos"], *c["target"], c.get("fov") or 60, c.get("mode") or "scene")
EOF
)
set -- $SPEC
[ "${1:-}" = "OK" ] || { echo "FAIL: ${SPEC#ERR }"; exit 1; }
FULL=$2 SCENE=$3 PROJ=$4 OUT=$5 W=$6 H=$7 PX=$8 PY=$9 PZ=${10} TX=${11} TY=${12} TZ=${13} FOV=${14} MODE=${15}
[ "$MODE" = "scene" ] || echo "주의: mode=$MODE — 씬 뷰 렌더 설정으로 찍는다"
CS=$(mktemp --suffix=.cs)
trap 'rm -f "$CS"' EXIT
cat > "$CS" <<EOF
if (UnityEditor.EditorApplication.isPlaying) return "FAIL: Play 중 — editor stop 뒤에";
var scn = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scn.name != "$SCENE") return "FAIL: 열린 씬 " + scn.name + " ≠ $SCENE";
var sv = UnityEditor.SceneView.lastActiveSceneView;
var src = sv != null ? sv.camera : UnityEngine.Camera.main;
if (src == null) return "FAIL: 씬 뷰·메인 카메라 없음";
var pos = new UnityEngine.Vector3(${PX}f, ${PY}f, ${PZ}f); var tgt = new UnityEngine.Vector3(${TX}f, ${TY}f, ${TZ}f);
var go = new UnityEngine.GameObject("_FbShotSrc") { hideFlags = UnityEngine.HideFlags.HideAndDontSave };
try {
  var cam = go.AddComponent<UnityEngine.Camera>(); cam.CopyFrom(src); cam.cameraType = UnityEngine.CameraType.Game; cam.enabled = false;
  cam.fieldOfView = ${FOV}f;
  go.transform.SetPositionAndRotation(pos, UnityEngine.Quaternion.LookRotation(tgt - pos));
  bool ok = PitCrewPlan.PlanIO.RenderPng(cam, "$OUT", $W, $H, null);
  return ok ? "OK $OUT ${W}x${H}" : "FAIL: RenderPng";
} finally { UnityEngine.Object.DestroyImmediate(go); }
EOF
R=$(UCLI_PROJ="$PROJ" bash "$ROOT/scripts/ucli-http.sh" exec "@$CS")
echo "$R"
case "$R" in OK*) exit 0 ;; *) exit 1 ;; esac
