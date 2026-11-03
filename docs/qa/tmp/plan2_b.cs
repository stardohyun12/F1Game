var sv = UnityEditor.SceneView.lastActiveSceneView;
var g = UnityEngine.GameObject.Find("TownV6_Gray/D198_Buildings/D198_depot");
PitCrewPlan.PlanIO.TryBounds(g, out var b);
// 대각선 한 획 (0.3,0.3)→(0.7,0.7), 0~1 왼쪽 위 원점
var s = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (int i = 0; i <= 20; i++) s.Add(new UnityEngine.Vector2(0.3f + 0.02f * i, 0.3f + 0.02f * i));
var strokes = new System.Collections.Generic.List<System.Collections.Generic.List<UnityEngine.Vector2>> { s };
var id = PitCrewPlan.PlanMode.SaveComment(sv.camera, sv.pivot, "scene", g, b.center, "TEST 그림 합성", strokes, 1280, 720);
var png = System.IO.Path.Combine(PitCrewPlan.PlanIO.FeedbackDir, "shots/" + id + ".png");
var tex = new UnityEngine.Texture2D(2, 2); tex.LoadImage(System.IO.File.ReadAllBytes(png));
// 텍스처 y 아래=0 → 왼쪽 위 (0.5,0.5) = 가운데, (0.3,0.3) = x384, y(텍스처)=720-216=504
UnityEngine.Color c1 = tex.GetPixel(640, 360), c2 = tex.GetPixel(384, 504), c3 = tex.GetPixel(384, 216);
var cs = PitCrewPlan.PlanIO.ReadComments("Map_TownV6");
var last = cs[cs.Count - 1];
var line = System.IO.File.ReadAllLines(PitCrewPlan.PlanIO.JsonlPath("Map_TownV6")).Last();
PitCrewPlan.PlanMode.RefreshPins(true);
return "id=" + id + " png=" + tex.width + "x" + tex.height + " center=" + c1 + " onStroke=" + c2 + " offStroke(mirror)=" + c3
  + "\nread: id=" + (last.id == id) + " point=" + last.point + " building=" + last.building + " status=" + last.status
  + "\nline has strokes=" + line.Contains("\"strokes\":[[[0.3") + " len=" + line.Length;
