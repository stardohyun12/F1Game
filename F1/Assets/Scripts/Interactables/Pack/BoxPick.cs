using PitCrew.PackBench;
using UnityEngine;

/// <summary>
/// D-290 포장대 선반의 빈 상자 하나(소·중·대). 상자 몸은 옛 PackBox 그대로(벽·뚜껑·테이프·흔들기 측정).
/// 선반에서 E = 집기 → 작업면에서 E = 놓기. 작업면 위에선 E = 닫기 / 들어 흔들기 / 내려놓기, 오른쪽 클릭 = 다시 열기.
/// </summary>
[RequireComponent(typeof(PackBox))]
public class BoxPick : PackThing
{
    public int sizeIndex;
    public PackBox Box { get; private set; }
    internal Vector3 shelfPos; internal Quaternion shelfRot; internal Transform shelfParent;

    void Awake()
    {
        Box = GetComponent<PackBox>();
        shelfParent = transform.parent; shelfPos = transform.localPosition; shelfRot = transform.localRotation;
    }

    public override void Use(PackTable2 t) => t.UseBox(this);

    internal void BackToShelf()
    {
        transform.SetParent(shelfParent, false);
        transform.localPosition = shelfPos; transform.localRotation = shelfRot;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        Box.ResetForNextDay();
        Box.Open();
        Box.SnapLid();
        Physics.SyncTransforms();
    }
}
