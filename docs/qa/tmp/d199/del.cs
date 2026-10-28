var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder();
var all = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var r in s.GetRootGameObjects()) all.AddRange(r.GetComponentsInChildren<UnityEngine.Transform>(true));
UnityEngine.Transform F(string n) { foreach (var t in all) if (t != null && t.name == n) return t; return null; }
string Path(UnityEngine.Transform t) { string c = ""; while (t != null) { c = t.name + "/" + c; t = t.parent; } return c; }
var kill = new System.Collections.Generic.List<UnityEngine.Transform>();
var gc = F("GarageCutscene"); if (gc) foreach (UnityEngine.Transform c in gc) if (c.GetComponent<UnityEngine.Renderer>()) kill.Add(c);
var gs = F("GarageScreen"); if (gs) foreach (UnityEngine.Transform c in gs) if (c.GetComponent<UnityEngine.Renderer>()) kill.Add(c);
var gp = F("TUT_Garage_PitCrew");
foreach (var n in new[] { "HallFloor","HallBack","HallL","HallR","HallRoof","FrontL","FrontR","Lintel","RoofCap","Workbench","TireStack","ShutterRoll","SignBoard","Label_PitCrew","ArtSlot_GarageSign" }) { var t = gp.Find(n); if (t) kill.Add(t); else sb.Append("miss " + n + "\n"); }
foreach (var t in all) if (t != null && t.name.StartsWith("ArtSlot_GarageShutter") && !kill.Contains(t)) kill.Add(t);
foreach (var n in new[] { "Q1_HomeRoom","Q2_Garage","Q3_Depot","Q4_PostOffice" }) { var t = F(n); if (t) kill.Add(t); }
var rm = F("TUT_Room");
foreach (var n in new[] { "Floor","Ceiling","WallBack","WallL","WallR","WinWallL","WinWallR","WinWallBottom","WinWallTop","WinFrameV","WinFrameH","Bed","Desk","DeskLegs","Radio","PiggyBank","RacePoster","EntryForm","ArtSlot_RacePoster","ArtSlot_EntryForm","ArtSlot_PiggyBank" }) { var t = rm.Find(n); if (t) kill.Add(t); else sb.Append("miss " + n + "\n"); }
foreach (var t in kill) { var r = t.GetComponent<UnityEngine.Renderer>(); sb.Append("DEL " + Path(t) + " ch=" + t.childCount + (t.gameObject.activeSelf ? "" : " [off]") + (r ? " " + r.GetType().Name : "") + "\n"); }
foreach (var t in kill) if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
// missing reference scan
int miss = 0, missScript = 0;
foreach (var r in s.GetRootGameObjects()) foreach (var go in r.GetComponentsInChildren<UnityEngine.Transform>(true)) {
  foreach (var c in go.GetComponents<UnityEngine.Component>()) {
    if (c == null) { missScript++; continue; }
    var so = new UnityEditor.SerializedObject(c); var it = so.GetIterator();
    while (it.Next(true)) if (it.propertyType == UnityEditor.SerializedPropertyType.ObjectReference && it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0) { miss++; sb.Append("MISSINGREF " + Path(go) + " " + c.GetType().Name + "." + it.propertyPath + "\n"); }
  }
}
sb.Append("deleted=" + kill.Count + " missingRef=" + miss + " missingScript=" + missScript + "\n");
var ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
sb.Append("saved=" + ok + "\n");
foreach (var n in new[] { "TUT_Garage_PitCrew","TUT_Room","GarageScreen","GarageCutscene" }) { var t = UnityEngine.GameObject.Find(n); if (t) { sb.Append("KEEP " + n + ": "); foreach (UnityEngine.Transform c in t.transform) sb.Append(c.name + " "); sb.Append("\n"); } }
return sb.ToString();
