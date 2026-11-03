// C-001 기준 1: 상처 단계 비교 한 장. 진짜 카트 = 2단계(가운데), 양옆에 같은 모양 사본(보이기 전용, 콜라이더·물리 없음) 1단계(왼쪽)·3단계(오른쪽).
// 각 카트 = 기운 회색 앞판 대역 + 연기. 1→3 으로 앞판이 더 꺾이고 범퍼·펜더가 더해지고 연기가 짙어진다. 카트 위 숫자 = 단계.
// 사본은 이 검증 사진용 상태 세팅일 뿐 — 게임 코드에는 없다.
if (!Application.isPlaying) return "FAIL: Play 중에만";
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.Car == null) return "FAIL: 감독·카트 없음";
var car = d.Car;
// 정비 자리 서쪽 끝에 서서 동쪽 카트들을 본다
d.DebugTeleport(new Vector3(215.6f, 26.7f, -55.4f), 90f, new Vector3(220.6f, 26.7f, -55.4f), 270f);   // 카트를 1.2 m 더 안쪽에 — 세 대가 한 화면에
var t = car.transform;
var dm = CartDamage.Of(car);
dm.Clear(); dm.AddStage(2, "crit1 비교(가운데)");
foreach (var old in UnityEngine.Object.FindObjectsByType<CartDamage>(FindObjectsSortMode.None)) if (old.name.StartsWith("C001_Compare")) UnityEngine.Object.Destroy(old.gameObject);
// 보는 쪽(동쪽)을 볼 때 왼쪽 = +z(북), 오른쪽 = −z(남)
var stages = new int[] { 1, 3 }; var dz = new float[] { 2.1f, -2.1f };
string info = "";
for (int k = 0; k < 2; k++)
{
    var root = new GameObject("C001_Compare_Stage" + stages[k]);
    root.transform.SetPositionAndRotation(t.position + new Vector3(0f, 0f, dz[k]), t.rotation);
    root.transform.localScale = t.lossyScale;
    int n = 0;
    foreach (var mf in car.GetComponentsInChildren<MeshFilter>())
    {
        var mr = mf.GetComponent<MeshRenderer>();
        if (mr == null || !mr.enabled || mf.sharedMesh == null || mf.name.StartsWith("CartDamage_")) continue;
        var g = new GameObject(mf.name);
        g.transform.SetParent(root.transform, false);
        g.transform.localPosition = t.InverseTransformPoint(mf.transform.position);
        g.transform.localRotation = Quaternion.Inverse(t.rotation) * mf.transform.rotation;
        Vector3 ls = mf.transform.lossyScale, rs = t.lossyScale;
        g.transform.localScale = new Vector3(ls.x / rs.x, ls.y / rs.y, ls.z / rs.z);
        g.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
        g.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
        n++;
    }
    var cd = root.AddComponent<CartDamage>();
    cd.AddStage(stages[k], "crit1 비교");
    info += " · 사본 " + stages[k] + "단계(메시 " + n + ")";
}
// 카트 위 단계 숫자(사진 판독용)
foreach (var lbl in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) if (lbl.name.StartsWith("C001_Label")) UnityEngine.Object.Destroy(lbl.gameObject);
var lz = new float[] { 2.1f, 0f, -2.1f }; var lt = new string[] { "1", "2", "3" };
for (int k = 0; k < 3; k++)
{
    var go = new GameObject("C001_Label" + lt[k]);
    go.transform.position = t.position + new Vector3(0.2f, 1.35f, lz[k]);
    go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
    var tm = go.AddComponent<TextMesh>();
    tm.text = lt[k]; tm.fontSize = 64; tm.characterSize = 0.06f; tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white;
    tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
}
// 연기는 켜진 뒤 2~3초에 걸쳐 차오른다 → 사진용으로 세 대 모두 2.5초 미리 돌려 둔다(같은 조건 비교)
foreach (var cdm in UnityEngine.Object.FindObjectsByType<CartDamage>(FindObjectsSortMode.None))
    foreach (var sp in cdm.GetComponentsInChildren<ParticleSystem>())
        if (sp.name.StartsWith("CartDamage_Smoke")) { sp.Simulate(2.5f, true, true); sp.Play(); }
var fc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
System.Func<Vector3> lookAt = () => t.position + new Vector3(-0.6f, 0.5f, 0f);
if (fc != null) fc.LookAt(lookAt());
int run = UnityEngine.Random.Range(1, int.MaxValue); UnityEditor.SessionState.SetInt("c001.run", run);
float t0 = Time.realtimeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
    if (!Application.isPlaying || UnityEditor.SessionState.GetInt("c001.run", 0) != run || Time.realtimeSinceStartup - t0 > 8f) { UnityEditor.EditorApplication.update -= cb; return; }
    if (fc != null) fc.LookAt(lookAt());
};
UnityEditor.EditorApplication.update += cb;
int parts = 0; foreach (var p in car.GetComponentsInChildren<Renderer>()) if (p.name.StartsWith("CartDamage_Part") && p.gameObject.activeInHierarchy) parts++;
return "가운데 진짜 카트 상처 " + dm.Stage + "단계(회색 조각 " + parts + "개 켜짐)" + info + " · 왼쪽→오른쪽 = 1·2·3단계";
