using UnityEngine;

/// <summary>
/// C-001 그레이박스: 카트 상처 0~3. 벽 같은 데 세게 부딪히면(접촉면 쪽 상대 속도가 hitSpeed 이상) 한 단계 오른다.
/// 보이기만 한다 — 운전·속도·화물에는 아무 영향 없음(카드 잠금, 영향은 도현 한 판 뒤 따로 정함).
/// 표시 = 회색 대역만: 앞판(회색 판)이 단계마다 더 기울고 어두워짐 + 작은 회색 연기 조각(단계마다 더 자주).
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
    Transform panel; Renderer panelR;
    Material mat, smokeMat;
    readonly Transform[] puffs = new Transform[10];
    readonly float[] puffAge = new float[10];
    float puffNext;
    const float PuffLife = 1.4f;

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
        if (panel != null) return;
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cube.GetComponent<Collider>());
        cube.name = "CartDamage_Panel (greybox)";
        cube.layer = gameObject.layer;
        panel = cube.transform;
        panel.SetParent(transform, false);
        panelR = cube.GetComponent<Renderer>();
        mat = new Material(panelR.sharedMaterial) { hideFlags = HideFlags.DontSave };
        panelR.sharedMaterial = mat;
        panelR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smokeMat = new Material(panelR.sharedMaterial) { hideFlags = HideFlags.DontSave, color = new Color(0.68f, 0.68f, 0.68f) };
    }

    void Show()
    {
        if (Stage == 0)
        {
            if (panel != null) panel.gameObject.SetActive(false);
            for (int i = 0; i < puffs.Length; i++) if (puffs[i] != null) puffs[i].gameObject.SetActive(false);
            return;
        }
        Build();
        panel.gameObject.SetActive(true);
        // 앞판: 카트 코 위에 걸친 회색 판. 단계마다 앞으로 더 꺾이고 옆으로 비틀린다.
        panel.localPosition = new Vector3(0.08f * Stage, 0.42f + 0.03f * Stage, 0.92f);
        panel.localRotation = Quaternion.Euler(18f * Stage, 6f * Stage, -9f * Stage);
        panel.localScale = new Vector3(0.95f, 0.32f, 0.06f);
        float g = 0.62f - 0.13f * Stage;
        mat.color = new Color(g, g, g);
    }

    void Update()
    {
        if (Stage == 0) return;
        float dt = Time.deltaTime;
        // 연기: 1단계 0.7초 · 2단계 0.35초 · 3단계 0.18초마다 한 조각
        puffNext -= dt;
        if (puffNext <= 0f)
        {
            puffNext = 0.7f / (1 << (Stage - 1));
            Puff();
        }
        for (int i = 0; i < puffs.Length; i++)
        {
            var p = puffs[i];
            if (p == null || !p.gameObject.activeSelf) continue;
            puffAge[i] += dt;
            float k = puffAge[i] / PuffLife;
            if (k >= 1f) { p.gameObject.SetActive(false); continue; }
            p.position += Vector3.up * (0.45f * dt);
            p.localScale = Vector3.one * Mathf.Lerp(0.06f, 0.16f, k) * (k > 0.75f ? (1f - k) / 0.25f : 1f);
        }
    }

    void Puff()
    {
        int idx = -1;
        for (int i = 0; i < puffs.Length; i++) if (puffs[i] == null || !puffs[i].gameObject.activeSelf) { idx = i; break; }
        if (idx < 0) return;
        if (puffs[idx] == null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cube.GetComponent<Collider>());
            cube.name = "CartDamage_Puff (greybox)";
            cube.layer = gameObject.layer;
            var r = cube.GetComponent<Renderer>();
            r.sharedMaterial = smokeMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            puffs[idx] = cube.transform;
        }
        var t = puffs[idx];
        t.gameObject.SetActive(true);
        t.position = transform.TransformPoint(new Vector3(Random.Range(-0.25f, 0.25f), 0.6f, 0.75f));
        t.rotation = Quaternion.Euler(Random.Range(0f, 90f), Random.Range(0f, 90f), 0f);
        t.localScale = Vector3.one * 0.06f;
        puffAge[idx] = 0f;
    }

    void OnDestroy()
    {
        for (int i = 0; i < puffs.Length; i++) if (puffs[i] != null) Destroy(puffs[i].gameObject);
        if (mat != null) Destroy(mat);
        if (smokeMat != null) Destroy(smokeMat);
    }
}
