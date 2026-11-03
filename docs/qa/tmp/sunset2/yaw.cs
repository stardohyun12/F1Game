// 앉은 눈에서 방위 4개(151 원래 / 175 / 195 / 215) 노을 켬 640x360 한 장씩 + 시야 안 나무 이름 집계
var dir = "D:/Github/F1Game/docs/qa/tmp/sunset2/"; System.IO.Directory.CreateDirectory(dir);
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
float[] yaws = { 151, 175, 195, 215 };
var sb = new System.Text.StringBuilder();
look.SetOn(true);
foreach (var y in yaws)
{
    c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(3, y, 0));
    c.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes(dir + "yaw" + y + ".png", tex.EncodeToPNG());
}
look.SetOn(false);
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
// 반경 450m 안 나무류 렌더러: 이름·메시·셰이더별 개수와 최근접 거리
var counts = new System.Collections.Generic.Dictionary<string, int>(); var near = new System.Collections.Generic.Dictionary<string, float>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    var mf = r.GetComponent<MeshFilter>(); string mesh = mf && mf.sharedMesh ? mf.sharedMesh.name : "-";
    string n = r.name + "|" + mesh;
    if (n.ToLower().IndexOf("tree") < 0 && n.ToLower().IndexOf("pine") < 0 && n.ToLower().IndexOf("palm") < 0 && n.ToLower().IndexOf("bush") < 0) continue;
    float dd = Vector3.Distance(r.bounds.center, seat.position); if (dd > 450) continue;
    var pr = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(r.gameObject);
    string key = (pr ? UnityEditor.AssetDatabase.GetAssetPath(pr) : "(no prefab)") + " :: " + mesh + " :: " + (r.sharedMaterial ? r.sharedMaterial.shader.name : "-") + " parent=" + (r.transform.parent ? r.transform.parent.name : "-");
    counts[key] = counts.ContainsKey(key) ? counts[key] + 1 : 1; near[key] = near.ContainsKey(key) ? Mathf.Min(near[key], dd) : dd;
}
foreach (var kv in counts) sb.Append(kv.Value + " near=" + near[kv.Key].ToString("F0") + "m  " + kv.Key + "\n");
return sb.ToString();
