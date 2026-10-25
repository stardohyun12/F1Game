#!/bin/bash
# 0001: 새 마을 레이아웃에서 출발 → 외딴집(route 0) 합성 주행 한 번
cd D:/Github/F1Game
U="bash scripts/ucli-http.sh"
$U exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Proto_CoastCourse.unity"); return "ok";'
$U editor play --wait >/dev/null 2>&1; sleep 3
$U exec 'Application.runInBackground = true; UnityEditor.SessionState.SetString("proto.drive",""); UnityEditor.SessionState.SetInt("proto.route",0); return "h";'
$U exec @docs/qa/tmp/proto-start-hill.cs | tail -1 | cut -c1-120
for i in $(seq 1 30); do
  r=$($U exec 'var rc=Object.FindFirstObjectByType<RunController>(); var m=rc.GetType().GetMethod("OnGoClicked",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic); if(m!=null) m.Invoke(rc,null); return rc.CurrentStage.ToString();' | tail -1)
  echo "$r" | grep -q Driving && break; sleep 2
done
echo "stage=$r"
$U exec @docs/qa/tmp/proto-drive.cs | tail -1 | cut -c1-100
for i in $(seq 1 60); do
  d=$($U exec 'return UnityEditor.SessionState.GetString("proto.drive","");' | tail -1)
  [ ${#d} -gt 4 ] && break; sleep 5
done
echo "drive=$d"
$U exec 'var rc=Object.FindFirstObjectByType<RunController>(); return rc.CurrentStage.ToString();' | tail -1
$U editor stop >/dev/null 2>&1
git checkout -- "F1/Assets/UI/Fonts/Korean" "F1/Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"
$U exec 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ComplexTrack.unity"); return "ok";'
