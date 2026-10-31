using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EngineAudio : MonoBehaviour
{
    [SerializeField] private ArcadeCarController car;
    [SerializeField] private AudioClip engineLoop;

    [Header("Pitch")]
    [SerializeField] private float idlePitch = 0.7f;
    [SerializeField] private float maxPitch = 2.1f;
    [SerializeField] private float pitchLerpSpeed = 6f;

    [Header("Volume")]
    // 도현 #3-2 (세션 16): 소리 큼 + 실제같음. idle 0.35→0.16, max 0.9→0.42.
    [SerializeField] private float idleVolume = 0.16f;
    [SerializeField] private float maxVolume = 0.42f;
    [SerializeField] private float volumeLerpSpeed = 4f;

    private AudioSource src;

    /// <summary>D-229 걷기·앉기(OnFootDirector) 중엔 주차된 카트 엔진 무음. 감독이 켜고 끈다.</summary>
    public static bool OnFootMute;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        if (car == null) car = GetComponentInParent<ArcadeCarController>();
        if (engineLoop != null) src.clip = engineLoop;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.pitch = idlePitch;
        src.volume = idleVolume;
        src.mute = true; // 시작 시 항상 muted — Update가 timeScale 보고 풀어줌
    }

    void OnEnable()
    {
        if (src != null && src.clip != null && !src.isPlaying) src.Play();
        // OnEnable 직후도 muted 유지 (선택 보드가 첫 프레임에 timeScale=0 세팅하기 전에 소리 새는 것 방지)
        if (src != null) src.mute = true;
    }

    void Update()
    {
        if (src == null || src.clip == null) return;

        // 엔진은 "실제 런 주행 중"에만 소리. 보드/정산/차고(런 비활성) 또는 일시정지면 무음.
        // (이전엔 timeScale만 봤는데, 런 후 보드 등에서 timeScale 경로가 새서 소리 잔류 — 2026-05-22 도현 보고)
        bool driving = !OnFootMute && Time.timeScale >= 0.01f
                       && car != null
                       && Services.Run != null && Services.Run.RunActive;
        if (!driving)
        {
            src.mute = true;
            return;
        }
        src.mute = false;

        float speedNorm = Mathf.Clamp01(Mathf.Abs(car.CurrentSpeedMps) / Mathf.Max(0.1f, car.MaxSpeedMps));
        float targetPitch = Mathf.Lerp(idlePitch, maxPitch, speedNorm);
        float targetVolume = Mathf.Lerp(idleVolume, maxVolume, speedNorm);

        src.pitch = Mathf.Lerp(src.pitch, targetPitch, pitchLerpSpeed * Time.deltaTime);
        src.volume = Mathf.Lerp(src.volume, targetVolume, volumeLerpSpeed * Time.deltaTime);
    }
}
