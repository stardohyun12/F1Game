using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 세션 14 후반: 차에 박스를 안 붙이고 월드 외딴 위치에 박스 + SimCam + Point Light 배치.
/// SimCam → CargoRT → CargoView HUD 로 "트렁크 CCTV" 표시.
/// CargoShakeSimulator는 CargoBox G를 읽어 박스 Transform에 lerp.
/// </summary>
public static class SetupCargoView
{
    const string BoxFbxPath = "Assets/Model/kenney_car-kit/Models/FBX format/box.fbx";
    const string RenderTexturePath = "Assets/Resources/CargoRT.renderTexture";
    static readonly Vector3 SimWorldPos = new Vector3(0f, -500f, 0f);

    [MenuItem("PitCrew/Cargo/Setup Cargo View (CCTV)")]
    public static void Execute()
    {
        var rt = EnsureRT();

        // 1. CargoSimRoot
        var root = GameObject.Find("CargoSimRoot");
        if (root == null) root = new GameObject("CargoSimRoot");
        root.transform.position = SimWorldPos;
        root.transform.rotation = Quaternion.identity;

        // 2. Box — Kenney box.fbx
        Transform boxT = root.transform.Find("Box");
        if (boxT == null)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(BoxFbxPath);
            if (fbx == null) { Debug.LogError("[CargoView] box.fbx 없음: " + BoxFbxPath); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            if (inst == null) inst = Object.Instantiate(fbx);
            inst.name = "Box";
            inst.transform.SetParent(root.transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one * 2f;
            boxT = inst.transform;
            Debug.Log("[CargoView] Box 배치 완료.");
        }

        // 3. SimCam
        Transform camT = root.transform.Find("SimCam");
        if (camT == null)
        {
            var camGO = new GameObject("SimCam", typeof(Camera));
            camGO.transform.SetParent(root.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.2f, -2.8f);
            camGO.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 20f;
            cam.depth = -2f;
            cam.targetTexture = rt;
            camT = camGO.transform;
            Debug.Log("[CargoView] SimCam 생성 + RT 연결.");
        }
        else
        {
            var cam = camT.GetComponent<Camera>();
            if (cam != null && cam.targetTexture != rt) cam.targetTexture = rt;
        }

        // 4. SimLight — Point (격리용)
        Transform lightT = root.transform.Find("SimLight");
        if (lightT == null)
        {
            var lightGO = new GameObject("SimLight", typeof(Light));
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(0.6f, 1.0f, -1.2f);
            var l = lightGO.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 6f;
            l.intensity = 3.5f;
            l.color = new Color(1f, 0.95f, 0.85f);
            Debug.Log("[CargoView] Point Light 배치.");
        }

        // 5. Shake Simulator
        if (root.GetComponent<CargoShakeSimulator>() == null)
        {
            var sim = root.AddComponent<CargoShakeSimulator>();
            // boxTransform은 Awake에서 transform.Find("Box")로 자동 할당.
            Debug.Log("[CargoView] CargoShakeSimulator 부착.");
        }

        // 6. CargoView HUD (UIRoot 아래)
        var uiRootT = UIRoot.Ensure();
        var existing = Object.FindFirstObjectByType<CargoView>();
        if (existing == null)
        {
            var go = new GameObject("CargoView", typeof(RectTransform));
            go.transform.SetParent(uiRootT, false);
            var cv = go.AddComponent<CargoView>();
            // SerializedObject로 RT 참조 직접 할당 (Resources.Load fallback 보강)
            var so = new SerializedObject(cv);
            var prop = so.FindProperty("renderTexture");
            if (prop != null) { prop.objectReferenceValue = rt; so.ApplyModifiedProperties(); }
            Debug.Log("[CargoView] HUD 배치.");
        }
        else
        {
            var so = new SerializedObject(existing);
            var prop = so.FindProperty("renderTexture");
            if (prop != null && prop.objectReferenceValue != rt) { prop.objectReferenceValue = rt; so.ApplyModifiedProperties(); }
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CargoView] Setup 완료. Play로 CCTV 창 확인.");
    }

    static RenderTexture EnsureRT()
    {
        var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (existing != null) return existing;

        Directory.CreateDirectory("Assets/Resources");
        var rt = new RenderTexture(512, 384, 16, RenderTextureFormat.ARGB32);
        rt.name = "CargoRT";
        rt.filterMode = FilterMode.Bilinear;
        AssetDatabase.CreateAsset(rt, RenderTexturePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[CargoView] RT 신규 생성: " + RenderTexturePath);
        return rt;
    }
}
