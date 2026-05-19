using UnityEngine;

/// <summary>
/// Composition Root — 씬에 단 하나. 모든 시스템을 Awake에서 Services에 등록.
///
/// 단일 파일 분리 이유 (세션 19, 2026-05-12):
///   Core.cs 단일 파일에 인터페이스 12 + Services + GameContext 통합했을 때
///   Unity가 MonoScript asset의 첫 번째 타입(IDeliveryManager)만 인식 →
///   GameContext가 GUID resolve 실패 → m_Script broken stub.
///   MonoBehaviour 클래스는 자기 .cs 파일이 필요하다는 Unity Editor의 제약.
///   인터페이스/static class는 한 파일 OK, MonoBehaviour는 분리 필수.
///
/// 인스펙터 비워두면 AutoFindMissing이 FindFirstObjectByType으로 자동 채움.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameContext : MonoBehaviour
{
    [Header("Core Systems — 인스펙터에 드래그")]
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private OverviewCamera overviewCamera;
    [SerializeField] private OrderBoard orderBoard;
    [SerializeField] private ResultPanel resultPanel;
    [SerializeField] private FuelSystem fuelSystem;
    [SerializeField] private CargoBox cargoBox;
    [SerializeField] private UpgradeSystem upgradeSystem;
    [SerializeField] private CargoShakeSimulator shakeSimulator;
    [SerializeField] private RunController runController;
    [SerializeField] private ArcadeCarController carController;
    [SerializeField] private TireSystem tireSystem;
    [SerializeField] private GearSystem gearSystem;
    [SerializeField] private PitCrew.Packing.PackingScreen packingScreen;
    [SerializeField] private RoutePlanScreen routePlanScreen;
    [SerializeField] private PitCrew.Garage.F1RestoreSystem restoreSystem;
    [SerializeField] private PitCrew.Garage.BoostSystem boostSystem;
    [SerializeField] private PitCrew.Garage.TireWearStore tireWearStore;

    [Header("Validation")]
    [SerializeField] private bool autoFindMissingOnAwake = true;
    [SerializeField] private bool logRegistration = true;

    void Awake()
    {
        Services.Reset();
        if (autoFindMissingOnAwake) AutoFindMissing();

        // 2026-05-16 수치 audit — 모든 런 자동 측정
        RunMetricsRecorder.EnsureExists();

        Services.Delivery = deliveryManager;
        Services.Overview = overviewCamera;
        Services.Board = orderBoard;
        Services.Result = resultPanel;
        Services.Fuel = fuelSystem;
        Services.Cargo = cargoBox;
        Services.Upgrade = upgradeSystem;
        Services.Shake = shakeSimulator;
        Services.Run = runController;
        Services.Car = carController;
        Services.Tires = tireSystem;
        Services.Gears = gearSystem;
        Services.Packing = packingScreen;
        Services.RoutePlan = routePlanScreen;
        Services.Restore = restoreSystem;
        Services.Boost = boostSystem;
        Services.TireWear = tireWearStore;

        if (logRegistration)
        {
            int registered = 0;
            if (Services.Delivery != null) registered++;
            if (Services.Overview != null) registered++;
            if (Services.Board != null) registered++;
            if (Services.Result != null) registered++;
            if (Services.Fuel != null) registered++;
            if (Services.Cargo != null) registered++;
            if (Services.Upgrade != null) registered++;
            if (Services.Shake != null) registered++;
            if (Services.Run != null) registered++;
            if (Services.Car != null) registered++;
            if (Services.Tires != null) registered++;
            if (Services.Gears != null) registered++;
            if (Services.Packing != null) registered++;
            if (Services.RoutePlan != null) registered++;
            if (Services.Restore != null) registered++;
            if (Services.Boost != null) registered++;
            if (Services.TireWear != null) registered++;
            Debug.Log($"[GameContext] {registered}/17 시스템 등록 완료.");
        }
    }

    void OnDestroy() => Services.Reset();

    void AutoFindMissing()
    {
        if (deliveryManager == null) deliveryManager = FindFirstObjectByType<DeliveryManager>();
        if (overviewCamera == null) overviewCamera = FindFirstObjectByType<OverviewCamera>();
        if (orderBoard == null) orderBoard = FindFirstObjectByType<OrderBoard>();
        if (resultPanel == null) resultPanel = FindFirstObjectByType<ResultPanel>();
        if (fuelSystem == null) fuelSystem = FindFirstObjectByType<FuelSystem>();
        if (cargoBox == null) cargoBox = FindFirstObjectByType<CargoBox>();
        if (upgradeSystem == null) upgradeSystem = FindFirstObjectByType<UpgradeSystem>();
        if (shakeSimulator == null) shakeSimulator = FindFirstObjectByType<CargoShakeSimulator>();
        if (runController == null) runController = FindFirstObjectByType<RunController>();
        if (carController == null) carController = FindFirstObjectByType<ArcadeCarController>();
        if (tireSystem == null) tireSystem = FindFirstObjectByType<TireSystem>();
        if (gearSystem == null) gearSystem = FindFirstObjectByType<GearSystem>();
        if (packingScreen == null) packingScreen = FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
        if (restoreSystem == null) restoreSystem = FindFirstObjectByType<PitCrew.Garage.F1RestoreSystem>();
        if (boostSystem == null) boostSystem = FindFirstObjectByType<PitCrew.Garage.BoostSystem>();
        if (tireWearStore == null) tireWearStore = FindFirstObjectByType<PitCrew.Garage.TireWearStore>();
        // 채널 8: RoutePlanScreen은 씬에 없으면 자동 생성 (런타임에서 자체 구축)
        if (routePlanScreen == null)
        {
            routePlanScreen = FindFirstObjectByType<RoutePlanScreen>(FindObjectsInactive.Include);
            if (routePlanScreen == null)
            {
                var go = new GameObject("RoutePlanScreen");
                routePlanScreen = go.AddComponent<RoutePlanScreen>();
            }
        }
    }
}
