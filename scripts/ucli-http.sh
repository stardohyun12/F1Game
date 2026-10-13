#!/usr/bin/env bash
# unity-cli(youngwoocho02) exe 가 없는 머신용 대체 — 같은 connector 의 HTTP 포트로 직접 보낸다.
# 지원: exec "<C#>" | editor play [--wait] | editor stop | console
# 포트: ~/.unity-cli/instances/*.json 중 이 리포(F1) 항목에서 읽는다.
# 사용: UCLI="bash scripts/ucli-http.sh" 로 regression-check.sh 의 UCLI 를 바꿔 쓴다.
set -u
PROJ="D:/Github/F1Game/F1"
PORT=$(python -X utf8 - "$PROJ" <<'EOF'
import json,glob,os,sys
proj=sys.argv[1].lower()
for f in glob.glob(os.path.expanduser("~/.unity-cli/instances/*.json")):
    try: d=json.load(open(f,encoding="utf-8"))
    except Exception: continue
    p=d.get("projectPath","").replace("\\","/").rstrip("/").lower()
    # Play/컴파일 중엔 state 가 ready 가 아니다 — stopped 만 제외
    if p==proj and d.get("state")!="stopped": print(d["port"]); break
EOF
)
[ -z "$PORT" ] && { echo "FAIL: F1 Editor connector not ready"; exit 1; }
U="http://127.0.0.1:$PORT/command"
[ "${1:-}" = "status" ] && { echo "F1 Editor connector: port $PORT"; exit 0; }

post() { python -X utf8 - "$U" "$1" "$2" <<'EOF'
import json,sys,urllib.request
u,cmd,params=sys.argv[1],sys.argv[2],sys.argv[3]
body={"command":cmd,"params":json.loads(params)}
req=urllib.request.Request(u,data=json.dumps(body).encode(),headers={"Content-Type":"application/json"})
try:
    d=json.loads(urllib.request.urlopen(req,timeout=280).read())
except Exception as e:
    print("FAIL: request error", e); sys.exit(1)
if not d.get("success"): print("FAIL:", d.get("message")); sys.exit(1)
out=d.get("data")
print(out if isinstance(out,str) else json.dumps(out,ensure_ascii=False))
EOF
}

case "${1:-}" in
  exec)
    code=$(python -X utf8 -c 'import json,sys; print(json.dumps({"code": sys.argv[1]}))' "$2")
    post exec "$code" ;;
  editor)
    case "${2:-}" in
      play)
        post exec '{"code":"UnityEditor.EditorApplication.EnterPlaymode(); return \"play\";"}' >/dev/null
        if [ "${3:-}" = "--wait" ]; then
          for i in $(seq 1 60); do sleep 1; r=$(post exec '{"code":"return UnityEditor.EditorApplication.isPlaying ? \"yes\" : \"no\";"}' 2>/dev/null); [ "$r" = "yes" ] && break; done
          sleep 2
        fi
        echo "play" ;;
      stop)
        post exec '{"code":"UnityEditor.EditorApplication.ExitPlaymode(); return \"stop\";"}' ;;
      *) echo "unsupported: editor $2"; exit 2 ;;
    esac ;;
  console)
    # unity-cli 의 `console --type error[,warning]` 호환 — connector 파라미터는 type/lines/stacktrace
    t="error"; [ "${2:-}" = "--type" ] && t="${3:-error}"
    post console "{\"type\":\"$t\",\"lines\":20,\"stacktrace\":\"none\"}" ;;
  *) echo "unsupported: $*"; exit 2 ;;
esac
