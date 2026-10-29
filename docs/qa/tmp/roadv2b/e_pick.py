# 1e: 시트 3장 카메라로 화면 격자(10x6) 광선 → 맞은 물체 이름·재질. 콜라이더 없는 RoadsV2 조각엔 임시 MeshCollider.
import glob, json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "..", "scripts"))
import fb_geom
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
IDS = sys.argv[1:] or ["230810-268e", "230823-7da7", "230939-4143"]
cams = []
for f in sorted(glob.glob(os.path.join(ROOT, "tools/viewer/feedback/*.jsonl"))):
    for line in open(f, encoding="utf-8"):
        if line.strip():
            d = json.loads(line)
            if any(i in (d.get("id") or "") for i in IDS):
                c = d["camera"]; cams.append((d["id"], c["pos"], c["target"], c.get("fov") or 60))
body = []
for cid, p, t, fov in cams:
    body.append(f'Shot("{cid}", new Vector3({p[0]}f,{p[1]}f,{p[2]}f), new Vector3({t[0]}f,{t[1]}f,{t[2]}f), {fov}f);')
code = r'''
var sb = new System.Text.StringBuilder(); var tmp = new List<MeshCollider>();
foreach (var mf in GameObject.Find("TownV6_Gray").GetComponentsInChildren<MeshFilter>(false)) { if (mf.GetComponent<Collider>() == null && mf.GetComponent<MeshRenderer>() != null && !mf.name.StartsWith("Bld")) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; tmp.Add(mc); } }
Physics.SyncTransforms();
void Shot(string id, Vector3 pos, Vector3 tgt, float fov) {
  var go = new GameObject("pick_cam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = fov; cam.aspect = 16f/9f; go.transform.position = pos; go.transform.LookAt(tgt);
  sb.AppendLine("== " + id);
  for (int y = 5; y >= 0; y--) { for (int x = 0; x < 10; x++) { var ray = cam.ViewportPointToRay(new Vector3((x + .5f) / 10f, (y + .5f) / 6f, 0));
      string s = "-"; if (Physics.Raycast(ray, out var h, 2000)) { var r = h.collider.GetComponent<MeshRenderer>(); s = h.collider.name.Replace("ROAD_","").Replace("camo_","c").Replace("tomo_","t") ; if (s.Length > 26) s = s.Substring(0, 26); s += "[" + (r && r.sharedMaterial ? r.sharedMaterial.name.Replace("TV6_","") : "?") + "]@" + h.point.x.ToString("F0") + "," + h.point.z.ToString("F0"); }
      sb.Append(s.PadRight(46)); } sb.AppendLine(); }
  UnityEngine.Object.DestroyImmediate(go);
}
''' + "\n".join(body) + r'''
foreach (var mc in tmp) UnityEngine.Object.DestroyImmediate(mc);
return sb.ToString();
'''
r = fb_geom.ucli_exec("D:/Github/F1Game/F1", code)
open(os.path.join(HERE, "e_pick.txt"), "w", encoding="utf-8").write(r)
print(r)
