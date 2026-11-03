var b = UnityEngine.GameObject.Find("TownV6_Gray").transform.Find("Buildings"); var sb = new System.Text.StringBuilder();
foreach (UnityEngine.Transform ch in b) { var r = ch.GetComponent<UnityEngine.MeshRenderer>(); if (r == null) continue;
  foreach (var m in r.sharedMaterials) { sb.Append(ch.name + " " + UnityEditor.AssetDatabase.GetAssetPath(m) + " sh=" + m.shader.name + " tex=" + (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null ? m.GetTexture("_MainTex").name : "null") + " tint=" + (m.HasProperty("_Tint") ? m.GetColor("_Tint").ToString() : "-") + "\n"); } }
var pm = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Scenes/Map_TownV6_Mats/Parcels_Body.mat"); sb.Append("Parcels_Body tex=" + (pm.GetTexture("_MainTex") != null ? pm.GetTexture("_MainTex").name : "null") + " tint=" + pm.GetColor("_Tint"));
return sb.ToString();
