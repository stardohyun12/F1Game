// 봇이 끝났으면 상태 문자열, 아니면 예외(→ ucli exit 1) — until 루프용.
var s = OnFootRouteBot.Status ?? "";
if (!(s.StartsWith("done") || s.StartsWith("FAIL"))) throw new System.Exception("running " + s);
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return "bot=" + s + " t=" + Time.time.ToString("F0") + " state=" + d.State + " delivered=" + d.Delivered + " handed=" + d.HandedOver + " resultPanel=" + ResultPanel.ShowCount;
