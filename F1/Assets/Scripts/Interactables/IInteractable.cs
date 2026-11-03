using UnityEngine;

/// <summary>
/// D-287 2단계: 시선이 닿는 물건 하나. 칩 글(Prompt)·누를 수 있나(CanUse)·누르면(Use) — 셋이면 E 칩이 뜬다.
/// 물건은 감독을 부르지 않는다. Use 는 신호(SignalBus)를 쏘고 끝 — 그 일은 맡은 단계가 한다.
/// 브리프의 세 개에 둘을 더했다: Anchor(칩이 뜰 자리) · Low(칩을 아래쪽에) — 예전 감독 분기에 있던 것 그대로.
/// </summary>
public interface IInteractable
{
    string Prompt { get; }
    bool CanUse { get; }
    void Use();
    /// <summary>칩을 띄울 세계 좌표(null = 칩 자리 없음).</summary>
    Vector3? Anchor { get; }
    /// <summary>칩을 물건 아래쪽에(칠판·낚시 자리).</summary>
    bool Low { get; }
}

/// <summary>물건 공통 바탕 — 「E 」로 시작하는 칩만 누를 수 있다(예전 분기와 같은 규칙).</summary>
public abstract class Interactable : IInteractable
{
    public abstract string Prompt { get; }
    public virtual bool CanUse => Prompt.StartsWith("E ");
    public abstract void Use();
    public abstract Vector3? Anchor { get; }
    public virtual bool Low => false;
}
