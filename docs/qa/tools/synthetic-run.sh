#!/usr/bin/env bash
set -eu
target="${1:?Depot, FactoryA or ContainerYard required}"
case "$target" in Depot|FactoryA|ContainerYard) ;; *) exit 2;; esac
u(){ bash scripts/ucli.sh exec "$1"; }
bash scripts/ucli.sh editor play
sleep 2
menu_start=$(date +%s)
u "Application.runInBackground=true; UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; var f=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance; foreach(var c in UnityEngine.Object.FindObjectsByType<OrderCardUI>(FindObjectsSortMode.None)){var j=(OrderJob)typeof(OrderCardUI).GetField(\"job\",f).GetValue(c);if(j.destinationLabels.Contains(\"$target\")){c.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));return \"selected $target\";}} return \"FAIL target absent\";"
u 'foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))if(b.name=="BoardGoButton"&&b.interactable)b.onClick.Invoke();return "routing";'
u 'foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))if(b.name=="Suggest")b.onClick.Invoke();return "suggested";'
u 'foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))if(b.name=="Confirm"&&b.interactable)b.onClick.Invoke();return "packing";'
if [ "$target" = ContainerYard ]; then
 bash docs/qa/tools/exec-file.sh docs/qa/tools/synthetic-drag.cs.txt
 sleep 2
 u 'UnityEditor.EditorApplication.ExecuteMenuItem("PitCrew/Capture/Auto Screenshot");return "wrapped screenshot";'
fi
u 'foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))if(b.name=="BtnGo"&&b.interactable)b.onClick.Invoke();return "placement (0% wrap allowed by design)";'
if [ "$target" = ContainerYard ]; then
 bash docs/qa/tools/exec-file.sh docs/qa/tools/synthetic-drag.cs.txt
 sleep 2
 u 'UnityEditor.EditorApplication.ExecuteMenuItem("PitCrew/Capture/Auto Screenshot");return UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>().ActiveItem.transform.localPosition;'
fi
echo "QA_MENU_ESTIMATE target=$target seconds=$(($(date +%s)-menu_start)) includes connector overhead"
bash docs/qa/tools/exec-file.sh docs/qa/tools/synthetic-drive.cs.txt
