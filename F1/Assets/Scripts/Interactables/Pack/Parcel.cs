using TMPro;
using UnityEngine;

/// <summary>
/// D-290 받는 곳 선반 위 오늘 물건 하나(첫날 = 달걀 한 판). 위에 종이 꼬리표(받는 사람·배달지)가 서 있고 가까이 가야 보인다 — 세상 속 물건, 화면 UI 아님.
/// E = 집기(손에). 포장대 상자 위에서 E = 상자 안으로 내려놓기(물리로 떨어짐 — 작은 상자는 테두리에 걸린다).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Parcel : PackThing
{
    [Tooltip("world.yaml cargo 이름(성질·크기를 찾는 열쇠).")]
    public string cargoName = "달걀 한 판";
    public string recipient = "줄리아";
    public string destLabel = "찻집";
    [Tooltip("종이 꼬리표(TextMeshPro 3D) — 가까이 가면 켠다.")]
    public GameObject tag3d;
    [Tooltip("이 거리(m) 안이면 꼬리표가 보인다.")]
    public float tagShowDist = 2.4f;

    public Rigidbody Body { get; private set; }
    public bool OnShelf { get; internal set; } = true;
    public Vector3 Size => transform.lossyScale;   // 회색 대역 = 단위 상자 하나

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Body.isKinematic = true;
        Body.interpolation = RigidbodyInterpolation.Interpolate;
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var tmp = tag3d != null ? tag3d.GetComponentInChildren<TextMeshPro>() : null;
        if (tmp != null) tmp.text = recipient + "\n" + destLabel;
    }

    void Update()
    {
        if (tag3d == null) return;
        var cam = Camera.main;
        bool show = OnShelf && cam != null && Vector3.Distance(cam.transform.position, transform.position) <= tagShowDist;
        if (tag3d.activeSelf != show) tag3d.SetActive(show);
    }

    public override void Use(PackTable2 t) => t.UseParcel(this);
}
