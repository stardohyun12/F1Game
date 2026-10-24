// 갈림목 근처 노면 단차: x 215~240, z 104~113 격자로 위에서 레이, 맞은 것 이름·높이
var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BuildPostcardSlice")).First(x => x != null);
float fs = (float)t.GetProperty("ForkS").GetValue(null);
var sb = new System.Text.StringBuilder("ForkS=" + fs.ToString("F1") + " ");
for (float x = 215; x <= 240; x += 2.5f)
{
    sb.Append("\nx" + x + ":");
    for (float z = 104; z <= 113; z += 1f)
    {
        if (Physics.Raycast(new Vector3(x, 20, z), Vector3.down, out var h, 40f))
            sb.Append(" z" + z + "=" + h.collider.name + "(" + h.collider.transform.parent?.name + ")" + h.point.y.ToString("F2"));
    }
}
return sb.ToString();
