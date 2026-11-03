// 7판 후속: 섬 자리 회색 대역 벽 숨김 목록 + 구름 띠 굵기·빈도 + 해 빛 세기 원복
var look = SunsetLook.Find();
var list = new System.Collections.Generic.List<Renderer>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    if (r.transform.parent != null && r.transform.parent.name.StartsWith("DEADEND_wall_tomo_886667732")) list.Add(r);
look.hideWhileOn = list.ToArray();
var s = look.cloudStrips; s.z = __COV__f; s.w = __W__f; look.cloudStrips = s;
look.sunIntensity = 1.25f;
UnityEditor.EditorUtility.SetDirty(look); look.Apply();
return "hide=" + list.Count + " strips=" + look.cloudStrips + " si=" + look.sunIntensity;
