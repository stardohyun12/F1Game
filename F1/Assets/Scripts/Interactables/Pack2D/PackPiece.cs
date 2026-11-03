using UnityEngine;

public enum PackPieceKind { Parcel, Box, Roll, Tape }

/// <summary>
/// D-294 포장 2D: 작업대 윗면 위의 끌리는 조각 하나. 자리는 판 좌표(u = 화면 오른쪽, v = 화면 위쪽, 판 가운데가 0) — 월드 좌표는 PackBoard 가 바꿔 준다.
/// 순수 자료(유니티 오브젝트 아님). 그리는 건 Presentation/PackBoardView.
/// </summary>
public class PackPiece
{
    public PackPieceKind kind;
    public string name;
    /// <summary>판 위 발자국 크기(u 쪽 · v 쪽, m).</summary>
    public float w, d;
    public float homeU, homeV;
    public float u, v;
    public bool lifted;
    /// <summary>상자 조각: 가운데 자리에 놓여 뚜껑이 열렸다.</summary>
    public bool open;
    /// <summary>켜진 조각만 보이고 집힌다(물건 조각은 손에 받은 물건이 있을 때만).</summary>
    public bool active = true;
    /// <summary>물건 조각이 가리키는 실제 Parcel(08 의 물건·꼬리표 재사용).</summary>
    public Parcel parcel;
    /// <summary>롤 조각의 완충재 이름(뽁뽁이·종이) — world.yaml fillers.</summary>
    public string filler;

    public bool Contains(float pu, float pv) => active && Mathf.Abs(pu - u) <= w * 0.5f && Mathf.Abs(pv - v) <= d * 0.5f;
    public void Home() { u = homeU; v = homeV; lifted = false; open = false; }
}
