// Play: Present 경로 640×360 한 장 → raw/s4_bench.png, 대상 화면 좌표(위=0) → raw/pos.jsonl
var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
int w = lr.RenderSize.x, h = lr.RenderSize.y;
var rt = new RenderTexture(w, h, 24); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
lr.Present(rt);
var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
System.IO.File.WriteAllBytes("D:/Github/F1Game/art/_review/2026-11-01-ui-mock/raw/s4_bench.png", tex.EncodeToPNG()); rt.Release(); UnityEngine.Object.DestroyImmediate(tex);
var sb = new System.Text.StringBuilder("{\"name\":\"s4_bench\"");
foreach (var nm in "label,item_eggs,bub3,box_floor".Split(','))
{
    if (nm.Length == 0) continue;
    var parts = nm.Split(':'); var go = GameObject.Find(parts[0]); if (go == null) continue;
    float dy = parts.Length > 1 ? float.Parse(parts[1]) : 0f;
    var v = cam.WorldToViewportPoint(go.transform.position + Vector3.up * dy);
    sb.Append(",\"" + nm + "\":[" + (v.x * w).ToString("F0") + "," + ((1 - v.y) * h).ToString("F0") + "," + v.z.ToString("F1") + "]");
}
sb.Append("}");
System.IO.File.AppendAllText("D:/Github/F1Game/art/_review/2026-11-01-ui-mock/raw/pos.jsonl", sb.ToString() + "\n");
return sb.ToString();
