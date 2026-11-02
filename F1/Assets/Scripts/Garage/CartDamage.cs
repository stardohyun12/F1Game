using UnityEngine;

/// <summary>
/// C-001 그레이박스: 카트 상처 0~3. 벽 같은 데 세게 부딪히면(접촉면 쪽 상대 속도가 hitSpeed 이상) 한 단계 오른다.
/// 보이기만 한다 — 운전·속도·화물에는 아무 영향 없음(카드 잠금, 영향은 도현 한 판 뒤 따로 정함).
/// 표시 = 회색 대역만: 단계마다 상한 조각이 하나씩 는다(1 앞판 기울어짐 · 2 + 왼쪽 펜더 들림 · 3 + 앞 범퍼 처짐), 전체가 어두워짐
/// + 회색 연기 조각(단계마다 더 자주·더 크게·더 진하게).
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
    Material smokeMat;
    const int PuffCount = 24;
    readonly Transform[] puffs = new Transform[PuffCount];
    readonly float[] puffAge = new float[PuffCount];
    float puffNext;
    const float PuffLife = 1.6f;

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
        smokeMat = new Material(src) { hideFlags = HideFlags.DontSave };
    }

    void Show()
    {
        if (Stage == 0)
        {
            for (int i = 0; i < MaxStage; i++) if (parts[i] != null) parts[i].gameObject.SetActive(false);
            for (int i = 0; i < PuffCount; i++) if (puffs[i] != null) puffs[i].gameObject.SetActive(false);
            return;
        }
        Build();
        for (int i = 0; i < MaxStage; i++) parts[i].gameObject.SetActive(i < Stage);
        // 0 앞판: 카트 코 위 회색 판. 단계마다 더 꺾이고 비틀린다.
        parts[0].localPosition = new Vector3(0.08f * Stage, 0.45f + 0.04f * Stage, 0.95f);
        parts[0].localRotation = Quaternion.Euler(20f * Stage, 8f * Stage, -12f * Stage);
        parts[0].localScale = new Vector3(0.95f, 0.34f, 0.07f);
        // 1 왼쪽 펜더: 옆으로 들려 바깥으로 벌어진 판(2단계부터)
        parts[1].localPosition = new Vector3(-0.62f, 0.42f, 0.55f);
        parts[1].localRotation = Quaternion.Euler(0f, 30f, -35f);
        parts[1].localScale = new Vector3(0.07f, 0.34f, 0.75f);
        // 2 앞 범퍼: 한쪽이 떨어져 비스듬히 처진 막대(3단계)
        parts[2].localPosition = new Vector3(0.1f, 0.2f, 1.18f);
        parts[2].localRotation = Quaternion.Euler(-10f, 0f, 22f);
        parts[2].localScale = new Vector3(1.15f, 0.14f, 0.12f);
        // 단계가 오를수록 전체가 어두워진다
        for (int i = 0; i < MaxStage; i++)
        {
            float g = 0.6f - 0.12f * Stage - 0.04f * i;
            partMats[i].color = new Color(g, g, g);
        }
        float sg = 0.85f - 0.08f * Stage;              // 연기: 짐 상자(갈색)와 갈리게 밝은 회색, 단계마다 조금 진해진다
        smokeMat.color = new Color(sg, sg, sg);
    }

    void Update()
    {
        if (Stage == 0) return;
        float dt = Time.deltaTime;
        // 연기: 1단계 0.6초 · 2단계 0.25초 · 3단계 0.1초마다 한 조각, 조각 크기도 단계마다 커진다
        puffNext -= dt;
        if (puffNext <= 0f)
        {
            puffNext = Stage == 1 ? 0.6f : Stage == 2 ? 0.25f : 0.1f;
            Puff();
        }
        float big = 0.18f + 0.1f * Stage;
        for (int i = 0; i < PuffCount; i++)
        {
            var p = puffs[i];
            if (p == null || !p.gameObject.activeSelf) continue;
            puffAge[i] += dt;
            float k = puffAge[i] / PuffLife;
            if (k >= 1f) { p.gameObject.SetActive(false); continue; }
            p.position += Vector3.up * (0.8f * dt);
            p.localScale = Vector3.one * Mathf.Lerp(0.1f, big, k) * (k > 0.75f ? (1f - k) / 0.25f : 1f);
        }
    }

    void Puff()
    {
        int idx = -1;
        for (int i = 0; i < PuffCount; i++) if (puffs[i] == null || !puffs[i].gameObject.activeSelf) { idx = i; break; }
        if (idx < 0) return;
        if (puffs[idx] == null)
        {
            puffs[idx] = MakeCube("CartDamage_Puff (greybox)", smokeMat);
        }
        var t = puffs[idx];
        t.gameObject.SetActive(true);
        t.position = transform.TransformPoint(new Vector3(Random.Range(-0.3f, 0.3f), 0.8f, 0.8f));
        t.rotation = Quaternion.Euler(Random.Range(0f, 90f), Random.Range(0f, 90f), 0f);
        t.localScale = Vector3.one * 0.1f;
        puffAge[idx] = 0f;
    }

    void OnDestroy()
    {
        for (int i = 0; i < PuffCount; i++) if (puffs[i] != null) Destroy(puffs[i].gameObject);
        for (int i = 0; i < MaxStage; i++) if (partMats[i] != null) Destroy(partMats[i]);
        if (smokeMat != null) Destroy(smokeMat);
    }
}
