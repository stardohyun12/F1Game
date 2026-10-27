// 과제 1815 탐색 2: 연석 로컬 상자·바닥 높이
var sb = new System.Text.StringBuilder();
foreach (Transform t in GameObject.Find("SceneA4").transform.Find("A4e_Curbs")) {
  var c = t.GetComponent<BoxCollider>(); var mr = t.GetComponent<MeshRenderer>();
  sb.Append(t.name + " pos " + t.position.ToString("F2") + " rot " + t.eulerAngles.ToString("F1") + " ls " + t.localScale.ToString("F2") + " cC " + c.center.ToString("F2") + " cS " + c.size.ToString("F2") + " mr " + (mr != null) + " comps " + string.Join("/", t.GetComponents<Component>().Select(x => x.GetType().Name)) + " layer " + t.gameObject.layer);
  var h = GameObject.Find("SceneA4").transform.Find("A4b_Houses/" + t.name.Substring(5)); if (h) sb.Append(" houseLocal " + t.InverseTransformPoint(h.position).ToString("F2"));
  // 상자 네 모서리 바닥 높이(위에서 아래로 레이, 연석 자신 제외)
  var hs = c.size * 0.5f;
  foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) {
    var wp = t.TransformPoint(c.center + new Vector3(sx * hs.x, 0, sz * hs.z)); var hits = Physics.RaycastAll(wp + Vector3.up * 5f, Vector3.down, 15f); float best = -999; string bn = "";
    foreach (var hh in hits) if (hh.collider != c && hh.point.y > best) { best = hh.point.y; bn = hh.collider.name; }
    sb.Append(" [" + sx + "," + sz + "] top " + (wp.y + hs.y).ToString("F2") + " gnd " + best.ToString("F2") + " " + bn);
  }
  sb.Append("\n");
}
var w = GameObject.Find("Wall_L"); sb.Append("Wall_L comps " + string.Join("/", w.GetComponents<Component>().Select(x => x.GetType().Name)) + " children " + w.transform.childCount + " mesh " + w.GetComponentInChildren<MeshFilter>().sharedMesh.name + " uvScale? " + w.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetTextureScale("_BaseMap"));
var wm = w.GetComponentInChildren<MeshRenderer>().sharedMaterial; sb.Append(" tex " + AssetDatabase.GetAssetPath(wm.GetTexture("_BaseMap")) + " shader " + wm.shader.name);
return sb.ToString();
