using System.Collections.Generic;
using UnityEngine;

public enum PackPieceKind { Parcel, Box, Roll, Tape, Sheet }

/// <summary>상자 뚜껑 위 테이프 한 줄(상자 가운데 기준 자리). horiz = 판 가로(u) 방향으로 가로지름.</summary>
public struct PackStrip { public float du, dv, len; public bool horiz; }

/// <summary>
/// D-294 포장 2D: 작업대 윗면 위의 끌리는 조각 하나. 자리는 판 좌표(u = 화면 오른쪽, v = 화면 위쪽, 판 가운데가 0) — 월드 좌표는 PackBoard 가 바꿔 준다.
/// 순수 자료(유니티 오브젝트 아님). 그리는 건 Presentation/PackBoardView.
/// </summary>
public class PackPiece
{
    public PackPieceKind kind;
    public string name;
    /// <summary>판 위 발자국 크기(u 쪽 · v 쪽, m). R 로 돌리면 서로 바뀐다.</summary>
    public float w, d;
    public float homeU, homeV, homeW, homeD;
    public float u, v;
    public bool lifted;
    /// <summary>R 로 90° 돌렸다(w·d 가 바뀐 상태).</summary>
    public bool rot90;
    /// <summary>상자 조각: 가운데 자리에 놓여 뚜껑이 열렸다.</summary>
    public bool open;
    /// <summary>켜진 조각만 보이고 집힌다(물건 조각은 손에 받은 물건이 있을 때만).</summary>
    public bool active = true;
    /// <summary>물건 조각이 가리키는 실제 Parcel(08 의 물건·꼬리표 재사용).</summary>
    public Parcel parcel;
    /// <summary>롤 조각의 완충재 이름(뽁뽁이·종이) — world.yaml fillers. 뜯긴 한 장(Sheet)도 같은 이름.</summary>
    public string filler;
    public PackFiller fillerKind;

    // ── 롤 ──
    public int left = 12, full = 12;

    // ── 상자(world.yaml boxes.inner, 눕힌 방향) ──
    public Vector3 inner;
    public bool closed;
    public PackFiller bottom, top;
    /// <summary>들어 있는 물건 조각. 있으면 그 조각은 집히지 않는다(상자와 함께 움직인다).</summary>
    public PackPiece content;
    /// <summary>물건 조각: 들어 있는 상자.</summary>
    public PackPiece holder;
    public PackFit fit = PackFit.Snug;
    public readonly List<PackStrip> strips = new List<PackStrip>();
    public float shakeSecs, rattle;
    public Vector2 jit;

    public bool Contains(float pu, float pv) => active && (holder == null || !holder.closed) && Mathf.Abs(pu - u) <= w * 0.5f && Mathf.Abs(pv - v) <= d * 0.5f;
    public bool Stuffed => content != null || bottom != PackFiller.None;
    public bool Taped => strips.Count > 0;

    public void Home()
    {
        u = homeU; v = homeV; w = homeW; d = homeD; rot90 = false; lifted = false; open = false; jit = Vector2.zero;
        closed = false; bottom = top = PackFiller.None; content = null; holder = null; fit = PackFit.Snug;
        strips.Clear(); shakeSecs = rattle = 0f;
    }

    /// <summary>R: 90° 돌림(발자국 w·d 바뀜).</summary>
    public void Rotate() { (w, d) = (d, w); rot90 = !rot90; }
}
