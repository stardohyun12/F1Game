var od = WorldData.Orders; var pk = od.packing; var tr = od.TraitOf("달걀 한 판");
var mid = pk.boxes[1].inner; var mm = new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction);
var sb = new System.Text.StringBuilder();
for (int w = 0; w <= 2; w++)
{
    var ph = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, w);
    float sh = ph.MeasureStandard(out int hits);
    float s0 = PackRules.StartFromShake(tr.trait, PackFiller.None, PackFiller.None, 0f);
    float st = PackRules.StartFromShake(tr.trait, PackFiller.None, PackFiller.None, sh, ph.wrapK);
    sb.AppendLine($"wrap {w}: part {ph.part.x:F3} gap/2 {ph.Limit.x * 1000f:F2}mm e {ph.e:F3} wrapK {ph.wrapK:F2} shake {sh * 100f:F2}cm hits {hits} start {st * 100f:F1}% dmg {(s0 - st) * 100f:F2}%p");
}
return sb.ToString();
