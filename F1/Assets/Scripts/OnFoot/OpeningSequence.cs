using System.Collections;
using UnityEngine;

/// <summary>
/// C-002 오프닝(그레이박스): 방에서 깨어남 → 집을 나섬 → 정비소 사장 대화(카트 열쇠) → 카트 탑승.
/// OnFootDirector 가 시작 때 같은 오브젝트에 붙이고 단계를 알려 준다. 이 컴포넌트는 단계 값과
/// 사장 대화 전 목적지 표식(정비소)만 맡는다 — 대화·탑승·표식 그리기는 감독·DotScreenUI 그대로.
/// QA: <c>Opening.Step</c> 이 Room → Front → Talk → Key → Cart 순으로 바뀐다.
/// </summary>
public class OpeningSequence : MonoBehaviour
{
    public enum Stage { None, Room, Front, Talk, Key, Cart }

    [Tooltip("깨어날 때 천장에서 방 물건(달력)으로 시선이 내려오는 시간(초).")]
    [SerializeField] float wakeSec = 1.2f;
    [Tooltip("사장에게 이 거리(m, 수평) 안이면 정비소 표식을 끈다 — 사장 [E] 칩과 겹치지 않게.")]
    [SerializeField] float bossNearM = 5f;

    public static OpeningSequence Current { get; private set; }
    public Stage Step { get; private set; } = Stage.None;
    public bool HasKey { get; private set; }
    /// <summary>단계가 바뀐 기록(QA 한 줄).</summary>
    public string Trail { get; private set; } = "";

    OnFootDirector dir;
    DeliveryPoint garageDest;
    float spawnY;

    void OnDestroy() { if (Current == this) Current = null; }

    /// <summary>새 게임 시작(감독 Start 끝). 방에서 눈을 뜬다.</summary>
    public void Begin(OnFootDirector d)
    {
        dir = d; Current = this;
        if (garageDest == null && d.Boss != null)
        {
            // 꺼진 DeliveryPoint — 표식 자리로만 쓴다(depotDest 와 같은 방식). 꺼 둬서 배달 목록·트리거에 안 걸린다.
            var go = new GameObject("DP_opening_garage");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            go.transform.position = d.Boss.position;
            garageDest = go.AddComponent<DeliveryPoint>();
            garageDest.Bind(null, "정비소");
        }
        HasKey = false; Trail = ""; Step = Stage.None;
        spawnY = d.Player.transform.position.y;
        Set(Stage.Room);
        StartCoroutine(Wake());
    }

    /// <summary>사장 대화 전 걷는 동안 표식이 가리킬 곳 — 집을 나선 뒤에만, 사장 바로 앞에선 끔.</summary>
    public DeliveryPoint PreTalkDest
    {
        get
        {
            if (garageDest == null || dir == null || Step != Stage.Front) return null;
            Vector3 a = dir.Player.transform.position, b = garageDest.transform.position;
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < bossNearM ? null : garageDest;
        }
    }

    public void OnTalkBegin() { if (Step == Stage.Front || Step == Stage.Room) Set(Stage.Talk); }
    public void OnTalkEnd() { if (Step == Stage.Talk) { HasKey = true; Set(Stage.Key); } }
    public void OnCartEnter() { if (Step == Stage.Key) Set(Stage.Cart); }

    void Update()
    {
        if (dir == null || Step != Stage.Room || dir.State != OnFootDirector.Mode.OnFoot) return;
        // 집을 나섰다 = 2층에서 내려왔고 머리 위가 하늘(지붕·천장 없음).
        Vector3 p = dir.Player.transform.position;
        if (p.y < spawnY - 1.5f && !Physics.Raycast(p + Vector3.up * 1.9f, Vector3.up, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            Set(Stage.Front);
    }

    void Set(Stage s)
    {
        if (Step == s) return;
        Step = s;
        Trail += (Trail.Length > 0 ? "→" : "") + s;
        Debug.Log($"[Opening] {s} ({Trail})");
    }

    /// <summary>눈을 뜬다: 누운 시선(천장)에서 방 달력 쪽으로 내려온다 — 달력 [E] 가 첫 화면에 걸린다.</summary>
    IEnumerator Wake()
    {
        var cam = dir.Player.Cam;
        if (cam == null) yield break;
        cam.HoldLook = true;
        float y0 = cam.Yaw;
        cam.Pitch = -75f;
        yield return null;                       // 순간이동한 몸·눈 자리가 한 프레임 뒤에 잡힌다 — 그 뒤에 달력 방향을 잰다
        // 달력 판 가운데(충돌체). PromptPoint([E] 칩 자리)는 판 위라 시선 레이가 벽에 맞는다.
        if (dir.Calendar != null) cam.LookAt(dir.Calendar.transform.position);
        float y1 = cam.Yaw, p1 = cam.Pitch, p0 = -75f;
        for (float t = 0f; t < wakeSec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / wakeSec);
            cam.Yaw = Mathf.LerpAngle(y0, y1, k); cam.Pitch = Mathf.Lerp(p0, p1, k);
            yield return null;
        }
        cam.Yaw = y1; cam.Pitch = p1;
        cam.HoldLook = false;
    }
}

/// <summary>QA 짧은 이름: <c>Opening.Step</c>.</summary>
public static class Opening
{
    public static OpeningSequence.Stage Step => OpeningSequence.Current != null ? OpeningSequence.Current.Step : OpeningSequence.Stage.None;
    public static string Trail => OpeningSequence.Current != null ? OpeningSequence.Current.Trail : "";
}
