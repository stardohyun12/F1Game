// Play 전용 임시: 1인칭 포장 작업대 대역(회색·단색 primitive). Stop 하면 사라진다. 카메라를 작업대 위로.
var old = GameObject.Find("UIMOCK_Bench"); if (old) UnityEngine.Object.Destroy(old);
var root = new GameObject("UIMOCK_Bench").transform;
var O = new Vector3(__OX__f, 0f, __OZ__f);
RaycastHit gh; Physics.Raycast(O + Vector3.up * 60f, Vector3.down, out gh, 120f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
O.y = gh.point.y; root.position = O;
var sh = Shader.Find("Universal Render Pipeline/Lit");
System.Func<Color, Material> M = c => { var m = new Material(sh); m.color = c; m.SetFloat("_Smoothness", 0.1f); return m; };
var mTable = M(new Color(0.45f, 0.36f, 0.30f)); var mWall = M(new Color(0.55f, 0.52f, 0.50f)); var mBox = M(new Color(0.70f, 0.54f, 0.36f));
var mBoxIn = M(new Color(0.58f, 0.44f, 0.29f)); var mPaper = M(new Color(0.80f, 0.79f, 0.75f)); var mBub = M(new Color(0.84f, 0.88f, 0.92f));
var mGray = M(new Color(0.62f, 0.62f, 0.62f)); var mLabel = M(new Color(0.93f, 0.88f, 0.78f)); var mTape = M(new Color(0.78f, 0.62f, 0.38f));
System.Func<PrimitiveType, Vector3, Vector3, Vector3, Material, string, Transform> P = (t, p, s, e, m, n) =>
{
    var g = GameObject.CreatePrimitive(t); g.name = n; UnityEngine.Object.Destroy(g.GetComponent<Collider>());
    g.transform.SetParent(root, false); g.transform.localPosition = p; g.transform.localScale = s; g.transform.localEulerAngles = e;
    g.GetComponent<Renderer>().sharedMaterial = m; return g.transform;
};
// 작업대 1.6×0.8 높이 0.9, 뒤 벽(합판) + 선반
P(PrimitiveType.Cube, new Vector3(0, 0.88f, 0), new Vector3(1.8f, 0.06f, 0.9f), Vector3.zero, mTable, "top");
P(PrimitiveType.Cube, new Vector3(0, 1.5f, 0.5f), new Vector3(2.4f, 1.6f, 0.04f), Vector3.zero, mWall, "wall");
P(PrimitiveType.Cube, new Vector3(0, 1.42f, 0.36f), new Vector3(1.8f, 0.03f, 0.26f), Vector3.zero, mTable, "shelf");
// 선반 위: 뽁뽁이 롤 2, 신문 더미, 테이프
P(PrimitiveType.Cylinder, new Vector3(-0.62f, 1.55f, 0.34f), new Vector3(0.22f, 0.12f, 0.22f), Vector3.zero, mBub, "roll1");
P(PrimitiveType.Cylinder, new Vector3(-0.36f, 1.53f, 0.34f), new Vector3(0.18f, 0.10f, 0.18f), Vector3.zero, mBub, "roll2");
P(PrimitiveType.Cube, new Vector3(0.45f, 1.48f, 0.34f), new Vector3(0.36f, 0.10f, 0.24f), new Vector3(0, 6, 0), mPaper, "papers");
P(PrimitiveType.Cylinder, new Vector3(0.75f, 1.47f, 0.32f), new Vector3(0.10f, 0.025f, 0.10f), new Vector3(0, 0, 0), mTape, "tape");
// 열린 상자(가운데 앞) 0.42×0.30×0.34
var bx = new Vector3(0.0f, 0.91f, -0.08f); float W = 0.42f, D = 0.34f, H = 0.26f, T = 0.012f;
P(PrimitiveType.Cube, bx + new Vector3(0, T / 2, 0), new Vector3(W, T, D), Vector3.zero, mBoxIn, "box_floor");
P(PrimitiveType.Cube, bx + new Vector3(0, H / 2, -D / 2), new Vector3(W, H, T), Vector3.zero, mBox, "box_front");
P(PrimitiveType.Cube, bx + new Vector3(0, H / 2, D / 2), new Vector3(W, H, T), Vector3.zero, mBoxIn, "box_back");
P(PrimitiveType.Cube, bx + new Vector3(-W / 2, H / 2, 0), new Vector3(T, H, D), Vector3.zero, mBox, "box_l");
P(PrimitiveType.Cube, bx + new Vector3(W / 2, H / 2, 0), new Vector3(T, H, D), Vector3.zero, mBox, "box_r");
// 뚜껑 날개 4 (밖으로 젖혀짐)
P(PrimitiveType.Cube, bx + new Vector3(0, H - 0.04f, -D / 2 - 0.075f), new Vector3(W, T, 0.17f), new Vector3(-50, 0, 0), mBox, "flap_f");
P(PrimitiveType.Cube, bx + new Vector3(0, H + 0.06f, D / 2 + 0.06f), new Vector3(W, T, 0.17f), new Vector3(55, 0, 0), mBox, "flap_b");
P(PrimitiveType.Cube, bx + new Vector3(-W / 2 - 0.08f, H + 0.02f, 0), new Vector3(0.17f, T, D), new Vector3(0, 0, 25), mBox, "flap_l");
P(PrimitiveType.Cube, bx + new Vector3(W / 2 + 0.08f, H + 0.02f, 0), new Vector3(0.17f, T, D), new Vector3(0, 0, -25), mBox, "flap_r");
// 상자 안: 바닥 신문지 구김(작은 기운 상자들) + 물건 대역(회색 상자 = 달걀 판 자리)
var rnd = new System.Random(7);
for (int i = 0; i < 14; i++)
{
    float x = (float)(rnd.NextDouble() - 0.5) * (W - 0.06f), z = (float)(rnd.NextDouble() - 0.5) * (D - 0.06f);
    float s = 0.05f + (float)rnd.NextDouble() * 0.04f;
    P(PrimitiveType.Cube, bx + new Vector3(x, 0.035f, z), new Vector3(s, s * 0.7f, s), new Vector3(rnd.Next(0, 60), rnd.Next(0, 360), rnd.Next(0, 60)), mPaper, "crumple" + i);
}
P(PrimitiveType.Cube, bx + new Vector3(-0.03f, 0.10f, 0.0f), new Vector3(0.28f, 0.07f, 0.24f), new Vector3(0, 4, 0), mGray, "item_eggs");
// 작업대 위: 뽁뽁이 덩어리(오른쪽), 신문지 몇 장(왼쪽), 라벨(앞 오른쪽)
for (int i = 0; i < 9; i++)
{
    float x = 0.42f + (float)rnd.NextDouble() * 0.22f, z = -0.12f + (float)rnd.NextDouble() * 0.22f; float s = 0.07f + (float)rnd.NextDouble() * 0.05f;
    P(PrimitiveType.Sphere, new Vector3(x, 0.93f + s * 0.3f, z), new Vector3(s * 1.3f, s * 0.6f, s), new Vector3(0, rnd.Next(0, 360), 0), mBub, "bub" + i);
}
P(PrimitiveType.Cube, new Vector3(-0.5f, 0.915f, -0.05f), new Vector3(0.34f, 0.008f, 0.26f), new Vector3(0, -12, 0), mPaper, "sheet1");
P(PrimitiveType.Cube, new Vector3(-0.46f, 0.922f, -0.02f), new Vector3(0.34f, 0.008f, 0.26f), new Vector3(0, 9, 0), mPaper, "sheet2");
for (int i = 0; i < 4; i++) P(PrimitiveType.Cube, new Vector3(-0.62f + i * 0.05f, 0.95f, -0.22f + (i % 2) * 0.04f), new Vector3(0.06f, 0.045f, 0.06f), new Vector3(rnd.Next(0, 50), rnd.Next(0, 360), rnd.Next(0, 50)), mPaper, "crump_t" + i);
P(PrimitiveType.Cube, new Vector3(0.36f, 0.915f, -0.30f), new Vector3(0.14f, 0.004f, 0.09f), new Vector3(0, -8, 0), mLabel, "label");
// 카메라: 감독·걷기·추격 카메라 끄고 작업대 앞 눈높이 1.55 에서 내려다봄
var cam = Camera.main;
var fc = cam.GetComponent<OnFootCamera>(); if (fc) fc.enabled = false;
var fl = cam.GetComponent<FollowCamera>(); if (fl) fl.enabled = false;
cam.transform.position = O + new Vector3(0, 1.62f, __CZ__f);
cam.transform.rotation = Quaternion.Euler(__PITCH__f, 0, 0);
cam.fieldOfView = 52f;
return "bench at " + O.ToString("F1") + " ground=" + gh.collider.name;
