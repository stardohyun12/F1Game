var sh = Shader.Find("PitCrew/SceneA7/Lit");
bool err = UnityEditor.ShaderUtil.ShaderHasError(sh);
var msgs = UnityEditor.ShaderUtil.GetShaderMessages(sh);
string m = "";
foreach (var x in msgs) m += x.severity + ":" + x.message + " @" + x.line + "; ";
return "fogField=" + (typeof(RoomDiorama).GetField("fog") != null) + " shaderErr=" + err + " msgs=" + m + " inv=" + WorldInvariantsProbe();
string WorldInvariantsProbe() {
  var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
  Transform room = null;
  foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (t.name == g.roomName && t.parent != null) { room = t; break; }
  Matrix4x4 box; A7ShadeGlobals.TryRoomBox(room, out box);
  Vector3 c; float low; bool ok = RoomDiorama.PickWindow(room, box, out c, out low);
  return ok + " c=" + c.ToString("F2") + " low=" + low.ToString("F2");
}
