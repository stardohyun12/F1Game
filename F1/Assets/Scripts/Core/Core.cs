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
}

public interface IUpgradeSystem
{
    int Wallet { get; }
    bool IsOwned(UpgradeEffect effect);
    void AddReward(int amount);
    /// <summary>채널 5 — 적재함 칸 수 (기본 1, 업그레이드로 최대 3).</summary>
    int BoxCapacity { get; }
}

public interface ICargoShakeSimulator
{
    void SetBoxColor(Color c);
    /// <summary>포장한 화물 큐브 1개를 트렁크에 마운트 (additive — 여러 번 호출 = 여러 개). SimCam(트렁크 CCTV)이 비춤.</summary>
    void MountWrappedItem(UnityEngine.GameObject cube);
    /// <summary>마운트된 화물 전부 제거 (런 시작 시 이전 잔재 청소).</summary>
    void ClearMountedItems();
}

public interface IRunController
{
    bool RunActive { get; }
    float TimeRemaining { get; }
    void BeginRun(OrderJob job);
    void BeginRun(OrderJob job, System.Collections.Generic.List<UnityEngine.GameObject> packedItems);
}

public interface IRoutePlanScreen
{
    bool IsVisible { get; }
    void Show(OrderJob job);
    void Hide();
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

// IPackingScreen은 PitCrew.Packing 네임스페이스에 정의됨 (Packing/IPackingScreen.cs)

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
    public static IRoutePlanScreen RoutePlan { get; internal set; }
    public static IArcadeCarController Car { get; internal set; }
    public static ITireSystem Tires { get; internal set; }
    public static IGearSystem Gears { get; internal set; }
    public static PitCrew.Packing.IPackingScreen Packing { get; internal set; }

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
        RoutePlan = null;
        Car = null;
        Tires = null;
        Gears = null;
        Packing = null;
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

// GameContext는 Core/GameContext.cs로 분리 (세션 19, 2026-05-12).
// Unity는 한 .cs 파일의 첫 번째 타입만 MonoScript로 인식 → MonoBehaviour 클래스가
// 다른 타입(인터페이스 등)과 한 파일에 있으면 GUID resolve 실패 → m_Script broken.
// 인터페이스 12 + Services static은 한 파일 OK이지만 GameContext는 분리 필수.
