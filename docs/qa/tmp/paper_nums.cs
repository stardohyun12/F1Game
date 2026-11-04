var od = WorldData.Orders; var pk = od.packing; var tr = od.TraitOf("달걀 한 판");
var mm = new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction);
var s = "";
for (int n = 0; n <= 6; n++) { var ph = PackBoard.Setup(pk.boxes[1].inner, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, n); s += "n=" + n + " fill=" + ph.fill.ToString("F2") + " shake=" + (ph.MeasureStandard() * 100f).ToString("F2") + "cm; "; }
return s + " r=" + pk.paperRadius + " m=" + pk.paperMass + " mu=" + pk.paperFriction;
