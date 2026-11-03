using UnityEngine;

// D-287: OnFootDirector 의 한 조각 — 다음 날 아침 되돌리기(아직 단계로 안 나눈 몫). 지도: docs/world/CODEMAP.md
// 2단계(과제 06): 보관소 몫(주문·전표·칠판)은 Stage_Depot.NewDay 로 갔다. 나머지는 3·4단계에서 단계마다 나눈다.
public partial class OnFootDirector
{
    /// <summary>Stage_DayEnd.Exit(일기 덮기) 가 DayStarted 를 쏜 뒤 부른다.</summary>
    internal void ResetForNextDay()
    {
        var mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        if (mgr != null) mgr.OnDeliveryCompleted.RemoveListener(OnFirstDelivered);
        // 이번 날 주문·건네기 상태
        firstJob = null;   // 맡은 주문·전표·칠판은 Stage_Depot 이 DayStarted 를 듣고 되돌렸다
        OrderStarted = false; Delivered = false; HandedOver = false; CarryingBox = false; Knocking = false; BedBox = false;
        HandoverTierIdx = -1; HandoverMoney = 0; moneyText = null; MoneyAt = -99f; MoneyEnd = -99f;
        StartIntegrity = -1f; freeDriveEntered = false;
        StepTrail = "없음";
        // 아침 = 오후 빛으로(노을은 건넨 뒤에만)
        if (duskRamp != null) { StopCoroutine(duskRamp); duskRamp = null; }
        HandedAt = -99f;
        var look = SunsetLook.Find();
        if (look != null && look.IsOn) { look.dusk = 0f; if (lookWasOn) look.Apply(); else look.SetOn(false); }
        talkLine = -1; curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
        // 손에 든 상자: 받는 사람에게 건넨 것은 치우고 새로 만든다(MoveBoxToHands 가 heldBox 를 비웠다)
        if (heldBoxGiven != null) { Destroy(heldBoxGiven); heldBoxGiven = null; }
        if (heldBox == null) BuildHeldBox();
        // 화물(카트 짐칸) — BeginCarry 가 꺼 둔 모양을 되살린다
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo != null && car != null && cargo.transform != car.transform)
            foreach (var r in cargo.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        // 배달지: 꺼 둔 것 되살리고 「완료」 풀기
        foreach (var dp in pointsOff) if (dp != null) dp.gameObject.SetActive(true);
        pointsOff.Clear();
        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) dp.ResetCompleted();
        // 작업대
        if (packSpot != null) packSpot.ResetForNextDay();
        // 카트는 아침 자리(보관소 흐름 = 보관소 앞)로, 나는 방으로
        ParkMorningCart();
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        player.enabled = true;
        EnterFoot();
        footCam.HoldLook = false;
        simInteract = false;
    }
}
