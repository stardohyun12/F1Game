// 테이프 디스펜서를 쥐고 상자 왼쪽 밖 → 상자 위 가운데 넘어까지 (아직 놓지 않음 — 놓으면 EndTape 가 닫는다)
var b = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>().PackSpot.Board2D;
var tape = b.Find(PackPieceKind.Tape);
float cu = PackBoard.SlotU, cv = PackBoard.SlotV + 0.02f;
if (b.Held == null)
{
    b.Begin(tape.u, tape.v);
    typeof(PackBoard).GetField("simHold", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(b, true);
    b.Move(cu - 0.15f, cv);
    return "grabbed " + (b.Held != null ? b.Held.name : "-");
}
b.Move(cu + 0.04f, cv);
return "held=" + b.Held.name + " u=" + b.Held.u.ToString("F3");
