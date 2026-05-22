#!/usr/bin/env bash
# PitCrew 회귀 점검 — Play 흐름의 각 상태에서 HarnessChecks 단언 호출.
# 우리가 실제로 겪은 버그(엔진음 잔류·맵 fog·화물 피벗)가 되살아나면 FAIL.
# 사용: bash scripts/regression-check.sh   (Editor 켜져 있어야)
UCLI="C:/Users/stard/AppData/Local/unity-cli/unity-cli.exe"
say() { "$UCLI" exec "$1" 2>/dev/null | grep -vE "Update available|unity-cli update|^$"; }

echo "=== PitCrew 회귀 점검 ==="
"$UCLI" editor play --wait >/dev/null 2>&1
sleep 1
# 프레임 틱 보장(백그라운드면 Update 멈춤) + 메인메뉴 시작
"$UCLI" exec "Application.runInBackground=true; foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)){ var t=b.GetComponentInChildren<TMPro.TMP_Text>(); if(t!=null && t.text.Contains(\"시작\")){ b.onClick.Invoke(); break; } } return \"setup\";" >/dev/null 2>&1
sleep 2

echo "[BOARD 주문보드]"
say "return HarnessChecks.CheckFog(false);"
say "return HarnessChecks.CheckRunInactiveSilent();"
say "return HarnessChecks.CheckCargoPivots();"

# 포장 화면 — 실제 입력경로(BeginStroke)로 wrap 생성되는지 (false-green 종류 검출)
"$UCLI" exec "var j=ScriptableObject.CreateInstance<OrderJob>(); j.cargoType=CargoType.Glass; j.destinationLabels=new System.Collections.Generic.List<string>{\"T\"}; var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); if(ps==null){ ps=new GameObject(\"PackingScreen\").AddComponent<PitCrew.Packing.PackingScreen>(); } ps.Show(j); return \"pack\";" >/dev/null 2>&1
sleep 1
echo "[PACKING 포장]"
say "return HarnessChecks.CheckWrapRealPath();"
"$UCLI" exec "var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); if(ps!=null) ps.Hide(); return \"hide\";" >/dev/null 2>&1
sleep 1

"$UCLI" exec "var j=ScriptableObject.CreateInstance<OrderJob>(); j.cargoType=CargoType.Glass; j.destinationLabels=new System.Collections.Generic.List<string>{\"T\"}; (Services.Run as RunController).BeginRun(j); return \"begin\";" >/dev/null 2>&1
sleep 2

echo "[DRIVING 주행]"
say "return HarnessChecks.CheckFog(true);"
say "return HarnessChecks.CheckEngineMute(false);"

"$UCLI" exec "typeof(RunController).GetMethod(\"EndRun\", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Services.Run as RunController, new object[]{true}); return \"end\";" >/dev/null 2>&1
sleep 2

echo "[RESULT 정산]"
say "return HarnessChecks.CheckRunInactiveSilent();"
say "return HarnessChecks.CheckEngineMute(true);"
say "return HarnessChecks.CheckFog(false);"

"$UCLI" editor stop >/dev/null 2>&1
echo "=== 끝 (FAIL 있으면 회귀) ==="
