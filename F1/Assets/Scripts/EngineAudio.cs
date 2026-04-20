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
    }

    void OnEnable()
    {
        if (src != null && src.clip != null && !src.isPlaying) src.Play();
    }

    void Update()
    {
        if (src == null || src.clip == null || car == null) return;

        float speedNorm = Mathf.Clamp01(Mathf.Abs(car.CurrentSpeedMps) / Mathf.Max(0.1f, car.MaxSpeedMps));
        float targetPitch = Mathf.Lerp(idlePitch, maxPitch, speedNorm);
        float targetVolume = Mathf.Lerp(idleVolume, maxVolume, speedNorm);

        src.pitch = Mathf.Lerp(src.pitch, targetPitch, pitchLerpSpeed * Time.deltaTime);
        src.volume = Mathf.Lerp(src.volume, targetVolume, volumeLerpSpeed * Time.deltaTime);
    }
}
