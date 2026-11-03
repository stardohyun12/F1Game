using UnityEngine;

// D-287: OnFootDirector 의 한 조각 — 다음 날 아침 되돌리기(감독 몫만). 지도: docs/world/CODEMAP.md
// 단계 몫은 단계가 DayStarted 를 듣고 스스로 되돌린다: 주문·전표·칠판 = Stage_Depot · 주문 출발·배달지 = Stage_Drive ·
// 상자·건네기 = Stage_Door · 노을 = Stage_Return · 아침 카트 자리 = Stage_Garage · 작업대·포장 결과 = Stage_Packing.
// 여기는 감독이 쥔 것(대화·돈 글자·몸)만.
public partial class OnFootDirector
{
    /// <summary>Stage_DayEnd.Exit(일기 덮기) 가 DayStarted 를 쏜 뒤 부른다.</summary>
    internal void ResetForNextDay()
    {
        ClearMoney();
        StepTrail = "없음";
        talkLine = -1; curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
        // 카트 자리 = Stage_Garage · 작업대 = Stage_Packing 이 DayStarted 에서. 나는 방으로
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        player.enabled = true;
        EnterFoot();
        footCam.HoldLook = false;
        simInteract = false;
    }
}
