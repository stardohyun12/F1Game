var sb = new System.Text.StringBuilder();
UnityEngine.Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None)) if (t.name == "ROOM_2F" && t.parent != null) room = t;
var house = room.parent;
sb.AppendLine("house=" + house.name + " parent=" + (house.parent? house.parent.name:"-") + " pos=" + house.position + " rot=" + house.eulerAngles + " scale=" + house.lossyScale);
foreach (UnityEngine.Transform c in house) { var r = c.GetComponent<UnityEngine.Renderer>(); sb.AppendLine(" H/" + c.name + " act=" + c.gameObject.activeSelf + (r? " r b=" + r.bounds.center.ToString("F1")+" sz="+r.bounds.size.ToString("F1") + " mat=" + string.Join(",", System.Linq.Enumerable.Select(r.sharedMaterials, m => m? m.name + "|" + m.shader.name : "null")) : "") + " kids=" + c.childCount); }
foreach (UnityEngine.Transform c in room) { var r = c.GetComponent<UnityEngine.Renderer>(); sb.AppendLine(" R/" + c.name + " act=" + c.gameObject.activeSelf + (r? " b=" + r.bounds.center.ToString("F2")+" sz="+r.bounds.size.ToString("F2") + " m=" + (r.sharedMaterial? r.sharedMaterial.name:"") : "") + " kids=" + c.childCount); }
A7ShadeGlobals.TryRoomBox(room, out var box);
var inv = box.inverse; sb.AppendLine("box center=" + inv.MultiplyPoint3x4(UnityEngine.Vector3.zero).ToString("F2") + " ex=" + inv.MultiplyVector(UnityEngine.Vector3.right).ToString("F2") + inv.MultiplyVector(UnityEngine.Vector3.up).ToString("F2") + inv.MultiplyVector(UnityEngine.Vector3.forward).ToString("F2"));
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>(); sb.AppendLine("A7 globals on " + (g? g.name : "none") + " outline=" + (g? g.roomOutline.ToString():""));
foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None)) sb.AppendLine("light " + l.name + " " + l.type + " I=" + l.intensity + " en=" + l.enabled + " act=" + l.gameObject.activeInHierarchy);
var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
sb.AppendLine("urp=" + urp.name + " addl=" + urp.additionalLightsRenderingMode + " max=" + urp.maxAdditionalLightsCount + " addShadows=" + urp.supportsAdditionalLightShadows);
foreach (var cam in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None)) sb.AppendLine("cam " + cam.name + " en=" + cam.enabled + " far=" + cam.farClipPlane + " clear=" + cam.clearFlags + " fov=" + cam.fieldOfView);
return sb.ToString();
