// 과제 20261105-0453 ①: 건물 안 상자마다 네 옆 — 상자 면까지(m) vs 그 건물 벽 콜라이더까지(m). 면이 벽보다 멀면 벽 밖으로 샌 것
var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var sb = new System.Text.StringBuilder();
bool bfSaved = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = PlayerPrefs.GetInt("ifxbf", 0) == 1;
sb.AppendLine("backfaces=" + Physics.queriesHitBackfaces);
foreach (var it in list)
{
    var inv = it.box.inverse;
    Vector3 o = inv.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up;
    Vector3 cq = it.box.MultiplyPoint3x4(o);
    sb.Append(it.name + " half=" + it.half.ToString("F2") + " c=" + inv.MultiplyPoint3x4(Vector3.zero).ToString("F1") + " |");
    Vector3[] ax = { inv.MultiplyVector(Vector3.right).normalized, -inv.MultiplyVector(Vector3.right).normalized, inv.MultiplyVector(Vector3.forward).normalized, -inv.MultiplyVector(Vector3.forward).normalized };
    float[] hw = { it.half.x, it.half.x, it.half.z, it.half.z };
    string[] nm = { "+x", "-x", "+z", "-z" };
    for (int s = 0; s < 4; s++)
    {
        var hs = Physics.RaycastAll(o, ax[s], 30f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hs, (a, b) => a.distance.CompareTo(b.distance));
        string w = "none";
        foreach (var h in hs) if (h.transform.IsChildOf(it.building) && A7ShadeGlobals.IsWall(h.collider)) { w = h.distance.ToString("F2") + ":" + h.collider.name; break; }
        string other = hs.Length > 0 ? hs[0].distance.ToString("F2") + ":" + hs[0].collider.name : "-";
        sb.Append(" " + nm[s] + " face=" + hw[s].ToString("F2") + " wall=" + w + " first=" + other + ";");
    }
    sb.AppendLine();
}
Physics.queriesHitBackfaces = bfSaved;
return sb.ToString();
