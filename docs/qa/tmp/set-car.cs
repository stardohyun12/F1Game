var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var so=new UnityEditor.SerializedObject(c);
so.FindProperty("driftVelocityAlign").floatValue=1.6f;
so.FindProperty("driftYawRateDeg").floatValue=130f;
so.FindProperty("driftYawResponse").floatValue=10f;
so.FindProperty("driftLongitudinalDrag").floatValue=0.15f;
so.ApplyModifiedProperties(); UnityEditor.EditorUtility.SetDirty(c);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
return "saved";
