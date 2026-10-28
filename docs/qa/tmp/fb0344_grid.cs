var sb = new System.Text.StringBuilder();
for (int z = -134; z <= -26; z++) {
  for (int x = 212; x <= 302; x++) {
    var hs = Physics.RaycastAll(new Vector3(x, 200f, z), Vector3.down, 400f);
    float ty = -99f; float roadY = -99f; bool blk = false;
    foreach (var h in hs) {
      var n = h.collider.name;
      if (n == "TownV6_Terrain") ty = h.point.y;
      else if (n.StartsWith("ROAD_")) roadY = Mathf.Max(roadY, h.point.y);
      else if (!h.collider.isTrigger) blk = true;
    }
    if (ty > -90f) foreach (var col in Physics.OverlapBox(new Vector3(x, Mathf.Max(ty, roadY) + 1.4f, z), new Vector3(.5f, 1f, .5f)))
      if (!col.isTrigger && col.name != "TownV6_Terrain" && !col.name.StartsWith("ROAD_")) blk = true;
    sb.Append(ty.ToString("F2")).Append(',').Append(roadY.ToString("F2")).Append(',').Append(blk ? 1 : 0).Append(' ');
  }
  sb.Append('\n');
}
System.IO.File.WriteAllText(@"D:/Github/F1Game/docs/qa/tmp/fb0344_grid.txt", sb.ToString());
return "ok";
