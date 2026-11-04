p = 'F1/Assets/Scripts/Presentation/PackBoardView.cs'
s = open(p, encoding='utf-8', newline='').read()
crlf = '\r\n' in s
s = s.replace('\r\n', '\n')


def rep(a, b, cnt=1):
    global s
    assert s.count(a) == cnt, (a[:60], s.count(a))
    s = s.replace(a, b)


rep("""/// 뜯긴 한 장(Sheet)은 생기고 사라지므로 조각은 처음 볼 때 만든다. 보드가 켜진 동안만 보인다. 조각 자료는 건드리지 않는다.
/// </summary>""", """/// 뜯긴 한 장(Sheet)은 생기고 사라지므로 조각은 처음 볼 때 만든다. 보드가 켜진 동안만 보인다. 조각 자료는 건드리지 않는다.
/// D-300 알갱이는 큰 원 + 작은 밝은 원(두 톤)으로 호를 그리며 떨어지고, 뽁뽁이 띠·구긴 종이 다각형·쏟는 삽·손·상자 덜컹은 PackBoardFx 가 그린다(소리도).
/// </summary>""")
rep("""    static readonly Color beadC = new Color(0.97f, 0.97f, 0.94f), binC""", """    static readonly Color beadC = new Color(0.97f, 0.97f, 0.94f), beadDkC = new Color(0.70f, 0.74f, 0.79f), binC""")
rep("""    Mesh beadMesh;
    readonly List<Vector3> bv = new List<Vector3>(4096), bn = new List<Vector3>(4096);
    readonly List<int> bt = new List<int>(8192);
    static readonly Vector2[] hex = { new Vector2(1f, 0f), new Vector2(0.5f, 0.866f), new Vector2(-0.5f, 0.866f), new Vector2(-1f, 0f), new Vector2(-0.5f, -0.866f), new Vector2(0.5f, -0.866f) };
""", """    PackBoardFx fx;
    PackBoardFx.Layer beadD, beadL;                                   // 알갱이 두 톤(어두운 큰 원 · 밝은 작은 원)
    readonly List<Vector3> ring = new List<Vector3>(12);
""")
rep("""        var bg = new GameObject("알갱이 메시");                               // 월드 좌표로 꼭짓점을 직접 쓴다
        bg.transform.SetParent(root, false); bg.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        beadMesh = new Mesh { name = "pack2d beads(runtime)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        beadMesh.MarkDynamic();
        bg.AddComponent<MeshFilter>().sharedMesh = beadMesh;
        var mr = bg.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Mat(beadC); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
    }

    void Dot(Vector3 c, float r)
    {
        int i = bv.Count;
        bv.Add(c); bn.Add(Vector3.up);
        for (int k = 0; k < 6; k++) { bv.Add(c + new Vector3(hex[k].x * r, 0f, hex[k].y * r)); bn.Add(Vector3.up); }
        for (int k = 0; k < 6; k++) { bt.Add(i); bt.Add(i + 1 + (k + 1) % 6); bt.Add(i + 1 + k); }   // 위에서 보아 시계 방향 = 앞면
    }
""", """        beadD = new PackBoardFx.Layer("알갱이 어두운 톤", root, Mat(beadDkC));   // 월드 좌표로 꼭짓점을 직접 쓴다
        beadL = new PackBoardFx.Layer("알갱이 밝은 톤", root, Mat(beadC));
        fx = new PackBoardFx(board, root, Mat);
    }

    void OnDestroy() { if (fx != null) fx.Dispose(); }

    void Round(PackBoardFx.Layer l, Vector3 c, float r, int sides)
    {
        ring.Clear();
        for (int k = 0; k < sides; k++) { float a = k * Mathf.PI * 2f / sides; ring.Add(c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r)); }
        l.Fan(c, ring, false);                                          // 위에서 보아 시계 방향 = 앞면
    }

    /// <summary>통 안의 알갱이: 밝은 한 톤 점.</summary>
    void Dot(Vector3 c, float r) => Round(beadL, c, r, 6);

    /// <summary>쌓이고 쏟아지는 알갱이: 어두운 작은 원 위에 밝은 더 작은 원(두 톤).</summary>
    void Bead(Vector3 c, float r)
    {
        Round(beadD, c, r, 8);
        Round(beadL, c + (board.Up * 0.2f - board.Right * 0.2f) * r + Vector3.up * 0.0012f, r * 0.6f, 6);
    }
""")
rep("""        bv.Clear(); bn.Clear(); bt.Clear();
        float r = PackBoard.BeadRadius * 0.6f;""", """        beadD.Clear(); beadL.Clear();
        float r = PackBoard.BeadRadius * 0.6f;""")
rep("""                    Dot(WorldAt(p.u + Mathf.Cos(a) * s * 1.3f, p.v + Mathf.Sin(a) * s * 0.8f, y + (i % 4) * 0.0012f), r * 1.2f);""", """                    Bead(WorldAt(p.u + Mathf.Cos(a) * s * 1.3f, p.v + Mathf.Sin(a) * s * 0.8f, y + (i % 4) * 0.0012f), r * 1.2f);""")
rep("""                    float k = Mathf.Clamp01(bd.t / PackBoard.BeadFlight); k = k * k * (3f - 2f * k);
                    var pos = Vector2.Lerp(bd.from, bd.off, k);
                    Dot(WorldAt(pu + pos.x, pv + pos.y, BoxTop + lift + p.drop.z + bd.fall.z + 0.003f + (i % 5) * 0.0012f), r);""", """                    float k0 = Mathf.Clamp01(bd.t / PackBoard.BeadFlight), k = k0 * k0 * (3f - 2f * k0);
                    var pos = Vector2.Lerp(bd.from, bd.off, k);
                    float arc = 4f * k0 * (1f - k0) * 0.045f;                    // D-300 호를 그리며: 날아가는 중간이 가장 높다
                    Bead(WorldAt(pu + pos.x, pv + pos.y, BoxTop + lift + p.drop.z + bd.fall.z + arc + 0.003f + (i % 5) * 0.0012f), r * (k0 < 1f ? 1.25f : 1f));""")
rep("""        foreach (var s in board.scatter) Dot(WorldAt(s.pos.x, s.pos.y, 0.06f), r);
        int sig = bv.Count;                                                     // 알갱이가 안 움직이면 메시를 다시 올리지 않는다(500개 다시 올리면 에디터 프레임이 11→39 ms)
        for (int i = 0; i < bv.Count; i += 7) sig = sig * 31 + bv[i].GetHashCode();
        if (sig == beadSig) return;
        beadSig = sig;
        beadMesh.Clear();
        if (bv.Count == 0) return;
        beadMesh.SetVertices(bv); beadMesh.SetNormals(bn); beadMesh.SetTriangles(bt, 0);
        beadMesh.RecalculateBounds();
    }""", """        foreach (var s in board.scatter) Bead(WorldAt(s.pos.x, s.pos.y, 0.06f), r);
        beadD.FlushIfChanged(); beadL.FlushIfChanged();                         // 알갱이가 안 움직이면 메시를 다시 올리지 않는다(500개 다시 올리면 에디터 프레임이 11→39 ms)
    }""")
rep("    int beadSig;\n\n", "")
rep("""        var b = Slab(p.name, root, c, p.crumple ? PrimitiveType.Cylinder : PrimitiveType.Cube); b.localScale = new Vector3(p.w, 0.03f, p.d); body[p] = b;   // 구긴 종이 = 원통(둥근 덩어리 대역)""", """        var b = Slab(p.name, root, c, p.crumple ? PrimitiveType.Cylinder : PrimitiveType.Cube); b.localScale = new Vector3(p.w, 0.03f, p.d); body[p] = b;
        if (p.crumple) b.GetComponent<Renderer>().enabled = false;           // D-300 구긴 종이는 PackBoardFx 가 울퉁불퉁 다각형으로 그린다(몸은 높이 계산용)""")
rep("""        var pcs = board.pieces;
        for (int n = 0; n < pcs.Count; n++) Ensure(pcs[n]);""", """        float now = Time.unscaledTime;
        fx.Begin();
        var pcs = board.pieces;
        for (int n = 0; n < pcs.Count; n++) Ensure(pcs[n]);""")
rep("""            float pu = p.u + p.jit.x, pv = p.v + p.jit.y;
            float z = p.perch > 0f""", """            float pu = p.u + p.jit.x, pv = p.v + p.jit.y;
            var shakeBox = p.kind == PackPieceKind.Box ? p : p.holder;           // D-300 안의 것이 움직인 만큼 상자 겉(과 든 물건)이 덜컹
            var sk = shakeBox != null && shakeBox.kind == PackPieceKind.Box ? fx.ShakeOf(shakeBox, now) : Vector3.zero;
            pu += sk.x; pv += sk.y;
            float z = p.perch > 0f""")
rep("""            float baseY = 0.012f + h * 0.5f + lift + z;
            if (p.holder != null) baseY = BoxTop + (p.holder.bottom != PackFiller.None ? SheetH : 0f) + ParcelH * 0.5f + (p.holder.lifted ? 0.07f : 0f) + p.holder.drop.z + z;""", """            float baseY = 0.012f + h * 0.5f + lift + z + sk.z;
            if (p.holder != null) baseY = BoxTop + (p.holder.bottom != PackFiller.None ? SheetH : 0f) + ParcelH * 0.5f + (p.holder.lifted ? 0.07f : 0f) + p.holder.drop.z + z + sk.z;""")
rep("""            float topY = baseY + h * 0.5f;
            if (shell""", """            float topY = baseY + h * 0.5f;
            if (p.crumple) fx.Paper(pu, pv, topY + 0.002f, p.w * 0.55f, p.crumpleT, 7 + pcs.IndexOf(p) % 5 * 3);
            if (p.kind == PackPieceKind.Parcel && p.wrap > 0) fx.WrapBands(p, pu, pv, topY + 0.003f, p.w, p.d, WrapRim, now);
            if (shell""")
rep("""            if (extras.TryGetValue(p, out var x)) topY = DrawBox(p, x, pu, pv, lift + z);""", """            if (extras.TryGetValue(p, out var x)) topY = DrawBox(p, x, pu, pv, lift + z + sk.z);""")
rep("""        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        DrawBeads();""", """        fx.End(now);
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        DrawBeads();""")
rep("""        while (x.wads.Count < p.wads.Count) x.wads.Add(Slab(p.name + " 구긴 종이", root, paperC, PrimitiveType.Cylinder));
        float rad = PackBoard.PaperRadius * 0.6f;
        for (int k = 0; k < x.wads.Count; k++)
        {
            bool has = k < p.wads.Count && !p.closed;
            SetAct(x.wads[k], has);
            if (!has) continue;
            var wd = p.wads[k];
            x.wads[k].GetComponent<Renderer>().sharedMaterial = Mat(k % 2 == 0 ? paperC : wadC);
            x.wads[k].localScale = new Vector3(rad * 2.2f, WadH * 0.5f, rad * 2.2f);
            Place(x.wads[k], pu + wd.off.x, pv + wd.off.y, BoxTop + lift + wd.fall.z + WadH * 0.5f + 0.002f);
        }""", """        float rad = PackBoard.PaperRadius * 0.6f;                               // D-300 울퉁불퉁 다각형(PackBoardFx.Paper) — 슬랩 아님
        if (!p.closed)
            for (int k = 0; k < p.wads.Count; k++)
            {
                var wd = p.wads[k];
                fx.Paper(pu + wd.off.x, pv + wd.off.y, BoxTop + lift + wd.fall.z + 0.022f, rad * 1.15f, 1f, 100 + k * 17 + p.name.Length);
            }""")
open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s)
print('crlf', crlf)
