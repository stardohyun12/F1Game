/// <summary>
/// D-290 포장 규칙(SPEC §6-4). 유니티 없이 돈다. 값의 원천 = world/world.yaml 의 packing — WorldOrders.ApplyPacking 이 시작 때 넣는다(기본값 = yaml 값).
/// 상자 크기: 물건보다 작으면 안 들어감(판정만 — 실제로 막는 건 물리), 여유가 snugGapMax 보다 크면 헐렁(흔들림↑).
/// 완충재 궁합: 화물 성질(깨짐/젖음)과 맞으면 손상을 줄이고, 안 맞으면 효과 0. 바닥 1장 + 덮기 1장의 평균만큼 줄인다.
/// </summary>
public enum PackFiller { None, Bubble, Paper }
public enum PackFit { TooSmall, Snug, Loose }

public static class PackRules
{
    /// <summary>각 변 여유(상자 안 − 물건)가 이 이하면 꼭 맞음(m). 가설.</summary>
    public static float SnugGapMax = 0.03f;
    /// <summary>아무 문제 없어도 길에서 생길 손상의 바탕(0~1). 가설.</summary>
    public static float BaseLoss = 0.25f;
    /// <summary>흔들기 덜그럭(rattle) 이 RattleFull 이면 RattleLoss 만큼 더 깎인다(선형). 가설 — 옛 작업대 값 그대로.</summary>
    public static float RattleLoss = 0.40f, RattleFull = 2f;
    /// <summary>헐렁한 상자 하나로 더 깎이는 값. 가설.</summary>
    public static float LooseLoss = 0.15f;
    public static float MinStart = 0.4f;

    /// <summary>궁합 표 [성질][완충재] = 손상 줄이는 비율(0~1). 성질: 0 깨짐 · 1 젖음. 완충재: 0 없음 · 1 뽁뽁이 · 2 종이.</summary>
    public static float[,] Cushions =
    {
        { 0f, 0.6f, 0.2f },   // 깨짐: 뽁뽁이 큼, 종이 작음
        { 0f, 0.5f, 0f },     // 젖음: 뽁뽁이(비닐)만, 종이는 젖어서 0
    };

    public static int TraitIndex(string trait) => trait == "깨짐" ? 0 : trait == "젖음" ? 1 : -1;

    /// <summary>완충재 한 장의 효과(0~1). 모르는 성질·없음 = 0.</summary>
    public static float Cushion(string trait, PackFiller f)
    {
        int t = TraitIndex(trait);
        if (t < 0 || f == PackFiller.None) return 0f;
        return Cushions[t, (int)f];
    }

    /// <summary>물건(가로·높이·세로)과 상자 안(가로·높이·세로). 눕힌 방향 그대로 비교(돌려 넣기 없음).</summary>
    public static PackFit FitOf(float ix, float iy, float iz, float bx, float by, float bz)
    {
        if (ix > bx || iy > by || iz > bz) return PackFit.TooSmall;
        float gap = System.Math.Max(bx - ix, bz - iz);
        return gap <= SnugGapMax ? PackFit.Snug : PackFit.Loose;
    }

    /// <summary>출발 때 화물 상태(0~1). 안 들어간 상자는 판정할 수 없다 → 바닥값.</summary>
    public static float StartIntegrity(string trait, PackFit fit, PackFiller bottom, PackFiller top, float rattle)
    {
        if (fit == PackFit.TooSmall) return MinStart;
        float r = rattle / System.Math.Max(0.01f, RattleFull);
        if (r < 0f) r = 0f; if (r > 1f) r = 1f;
        float loss = BaseLoss + RattleLoss * r + (fit == PackFit.Loose ? LooseLoss : 0f);
        float protect = (Cushion(trait, bottom) + Cushion(trait, top)) * 0.5f;
        loss *= 1f - protect;
        float v = 1f - loss;
        return v < MinStart ? MinStart : v > 1f ? 1f : v;
    }
}
