// ⑦ 계단 지름길 + 계단식 밭 (에디트 모드 렌더). 아래 길 동쪽 위에서 계단(헤어핀 두 다리 사이)과 서쪽 밭을 함께 본다.
var bs = new System.Collections.Generic.List<Vector3>();
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name.StartsWith("STAIR_Bollard_")) bs.Add(t.position);
if (bs.Count == 0) return "no bollards";
var mid = Vector3.zero; foreach (var b in bs) mid += b; mid /= bs.Count;
var field = new Vector3(73f, mid.y - 1f, 193f);
var look = Vector3.Lerp(mid, field, 0.4f);
var src = UnityEngine.Object.FindFirstObjectByType<FollowCamera>().GetComponent<Camera>();
var go = new GameObject("TmpCap7"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(src); cam.fieldOfView = 55f; cam.farClipPlane = 2000f;
go.transform.position = mid + new Vector3(14f, 9f, 22f); go.transform.LookAt(look);
var rt = new RenderTexture(1920, 1080, 24); var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply(); RenderTexture.active = null;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/hilltop-7-stairs.png", tex.EncodeToPNG());
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(rt);
return "bollards " + bs.Count + " mid " + mid.ToString("F1") + " cam " + (mid + new Vector3(14f, 9f, 22f)).ToString("F0");
