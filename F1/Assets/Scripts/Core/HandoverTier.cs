/// <summary>
/// D-250 ④ 화물 상태 3단 [가설 — 도현 판정 전]. 깨져도 이야기는 이어지고 돈과 그 자리 반응만 달라진다.
/// 기준 = CargoBox.IntegrityPercent(HP / 최대 HP). 대사는 대역 글 — 실제 글은 도현이 정한다.
/// D-287: OnFoot/DoorHandover.cs 에서 Core 로 자리만 옮김(값 그대로).
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
