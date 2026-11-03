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

    [Header("D-290 포장 v3(도현 묘사) — 켜면 옛 작업대는 꺼 둔다(지우지 않음)")]
    [SerializeField] PackTable2 v3;
    [SerializeField] bool useV3 = true;
    public bool UseV3 => useV3 && v3 != null;
    public PackTable2 V3Table => UseV3 ? v3 : null;

    [Header("D-294 포장 2D 작업대 — 켜면 포장대 [E] 가 2D 보드(화면 = 작업대 윗면). 08 의 3D 손 흐름은 꺼 둔다(지우지 않음)")]
    [SerializeField] bool use2D = true;
    public bool Use2D => use2D && UseV3;
    /// <summary>봇·디버그: 2D 스위치를 런타임에 바꾼다(씬 값은 그대로).</summary>
    public void DebugUse2D(bool on) { use2D = on; if (v3 != null) v3.Board2D = Use2D; }
    PackBoard board2D;
    /// <summary>2D 보드(처음 부를 때 런타임에 만든다 — 씬 수정 없음). 스위치가 꺼졌으면 null.</summary>
    public PackBoard Board2D
    {
        get
        {
            if (!Use2D) return null;
            if (board2D == null)
            {
                board2D = gameObject.AddComponent<PackBoard>();
                board2D.Build(v3);
            }
            return board2D;
        }
    }
    /// <summary>보드 글자에 쓸 글꼴 — 08 물건 꼬리표와 같은 것(한글이 되는 글꼴). 그리기(Presentation/PackBoardView)는 Stage_Packing 이 붙인다.</summary>
    public TMPro.TMP_FontAsset BoardFont
    {
        get
        {
            var tag = v3 != null && v3.receive != null && v3.receive.today != null && v3.receive.today.Length > 0 && v3.receive.today[0].tag3d != null
                ? v3.receive.today[0].tag3d.GetComponentInChildren<TMPro.TextMeshPro>(true) : null;
            return tag != null ? tag.font : null;
        }
    }

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
    public bool Ready => UseV3 ? v3.Ready && !Taken : bench != null && hasResult && resultBox != null && resultBox == bench.BenchBox
                         && resultBox.IsClosed && resultBox.Taped && resultBox.OpenCount == resultOpenCount && result.closed && result.taped;
    /// <summary>작업대 상자가 닫히고 테이프까지 됐는데 아직 안 흔들어 봄.</summary>
    public bool NeedsShake => !UseV3 && bench != null && !Ready && bench.BenchBox != null && bench.BenchBox.IsClosed && bench.BenchBox.Taped;
    public PackBench Bench => bench;
    public PackBox.ShakeResult Result => UseV3 ? v3.Result : result;
    float startIntegrity = 1f;
    public float StartIntegrity01 { get => UseV3 ? v3.StartIntegrity01 : startIntegrity; private set => startIntegrity = value; }
    public Transform Eye => eye;

    void Awake()
    {
        if (packedMark != null) packedMark.SetActive(false);
        if (cargo != null) cargo.SetActive(false);
        if (bench != null) bench.enabled = false;
        if (cursor != null) cursor.enabled = false;
        // 옛 작업대 off(스위치): PackBench 컴포넌트는 Eye 에 있고 판·상자·재료는 형제다 — 자식 전부를 끈다(v3 는 형제 PackV3)
        if (UseV3) foreach (Transform c in transform) c.gameObject.SetActive(false);
        if (v3 != null) v3.Board2D = Use2D;
    }

    // ── 다음 날 아침(과제 20261102-1951): 처음 모습 스냅샷 → 되돌리기 ──
    struct Snap { public Transform t, parent; public Vector3 lp, ls; public Quaternion lr; public bool active; public Rigidbody rb; public bool kin, grav; }
    readonly System.Collections.Generic.List<Snap> snaps = new System.Collections.Generic.List<Snap>();
    readonly System.Collections.Generic.HashSet<Transform> snapSet = new System.Collections.Generic.HashSet<Transform>();

    void Start()
    {
        // Awake(숨기기)와 PackBox.Build 가 다 끝난 첫 모습 — 작업대 하위 전부
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            var rb = t.GetComponent<Rigidbody>();
            snaps.Add(new Snap { t = t, parent = t.parent, lp = t.localPosition, lr = t.localRotation, ls = t.localScale, active = t.gameObject.activeSelf,
                                 rb = rb, kin = rb != null && rb.isKinematic, grav = rb != null && rb.useGravity });
            snapSet.Add(t);
        }
    }

    /// <summary>하루가 바뀌었다: 손·상자·재료를 처음 자리로, 만든 것(구긴 종이·뽁뽁이·라벨)은 치우고, 흔들기 결과·포장 상태를 비운다.</summary>
    public void ResetForNextDay()
    {
        if (UseV3) { v3.ResetForNextDay(); if (board2D != null) { board2D.SetOn(false, null, null); board2D.ResetAll(); } Taken = false; return; }
        if (snaps.Count == 0) return;
        if (bench != null) { SetPacking(false); bench.ResetForNextDay(); }
        // 작업 중 새로 생긴 물건(뽁뽁이·구긴 신문지 등)
        foreach (var it in FindObjectsByType<PackItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (!snapSet.Contains(it.transform)) Destroy(it.gameObject);
        foreach (var sh in FindObjectsByType<BubbleSheet>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (!snapSet.Contains(sh.transform)) Destroy(sh.gameObject);
        foreach (var t in GetComponentsInChildren<Transform>(true)) if (t != null && !snapSet.Contains(t) && t.parent != null) Destroy(t.gameObject);
        foreach (var sn in snaps)
        {
            if (sn.t == null) continue;
            if (sn.t.parent != sn.parent) sn.t.SetParent(sn.parent, false);
            sn.t.localPosition = sn.lp; sn.t.localRotation = sn.lr; sn.t.localScale = sn.ls;
            if (sn.t.gameObject.activeSelf != sn.active) sn.t.gameObject.SetActive(sn.active);
            if (sn.rb != null)
            {
                sn.rb.isKinematic = sn.kin; sn.rb.useGravity = sn.grav;
                if (!sn.kin) { sn.rb.linearVelocity = Vector3.zero; sn.rb.angularVelocity = Vector3.zero; }
            }
        }
        Physics.SyncTransforms();
        foreach (var b in GetComponentsInChildren<PackBox>(true)) b.ResetForNextDay();
        Taken = false; hasResult = false; resultBox = null; resultOpenCount = 0; result = default;
        StartIntegrity01 = 1f; packedLegacy = false;
        if (packedMark != null) packedMark.SetActive(false);
        if (cargo != null) cargo.SetActive(false);
        gameObject.SetActive(true);
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
        if (bench == null || UseV3) return;
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
        if (UseV3) { Taken = true; v3.Take(); return; }
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
            if (UseV3) return v3.workSpot != null ? v3.workSpot.position + Vector3.up * 0.3f : transform.position + Vector3.up * 0.3f;
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
