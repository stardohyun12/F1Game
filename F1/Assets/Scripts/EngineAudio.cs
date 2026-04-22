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
    [SerializeField] private float idleVolume = 0.35f;
    [SerializeField] private float maxVolume = 0.9f;
    [SerializeField] private float volumeLerpSpeed = 4f;

    private AudioSource src;

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
        if (src == null || src.clip == null || car == null) return;

        // 게임 일시정지 (주문 보드 / 정산 화면) 중엔 엔진 즉시 무음 (mute로 확실히 차단)
        if (Time.timeScale < 0.01f)
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
