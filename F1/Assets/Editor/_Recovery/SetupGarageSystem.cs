#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using PitCrew.Garage;

public static class SetupGarageSystem
{
    const string Dir = "Assets/Data/Garage";

    [MenuItem("PitCrew/Garage/Setup F1Parts + Catalog")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(Dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            AssetDatabase.CreateFolder("Assets/Data", "Garage");
        }

        // D-080~082, D-088 가격·flavor 표
        var data = new (F1PartId id, string name, int cost, string flavor, string mech)[]
        {
            (F1PartId.Engine,     "엔진",     1000,  "엔진이 돌아왔다. 시동 소리가 살아있다.",       "최고속 +20%, 가속 +30%"),
            (F1PartId.Gearbox,    "변속기",   4000,  "변속기. 수동 변속이 가능해졌다.",            "K=다운시프트 / L=업시프트 수동 모드 unlock"),
            (F1PartId.Tires,      "타이어",   4000,  "타이어 4종 사용 가능. 잡 출발 전 선택.",       "드라이/웨트/소프트/하드 선택 + 영구 마모"),
            (F1PartId.Suspension, "서스펜션", 8000,  "서스 복원. 점프대·험지가 도시에 활성.",        "외곽 점프 구간 통과 가능 (heightMask)"),
            (F1PartId.Aero,       "에어로",   12000, "에어로 윙. 부스트가 가능하다.",              "Space = 부스트 (+50% 가속 2초, 재충전 5초)"),
            (F1PartId.Paint,      "도색",     25000, "도색 완료. 그랑프리 복귀 준비가 끝났다.",      "외형 변화 + 엔드게임 서사 트리거"),
        };

        var catalog = ScriptableObject.CreateInstance<F1RestoreCatalog>();
        AssetDatabase.CreateAsset(catalog, $"{Dir}/F1RestoreCatalog.asset");

        foreach (var d in data)
        {
            var part = ScriptableObject.CreateInstance<F1Part>();
            part.id = d.id;
            part.displayName = d.name;
            part.restoreCost = d.cost;
            part.flavorLine = d.flavor;
            part.mechanicDescription = d.mech;
            part.cutsceneSeconds = 2.5f;
            AssetDatabase.CreateAsset(part, $"{Dir}/F1Part_{d.id}.asset");
            catalog.parts.Add(part);
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = catalog;
        Debug.Log($"[SetupGarageSystem] 6 F1Part + Catalog 생성 완료: {Dir}");
    }
}
#endif
