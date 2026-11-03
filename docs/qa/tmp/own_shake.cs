var od = WorldData.Orders; var pk = od.packing; var tr = od.TraitOf("달걀 한 판");
var sb = new System.Text.StringBuilder();
for (int bi = 1; bi < 3; bi++)
{
    var bx = pk.boxes[bi]; var m = new PackPhysics.Mat(bx.mass, bx.bounce, bx.friction);
    var f = bi == 1 ? PackFiller.Bubble : PackFiller.None;
    var ph = PackBoard.Setup(bx.inner, false, m, tr, false, f, f);
    ph.Reset(Vector2.zero, Vector2.zero);
    float dt = 1f / 60f, w = 28f;
    for (int i = 0; i < 156; i++) { float t = i * dt; ph.Advance(dt, new Vector2(-0.12f * w * w * Mathf.Sin(w * t), 0f)); }
    sb.Append(bx.name + "(" + f + ") 봇식 2.6s 직접: 흔든시간 " + ph.ShakeTime.ToString("F2") + "s 합 " + (ph.PeakWindow * 100f).ToString("F1") + "cm 부딪힘 " + ph.Hits + "; ");
}
return sb.ToString();
