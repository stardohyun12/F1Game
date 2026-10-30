using UnityEngine;

/// <summary>
/// 기능 1: 문짝 하나(house_home 의 DOOR_*)를 경첩 축으로 0.4 초 부드럽게 열고 닫는다.
/// 경첩·닫힌 자세는 설치 메뉴(OnFootSetup)가 메시에서 찾아 적어 둔다 — 모두 부모(집) 좌표.
/// 문 BoxCollider 는 같은 Transform 에 붙어 있어 같이 돈다. 열 때는 사람 반대쪽으로 민다.
/// </summary>
public class DoorHinge : MonoBehaviour
{
    [Tooltip("경첩 축이 지나는 점(부모 좌표).")]
    public Vector3 hingeLocal;
    [Tooltip("닫힌 자세(부모 좌표). 설치 시점 자세에서, 반쯤 열린 채 모델링된 문은 문틀에 맞춘 각도.")]
    public Vector3 closedLocalPos;
    public Quaternion closedLocalRot = Quaternion.identity;
    [Tooltip("문틀 맞춤으로 돌린 각(도). 0 = 놓인 그대로가 닫힌 자세.")]
    public float fitYaw;
    public float openAngle = 95f;
    public float duration = 0.4f;
    [Tooltip("문짝 가운데(부모 좌표) — 가까운 문 찾기·안내용.")]
    public Vector3 centerLocal;
    [Tooltip("닫혔을 때 경첩 → 문 끝 방향(부모 좌표, 수평).")]
    public Vector3 leafDirLocal = Vector3.right;
    public bool configured;
    [Tooltip("문과 같이 도는 따로 된 물체(손잡이 KNOB_* 등). 모델링 자세 그대로 문에 붙어 간다.")]
    public Transform[] riders = new Transform[0];

    public bool IsOpen { get; private set; }
    public bool Moving => t < 1f;

    float angle, from, to, t = 1f;
    Vector3[] riderPos; Quaternion[] riderRot;

    void Awake()
    {
        if (!configured) { enabled = false; return; }
        riderPos = new Vector3[riders.Length]; riderRot = new Quaternion[riders.Length];
        for (int i = 0; i < riders.Length; i++)
            if (riders[i] != null) { riderPos[i] = transform.InverseTransformPoint(riders[i].position); riderRot[i] = Quaternion.Inverse(transform.rotation) * riders[i].rotation; }
        Apply(0f);
    }

    Vector3 Up => transform.parent != null ? transform.parent.InverseTransformDirection(Vector3.up) : Vector3.up;

    void Apply(float a)
    {
        angle = a;
        Quaternion r = Quaternion.AngleAxis(a, Up);
        transform.localPosition = hingeLocal + r * (closedLocalPos - hingeLocal);
        transform.localRotation = r * closedLocalRot;
        if (riderPos == null) return;
        for (int i = 0; i < riders.Length; i++)
            if (riders[i] != null) riders[i].SetPositionAndRotation(transform.TransformPoint(riderPos[i]), transform.rotation * riderRot[i]);
    }

    public Vector3 CenterWorld
    {
        get
        {
            Vector3 c = hingeLocal + Quaternion.AngleAxis(angle, Up) * (centerLocal - hingeLocal);
            return transform.parent != null ? transform.parent.TransformPoint(c) : c;
        }
    }

    /// <summary>열려 있으면 닫고, 닫혀 있으면 from(사람 자리) 반대쪽으로 연다.</summary>
    public void Toggle(Vector3 from)
    {
        if (IsOpen) { Animate(0f); IsOpen = false; return; }
        Vector3 p = transform.parent != null ? transform.parent.InverseTransformPoint(from) : from;
        Vector3 toPerson = p - hingeLocal; toPerson -= Vector3.Project(toPerson, Up);
        // 문 끝이 돌아가는 방향: +각 = Up 축 기준 회전. 끝이 사람에게서 멀어지는 쪽을 고른다.
        Vector3 endPlus = Quaternion.AngleAxis(openAngle, Up) * leafDirLocal;
        Vector3 endMinus = Quaternion.AngleAxis(-openAngle, Up) * leafDirLocal;
        float sign = Vector3.Dot(endPlus, toPerson) <= Vector3.Dot(endMinus, toPerson) ? 1f : -1f;
        Animate(sign * openAngle);
        IsOpen = true;
    }

    void Animate(float target) { from = angle; to = target; t = 0f; }

    void Update()
    {
        if (t >= 1f) return;
        t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, duration));
        Apply(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
    }
}
