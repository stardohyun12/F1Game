using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 과제 0032: 차 외형 후보(A·C)를 Visual 만 바꿔 끼운다. 물리(Race·콜라이더·ArcadeCarController)는 그대로.
/// 현재 차 = FormulaGameplayVisual 을 켜고 후보를 끈다. 후보 = 같은 자리(로컬 위치·스케일)에 FBX 를 세운다.
/// 바퀴 피벗은 이름이 아니라 실제 가져온 위치로 앞/뒤·반경을 정한다(GLB 와 Unity 의 X 부호가 반대 — current-contract.md).
/// 기본은 현재 차(도현 선택 대기).
/// </summary>
public static class CarVisualSwitcher
{
    const string Dir = "Assets/Art/BayLoop/Car/";

    [MenuItem("PitCrew/Car/Use Current")] public static void UseCurrent() => Debug.Log(Apply(null));
    [MenuItem("PitCrew/Car/Use A")] public static void UseA() => Debug.Log(Apply("A"));
    [MenuItem("PitCrew/Car/Use C")] public static void UseC() => Debug.Log(Apply("C"));

    // 과제 0208: 기존 차의 운전자(헬멧)·배달 가방을 후보에도 붙일지. 합성 시안엔 둘 다 없어서 기본은 끔(PD 판정 대기).
    const string DriverPref = "PitCrew.Car.Driver", CargoPref = "PitCrew.Car.Cargo";
    public static bool ShowDriver { get => EditorPrefs.GetBool(DriverPref, false); set => EditorPrefs.SetBool(DriverPref, value); }
    public static bool ShowCargo { get => EditorPrefs.GetBool(CargoPref, false); set => EditorPrefs.SetBool(CargoPref, value); }
    [MenuItem("PitCrew/Car/Toggle Driver")] static void ToggleDriver() { ShowDriver = !ShowDriver; Debug.Log("[CarVisual] 운전자 " + ShowDriver + " " + ApplyExtras()); }
    [MenuItem("PitCrew/Car/Toggle Cargo Bag")] static void ToggleCargo() { ShowCargo = !ShowCargo; Debug.Log("[CarVisual] 가방 " + ShowCargo + " " + ApplyExtras()); }

    // 과제 0208: 합성 시안(Blender) 과 같은 밝기·색으로 보이게 하는 차 머티리얼 틴트. 세계 조명·포스트는 그대로 두고 차 쪽만 보정.
    // 측정: docs/qa/tmp/c0208_metric.py, 정사영 3/4·뒤 3/4 두 앵글 평균색 ΔE (보고 programmer-20261026-0208).
    // C 는 아이보리가 해 색(주황)·포스트에 누렇게 눌려 파랑을 더 올린다.
    static Color TintFor(string which) => which == "C" ? new Color(1.15f, 1.2f, 1.3f, 1f) : new Color(1.12f, 1.12f, 1.12f, 1f);

    /// <summary>which = null(현재) / "A" / "C". 씬을 저장한다(플레이 중이면 저장 안 하고 그 판에만).</summary>
    public static string Apply(string which)
    {
        var race = Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
        if (race == null) return "[CarVisual] ArcadeCarController 없음";
        var cur = race.transform.Find("FormulaGameplayVisual");
        if (cur == null) return "[CarVisual] FormulaGameplayVisual 없음";
        // 과제 0208: 이름이 되돌려진 후보(임포트 갱신으로 "C" 가 된 것)도 지운다 — 기존 차가 아닌 FormulaVehicleVisual 은 전부 후보.
        foreach (Transform c in race.transform.Cast<Transform>().ToList())
            if (c != cur && (c.name.StartsWith("CarVisual_") || c.GetComponent<PitCrew.Garage.FormulaVehicleVisual>() != null)) Object.DestroyImmediate(c.gameObject);
        string msg;
        if (which == null)
        {
            cur.gameObject.SetActive(true);
            msg = "[CarVisual] 현재 차";
        }
        else
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + which + ".fbx");
            if (fbx == null) return "[CarVisual] " + Dir + which + ".fbx 없음";
            var mat = AtlasMaterial(which, cur);   // 임포트 갱신이 있을 수 있어 인스턴스 만들기 전에
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            go.name = "CarVisual_" + which;
            var t = go.transform; t.SetParent(race.transform, false);
            t.localPosition = cur.localPosition; t.localRotation = cur.localRotation; t.localScale = cur.localScale;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (mat != null) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            // 과제 0208: 바퀴 아닌 부품을 한 피벗(ChassisLean)에 모은다 — 전엔 body 만 기울어서 날개·실내가 따로 놀았다.
            // 프리팹 인스턴스는 자식 옮기기가 안 돼 푼다(Apply 가 매번 FBX 에서 다시 만드니 연결이 끊겨도 손해 없음).
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var lean = new GameObject("ChassisLean").transform; lean.SetParent(go.transform, false);
            foreach (Transform c in go.transform.Cast<Transform>().ToList())
                if (c != lean && !c.name.StartsWith("wheel")) c.SetParent(lean, true);
            // 기존 차의 운전자·배달 가방 사본. 기존 Visual 과 후보 루트의 로컬 자세가 같아서(위에서 복사) 같은 로컬 자리에 두면 좌석·짐 트레이에 맞는다(측정 0208).
            foreach (var src in new[] { "ChassisLean/driver", "ChassisLean/CargoMount" })
            {
                var s = cur.Find(src); if (s == null) continue;
                var copy = Object.Instantiate(s.gameObject, lean);
                copy.name = s.name == "driver" ? "Extra_Driver" : "Extra_CargoMount";
                copy.transform.localPosition = s.localPosition; copy.transform.localRotation = s.localRotation; copy.transform.localScale = s.localScale;
            }
            // 바퀴: 실제 위치(차 기준 로컬)로 앞/뒤 판단, 반경 = 메시 높이 절반
            var wheels = new List<PitCrew.Garage.FormulaVehicleVisual.Wheel>();
            var sb = new System.Text.StringBuilder();
            foreach (var w in go.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("wheel")))
            {
                var lp = race.transform.InverseTransformPoint(w.position);
                var mr = w.GetComponent<Renderer>();
                float rad = mr != null ? mr.bounds.extents.y : 0.35f;
                wheels.Add(new PitCrew.Garage.FormulaVehicleVisual.Wheel { transform = w, front = lp.z > 0f, radius = rad / Mathf.Max(1e-4f, w.lossyScale.y) });
                bool nameLeft = w.name.Contains("left");
                sb.Append($" {w.name}=({lp.x:F2},{lp.y:F2},{lp.z:F2}) r{rad:F2}{(nameLeft == lp.x < 0f ? "" : " ⚠이름과X부호반대")}");
            }
            var vv = go.AddComponent<PitCrew.Garage.FormulaVehicleVisual>();
            vv.wheels = wheels.ToArray();
            vv.chassis = lean;
            cur.gameObject.SetActive(false);
            msg = $"[CarVisual] {which} 바퀴{wheels.Count}:{sb} {ApplyExtras()}";
        }
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(race.gameObject.scene);
            EditorSceneManager.SaveScene(race.gameObject.scene);
        }
        return msg;
    }

    /// <summary>후보 차의 Extra_Driver·Extra_CargoMount 켜고 끄기 (EditorPrefs). 씬 저장.</summary>
    static string ApplyExtras()
    {
        var race = Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
        if (race == null) return "";
        int n = 0;
        foreach (var t in race.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Extra_Driver") { t.gameObject.SetActive(ShowDriver); n++; }
            else if (t.name == "Extra_CargoMount") { t.gameObject.SetActive(ShowCargo); n++; }
        }
        if (!Application.isPlaying && n > 0) { EditorSceneManager.MarkSceneDirty(race.gameObject.scene); EditorSceneManager.SaveScene(race.gameObject.scene); }
        return $"extras driver={ShowDriver} cargo={ShowCargo} ({n})";
    }

    /// <summary>후보 아틀라스를 현재 차 머티리얼(같은 셰이더)에 꽂은 사본. Assets/Art/BayLoop/Car/Candidate_X.mat</summary>
    static Material AtlasMaterial(string which, Transform cur)
    {
        var path = Dir + "Candidate_" + which + ".mat";
        // 과제 0208: 세계 아틀라스와 같은 임포트(Point·mip 없음·무압축). 기본값(Bilinear·mip·DXT1)이면 32px/m 칠이 번져 차체가 회색 얼룩으로 뭉갰다.
        if (AssetImporter.GetAtPath(Dir + which + "_Atlas.png") is TextureImporter ti &&
            (ti.filterMode != FilterMode.Point || ti.mipmapEnabled || ti.textureCompression != TextureImporterCompression.Uncompressed))
        {
            ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 512; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + which + "_Atlas.png");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var src = cur.GetComponentsInChildren<Renderer>(true).Select(r => r.sharedMaterial).FirstOrDefault(m => m != null);
            if (src == null) return null;
            mat = new Material(src) { name = "Candidate_" + which };
            AssetDatabase.CreateAsset(mat, path);
        }
        if (tex != null)
        {
            foreach (var p in new[] { "_BaseMap", "_MainTex", "_Albedo", "_Atlas" }) if (mat.HasProperty(p)) mat.SetTexture(p, tex);
            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", TintFor(which));
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
        }
        return mat;
    }
}
