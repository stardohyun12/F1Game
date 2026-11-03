var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
var ho = d.Handover;
var cam = Camera.main;
string onScreen = "-";
if (ho != null && ho.recipient != null && ho.recipient.gameObject.activeInHierarchy && cam != null)
{
  var vp = cam.WorldToViewportPoint(ho.recipient.position + Vector3.up * 1.4f);
  onScreen = (vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1) + " vp=" + vp.ToString("F2");
}
return "state=" + d.State + " prompt='" + d.Prompt + "' sub='" + d.Subtitle + "' carry=" + d.CarryingBox + " knocking=" + d.Knocking + " talk=" + d.InTalk
  + " open=" + (ho != null && ho.IsOpen) + " tier=" + d.HandoverTierIdx + " money=" + d.HandoverMoney + " moneyUI='" + (ui != null ? ui.MoneyShown : "") + "' handed=" + d.HandedOver
  + " resultPanel=" + ResultPanel.ShowCount + " ts=" + Time.timeScale + " camYaw=" + (cam != null ? cam.transform.eulerAngles.y.ToString("F0") : "-") + " julia_on_screen=" + onScreen;
