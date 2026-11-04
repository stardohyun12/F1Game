import sys
sys.path.insert(0, 'D:/Github/F1Game/docs/qa/tmp')
from rep import edit

P = 'D:/Github/F1Game/F1/Assets/Scripts/Presentation/PackBoardView.cs'
pairs = []

pairs.append(('''/// 뜯긴 한 장(Sheet)은 생기고 사라지므로''', '''/// D-296 알갱이(통의 흰 점 · 삽에 뜬 한 줌 · 상자에 쏟은 알갱이 · 흩어지는 알갱이)는 작은 육각형 원판 수백 개를 메시 하나에 모아 그린다 — 드로콜 1.
/// 뜯긴 한 장(Sheet)은 생기고 사라지므로'''))

pairs.append(('''    Material Mat(Color c)
    {''', '''    static readonly Color beadC = new Color(0.97f, 0.97f, 0.94f), binC = new Color(0.40f, 0.46f, 0.52f), scoopC = new Color(0.72f, 0.72f, 0.76f);
    Mesh beadMesh;
    readonly List<Vector3> bv = new List<Vector3>(4096), bn = new List<Vector3>(4096);
    readonly List<int> bt = new List<int>(8192);
    static readonly Vector2[] hex = { new Vector2(1f, 0f), new Vector2(0.5f, 0.866f), new Vector2(-0.5f, 0.866f), new Vector2(-1f, 0f), new Vector2(-0.5f, -0.866f), new Vector2(0.5f, -0.866f) };

    Material Mat(Color c)
    {'''))

pairs.append(('''        Place(slotFrame, PackBoard.SlotU, PackBoard.SlotV, 0.0085f); Place(slotIn, PackBoard.SlotU, PackBoard.SlotV, 0.0095f);
    }
''', '''        Place(slotFrame, PackBoard.SlotU, PackBoard.SlotV, 0.0085f); Place(slotIn, PackBoard.SlotU, PackBoard.SlotV, 0.0095f);
        var bg = new GameObject("알갱이 메시");                               // 월드 좌표로 꼭짓점을 직접 쓴다
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

    Vector3 WorldAt(float u, float v, float y) => board.Origin + board.Right * u + board.Up * v + Vector3.up * y;
    static float Frac(float x) => x - Mathf.Floor(x);

    /// <summary>알갱이 전부(통의 점 · 삽의 한 줌 · 열린 상자 안 · 흩어지는 것)를 한 메시로.</summary>
    void DrawBeads()
    {
        bv.Clear(); bn.Clear(); bt.Clear();
        float r = PackBoard.BeadRadius * 0.6f;                                  // 판 위 알갱이 반지름 = 실제 × 상자와 같은 축척(0.6)
        foreach (var p in board.pieces)
        {
            if (!p.active) continue;
            if (p.kind == PackPieceKind.BeadBin)                                // 통: 흰 점 가득(자리 고정 무작위)
            {
                const int cols = 16, rows = 6;
                for (int i = 0; i < cols * rows; i++)
                {
                    float jx = Frac(Mathf.Sin(i * 12.9898f) * 43758.5453f) - 0.5f, jy = Frac(Mathf.Sin(i * 78.233f) * 12345.6789f) - 0.5f;
                    float du = ((i % cols + 0.5f) / cols - 0.5f + jx * 0.5f / cols) * (p.w - 0.03f), dv = ((i / cols + 0.5f) / rows - 0.5f + jy * 0.5f / rows) * (p.d - 0.03f);
                    Dot(WorldAt(p.u + du, p.v + dv, 0.027f + 0.015f + 0.003f), r * 1.2f);
                }
            }
            else if (p.kind == PackPieceKind.Scoop)                             // 삽에 뜬 한 줌
            {
                int n = PackBoard.BeadScoop;
                float y = 0.012f + 0.015f + (p.lifted ? 0.07f : 0f) + 0.015f + 0.004f;
                for (int i = 0; i < n; i++)
                {
                    float a = i * 2.39996f, s = Mathf.Sqrt((i + 0.5f) / n) * 0.035f;
                    Dot(WorldAt(p.u + Mathf.Cos(a) * s * 1.3f, p.v + Mathf.Sin(a) * s * 0.8f, y + (i % 4) * 0.0012f), r * 1.2f);
                }
            }
            else if (p.kind == PackPieceKind.Box && !p.closed && p.beads.Count > 0)
            {
                float lift = p.lifted ? 0.07f : 0f, pu = p.u + p.jit.x, pv = p.v + p.jit.y;
                for (int i = 0; i < p.beads.Count; i++)
                {
                    var bd = p.beads[i];
                    if (bd.delay > 0f) continue;                                 // 아직 안 쏟아짐
                    float k = Mathf.Clamp01(bd.t / PackBoard.BeadFlight); k = k * k * (3f - 2f * k);
                    var pos = Vector2.Lerp(bd.from, bd.off, k);
                    Dot(WorldAt(pu + pos.x, pv + pos.y, BoxTop + lift + p.drop.z + bd.fall.z + 0.003f + (i % 5) * 0.0012f), r);
                }
            }
        }
        foreach (var s in board.scatter) Dot(WorldAt(s.pos.x, s.pos.y, 0.06f), r);
        beadMesh.Clear();
        if (bv.Count == 0) return;
        beadMesh.SetVertices(bv); beadMesh.SetNormals(bn); beadMesh.SetTriangles(bt, 0);
        beadMesh.RecalculateBounds();
    }
'''))

# 조각 색
pairs.append(('''var c = p.kind == PackPieceKind.Parcel ? parcelC : p.kind == PackPieceKind.Box ? boxC : p.kind == PackPieceKind.Tape ? tapeC : FillerColor(p.fillerKind);''',
              '''var c = p.kind == PackPieceKind.Parcel ? parcelC : p.kind == PackPieceKind.Box ? boxC : p.kind == PackPieceKind.Tape ? tapeC : p.kind == PackPieceKind.BeadBin ? binC : p.kind == PackPieceKind.Scoop ? scoopC : FillerColor(p.fillerKind);'''))

# 글자: 삽은 글자 없음
pairs.append(('''bool hideLabel = p.crumple || (p.kind == PackPieceKind.Box && p.Stuffed && !p.closed);''',
              '''bool hideLabel = p.crumple || p.kind == PackPieceKind.Scoop || (p.kind == PackPieceKind.Box && p.Stuffed && !p.closed);'''))

# LateUpdate 끝에서 알갱이
pairs.append(('''            if (t.text != s) t.text = s;
        }
    }''', '''            if (t.text != s) t.text = s;
        }
        DrawBeads();
    }'''))

edit(P, pairs)
