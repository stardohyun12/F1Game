using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ── 개발 디버그 창(D-248, DebugWindow)이 부르는 것 — 정식 빌드엔 없다 ──
    public bool DebugReady => player != null && cam != null && State != Mode.Starting;
    public FishingSpot DebugSpot => spots.Count > 0 ? spots[0] : null;
    public Vector3 DebugCartSpot => cartSpot != null ? cartSpot.position : Vector3.zero;

    /// <summary>대화·앉기·카트에서 빠져나와 걷기 몸을 켠다(위치는 그대로). 카메라·시야각은 각자 정리한다.</summary>
    void DebugLeaveState()
    {
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        if (InTalk)
        {
            talkLine = -1;
            curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
            player.enabled = true;
            if (talkFocus != null) { StopCoroutine(talkFocus); talkFocus = null; }
            footCam.HoldLook = false;
            if (subRoot != null) subRoot.SetActive(false);
        }
        if (State == Mode.Board)
        {
            if (boardMove != null) { StopCoroutine(boardMove); boardMove = null; }
            orderBoard.SetHint(false);
            cam.transform.SetPositionAndRotation(boardEyePos, boardEyeRot);
            player.enabled = true;
        }
        if (State == Mode.Pack)
        {
            if (packMove != null) { StopCoroutine(packMove); packMove = null; }
            packSpot.Bench.ReleaseAll(); packSpot.SetPacking(false); packLeaving = false;
            cam.transform.SetPositionAndRotation(packEyePos, packEyeRot);
            player.enabled = true;
        }
        if (State == Mode.Seated)
        {
            var s = seatedAt; seatedAt = null;
            if (s != null) s.Stand();
            player.gameObject.SetActive(true);
        }
        else if (State == Mode.InCart)
        {
            SetCarControl(false);
            if (carRb != null) { carRb.linearVelocity = Vector3.zero; carRb.angularVelocity = Vector3.zero; }
            player.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 걷기 상태로 feet 에 순간이동(땅은 레이로 맞춘다). carPos 가 있으면 카트도 그 자리로. 결과 한 줄(땅 높이·카트)을 돌려준다.
    /// </summary>
    public string DebugTeleport(Vector3 feet, float yaw, Vector3? carPos, float carYaw)
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        DebugLeaveState();
        string g = "땅 못 찾음(표 높이 그대로)";
        if (Physics.Raycast(feet + Vector3.up * 1.5f, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        { feet = hit.point + Vector3.up * 0.05f; g = $"땅 {hit.collider.name} y={hit.point.y:F2}"; }
        string c = "카트 그대로";
        if (carPos.HasValue) { ParkCar(carPos.Value, carYaw); c = "카트 " + car.transform.position.ToString("F1"); }
        player.Teleport(feet, yaw);
        EnterFoot();
        footCam.Yaw = yaw; footCam.Pitch = 0f;
        Mark($"디버그 이동 {feet.ToString("F1")} yaw{yaw:F0} / {g} / {c}");
        return $"{feet.ToString("F1")} {g} {c}";
    }

    /// <summary>카트를 walker 주변 빈자리(오른쪽·왼쪽·뒤·앞 순)에 놓을 후보. 다 막히면 null(= 카트는 그대로).</summary>
    public Vector3? DebugFindCarSpot(Vector3 feet, float yaw)
    {
        var fw = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; var rt = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        var half = new Vector3(1.0f, 0.5f, 2.2f);
        foreach (var off in new[] { rt * 3.5f, -rt * 3.5f, -fw * 5f, fw * 5f, rt * 6f, -rt * 6f })
        {
            Vector3 p = feet + off;
            if (!Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var h, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(h.point.y - feet.y) > 1.2f) continue;
            Vector3 center = h.point + Vector3.up * 0.7f;
            if (Physics.CheckBox(center, half, Quaternion.Euler(0f, yaw, 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            return h.point;
        }
        // 경사라 위 검사에서 다 떨어졌으면: 높이 차 3 m 까지 받고 상자를 0.5 m 띄워 검사(내려앉는 건 ParkCar 가 맡는다)
        foreach (var off in new[] { rt * 3.5f, -rt * 3.5f, -fw * 5f, fw * 5f })
        {
            Vector3 p = feet + off;
            if (!Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out var h, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(h.point.y - feet.y) > 3f) continue;
            if (Physics.CheckBox(h.point + Vector3.up * 1.2f, half, Quaternion.Euler(0f, yaw, 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            return h.point;
        }
        return null;
    }

    /// <summary>카트 바로 옆(내릴 때 서는 자리)으로 — 카트를 보고 선다.</summary>
    public string DebugGoToCar()
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        DebugLeaveState();
        Vector3 p = ExitPoint();
        Vector3 to = car.transform.position - p; to.y = 0f;
        float yaw = to.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(to).eulerAngles.y : 0f;
        player.Teleport(p, yaw);
        EnterFoot();
        footCam.Yaw = yaw; footCam.Pitch = 15f;
        Mark($"디버그 카트 옆 {p.ToString("F1")} 카트 {car.transform.position.ToString("F1")}");
        return $"{p.ToString("F1")} 카트 {car.transform.position.ToString("F1")}";
    }

    /// <summary>낚시 자리로 가서 앉는다(앉기 + 찌 던지기까지 — 앉으면 자리가 알아서 던진다).</summary>
    public string DebugFishNow()
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        var s = DebugSpot;
        if (s == null) return "낚시 자리 없음";
        DebugLeaveState();
        player.Teleport(s.StandPoint(), s.SeatYaw);
        EnterFoot();
        footCam.Yaw = s.SeatYaw; footCam.Pitch = 0f;
        Sit(s);
        return "앉음 " + s.name;
    }

    public bool DebugRecast() => seatedAt != null && seatedAt.Recast();

    /// <summary>
    /// QA(과제 20261102-1543): 첫 주문을 출발시키고(아직이면) 카트를 배달지 앞 길에 세운 뒤 화물 HP 를 hp01 로 맞추고 도착·내리기까지.
    /// 그 뒤는 사람이 하듯 문을 보고 E. 운전을 건너뛰고 3단 반응만 빨리 보려는 것.
    /// </summary>
    public string DebugArriveForHandover(float hp01, Vector3 carPos, float carYaw, Vector3 feet, float feetYaw)
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        if (handover == null || firstDrop == null) return "배선 없음(handover/firstDrop)";
        if (Delivered) return "이미 도착함 — Play 다시";
        DebugLeaveState();
        Talked = true;
        if (!OrderStarted) { if (depot.PickedJob == null) depot.PickedJob = depot.MakeFirstJob(); BeginFirstOrder(); freeDriveEntered = true; }
        ParkCar(carPos, carYaw);
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo != null) cargo.DebugSetIntegrity(hp01);
        var mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        if (mgr != null) mgr.OnDeliveryReached(firstDrop);
        SetCarControl(false);
        player.gameObject.SetActive(true);
        player.Teleport(feet, feetYaw);
        EnterFoot();
        footCam.Yaw = feetYaw; footCam.Pitch = 0f;
        if (!CarryingBox) BeginCarry();
        return $"도착={Delivered} 들기={CarryingBox} 화물={(cargo != null ? (cargo.IntegrityPercent * 100f).ToString("F0") : "?")}% runActive={(rc != null && rc.RunActive)} 정산창={ResultPanel.ShowCount}";
    }

    /// <summary>QA: 시선이 어디를 보든 바로 두드린다(문이 화면 밖 → 받는 사람이 시선 이동으로 들어오는지).</summary>
    public bool DebugKnock()
    {
        if (State != Mode.OnFoot || !CarryingBox || handover == null || handover.Busy || handover.IsOpen || InTalk || Knocking) return false;
        Knock();
        return true;
    }

    /// <summary>타기 ↔ 내리기. 정지 속도 조건·사장 대화 조건 없이 바로.</summary>
    public string DebugToggleCart()
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        if (State == Mode.InCart) { ExitCart(); return "내림"; }
        DebugLeaveState();
        debugFreeCart = true;   // 첫 주문(포장 화면·timeScale 0)을 건드리지 않고 자유 주행으로 탄다
        EnterCart();
        debugFreeCart = false;
        return "탐";
    }
#endif
}
