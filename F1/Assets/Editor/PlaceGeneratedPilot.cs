using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// D-095 파일럿: 항구 블록(캡처 지점 60,96 주변)의 Kenney 창고 4채를 끄고
/// 생성기(tools/asset-gen/proc_building.py) 건물 4채를 같은 셀에 세운다. 앞면이 도로(x=60)를 보게 회전.
/// 되돌리기: PitCrew/Art/Pilot Revert (생성 건물 제거 + 원래 건물 켬). 씬 저장 안 함.
/// </summary>
public static class PlaceGeneratedPilot
{
    const string RootName = "GeneratedPilot";
    // (원래 건물 이름, 생성 FBX, 앞면이 봐야 할 방향)
    static readonly (string orig, string fbx, Vector3 face)[] Slots =
    {
        ("B_W_9_16",  "Assets/Model/Generated/harbor_0.fbx", Vector3.right),
        ("B_W_9_17",  "Assets/Model/Generated/harbor_1.fbx", Vector3.right),
        ("B_W_11_16", "Assets/Model/Generated/harbor_2.fbx", Vector3.left),
        ("B_W_11_17", "Assets/Model/Generated/harbor_3.fbx", Vector3.left),
    };

    /// <summary>메시 바운즈의 비대칭(계단·차양·발코니가 앞으로 튀어나옴)으로 앞면 축을 찾는다.</summary>
    static Vector3 FrontAxisLocal(GameObject prefab)
    {
        var mf = prefab.GetComponentInChildren<MeshFilter>();
        var b = mf.sharedMesh.bounds;
        float dx = Mathf.Abs(b.max.x) - Mathf.Abs(b.min.x);
        float dz = Mathf.Abs(b.max.z) - Mathf.Abs(b.min.z);
        if (Mathf.Abs(dz) >= Mathf.Abs(dx)) return dz > 0 ? Vector3.forward : Vector3.back;
        return dx > 0 ? Vector3.right : Vector3.left;
    }

    [MenuItem("PitCrew/Art/Pilot Place Generated Harbor Block")]
    public static void Execute()
    {
        var root = GameObject.Find(RootName) ?? new GameObject(RootName);
        var log = new List<string>();
        foreach (var s in Slots)
        {
            var orig = GameObject.Find(s.orig);
            if (orig == null) { log.Add($"{s.orig} 없음"); continue; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(s.fbx);
            if (prefab == null) { log.Add($"{s.fbx} 없음"); continue; }
            orig.SetActive(false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "Gen_" + System.IO.Path.GetFileNameWithoutExtension(s.fbx);
            inst.transform.SetParent(root.transform, true);
            var front = FrontAxisLocal(prefab);
            // 로컬 front 를 월드 face 로 돌리는 yaw
            float yaw = Vector3.SignedAngle(front, s.face, Vector3.up);
            inst.transform.SetPositionAndRotation(new Vector3(orig.transform.position.x, 0f, orig.transform.position.z), Quaternion.Euler(0f, yaw, 0f));
            log.Add($"{s.orig} → {inst.name} front={front} yaw={yaw:0}");
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Pilot] " + string.Join(" / ", log));
    }

    [MenuItem("PitCrew/Art/Pilot Revert")]
    public static void Revert()
    {
        var root = GameObject.Find(RootName);
        if (root != null) Object.DestroyImmediate(root);
        foreach (var s in Slots)
        {
            // 비활성 오브젝트는 Find 로 안 잡힘 → Buildings 자식 순회
            var b = GameObject.Find("Buildings");
            if (b == null) break;
            foreach (Transform t in b.transform) if (t.name == s.orig) t.gameObject.SetActive(true);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Pilot] 되돌림");
    }
}
