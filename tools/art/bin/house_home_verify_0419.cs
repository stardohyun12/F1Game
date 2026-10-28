var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var slot = GameObject.Find("STUDIO/STUDIO_Slot");
var home = slot.transform.Find("house_home");
var log = new System.Text.StringBuilder();
log.AppendLine("project=" + Application.dataPath + " scene=" + scene.name + " playing=" + Application.isPlaying + " dirty=" + scene.isDirty + " compiling=" + UnityEditor.EditorApplication.isCompiling);
log.AppendLine("prefab=" + UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(home.gameObject));
int glassCount = 0; foreach (var mr in home.GetComponentsInChildren<Renderer>(true)) {
    if (!mr.name.StartsWith("GLASS_")) continue;
    glassCount++;
    var mat = mr.sharedMaterial;
    if (glassCount == 1) log.AppendLine("glass=" + UnityEditor.AssetDatabase.GetAssetPath(mat) + " alpha=" + mat.GetColor("_BaseColor").a + " surface=" + mat.GetFloat("_Surface") + " queue=" + mat.renderQueue);
}
log.AppendLine("glass_count=" + glassCount + " meshes=" + home.GetComponentsInChildren<MeshFilter>(true).Length);
var warn = new System.Collections.Generic.List<string>();
foreach (var renderer in home.GetComponentsInChildren<Renderer>(true)) foreach (var mat in renderer.sharedMaterials) if (!mat || !mat.shader || mat.shader.name.Contains("InternalError")) warn.Add(renderer.name);
log.AppendLine("missing_materials=" + string.Join(",", warn));
return log.ToString();
