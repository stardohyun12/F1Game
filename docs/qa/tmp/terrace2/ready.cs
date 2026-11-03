return (UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating)?"busy":"ready "+typeof(TerraceBlock).GetMethod("Build").ToString();
