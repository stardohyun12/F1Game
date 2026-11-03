// Play: 노을 켜고 낚시 자리에 앉음(= 던지기 시작) + 같은 프레임에 검산 프로브 __DUR__ 초 시작(던지기 → 착수 → 출렁)
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null) return "no director";
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
var m = look.seaMaterial; m.SetFloat("_Style", __STYLE__f);
if (d.State.ToString() == "Seated") return "already seated — stop/play again";
if (d.State.ToString() != "OnFoot") return "state=" + d.State;
d.GoToSpot();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var spots = (System.Collections.IList)typeof(OnFootDirector).GetField("spots", bf).GetValue(d);
typeof(OnFootDirector).GetMethod("Sit", bf).Invoke(d, new object[] { spots[0] });
FishingRippleProbe.Begin(__DUR__f, "D:/Github/F1Game/docs/qa/evidence/fishing-ripple/__TAG__/", "__TAG__");
var spot = (FishingSpot)spots[0];
return "state=" + d.State + " sunset=" + look.IsOn + " near=" + look.seaNear.enabled + " style=" + m.GetFloat("_Style") + " castTo~" + (spot.seat.position + spot.seat.forward * spot.castDistance).ToString("F1") + " seaY=" + look.seaNear.transform.position.y + " clip=" + (spot.splashClip != null);
