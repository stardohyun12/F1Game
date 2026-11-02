using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// C-001 그레이박스: 정비소 안 정비 자리(회색 바닥 판). 상처 있는 카트가 자리 안에 있고, 사람이 내려서 자리 안에 서 있으면
/// OnFootDirector 가 [E 고치기] 칩을 띄우고, E 를 누르면 Repair → 상처 0 + DayLog 수리비 한 줄.
/// 씬에 저장하지 않는다 — Map_TownV6 에서 Play 시작 때 스스로 하나 만든다(정비소 문 안쪽 바닥, x 215.5~222 · z −57.5~−52.5).
/// 카트에 CartDamage 를 붙이는 것도 여기서(감독이 카트를 찾은 뒤 첫 Update).
/// </summary>
public class RepairSpot : MonoBehaviour
{
    public const int Cost = 300;   // 가설: 수리비 한 번 300원 — 도현 한 판 뒤 정함

    public static RepairSpot Active { get; private set; }

    [SerializeField] Vector3 size = new Vector3(6.5f, 3f, 5f);   // 바닥 판 가로·높이(판정)·세로

    OnFootDirector dir;

    const string SceneName = "Map_TownV6";
    static readonly Vector3 Center = new Vector3(218.75f, 26.73f, -55.0f);   // 정비소 문(x≈222) 안쪽, 장소 9 「정비소 안」 선 자리(216)가 안에 든다

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (SceneManager.GetActiveScene().name != SceneName || Object.FindFirstObjectByType<RepairSpot>() != null) return;
        var go = new GameObject("RepairSpot (C-001 greybox)");
        Vector3 p = Center;
        if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
        go.transform.position = p;
        go.AddComponent<RepairSpot>();
    }

    void Awake()
    {
        Active = this;
        // 회색 바닥 표시: 충돌체 없는 얇은 판(시선 레이·카트 바퀴에 안 걸린다)
        var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(mark.GetComponent<Collider>());
        mark.name = "RepairSpot_Floor (greybox)";
        mark.transform.SetParent(transform, false);
        mark.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        mark.transform.localScale = new Vector3(size.x, 0.02f, size.z);
        var r = mark.GetComponent<Renderer>();
        var m = new Material(r.sharedMaterial) { hideFlags = HideFlags.DontSave, color = new Color(0.45f, 0.45f, 0.45f) };
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void OnDestroy() { if (Active == this) Active = null; }

    void Update()
    {
        if (dir == null) dir = Object.FindFirstObjectByType<OnFootDirector>();
        if (dir != null && dir.Car != null && dir.Car.GetComponent<CartDamage>() == null) CartDamage.Of(dir.Car);
    }

    public bool Contains(Vector3 p)
    {
        var l = transform.InverseTransformPoint(p);
        return Mathf.Abs(l.x) <= size.x * 0.5f && Mathf.Abs(l.z) <= size.z * 0.5f && l.y > -1f && l.y < size.y;
    }

    /// <summary>사람(feet)과 카트가 둘 다 자리 안이고 카트에 상처가 있으면 true.</summary>
    public static bool Ready(Vector3 feet, Component car)
    {
        if (Active == null || car == null) return false;
        var d = car.GetComponent<CartDamage>();
        return d != null && d.Stage > 0 && Active.Contains(feet) && Active.Contains(car.transform.position);
    }

    /// <summary>고친다: 상처 0 + 그날 기록에 수리비. 고친 값(원)을 돌려준다.</summary>
    public static int Repair(Component car)
    {
        var d = car != null ? car.GetComponent<CartDamage>() : null;
        if (d == null || d.Stage == 0) return 0;
        int was = d.Stage;
        d.Clear();
        DayLog.RecordRepair(Cost);
        Debug.Log($"[RepairSpot] 수리 상처 {was}→0 · 수리비 −{Cost}");
        return Cost;
    }
}
