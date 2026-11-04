// ① 중 상자를 가운데로 · 뽁뽁이 두 겹 감기 (감기는 Settling 이 끝나야 다음 겹이 먹으므로 두 번째 겹은 b3_wrap2 에서)
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var b = d.PackSpot.Board2D;
PackPiece bM = null; for (int i = 0; i < 3; i++) { var x = b.Find(PackPieceKind.Box, i); if (x != null && x.name != null && x.name.Contains("중")) bM = x; }
var bubble = b.Find(PackPieceKind.Roll, 0); var pc = b.Find(PackPieceKind.Parcel);
b.SimDrag(bM, PackBoard.SlotU, PackBoard.SlotV);
b.SimDrag(bubble, pc.u, pc.v);
return "box=" + bM.name + " open=" + bM.open + " wrap=" + pc.wrap + " cam h=" + b.CamHeight.ToString("F3");
