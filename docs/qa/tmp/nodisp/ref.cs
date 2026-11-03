var m = typeof(UnityEditor.EditorGUIUtility).GetMethod("IsDisplayReferencedByCameras", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
bool r = (bool)m.Invoke(null, new object[] { 0 });
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return "display0Referenced=" + r + " warningShown=" + (!r) + " state=" + (d != null ? d.State.ToString() : "-") + " sunset=" + SunsetLook.Find().IsOn;
