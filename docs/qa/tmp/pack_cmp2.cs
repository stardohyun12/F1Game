var od = WorldData.Orders; var pk = od.packing;
var tr = od.TraitOf("달걀 한 판");
var mid = pk.boxes[1].inner;
var mm = new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction);
float none = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None).MeasureStandard();
float b60 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, 0, 60).MeasureStandard();
float b90 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, 0, 90).MeasureStandard();
return "none=" + none.ToString("F5") + " b60=" + b60.ToString("F5") + " ratio=" + (b60/none).ToString("F5") + " b90ratio=" + (b90/none).ToString("F5");
