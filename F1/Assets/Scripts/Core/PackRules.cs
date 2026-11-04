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
    /// <summary>D-295: 1.5 s 흔들림 합(m)이 이만큼이면 흔들림 손실이 최대(RattleLoss). 가설 — 곡선은 선형.</summary>
    public static float ShakeFull = 0.5f;
    /// <summary>D-296 뽁뽁이 감싸기: 한 겹이 물건 한 변에 더하는 두께(m) · 한 겹이 부딪힘 세기를 줄이는 비율 · 최대 겹 수. 가설.</summary>
    public static float WrapThick = 0.003f, WrapDamp = 0.5f;
    public const int WrapMax = 2;

    /// <summary>겹 계수(0~1): 감싼 겹 수만큼 부딪힘 세기·반발에 곱한다. 0겹 = 1.</summary>
    public static float WrapK(int layers)
    {
        if (layers <= 0) return 1f;
        if (layers > WrapMax) layers = WrapMax;
        return (float)System.Math.Pow(System.Math.Max(0f, 1f - WrapDamp), layers);
    }

    /// <summary>감싼 물건의 실제 크기 한 변: 겹마다 양쪽으로 WrapThick.</summary>
    public static float Wrapped(float side, int layers) => side + 2f * WrapThick * System.Math.Min(System.Math.Max(layers, 0), WrapMax);

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

    /// <summary>
    /// D-295 물리 시뮬 판 시작값: 흔들림 측정(1.5 s 동안 물건이 상자 기준 움직인 거리 합, m)에서 온다. 틈 0(꽉 낌) = 흔들림 0 → 바탕 손상만,
    /// ShakeFull 이상 움직이면 RattleLoss 만큼 더(wrapK = 감싼 겹 계수 — 움직인 양에 곱해 부딪힘 손상만 줄인다). 헐렁한 상자의 벌점(LooseLoss)은 따로 없다 — 틈이 크면 많이 움직이니 이미 흔들림에 들어 있다.
    /// </summary>
    public static float StartFromShake(string trait, PackFiller bottom, PackFiller top, float shakeMeters, float wrapK = 1f)
    {
        float r = shakeMeters * wrapK / System.Math.Max(0.01f, ShakeFull);               // D-296: 감싼 겹 계수는 흔들림(부딪힘 손상)에만 곱한다
        if (r < 0f) r = 0f; if (r > 1f) r = 1f;
        float loss = (BaseLoss + RattleLoss * r) * (1f - (Cushion(trait, bottom) + Cushion(trait, top)) * 0.5f);
        float v = 1f - loss;
        return v < MinStart ? MinStart : v > 1f ? 1f : v;
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
