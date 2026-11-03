var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var fc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
var mid = Vector3.Lerp(d.Car.transform.position, d.Boss.position + Vector3.up * 1.6f, 0.3f);
fc.LookAt(mid);
typeof(OnFootDirector).GetMethod("CartLocked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(d, null);
d.Say(d.Boss, "사장", d.TalkText, 25f);   // QA 캡처용: 같은 줄을 25초 붙잡아 둔다(exec 한 번에 ~7초)
ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/packing-hands/key-1-locked.png");
return "tries=" + d.CartLockedTries + " aside=" + d.AsideOn + " text=" + d.TalkText + " t=" + Time.unscaledTime;
