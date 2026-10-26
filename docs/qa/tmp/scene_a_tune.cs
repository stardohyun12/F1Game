// 과제 0429-5 — 게이트 통과판 0 → 가장 가까운 V3 를 한 번 조정(셰이더·안개·조명 값만). 아래 빈 칸(땅의 큰 매끈한 면)을 겨냥: 얼룩 진폭↑·크기↓, 안개 최대↓(밝기 폭)
int n = 0;
foreach (var g in AssetDatabase.FindAssets("SA_V3_ t:Material", new[] { "Assets/Art/SceneA/Mats" }))
{
  var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
  m.SetFloat("_Blotch", 0.32f); m.SetFloat("_BlotchSize", 0.9f); m.SetFloat("_FogMax", 0.45f); m.SetFloat("_FootDark", 0.40f);
  m.SetFloat("_Sun", 1.15f); m.SetColor("_Ambient", m.GetColor("_FogColor") * 0.62f); EditorUtility.SetDirty(m); n++;
}
AssetDatabase.SaveAssets();
System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a/_tag.txt"), "_tune");
return "tuned " + n;
