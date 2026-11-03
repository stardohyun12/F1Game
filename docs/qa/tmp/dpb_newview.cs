var r = PackBenchBuilder.BuildDepot();
var b = GameObject.Find("OnFoot_Depot/PackSpot/PackBench/WorkBoard");
string mat = b ? b.GetComponent<Renderer>().sharedMaterial.name : "없음";
// 처음 보는 자리: 보관소 문 쪽(사람 높이 1.6m)에서 카운터를 비스듬히
var tray = GameObject.Find("OnFoot_Depot/PackSpot/PackBench/EggTray");
bool trayWas = tray != null && tray.activeSelf; if (tray) tray.SetActive(true);
var go = new GameObject("tmp_cam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 60f; cam.nearClipPlane = 0.05f;
var eye = new Vector3(271.6f, 31.06f + 1.6f, -32.0f); var tgt = new Vector3(274.4f, 32.1f, -29.6f);
go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(tgt - eye));
var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render();
RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/depot-packbench/new_angle_from_door.png", tex.EncodeToPNG());
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(tex);
if (tray) tray.SetActive(trayWas);
return r + " | board mat=" + mat;
