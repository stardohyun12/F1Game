if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) throw new System.Exception("F1Studio only");
UnityEditor.SessionState.SetString("studio.cap", "0|field_baseline|house_home");
UnityEditor.EditorApplication.EnterPlaymode();
return "baseline play requested";
