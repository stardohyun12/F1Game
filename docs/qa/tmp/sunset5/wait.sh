# 컴파일·임포트 끝날 때까지 기다림
for i in $(seq 1 30); do
  r=$(bash scripts/ucli-http.sh exec 'return (UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating) ? "busy" : ("idle near=" + (typeof(SunsetLook).GetField("seaNear")!=null));' 2>&1 | tail -1)
  echo "$r"
  case "$r" in *idle*) exit 0;; esac
  sleep 4
done
