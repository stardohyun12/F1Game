using UnityEditor;
using UnityEngine;

// CargoShakeSimulator의 씬 인스턴스 파라미터를 새 default로 재설정.
public static class TuneCargoShake
{
    public static void Execute()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");

        var sims = Object.FindObjectsByType<CargoShakeSimulator>(FindObjectsSortMode.None);
        Debug.Log($"[TuneCargo] found {sims.Length} CargoShakeSimulator(s)");

        foreach (var s in sims)
        {
            var so = new SerializedObject(s);
            var sf = so.FindProperty("staticFrictionG");
            var kf = so.FindProperty("kineticFriction");
            var ac = so.FindProperty("accelScale");
            Debug.Log($"[TuneCargo] '{s.gameObject.name}' before: sf={sf.floatValue}, kf={kf.floatValue}, ac={ac.floatValue}");
            sf.floatValue = 0.12f;
            kf.floatValue = 1.0f;
            ac.floatValue = 1.5f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(s);
            Debug.Log($"[TuneCargo] '{s.gameObject.name}' after:  sf=0.12, kf=1.0, ac=1.5");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[TuneCargo] saved.");
    }
}
