    // ── 물리 한 걸음(매 프레임) · 흔들기: 닫은 상자를 집은 채 마우스가 오가면 상자 가속 → 안의 물건이 실제로 움직인다 ──
    static float DropBounce(PackPiece p)
    {
        if (p.kind == PackPieceKind.Box) return BoxMat(p.name).bounce;
        if (p.kind == PackPieceKind.Parcel && p.parcel != null) { var t = WorldData.Orders != null ? WorldData.Orders.TraitOf(p.parcel.cargoName) : null; if (t != null) return t.bounce; }
        return 0.3f;
    }

    /// <summary>떨어지는 것·걸린 것·상자 안 물건을 dt 만큼. 상자를 쥐고 있으면 손 속도의 변화 = 상자 가속(m/s²)을 안의 물리에 먹인다.</summary>
    public void Tick(float dt)
    {
        if (dt <= 0f) return;
        foreach (var p in pieces.ToArray())
        {
            if (p.kind != PackPieceKind.Roll && p.kind != PackPieceKind.Tape) p.drop.Step(dt);
            p.dropBottom.Step(dt); p.dropTop.Step(dt);
            var pos = new Vector2(p.u, p.v);
            Vector2 acc = Vector2.zero;
            if (Held == p)
            {
                var v = (pos - p.lastPos) / dt;
                acc = Vector2.ClampMagnitude((v - p.hand) / dt, 300f);
                p.hand = v;
            }
            p.lastPos = pos;
            if (p.perch > 0f)
            {
                p.perch -= dt;
                if (p.perch <= 0f) { p.perch = 0f; p.Home(); PackBench.PlayCue(4); Act("벽에 걸린 물건이 튕겨 나옴"); }
            }
            if (p.kind != PackPieceKind.Box || p.phys == null) continue;
            p.phys.Advance(dt, acc);
            PlaceContent(p);
            if (p.closed) { p.rattle = p.phys.PeakWindow; p.shakeSecs = p.phys.ShakeTime; }
            p.jit = p.closed ? Vector2.ClampMagnitude(p.phys.vel * 0.03f, 0.02f) : Vector2.zero;
            float im = p.phys.TakeImpact();
            if (im > 0.02f) Rattle(im);
        }
    }

    void Rattle(float impact)
    {
        if (sfxItem == null) { var g = new GameObject("pack2d_sfx") { hideFlags = HideFlags.HideAndDontSave }; sfxItem = g.AddComponent<PackItem>(); sfxItem.kind = PackItemKind.Eggs; }
        PackBench.PlayRattle(sfxItem, Mathf.Clamp01(impact / 0.5f));
        RattleBeats++;
    }

    /// <summary>QA: 이 보드가 낸 덜그럭 소리 수.</summary>
    public int RattleBeats { get; private set; }

    /// <summary>QA: 떨어지는 조각·걸린 물건이 아직 있나(없으면 다 자리 잡음).</summary>
    public bool Settling
    {
        get
        {
            foreach (var p in pieces) if (p.drop.Active || p.dropBottom.Active || p.dropTop.Active || p.perch > 0f) return true;
            return false;
        }
    }

    /// <summary>마지막으로 들고 나간 상자의 흔들림 측정(m)과 어떻게 쟀는지.</summary>
    public float LastShake { get; private set; }
    public string LastShakeHow { get; private set; } = "";

    /// <summary>상자를 판 밖(아래)으로 끌었다: 흔들림 측정 → 화물 시작값을 Core 로 구해 포장대(PackTable2)에 넘기고, 판에서 물러난 뒤 BoxTaken 을 올린다.</summary>
    void TakeOut(PackPiece box)
    {
        var parcel = box.content;
        var tr = TraitOfParcel(parcel);
        float shake; string how;
        if (box.phys == null) { shake = 0f; how = "물리 없음"; }
        else if (box.shakeSecs >= PackPhysics.Window) { shake = box.rattle; how = $"직접 흔든 {box.shakeSecs:F1}s 중 1.5s 합"; }
        else { shake = box.phys.MeasureStandard(); how = $"흔든 시간 {box.shakeSecs:F1}s < 1.5s → 표준 흔들기(진폭 {PackPhysics.StdAmp * 100f:F0}cm·{PackPhysics.StdHz:F0}Hz)로 잼"; }
        float start = PackRules.StartFromShake(tr.trait, box.bottom, box.top, shake);
        var lim = box.phys != null ? box.phys.Limit : Vector2.zero;
        var r = new PackBox.ShakeResult
        {
            box = box.name, seconds = box.shakeSecs, rattle = shake, closed = true, taped = true, upright = true, locks = "", faces = "",
            gap = Mathf.Max(lim.x, lim.y) * 2f,
        };
        LastStart = start; LastResult = r; LastShake = shake; LastShakeHow = how;
        if (table != null) table.Accept2D(start, r, box.bottom, box.top, box.fit);
        Act($"상자 들고 나감 → 화물 시작 {start * 100f:F0}% (흔들림 {shake * 100f:F1} cm / {how}; 상자 {box.name} 바닥={box.bottom} 덮기={box.top} 테이프 {box.strips.Count}줄)");
        box.active = false; parcel.active = false; InSlot = null;
        pendingTake = true; ExitAsked = true;
    }
