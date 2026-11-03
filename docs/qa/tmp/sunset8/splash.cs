// Play: 찌 착수 소리 상태 한 줄 (앉은 직후 0.25 s 간격으로 부름)
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var src = (AudioSource)typeof(FishingSpot).GetField("splash", bf).GetValue(spot);
float castT = (float)typeof(FishingSpot).GetField("castT", bf).GetValue(spot);
bool landed = (bool)typeof(FishingSpot).GetField("landed", bf).GetValue(spot);
return "t=" + Time.time.ToString("F2") + " castT=" + castT.ToString("F2") + " landed=" + landed + " playing=" + (src != null && src.isPlaying) + " clip=" + (spot.splashClip ? spot.splashClip.name : "-");
