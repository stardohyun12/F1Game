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
        // D-046 (도현 세션 15): 박스를 floor 위에 정확히 정착시킴.
        // Box scale.y = 0.65 → half = 0.325. Floor top = 0 (floor scale.y=0.04, localY=-0.02).
        // 박스 bottom = floor top = 0이 되도록 박스 center y = 0.325.
        boxGO.transform.localPosition = new Vector3(0f, 0.325f, 0f);
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

        // 3. SimCam Rig — D-047 (도현 세션 15): CargoSimRoot 외부에 worldspace fixed.
        // 이유: trunk가 차량 회전 따라 기울어지는데 카메라도 함께 기울면 시점 자체가 기울어
        // "평지에서 박스 기울어 보임" 발생. 카메라를 외부 fixed로 두면 평지=평면, 사면=기울기 명확.
        GameObject camRig = GameObject.Find("CargoSimCamRig");
        if (camRig == null)
        {
            camRig = new GameObject("CargoSimCamRig");
            Debug.Log("[CargoView] CargoSimCamRig 생성 (worldspace fixed).");
        }
        camRig.transform.position = SimWorldPos + new Vector3(0f, 2.5f, -0.8f);
        camRig.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

        // 기존 SimCam이 root 자식이었다면 제거 (이전 setup 호환).
        var oldRootCam = root.transform.Find("SimCam");
        if (oldRootCam != null) Object.DestroyImmediate(oldRootCam.gameObject);

        Transform camT = camRig.transform.Find("SimCam");
        GameObject camGO;
        if (camT == null)
        {
            camGO = new GameObject("SimCam", typeof(Camera));
            camGO.transform.SetParent(camRig.transform, false);
            camT = camGO.transform;
            Debug.Log("[CargoView] SimCam 생성 (CargoSimCamRig 자식).");
        }
        else
        {
            camGO = camT.gameObject;
        }
        camT.localPosition = Vector3.zero;
        camT.localRotation = Quaternion.identity;

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
        var simComp = root.GetComponent<CargoShakeSimulator>();
        if (simComp == null)
        {
            simComp = root.AddComponent<CargoShakeSimulator>();
            Debug.Log("[CargoView] CargoShakeSimulator 부착.");
        }
        // D-048: simCamRig 자동 할당 + D-049: enableRotation/matchCarRotation 강제 reset.
        var simSO = new SerializedObject(simComp);
        var rigProp = simSO.FindProperty("simCamRig");
        if (rigProp != null) rigProp.objectReferenceValue = camRig.transform;
        // 인스펙터 어쩌다 enableRotation=true 켰어도 메뉴 재실행 시 false 강제 (트렁크 평면 보장).
        var enableRotProp = simSO.FindProperty("enableRotation");
        if (enableRotProp != null) enableRotProp.boolValue = false;
        var matchRotProp = simSO.FindProperty("matchCarRotation");
        if (matchRotProp != null) matchRotProp.boolValue = true;
        simSO.ApplyModifiedProperties();
        Debug.Log("[CargoView] CargoShakeSimulator: simCamRig 연결 + enableRotation=false + matchCarRotation=true 강제.");

        // 5.5. 트렁크 시각화 (도현 세션 15 — 박스 미끄러짐 공간 표현)
        // CargoShakeSimulator.maxOffset 0.5 매칭. 박스 좌우/앞뒤 ±0.5m 미끄러짐 공간.
        SetupTrunkWalls(root.transform, shader);

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

    /// <summary>
    /// 트렁크 4벽 + 바닥 시각화 (도현 세션 15).
    /// CargoShakeSimulator.maxOffset 0.5 + 박스 0.45 half = 트렁크 1.0 half (= 2.0m × 2.0m 정사각형).
    /// 박스가 벽에 부딪히고 그 자리에 머무는 모습이 CCTV에 명확히 보이도록.
    /// </summary>
    static void SetupTrunkWalls(Transform root, Shader shader)
    {
        const float halfSize = 1.0f;       // 트렁크 half (x/z)
        const float wallHeight = 0.7f;     // 박스 0.65 + 약간
        const float wallThickness = 0.04f;
        const float floorThickness = 0.04f;

        // 색: 어두운 갈색 (트렁크 내부)
        var wallMat = new Material(shader) { name = "TrunkWallMat" };
        wallMat.color = new Color(0.20f, 0.16f, 0.13f);
        var floorMat = new Material(shader) { name = "TrunkFloorMat" };
        floorMat.color = new Color(0.13f, 0.10f, 0.08f);

        // 기존 벽 제거 (force recreate)
        var existingWalls = new[] { "TrunkFloor", "WallFront", "WallBack", "WallLeft", "WallRight" };
        foreach (var n in existingWalls)
        {
            var t = root.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // 바닥
        CreateBox(root, "TrunkFloor",
            new Vector3(0f, -floorThickness * 0.5f, 0f),
            new Vector3(halfSize * 2f, floorThickness, halfSize * 2f),
            floorMat);

        // 4벽
        CreateBox(root, "WallFront",
            new Vector3(0f, wallHeight * 0.5f, halfSize + wallThickness * 0.5f),
            new Vector3(halfSize * 2f + wallThickness * 2f, wallHeight, wallThickness),
            wallMat);

        CreateBox(root, "WallBack",
            new Vector3(0f, wallHeight * 0.5f, -halfSize - wallThickness * 0.5f),
            new Vector3(halfSize * 2f + wallThickness * 2f, wallHeight, wallThickness),
            wallMat);

        CreateBox(root, "WallLeft",
            new Vector3(-halfSize - wallThickness * 0.5f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, halfSize * 2f),
            wallMat);

        CreateBox(root, "WallRight",
            new Vector3(halfSize + wallThickness * 0.5f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, halfSize * 2f),
            wallMat);

        Debug.Log("[CargoView] 트렁크 4벽 + 바닥 배치 (2.0m × 2.0m).");
    }

    static void CreateBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col); // 시뮬 전용, 물리 불필요
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
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
