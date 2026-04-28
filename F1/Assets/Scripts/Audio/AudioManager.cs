using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전역 오디오 매니저 (Day 25, Phase 1 Week 4).
///
/// 인프라만 + 후크 자동 등록. 도현은 인스펙터에서 AudioClip 4종(+) 할당하면 작동.
///
/// 후크 (이벤트 → SFX):
/// - CargoBox.OnImpact     → impactClip (volume=속도 비례)
/// - CargoBox.OnTotaled    → totaledClip
/// - DeliveryManager.OnDeliveryCompleted → deliveryClip
/// - UpgradeSystem.OnUpgradePurchased    → upgradeBoughtClip
/// - RunController.LastRunSuccess(true)  → resultSuccessClip
/// - LastRunSuccess(false)               → resultFailClip
///
/// timeScale=0(정산 화면)에서도 작동. AudioSource.ignoreListenerPause=true.
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips (도현 인스펙터 할당)")]
    [SerializeField] private AudioClip cargoImpactClip;
    [SerializeField] private AudioClip cargoTotaledClip;
    [SerializeField] private AudioClip deliveryClip;
    [SerializeField] private AudioClip upgradeBoughtClip;
    [SerializeField] private AudioClip resultSuccessClip;
    [SerializeField] private AudioClip resultFailClip;

    [Header("Music")]
    [SerializeField] private AudioClip runMusicClip;
    // 도현 #3-2 (세션 16): 소리 너무 큼 + 실제같음. 음악 0.4→0.18.
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.18f;

    [Header("Pool")]
    [Tooltip("동시에 재생 가능한 SFX 수.")]
    [SerializeField] private int sfxPoolSize = 6;
    // 도현 #3-2: SFX 1.0→0.55 (전반 인하). 카툰 톤이라 실제 음원 그대로면 위화감.
    [SerializeField, Range(0f, 1f)] private float sfxMasterVolume = 0.55f;

    AudioSource[] sfxSources;
    AudioSource musicSource;
    int sfxIndex;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // SFX 풀
        sfxSources = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.ignoreListenerPause = true; // timeScale=0 무관
            sfxSources[i] = src;
        }

        // Music
        var mgo = new GameObject("Music");
        mgo.transform.SetParent(transform);
        musicSource = mgo.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        // 후크 자동 등록
        HookEvents();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (runMusicClip != null) PlayMusic(runMusicClip);
    }

    void HookEvents()
    {
        var cargo = FindFirstObjectByType<CargoBox>();
        if (cargo != null)
        {
            cargo.OnImpact += OnCargoImpact;
            cargo.OnTotaled += OnCargoTotaled;
        }
        var dm = FindFirstObjectByType<DeliveryManager>();
        if (dm != null)
        {
            dm.OnDeliveryCompleted.AddListener(_ => PlayClip(deliveryClip));
        }
        var ups = UpgradeSystem.Instance;
        if (ups != null)
        {
            ups.OnUpgradePurchased += _ => PlayClip(upgradeBoughtClip);
        }
    }

    void OnCargoImpact(float speedKmh)
    {
        // 충돌 속도에 따라 볼륨 스케일. PlayClip 내부에서 다시 sfxMasterVolume 곱 — 여기선 0~1만.
        float v = Mathf.Clamp01(speedKmh / 50f);
        PlayClip(cargoImpactClip, v);
    }

    void OnCargoTotaled() => PlayClip(cargoTotaledClip);

    public void PlayResultSfx(bool success)
    {
        PlayClip(success ? resultSuccessClip : resultFailClip);
    }

    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSources == null || sfxSources.Length == 0) return;
        var src = sfxSources[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
        src.volume = volume * sfxMasterVolume;
        src.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip, bool fadeIn = false)
    {
        if (clip == null) { StopMusic(); return; }
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
}
