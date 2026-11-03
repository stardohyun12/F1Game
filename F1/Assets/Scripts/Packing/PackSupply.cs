using UnityEngine;

namespace PitCrew.PackBench
{
    public enum PackSupplyKind { Newspaper, Bubble, Tape, Label }

    /// <summary>
    /// 작업대 위 재료 더미(신문지 묶음·뽁뽁이 롤·테이프·라벨 묶음). 수량 없음(무한) — 선반 실물 수량은 다음 판.
    /// D-287 6단계: 작업대(Interactables/Pack)와 달리 본체(PitCrew 어셈블리)에 둔다 — 씬(Map_TownV6)이 이 컴포넌트를
    /// 「PitCrew::PitCrew.PackBench.PackSupply」 이름으로 찾아서, 어셈블리를 옮기면 Missing 이 된다(실측 Newspaper·BubbleRoll).
    /// </summary>
    public class PackSupply : MonoBehaviour
    {
        public PackSupplyKind kind;
    }
}
