using UnityEditor;
using UnityEngine;

public static class FixCarFriction
{
    public static void Execute()
    {
        // 저마찰 PhysicMaterial 자산 생성 (또는 재사용)
        const string path = "Assets/Materials/CarLowFriction.physicMaterial";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial("CarLowFriction")
            {
                dynamicFriction = 0.05f,
                staticFriction = 0.05f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Average,
            };
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Friction] Created {path}");
        }
        else
        {
            mat.dynamicFriction = 0.05f;
            mat.staticFriction = 0.05f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            Debug.Log($"[Friction] Updated existing {path}");
        }

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        var box = car.GetComponent<BoxCollider>();
        box.sharedMaterial = mat;
        EditorUtility.SetDirty(box);

        // 씬에 변경 저장 (Edit Mode 시)
        if (!Application.isPlaying)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[Friction] Assigned to car BoxCollider. sharedMaterial={box.sharedMaterial?.name}, static={mat.staticFriction}, combine={mat.frictionCombine}");
    }
}
