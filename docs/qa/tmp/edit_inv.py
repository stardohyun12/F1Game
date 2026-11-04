import sys
sys.path.insert(0, 'D:/Github/F1Game/docs/qa/tmp')
from rep import edit

P = 'D:/Github/F1Game/F1/Assets/Scripts/Debug/WorldInvariants.cs'
pairs = []

pairs.append(('''() => PackPaperShake()),
    };
''', '''() => PackPaperShake()),
        ("포장 알갱이(D-296): 달걀 + 중 상자 + 알갱이 2줌 → 흔들림 ≤ 알갱이 0일 때의 50% · 같은 양이면 무거운 물건일수록 덜 붙듦 · 개수 늘면 메움↑(≤0.9)", () => PackBeadShake()),
    };
'''))

pairs.append(('''    /// <summary>
    /// D-296 구긴 종이: 달걀 한 판 + 중 상자(안 감쌈)를''', '''    /// <summary>
    /// D-296 스티로폼 알갱이: 달걀 한 판 + 중 상자(안 감쌈)를 표준 흔들기(진폭 4 cm · 3 Hz)로 흔든 덜그럭 값이 알갱이 0줌일 때보다 2줌(= 2 × bead_scoop 개)일 때 절반 이하여야 한다.
    /// 같은 개수·같은 상자 틈이면 물건이 무거울수록(달걀 0.6 kg &gt; 생선 1.5 kg) 알갱이가 덜 붙든다(질량 차) · 개수가 늘수록 메움이 줄지 않고 BeadFillMax 를 넘지 않으며 bead_max 개에서도 그렇다. 씬 없이 돈다.
    /// </summary>
    static List<string> PackBeadShake()
    {
        var bad = new List<string>();
        var od = WorldData.Orders; var pk = od != null ? od.packing : null;
        var tr = od != null ? od.TraitOf("달걀 한 판") : null;
        if (tr == null) tr = new WorldOrders.Trait { cargo = "달걀 한 판", trait = "깨짐", size = new Vector3(0.30f, 0.07f, 0.30f), mass = 0.6f };
        bool yaml = pk != null && pk.boxes.Count >= 3;
        Vector3 mid = yaml ? pk.boxes[1].inner : new Vector3(0.315f, 0.15f, 0.315f), big = yaml ? pk.boxes[2].inner : new Vector3(0.46f, 0.24f, 0.46f);
        var mm = yaml ? new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction) : new PackPhysics.Mat(0.12f, 0.3f, 0.4f);
        if (PackBoard.BeadScoop <= 0 || PackBoard.BeadMax < PackBoard.BeadScoop * 2 || PackBoard.BeadRadius <= 0f || PackBoard.BeadMass <= 0f || PackBoard.BeadHold <= 0f)
            bad.Add($"bead_* 숫자가 이상함 scoop={PackBoard.BeadScoop} max={PackBoard.BeadMax} r={PackBoard.BeadRadius} m={PackBoard.BeadMass} hold={PackBoard.BeadHold}");
        int two = PackBoard.BeadScoop * 2;
        float s0 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None).MeasureStandard();
        var withBeads = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, 0, two);
        float s2 = withBeads.MeasureStandard();
        if (s0 <= 0f) bad.Add($"알갱이 0 + 중 상자 + {tr.cargo}: 흔들림 {s0 * 100f:F2} cm — 비교할 기준이 없음");
        else if (s2 > s0 * 0.5f) bad.Add($"알갱이 2줌({two}개) 흔들림 {s2 * 100f:F2} cm > 알갱이 0 {s0 * 100f:F2} cm 의 50% (틈 메움 {withBeads.fill * 100f:F0}%)");
        float eggFill = PackBoard.BeadFill(mid, tr.size.x, tr.size.z, 0.6f, two), fishFill = PackBoard.BeadFill(mid, tr.size.x, tr.size.z, 1.5f, two);
        if (!(eggFill > fishFill)) bad.Add($"같은 알갱이 {two}개: 0.6 kg 물건 메움 {eggFill:F2} ≤ 1.5 kg 물건 {fishFill:F2} — 무거울수록 덜 붙들어야 함");
        float prev = 0f;
        for (int n = 1; n <= PackBoard.BeadMax; n += Mathf.Max(1, PackBoard.BeadScoop))
        {
            float f = PackBoard.BeadFill(big, tr.size.x, tr.size.z, tr.mass, n);
            if (f < prev || f > PackBoard.BeadFillMax + 1e-4f || f <= 0f) bad.Add($"대 상자 알갱이 {n}개 메움 {f:F3} (앞 {prev:F3}, 0 초과 {PackBoard.BeadFillMax:F2} 이하·줄지 않아야 함)");
            prev = f;
        }
        return bad;
    }

    /// <summary>
    /// D-296 구긴 종이: 달걀 한 판 + 중 상자(안 감쌈)를'''))

edit(P, pairs)
