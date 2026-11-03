// 칼날 자리 둘레 10×12 m 를 5 cm 격자로 위에서 쏴 지형 콜라이더가 비는 점(구멍)과 카메라 쪽 레이(눈→점)가 지형을 지나치는 점을 센다
var t = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var mc = t.GetComponent<MeshCollider>();
var eye = new Vector3(279f, 33.58f + 1.6f, -16f);
int miss = 0, through = 0, n = 0; Vector3 fm = default, ft = default; float maxJump = 0; Vector3 fj = default;
for (float z = -31f; z <= -19f; z += .05f) for (float x = 274f; x <= 284f; x += .05f)
{
    n++;
    if (!mc.Raycast(new Ray(new Vector3(x, 80f, z), Vector3.down), out var h, 120f)) { if (miss++ == 0) fm = new Vector3(x, 0, z); continue; }
    // 눈에서 이 점으로 쏜 레이가 이 점보다 30 cm 넘게 멀리서 맞으면 = 그 사이에 구멍
    var d = h.point - eye; float L = d.magnitude;
    if (!mc.Raycast(new Ray(eye, d / L), out var h2, L + 5f)) { if (through++ == 0) ft = h.point; }
    else if (h2.distance > L + .3f) { if (through++ == 0) ft = h.point; }
}
return $"점 {n} · 위에서 빈 점 {miss} (첫 {fm}) · 눈에서 지나친 점 {through} (첫 {ft})";
