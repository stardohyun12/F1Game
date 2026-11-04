import sys
sys.path.insert(0, 'D:/Github/F1Game/docs/qa/tmp')
from rep import edit

P = 'D:/Github/F1Game/F1/Assets/Scripts/Interactables/Pack2D/PackBoard.cs'
pairs = []

# ── 헤더 설명 ──
pairs.append(('''/// D-295: 놓는 순간부터 물리''', '''/// D-296 알갱이: 판 오른쪽 통을 클릭하면 삽 한 번 = 한 줌(손에 삽 조각), 열린 상자 위에서 놓으면 쏟아져 틈을 고르게 메운다(상자 밖이면 흩어져 사라짐).
/// D-295: 놓는 순간부터 물리'''))

# ── Build: 통 ──
pairs.append(('''        Add(PackPieceKind.Tape, "테이프", ru + 0.05f, -0.2f, 0.09f, 0.09f);
''', '''        Add(PackPieceKind.Tape, "테이프", ru + 0.05f, -0.2f, 0.09f, 0.09f);
        Add(PackPieceKind.BeadBin, "알갱이 통", BinU, BinV, 0.36f, 0.16f);        // 판 오른쪽 가운데 띠(상자 줄과 롤 줄 사이)
'''))

# ── 상수·yaml 값 ──
pairs.append(('''    public static float PaperMass => Pk != null ? Pk.paperMass : 0.01f;
''', '''    public static float PaperMass => Pk != null ? Pk.paperMass : 0.01f;

    // ── D-296 스티로폼 알갱이: world.yaml packing bead_* ──
    /// <summary>통 자리(판 m) · 한 상자 알갱이가 메울 수 있는 틈 비율의 위 · 알갱이가 쏟아져 자리까지 날아가는 시간(s).</summary>
    public const float BinU = 0.60f, BinV = -0.04f, BeadFillMax = 0.9f, BeadFlight = 0.2f;
    public static int BeadScoop => Pk != null ? Pk.beadScoop : 30;
    public static int BeadMax => Pk != null ? Pk.beadMax : 500;
    public static float BeadRadius => Pk != null ? Pk.beadRadius : 0.006f;
    public static float BeadMass => Pk != null ? Pk.beadMass : 0.002f;
    public static float BeadFriction => Pk != null ? Pk.beadFriction : 0.95f;
    public static float BeadBounce => Pk != null ? Pk.beadBounce : 0.10f;
    public static float BeadHold => Pk != null ? Pk.beadHold : 0.10f;

    /// <summary>상자 밖에 쏟아 흩어지는 알갱이(그림용, 0.5~0.8 s 뒤 사라짐). 자리는 판 m.</summary>
    public sealed class Scatter { public Vector2 pos, vel; public float life; }
    public readonly List<Scatter> scatter = new List<Scatter>();
    readonly System.Random rng = new System.Random(296);
    float Rnd(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    public int LastSpilled { get; private set; }
'''))

# ── Setup: 알갱이 ──
pairs.append(('''    public static PackPhysics Setup(Vector3 inner, bool boxRot, PackPhysics.Mat boxMat, WorldOrders.Trait cargo, bool parcelRot, PackFiller bottom, PackFiller top, int wrap = 0, int wads = 0)''',
'''    public static PackPhysics Setup(Vector3 inner, bool boxRot, PackPhysics.Mat boxMat, WorldOrders.Trait cargo, bool parcelRot, PackFiller bottom, PackFiller top, int wrap = 0, int wads = 0, int beads = 0)'''))
pairs.append(('''            ph.mu = Mathf.Lerp(ph.mu, Mathf.Max(ph.mu, PaperFriction), ph.fill);
        }
        return ph;
    }
''', '''            ph.mu = Mathf.Lerp(ph.mu, Mathf.Max(ph.mu, PaperFriction), ph.fill);
        }
        if (beads > 0)                                                        // D-296: 알갱이는 덩어리 없이 틈을 고르게 메우되, 가벼워서 무거운 물건은 덜 붙든다
        {
            float fb = BeadFill(inner, sx, sz, cargo.mass, beads);
            ph.fill = 1f - (1f - ph.fill) * (1f - fb);                        // 종이 메움과 겹쳐 쌓는다
            ph.e *= Mathf.Lerp(1f, BeadBounce, fb);
            ph.mu = Mathf.Lerp(ph.mu, Mathf.Max(ph.mu, BeadFriction), fb);
        }
        return ph;
    }

    /// <summary>
    /// 알갱이 beads 개가 틈(상자 바닥 − 물건 발자국)을 메우는 비율(0~BeadFillMax) × 붙듦. 붙듦 = M/(M + bead_hold × 물건 질량), M = 개수 × bead_mass.
    /// 같은 개수라도 물건이 무거울수록 붙듦이 줄고(달걀 &gt; 유리병 &gt; 생선), 더 퍼 넣을수록 는다.
    /// </summary>
    public static float BeadFill(Vector3 inner, float sx, float sz, float cargoMass, int beads)
    {
        if (beads <= 0) return 0f;
        float gapArea = Mathf.Max(1e-4f, inner.x * inner.z - sx * sz), r = BeadRadius;
        float raw = Mathf.Min(BeadFillMax, beads * Mathf.PI * r * r / gapArea);
        float m = beads * BeadMass;
        return raw * m / (m + BeadHold * Mathf.Max(0f, cargoMass));
    }
'''))
pairs.append(('''box.rot90, BoxMat(box.name), TraitOfParcel(pc), pc.rot90, box.bottom, box.top, pc.wrap, box.wads.Count);''',
              '''box.rot90, BoxMat(box.name), TraitOfParcel(pc), pc.rot90, box.bottom, box.top, pc.wrap, box.wads.Count, box.beads.Count);'''))

# ── ResetAll ──
pairs.append(('''        pieces.RemoveAll(p => p.kind == PackPieceKind.Sheet);
        foreach (var p in pieces)''', '''        pieces.RemoveAll(p => p.kind == PackPieceKind.Sheet || p.kind == PackPieceKind.Scoop);
        scatter.Clear(); LastSpilled = 0;
        foreach (var p in pieces)'''))

# ── Begin ──
pairs.append(('''        else if (p.kind == PackPieceKind.Box && p.content != null && !p.closed) { Deny''', '''        else if (p.kind == PackPieceKind.BeadBin)                            // D-296: 통은 집는 게 아니라 삽으로 한 줌 떠서 손에 든다
        {
            p = Add(PackPieceKind.Scoop, "삽", u, v, 0.10f, 0.06f);
            PackBench.PlayCue(5);
            Act($"삽으로 알갱이 한 줌 뜸({BeadScoop}개)");
        }
        else if (p.kind == PackPieceKind.Box && p.content != null && !p.closed) { Deny'''))
pairs.append(('''p.bottom = p.top = PackFiller.None; p.dropBottom = p.dropTop = default; p.wads.Clear(); Act($"{p.name} 바닥 완충재·구긴 종이 걷어냄"); }''',
              '''p.bottom = p.top = PackFiller.None; p.dropBottom = p.dropTop = default; p.wads.Clear(); p.beads.Clear(); Act($"{p.name} 바닥 완충재·구긴 종이·알갱이 걷어냄"); }'''))
pairs.append(('''        if (p.kind != PackPieceKind.Sheet) { Act($"집음 {p.name}"); if (p.kind != PackPieceKind.Tape) PackBench.PlayCue(0); }''',
              '''        if (p.kind != PackPieceKind.Sheet && p.kind != PackPieceKind.Scoop) { Act($"집음 {p.name}"); if (p.kind != PackPieceKind.Tape) PackBench.PlayCue(0); }'''))

# ── Rotate ──
pairs.append(('''if (p == null || p.kind == PackPieceKind.Roll || p.kind == PackPieceKind.Tape) return;
        p.Rotate();''', '''if (p == null || p.kind == PackPieceKind.Roll || p.kind == PackPieceKind.Tape || p.kind == PackPieceKind.Scoop) return;
        p.Rotate();'''))

# ── End ──
pairs.append(('''            case PackPieceKind.Tape: EndTape(p); break;
            default:''', '''            case PackPieceKind.Tape: EndTape(p); break;
            case PackPieceKind.Scoop: EndScoop(p); break;
            default:'''))
pairs.append(('''        if (p.kind != PackPieceKind.Roll) p.drop.Drop(DropZ, DropBounce(p));          // 놓는 순간부터 떨어진다''',
              '''        if (p.kind != PackPieceKind.Roll && p.kind != PackPieceKind.Scoop) p.drop.Drop(DropZ, DropBounce(p));   // 놓는 순간부터 떨어진다'''))

# ── EndScoop / Pour (PutWad 앞에) ──
pairs.append(('''    /// <summary>
    /// D-296 구긴 종이를 열린 상자(가운데에 놓인) 위에서 놓으면''', '''    /// <summary>
    /// D-296 삽을 열린 상자(가운데에 놓인) 위에서 놓으면 한 줌이 쏟아져 틈에 쌓인다. 상자 밖이면 흩어져 사라진다. 한 상자 BeadMax 개가 넘으면 넘친 만큼 흩어진다.
    /// 알갱이는 쏟은 자리에서 날아가 상자 틈(물건 발자국 밖 · 아직 물건이 없으면 바닥 어디든)에 고르게 떨어진다 — 덩어리 지지 않는다.
    /// </summary>
    void EndScoop(PackPiece sheet)
    {
        pieces.Remove(sheet);
        var box = InSlot;
        int n = BeadScoop;
        var at = new Vector2(sheet.u, sheet.v);
        LastSpilled = 0;
        if (box == null || box.closed || Mathf.Abs(sheet.u - box.u) > box.w * 0.5f || Mathf.Abs(sheet.v - box.v) > box.d * 0.5f)
        {
            Scatter_(at, n); PackBench.PlayCue(5); Act("알갱이 상자 밖에 쏟음 → 흩어져 사라짐"); return;
        }
        int put = Mathf.Min(n, Mathf.Max(0, BeadMax - box.beads.Count));
        if (put < n) { LastSpilled = n - put; Scatter_(at, n - put); }
        float rad = BeadRadius * BoxScale;
        var from = at - new Vector2(box.u, box.v);
        int first = box.beads.Count;
        for (int i = 0; i < put; i++)
        {
            var bd = new PackPiece.Bead { from = from + new Vector2(Rnd(-0.02f, 0.02f), Rnd(-0.02f, 0.02f)), off = PourTarget(box, rad), delay = i * 0.012f + Rnd(0f, 0.04f) };
            box.beads.Add(bd);
        }
        Relax(box, first, rad);
        if (put > 0) RefreshPhys(box);
        PackBench.PlayCue(5);
        float fill = box.phys != null ? box.phys.fill : 0f;
        Act($"알갱이 {put}개 쏟음 → {box.name} 에 {box.beads.Count}/{BeadMax}개{(put < n ? $" · {n - put}개 넘쳐 흩어짐" : "")}{(box.phys != null ? $" · 틈 {fill * 100f:F0}% 메움" : "")}");
    }

    /// <summary>알갱이가 앉을 자리: 상자 안 바닥에서 고르게. 물건이 이미 있으면 물건 발자국 밖(틈) — 물건 위에 떨어질 자리는 가까운 가장자리 밖으로 밀려난다.</summary>
    Vector2 PourTarget(PackPiece box, float rad)
    {
        float mx = box.w * 0.5f - rad, mz = box.d * 0.5f - rad;
        var off = new Vector2(Rnd(-mx, mx), Rnd(-mz, mz));
        var c = box.content;
        if (c == null) return off;
        var co = new Vector2(c.u - box.u, c.v - box.v);
        float hx = c.w * 0.5f + rad * 0.3f, hz = c.d * 0.5f + rad * 0.3f;
        for (int k = 0; k < 12; k++)
        {
            var rel = off - co;
            if (Mathf.Abs(rel.x) >= hx || Mathf.Abs(rel.y) >= hz) return off;
            off = new Vector2(Rnd(-mx, mx), Rnd(-mz, mz));
        }
        var r2 = off - co;                                                    // 끝내 물건 위면 가까운 가장자리 밖으로
        if (hx - Mathf.Abs(r2.x) < hz - Mathf.Abs(r2.y)) off.x = co.x + (r2.x >= 0f ? 1f : -1f) * hx;
        else off.y = co.y + (r2.y >= 0f ? 1f : -1f) * hz;
        return new Vector2(Mathf.Clamp(off.x, -mx, mx), Mathf.Clamp(off.y, -mz, mz));
    }

    /// <summary>새 알갱이(first 번째부터)가 먼저 앉은 것과 반지름 1.5배 안이면 서로 조금씩 밀어낸다(4번). 자리가 모자라면 겹친 채 쌓인다.</summary>
    void Relax(PackPiece box, int first, float rad)
    {
        float mx = box.w * 0.5f - rad, mz = box.d * 0.5f - rad, sep = rad * 1.5f, sep2 = sep * sep;
        var l = box.beads;
        for (int pass = 0; pass < 4; pass++)
            for (int i = first; i < l.Count; i++)
            {
                var a = l[i];
                for (int j = 0; j < l.Count; j++)
                {
                    if (j == i) continue;
                    var d = a.off - l[j].off; float m2 = d.sqrMagnitude;
                    if (m2 >= sep2) continue;
                    var dir = m2 > 1e-10f ? d / Mathf.Sqrt(m2) : new Vector2(Rnd(-1f, 1f), Rnd(-1f, 1f)).normalized;
                    a.off += dir * ((sep - Mathf.Sqrt(m2)) * (j < first ? 0.6f : 0.3f));
                }
                a.off = new Vector2(Mathf.Clamp(a.off.x, -mx, mx), Mathf.Clamp(a.off.y, -mz, mz));
            }
    }

    void Scatter_(Vector2 at, int n)
    {
        for (int i = 0; i < n && scatter.Count < 160; i++)
        {
            float a = Rnd(0f, Mathf.PI * 2f), s = Rnd(0.25f, 0.8f);
            scatter.Add(new Scatter { pos = at, vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, life = Rnd(0.4f, 0.8f) });
        }
    }

    /// <summary>
    /// D-296 구긴 종이를 열린 상자(가운데에 놓인) 위에서 놓으면'''))

# ── Tick: 알갱이·흩어짐 ──
pairs.append(('''            foreach (var w in p.wads) w.fall.Step(dt);
            if (p.crumple''', '''            foreach (var w in p.wads) w.fall.Step(dt);
            foreach (var bd in p.beads)                                       // 쏟아지는 알갱이: 차례로 출발 → 날아가며 떨어져 앉는다
            {
                if (bd.delay > 0f) { bd.delay -= dt; if (bd.delay <= 0f) bd.fall.Drop(DropZ + 0.04f, 0.15f); continue; }
                if (bd.t < BeadFlight) bd.t = Mathf.Min(BeadFlight, bd.t + dt);
                bd.fall.Step(dt);
            }
            if (p.crumple'''))
pairs.append(('''            if (p.kind != PackPieceKind.Roll && p.kind != PackPieceKind.Tape) p.drop.Step(dt);''',
              '''            if (p.kind != PackPieceKind.Roll && p.kind != PackPieceKind.Tape && p.kind != PackPieceKind.BeadBin) p.drop.Step(dt);'''))
pairs.append(('''    public void Tick(float dt)
    {
        if (dt <= 0f) return;
''', '''    public void Tick(float dt)
    {
        if (dt <= 0f) return;
        for (int k = scatter.Count - 1; k >= 0; k--)
        {
            var s = scatter[k];
            s.life -= dt; s.pos += s.vel * dt; s.vel *= Mathf.Pow(0.05f, dt);
            if (s.life <= 0f) scatter.RemoveAt(k);
        }
'''))

# ── Settling ──
pairs.append(('''                foreach (var w in p.wads) if (w.fall.Active) return true;
            }
            return false;''', '''                foreach (var w in p.wads) if (w.fall.Active) return true;
                foreach (var bd in p.beads) if (bd.Moving) return true;
            }
            return scatter.Count > 0;'''))

# ── TakeOut 로그 ──
pairs.append(('''감쌈 {parcel.wrap}겹(계수 {wrapK:F2}) 테이프''', '''감쌈 {parcel.wrap}겹(계수 {wrapK:F2}) 구긴 종이 {box.wads.Count} 알갱이 {box.beads.Count} 테이프'''))

edit(P, pairs)
