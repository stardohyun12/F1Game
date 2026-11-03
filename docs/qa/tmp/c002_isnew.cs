if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return "busy";
return typeof(OpeningSequence).GetProperty("Caption") != null ? "idle new code" : "idle old code";
