// 1514: Coast_* 메시 bounds·재질 셰이더, BL_Concrete 텍스처/타일링
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Coast_Masonry") || r.name.StartsWith("Coast_Wet")).Take(12))
    sb.Append(r.name + " " + r.bounds.min.ToString("F0") + "~" + r.bounds.max.ToString("F0") + " tris " + (r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3) + "\n");
var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_BayLoop_Mats/BL_Concrete.mat");
if (m == null) { var og = GameObject.Find("OG_G_quay"); m = og.GetComponent<Renderer>().sharedMaterial; }
sb.Append("mat " + m.name + " shader " + m.shader.name + " tex " + (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") ? m.GetTexture("_MainTex").name : "-") + " st " + (m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex").ToString() : "") + " path " + UnityEditor.AssetDatabase.GetAssetPath(m));
return sb.ToString();
