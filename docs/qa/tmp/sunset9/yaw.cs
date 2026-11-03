// Play: 앉은 시선 yaw = __YAW__, pitch 0
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(FishingSpot).GetField("pitch", bf).SetValue(spot, 0f);
typeof(FishingSpot).GetField("yaw", bf).SetValue(spot, Mathf.Clamp(__YAW__f, -spot.yawLeft, spot.yawRange));
return "yaw=" + typeof(FishingSpot).GetField("yaw", bf).GetValue(spot);
