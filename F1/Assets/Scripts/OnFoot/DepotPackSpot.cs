using PitCrew.PackBench;
using UnityEngine;

/// <summary>
/// D-250(과제 20261102-1515): 택배 보관소 포장 자리. 대역 = 작업대 위 회색 상자 하나([E] 한 번으로 「포장됨」).
/// D-251(과제 20261102-1651): bench 가 있으면 Proto_PackBench 작업대를 그대로 쓴다 —
///   [E] 로 포장 시점(eye) → 싸고 닫고 테이프 → 들어서 흔들기 → 그 결과(덜그럭)가 화물 상태 시작값(StartIntegrity01).
///   흔들어 본 상자를 걷기 시점에서 [E] 로 들면(Take) Packed. 카트에 실으면(Load) 작업대에서 상자가 사라진다.
/// 작업대·상자·재료는 PackBenchBuilder.BuildDepot 가 만든 회색 대역 — 아트 납품 아님.
/// </summary>
public class DepotPackSpot : MonoBehaviour
{
    [Tooltip("포장되면 켜지는 것(테이프 띠 대역) — bench 없을 때만.")]
    [SerializeField] GameObject packedMark;

    [Header("D-251 작업대(비면 예전 대역)")]
    [SerializeField] PackBench bench;
    [SerializeField] Behaviour cursor;
    [Tooltip("포장 시점 눈 자리(작업대 앞 0.7 m 위, 48° 내려다봄).")]
    [SerializeField] Transform eye;
    [Tooltip("칠판에서 주문을 맡으면 작업대에 놓이는 화물(첫 주문 = 달걀 한 판).")]
    [SerializeField] GameObject cargo;

    [Header("포장 결과 → 화물 시작값(가설)")]
    [Tooltip("덜그럭(rattle) 이 값이면 rattleLoss 만큼 깎인다(선형, 넘으면 그대로).")]
    // 가설(11/02 실측): 맨바닥에 달걀판만 = rattle 1.23, 꽉 채움 = 0.00 → 2.0·0.40 이면 대충 75% / 꽉 100%
    [SerializeField] float rattleFull = 2f;
    [SerializeField] float rattleLoss = 0.40f;
    [Tooltip("판이 뒤집혀 있으면 더 깎임.")]
    [SerializeField] float notUprightLoss = 0.15f;
    [Tooltip("상자 밖으로 나간 물건 하나당.")]
    [SerializeField] float escapedLoss = 0.10f;
    [SerializeField] float minStart = 0.4f;

    bool packedLegacy;
    PackBox resultBox; int resultOpenCount; PackBox.ShakeResult result; bool hasResult;

    public bool HasBench => bench != null;
    /// <summary>작업대 모드: 상자를 들었다(=카트로 가져갈 수 있다). 대역 모드: [E] 한 번.</summary>
    public bool Packed => bench != null ? Taken : packedLegacy;
    public bool Taken { get; private set; }
    /// <summary>닫고 테이프 붙인 상자를 흔들어 봤고, 그 뒤로 다시 안 열었다.</summary>
    public bool Ready => bench != null && hasResult && resultBox != null && resultBox == bench.BenchBox
                         && resultBox.IsClosed && resultBox.Taped && resultBox.OpenCount == resultOpenCount && result.closed && result.taped;
    /// <summary>작업대 상자가 닫히고 테이프까지 됐는데 아직 안 흔들어 봄.</summary>
    public bool NeedsShake => bench != null && !Ready && bench.BenchBox != null && bench.BenchBox.IsClosed && bench.BenchBox.Taped;
    public PackBench Bench => bench;
    public PackBox.ShakeResult Result => result;
    public float StartIntegrity01 { get; private set; } = 1f;
    public Transform Eye => eye;

    void Awake()
    {
        if (packedMark != null) packedMark.SetActive(false);
        if (cargo != null) cargo.SetActive(false);
        if (bench != null) bench.enabled = false;
        if (cursor != null) cursor.enabled = false;
    }

    void OnEnable() { PackBench.Shaken += OnShaken; }
    void OnDisable() { PackBench.Shaken -= OnShaken; }

    void OnShaken(PackBox box, PackBox.ShakeResult r)
    {
        if (bench == null || box != bench.BenchBox) return;
        resultBox = box; resultOpenCount = box.OpenCount; result = r; hasResult = true;
        float loss = rattleLoss * Mathf.Clamp01(r.rattle / Mathf.Max(0.01f, rattleFull))
                   + (r.upright ? 0f : notUprightLoss) + escapedLoss * r.escaped;
        StartIntegrity01 = Mathf.Clamp(1f - loss, minStart, 1f);
        Debug.Log($"[DepotPack] 흔들기 결과 rattle={r.rattle:F2} upright={r.upright} escaped={r.escaped} closed={r.closed} taped={r.taped} → 화물 시작 {StartIntegrity01 * 100f:F0}%");
    }

    /// <summary>칠판에서 주문을 맡았다 — 화물을 작업대에 놓는다.</summary>
    public void ShowCargo() { if (cargo != null && !cargo.activeSelf) cargo.SetActive(true); }

    /// <summary>포장 시점 켜기/끄기(카메라 옮기기는 OnFootDirector).</summary>
    public void SetPacking(bool on, Camera cam = null)
    {
        if (bench == null) return;
        if (cam != null) bench.cam = cam;                                  // 걷기·주행과 같은 카메라(PackBenchCursor 가 첫 켜짐에 이걸 잡는다)
        bench.enabled = on;
        if (cursor != null) cursor.enabled = on;
    }

    public void Pack()
    {
        packedLegacy = true;
        if (packedMark != null) packedMark.SetActive(true);
    }

    /// <summary>걷기 시점에서 흔들어 본 상자를 든다 — 작업대 상자를 숨긴다(손에 든 상자는 OnFootDirector 의 HeldBox 대역).</summary>
    public void Take()
    {
        if (!Ready) return;
        Taken = true;
        resultBox.gameObject.SetActive(false);
    }

    /// <summary>카트에 실었다 — 대역 모드는 작업대에서 상자를 치운다. 작업대 모드는 Take 에서 이미 치웠다.</summary>
    public void Load() { if (bench == null) gameObject.SetActive(false); }

    /// <summary>[E] 칩 자리 = 상자 윗면 가운데(작업대 모드: 작업대 상자 또는 작업 자리 위).</summary>
    public Vector3 Top
    {
        get
        {
            if (bench != null)
            {
                var b = bench.BenchBox;
                if (b != null && b.gameObject.activeInHierarchy) return b.transform.position + Vector3.up * (b.RimY + 0.08f);
                return bench.benchSpot != null ? bench.benchSpot.position + Vector3.up * 0.25f : transform.position + Vector3.up * 0.3f;
            }
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return transform.position + Vector3.up * 0.3f;
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            return new Vector3(bb.center.x, bb.max.y + 0.05f, bb.center.z);
        }
    }
}
