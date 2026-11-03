var m = typeof(ParcelBuild).GetMethod("Relief", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
return (UnityEditor.EditorApplication.isCompiling ? "compiling " : "idle ") + (m != null ? "hasRelief" : "noRelief") + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
