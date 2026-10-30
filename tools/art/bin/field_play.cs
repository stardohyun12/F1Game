if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) throw new System.Exception("F1Studio only");
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "FieldMock") throw new System.Exception("FieldMock required");
UnityEditor.EditorApplication.EnterPlaymode();return "field play requested";
