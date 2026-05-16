using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 2026-05-16 수치 audit — 한 런 자동 측정 + CSV append.
///
/// 도현 결정 막힘 진단(#4): 한 런 평균 시간/보상/손상 수치 0 → 디자인 결정 추측만.
/// 도현이 운전만 5런 돌리면 표 1장 나옴. 그 표가 차고/페이싱 결정의 base.
///
/// 측정 항목:
///   - elapsedSec, distance_m, avgSpeed_kmh
///   - reward, integrity%, completedCount/ordersCount
///   - walletBefore/After
///   - success flag
///
/// CSV 위치: Application.persistentDataPath/runs.csv
///   Windows: %USERPROFILE%/AppData/LocalLow/&lt;Company&gt;/&lt;Product&gt;/runs.csv
///   "PitCrew/Metrics/Open CSV" 메뉴로 바로 열기.
///
/// 자동 등록: GameContext에서 컴포넌트 추가하거나, 어디든 한 번 add 하면 DontDestroyOnLoad로 살아남음.
/// </summary>
[DisallowMultipleComponent]
public class RunMetricsRecorder : MonoBehaviour
{
    static RunMetricsRecorder _instance;

    RunController run;
    Transform carT;
    Vector3 lastCarPos;
    float distanceAccum;
    int walletBefore;
    int runIndex;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        TryHook();
    }

    void Update()
    {
        if (run == null || !run.RunActive) return;
        if (carT == null)
        {
            var c = Services.Car;
            if (c != null) { carT = c.Transform; lastCarPos = carT.position; }
            return;
        }
        // 거리 누적 (수평만 — y 점프 무시)
        Vector3 cur = carT.position;
        Vector3 d = cur - lastCarPos;
        d.y = 0f;
        distanceAccum += d.magnitude;
        lastCarPos = cur;
    }

    void TryHook()
    {
        if (run != null) return;
        run = Services.Run as RunController;
        if (run == null) return;
        run.OnRunStarted -= OnRunStarted;
        run.OnRunStarted += OnRunStarted;
        run.OnRunEnded -= OnRunEnded;
        run.OnRunEnded += OnRunEnded;
    }

    void OnRunStarted(RunController r)
    {
        distanceAccum = 0f;
        carT = null; // Update에서 Services.Car 재캐시
        var up = Services.Upgrade as UpgradeSystem;
        walletBefore = up != null ? up.Wallet : 0;
    }

    void OnRunEnded(RunController r)
    {
        runIndex++;
        var up = Services.Upgrade as UpgradeSystem;
        int walletAfter = up != null ? up.Wallet : 0;
        float t = Mathf.Max(0.001f, r.LastElapsed);
        float avgSpeedKmh = (distanceAccum / t) * 3.6f;

        var fuel = Services.Fuel;
        // FuelSystem.FuelPercent 같은 게 있으면 사용. 없으면 -1.
        float fuelPct = -1f;
        if (fuel != null)
        {
            var t_fuel = fuel.GetType();
            var prop = t_fuel.GetProperty("FuelPercent") ?? t_fuel.GetProperty("Percent");
            if (prop != null && prop.PropertyType == typeof(float))
                fuelPct = (float)prop.GetValue(fuel);
        }

        string line = string.Join(",",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            runIndex.ToString(),
            r.LastRunSuccess ? "1" : "0",
            r.CurrentOrders.Count.ToString(),
            r.CompletedCount.ToString(),
            r.LastElapsed.ToString("F1"),
            distanceAccum.ToString("F0"),
            avgSpeedKmh.ToString("F1"),
            r.LastReward.ToString(),
            (r.LastIntegrityPercent * 100f).ToString("F0"),
            walletBefore.ToString(),
            walletAfter.ToString(),
            fuelPct.ToString("F0")
        );

        string path = CsvPath();
        bool exists = File.Exists(path);
        try
        {
            using (var sw = new StreamWriter(path, append: true, Encoding.UTF8))
            {
                if (!exists)
                    sw.WriteLine("timestamp,run#,success,orders,completed,elapsedSec,distance_m,avgSpeed_kmh,reward,integrity%,walletBefore,walletAfter,fuelPct");
                sw.WriteLine(line);
            }
            Debug.Log($"[Metrics] 런 #{runIndex}: {r.LastElapsed:F0}s, {distanceAccum:F0}m, {avgSpeedKmh:F0}km/h, ₩{r.LastReward} ({(r.LastIntegrityPercent*100f):F0}%) → {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Metrics] CSV write 실패: {e.Message}");
        }
    }

    public static string CsvPath() => Path.Combine(Application.persistentDataPath, "runs.csv");

    /// <summary>외부 진입 — GameContext 등에서 호출. 멱등.</summary>
    public static void EnsureExists()
    {
        if (_instance != null) return;
        var go = new GameObject("RunMetricsRecorder");
        go.AddComponent<RunMetricsRecorder>();
    }
}
