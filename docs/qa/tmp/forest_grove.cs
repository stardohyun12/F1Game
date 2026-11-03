// artlive 20261031-2009 숲 나무 F1·F2 납품본(Library/Trees 프리팹)을 스튜디오 비탈에 무리로 심는다. 비탈·카메라는 1926 판(forest_import.cs)이 만든 것을 그대로 쓴다.
// 무리 배치 규칙(D-228): 간격 = 수관 폭 W(4.9m) × 0.70(가로) · 0.78(줄 간격), 줄마다 반 칸 엇갈림, 자리 흔들림 ±15%, 크기 ×0.85~1.15(±15%), 기울기 ±4°(수평 축 둘), 방향 임의.
//   GROVE_F1 / GROVE_F2 = 한 종만 · GROVE_MIX = F1 60% + F2 40% · GROVE_F1_old = 새 꼴을 1926 배치(간격 3.8×4.4, 크기 .8~1.2, 기울기 없음, 25그루)로 → 꼴만 바뀐 효과
if (Application.isPlaying) return "FAIL: Play 끄고";
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio 만";
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform; var fo = slotT.Find("forest"); if (!fo) return "FAIL: forest 없음 — forest_import.cs 먼저";
System.Func<float, float, float> H = (x, z) => z > -15f ? 0f : (-15f - z) * .5f + 1.4f * Mathf.Sin(x * .19f + .6f) * Mathf.Clamp01((-15f - z) / 8f);   // forest_import.cs 와 같은 비탈
foreach (var nm in new[] { "GROVE_F1", "GROVE_F2", "GROVE_F3", "GROVE_F4", "GROVE_MIX", "GROVE_F1_old", "SINGLE_F1", "SINGLE_F2", "SINGLE_F3", "SINGLE_F4" }) { var o = fo.Find(nm); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject); }
var P = new[] { AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Library/Trees/forest_F1/forest_F1.prefab"), AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Library/Trees/forest_F2/forest_F2.prefab") };
if (!P[0] || !P[1]) return "FAIL: 프리팹 없음";
const float W = 4.9f;
GameObject Plant(Transform par, int kind, string nm, float x, float z, float yaw, float sc, float tx, float tz) {
  var t = (GameObject)PrefabUtility.InstantiatePrefab(P[kind]); t.name = nm; t.transform.SetParent(par, false);
  t.transform.localPosition = new Vector3(x, H(x, z) - .1f, z); t.transform.localRotation = Quaternion.Euler(tx, yaw, tz); t.transform.localScale = Vector3.one * sc; return t; }
void Grove(string name, int cols, int rows, float dx, float dz, float jit, float smin, float smax, float tilt, System.Func<System.Random, int> pick, int seed) {
  var g = new GameObject(name); g.transform.SetParent(fo, false); var rnd = new System.Random(seed); int k = 0;
  for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) {
    float x = -4f + (c - (cols - 1) / 2f + (r % 2) * .5f) * dx + (float)(rnd.NextDouble() * 2 - 1) * jit * dx;
    float z = -18.5f - r * dz + (float)(rnd.NextDouble() * 2 - 1) * jit * dz;
    int kind = pick(rnd); float sc = smin + (float)rnd.NextDouble() * (smax - smin), yaw = (float)rnd.NextDouble() * 360f;
    float tx = (float)(rnd.NextDouble() * 2 - 1) * tilt, tz = (float)(rnd.NextDouble() * 2 - 1) * tilt;
    Plant(g.transform, kind, name + "_" + (++k).ToString("00"), x, z, yaw, sc, tx, tz); }
  g.SetActive(false); }
Grove("GROVE_F1", 6, 6, W * .70f, W * .78f, .15f, .85f, 1.15f, 4f, r => 0, 11);
Grove("GROVE_F2", 6, 6, W * .70f, W * .78f, .15f, .85f, 1.15f, 4f, r => 1, 13);
Grove("GROVE_MIX", 6, 6, W * .70f, W * .78f, .15f, .85f, 1.15f, 4f, r => r.NextDouble() < .6 ? 0 : 1, 17);
Grove("GROVE_F1_old", 5, 5, 3.8f, 4.4f, .2f, .8f, 1.2f, 0f, r => 0, 7);
for (int i = 0; i < 2; i++) { var s = Plant(fo, i, "SINGLE_F" + (i + 1), 10f, -8f, 30f, 1f, 0, 0); s.SetActive(false); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slotT.gameObject.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(slotT.gameObject.scene);
return "OK 무리 4 + 단독 2";
