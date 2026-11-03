using UnityEngine;

namespace PitCrew.PackBench
{
    /// <summary>
    /// 과제 20261102-1951(하루 끝 → 다음 날 아침): 작업대의 손·상자 상태를 처음으로. 물건·상자 위치는 DepotPackSpot 이 되돌린다.
    /// 부르는 때 = 작업대가 꺼져 있고(걷기 시점) 일기를 덮은 직후.
    /// </summary>
    public partial class PackBench
    {
        public void ResetForNextDay()
        {
            StopAllCoroutines();
            if (toolVis != null) { Destroy(toolVis); toolVis = null; }
            if (toolSupply != null) foreach (var rr in toolSupply.GetComponentsInChildren<Renderer>()) rr.enabled = true;
            tool = Tool.None; toolSupply = null; taping = false;
            held = null; heldSheet = null; pullSheet = null;
            rotating = false; crumpling = false; rotAcc = Vector2.zero; breakT = 0f; holdDist = 0.8f;
            benchBox = null; lifted = null; moving = null; liftedReturning = false;
            firstTouch = -1f; timed = false; hasHit = false;
            simHold = false; prevSimHold = false; simShakeUntil = -1f;
            Prompt = ""; PromptAnchor = null;
            shakenBox = null; shakenOpen = 0; gapAt = -9f; CurrentStep = Step.Pick;
        }
    }
}
