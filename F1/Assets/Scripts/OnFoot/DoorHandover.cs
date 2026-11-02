using System.Collections;
using UnityEngine;

/// <summary>
/// D-250 ③④(과제 20261102-1543): 배달지 문 앞 건네기 대역. 문짝(경첩으로 도는 판) + 받는 사람(캡슐) + 똑똑 소리.
/// 문을 두드리면 문이 열리고 받는 사람이 안에서 문턱 밖(standOut)으로 나온다. 건넨 뒤엔 다시 들어가고 문이 닫힌다.
/// 진행(대사·상자·돈)은 OnFootDirector 가 맡고, 여기는 세상 속 물건만 움직인다.
/// 설치: 에디트 모드 스크립트(docs/qa/tmp/handover/setup.cs)가 STOP_tea 아래에 만든다. 아트는 나중에 door·recipient 자리에.
/// </summary>
public class DoorHandover : MonoBehaviour
{
    [Tooltip("문짝 경첩(이 Transform 이 Y 축으로 돈다). 자식에 문짝 판(콜라이더 = [E] 두드리기 시선 대상).")]
    public Transform hinge;
    [Tooltip("받는 사람 대역(1.65 m 캡슐). 처음엔 문 안쪽에 숨어 있다.")]
    public Transform recipient;
    [Tooltip("받는 사람이 나와 서는 자리(문턱 밖). 앞(+Z)이 길 쪽.")]
    public Transform standOut;
    [Tooltip("받는 사람 이름 — 말풍선 이름 탭.")]
    public string recipientName = "줄리아";
    [Tooltip("문이 열리는 각도(도). 바깥으로 연다.")]
    public float openAngle = -95f;
    [Tooltip("똑똑 소리. 비면 코드로 만든 나무 두드림 두 번(대역).")]
    public AudioClip knockClip;
    [Range(0f, 1f)] public float knockVolume = 0.9f;
    [Header("대역 색(기본 재질에 MPB — 공유 재질 안 건드림)")]
    public Color doorColor = new Color(0.36f, 0.25f, 0.18f);
    public Color recipientColor = new Color(0.55f, 0.50f, 0.62f);
    [Tooltip("문 열면 보이는 안쪽 어둠(문틀 안 판).")]
    public Renderer opening;
    public Color openingColor = new Color(0.07f, 0.06f, 0.05f);

    AudioSource src;
    Vector3 insidePos;
    Quaternion hingeClosed;
    static AudioClip procKnock;

    public bool IsOpen { get; private set; }
    public bool Busy { get; private set; }
    /// <summary>[E] 칩 자리 = 문짝 가운데.</summary>
    public Vector3 DoorCenter
    {
        get
        {
            var r = hinge != null ? hinge.GetComponentInChildren<Renderer>() : null;
            return r != null ? r.bounds.center : transform.position + Vector3.up * 1.0f;
        }
    }
    /// <summary>상자를 받는 손 자리(가슴 앞).</summary>
    public Vector3 HandsPoint => recipient != null ? recipient.position + Vector3.up * 1.05f + recipient.forward * 0.32f : transform.position;

    void Awake()
    {
        if (hinge != null) { hingeClosed = hinge.localRotation; Tint(hinge, doorColor); }
        if (opening != null) Tint(opening.transform, openingColor);
        if (recipient != null) { Tint(recipient, recipientColor); insidePos = recipient.position; recipient.gameObject.SetActive(false); }
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false; src.spatialBlend = 0.6f; src.minDistance = 2f; src.maxDistance = 25f;
    }

    static void Tint(Transform root, Color c)
    {
        var mpb = new MaterialPropertyBlock();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c); mpb.SetColor("_Tint", c);
            r.SetPropertyBlock(mpb);
        }
    }

    public void PlayKnock()
    {
        var c = knockClip != null ? knockClip : ProcKnock();
        src.transform.position = DoorCenter;
        src.PlayOneShot(c, knockVolume);
    }

    /// <summary>문 열기 + 받는 사람이 나와서 face 쪽을 본다(sec 초).</summary>
    public IEnumerator Open(Vector3 face, float sec = 0.6f)
    {
        Busy = true;
        if (recipient != null)
        {
            recipient.position = insidePos;
            recipient.gameObject.SetActive(true);
        }
        Quaternion open = hingeClosed * Quaternion.Euler(0f, openAngle, 0f);
        Vector3 outPos = standOut != null ? standOut.position : insidePos;
        Vector3 to = face - outPos; to.y = 0f;
        Quaternion look = to.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(to.normalized, Vector3.up) : (recipient != null ? recipient.rotation : Quaternion.identity);
        for (float t = 0f; t < sec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / sec);
            if (hinge != null) hinge.localRotation = Quaternion.Slerp(hingeClosed, open, Mathf.Clamp01(k * 1.6f));   // 문이 먼저, 사람이 뒤따라
            if (recipient != null) { recipient.position = Vector3.Lerp(insidePos, outPos, Mathf.Clamp01(k * 1.25f - 0.25f)); recipient.rotation = look; }
            yield return null;
        }
        if (hinge != null) hinge.localRotation = open;
        if (recipient != null) recipient.position = outPos;
        IsOpen = true; Busy = false;
    }

    /// <summary>받는 사람이 들어가고 문이 닫힌다.</summary>
    public IEnumerator Close(float sec = 0.6f)
    {
        Busy = true;
        Quaternion open = hinge != null ? hinge.localRotation : Quaternion.identity;
        Vector3 from = recipient != null ? recipient.position : insidePos;
        for (float t = 0f; t < sec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / sec);
            if (recipient != null) recipient.position = Vector3.Lerp(from, insidePos, Mathf.Clamp01(k * 1.6f));
            if (hinge != null) hinge.localRotation = Quaternion.Slerp(open, hingeClosed, Mathf.Clamp01(k * 1.25f - 0.25f));
            yield return null;
        }
        if (hinge != null) hinge.localRotation = hingeClosed;
        if (recipient != null) recipient.gameObject.SetActive(false);
        IsOpen = false; Busy = false;
    }

    /// <summary>대역 똑똑: 나무 판을 손마디로 두 번 — 낮은 감쇠 사인(약 150 Hz) + 짧은 잡음 딸깍, 0.2초 간격.</summary>
    static AudioClip ProcKnock()
    {
        if (procKnock != null) return procKnock;
        const int rate = 44100;
        int n = (int)(rate * 0.55f);
        var d = new float[n];
        var rng = new System.Random(7);
        foreach (float at in new[] { 0.02f, 0.24f })
        {
            int s0 = (int)(at * rate);
            for (int i = 0; i < (int)(rate * 0.12f) && s0 + i < n; i++)
            {
                float t = i / (float)rate;
                float body = Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t * 38f) + 0.5f * Mathf.Sin(2f * Mathf.PI * 410f * t) * Mathf.Exp(-t * 70f);
                float click = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 400f);
                d[s0 + i] += 0.75f * body + 0.35f * click;
            }
        }
        procKnock = AudioClip.Create("KnockProcedural", n, 1, rate, false);
        procKnock.SetData(d, 0);
        return procKnock;
    }
}

/// <summary>
/// D-250 ④ 화물 상태 3단 [가설 — 도현 판정 전]. 깨져도 이야기는 이어지고 돈과 그 자리 반응만 달라진다.
/// 기준 = CargoBox.IntegrityPercent(HP / 최대 HP). 대사는 대역 글 — 실제 글은 도현이 정한다.
/// </summary>
public static class HandoverTier
{
    /// <summary>이 이상이면 멀쩡(옛 등급 "완벽" 80% 와 같은 선).</summary>
    public const float OkMin = 0.8f;
    /// <summary>이 이상이면 조금 상함, 밑이면 깨짐.</summary>
    public const float DentMin = 0.4f;
    public static readonly string[] Name = { "멀쩡", "조금 상함", "깨짐" };
    /// <summary>받는 돈 비율 — 깨져도 0 은 아니다(이야기는 이어짐, D-250 4).</summary>
    public static readonly float[] Pay = { 1.0f, 0.7f, 0.3f };
    public const string Greeting = "(문 열고 인사 자리)";
    public static readonly string[] Reaction = { "(멀쩡 반응 자리)", "(조금 상함 반응 자리)", "(깨짐 반응 자리)" };

    public static int Of(float integrity01) => integrity01 >= OkMin ? 0 : integrity01 >= DentMin ? 1 : 2;
}
