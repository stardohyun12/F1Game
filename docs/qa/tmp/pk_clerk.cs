var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var spot = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>();
var h = spot.Hands;
var cam = (Camera)typeof(DotScreenUI).GetField("cam", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(UnityEngine.Object.FindFirstObjectByType<DotScreenUI>());
if (h.Clerk == null) return "clerk null";
var top = d.SpeakerTop;
var sp = cam.WorldToScreenPoint(top);
var fl = h.transform.Find("Hands_Box/Floor");
var r = fl != null ? fl.GetComponent<Renderer>().sharedMaterial : null;
return "clerk=" + h.Clerk.position + " active=" + h.Clerk.gameObject.activeInHierarchy + " top=" + top + " cam=" + cam.transform.position + " fw=" + cam.transform.forward + " scr=" + sp
     + " shader=" + (r != null ? r.shader.name : "?") + " col=" + (r != null && r.HasProperty("_BaseColor") ? r.GetColor("_BaseColor").ToString() : "-") + " who=" + (d.AsideWho != null ? d.AsideWho.name : "null");
