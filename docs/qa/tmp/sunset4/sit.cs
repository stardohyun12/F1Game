// Play: 걷기 상태면 자리로 → 앉기(다음 프레임에 처리됨)
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
string s = "state=" + d.State + " seated=" + spot.Seated;
if (!spot.Seated) { bool g = d.GoToSpot(); d.SimInteract(); s += " go=" + g; }
return s;
