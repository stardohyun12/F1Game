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
            Debug.Log($"[GameContext] {registered}/8 시스템 등록 완료.");
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
    }
}

#endregion
