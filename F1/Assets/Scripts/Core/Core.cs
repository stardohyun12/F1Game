using UnityEngine;
using UnityEngine.Events;

// =====================================================================================
// PitCrew Phase 1 — DIP + Service Locator + Composition Root.
// 학습 노트: GDD/architecture/phase1-dip-service-locator.md
// 파일 맵:    GDD/architecture/file-map.md
//
// 단일 파일 정리: Unity AssetDatabase 부분 import 회피용 (세션 18).
// =====================================================================================

#region Interfaces (DIP)

public interface IDeliveryManager
{
    void ActivateSubset(int n);
    UnityEvent<DeliveryPoint> OnDeliveryCompleted { get; }
    UnityEvent OnAllCompleted { get; }
}

public interface IOverviewCamera
{
    bool IsInOverview { get; }
    void GoToOverview(System.Action onDone = null);
    void GoToFollow(System.Action onDone = null);
}

public interface IOrderBoard
{
    void Open();
    void Close();
}

public interface IResultPanel
{
    void Show(RunController ctrl);
    void Hide();
}

public interface IFuelSystem
{
    /// <summary>0~1 사이 연료량.</summary>
    float FuelLevel { get; }
    void Refill();
}

public interface ICargoBox
{
    float IntegrityPercent { get; }
    void SetCargoType(CargoType newType);
    void SetPacking(PackingTier tier);
}

public interface IUpgradeSystem
{
    int Wallet { get; }
    bool IsOwned(UpgradeEffect effect);
    void AddReward(int amount);
}

public interface ICargoShakeSimulator
{
    void SetBoxColor(Color c);
}

public interface IRunController
{
    bool RunActive { get; }
    float TimeRemaining { get; }
    void BeginRun(OrderJob job);
    void BeginRun(OrderJob job, PackingTier? packingOverride);
}

public interface IArcadeCarController
{
    bool IsDrifting { get; }
    float CurrentSpeedMps { get; }
    float MaxSpeedMps { get; }
    Transform Transform { get; }
}

public interface ITireSystem
{
    float Wear { get; }
    float GripMultiplier { get; }
    float SpeedMultiplier { get; }
}

public interface IGearSystem
{
    int CurrentGear { get; }
    string GearLabel { get; }
    event System.Action<int, int> OnShift;
}

#endregion

#region Service Locator (정적 레지스트리)

public static class Services
{
    public static IDeliveryManager Delivery { get; internal set; }
    public static IOverviewCamera Overview { get; internal set; }
    public static IOrderBoard Board { get; internal set; }
    public static IResultPanel Result { get; internal set; }
    public static IFuelSystem Fuel { get; internal set; }
    public static ICargoBox Cargo { get; internal set; }
    public static IUpgradeSystem Upgrade { get; internal set; }
    public static ICargoShakeSimulator Shake { get; internal set; }
    public static IRunController Run { get; internal set; }
    public static IArcadeCarController Car { get; internal set; }
    public static ITireSystem Tires { get; internal set; }
    public static IGearSystem Gears { get; internal set; }

    public static void Reset()
    {
        Delivery = null;
        Overview = null;
        Board = null;
        Result = null;
        Fuel = null;
        Cargo = null;
        Upgrade = null;
        Shake = null;
        Run = null;
        Car = null;
        Tires = null;
        Gears = null;
    }

    public static T Require<T>(T service, string callerHint = null) where T : class
    {
        if (service == null)
        {
            Debug.LogError($"[Services] {typeof(T).Name} 미등록 — GameContext가 씬에 있는지 확인. caller: {callerHint}");
        }
        return service;
    }
}

#endregion

#region GameContext (Composition Root)

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

    [Header("Validation")]
    [SerializeField] private bool autoFindMissingOnAwake = true;
    [SerializeField] private bool logRegistration = true;

    void Awake()
    {
        Services.Reset();
        if (autoFindMissingOnAwake) AutoFindMissing();

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
            Debug.Log($"[GameContext] {registered}/12 시스템 등록 완료.");
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
    }
}

#endregion
