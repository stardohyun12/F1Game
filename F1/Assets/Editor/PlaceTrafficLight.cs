using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 교차로 신호등 자동 배치 (세션 16, 도현 #3 게임플레이).
///
/// 동작
/// - TrackLayout roadMask 스캔 → 4-방위 모두 도로인 cell = 4-way 교차로 후보.
/// - 후보 중 처음 N개에 TrafficLight + TrafficLightArea 배치.
/// - 신호등 위치: cell 중심에서 약간 빗겨 (정지선 표현).
/// - TrafficLightArea: 차량 진입 방향(가장 가까운 도로 방위) 직전에 BoxCollider trigger.
///
/// 메뉴: PitCrew/Track/Place Traffic Lights
/// </summary>
public static class PlaceTrafficLight
{
    const float CellSize = 6f;
    const int MaxLights = 2;     // Phase 1 단순화 — 너무 많으면 흐름 끊김

    [MenuItem("PitCrew/Track/Place Traffic Lights")]
    public static void Execute()
    {
        var layoutGuids = AssetDatabase.FindAssets("t:TrackLayout");
        if (layoutGuids.Length == 0) { Debug.LogError("[Traffic] TrackLayout 없음."); return; }
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(layoutGuids[0]));
        if (layout == null) { Debug.LogError("[Traffic] layout 로드 실패."); return; }

        Debug.Log($"[Traffic] === 신호등 배치 시작 (layout={layout.name}) ===");

        // 기존 root 제거 (재실행 안전)
        var existing = GameObject.Find("TrafficLights");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("TrafficLights");
        Undo.RegisterCreatedObjectUndo(root, "Place TrafficLights");

        // 4-way 교차로 검출
        int placed = 0;
        for (int r = 0; r < layout.Height && placed < MaxLights; r++)
        {
            for (int c = 0; c < layout.Width && placed < MaxLights; c++)
            {
                if (!layout.IsRoad(c, r)) continue;
                if (!layout.IsRoad(c + 1, r) || !layout.IsRoad(c - 1, r)) continue;
                if (!layout.IsRoad(c, r + 1) || !layout.IsRoad(c, r - 1)) continue;

                // 4-way 교차로 발견. 코너에 신호등 1개.
                CreateTrafficLight(root.transform, c, r, placed);
                placed++;
            }
        }

        Debug.Log($"[Traffic] 4-way 교차로 {placed}개 신호등 배치.");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        if (placed == 0)
        {
            Debug.LogWarning("[Traffic] 4-way 교차로 없음 — 수동 배치 또는 layout 검토 필요.");
            EditorUtility.DisplayDialog("Traffic Lights",
                "4-way 교차로(4방위 도로) 자동 검출 실패.\n인스펙터에서 TrafficLight 컴포넌트 직접 부착하세요.",
                "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("Traffic Lights",
                $"✓ {placed}개 신호등 배치.\n사이클: Red 5s → Yellow 1s → Green 5s.\nRed 진입 + 8km/h 초과 = 화물 데미지 15.",
                "확인");
        }
    }

    static void CreateTrafficLight(Transform parent, int c, int r, int idx)
    {
        // 신호등 본체 — cell 코너(SE) 위 5m 기둥 위치
        var lightGo = new GameObject($"TrafficLight_{c}_{r}");
        lightGo.transform.SetParent(parent, false);
        // cell 중심에서 SE 방향 약간 (정지선과 시각 분리)
        lightGo.transform.localPosition = new Vector3(
            (c + 0.5f) * CellSize + 1.8f,
            0f,
            (r - 0.5f) * CellSize + 1.8f
        );

        // 단순 기둥 (cube) — 시각용
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "Pole";
        pole.transform.SetParent(lightGo.transform, false);
        pole.transform.localScale = new Vector3(0.25f, 5.0f, 0.25f);
        pole.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        var poleCol = pole.GetComponent<Collider>();
        if (poleCol != null) Object.DestroyImmediate(poleCol);
        var poleShader = Shader.Find("Universal Render Pipeline/Lit");
        if (poleShader == null) poleShader = Shader.Find("Standard");
        var poleMat = new Material(poleShader) { name = "TrafficLight_PoleMat", color = new Color(0.20f, 0.20f, 0.22f) };
        pole.GetComponent<MeshRenderer>().sharedMaterial = poleMat;

        // TrafficLight 컴포넌트 (lamp 자동 생성)
        var light = lightGo.AddComponent<TrafficLight>();
        // 직교 신호 시뮬: 짝수 idx는 Red 시작, 홀수 idx는 Green 시작 (가시 변화)
        var so = new SerializedObject(light);
        var offsetProp = so.FindProperty("startOffsetSec");
        if (offsetProp != null)
        {
            offsetProp.floatValue = idx % 2 == 0 ? 0f : 6f;     // 6s = Red+Yellow → Green 진입
            so.ApplyModifiedProperties();
        }

        // 위반 검출 영역 — 신호등 앞쪽 cell (정지선)
        var areaGo = new GameObject("Area");
        areaGo.transform.SetParent(lightGo.transform, false);
        areaGo.transform.localPosition = new Vector3(-3f, 0.5f, 1f);
        var col = areaGo.AddComponent<BoxCollider>();
        col.size = new Vector3(4.5f, 1f, 1f);
        col.isTrigger = true;
        var area = areaGo.AddComponent<TrafficLightArea>();
        var areaSO = new SerializedObject(area);
        var lightProp = areaSO.FindProperty("light");
        if (lightProp != null) { lightProp.objectReferenceValue = light; areaSO.ApplyModifiedProperties(); }
    }
}
