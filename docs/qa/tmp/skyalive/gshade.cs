// 큰길 바닥·건물 셰이더 + 해 높이 + 구름 그림자 전역값
var p = new Vector3(-10f, 90f, -83f); string r = "";
foreach (var dx in new[] { 0f, 20f, 40f })
{
    RaycastHit h;
    if (Physics.Raycast(p + new Vector3(dx, 0, 0), Vector3.down, out h, 120f))
    {
        var mr = h.collider.GetComponent<Renderer>();
        r += h.collider.name + " -> " + (mr ? mr.sharedMaterial.shader.name : "no renderer") + "\n";
    }
}
var sun = RenderSettings.sun;
return r + "sun fwd=" + (sun ? (-sun.transform.forward).ToString("F2") : "-") + " clouds=" + Shader.GetGlobalVector("_PCSkyClouds") + " wind=" + Shader.GetGlobalVector("_PCSkyWind") + " seaClock=" + Shader.GetGlobalFloat("_PCSeaClock");
