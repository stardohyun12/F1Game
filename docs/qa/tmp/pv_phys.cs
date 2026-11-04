var od = WorldData.Orders; var pk = od.packing; var tr = od.TraitOf("달걀 한 판");
var mid = pk.boxes[1].inner; var mm = new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction);
float none = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None).MeasureStandard() * 100f;
float wrap2 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 2).MeasureStandard() * 100f;
float paper4 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, 4).MeasureStandard() * 100f;
float beads2 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, 0, PackBoard.BeadScoop * 2).MeasureStandard() * 100f;
return "없음 " + none.ToString("F2") + " | 뽁뽁이2겹 " + wrap2.ToString("F2") + " | 종이4 " + paper4.ToString("F2") + " | 알갱이2줌 " + beads2.ToString("F2") + "   (기준 21b: 13.76 / 2.70 / 2.02 / 6.41)";
