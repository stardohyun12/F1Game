using NUnit.Framework;

/// <summary>D-290: 포장 규칙(상자 크기·완충재 궁합). 값 = world.yaml packing 기본값.</summary>
public class PackRulesTests
{
    // 달걀 한 판 0.30×0.07×0.30 — 상자 소 0.22 / 중 0.315 / 대 0.46
    [Test] public void 상자_작으면_안들어감() { Assert.AreEqual(PackFit.TooSmall, PackRules.FitOf(0.30f, 0.07f, 0.30f, 0.22f, 0.12f, 0.22f)); }
    [Test] public void 상자_중간_꼭맞음() { Assert.AreEqual(PackFit.Snug, PackRules.FitOf(0.30f, 0.07f, 0.30f, 0.315f, 0.15f, 0.315f)); }
    [Test] public void 상자_크면_헐렁() { Assert.AreEqual(PackFit.Loose, PackRules.FitOf(0.30f, 0.07f, 0.30f, 0.46f, 0.24f, 0.46f)); }

    [Test] public void 궁합_깨짐_뽁뽁이가_종이보다_큼()
    {
        Assert.Greater(PackRules.Cushion("깨짐", PackFiller.Bubble), PackRules.Cushion("깨짐", PackFiller.Paper));
        Assert.Greater(PackRules.Cushion("깨짐", PackFiller.Paper), 0f);
    }
    [Test] public void 궁합_안맞으면_0()
    {
        Assert.AreEqual(0f, PackRules.Cushion("젖음", PackFiller.Paper));
        Assert.AreEqual(0f, PackRules.Cushion("기울기", PackFiller.Bubble));
        Assert.AreEqual(0f, PackRules.Cushion("깨짐", PackFiller.None));
    }
    [Test] public void 시작상태_뽁뽁이_종이_맨몸_순()
    {
        float b = PackRules.StartIntegrity("깨짐", PackFit.Snug, PackFiller.Bubble, PackFiller.Bubble, 0f);
        float p = PackRules.StartIntegrity("깨짐", PackFit.Snug, PackFiller.Paper, PackFiller.Paper, 0f);
        float n = PackRules.StartIntegrity("깨짐", PackFit.Snug, PackFiller.None, PackFiller.None, 0f);
        Assert.Greater(b, p); Assert.Greater(p, n);
        Assert.AreEqual(0, HandoverTier.Of(b));   // 뽁뽁이로 꼭 맞게 = 멀쩡
        Assert.AreEqual(1, HandoverTier.Of(n));   // 맨몸 달걀 = 조금 상함
    }
    [Test] public void 시작상태_헐렁하면_낮음()
    {
        Assert.Less(PackRules.StartIntegrity("깨짐", PackFit.Loose, PackFiller.None, PackFiller.None, 1.2f),
                    PackRules.StartIntegrity("깨짐", PackFit.Snug, PackFiller.None, PackFiller.None, 1.2f));
    }
    // D-296 뽁뽁이 감싸기: 겹 계수 = (1-wrap_damp)^겹, 크기는 겹마다 양쪽으로 wrap_thick
    [Test] public void 감싸기_겹_계수는_겹마다_줄어듦()
    {
        Assert.AreEqual(1f, PackRules.WrapK(0));
        Assert.Less(PackRules.WrapK(1), PackRules.WrapK(0));
        Assert.Less(PackRules.WrapK(2), PackRules.WrapK(1));
        Assert.AreEqual(PackRules.WrapK(PackRules.WrapMax), PackRules.WrapK(PackRules.WrapMax + 5));   // 최대 겹 넘으면 더 안 줄어듦
    }
    [Test] public void 감싸기_크기는_겹마다_커지고_달걀_2겹은_중상자에_들어감()
    {
        Assert.Greater(PackRules.Wrapped(0.30f, 1), 0.30f);
        Assert.Greater(PackRules.Wrapped(0.30f, 2), PackRules.Wrapped(0.30f, 1));
        float w = PackRules.Wrapped(0.30f, 2), h = PackRules.Wrapped(0.07f, 2);
        Assert.AreEqual(PackFit.Snug, PackRules.FitOf(w, h, w, 0.315f, 0.15f, 0.315f));
    }
    [Test] public void 감싸기_겹_계수는_흔들림_손상만_줄임()
    {
        float s0 = PackRules.StartFromShake("깨짐", PackFiller.None, PackFiller.None, 0f);
        float bare = PackRules.StartFromShake("깨짐", PackFiller.None, PackFiller.None, 0.2f);
        float wrapped = PackRules.StartFromShake("깨짐", PackFiller.None, PackFiller.None, 0.2f, PackRules.WrapK(2));
        Assert.Greater(wrapped, bare);                        // 같은 흔들림이면 감싼 쪽 손상이 작다
        Assert.LessOrEqual(wrapped, s0);                      // 바탕 손상은 그대로 — 0 흔들림보다 좋아지진 않는다
    }
    [Test] public void 시작상태_바닥값과_안들어감()
    {
        Assert.AreEqual(PackRules.MinStart, PackRules.StartIntegrity("깨짐", PackFit.TooSmall, PackFiller.Bubble, PackFiller.Bubble, 0f));
        Assert.GreaterOrEqual(PackRules.StartIntegrity("깨짐", PackFit.Loose, PackFiller.None, PackFiller.None, 99f), PackRules.MinStart);
    }
}
