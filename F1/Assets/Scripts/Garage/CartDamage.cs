using UnityEngine;

/// <summary>
/// C-001 그레이박스: 카트 상처 0~3. 벽 같은 데 세게 부딪히면(접촉면 쪽 상대 속도가 hitSpeed 이상) 한 단계 오른다.
/// 보이기만 한다 — 운전·속도·화물에는 아무 영향 없음(카드 잠금, 영향은 도현 한 판 뒤 따로 정함).
/// 표시 = 연기 + 기운 회색 앞판 대역(카드 수용 기준 1 「앞판이 기운 회색 대역 + 작은 연기」 — 검증 2회 불통과 원인이 앞판 없음이라 되살림).
/// 연기: 반투명 파티클(URP Particles/Lit — 맵 빛·안개를 받음, 단계마다 더 자주·더 크게·더 진하게).
/// 앞판: 카트 코 아래 경첩에서 앞으로 꺾인 회색 판. 1 살짝 꺾임 → 2 더 꺾이고 범퍼가 한쪽으로 처짐 → 3 크게 꺾이고 옆 펜더까지 벌어짐.
/// 카트에 미리 붙이지 않는다 — RepairSpot 또는 디버그 창이 Of(car) 로 처음 부를 때 붙는다.
/// </summary>
public class CartDamage : MonoBehaviour
{
    public const int MaxStage = 3;

    [Tooltip("이 속도(m/s, 접촉면 쪽 성분) 이상으로 부딪히면 한 단계")]
    [SerializeField] float hitSpeed = 7f;          // 가설: 약 25 km/h 로 벽에 박는 정도
    [SerializeField] float hitCooldown = 1.0f;     // 한 번 박을 때 접촉이 여러 번 와도 한 단계만

    public int Stage { get; private set; }
    /// <summary>마지막으로 단계가 오른 이유(QA).</summary>
    public string LastHit { get; private set; } = "";

    float lastHitAt = -99f;
    // 연기 = 파티클(2026-11-03 도현 판정 「연기도 어느 정도 사실적으로 — 지금은 다른 에셋·환경과 안 어울림」).
    // 불투명 회색 상자 → 반투명 부드러운 덩어리. URP Particles/Lit 라 맵 해·주변광·안개를 실시간으로 받는다
    // (텍스처는 흰색 + 모양(알파)만 — 빛·그림자를 굽지 않음). 텍스처는 코드로 만든 값 노이즈(AI·외부 에셋 아님).
    ParticleSystem smoke;
    Material smokeMat;
    // 회색 대역 조각(콜라이더 없음 — 보이기만, 운전에 영향 없음)
    Transform hinge, panel, bumper, fender;
    Material greyMat;
    static Texture2D smokeTex;

    public static CartDamage Of(Component car)
    {
        if (car == null) return null;
        var d = car.GetComponent<CartDamage>();
        return d != null ? d : car.gameObject.AddComponent<CartDamage>();
    }

    public void AddStage(int n, string why)
    {
        int s = Mathf.Clamp(Stage + n, 0, MaxStage);
        if (s == Stage) return;
        Stage = s; LastHit = why;
        Debug.Log($"[CartDamage] 상처 {Stage}단계 — {why}");
        Show();
    }

    public void Clear()
    {
        Stage = 0; LastHit = "수리";
        Show();
    }

    void OnCollisionEnter(Collision c)
    {
        if (Time.time - lastHitAt < hitCooldown) return;
        if (c.rigidbody != null && c.rigidbody.transform.IsChildOf(transform)) return;   // 실은 짐
        float best = 0f;
        for (int i = 0; i < c.contactCount; i++)
        {
            var ct = c.GetContact(i);
            if (ct.normal.y > 0.5f) continue;            // 바닥에 떨어진 것(착지)은 상처가 아니다
            best = Mathf.Max(best, Mathf.Abs(Vector3.Dot(c.relativeVelocity, ct.normal)));
        }
        if (best < hitSpeed) return;
        lastHitAt = Time.time;
        AddStage(1, $"{c.collider.name} 에 {best:F1} m/s");
    }

    void Build()
    {
        if (smoke == null) BuildSmoke();
        if (hinge == null) BuildParts();
    }

    // 카트 로컬 단위(카트 루트 스케일 0.7): 너비 ±0.91 · 높이 0~1.3 · 코 끝 z 1.41(2026-11-03 메시 경계 실측)
    void BuildParts()
    {
        greyMat = MakeGreyMat();
        hinge = new GameObject("CartDamage_Hinge (greybox)").transform;
        hinge.SetParent(transform, false);
        hinge.localPosition = new Vector3(0f, 0.30f, 1.42f);        // 코 아래 모서리 = 앞판이 꺾이는 경첩
        panel = Part("CartDamage_Part_FrontPanel", hinge, new Vector3(0f, 0.28f, 0.05f), new Vector3(1.35f, 0.56f, 0.07f));
        bumper = Part("CartDamage_Part_Bumper", transform, Vector3.zero, new Vector3(1.75f, 0.16f, 0.2f));
        fender = Part("CartDamage_Part_Fender", transform, Vector3.zero, new Vector3(0.07f, 0.42f, 0.7f));
    }

    Transform Part(string n, Transform parent, Vector3 pos, Vector3 scale)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cube.GetComponent<Collider>());
        cube.name = n + " (greybox)";
        cube.layer = gameObject.layer;
        var t = cube.transform;
        t.SetParent(parent, false);
        t.localPosition = pos; t.localScale = scale;
        var r = cube.GetComponent<Renderer>();
        r.sharedMaterial = greyMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return t;
    }

    static Material MakeGreyMat()
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { name = "CartDamage_Grey (runtime)", hideFlags = HideFlags.DontSave };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        return m;
    }

    void ShowParts()
    {
        bool on = Stage > 0;
        hinge.gameObject.SetActive(on);
        bumper.gameObject.SetActive(Stage >= 2);
        fender.gameObject.SetActive(Stage >= 3);
        if (!on) return;
        // 앞판: 단계마다 보닛 쪽으로 더 젖혀지고(−15→−30→−48°, 윗면이 위·빛 쪽을 봐서 어두운 차고에서도 회색으로 읽힘) 한쪽으로 더 비틀린다(10→21→33°)
        float k = Stage;
        hinge.localRotation = Quaternion.Euler(Stage == 1 ? -15f : Stage == 2 ? -30f : -48f, 5f * k, 11f * k - 1f);
        // 범퍼(2단계부터): 오른쪽 끝이 땅으로 처짐
        bumper.localPosition = new Vector3(0.1f * k, 0.22f - 0.03f * k, 1.50f + 0.04f * k);
        bumper.localRotation = Quaternion.Euler(0f, 6f * k, -9f * k);
        // 펜더(3단계): 왼쪽 앞 펜더가 바깥으로 벌어짐
        fender.localPosition = new Vector3(-0.98f, 0.55f, 1.0f);
        fender.localRotation = Quaternion.Euler(0f, -25f, 30f);
        float g = Stage == 1 ? 0.85f : Stage == 2 ? 0.75f : 0.65f;   // 심할수록 조금 어두운 회색(차고 안이 어두워 밝은 쪽으로)
        greyMat.color = new Color(g, g, g);
        if (greyMat.HasProperty("_BaseColor")) greyMat.SetColor("_BaseColor", new Color(g, g, g));
        // 차고 그늘·노을빛에서 보랏빛 검정으로 묻혀(2026-11-03 캡처) 회색 대역으로 읽히게 약한 중성 회색 자체 밝기
        greyMat.EnableKeyword("_EMISSION");
        if (greyMat.HasProperty("_EmissionColor")) greyMat.SetColor("_EmissionColor", new Color(g, g, g) * 0.35f);
    }

    void Show()
    {
        if (Stage == 0)
        {
            if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (hinge != null) ShowParts();
            return;
        }
        Build();
        ShowParts();
        // 연기: 단계마다 더 자주·더 크게·더 진하게(1 가는 회색 줄기 → 3 짙은 연기 기둥).
        // 연기가 유일한 표시라(앞판 뺌) 1단계도 정비소 안 어두운 바닥·밝은 문 밖 앞에서 읽혀야 한다 — 1단계 기존 값(6개/초·옅음)은 화면에서 안 읽혔다.
        var main = smoke.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f + 0.15f * Stage, 0.55f + 0.25f * Stage);
        // 밝은 회색 — 단계는 양·크기·진하기(알파)로 구분. 3단계를 검게(0.3) 하면 어두운 차고 벽 앞에서 사라졌다(검증 2회째 crit1). 빛은 셰이더가 곱한다
        float sg = Stage == 1 ? 0.85f : Stage == 2 ? 0.8f : 0.72f;
        float sa = Stage == 1 ? 0.9f : 1f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(sg, sg * 0.97f, sg * 0.93f, sa), new Color(sg * 0.85f, sg * 0.83f, sg * 0.8f, sa * 0.85f));
        var em = smoke.emission;
        em.rateOverTime = Stage == 1 ? 10f : Stage == 2 ? 16f : 24f;
        if (!smoke.isPlaying) smoke.Play();
    }

    void BuildSmoke()
    {
        var go = new GameObject("CartDamage_Smoke (greybox)");
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.75f, 0.8f);   // 카트 코 위(엔진 덮개 틈)
        smoke = go.AddComponent<ParticleSystem>();
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = smoke.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // 카트가 달리면 연기는 뒤에 남는다
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = -0.02f;                                 // 더운 연기라 천천히 떠오름
        main.maxParticles = 120;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        var vel = smoke.velocityOverLifetime;                           // 위로 + 살짝 옆으로 흩어짐
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

        var noise = smoke.noise;                                        // 곧은 기둥이 아니라 일렁이게
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.3f;

        var rot = smoke.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

        var size = smoke.sizeOverLifetime;                              // 나올 땐 작고 퍼지며 커짐
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.4f, 1f), new Keyframe(1f, 1.9f)));

        var col = smoke.colorOverLifetime;                              // 짧게 짙어졌다가 퍼지면서 천천히 옅어짐
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.92f, 0.92f, 0.92f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.sortMode = ParticleSystemSortMode.Distance;
        r.material = smokeMat = MakeSmokeMat();
    }

    /// URP Particles/Lit 반투명(알파 블렌드) — 맵 해·주변광·안개를 받는다. 없으면 Unlit → Sprites/Default 순으로 물러선다.
    static Material MakeSmokeMat()
    {
        var sh = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var m = new Material(sh) { name = "CartDamage_Smoke (runtime)", hideFlags = HideFlags.DontSave };
        m.SetFloat("_Surface", 1f);                                  // Transparent
        m.SetFloat("_Blend", 0f);                                    // Alpha
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Smoothness", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        var tex = SmokeTex();
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        return m;
    }

    /// 32×32 연기 덩어리: 둥근 감쇠 × 펄린 노이즈 3겹(울퉁불퉁한 가장자리). 색은 흰색 — 알파에 모양만 담는다(빛 굽지 않음).
    static Texture2D SmokeTex()
    {
        if (smokeTex != null) return smokeTex;
        const int N = 32;
        smokeTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "CartDamage_SmokeTex", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float n = 0.5f * Mathf.PerlinNoise(x * 0.12f + 3.1f, y * 0.12f + 7.7f)
                        + 0.3f * Mathf.PerlinNoise(x * 0.25f + 11.3f, y * 0.25f + 1.9f)
                        + 0.2f * Mathf.PerlinNoise(x * 0.5f + 5.5f, y * 0.5f + 13.1f);
                float d = Mathf.Sqrt(u * u + v * v) + (n - 0.5f) * 0.55f;   // 가장자리가 노이즈로 울퉁불퉁
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);                                  // 부드러운 감쇠
                a *= Mathf.Lerp(0.7f, 1f, n);                               // 속 짙기도 고르지 않게
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        smokeTex.SetPixels32(px);
        smokeTex.Apply(false, true);
        return smokeTex;
    }

    void OnDestroy()
    {
        if (smoke != null) Destroy(smoke.gameObject);
        if (smokeMat != null) Destroy(smokeMat);
        if (hinge != null) Destroy(hinge.gameObject);
        if (bumper != null) Destroy(bumper.gameObject);
        if (fender != null) Destroy(fender.gameObject);
        if (greyMat != null) Destroy(greyMat);
    }
}
