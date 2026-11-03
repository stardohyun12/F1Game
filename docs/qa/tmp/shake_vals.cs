var od = WorldData.Orders; var pk = od.packing; var tr = od.TraitOf("달걀 한 판");
var sb = new System.Text.StringBuilder();
for (int bi = 0; bi < 3; bi++)
{
    var bx = pk.boxes[bi]; var m = new PackPhysics.Mat(bx.mass, bx.bounce, bx.friction);
    foreach (var f in new[] { PackFiller.None, PackFiller.Bubble, PackFiller.Paper })
    {
        float v = PackBoard.Setup(bx.inner, false, m, tr, false, f, f).MeasureStandard();
        sb.Append(bx.name + " 바닥·덮기=" + f + " → " + (v * 100f).ToString("F1") + " cm; ");
    }
}
return sb.ToString();
