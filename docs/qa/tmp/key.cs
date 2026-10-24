// 가상 키보드로 D 를 0.6초 누름 → 활성 화물 회전 전후 비교. SessionState "qakey" 에 결과.
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var kb=UnityEngine.InputSystem.Keyboard.current;
if(kb==null) kb=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
var item=ps.ActiveItem; var q0=item.transform.rotation;
string ph=ps.GetType().GetField("phase",bf).GetValue(ps)+"";
float t0=Time.realtimeSinceStartup; int fr=0; UnityEditor.EditorApplication.CallbackFunction cb=null;
UnityEditor.SessionState.SetString("qakey","running");
cb=()=>{ float t=Time.realtimeSinceStartup-t0; fr++;
 var keys = new System.Collections.Generic.List<UnityEngine.InputSystem.Key>();
 if(t<0.6f){ keys.Add(UnityEngine.InputSystem.Key.D); keys.Add(UnityEngine.InputSystem.Key.W); }
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys.ToArray()));
 if(t>1.0f){ UnityEditor.EditorApplication.update-=cb; float ang=Quaternion.Angle(q0,item.transform.rotation);
  UnityEditor.SessionState.SetString("qakey","phase="+ph+" dragging="+ps.IsDraggingTrunkItem+" angleChanged="+ang.ToString("F1")+" rot="+item.transform.eulerAngles+" frames="+fr+" kbDev="+kb.deviceId+" behav="+UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode); } };
UnityEditor.EditorApplication.update+=cb; return "pressing D+W phase="+ph;
