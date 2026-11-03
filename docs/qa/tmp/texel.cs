// 텍셀 밀도(텍셀/m) — 메시 UV 넓이 × 텍스처 크기 ÷ 월드 넓이. 화면 px/m = H / (2 d tan(fov/2))
var sb = new System.Text.StringBuilder();
string[] names = { "HOUSE_shell", "RUG_01", "POSTER_01", "POSTER_02", "CALENDAR_01", "DESK_01", "BED_01", "DOOR_ROOM", "BOOK_01", "RADIO_01" };
var root = GameObject.Find("TownV6_Gray/D198_Buildings/house_home");
foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) {
  if (System.Array.IndexOf(names, mf.name) < 0) continue;
  var r = mf.GetComponent<Renderer>(); var m = mf.sharedMesh; if (m == null || r == null) continue;
  var mats = r.sharedMaterials; var v = m.vertices; var uv = m.uv; var M = mf.transform.localToWorldMatrix;
  for (int s = 0; s < m.subMeshCount; s++) {
    var mat = mats[System.Math.Min(s, mats.Length - 1)]; var t = mat.GetTexture("_BaseMap"); if (t == null) continue;
    var tri = m.GetTriangles(s); double uA = 0, wA = 0;
    for (int i = 0; i < tri.Length; i += 3) {
      Vector3 a = M.MultiplyPoint3x4(v[tri[i]]), b = M.MultiplyPoint3x4(v[tri[i+1]]), c = M.MultiplyPoint3x4(v[tri[i+2]]);
      double w = Vector3.Cross(b - a, c - a).magnitude * 0.5; if (w < 1e-6) continue;
      Vector2 p = uv[tri[i]], q = uv[tri[i+1]], o = uv[tri[i+2]];
      double u = System.Math.Abs((q.x - p.x) * (o.y - p.y) - (o.x - p.x) * (q.y - p.y)) * 0.5 * t.width * t.height;
      uA += u; wA += w; }
    if (wA > 0) sb.Append(mf.name).Append(" sub").Append(s).Append(" ").Append(mat.name).Append(" texel/m=").Append(System.Math.Sqrt(uA / wA).ToString("F1")).Append(" area=").Append(wA.ToString("F2")).Append("\n");
  }
}
// 바깥 비교: 마을 건물 하나(외벽)
foreach (var n in new[] { "TownV6_Gray/TownV6_Terrain" }) { var g = GameObject.Find(n); if (g != null) sb.Append(n).Append(" mat ").Append(g.GetComponent<Renderer>().sharedMaterial.name).Append("\n"); }
return sb.ToString();
