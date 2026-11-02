var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var r = Still("test_fish", null, null, 0f);
return r + " state=" + d.State + " landed=" + d.DebugSpot.Landed;
