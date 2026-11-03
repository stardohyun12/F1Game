using UnityEngine;

/// <summary>
/// 과제 0404(맵 코멘트 03:44·03:46): A7 셰이더(PitCrew/SceneA7/Lit · Library/A7LitPoint)는 그늘 색·3단 문턱을 전역값
/// _A7Shade·_A7T·_A7Mid 에서 읽는다. 그 값을 넣는 건 A7LightToggles 뿐인데, 그건 해(A7_Sun)까지 새로 만들어 씬 빛을 바꾼다.
/// 해가 이미 있는 씬(Map_TownV6)에는 이것만 둔다 — 전역값 셋만 넣고 빛은 안 건드린다. 없으면 그늘 면이 검정(0,0,0)이 된다.
/// 기본값 = Scene_A7_Light 의 A7LightToggles 값과 같은 결과.
/// </summary>
[ExecuteAlways]
public sealed class A7ShadeGlobals : MonoBehaviour
{
    [Range(0.3f, 1f)] public float ambientAmount = 0.62f;
    public Color skyFogColor = new Color(0.8f, 0.87f, 0.93f);
    public Color shadeTeal = new Color(0.55f, 0.76f, 0.80f);
    [Range(0f, 1f)] public float tealAmount = 0.5f;
    [Range(0f, 1f)] public float brightAbove = 0.3f;
    [Range(-0.5f, 0.5f)] public float sideAbove = 0.02f;
    [Range(0f, 1f)] public float sideLevel = 0.6f;

    [Header("실내 도트(과제 20261104-0501) — 방 상자 안 A7 면만 바다 화면 문법으로")]
    [Tooltip("켜면 이름이 roomName 인 오브젝트(집 방 소품 묶음)를 둘러싼 벽 상자 안에서 SceneA7/Lit 이 텍스처를 걸러 읽고, 빛 3단·근거리 안개·밝기 단+디더를 쓴다.")]
    public bool indoorDot = true;
    public string roomName = "ROOM_2F";
    [Range(4, 32)] public int indoorLevels = 10;
    [Range(0f, 0.5f)] public float indoorFogMax = 0.15f;
    public float indoorFogEnd = 6f;
    [Tooltip("텍스처 거르는 탭 간격(텍셀). 0 = 안 거름")] [Range(0f, 4f)] public float indoorFilterTexels = 0.5f;
    [Tooltip("국소 평균 쪽으로 대비 줄임")] [Range(0f, 1f)] public float indoorMeanPull = 0.15f;
    [Tooltip("밝기 단 끝 디더 폭(0..1). 0.15 에서 바다 화면 디더 비율과 비슷(실측 방 0.027 vs 바다 0.021)")] [Range(0f, 1f)] public float indoorDitherWidth = 0.15f;
    [Tooltip("단 자를 때 텍스처 결 빼기. 0 = 결째로 잘라 얼룩이 단으로 뭉개짐")] [Range(0f, 1f)] public float indoorGrainKeep = 0f;

    static readonly int ShadeId = Shader.PropertyToID("_A7Shade"), TId = Shader.PropertyToID("_A7T"), MidId = Shader.PropertyToID("_A7Mid");
    static readonly int InBoxId = Shader.PropertyToID("_A7InBox"), InPId = Shader.PropertyToID("_A7InP"), InQId = Shader.PropertyToID("_A7InQ");

    /// <summary>지금 넣은 방 상자(월드 → -1..1). 상자가 없으면 zero.</summary>
    public Matrix4x4 IndoorBox { get; private set; }

    void OnEnable() => Apply();
    void OnValidate() { if (isActiveAndEnabled) Apply(); }
    void OnDisable() => Shader.SetGlobalVector(InPId, Vector4.zero);

    public void Apply()
    {
        var shade = Color.Lerp(skyFogColor, shadeTeal, tealAmount) * Mathf.Max(0.3f, ambientAmount);
        Shader.SetGlobalColor(ShadeId, new Color(shade.r, shade.g, shade.b, 1f));
        Shader.SetGlobalVector(TId, new Vector4(brightAbove, sideAbove, 0f, 0f));
        Shader.SetGlobalFloat(MidId, sideLevel);
        ApplyIndoor();
    }

    void ApplyIndoor()
    {
        IndoorBox = Matrix4x4.zero;
        var room = indoorDot ? FindRoom(roomName) : null;
        if (room == null || !TryRoomBox(room, out var box)) { Shader.SetGlobalVector(InPId, Vector4.zero); return; }
        IndoorBox = box;
        Shader.SetGlobalMatrix(InBoxId, box);
        Shader.SetGlobalVector(InPId, new Vector4(1f, indoorLevels, indoorFogMax, indoorFogEnd));
        Shader.SetGlobalVector(InQId, new Vector4(indoorFilterTexels, indoorMeanPull, indoorDitherWidth, indoorGrainKeep));
    }

    /// <summary>불변 검사용: 실내 도트가 켜져 있으면 방 상자가 잡혔고 전역값이 들어갔는지.</summary>
    public bool IndoorReady => !indoorDot || (IndoorBox != Matrix4x4.zero && Shader.GetGlobalVector(InPId).x > 0.5f);

    static Transform FindRoom(string n)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == n && t.parent != null) return t;
        return null;
    }

    /// <summary>
    /// 방 상자 = 방 소품 묶음 가운데에서 집 축(부모 = 집) ±x·±z·위·아래로 레이를 쏴 처음 닿는 벽·바닥·천장까지(+6 cm).
    /// 좌표를 손으로 적지 않는다 — 집이 옮겨지거나 돌아가도 따라간다. 한 방향이라도 못 닿으면 상자 없음(꺼짐).
    /// </summary>
    public static bool TryRoomBox(Transform room, out Matrix4x4 worldToBox)
    {
        worldToBox = Matrix4x4.zero;
        var house = room.parent;
        bool any = false; var b = new Bounds();
        foreach (var r in room.GetComponentsInChildren<Renderer>())
        {
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) return false;
        Vector3 o = house.InverseTransformPoint(b.center);
        var dirs = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var hi = -lo;
        foreach (var d in dirs)
        {
            // 옆으로 ±0.6 m 띄운 레이 셋의 가운데 값 — 소품 하나에 막히거나 문틈 하나로 빠져도 벽을 고른다
            var side = Mathf.Abs(d.x) > 0.5f ? Vector3.forward : Vector3.right;
            var hits = new float[3]; int k = 0;
            foreach (float s in new[] { -0.6f, 0f, 0.6f })
            {
                var wo = house.TransformPoint(o + side * s); var wd = house.TransformDirection(d);
                if (!Physics.Raycast(wo, wd, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) return false;
                hits[k++] = hit.distance / house.lossyScale.x;
            }
            System.Array.Sort(hits);
            float best = hits[1];
            var p = o + d * (best + 0.06f);
            lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
        }
        var c = (lo + hi) * 0.5f; var e = (hi - lo) * 0.5f;
        // 집 로컬 → 상자 -1..1
        worldToBox = Matrix4x4.Scale(new Vector3(1f / e.x, 1f / e.y, 1f / e.z)) * Matrix4x4.Translate(-c) * house.worldToLocalMatrix;
        return true;
    }
}
