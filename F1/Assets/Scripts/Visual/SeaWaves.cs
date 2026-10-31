using UnityEngine;

/// <summary>
/// D-235 노을 바다(DuskSea.shader)의 Gerstner 파 4개를 C# 으로 똑같이 계산한다 — 찌가 셰이더가 그린 수면에 뜨게.
///  · 파라미터는 머티리얼에서 읽는다(_W0~3, _Gerst, _Disp, _Style, _StepFps, _DepthTex/_DepthRect) → 셰이더 값을 바꾸면 찌도 따라감.
///  · 시간 = 셰이더 Now() 와 같다: 전역 _PCSeaClock.x = 1 이면 y, 아니면 _Time.y(URP: Play = Time.time). B 는 _StepFps 박자로 끊김.
///  · 셰이더는 정점을 옆으로도 민다 → 「이 xz 위에 있는 수면」을 찾으려면 움직이기 전 자리 x0 를 거꾸로 찾는다(x0 + 옆밀림(x0) = xz, 4번 반복).
///  · 파문(_PCRipples, 최대 4개) 전역도 여기서 넣는다.
/// </summary>
public static class SeaWaves
{
    static readonly int ClockId = Shader.PropertyToID("_PCSeaClock"), RipId = Shader.PropertyToID("_PCRipples");
    static readonly int[] WId = { Shader.PropertyToID("_W0"), Shader.PropertyToID("_W1"), Shader.PropertyToID("_W2"), Shader.PropertyToID("_W3") };
    static readonly int GerstId = Shader.PropertyToID("_Gerst"), DispId = Shader.PropertyToID("_Disp"), StyleId = Shader.PropertyToID("_Style"),
        FpsId = Shader.PropertyToID("_StepFps"), DepthTexId = Shader.PropertyToID("_DepthTex"), DepthRectId = Shader.PropertyToID("_DepthRect");

    /// <summary>셰이더 Now() 와 같은 시계(초).</summary>
    public static float Now()
    {
        var c = Shader.GetGlobalVector(ClockId);
        return c.x > .5f ? c.y : (Application.isPlaying ? Time.time : Time.realtimeSinceStartup);   // URP ScriptableRenderer 가 _Time 에 넣는 값과 같음
    }

    // 깊이 칸(_DepthTex R) — 읽기 불가 텍스처라 한 번 GPU 에서 복사해 둔다
    static Texture2D depthCpu; static Texture depthSrc;
    static Texture2D DepthCpu(Material m)
    {
        var src = m.GetTexture(DepthTexId);
        if (src == null) return null;
        if (depthCpu != null && depthSrc == src) return depthCpu;
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active; RenderTexture.active = rt;
        depthCpu = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        depthCpu.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); depthCpu.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        depthSrc = src;
        return depthCpu;
    }

    /// <summary>셰이더 DepthDist().x — 물 밑 깊이(m). 깊이 칸 밖 = 최대 깊이.</summary>
    static float Depth(Material m, float x, float z)
    {
        var r = m.GetVector(DepthRectId);
        float u = (x - r.x) / r.z, v = (z - r.y) / r.z;
        if (u < 0 || u > 1 || v < 0 || v > 1) return r.w;
        var t = DepthCpu(m);
        return t != null ? t.GetPixelBilinear(u, v).r * r.w : r.w;
    }

    static float ShallowAtt(Material m, float depth)
    {
        float s = m.GetVector(GerstId).w;
        return Mathf.Clamp01(depth / s) * .85f + .15f * Mathf.Clamp01(depth / (s * .3f));
    }

    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

    /// <summary>셰이더 WaveTime().</summary>
    public static float WaveTime(Material m)
    {
        float t = Now(), fps = m.GetFloat(StyleId) > .5f ? m.GetFloat(FpsId) : m.GetVector(GerstId).z;
        if (fps > .5f) t = Mathf.Floor(t * fps) / fps;
        return t * m.GetVector(GerstId).y;
    }

    /// <summary>셰이더 vert() 의 정점 이동(움직이기 전 자리 xz0 → 옆·위 밀림). 격자 밖이면 0.</summary>
    static Vector3 Displace(Material m, Vector2 xz0, float t, out Vector3 dDx, out Vector3 dDz)
    {
        dDx = dDz = Vector3.zero;
        var disp = m.GetVector(DispId); var g = m.GetVector(GerstId);
        float r = Vector2.Distance(xz0, new Vector2(disp.y, disp.z));
        if (disp.x < .5f || r > disp.w) return Vector3.zero;
        float spacing = Mathf.Max(.054f * r, .02f);
        float edge = 1 - Smooth(disp.w * .7f, disp.w * .97f, r);
        float att = ShallowAtt(m, Depth(m, xz0.x, xz0.y)) * edge;
        Vector3 d = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            var w = m.GetVector(WId[i]);
            float a = w.x * Mathf.Deg2Rad; Vector2 D = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            float k = 6.2832f / w.y, A = w.z * att * (1 - Smooth(.12f, .25f, spacing / w.y));
            float Q = g.x / Mathf.Max(k * w.z * 4, 1e-4f);
            float ph = k * Vector2.Dot(D, xz0) - Mathf.Sqrt(9.8f * k) * t + w.w;
            float c = Mathf.Cos(ph), s = Mathf.Sin(ph);
            d += new Vector3(Q * A * D.x * c, A * s, Q * A * D.y * c);
            // 기울기용 편미분(감쇠 att 의 공간 변화는 무시 — 찌 기울기만 씀)
            float dc = -s * k, ds = c * k;
            dDx += new Vector3(Q * A * D.x * dc * D.x, A * ds * D.x, Q * A * D.y * dc * D.x);
            dDz += new Vector3(Q * A * D.x * dc * D.y, A * ds * D.y, Q * A * D.y * dc * D.y);
        }
        return d;
    }

    /// <summary>
    /// 물 알갱이 하나(움직이기 전 자리 x0, z0)가 지금 있는 월드 자리와 그 자리 수면 노멀 — 셰이더 정점과 똑같이 옆·위로 돈다.
    /// 뜬 물체(찌)는 이걸 따르면 그려진 수면에 그대로 붙는다.
    /// </summary>
    public static Vector3 Particle(Material m, float seaY, float x0, float z0, out Vector3 normal)
    {
        var d = Displace(m, new Vector2(x0, z0), WaveTime(m), out var dx, out var dz);
        var tx = new Vector3(1 + dx.x, dx.y, dx.z); var tz = new Vector3(dz.x, dz.y, 1 + dz.z);
        normal = Vector3.Cross(tz, tx).normalized;
        return new Vector3(x0 + d.x, seaY + d.y, z0 + d.z);
    }

    /// <summary>
    /// 월드 xz 바로 위(아래)의 수면 높이(바다 판 y 기준 + 파)와 수면 노멀. seaY = 바다 판(Sea/SeaNear) 월드 y.
    /// </summary>
    public static float Sample(Material m, float seaY, float x, float z, out Vector3 normal)
    {
        float t = WaveTime(m);
        Vector2 target = new Vector2(x, z), x0 = target;
        Vector3 d = Vector3.zero, dx = Vector3.zero, dz = Vector3.zero;
        for (int it = 0; it < 5; it++)
        {
            d = Displace(m, x0, t, out dx, out dz);
            x0 = target - new Vector2(d.x, d.z);
        }
        d = Displace(m, x0, t, out dx, out dz);
        var tx = new Vector3(1 + dx.x, dx.y, dx.z); var tz = new Vector3(dz.x, dz.y, 1 + dz.z);
        normal = Vector3.Cross(tz, tx).normalized;
        return seaY + d.y;
    }

    // 파문: (x, z, 시작 시각 = Now(), 세기 0..1). 세기 0 = 빈 칸
    static readonly Vector4[] ripples = new Vector4[4];
    static int next;
    public static void AddRipple(Vector3 at, float strength)
    {
        ripples[next] = new Vector4(at.x, at.z, Now(), strength);
        next = (next + 1) % ripples.Length;
        Shader.SetGlobalVectorArray(RipId, ripples);
    }
    public static void ClearRipples()
    {
        for (int i = 0; i < ripples.Length; i++) ripples[i] = Vector4.zero;
        next = 0;
        Shader.SetGlobalVectorArray(RipId, ripples);
    }
}
