using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 과제 2024 (D-147): 그림체 Look 을 "씬 하나"에만 건다. 머티리얼 에셋은 건드리지 않는다 — 셰이더 전역값만 넣는다.
///  · _PCLook = look (이 컴포넌트가 없는 씬 = 0 = 지금 판. ComplexTrack 이 Map_BayLoop 의 Look 을 따라가지 않는다)
///  · look 5 EDC-PS1 (docs/research/2026-10-26-edc-visual-grammar.md §6):
///    규칙 5·6 안개 = 하늘 = 빛 한 색 — 지금 무드의 RenderSettings.fogColor 를 그대로 쓰고, 거리·최대 섞임·밝기는 아래 시간대 표(무드 안개색이 가장 가까운 줄들을 섞음).
///    규칙 1 480×270(LowResRenderTest, Play 만) · 규칙 7 실시간 그림자 끔(Play 만) + 블롭 그림자 · 규칙 8 빛 웅덩이(가로등·벽등·전조등)
///    규칙 9 렌더러별 스냅 배수(작은 물건·사람 2px) · 사람 애니 12fps 계단(Play 만).
/// 무드(MoodController/MoodSwitcher/OpeningTutorial)는 그대로 RenderSettings 를 바꾸고, 여기서 매 프레임 그걸 읽어 전역으로 옮긴다.
/// </summary>
[ExecuteAlways, DefaultExecutionOrder(1000)]
public sealed class PCLookScene : MonoBehaviour
{
    [Tooltip("0 Current · 1 Clean · 2 Painterly · 3 Pixel-Soft · 4 Retro-Clean · 5 EDC-PS1")]
    public int look;
    public Shader discShader;
    public int lowResHeight = 270;
    public float animFps = 12f;

    // 시간대 표(규칙 6). fog = 무드 안개색(MoodSwitcher BuildDayProfiles 와 같은 값, 가까운 줄을 고르는 열쇠), s/e = 선형 안개 m, max = 최대 섞임(원경 실루엣이 70~85% 로 남게),
    // light = 면 밝기(그늘 쪽 = ×0.87), pool = 빛 웅덩이 세기. 오후는 D-146 "어둡고 탁함 금지" → 가장 밝고 안개 최대 .7 로 연하게(가설).
    // 5b(D-148 밝고 맑게): sky = 하늘 꼭대기 색(지평선은 fog), shade = 그늘 면 배수(해 쪽 대비 차 20~28%). 안개는 시작을 2배 멀리·최대 섞임 ↓ → 원경까지 대비가 남는다.
    struct Tod { public Color fog, sky; public float s, e, max, shade; public Color light; public float pool; }
    static readonly Tod[] Table =
    {
        new Tod { fog = new Color(0.88f, 0.90f, 0.92f), sky = new Color(0.60f, 0.72f, 0.86f), s = 35, e = 380, max = .62f, shade = .78f, light = new Color(1.30f, 1.30f, 1.30f), pool = 0f },    // 아침
        new Tod { fog = new Color(0.80f, 0.88f, 0.96f), sky = new Color(0.44f, 0.62f, 0.86f), s = 80, e = 600, max = .50f, shade = .75f, light = new Color(1.40f, 1.38f, 1.34f), pool = 0f },    // 오후
        // 2124(D-150 TONE 4 「돌아오는 길 × 노을」): 안개 주황 ↓·시작 멀리·최대 ↓ → 근경 또렷, 바다 회청색 남음. 그늘 .72→.64 → 따뜻한 면/차가운 그늘 대비.
        new Tod { fog = new Color(0.95f, 0.80f, 0.70f), sky = new Color(0.54f, 0.58f, 0.76f), s = 90, e = 620, max = .50f, shade = .64f, light = new Color(1.40f, 1.16f, 0.98f), pool = .7f },   // 노을
        new Tod { fog = new Color(0.40f, 0.46f, 0.66f), sky = new Color(0.16f, 0.21f, 0.42f), s = 30, e = 240, max = .75f, shade = .80f, light = new Color(0.68f, 0.73f, 0.90f), pool = 1f },    // 초저녁
    };

    static readonly int LookId = Shader.PropertyToID("_PCLook"), FogCId = Shader.PropertyToID("_PCFogColor"), FogPId = Shader.PropertyToID("_PCFogParams"),
        LightId = Shader.PropertyToID("_PCLight"), PoolId = Shader.PropertyToID("_PCPool"), SnapId = Shader.PropertyToID("_PCSnapK"), KeepId = Shader.PropertyToID("_PCKeep"),
        SkyId = Shader.PropertyToID("_PCSkyColor"), FieldId = Shader.PropertyToID("_PCField");

    public Color FogColor { get; private set; }
    public Shader skyShader;
    Material skyMat;

    // 5c(D-149 지글거림 금지 — 은은하게): 기본값이 5c. 끄면 5b 와 같다(전/후 비교용 토글).
    [Header("5c 시간적 안정성")]
    public bool vertexSnap = false;      // 정점 스냅(움직이면 1px씩 튐) — 옵션만 남김
    public bool mipmaps = true;          // 밉맵 단계 읽기(먼 곳 텍셀 크롤링)
    public bool smoothUpscale = true;    // 바이리니어 확대 + 블룸·헤이즈(Present.shader)
    [Range(1, 8)] public int msaa = 4;   // 저해상도 RT 멀티샘플(가장자리 크롤링)
    public Shader presentShader;
    Material presentMat;
    static readonly int SnapOnId = Shader.PropertyToID("_PCSnapOn"), MipId = Shader.PropertyToID("_PCMip");
    /// <summary>카메라에 5b 하늘(지평선 안개 → 위 파랑)을 건다. 캡처 스크립트의 임시 카메라도 부른다.</summary>
    public void ApplySky(Camera cam)
    {
        if (skyShader == null) skyShader = Shader.Find("PitCrew/Look/SkyGrad");
        if (skyMat == null && skyShader != null) skyMat = new Material(skyShader) { hideFlags = HideFlags.DontSave };
        if (skyMat == null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = FogColor; return; }
        if (!cam.TryGetComponent<Skybox>(out var sb)) sb = cam.gameObject.AddComponent<Skybox>();   // ?? 는 Unity 가짜 null 을 못 거른다
        sb.material = skyMat; sb.enabled = true;
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    /// <summary>활성 씬이 Look 5 면 true — 무드가 월드 머티리얼(공유 에셋)에 안개·주변광을 쓰지 않게 한다(MoodController.WorldMaterials).</summary>
    public static bool OwnsWorld { get { var c = FindFirstObjectByType<PCLookScene>(); return c != null && c.isActiveAndEnabled && c.look == 5; } }
    Transform discRoot; Material poolMat, coneMat, blobMat;
    readonly List<(Transform target, Transform disc, float lift)> blobs = new List<(Transform, Transform, float)>();
    readonly List<Animator> stepped = new List<Animator>();
    readonly List<Renderer> hidden = new List<Renderer>();
    Transform cone, coneCar;
    float animAcc, nextScan; MaterialPropertyBlock mpb;
    int builtFor = -1; Light sun; Camera lookCam; CameraClearFlags camFlags; LightShadows sunShadows = LightShadows.None;

    void OnEnable() { builtFor = -1; nextScan = 0; }

    void OnDisable()
    {
        Shader.SetGlobalFloat(LookId, 0);
        Teardown();
    }

    void Update() => Tick();
    void LateUpdate() { if (look == 5) PlaceBlobs(); }

    /// <summary>한 프레임 몫. 캡처 스크립트가 look 을 바꾼 뒤 같은 호출 안에서 바로 부른다.</summary>
    public void Tick()
    {
        Shader.SetGlobalFloat(LookId, look);
        if (look != 5) { if (builtFor == 5) Teardown(); builtFor = look; return; }
        if (builtFor != 5) { Build(); builtFor = 5; }
        PushGlobals();
        if (Application.isPlaying)
        {
            if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 2f; MarkRenderers(true); ScanAnimators(); }
            var cam = Camera.main;
            if (cam != null)
            {
                if (lookCam != cam) { lookCam = cam; camFlags = cam.clearFlags; }
                ApplySky(cam);   // 5b: 지평선만 안개, 위는 맑은 파랑
                if (!cam.TryGetComponent<LowResRenderTest>(out var lr)) lr = cam.gameObject.AddComponent<LowResRenderTest>();
                if (presentShader == null) presentShader = Shader.Find("PitCrew/Look/Present");
                if (presentMat == null && presentShader != null) presentMat = new Material(presentShader) { hideFlags = HideFlags.DontSave };
                lr.targetHeight = lowResHeight; lr.integerScale = true;   // 5b: 화면 = 정수배
                lr.smooth = smoothUpscale && presentMat != null; lr.presentMaterial = presentMat; lr.msaa = msaa;   // 5c: 부드러운 확대
                if (!lr.enabled) lr.enabled = true;
            }
            if (sun == null) { sun = RenderSettings.sun; if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; } }
            if (sun != null && sun.shadows != LightShadows.None) { sunShadows = sun.shadows; sun.shadows = LightShadows.None; }   // 규칙 7(무드가 다시 켜도 끈다)
            StepAnimators();
        }
        PlaceBlobs();
    }

    /// <summary>시간대 표를 지금 무드 안개색 기준으로 섞어 전역에 넣는다. 에디터 캡처 스크립트도 부른다.</summary>
    public void PushGlobals()
    {
        Color fc = RenderSettings.fogColor; FogColor = fc;
        float ws = 0, s = 0, e = 0, mx = 0, sh = 0, pool = 0; Color li = Color.clear, sky = Color.clear;
        foreach (var t in Table)
        {
            float d = (t.fog.r - fc.r) * (t.fog.r - fc.r) + (t.fog.g - fc.g) * (t.fog.g - fc.g) + (t.fog.b - fc.b) * (t.fog.b - fc.b);
            float w = 1f / ((d + 1e-4f) * (d + 1e-4f));
            ws += w; s += t.s * w; e += t.e * w; mx += t.max * w; sh += t.shade * w; li += t.light * w; sky += t.sky * w; pool += t.pool * w;
        }
        Shader.SetGlobalColor(FogCId, fc.linear);   // 전역 색은 Linear 변환을 안 해 준다(머티리얼 색과 달리) — 빼면 안개가 0.40→170 으로 떠서 하늘(배경)과 갈린다
        Shader.SetGlobalColor(SkyId, (sky / ws).linear);
        Shader.SetGlobalFloat(SnapOnId, vertexSnap ? 1 : 0);
        Shader.SetGlobalFloat(MipId, mipmaps ? 1 : 0);
        Shader.SetGlobalVector(FogPId, new Vector4(s / ws, e / ws, mx / ws, sh / ws));
        Shader.SetGlobalColor(LightId, li / ws);
        Shader.SetGlobalFloat(PoolId, pool / ws);
    }

    // ------------------------------------------------------------------ 렌더러 표시(규칙 9·12)
    /// <summary>작은 물건(경계 상자 가장 긴 변 < 2m)·사람 = 스냅 2px, 차·사람 = 손픽셀(블러·채도 제외). on=false 면 지운다.</summary>
    public void MarkRenderers(bool on)
    {
        mpb ??= new MaterialPropertyBlock();
        var car = FindFirstObjectByType<ArcadeCarController>();
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r is ParticleSystemRenderer || r.sharedMaterial == null) continue;
            bool person = r.GetComponentInParent<Resident>() != null || r.GetComponentInParent<Pedestrian>() != null;
            bool isCar = car != null && r.transform.IsChildOf(car.transform);
            var ext = r.bounds.size; bool small = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z)) < 2f;
            float k = on && (person || small || isCar) ? 2f : 0f, keep = on && (person || isCar) ? 1f : 0f;
            float field = on && r.sharedMaterial.name.StartsWith("BL_Field") ? 1f : 0f;   // 5b: 라임 밭 → 바랜 올리브/황토
            r.GetPropertyBlock(mpb);
            if (Mathf.Approximately(mpb.GetFloat(SnapId), k) && Mathf.Approximately(mpb.GetFloat(KeepId), keep) && Mathf.Approximately(mpb.GetFloat(FieldId), field)) continue;
            mpb.SetFloat(SnapId, k); mpb.SetFloat(KeepId, keep); mpb.SetFloat(FieldId, field); r.SetPropertyBlock(mpb);
        }
    }

    // ------------------------------------------------------------------ 사람 애니 12fps 계단(규칙 9)
    void ScanAnimators()
    {
        stepped.RemoveAll(a => a == null);
        foreach (var res in FindObjectsByType<Resident>(FindObjectsSortMode.None))
            if (res.anim != null && !stepped.Contains(res.anim)) { res.anim.enabled = false; stepped.Add(res.anim); }
    }

    void StepAnimators()
    {
        animAcc += Time.deltaTime; float step = 1f / Mathf.Max(1f, animFps);
        if (animAcc < step) return;
        foreach (var a in stepped) if (a != null && a.isActiveAndEnabled == false && a.gameObject.activeInHierarchy) a.Update(animAcc);
        animAcc = 0;
    }

    // ------------------------------------------------------------------ 바닥 원판: 빛 웅덩이(규칙 8)·블롭 그림자(규칙 7)
    void Build()
    {
        Teardown();
        if (discShader == null) discShader = Shader.Find("PitCrew/Look/FloorDisc");
        if (discShader == null) return;
        poolMat = NewMat(new Color(0.55f, 0.42f, 0.24f, 1f), true, false);   // 1차 캡처에서 밝은 인도 위 하얗게 날아감 → 절반
        coneMat = NewMat(new Color(0.50f, 0.47f, 0.37f, 1f), true, true);
        blobMat = NewMat(new Color(0, 0, 0, 0.45f), false, false);
        discRoot = new GameObject("PCLook_Discs") { hideFlags = HideFlags.DontSave }.transform;
        int pools = 0;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name == "StreetLamp") { Pool(t.position, 5f); pools++; }
            else if (t.name == "WallLamp") { Pool(t.position + t.forward * 1.2f, 3f); pools++; }
        }
        var car = FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            Blob(car.transform, 2.3f, 4.8f, 0.5f);
            // 차 앞 3~18m 원뿔. 1차: 차 자식 로컬 고정이면 길 아래로 묻혔다 → 블롭처럼 매 프레임 땅에 붙인다(중심 = 차 앞 10.5m)
            cone = Quad("HeadlightPool", coneMat, discRoot); cone.localScale = new Vector3(9f, 15f, 1f); coneCar = car.transform;
        }
        // 규칙 5: 하늘 = 안개 한 색 → 구름 덩어리는 Look 5 에서 숨긴다(0 으로 돌아가면 다시 켬)
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (!r.forceRenderingOff && r.name.StartsWith("BLOCKOUT_CloudPuff")) { r.forceRenderingOff = true; hidden.Add(r); }   // 직렬화 안 되는 플래그 → 씬 파일은 그대로
        foreach (var p in FindObjectsByType<Resident>(FindObjectsSortMode.None)) Blob(p.transform.parent != null ? p.transform.parent : p.transform, 0.9f, 0.9f, 0.4f);
        Debug.Log($"[PCLook 5] 빛 웅덩이 {pools} · 블롭 {blobs.Count}");
    }

    Material NewMat(Color c, bool add, bool cone)
    {
        var m = new Material(discShader) { hideFlags = HideFlags.DontSave };
        m.SetColor("_Color", c); m.SetFloat("_Add", add ? 1 : 0); m.SetFloat("_Cone", cone ? 1 : 0);
        m.SetFloat("_SrcB", add ? 1 : 2); m.SetFloat("_DstB", add ? 1 : 0);   // 가산 One One / 곱 DstColor Zero
        return m;
    }

    Transform Quad(string n, Material m, Transform parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = n; g.hideFlags = HideFlags.DontSave;
        var col = g.GetComponent<Collider>(); if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        g.transform.SetParent(parent != null ? parent : discRoot, false);
        return g.transform;
    }

    void Pool(Vector3 at, float radius)
    {
        var q = Quad("LightPool", poolMat, discRoot);
        Vector3 p = at, n = Vector3.up;
        if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) { p = hit.point; n = hit.normal; }
        q.SetPositionAndRotation(p + n * 0.06f, Quaternion.LookRotation(-n) );
        q.localScale = Vector3.one * radius * 2f;
    }

    void Blob(Transform target, float w, float l, float lift)
    {
        var q = Quad("BlobShadow", blobMat, discRoot); q.localScale = new Vector3(w, l, 1f);
        blobs.Add((target, q, lift));
    }

    readonly RaycastHit[] hits = new RaycastHit[8];
    void PlaceBlobs()
    {
        foreach (var (target, disc, lift) in blobs)
        {
            if (target == null || disc == null) continue;
            disc.gameObject.SetActive(target.gameObject.activeInHierarchy);
            Vector3 o = target.position + Vector3.up * 1f, p = target.position, n = Vector3.up; float best = 99f;
            int c = Physics.RaycastNonAlloc(o, Vector3.down, hits, 6f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < c; i++) if (!hits[i].transform.IsChildOf(target) && hits[i].distance < best) { best = hits[i].distance; p = hits[i].point; n = hits[i].normal; }
            var fwd = Vector3.ProjectOnPlane(target.forward, n);
            disc.SetPositionAndRotation(p + n * 0.05f, Quaternion.LookRotation(-n, fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.forward));
        }
        if (cone != null && coneCar != null)
        {
            var f = coneCar.forward; f.y = 0; f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
            Vector3 c = coneCar.position + f * 10.5f, p = c, n = Vector3.up; float best = 99f;
            int k = Physics.RaycastNonAlloc(c + Vector3.up * 4f, Vector3.down, hits, 12f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < k; i++) if (!hits[i].transform.IsChildOf(coneCar) && hits[i].distance < best) { best = hits[i].distance; p = hits[i].point; n = hits[i].normal; }
            cone.SetPositionAndRotation(p + n * 0.06f, Quaternion.LookRotation(-n, Vector3.ProjectOnPlane(f, n)));
        }
    }

    void Teardown()
    {
        if (discRoot != null) { if (Application.isPlaying) Destroy(discRoot.gameObject); else DestroyImmediate(discRoot.gameObject); }
        var car = FindFirstObjectByType<ArcadeCarController>();
        if (car != null) foreach (Transform c in car.transform) if (c.name == "HeadlightPool") { if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject); }
        foreach (var (_, d, _) in blobs) if (d != null) { if (Application.isPlaying) Destroy(d.gameObject); else DestroyImmediate(d.gameObject); }
        blobs.Clear();
        cone = null; coneCar = null;
        foreach (var r in hidden) if (r != null) r.forceRenderingOff = false;
        hidden.Clear();
        foreach (var a in stepped) if (a != null) a.enabled = true;
        stepped.Clear();
        if (Application.isPlaying)
        {
            MarkRenderers(false);
            if (lookCam != null) { lookCam.clearFlags = camFlags; var lr = lookCam.GetComponent<LowResRenderTest>(); if (lr != null) lr.enabled = false; var sb = lookCam.GetComponent<Skybox>(); if (sb != null) sb.enabled = false; }
            if (sun != null && sunShadows != LightShadows.None) sun.shadows = sunShadows;            lookCam = null;
        }
        discRoot = null;
    }
}
