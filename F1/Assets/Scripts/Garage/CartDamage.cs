using UnityEngine;

/// <summary>
/// C-001 그레이박스: 카트 상처 0~3. 벽 같은 데 세게 부딪히면(접촉면 쪽 상대 속도가 hitSpeed 이상) 한 단계 오른다.
/// 보이기만 한다 — 운전·속도·화물에는 아무 영향 없음(카드 잠금, 영향은 도현 한 판 뒤 따로 정함).
/// 표시 = 회색 대역만: 단계마다 상한 조각이 하나씩 는다(1 앞판 기울어짐 · 2 + 왼쪽 펜더 들림 · 3 + 앞 범퍼 처짐), 전체가 어두워짐
/// + 반투명 연기 파티클(URP Particles/Lit — 맵 빛·안개를 받음, 단계마다 더 자주·더 크게·더 진하게).
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
    // 상한 조각: 단계마다 하나씩 늘어난다(0=앞판 기울어짐, 1=왼쪽 펜더 들림, 2=앞 범퍼 처짐). 회색 대역.
    readonly Transform[] parts = new Transform[MaxStage];
    Material[] partMats = new Material[MaxStage];
    // 연기 = 파티클(2026-11-03 도현 판정 「연기도 어느 정도 사실적으로 — 지금은 다른 에셋·환경과 안 어울림」).
    // 불투명 회색 상자 → 반투명 부드러운 덩어리. URP Particles/Lit 라 맵 해·주변광·안개를 실시간으로 받는다
    // (텍스처는 흰색 + 모양(알파)만 — 빛·그림자를 굽지 않음). 텍스처는 코드로 만든 값 노이즈(AI·외부 에셋 아님).
    ParticleSystem smoke;
    Material smokeMat;
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

    Transform MakeCube(string name, Material m)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cube.GetComponent<Collider>());
        cube.name = name;
        cube.layer = gameObject.layer;
        var r = cube.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return cube.transform;
    }

    void Build()
    {
        if (parts[0] != null) return;
        var baseMat = GameObject.CreatePrimitive(PrimitiveType.Cube);   // 기본 재질만 빌림
        var src = baseMat.GetComponent<Renderer>().sharedMaterial;
        Destroy(baseMat);
        for (int i = 0; i < MaxStage; i++)
        {
            partMats[i] = new Material(src) { hideFlags = HideFlags.DontSave };
            parts[i] = MakeCube("CartDamage_Part" + i + " (greybox)", partMats[i]);
            parts[i].SetParent(transform, false);
        }
        BuildSmoke();
    }

    void Show()
    {
        if (Stage == 0)
        {
            for (int i = 0; i < MaxStage; i++) if (parts[i] != null) parts[i].gameObject.SetActive(false);
            if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return;
        }
        Build();
        for (int i = 0; i < MaxStage; i++) parts[i].gameObject.SetActive(i < Stage);
        // 0 앞판: 카트 코 위 회색 판. 단계마다 더 꺾이고 비틀린다(1단계 살짝 → 3단계 크게 젖혀짐).
        parts[0].localPosition = new Vector3(0.1f * Stage, 0.45f + 0.06f * Stage, 0.95f);
        parts[0].localRotation = Quaternion.Euler(18f * Stage, 10f * Stage, -15f * Stage);
        parts[0].localScale = new Vector3(0.95f, 0.38f, 0.08f);
        // 1 왼쪽 펜더: 옆으로 들려 카트 폭 밖으로 크게 벌어진 판(2단계부터) — 앞에서 봐도 옆으로 삐져나와 보이게
        parts[1].localPosition = new Vector3(-0.85f, 0.5f, 0.6f);
        parts[1].localRotation = Quaternion.Euler(0f, 25f, -50f);
        parts[1].localScale = new Vector3(0.08f, 0.5f, 0.8f);
        // 2 앞 범퍼: 한쪽이 떨어져 땅까지 비스듬히 처진 막대(3단계)
        parts[2].localPosition = new Vector3(0.15f, 0.22f, 1.2f);
        parts[2].localRotation = Quaternion.Euler(-10f, 0f, 28f);
        parts[2].localScale = new Vector3(1.3f, 0.16f, 0.14f);
        // 단계가 오를수록 전체가 어두워진다
        for (int i = 0; i < MaxStage; i++)
        {
            float g = 0.6f - 0.12f * Stage - 0.04f * i;
            partMats[i].color = new Color(g, g, g);
        }
        // 연기: 단계마다 더 자주·더 크게·더 진하게(1 엷은 김 → 3 짙은 회색 연기)
        var main = smoke.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f + 0.15f * Stage, 0.4f + 0.25f * Stage);
        float sg = 0.62f - 0.1f * Stage;                       // 1단계 회색(0.52) → 3단계 어두운 회색(0.32) — 빛은 셰이더가 곱한다
        float sa = Stage == 1 ? 0.75f : Stage == 2 ? 0.9f : 1f; // 1단계 엷은 김 → 3단계 짙은 연기
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(sg, sg * 0.97f, sg * 0.93f, sa), new Color(sg * 0.85f, sg * 0.83f, sg * 0.8f, sa * 0.8f));
        var em = smoke.emission;
        em.rateOverTime = Stage == 1 ? 6f : Stage == 2 ? 11f : 18f;
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
        main.maxParticles = 80;
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
        for (int i = 0; i < MaxStage; i++) if (partMats[i] != null) Destroy(partMats[i]);
        if (smokeMat != null) Destroy(smokeMat);
    }
}
