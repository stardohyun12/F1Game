var sp = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>(UnityEngine.FindObjectsInactive.Include);
var t = sp.V3Table;
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool dirty0 = scene.isDirty;
var go = new UnityEngine.GameObject("PackBoard2D_shot") { hideFlags = UnityEngine.HideFlags.HideAndDontSave };
var cgo = new UnityEngine.GameObject("PackBoard2D_shot_cam") { hideFlags = UnityEngine.HideFlags.HideAndDontSave };
var rt = new UnityEngine.RenderTexture(960, 540, 24) { filterMode = UnityEngine.FilterMode.Point };
string res = "";
PackBoard b = null;
// 플레이 땐 DepotPackSpot.Awake 가 옛 작업대 자식을 전부 끈다 — 에디트 모드 그림도 같게
var kids = new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach (UnityEngine.Transform ch in sp.transform) if (ch.gameObject.activeSelf) kids.Add(ch.gameObject);
foreach (var k in kids) k.SetActive(false);
try
{
    b = go.AddComponent<PackBoard>();
    b.Build(t);
    var cam = cgo.AddComponent<UnityEngine.Camera>();
    var main = UnityEngine.Camera.main;
    if (main != null) cam.CopyFrom(main);
    cam.targetTexture = rt; cam.aspect = 960f / 540f;
    var v = PackBoardView.Create(b, sp.BoardFont);
    b.SetOn(true, cam, t.receive.today[0]);
    b.SimDrag(b.Find(PackPieceKind.Box, 1), PackBoard.SlotU, PackBoard.SlotV);
    b.CamPose(cam, out var pos, out var rot);
    cam.transform.SetPositionAndRotation(pos, rot);
    typeof(PackBoardView).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(v, null);
    foreach (var tm in go.GetComponentsInChildren<TMPro.TextMeshPro>(true)) tm.ForceMeshUpdate();
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(960, 540, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 960, 540), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/pack2d-board-on.png", tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex);
    res = "ok cam " + pos + " open=" + b.InSlot?.name + " | " + b.LastAct;
}
finally
{
    if (b != null) b.SetOn(false, null, null);   // 숨겼던 3D 렌더러 되돌림
    foreach (var k in kids) if (k != null) k.SetActive(true);
    UnityEngine.RenderTexture.active = null;
    UnityEngine.Object.DestroyImmediate(rt);
    UnityEngine.Object.DestroyImmediate(go);
    UnityEngine.Object.DestroyImmediate(cgo);
}
return res + " dirty " + dirty0 + "→" + scene.isDirty;
