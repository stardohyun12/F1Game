// 바다를 잠깐 끄고 같은 시점 — 물 밑에 그려지는 바닥이 있나
var dir = "D:/Github/F1Game/docs/qa/tmp/sunset2/";
var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform; var sea = GameObject.Find("Sea").GetComponent<Renderer>();
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(3, 180, 0));
var rt = new RenderTexture(640, 360, 24); c.targetTexture = rt; var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
sea.enabled = false; c.Render(); sea.enabled = true;
RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
System.IO.File.WriteAllBytes(dir + "nosea.png", tex.EncodeToPNG());
var terr = GameObject.Find("TownV6_Terrain"); string info = "";
if (terr) foreach (var r in terr.GetComponentsInChildren<Renderer>(true)) info += r.name + " en=" + r.enabled + " b=" + r.bounds.min.y.ToString("F1") + " sh=" + (r.sharedMaterial ? r.sharedMaterial.shader.name : "-") + " q=" + (r.sharedMaterial ? r.sharedMaterial.renderQueue : 0) + "; ";
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok " + info;
