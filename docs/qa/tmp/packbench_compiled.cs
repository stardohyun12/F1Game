if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return "c";
var t = System.Type.GetType("PackBenchBuilder, PitCrew.Editor");
var b = System.Type.GetType("PitCrew.PackBench.PackBench, PitCrew");
return (t != null && b != null) ? "ok" : ("missing builder=" + (t != null) + " bench=" + (b != null));
