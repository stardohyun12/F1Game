if (UnityEditor.EditorApplication.isCompiling) return "c";
return System.Type.GetType("AmbienceZones, Assembly-CSharp") != null ? "ready" : "missing";
