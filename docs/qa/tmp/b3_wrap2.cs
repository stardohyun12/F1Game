var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var b = d.PackSpot.Board2D;
var bubble = b.Find(PackPieceKind.Roll, 0); var pc = b.Find(PackPieceKind.Parcel);
if (b.Settling) return "settling";
b.SimDrag(bubble, pc.u, pc.v);
return "wrap=" + pc.wrap;
