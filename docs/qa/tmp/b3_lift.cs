// 감싼 물건을 쥐고 열린 상자 위로 (아직 놓지 않음)
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var b = d.PackSpot.Board2D;
var pc = b.Find(PackPieceKind.Parcel);
if (b.Held == null)
{
    b.Begin(pc.u, pc.v);
    typeof(PackBoard).GetField("simHold", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(b, true);
}
b.Move(PackBoard.SlotU - 0.03f, PackBoard.SlotV + 0.06f);
return "held=" + (b.Held != null ? b.Held.name : "-") + " grip=" + b.Gripping + " lifted=" + pc.lifted + " hand=" + b.HandDrawn;
