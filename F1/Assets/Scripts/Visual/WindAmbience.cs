using UnityEngine;

/// <summary>
/// 과제 1827: 바람 소리. 소리 파일이 아직 없어서 시작할 때 코드로 만든다(갈색 잡음 + 느린 돌풍, 6초 반복).
/// 크기는 카메라 높이로: lowY 아래는 거의 안 들리고 highY(언덕 정점·들판) 에서 가장 크다. 실제 소리 에셋이 생기면 clip 슬롯에 넣으면 그걸 쓴다.
/// </summary>
public class WindAmbience : MonoBehaviour
{
    public AudioClip clip;
    public float lowY = 6f, highY = 19f;
    public float quietVolume = 0.04f, loudVolume = 0.32f;
    [Tooltip("D-227: 걷기·앉기처럼 런 밖에서도 울리는 바람(해안 분위기). 끄면 예전처럼 런 중에만.")]
    public bool ignoreRun;
    [Range(0f, 1f), Tooltip("D-227: 돌풍 — 음량을 십몇 초 주기로 천천히 오르내림(0 = 일정).")]
    public float gust;
    AudioSource src;
    Transform listener;

    void Start()
    {
        // D-227: 이 오브젝트에 AudioSource 슬롯이 이미 있으면(3D 거리 설정 포함) 그걸 쓴다 — 없으면 예전처럼 2D 로 새로
        bool own = !TryGetComponent(out src);
        if (own) src = gameObject.AddComponent<AudioSource>();
        src.clip = clip != null ? clip : (src.clip != null ? src.clip : MakeWind(6f));
        src.loop = true; if (own) src.spatialBlend = 0f; src.volume = quietVolume; src.playOnAwake = false;
        src.Play();
    }

    void Update()
    {
        // 과제 0131: 런이 안 도는 화면(보드·정산·차고)에선 멈춘다 — 회귀 점검 '런 비활성 잔류 소리' 규칙(HarnessChecks.CheckRunInactiveSilent).
        bool run = ignoreRun || Services.Run == null || Services.Run.RunActive;
        if (!run) { if (src.isPlaying) src.Pause(); return; }
        if (!src.isPlaying) src.UnPause();
        if (listener == null) { var l = FindFirstObjectByType<AudioListener>(); if (l != null) listener = l.transform; else return; }
        float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lowY, highY, listener.position.y));
        // D-227: 녹음 바람(amb_wind_01)은 음량이 거의 일정 → gust 만큼 천천히 흔든다(0 = 예전 그대로)
        float g = gust > 0f ? 1f - gust + gust * Mathf.PerlinNoise(Time.time * 0.11f, 0.37f) * 1.6f : 1f;
        src.volume = Mathf.MoveTowards(src.volume, Mathf.Lerp(quietVolume, loudVolume, k) * g, Time.deltaTime * 0.2f);
    }

    /// <summary>갈색 잡음을 돌풍 곡선으로 키웠다 줄였다. 끝 0.5초를 처음과 섞어 이음매를 없앤다.</summary>
    static AudioClip MakeWind(float seconds)
    {
        const int rate = 22050; int n = (int)(rate * seconds), fade = rate / 2;
        var data = new float[n]; var rng = new System.Random(1827); float b = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            b = Mathf.Clamp(b + ((float)rng.NextDouble() * 2f - 1f) * 0.02f, -1f, 1f) * 0.998f;
            lp += (b - lp) * 0.08f;
            float t = i / (float)rate;
            float gust = 0.55f + 0.3f * Mathf.Sin(t * 2f * Mathf.PI / seconds) + 0.15f * Mathf.Sin(t * 2f * Mathf.PI * 3f / seconds + 1.3f);
            data[i] = lp * gust * 2.2f;
        }
        for (int i = 0; i < fade; i++) { float a = i / (float)fade; data[i] = data[i] * a + data[n - fade + i] * (1f - a); }
        System.Array.Resize(ref data, n - fade);
        var c = AudioClip.Create("WindProcedural", data.Length, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
}
