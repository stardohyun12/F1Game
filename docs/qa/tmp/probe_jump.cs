var q = new Vector2(-151.5f, 348.5f); var s = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
  for (int i = 0; i < r.centerline.Length; i++) { var p = r.centerline[i]; if ((new Vector2(p.x, p.z) - q).magnitude < 6f) s.Append($"{r.roadId}[{i}/{r.centerline.Length}] ({p.x:F1},{p.y:F2},{p.z:F1}) w{r.width}\n"); }
return s.ToString();
