// Play 중: 지금 Camera.main 화면 격자 레이캐스트 → 맞은 건물 머티리얼·텍스처 집계(창 발광 커버리지 확인)
var cam = Camera.main; var cnt = new Dictionary<string, int>();
for (int x = 0; x < 24; x++) for (int y = 0; y < 14; y++)
{
    var ray = cam.ViewportPointToRay(new Vector3((x + .5f) / 24f, (y + .5f) / 14f, 0));
    if (!Physics.Raycast(ray, out var hit, 400f)) continue;
    var r = hit.collider.GetComponentInParent<Renderer>() ?? hit.collider.GetComponentInChildren<Renderer>();
    string k = hit.collider.name;
    if (r != null) k += " | " + string.Join(",", r.sharedMaterials.Where(m => m != null).Select(m => m.name + ":" + (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null ? m.GetTexture("_MainTex").name : "-")));
    cnt[k] = cnt.TryGetValue(k, out var c) ? c + 1 : 1;
}
return "cam=" + cam.transform.position + "\n" + string.Join("\n", cnt.OrderByDescending(p => p.Value).Take(15).Select(p => p.Value + "  " + p.Key));
