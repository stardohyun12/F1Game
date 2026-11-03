// 편집 모드: 앉은 시점 640x360 의 픽셀(__X__, __Y__ 위에서) 에 보이는 물체
var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform; var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var go = new GameObject("_tmpP"); var c = go.AddComponent<Camera>(); c.fieldOfView = spot.seatedFov; c.aspect = 16f / 9f;
var e = seat.eulerAngles; c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(e.x + spot.seatLookDown, e.y + __YAW__f, 0));
var ray = c.ViewportPointToRay(new Vector3(__X__f / 640f, 1f - __Y__f / 360f, 0)); UnityEngine.Object.DestroyImmediate(go);
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    if (r.bounds.IntersectRay(ray, out float d) && d < 400f) sb.Append(d.ToString("F0") + "m " + r.name + " / " + (r.transform.parent ? r.transform.parent.name : "") + " / " + r.transform.root.name + " mat=" + (r.sharedMaterial ? r.sharedMaterial.name : "") + " pos=" + r.transform.position.ToString("F1") + "\n");
}
return sb.ToString();
