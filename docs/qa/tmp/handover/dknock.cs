var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return "dknock=" + d.DebugKnock() + " camYaw=" + Camera.main.transform.eulerAngles.y.ToString("F0");
