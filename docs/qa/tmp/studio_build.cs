// artlive 1534 (D-191): 건물 스튜디오 씬 Studio_Building 만들기. 다시 돌리면 처음부터 다시 만든다.
// 빛 = ITER_Light 와 같은 A7LightToggles 기본값, 하늘 = A7 Sky 메시 + 재질 사본, 땅 30×30 + 앞 길 띠 4m(z 1.5..5.5), STUDIO_Slot = 원점(건물 피벗 = 바닥 앞 가운데, 정면 +Z).
var log = new System.Text.StringBuilder();
const string S = "Assets/Scenes/Studio_Building.unity", M = "Assets/Scenes/Studio_Building_Mats";
if (!AssetDatabase.IsValidFolder(M)) AssetDatabase.CreateFolder("Assets/Scenes", "Studio_Building_Mats");
var sc = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
RenderSettings.fog = false; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white;
RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
// 재질 사본(공유 원본은 읽기만)
System.Func<string, string, Material> Copy = (src, nm) => { var p = M + "/" + nm + ".mat"; AssetDatabase.DeleteAsset(p); var m = new Material(AssetDatabase.LoadAssetAtPath<Material>(src)); AssetDatabase.CreateAsset(m, p); return m; };
var mSky = Copy("Assets/Art/SceneA4/Mats/SA4_sky_grad_nofog.mat", "ST_sky");
var mGround = Copy("Assets/Art/SceneA7/Mats/SA7_yard_ground_soft.mat", "ST_ground");
var mRoad = Copy("Assets/Art/SceneA7/Mats/SA7_asphalt_tile.mat", "ST_road");
// 메시: 땅 = 15m 칸 2×2(칸마다 마당 텍스처 0..0.47×0..0.94 = 32px/m, 텍스처가 Clamp 라 반복 대신 칸), 길 = 15m 칸 2개(u = 폭 4m, v = 길이 15m)
System.Func<string, float, float, float, float, float, float, float, Mesh> Grid = (nm, x0, z0, cw, cd, nx, umax, vmax) => {
  var v = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>(); var tri = new System.Collections.Generic.List<int>();
  int n = (int)nx; int nz = nm == "ST_road_mesh" ? 1 : n;
  for (int i = 0; i < n; i++) for (int j = 0; j < nz; j++) { int b = v.Count; float ax = x0 + i * cw, az = z0 + j * cd; float y = nm == "ST_road_mesh" ? 0.01f : 0f;
    v.Add(new Vector3(ax, y, az)); v.Add(new Vector3(ax + cw, y, az)); v.Add(new Vector3(ax, y, az + cd)); v.Add(new Vector3(ax + cw, y, az + cd));
    if (nm == "ST_road_mesh") { uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, vmax)); uv.Add(new Vector2(umax, 0)); uv.Add(new Vector2(umax, vmax)); }
    else { uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(umax, 0)); uv.Add(new Vector2(0, vmax)); uv.Add(new Vector2(umax, vmax)); }
    tri.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
  var me = new Mesh { name = nm }; me.SetVertices(v); me.SetUVs(0, uv); me.SetTriangles(tri, 0); me.RecalculateNormals(); me.RecalculateBounds(); me.RecalculateTangents();
  var p = M + "/" + nm + ".asset"; AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(me, p); return me; };
// s00 뒤 고침: v 0.94 는 텍스처 위쪽 어두운 빈칸에 닿아 칸 경계마다 검푸른 띠 → v 0.82 까지만. _Sat 2.6(A7 원본)은 흙이 주황으로 뜸 → 사본만 1.
var gMesh = Grid("ST_ground_mesh", -15, -15, 15, 15, 2, 0.47f, 0.82f); mGround.SetFloat("_Sat", 1f);
var rMesh = Grid("ST_road_mesh", -15, 1.5f, 15, 4, 2, 1f, 1f);
var root = new GameObject("STUDIO");
root.AddComponent<AudioListener>();
System.Func<string, Mesh, Material, Transform, GameObject> Obj = (nm, me, ma, par) => { var g = new GameObject(nm); g.transform.SetParent(par, false); g.AddComponent<MeshFilter>().sharedMesh = me; g.AddComponent<MeshRenderer>().sharedMaterial = ma; return g; };
Obj("ST_Sky", AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/SceneA2/MatsD/d_sky.asset"), mSky, root.transform).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
var gg = Obj("ST_Ground", gMesh, mGround, root.transform); gg.AddComponent<MeshCollider>();
Obj("ST_Road", rMesh, mRoad, root.transform);
var lg = new GameObject("STUDIO_Light"); lg.transform.SetParent(root.transform, false); lg.AddComponent<A7LightToggles>();
var slot = new GameObject("STUDIO_Slot"); slot.transform.SetParent(root.transform, false);
// 고정 카메라 4대(A7 GameCam 과 같은 값: fov 64, 배경색, LowRes 720 Nearest). 겨냥점 = 건물 앞 2m 안쪽
var cams = new GameObject("STUDIO_Cams"); cams.transform.SetParent(root.transform, false);
var bg = new Color(0.95f, 0.886f, 0.73f);
System.Action<string, Vector3, Vector3, bool> Cam = (nm, pos, look, on) => { var g = new GameObject(nm); g.transform.SetParent(cams.transform, false); g.transform.position = pos; g.transform.rotation = Quaternion.LookRotation(look - pos);
  var c = g.AddComponent<Camera>(); c.fieldOfView = 64; c.nearClipPlane = 0.1f; c.farClipPlane = 2000; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = bg;
  var lr = g.AddComponent<LowResRenderTest>(); lr.targetHeight = 720; lr.smooth = false; if (on) g.tag = "MainCamera"; g.SetActive(on); };
var aim = new Vector3(0, 1.8f, -2f);
var dRoad = Quaternion.Euler(0, 40, 0) * Vector3.forward * 20f; Cam("Cam_ST_road", new Vector3(aim.x + dRoad.x, 1.0f, aim.z + dRoad.z), aim, false);   // 카트 눈높이 1.0, 정면 40° 비스듬히 20m
Cam("Cam_ST_front", new Vector3(0, 1.7f, 8f), new Vector3(0, 1.9f, 0), false);                                                                       // 정면 8m
var d34 = Quaternion.Euler(0, 45, 0) * Vector3.forward * 12f; Cam("Cam_ST_34", new Vector3(aim.x + d34.x, 3.2f, aim.z + d34.z), aim, true);           // 3/4 12m(평소 켜 둠)
Cam("Cam_ST_close", new Vector3(0, 1.5f, 3f), new Vector3(0, 1.4f, 0), false);                                                                       // 입구 3m
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc, S);
log.Append("saved " + S + " ground " + AssetDatabase.GetAssetPath(mGround) + " road " + AssetDatabase.GetAssetPath(mRoad) + "\n");
foreach (var c in cams.GetComponentsInChildren<Camera>(true)) log.Append(c.name + " " + c.transform.position.ToString("F1") + "\n");
return log.ToString();
