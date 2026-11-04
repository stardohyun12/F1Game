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
    /// <summary>D-296 물건 조각: 뽁뽁이로 감싼 겹 수(0~PackRules.WrapMax). 물건 겉에 붙고 크기가 조금 커진다. Home 에서 0.</summary>
    public int wrap;

    /// <summary>D-296 뜯은 종이 한 장(Sheet): 집는 순간 구겨진 둥근 덩어리(crumple). crumpleT 0 = 평평한 장 → 1 = 다 구겨짐(그림용).</summary>
    public bool crumple;
    public float crumpleT;

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
    /// <summary>D-296 상자 조각: 열린 상자 틈에 자리 잡은 구긴 종이 덩어리(자리는 상자 가운데 기준 판 m). 최대 PackBoard.PaperMax.</summary>
    public readonly List<Wad> wads = new List<Wad>();
    public sealed class Wad { public Vector2 off; public PackPhysics.Fall fall; }
    /// <summary>shakeSecs = 흔든 시간(s) · rattle = 덜그럭 값 = 1.5 s 동안 물건이 상자 기준 움직인 거리 합의 최댓값(m, PackPhysics.PeakWindow).</summary>
    public float shakeSecs, rattle;
    public Vector2 jit;

    // ── D-295 물리 ──
    /// <summary>상자 조각: 안에 든 물건의 물리(물건이 들어간 동안만).</summary>
    public PackPhysics phys;
    /// <summary>놓은 순간부터 떨어지는 높이(조각 자신 · 상자 안 바닥 완충재 · 덮은 완충재).</summary>
    public PackPhysics.Fall drop, dropBottom, dropTop;
    /// <summary>들어가지 못하고 상자 벽 위에 걸려 있는 남은 시간(s). 0 이면 아님.</summary>
    public float perch;
    /// <summary>손으로 끄는 동안의 속도(판 m/s) — 놓을 때 던지는 속도. 상자의 가속(흔들림)도 여기서 구한다.</summary>
    public Vector2 hand, lastPos;

    public bool Contains(float pu, float pv) => active && perch <= 0f && (holder == null || !holder.closed) && Mathf.Abs(pu - u) <= w * 0.5f && Mathf.Abs(pv - v) <= d * 0.5f;
    public bool Stuffed => content != null || bottom != PackFiller.None || wads.Count > 0;
    public bool Taped => strips.Count > 0;

    public void Home()
    {
        u = homeU; v = homeV; w = homeW; d = homeD; rot90 = false; lifted = false; open = false; jit = Vector2.zero;
        wrap = 0; closed = false; bottom = top = PackFiller.None; content = null; holder = null; fit = PackFit.Snug;
        strips.Clear(); wads.Clear(); shakeSecs = rattle = 0f; phys = null; drop = dropBottom = dropTop = default; perch = 0f; hand = Vector2.zero; lastPos = new Vector2(u, v);
    }

    /// <summary>R: 90° 돌림(발자국 w·d 바뀜).</summary>
    public void Rotate() { (w, d) = (d, w); rot90 = !rot90; }
}
