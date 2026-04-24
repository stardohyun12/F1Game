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

        // 2. Box — Primitive Cube + 갈색 material (Kenney에 crate/pallet prop 부재).
        // 매 실행마다 재생성(force recreate)해서 튜닝 변경 즉시 반영.
        Transform boxT = root.transform.Find("Box");
        if (boxT != null) Object.DestroyImmediate(boxT.gameObject);

        var boxGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boxGO.name = "Box";
        boxGO.transform.SetParent(root.transform, false);
        boxGO.transform.localPosition = Vector3.zero;
        boxGO.transform.localRotation = Quaternion.identity;
        boxGO.transform.localScale = new Vector3(0.9f, 0.65f, 0.6f); // 배송박스 비율
        var boxCol = boxGO.GetComponent<Collider>();
        if (boxCol != null) Object.DestroyImmediate(boxCol);        // 시뮬 전용, 물리 불필요

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var boxMat = new Material(shader) { name = "CargoBoxMat" };
        boxMat.color = new Color(0.62f, 0.42f, 0.22f);              // 골판지 갈색
        boxGO.GetComponent<MeshRenderer>().sharedMaterial = boxMat;
        boxT = boxGO.transform;
        Debug.Log("[CargoView] Cube Box 배치 (갈색 골판지).");

        // 3. SimCam — 매번 tuning 값 덮어쓰기.
        Transform camT = root.transform.Find("SimCam");
        GameObject camGO;
        if (camT == null)
        {
            camGO = new GameObject("SimCam", typeof(Camera));
            camGO.transform.SetParent(root.transform, false);
            camT = camGO.transform;
            Debug.Log("[CargoView] SimCam 생성.");
        }
        else
        {
            camGO = camT.gameObject;
        }
        // 탑다운 시점 — 트렁크 뚜껑 열고 내려다보는 느낌 (78° 틸트로 약간 입체감).
        camT.localPosition = new Vector3(0f, 2.6f, -0.2f);
        camT.localRotation = Quaternion.Euler(78f, 0f, 0f);
        var simCam = camGO.GetComponent<Camera>();
        simCam.clearFlags = CameraClearFlags.SolidColor;
        simCam.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 1f); // 어두운 트렁크 내부
        simCam.fieldOfView = 55f;
        simCam.nearClipPlane = 0.3f;
        simCam.farClipPlane = 20f;
        simCam.depth = -2f;
        simCam.targetTexture = rt;

        // 4. SimLight — Point (격리용). 매 실행 tuning 덮어쓰기.
        Transform lightT = root.transform.Find("SimLight");
        GameObject lightGO;
        if (lightT == null)
        {
            lightGO = new GameObject("SimLight", typeof(Light));
            lightGO.transform.SetParent(root.transform, false);
            lightT = lightGO.transform;
            Debug.Log("[CargoView] Point Light 생성.");
        }
        else
        {
            lightGO = lightT.gameObject;
        }
        lightT.localPosition = new Vector3(0.3f, 2.0f, 0.1f); // 탑다운 시점에 맞춰 박스 위쪽
        var simLight = lightGO.GetComponent<Light>();
        simLight.type = LightType.Point;
        simLight.range = 7f;
        simLight.intensity = 4f;
        simLight.color = new Color(1f, 0.95f, 0.85f);

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
