using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    public bool InTalk => talkLine >= 0;
    /// <summary>지금 자막 한 줄(대화 중이 아니면 "").</summary>
    public string Subtitle => InTalk ? Lines[talkLine].who + ": " + Lines[talkLine].text : "";
    public string TalkWho => InTalk ? Lines[talkLine].who : "";
    public string TalkText => InTalk ? Lines[talkLine].text : "";
    /// <summary>지금 줄이 시작된 때(unscaled) — 말풍선 한 글자씩.</summary>
    public float TalkLineAt => talkLineAt;
    /// <summary>말하는 사람 머리 꼭대기(렌더러 경계 맨 위).</summary>
    public Vector3 SpeakerTop => TopOf(speaker != null ? speaker : boss);
    /// <summary>지금 말하는 사람(사장 또는 문 앞 받는 사람).</summary>
    public Transform Speaker => speaker != null ? speaker : boss;
    Transform speaker;                       // null = 사장
    (string who, string text)[] curLines;    // null = 사장 대사
    System.Action<int> onTalkLine;           // 줄이 바뀔 때(새 줄 번호)
    System.Action onTalkEnd;

    int talkLine = -1;
    float talkLineAt;
    TextMeshProUGUI subText;
    GameObject subRoot;

    // ── 기능 2: 사장 대화 → 첫 주문 ──
    void BeginTalk() => BeginTalk(null, null, null, null);

    /// <summary>대화 시작. lines·who 가 null 이면 사장 대사. 문 앞 건네기도 같은 말풍선·시선 이동(D-240)을 쓴다.</summary>
    void BeginTalk((string who, string text)[] lines, Transform who, System.Action<int> onLine, System.Action onEnd)
    {
        curLines = lines; speaker = who; onTalkLine = onLine; onTalkEnd = onEnd;
        var sp = Speaker;
        talkLine = 0; talkLineAt = Time.unscaledTime;
        player.enabled = false;                                    // 듣는 동안 안 걷는다(시선은 그대로)
        Vector3 to = player.transform.position - sp.position; to.y = 0f;
        if (to.sqrMagnitude > 1e-4f) sp.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        SetPrompt("");
        ShowSubtitle();
        if (talkFocusSec > 0f) { if (talkFocus != null) StopCoroutine(talkFocus); talkFocus = StartCoroutine(FocusSpeaker()); }
        Mark(sp == boss ? "사장 대화 시작" : sp.name + " 대화 시작");
        if (sp == boss && opening != null) opening.OnTalkBegin();
    }

    /// <summary>
    /// UI 1판(D-240): 대화가 시작되면 카메라가 말하는 사람 쪽으로 talkFocusSec 동안 부드럽게 돈다(머리가 화면 가운데 근처, 말풍선은 그 위).
    /// 말하는 사람이 화면 밖이어도 이 회전으로 화면 안에 들어온다. 대화 동안 마우스 시선은 잠그고, 끝나면 그 자리에서 다시 플레이어 조작.
    /// </summary>
    IEnumerator FocusSpeaker()
    {
        footCam.HoldLook = true;
        float y0 = footCam.Yaw, p0 = footCam.Pitch;
        footCam.LookAt(SpeakerTop - Vector3.up * talkFocusDrop);
        float dy = Mathf.DeltaAngle(y0, footCam.Yaw), p1 = footCam.Pitch;
        Mark($"시선 이동 {dy:F1}° 옆 / {p1 - p0:F1}° 위아래, {talkFocusSec:F2}s");
        // 한 프레임이 길어도(첫 한글 글자 굽기 등) 최대 1/30초씩만 — 회전이 한 번에 건너뛰지 않게.
        for (float t = 0f; t < talkFocusSec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / talkFocusSec);
            footCam.Yaw = y0 + dy * k; footCam.Pitch = Mathf.Lerp(p0, p1, k);
            yield return null;
        }
        footCam.Yaw = y0 + dy; footCam.Pitch = p1;
        talkFocus = null;
    }

    /// <summary>개발·캡처용: 시선이 어디를 보든 바로 사장 대화를 시작한다(화면 밖 사장 → 시선 이동 확인).</summary>
    public bool ForceTalk()
    {
        if (State != Mode.OnFoot || InTalk || boss == null) return false;
        BeginTalk();
        return true;
    }

    Vector3 TopOf(Transform t)
    {
        if (t == null) return Vector3.zero;
        if (!anchorOffset.TryGetValue(t, out var off))
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) off = Vector3.up * 1.8f;
            else { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); off = new Vector3(b.center.x, b.max.y, b.center.z) - t.position; }
            anchorOffset[t] = off;
        }
        return t.position + off;
    }

    Vector3? CenterOf(DoorHinge d)
    {
        if (d == null) return null;
        var rs = d.GetComponentsInChildren<Renderer>();   // 문은 돌아가므로 매번(문 몇 개뿐)
        if (rs.Length == 0) return d.transform.position;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    void UpdateTalk(bool press)
    {
        if (!press || Time.unscaledTime - talkLineAt < 0.3f) return;   // 한 줄은 최소 0.3초(연타로 건너뛰기 방지)
        talkLine++; talkLineAt = Time.unscaledTime;
        if (talkLine < Lines.Length) { ShowSubtitle(); onTalkLine?.Invoke(talkLine); return; }
        talkLine = -1;
        player.enabled = true;
        if (talkFocus != null) { StopCoroutine(talkFocus); talkFocus = null; }
        footCam.HoldLook = false;                                  // 다시 플레이어 시선
        if (subRoot != null) subRoot.SetActive(false);
        if (curLines != null)
        {
            // 문 앞 건네기 등 사장 아닌 대화 — 사장 대화 끝 처리(목적지 시선)는 안 한다.
            var end = onTalkEnd;
            curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
            end?.Invoke();
            return;
        }
        Talked = true;
        Mark("사장 대화 끝" + (DepotFlow ? " → 보관소로" : ""));
        if (opening != null) { opening.OnTalkEnd(); Mark("카트 열쇠 받음"); SignalBus.Raise(Signal.KeyReceived); }
        if (destFocusSec > 0f && WalkDest != null && UseDot) destFocus = StartCoroutine(FocusDest(WalkDest));
    }

    void ShowSubtitle()
    {
        Mark("자막 " + Subtitle);
        if (subText == null) return;
        subText.text = "<b>" + Lines[talkLine].who + "</b>  " + Lines[talkLine].text +"  <size=70%><color=#FFFFFFAA>[E]</color></size>";
        subRoot.SetActive(true);
    }
}
