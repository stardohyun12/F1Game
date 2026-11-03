// 캡처와 같은 시점(에디트 모드 임시 카메라)으로 지형 렌더러 켬/끔 두 장 — 칼날이 지형 것인지 가른다. 끝나면 렌더러 원상태.
var eye = new Vector3(279f, 33.58f + 1.6f, -16f); var tgt = new Vector3(279f, 31.2f, -25f);
var go = new GameObject("tmp_qa_cam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 60f;
cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(tgt - eye));
var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt;
var tr = GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<Renderer>(); bool was = tr.enabled;
string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/20261104-0715/"));
var res = "";
foreach (var on in new[] { true, false })
{
    tr.enabled = on; cam.Render();
    RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(root + "edit_terrain_" + (on ? "on" : "off") + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
    res += (on ? "on " : "off ");
}
tr.enabled = was; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
return res + " terrain enabled=" + tr.enabled + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
