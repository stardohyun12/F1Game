p = 'F1/Assets/Scripts/Visual/RoomDiorama.cs'
s = open(p, encoding='utf-8').read()


def rep(a, b):
    global s
    assert s.count(a) == 1, a[:60]
    s = s.replace(a, b)


rep('''/// - 그리는 동안(저해상 카메라에만): A7 셰이더 전역 _A7Dio 를 켜 방 상자 밖·카메라 쪽 벽·천장을 버린다. 배경 검정, 방과 안 겹치는 렌더러·지형은 끈다(forceRenderingOff — enabled 는 안 건드림).
///   빛 = 창 한 곳 + 책상 전등 한 곳(실시간 점광원, 실행 중에만 만든다). 방에 닿는 다른 점·스팟 빛과 전역광은 낮춘다. 나오면 전부 되돌린다.''',
'''/// - 그리는 동안(저해상 카메라에만): A7 셰이더 전역 _A7Dio 를 켜 집을 단면으로 자른다 — 방 천장 위(지붕 포함)와 카메라 쪽 벽 바깥만 버리고,
///   집 밖(하늘·바다·마을·지형)과 방 바닥 아래(1층)는 그대로 그린다. 배경은 카메라 원래 하늘(과제 20261104-1635, D-299 「창 밖은 바깥 세계」).
///   빛 = 씬의 해(방향광 — 시간대 dusk 로 색·각도가 바뀜, 그림자 켬) × (1 − 안개) + 약한 바탕빛 + 책상 전등 한 곳(실시간 점광원, 실행 중에만 만든다).
///   해가 꺼지면(밤) 전등만 남는다. 방에 닿는 다른 점·스팟 빛과 전역광은 낮춘다. 창 유리는 투명 사본으로 바꿔 그린다. 나오면 전부 되돌린다.''')
rep('''    public float distance = 4.5f;''', '''    public float distance = 5f;''')
rep('''    public Color ambient = new Color(0.10f, 0.11f, 0.14f);
    public Color windowColor = new Color(0.62f, 0.74f, 1.00f);
    public float windowIntensity = 3.2f, windowRange = 4.5f;
''', '''    public Color ambient = new Color(0.10f, 0.11f, 0.14f);
    [Tooltip("날씨 안개 0..1 — 창빛(해) 세기 = 1 − 안개. 날씨 시스템이 아직 없어 값 하나만 둔다(생기면 여기 묶는다)")]
    [Range(0f, 1f)] public float fog = 0f;
    [Tooltip("창 유리 투명도(0 = 안 보임, 1 = 불투명)")]
    [Range(0f, 1f)] public float glassAlpha = 0.18f;
    [Tooltip("단면으로 자르는 집 범위(방 상자 좌표 — 방 가운데에서 이만큼 옆까지). 그 밖 세계는 안 자른다")]
    public float houseReach = 3f;
''')
rep('''    // 되돌릴 것
    readonly List<Renderer> hidden = new List<Renderer>();
    readonly List<Terrain> hiddenTerrain = new List<Terrain>();
    readonly List<Light> dimmed = new List<Light>();
    Light winLight, lampLight;
    CameraClearFlags savedClear; Color savedBg; float savedAmb; bool rendering;''', '''    // 되돌릴 것
    readonly List<Renderer> glass = new List<Renderer>();
    readonly List<Material> glassSaved = new List<Material>();
    readonly List<Light> dimmed = new List<Light>();
    Light lampLight; Material glassMat;
    float savedAmb; bool rendering;
    /// <summary>창 고르기 결과(검사·캡처용) — 책상 창 가운데. 못 찾으면 방 가운데.</summary>
    public Vector3 WindowCenter { get; private set; }''')
rep('''        if (w <= 0f) { RestoreDot(); return; }
''', '''        if (rendering) UpdateGlass();
        if (w <= 0f) { RestoreDot(); return; }
''')
a = s.index('        if (on)\n        {\n            savedClear')
b = s.index('        ApplyCalendar();\n    }\n\n    Bounds WorldBounds()')
s = s[:a] + '''        if (on)
        {
            savedAmb = RenderSettings.ambientIntensity;
            RenderSettings.ambientIntensity = savedAmb * 0.25f;
            var wb = WorldBounds();
            // 창 유리: 19 는 껐다 → 투명 사본으로 바꿔 그린다(공유 .mat 은 안 건드림). 단면에 걸린 유리는 UpdateGlass 가 매 프레임 숨긴다
            foreach (var r in room.parent.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("GLASS_")) continue;
                glass.Add(r); glassSaved.Add(r.sharedMaterial);
                r.sharedMaterial = GlassMat(r.sharedMaterial);
            }
            UpdateGlass();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (!l.enabled || l.type == LightType.Directional) continue;
                if (wb.SqrDistance(l.transform.position) < l.range * l.range) { l.enabled = false; dimmed.Add(l); }
            }
            PickWindow(room, box, out var wc, out _); WindowCenter = wc;
            lampLight = MakeLight("DIO_DeskLamp", LampPoint(), lampColor, lampIntensity, lampRange);
            foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
        }
        else
        {
            RenderSettings.ambientIntensity = savedAmb;
            for (int i = 0; i < glass.Count; i++) if (glass[i] != null) { glass[i].sharedMaterial = glassSaved[i]; glass[i].forceRenderingOff = false; }
            foreach (var l in dimmed) if (l != null) l.enabled = true;
            glass.Clear(); glassSaved.Clear(); dimmed.Clear();
            if (lampLight != null) Destroy(lampLight.gameObject);
            if (player != null && foot != null && foot.enabled)
                foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
''' + s[b:]
a = s.index('    // 창 = 방 상자 벽에 붙은')
b = s.index('    // 책상 전등 = ')
s = s[:a] + '''    /// <summary>바닥에서 이 높이(m) 안까지 내려오는 유리 = 유리문 — 창 고르기에서 뺀다(19 는 문 유리를 창으로 골랐다).</summary>
    public const float DoorGlassFloor = 0.5f;

    /// <summary>
    /// 창 = 방 상자 벽에 붙은 GLASS_* 중 유리문(아래 끝이 바닥 + DoorGlassFloor 안)을 뺀 것을 벽(±x·±z)별로 묶어 넓이가 가장 큰 무리 → 책상 창.
    /// 돌려주는 것: 그 무리 가운데, 아래 끝의 바닥 위 높이(m). 없으면 false·방 가운데.
    /// </summary>
    public static bool PickWindow(Transform room, Matrix4x4 box, out Vector3 center, out float bottomAboveFloor)
    {
        var inv = box.inverse;
        float floorY = inv.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)).y;
        var sum = new Dictionary<int, Vector3>(); var area = new Dictionary<int, float>(); var low = new Dictionary<int, float>();
        foreach (var r in room.parent.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.name.StartsWith("GLASS_")) continue;
            Vector3 q = box.MultiplyPoint3x4(r.bounds.center);
            if (MaxAbs(q) > 1.15f || Mathf.Abs(q.y) > 0.9f) continue;
            if (r.bounds.min.y - floorY < DoorGlassFloor) continue;
            int side = Mathf.Abs(q.x) > Mathf.Abs(q.z) ? (q.x > 0 ? 0 : 1) : (q.z > 0 ? 2 : 3);
            var sz = r.bounds.size; float a = sz.x * sz.y + sz.z * sz.y;
            sum[side] = (sum.TryGetValue(side, out var v) ? v : Vector3.zero) + r.bounds.center * a;
            area[side] = (area.TryGetValue(side, out var f) ? f : 0f) + a;
            low[side] = Mathf.Min(low.TryGetValue(side, out var m) ? m : float.MaxValue, r.bounds.min.y - floorY);
        }
        int best = -1; float ba = 0f;
        foreach (var kv in area) if (kv.Value > ba) { ba = kv.Value; best = kv.Key; }
        center = inv.MultiplyPoint3x4(Vector3.zero); bottomAboveFloor = -1f;
        if (best < 0) return false;
        center = sum[best] / ba; bottomAboveFloor = low[best];
        return true;
    }

    // 셰이더 A7DioCut 과 같은 규칙(유리처럼 A7 이 아닌 것용): 집 범위 안에서 천장 위, 또는 카메라 쪽 벽 바깥
    bool Cut(Vector3 wp)
    {
        Vector3 b = box.MultiplyPoint3x4(wp), c = box.MultiplyPoint3x4(cam.transform.position);
        if (Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.z)) > houseReach || b.y < -1f) return false;
        if (b.y > ceilCut) return true;
        if (Mathf.Abs(c.x) > 1f && b.x * Mathf.Sign(c.x) > wallCut) return true;
        if (Mathf.Abs(c.z) > 1f && b.z * Mathf.Sign(c.z) > wallCut) return true;
        return false;
    }

    void UpdateGlass()
    {
        foreach (var r in glass) if (r != null) r.forceRenderingOff = Cut(r.bounds.center);
    }

    // 원래 유리 색 그대로 + 투명(URP Unlit 사본, 실행 중에만)
    Material GlassMat(Material src)
    {
        if (glassMat == null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "DIO_Glass(runtime)" };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_Cull", 0f); m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            glassMat = m;
        }
        Color c = src != null && src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : new Color(0.7f, 0.8f, 0.9f);
        c.a = glassAlpha; glassMat.SetColor("_BaseColor", c);
        return glassMat;
    }

''' + s[b:]
rep('''        Shader.SetGlobalVector(DioId, new Vector4(1f, 0f, wallCut, ceilCut));
        Shader.SetGlobalColor(DioAmbId, ambient);''', '''        // y = 해 세기 배수(창빛 = 1 − 안개) · 바탕빛 w = 단면으로 자르는 집 범위
        Shader.SetGlobalVector(DioId, new Vector4(1f, 1f - Mathf.Clamp01(fog), wallCut, ceilCut));
        Color a = QualitySettings.activeColorSpace == ColorSpace.Linear ? ambient.linear : ambient;
        Shader.SetGlobalVector(DioAmbId, new Vector4(a.r, a.g, a.b, houseReach));''')
rep('''    void OnDisable()
    {''', '''    void OnDestroy() { if (glassMat != null) Destroy(glassMat); }

    void OnDisable()
    {''')
open(p, 'w', encoding='utf-8').write(s)
print('ok')
