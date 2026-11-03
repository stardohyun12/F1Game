using System.Collections;
using UnityEngine;

/// <summary>
/// C-002 오프닝(그레이박스): 방에서 깨어남 → 집을 나섬 → 정비소 사장 대화(카트 열쇠) → 카트 탑승.
/// OnFootDirector 가 시작 때 같은 오브젝트에 붙이고 단계를 알려 준다. 이 컴포넌트는 단계 값과 깨어나는 시선만 맡는다.
/// 두 번째 한 판 메모(D-284): 안내 글·집 앞 시선 돌림·표지판은 없다 — "억지로 강제적으로 가게 한 느낌"(SPEC §1 강제 안내 금지).
/// 집 앞에서 정비소가 보이는지는 건물 배치가 정한다.
/// QA: <c>Opening.Step</c> 이 Room → Front → Talk → Key → Cart 순으로 바뀐다.
/// </summary>
public class OpeningSequence : MonoBehaviour
{
    public enum Stage { None, Room, Front, Talk, Key, Cart }

    [Tooltip("깨어나서 침대를 내려다보는 시간(초) — 그 뒤 달력으로 고개를 돌린다.")]
    [SerializeField] float bedHoldSec = 0.9f;
    [Tooltip("침대에서 방 물건(달력)으로 시선이 옮겨 가는 시간(초).")]
    [SerializeField] float wakeSec = 1.4f;

    public static OpeningSequence Current { get; private set; }
    public Stage Step { get; private set; } = Stage.None;
    public bool HasKey { get; private set; }
    /// <summary>단계가 바뀐 기록(QA 한 줄).</summary>
    public string Trail { get; private set; } = "";

    // D-287 6단계: 감독 대신 쓰는 것만 받는다(사람·달력·침대·「지금 걷기인가」).
    OnFootPlayer player;
    RoomCalendar calendar;
    RoomBed bed;
    System.Func<bool> onFoot;
    float spawnY;

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    /// <summary>새 게임 시작(감독 Start 끝). 방에서 눈을 뜬다.</summary>
    public void Begin(OnFootPlayer player, RoomCalendar calendar, RoomBed bed, System.Func<bool> onFoot)
    {
        this.player = player; this.calendar = calendar; this.bed = bed; this.onFoot = onFoot; Current = this;
        HasKey = false; Trail = ""; Step = Stage.None;
        spawnY = player.transform.position.y;
        Set(Stage.Room);
        StopAllCoroutines();
        StartCoroutine(Wake());
    }

    public void OnTalkBegin() { if (Step == Stage.Front || Step == Stage.Room) Set(Stage.Talk); }
    public void OnTalkEnd() { if (Step == Stage.Talk) { HasKey = true; Set(Stage.Key); } }
    // 첫 과제(SPEC §11-7): 사장 대화 없이 타도 오프닝을 끝낸다.
    public void OnCartEnter() { if (Step == Stage.Key || Step == Stage.Front || Step == Stage.Room) Set(Stage.Cart); }

    void Update()
    {
        if (player == null || !onFoot() || Step != Stage.Room) return;
        // 집을 나섰다 = 2층에서 내려왔고 머리 위가 하늘(지붕·천장 없음).
        Vector3 p = player.transform.position;
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

    /// <summary>눈을 뜬다: 방금 일어난 침대를 내려다보다가 방 달력 쪽으로 고개를 돌린다 — 침대 → 방 안 → 달력 [E] 순으로 보인다.</summary>
    IEnumerator Wake()
    {
        var cam = player.Cam;
        if (cam == null) yield break;
        cam.HoldLook = true;
        yield return null;                       // 순간이동한 몸·눈 자리가 한 프레임 뒤에 잡힌다 — 그 뒤에 방향을 잰다
        // 달력 방향은 눈이 수평일 때 먼저 잰다 — 침대를 내려다본 자세(눈 자리가 앞·아래로 옮겨 감)에서 재면 조준점이 달력 아래로 빠져 [E] 가 안 떴다.
        // 달력 판 가운데(충돌체). PromptPoint([E] 칩 자리)는 판 위라 시선 레이가 벽에 맞는다.
        if (calendar != null) cam.LookAt(calendar.transform.position);
        float y1 = cam.Yaw, p1 = cam.Pitch;
        // 침대 머리맡 쪽(침대 자리에서 0.3 m 서쪽) — 이불·베개가 화면 가운데.
        if (bed != null) cam.LookAt(bed.transform.position + new Vector3(-0.3f, 0f, 0f));
        float y0 = cam.Yaw, p0 = cam.Pitch;
        for (float t = 0f; t < bedHoldSec; t += Time.unscaledDeltaTime) { cam.Yaw = y0; cam.Pitch = p0; yield return null; }
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
