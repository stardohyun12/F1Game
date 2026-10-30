if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) throw new System.Exception("F1Studio only");
UnityEditor.EditorApplication.ExitPlaymode();
return "stop requested";
