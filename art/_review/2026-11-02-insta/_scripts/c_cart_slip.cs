DebugWindow.SetDusk(0f);
var sb = new System.Text.StringBuilder();
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var box = UnityEngine.Object.FindFirstObjectByType<CargoBox>();
var bp = box.transform.position;
sb.Append("box fwd=" + box.transform.forward.ToString("F2") + " parent=" + (box.transform.parent != null ? box.transform.parent.name : "-") + "\n");
// 카트 짐칸 상자 근접 후보 3
sb.Append(Still("cand_cart0", bp + new Vector3(-2.2f, 1.6f, -2.2f), bp + new Vector3(0, 0.15f, 0), 45f));
sb.Append(Still("cand_cart1", bp + new Vector3(2.4f, 1.5f, -1.6f), bp + new Vector3(0, 0.15f, 0), 45f));
sb.Append(Still("cand_cart2", bp + new Vector3(0f, 1.7f, -3.0f), bp + new Vector3(0, 0.2f, 0), 45f));
// 전표: Tab 을 누른 것과 같게
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
sb.Append("ui=" + (ui != null) + " pickedJob=" + (d.PickedJob != null) + " orderStarted=" + d.OrderStarted + "\n");
if (ui != null) ui.SimSlip = true;
return sb.ToString();
