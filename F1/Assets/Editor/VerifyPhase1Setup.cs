using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 1 setup 자동 진단 (세션 15 후반).
/// 도현이 메뉴 한 번 클릭 → 어디 누락됐는지 즉시 파악.
///
/// 메뉴: PitCrew/Phase 1/Verify Setup
/// </summary>
public static class VerifyPhase1Setup
{
    [MenuItem("PitCrew/Phase 1/Verify Setup")]
    public static void Verify()
    {
        var report = new List<string>();
        int ok = 0, fail = 0;

        Debug.Log("=== [Phase1 Verify] === Phase 1 setup 점검 시작 ===");

        // 1. 핵심 컴포넌트 존재
        Check(report, ref ok, ref fail, "CargoBox", Object.FindFirstObjectByType<CargoBox>() != null);
        Check(report, ref ok, ref fail, "OrderBoard", Object.FindFirstObjectByType<OrderBoard>() != null);
        Check(report, ref ok, ref fail, "ResultPanel", Object.FindFirstObjectByType<ResultPanel>() != null);
        Check(report, ref ok, ref fail, "RunController", Object.FindFirstObjectByType<RunController>() != null);
        Check(report, ref ok, ref fail, "DeliveryManager", Object.FindFirstObjectByType<DeliveryManager>() != null);
        Check(report, ref ok, ref fail, "FuelSystem", Object.FindFirstObjectByType<FuelSystem>() != null);
        Check(report, ref ok, ref fail, "ArcadeCarController", Object.FindFirstObjectByType<ArcadeCarController>() != null);
        Check(report, ref ok, ref fail, "FollowCamera", Object.FindFirstObjectByType<FollowCamera>() != null);
        Check(report, ref ok, ref fail, "IntegrityHUD", Object.FindFirstObjectByType<IntegrityHUD>() != null);

        // 2. 신규 시스템 (Phase 1 후반)
        Check(report, ref ok, ref fail, "UpgradeSystem", UpgradeSystem.Instance != null || Object.FindFirstObjectByType<UpgradeSystem>() != null);
        Check(report, ref ok, ref fail, "AudioManager", Object.FindFirstObjectByType<AudioManager>() != null);
        Check(report, ref ok, ref fail, "TireSystem (Day 20 통합)", Object.FindFirstObjectByType<TireSystem>() != null);
        Check(report, ref ok, ref fail, "CargoShakeSimulator", Object.FindFirstObjectByType<CargoShakeSimulator>() != null);

        // 3. 트렁크 시각 setup
        var simRoot = GameObject.Find("CargoSimRoot");
        Check(report, ref ok, ref fail, "CargoSimRoot", simRoot != null);
        var simCamRig = GameObject.Find("CargoSimCamRig");
        Check(report, ref ok, ref fail, "CargoSimCamRig (D-047 분리)", simCamRig != null);

        if (simRoot != null)
        {
            Check(report, ref ok, ref fail, "  TrunkFloor", simRoot.transform.Find("TrunkFloor") != null);
            Check(report, ref ok, ref fail, "  WallFront", simRoot.transform.Find("WallFront") != null);
            Check(report, ref ok, ref fail, "  WallBack", simRoot.transform.Find("WallBack") != null);
            Check(report, ref ok, ref fail, "  WallLeft", simRoot.transform.Find("WallLeft") != null);
            Check(report, ref ok, ref fail, "  WallRight", simRoot.transform.Find("WallRight") != null);

            var box = simRoot.transform.Find("Box");
            Check(report, ref ok, ref fail, "  Box", box != null);
            if (box != null)
            {
                bool floorCorrect = Mathf.Abs(box.localPosition.y - 0.325f) < 0.01f;
                Check(report, ref ok, ref fail, $"  Box localY = 0.325 (D-046 정착) [현재 {box.localPosition.y:F3}]", floorCorrect);
            }
        }

        // 4. CargoShakeSimulator 설정
        var shake = Object.FindFirstObjectByType<CargoShakeSimulator>();
        if (shake != null)
        {
            var so = new SerializedObject(shake);
            var enableRot = so.FindProperty("enableRotation");
            var matchRot = so.FindProperty("matchCarRotation");
            var rigField = so.FindProperty("simCamRig");
            Check(report, ref ok, ref fail, "  enableRotation = false (D-049 평면 보장)",
                enableRot != null && enableRot.boolValue == false);
            Check(report, ref ok, ref fail, "  matchCarRotation = true (D-048 yaw 매칭)",
                matchRot != null && matchRot.boolValue == true);
            Check(report, ref ok, ref fail, "  simCamRig 연결",
                rigField != null && rigField.objectReferenceValue != null);
        }

        // 5. 자산
        var orderCatalog = AssetDatabase.LoadAssetAtPath<OrderCatalog>("Assets/Data/Orders/OrderCatalogExpanded.asset");
        Check(report, ref ok, ref fail, "OrderCatalogExpanded.asset (8개 주문 풀)",
            orderCatalog != null && orderCatalog.jobs != null && orderCatalog.jobs.Count >= 8);

        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>("Assets/Data/Upgrades/UpgradeCatalog.asset");
        Check(report, ref ok, ref fail, "UpgradeCatalog.asset (5개 업그레이드)",
            upgradeCatalog != null && upgradeCatalog.upgrades != null && upgradeCatalog.upgrades.Count >= 5);

        // 6. 인스펙터 연결
        var orderBoard = Object.FindFirstObjectByType<OrderBoard>();
        if (orderBoard != null)
        {
            var so = new SerializedObject(orderBoard);
            var prop = so.FindProperty("catalog");
            bool linked = prop != null && prop.objectReferenceValue != null;
            Check(report, ref ok, ref fail, "OrderBoard.catalog 연결", linked);
        }

        var resultPanel = Object.FindFirstObjectByType<ResultPanel>();
        if (resultPanel != null)
        {
            var so = new SerializedObject(resultPanel);
            var prop = so.FindProperty("upgradeCatalog");
            bool linked = prop != null && prop.objectReferenceValue != null;
            Check(report, ref ok, ref fail, "ResultPanel.upgradeCatalog 연결", linked);
        }

        // 7. TrackLayout
        var trackLayout = AssetDatabase.LoadAssetAtPath<TrackLayout>("Assets/Data/ComplexTrackLayout.asset");
        if (trackLayout != null)
        {
            Check(report, ref ok, ref fail, $"ComplexTrackLayout.startYawDeg = 90 [현재 {trackLayout.startYawDeg}]",
                Mathf.Approximately(trackLayout.startYawDeg, 90f));
        }

        // 결과 출력
        var sep = "\n  ";
        var summary = $"Phase 1 Verify: {ok} OK / {fail} FAIL\n\n" + string.Join("\n", report);
        Debug.Log("[Phase1 Verify]\n" + summary);

        var icon = fail == 0 ? "✓" : "⚠";
        EditorUtility.DisplayDialog("Phase 1 Verify",
            $"{icon} {ok} OK / {fail} FAIL\n\n" +
            (fail == 0
                ? "모든 setup 정상.\nPlay 가능."
                : "FAIL 항목 Console 확인:\n  PitCrew/Phase 1/Full Integration Setup\n  PitCrew/Cargo/Setup Cargo View (CCTV)\n  메뉴 재실행 권장."),
            "확인");
    }

    static void Check(List<string> report, ref int ok, ref int fail, string label, bool condition)
    {
        if (condition)
        {
            ok++;
            report.Add($"✓ {label}");
        }
        else
        {
            fail++;
            report.Add($"✗ {label}");
        }
    }
}
