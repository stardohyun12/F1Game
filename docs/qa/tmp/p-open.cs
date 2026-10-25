var sc=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ComplexTrack.unity");
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
var f1=typeof(ArcadeCarController).GetField("fallRespawnY",bf); var f2=typeof(ArcadeCarController).GetField("safePointAboveFall",bf);
return "opened "+sc.name+" fallY="+f1?.GetValue(c)+" safeAbove="+f2?.GetValue(c);
