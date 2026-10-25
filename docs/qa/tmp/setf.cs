var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var T=typeof(ArcadeCarController);
foreach(var n in new string[]{"driftGripMultiplier","driftVelocityAlign","driftYawRateDeg","driftYawResponse","driftGripRecoverTime","driftLongitudinalDrag"}){ float d=UnityEditor.SessionState.GetFloat("qa_"+n,-999f); if(d!=-999f) T.GetField(n,bf).SetValue(c,d); }
var s=""; foreach(var n in new string[]{"driftGripMultiplier","driftVelocityAlign","driftYawRateDeg","driftYawResponse","driftGripRecoverTime","driftLongitudinalDrag"}) s+=n+"="+T.GetField(n,bf).GetValue(c)+" ";
return s;
